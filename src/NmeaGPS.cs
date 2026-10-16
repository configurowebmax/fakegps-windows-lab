// NmeaGPS - Emulador de receptor GPS por puerto serie (NMEA 0183)
// Emite $GPGGA, $GPRMC, $GPGSA y $GPGSV una vez por segundo con una posicion fija.
//
// Uso:
//   NmeaGPS.exe --port COM10 --lat 48.8566 --lon 2.3522 [--baud 4800]
//   NmeaGPS.exe --stdout --lat 48.8566 --lon 2.3522        (prueba sin puerto)
//
// Mientras corre, escribe "lat,lon" + Enter para moverte, o "q" para salir.

using System;
using System.Globalization;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace NmeaGPS
{
    class Program
    {
        static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
        static readonly object Sync = new object();
        static double _lat = 48.8566, _lon = 2.3522;
        static volatile bool _running = true;

        static int Main(string[] args)
        {
            string port = null;
            int baud = 4800;
            bool toStdout = false;

            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i].ToLowerInvariant();
                if (a == "--port" && i + 1 < args.Length) port = args[++i];
                else if (a == "--baud" && i + 1 < args.Length) baud = int.Parse(args[++i], Inv);
                else if (a == "--lat" && i + 1 < args.Length) _lat = double.Parse(args[++i], Inv);
                else if (a == "--lon" && i + 1 < args.Length) _lon = double.Parse(args[++i], Inv);
                else if (a == "--stdout") toStdout = true;
                else if (a == "--list") { ListPorts(); return 0; }
                else { PrintHelp(); return 1; }
            }

            if (!toStdout && port == null)
            {
                PrintHelp();
                Console.WriteLine();
                ListPorts();
                return 1;
            }

            SerialPort sp = null;
            if (!toStdout)
            {
                try
                {
                    sp = new SerialPort(port, baud, Parity.None, 8, StopBits.One);
                    sp.NewLine = "\r\n";
                    sp.Encoding = Encoding.ASCII;
                    sp.WriteTimeout = 1000;
                    sp.Open();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ERROR abriendo {0}: {1}", port, ex.Message);
                    ListPorts();
                    return 2;
                }
                Console.WriteLine("Emitiendo NMEA por {0} a {1} baudios.", port, baud);
            }

            Console.WriteLine("Posicion inicial: {0}, {1}", _lat.ToString(Inv), _lon.ToString(Inv));
            Console.WriteLine("Escribe 'lat,lon' + Enter para cambiar (ej: 40.7128,-74.0060). 'q' para salir.");

            var emitter = new Thread(() => EmitLoop(sp, toStdout)) { IsBackground = true };
            emitter.Start();

            while (_running)
            {
                string line = Console.ReadLine();
                if (line == null || line.Trim().ToLowerInvariant() == "q") break;
                string[] p = line.Split(',');
                double la, lo;
                if (p.Length == 2 &&
                    double.TryParse(p[0].Trim(), NumberStyles.Float, Inv, out la) &&
                    double.TryParse(p[1].Trim(), NumberStyles.Float, Inv, out lo) &&
                    la >= -90 && la <= 90 && lo >= -180 && lo <= 180)
                {
                    lock (Sync) { _lat = la; _lon = lo; }
                    Console.WriteLine("-> Nueva posicion: {0}, {1}", la.ToString(Inv), lo.ToString(Inv));
                }
                else if (line.Trim().Length > 0)
                {
                    Console.WriteLine("Formato invalido. Usa punto decimal: 48.8566,2.3522");
                }
            }

            _running = false;
            Thread.Sleep(200);
            if (sp != null && sp.IsOpen) sp.Close();
            return 0;
        }

        static void EmitLoop(SerialPort sp, bool toStdout)
        {
            while (_running)
            {
                double lat, lon;
                lock (Sync) { lat = _lat; lon = _lon; }
                DateTime now = DateTime.UtcNow;

                string[] sentences = BuildSentences(lat, lon, now);
                try
                {
                    foreach (string s in sentences)
                    {
                        if (toStdout) Console.WriteLine(s);
                        else sp.WriteLine(s);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error escribiendo en el puerto: " + ex.Message);
                }

                // Alinear al siguiente segundo
                int wait = 1000 - DateTime.UtcNow.Millisecond;
                Thread.Sleep(wait > 0 ? wait : 1000);
            }
        }

        static string[] BuildSentences(double lat, double lon, DateTime utc)
        {
            string time = utc.ToString("HHmmss", Inv) + ".00";
            string date = utc.ToString("ddMMyy", Inv);
            string latStr, latHem, lonStr, lonHem;
            ToNmea(lat, true, out latStr, out latHem);
            ToNmea(lon, false, out lonStr, out lonHem);

            string gga = string.Format(Inv,
                "GPGGA,{0},{1},{2},{3},{4},1,08,0.9,25.0,M,0.0,M,,",
                time, latStr, latHem, lonStr, lonHem);

            string rmc = string.Format(Inv,
                "GPRMC,{0},A,{1},{2},{3},{4},0.0,0.0,{5},,,A",
                time, latStr, latHem, lonStr, lonHem, date);

            string gsa = "GPGSA,A,3,02,05,09,12,15,18,21,25,,,,,1.6,0.9,1.3";

            // 8 satelites en 2 sentencias GSV (prn, elev, azimut, SNR)
            string gsv1 = "GPGSV,2,1,08,02,45,050,42,05,60,120,45,09,30,200,40,12,70,300,47";
            string gsv2 = "GPGSV,2,2,08,15,25,020,38,18,50,160,44,21,35,240,41,25,15,330,36";

            return new[] { Wrap(gga), Wrap(rmc), Wrap(gsa), Wrap(gsv1), Wrap(gsv2) };
        }

        // Convierte grados decimales a ddmm.mmmm / dddmm.mmmm
        static void ToNmea(double value, bool isLat, out string text, out string hem)
        {
            hem = isLat ? (value >= 0 ? "N" : "S") : (value >= 0 ? "E" : "W");
            double abs = Math.Abs(value);
            int deg = (int)Math.Floor(abs);
            double min = (abs - deg) * 60.0;
            // Evitar 60.0000 por redondeo
            if (Math.Round(min, 4) >= 60.0) { deg += 1; min = 0; }
            string degFmt = isLat ? "00" : "000";
            text = deg.ToString(degFmt, Inv) + min.ToString("00.0000", Inv);
        }

        // Agrega '$' y '*checksum' (XOR de todos los caracteres entre $ y *)
        static string Wrap(string body)
        {
            byte cs = 0;
            foreach (char c in body) cs ^= (byte)c;
            return "$" + body + "*" + cs.ToString("X2", Inv);
        }

        static void ListPorts()
        {
            string[] ports = SerialPort.GetPortNames();
            Console.WriteLine("Puertos COM disponibles: " + (ports.Length == 0 ? "(ninguno)" : string.Join(", ", ports)));
        }

        static void PrintHelp()
        {
            Console.WriteLine("NmeaGPS - Emulador de GPS NMEA 0183");
            Console.WriteLine("  NmeaGPS.exe --port COM10 --lat 48.8566 --lon 2.3522 [--baud 4800]");
            Console.WriteLine("  NmeaGPS.exe --stdout --lat 48.8566 --lon 2.3522   (solo imprime, para probar)");
            Console.WriteLine("  NmeaGPS.exe --list                                (lista puertos COM)");
        }
    }
}
