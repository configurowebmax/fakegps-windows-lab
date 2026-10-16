using System;
using System.Device.Location;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Win32;

namespace FakeGPS
{
    class Program
    {
        private const string LatProp = "SENSOR_PROPERTY_LATITUDE";
        private const string LongProp = "SENSOR_PROPERTY_LONGITUDE";
        private static readonly Regex LatLongRegex = new Regex(@"^([-+]?\d{1,2}([.]\d+)?),\s*([-+]?\d{1,3}([.]\d+)?)$");

        static void Main(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                PrintHelp();
                return;
            }

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg == "-h" || arg == "--help" || arg == "/?")
                {
                    PrintHelp();
                    return;
                }
                else if (arg == "-s" || arg == "--set")
                {
                    if (i + 1 >= args.Length)
                    {
                        Console.WriteLine("Error: Missing coordinate argument for -s.");
                        return;
                    }
                    string coordStr = args[++i].Trim();
                    SetCoordinates(coordStr);
                }
                else if (arg == "-g" || arg == "--get")
                {
                    GetCoordinates();
                }
                else
                {
                    if (LatLongRegex.IsMatch(arg))
                    {
                        SetCoordinates(arg);
                    }
                    else
                    {
                        Console.WriteLine("Unknown argument: " + arg);
                        PrintHelp();
                    }
                }
            }
        }

        static RegistryKey FindDeviceKey()
        {
            // First check the default hardcoded path
            string defaultPath = @"SYSTEM\CurrentControlSet\Enum\ROOT\UNKNOWN\0000\Device Parameters\FakeGPS";
            try
            {
                var k = Registry.LocalMachine.OpenSubKey(defaultPath, true);
                if (k != null) return k;
            }
            catch { }

            // Also check SENSOR category
            string sensorPath = @"SYSTEM\CurrentControlSet\Enum\ROOT\SENSOR\0000\Device Parameters\FakeGPS";
            try
            {
                var k = Registry.LocalMachine.OpenSubKey(sensorPath, true);
                if (k != null) return k;
            }
            catch { }

            // Search under ROOT\
            try
            {
                using (var root = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\ROOT"))
                {
                    if (root != null)
                    {
                        foreach (var sub in root.GetSubKeyNames())
                        {
                            using (var subKey = root.OpenSubKey(sub))
                            {
                                if (subKey == null) continue;
                                foreach (var inst in subKey.GetSubKeyNames())
                                {
                                    string testPath = string.Format(@"SYSTEM\CurrentControlSet\Enum\ROOT\{0}\{1}\Device Parameters\FakeGPS", sub, inst);
                                    var found = Registry.LocalMachine.OpenSubKey(testPath, true);
                                    if (found != null) return found;
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            return null;
        }

        static void SetCoordinates(string coordStr)
        {
            var match = LatLongRegex.Match(coordStr);
            if (!match.Success)
            {
                Console.WriteLine("Error: Invalid coordinate format. Expected <lat,long> (e.g. -22.9068,-43.1729)");
                return;
            }

            string[] parts = coordStr.Split(',');
            if (parts.Length != 2)
            {
                Console.WriteLine("Error: Invalid coordinate format.");
                return;
            }

            double lat = Convert.ToDouble(parts[0].Trim(), CultureInfo.InvariantCulture);
            double lon = Convert.ToDouble(parts[1].Trim(), CultureInfo.InvariantCulture);

            try
            {
                using (var key = FindDeviceKey())
                {
                    if (key == null)
                    {
                        Console.WriteLine("Error: FakeGPS registry key not found.");
                        Console.WriteLine("Make sure FakeGPS is installed in Device Manager and you run CMD as Administrator.");
                        return;
                    }

                    // Write as CultureInvariant strings so Windows & C runtime read dot decimals properly
                    key.SetValue(LatProp, lat.ToString(CultureInfo.InvariantCulture), RegistryValueKind.String);
                    key.SetValue(LongProp, lon.ToString(CultureInfo.InvariantCulture), RegistryValueKind.String);
                }

                Console.WriteLine("The following location has been set in the driver's registry settings:");
                Console.WriteLine("Lat:\t{0}", lat.ToString(CultureInfo.InvariantCulture));
                Console.WriteLine("Long:\t{0}", lon.ToString(CultureInfo.InvariantCulture));
                Console.WriteLine("\n[EXITO] Coordenadas guardadas correctamente.");
                Console.WriteLine("Para refrescar: Deshabilita y vuelve a habilitar 'FakeGPS Geolocation Driver' en el Administrador de dispositivos (o reinicia la VM).");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error writing to registry: " + ex.Message);
            }
        }

        static void GetCoordinates()
        {
            try
            {
                using (var watcher = new GeoCoordinateWatcher())
                {
                    watcher.TryStart(true, TimeSpan.FromMilliseconds(2000));
                    Thread.Sleep(1000);

                    var loc = watcher.Position.Location;
                    Console.WriteLine("The following location has been returned from the Windows location API:");
                    Console.WriteLine("Lat:\t{0}", loc.Latitude.ToString(CultureInfo.InvariantCulture));
                    Console.WriteLine("Long:\t{0}", loc.Longitude.ToString(CultureInfo.InvariantCulture));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading location API: " + ex.Message);
            }
        }

        static void PrintHelp()
        {
            Console.WriteLine("FakeGPS CLI (Fixed Culture Edition)");
            Console.WriteLine("Usage: FakeGPS.exe -command");
            Console.WriteLine("  FakeGPS.exe -s <lat,long>   Set latitude and longitude (e.g. -22.9068,-43.1729)");
            Console.WriteLine("  FakeGPS.exe -g              Get current location from Windows Location API");
            Console.WriteLine("  FakeGPS.exe -h              Show this help");
        }
    }
}
