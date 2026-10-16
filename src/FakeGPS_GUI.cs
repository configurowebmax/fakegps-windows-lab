using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO.Ports;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using System.Device.Location;

namespace FakeGPS.Visual
{
    static class Program
    {
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [STAThread]
        static void Main()
        {
            try { SetProcessDPIAware(); } catch { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public class MainForm : Form
    {
        // UI Components
        private TextBox txtSmartInput;
        private Label lblLatVal;
        private Label lblLonVal;
        private TextBox txtXyzValue;
        private Button btnCopyXyz;
        private Button btnApplyLocation;
        private Button btnOpenGpsComplete;
        private Button btnEmitNmea;
        private Button btnCheckWindows;
        private Button btnOpenMaps;
        private Label lblSensorStatus;
        private Panel pnlStatusIndicator;

        // State
        private double _currentLat = 52.571682;
        private double _currentLon = 13.444128;
        private volatile bool _isEmitting = false;
        private Thread _emitterThread;
        private SerialPort _serialPort;
        private readonly object _nmeaLock = new object();

        // Colors
        private readonly Color ColBg = Color.FromArgb(248, 250, 252);        // Slate 50
        private readonly Color ColCard = Color.White;
        private readonly Color ColBorder = Color.FromArgb(226, 232, 240);    // Slate 200
        private readonly Color ColPrimary = Color.FromArgb(37, 99, 235);     // Blue 600
        private readonly Color ColPrimaryHover = Color.FromArgb(29, 78, 216);// Blue 700
        private readonly Color ColSuccess = Color.FromArgb(16, 185, 129);    // Emerald 500
        private readonly Color ColTextDark = Color.FromArgb(15, 23, 42);     // Slate 900
        private readonly Color ColTextMuted = Color.FromArgb(100, 116, 139); // Slate 500

        public MainForm()
        {
            InitializeUi();
            UpdateCoordinates(_currentLat, _currentLon);
            CheckCurrentWindowsLocationAsync();
        }

        private void InitializeUi()
        {
            this.Text = "FakeGPS Visual - Panel de Ubicación Windows 11";
            this.Size = new Size(680, 780);
            this.MinimumSize = new Size(680, 780);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            this.BackColor = ColBg;
            this.ForeColor = ColTextDark;

            var mainContainer = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(24, 16, 24, 24)
            };
            this.Controls.Add(mainContainer);

            int curY = 10;

            // 1. Header
            var lblTitle = new Label
            {
                Text = "FakeGPS Visual",
                Font = new Font("Segoe UI", 18f, FontStyle.Bold),
                ForeColor = ColPrimary,
                Location = new Point(0, curY),
                AutoSize = true
            };
            mainContainer.Controls.Add(lblTitle);

            var lblSubtitle = new Label
            {
                Text = "Simulador visual de geolocalización para Windows 11, Chrome y Edge",
                Font = new Font("Segoe UI", 9.5f),
                ForeColor = ColTextMuted,
                Location = new Point(2, curY + 34),
                AutoSize = true
            };
            mainContainer.Controls.Add(lblSubtitle);
            curY += 70;

            // 2. Card: Status Sensor Bar
            var cardStatus = CreateCard(0, curY, 610, 50, mainContainer);
            pnlStatusIndicator = new Panel
            {
                Size = new Size(12, 12),
                Location = new Point(16, 19),
                BackColor = ColSuccess
            };
            cardStatus.Controls.Add(pnlStatusIndicator);

            lblSensorStatus = new Label
            {
                Text = "Consultando sensor de Windows...",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = ColTextDark,
                Location = new Point(36, 16),
                AutoSize = true
            };
            cardStatus.Controls.Add(lblSensorStatus);
            curY += 62;

            // 3. Card: Pegado Inteligente
            var cardSmart = CreateCard(0, curY, 610, 150, mainContainer);
            var lblSmartTitle = new Label
            {
                Text = "1. Pegado Inteligente (Google Maps)",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Location = new Point(16, 12),
                AutoSize = true
            };
            cardSmart.Controls.Add(lblSmartTitle);

            var lblSmartHelp = new Label
            {
                Text = "Pega aquí coordenadas o cualquier link de Google Maps (ej: 52.57168, 13.44412 o https://maps.google.com/...):",
                Font = new Font("Segoe UI", 9f),
                ForeColor = ColTextMuted,
                Location = new Point(16, 36),
                AutoSize = true
            };
            cardSmart.Controls.Add(lblSmartHelp);

            txtSmartInput = new TextBox
            {
                Text = "52.571682, 13.444128",
                Font = new Font("Consolas", 10.5f),
                Location = new Point(16, 62),
                Size = new Size(576, 28)
            };
            txtSmartInput.TextChanged += TxtSmartInput_TextChanged;
            cardSmart.Controls.Add(txtSmartInput);

            var pnlCoordBadges = new Panel { Location = new Point(16, 100), Size = new Size(576, 36) };
            lblLatVal = new Label
            {
                Text = "Latitud: 52.571682",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = ColPrimary,
                Location = new Point(0, 8),
                AutoSize = true
            };
            pnlCoordBadges.Controls.Add(lblLatVal);

            lblLonVal = new Label
            {
                Text = "Longitud: 13.444128",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = ColPrimary,
                Location = new Point(220, 8),
                AutoSize = true
            };
            pnlCoordBadges.Controls.Add(lblLonVal);
            cardSmart.Controls.Add(pnlCoordBadges);
            curY += 162;

            // 4. Card: Presets
            var cardPresets = CreateCard(0, curY, 610, 115, mainContainer);
            var lblPresetsTitle = new Label
            {
                Text = "2. Destinos Populares Rápidos",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Location = new Point(16, 12),
                AutoSize = true
            };
            cardPresets.Controls.Add(lblPresetsTitle);

            var flowPresets = new FlowLayoutPanel
            {
                Location = new Point(16, 40),
                Size = new Size(580, 65),
                AutoScroll = false
            };
            AddPresetButton(flowPresets, "🇩🇪 Berlín", 52.571682, 13.444128);
            AddPresetButton(flowPresets, "🇧🇷 Río de Janeiro", -22.951939, -43.210564);
            AddPresetButton(flowPresets, "🇨🇴 Bogotá", 4.710989, -74.072092);
            AddPresetButton(flowPresets, "🇪🇸 Madrid", 40.416775, -3.703790);
            AddPresetButton(flowPresets, "🇲🇽 CDMX", 19.432608, -99.133208);
            AddPresetButton(flowPresets, "🇫🇷 París", 48.856614, 2.352222);
            AddPresetButton(flowPresets, "🇺🇸 Nueva York", 40.712776, -74.005974);
            AddPresetButton(flowPresets, "🇯🇵 Tokio", 35.676192, 139.650311);
            cardPresets.Controls.Add(flowPresets);
            curY += 127;

            // 5. Card: Formato exacto para GPSDirect (Casilla X,Y,Z)
            var cardGpsDirect = CreateCard(0, curY, 610, 195, mainContainer);
            var lblGpsTitle = new Label
            {
                Text = "3. Aplicar en GPSDirect (Casilla X, Y, Z)",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                Location = new Point(16, 12),
                AutoSize = true
            };
            cardGpsDirect.Controls.Add(lblGpsTitle);

            var lblGpsDesc = new Label
            {
                Text = "GPSDirect requiere X=Longitud, Y=Latitud, Z=Altitud. Esta casilla ya lo tiene listo:",
                Font = new Font("Segoe UI", 9f),
                ForeColor = ColTextMuted,
                Location = new Point(16, 36),
                AutoSize = true
            };
            cardGpsDirect.Controls.Add(lblGpsDesc);

            txtXyzValue = new TextBox
            {
                ReadOnly = true,
                BackColor = Color.FromArgb(241, 245, 249),
                Font = new Font("Consolas", 11f, FontStyle.Bold),
                ForeColor = Color.DarkRed,
                Location = new Point(16, 62),
                Size = new Size(390, 29)
            };
            cardGpsDirect.Controls.Add(txtXyzValue);

            btnCopyXyz = new Button
            {
                Text = "📋 Copiar X,Y,Z",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = ColPrimary,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(415, 60),
                Size = new Size(177, 33),
                Cursor = Cursors.Hand
            };
            btnCopyXyz.FlatAppearance.BorderSize = 0;
            btnCopyXyz.Click += BtnCopyXyz_Click;
            cardGpsDirect.Controls.Add(btnCopyXyz);

            btnApplyLocation = new Button
            {
                Text = "Aplicar ubicación",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = ColSuccess,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(415, 100),
                Size = new Size(177, 33),
                Cursor = Cursors.Hand
            };
            btnApplyLocation.FlatAppearance.BorderSize = 0;
            btnApplyLocation.Click += BtnApplyLocation_Click;
            cardGpsDirect.Controls.Add(btnApplyLocation);

            btnOpenGpsComplete = new Button
            {
                Text = "🚀 Abrir GPSComplete (Driver)",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                BackColor = Color.FromArgb(241, 245, 249),
                FlatStyle = FlatStyle.Flat,
                Location = new Point(16, 145),
                Size = new Size(250, 36),
                Cursor = Cursors.Hand
            };
            btnOpenGpsComplete.FlatAppearance.BorderColor = ColBorder;
            btnOpenGpsComplete.Click += (s, e) => {
                StartGpsComplete();
                return;
                /*
                try {
                    Process.Start("C:\\GPSComplete\\gpscomplete.exe");
                } catch {
                    try { Process.Start("\\\\VBOXSVR\\fakeGPS\\tools\\gpscomplete.exe"); }
                    catch (Exception ex) { MessageBox.Show("No se encontró gpscomplete.exe: " + ex.Message); }
                }
            };*/
            };
            cardGpsDirect.Controls.Add(btnOpenGpsComplete);

            btnEmitNmea = new Button
            {
                Text = "📡 Emitir por COM10 (NMEA)",
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                BackColor = Color.FromArgb(241, 245, 249),
                FlatStyle = FlatStyle.Flat,
                Location = new Point(280, 145),
                Size = new Size(312, 36),
                Cursor = Cursors.Hand
            };
            btnEmitNmea.FlatAppearance.BorderColor = ColBorder;
            btnEmitNmea.Click += BtnEmitNmea_Click;
            cardGpsDirect.Controls.Add(btnEmitNmea);
            curY += 207;

            // 6. Action buttons: Google Maps and Test
            btnOpenMaps = new Button
            {
                Text = "🗺️ Ver Esta Ubicación en Google Maps",
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                BackColor = ColSuccess,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(0, curY),
                Size = new Size(360, 48),
                Cursor = Cursors.Hand
            };
            btnOpenMaps.FlatAppearance.BorderSize = 0;
            btnOpenMaps.Click += (s, e) => {
                string url = string.Format(CultureInfo.InvariantCulture, "https://www.google.com/maps/@{0},{1},15z", _currentLat, _currentLon);
                try {
                    Process.Start("C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe", "\"" + url + "\"");
                } catch {
                    Process.Start(url);
                }
            };
            mainContainer.Controls.Add(btnOpenMaps);

            btnCheckWindows = new Button
            {
                Text = "🔄 Probar Sensor Windows",
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                BackColor = ColCard,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(375, curY),
                Size = new Size(235, 48),
                Cursor = Cursors.Hand
            };
            btnCheckWindows.FlatAppearance.BorderColor = ColBorder;
            btnCheckWindows.Click += (s, e) => CheckCurrentWindowsLocationAsync();
            mainContainer.Controls.Add(btnCheckWindows);

            this.FormClosing += (s, e) => StopNmeaEmission();
        }

        private void StartGpsComplete()
        {
            string[] candidates = {
                "C:\\GPSComplete\\gpscomplete.exe",
                "\\\\VBOXSVR\\fakeGPS\\tools\\gpscomplete.exe",
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools\\gpscomplete.exe")
            };

            string gpsPath = null;
            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    gpsPath = candidate;
                    break;
                }
            }

            if (gpsPath == null)
            {
                MessageBox.Show(
                    "No se encontro GPSComplete. Revisa \\\\VBOXSVR\\fakeGPS\\tools.",
                    "GPSComplete", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = gpsPath,
                    Verb = "runas",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo iniciar GPSComplete: " + ex.Message,
                    "GPSComplete", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ApplyWindowsSensorLocation(double lat, double lon)
        {
            string[] candidates = {
                "C:\\GPSComplete\\Aplicar-Ubicacion-Windows.bat",
                "\\\\VBOXSVR\\fakeGPS\\Aplicar-Ubicacion-Windows.bat",
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Aplicar-Ubicacion-Windows.bat")
            };

            string batchPath = null;
            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    batchPath = candidate;
                    break;
                }
            }

            if (batchPath == null)
            {
                MessageBox.Show(
                    "No se encontro el aplicador del sensor Windows. Ejecuta install_visual_app.bat.",
                    "FakeGPS", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string args = string.Format(
                CultureInfo.InvariantCulture,
                "/c \"\"{0}\" {1:F6} {2:F6}\"",
                batchPath, lat, lon);

            Process.Start(new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = args,
                Verb = "runas",
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(batchPath)
            });

            lblSensorStatus.Text = "Aplicando ubicacion al sensor Windows... acepta el permiso de administrador.";
            lblSensorStatus.ForeColor = ColPrimary;
            pnlStatusIndicator.BackColor = Color.Orange;
        }

        private Panel CreateCard(int x, int y, int w, int h, Control parent)
        {
            var pnl = new Panel
            {
                Location = new Point(x, y),
                Size = new Size(w, h),
                BackColor = ColCard,
                BorderStyle = BorderStyle.None
            };
            pnl.Paint += (s, e) => {
                using (var pen = new Pen(ColBorder, 1))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, w - 1, h - 1);
                }
            };
            parent.Controls.Add(pnl);
            return pnl;
        }

        private void AddPresetButton(FlowLayoutPanel panel, string name, double lat, double lon)
        {
            var btn = new Button
            {
                Text = name,
                AutoSize = true,
                Height = 32,
                BackColor = Color.FromArgb(241, 245, 249),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9f),
                Cursor = Cursors.Hand,
                Margin = new Padding(3, 3, 3, 3)
            };
            btn.FlatAppearance.BorderColor = ColBorder;
            btn.Click += (s, e) => {
                txtSmartInput.Text = string.Format(CultureInfo.InvariantCulture, "{0}, {1}", lat, lon);
            };
            panel.Controls.Add(btn);
        }

        private void TxtSmartInput_TextChanged(object sender, EventArgs e)
        {
            string raw = txtSmartInput.Text.Trim();
            if (string.IsNullOrEmpty(raw)) return;

            // Check if it's a Google Maps URL with @lat,lon
            var urlMatch = Regex.Match(raw, @"@([-+]?\d{1,2}\.\d+),([-+]?\d{1,3}\.\d+)");
            if (urlMatch.Success)
            {
                double la = double.Parse(urlMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                double lo = double.Parse(urlMatch.Groups[2].Value, CultureInfo.InvariantCulture);
                UpdateCoordinates(la, lo);
                return;
            }

            // Standard lat,lon match
            var coordMatch = Regex.Match(raw, @"([-+]?\d{1,2}(?:\.\d+)?)\s*[,;\s]\s*([-+]?\d{1,3}(?:\.\d+)?)");
            if (coordMatch.Success)
            {
                double la, lo;
                if (double.TryParse(coordMatch.Groups[1].Value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out la) &&
                    double.TryParse(coordMatch.Groups[2].Value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out lo))
                {
                    if (la >= -90 && la <= 90 && lo >= -180 && lo <= 180)
                    {
                        UpdateCoordinates(la, lo);
                    }
                }
            }
        }

        private void UpdateCoordinates(double lat, double lon)
        {
            _currentLat = lat;
            _currentLon = lon;

            lblLatVal.Text = string.Format(CultureInfo.InvariantCulture, "Latitud: {0:F6}", lat);
            lblLonVal.Text = string.Format(CultureInfo.InvariantCulture, "Longitud: {0:F6}", lon);

            // X = Longitude, Y = Latitude, Z = Altitude
            txtXyzValue.Text = string.Format(CultureInfo.InvariantCulture, "{0:F6},{1:F6},0", lon, lat);

            lock (_nmeaLock)
            {
                // Update live emission if running
            }
        }

        private void BtnCopyXyz_Click(object sender, EventArgs e)
        {
            try
            {
                Clipboard.SetText(txtXyzValue.Text);
                btnCopyXyz.Text = "✅ ¡Copiado!";
                btnCopyXyz.BackColor = ColSuccess;

                var timer = new System.Windows.Forms.Timer { Interval = 2000 };
                timer.Tick += (s, args) => {
                    btnCopyXyz.Text = "📋 Copiar X,Y,Z";
                    btnCopyXyz.BackColor = ColPrimary;
                    timer.Stop();
                    timer.Dispose();
                };
                timer.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error al copiar: " + ex.Message);
            }
        }

        private void BtnApplyLocation_Click(object sender, EventArgs e)
        {
            string raw = txtSmartInput.Text.Trim();
            if (string.IsNullOrWhiteSpace(raw) ||
                _currentLat < -90 || _currentLat > 90 ||
                _currentLon < -180 || _currentLon > 180)
            {
                MessageBox.Show("Introduce una coordenada válida en formato latitud,longitud.",
                    "Ubicación inválida", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtSmartInput.Focus();
                return;
            }

            try
            {
                ApplyWindowsSensorLocation(_currentLat, _currentLon);

                // The worker reads _currentLat/_currentLon on every cycle, so an
                // active stream changes location without restarting the receiver.
                if (!_isEmitting)
                {
                    StartNmeaEmission();
                }

                if (_isEmitting)
                {
                    lblSensorStatus.Text = string.Format(
                        CultureInfo.InvariantCulture,
                        "Ubicación aplicada: {0:F6}, {1:F6}. NMEA activo en COM10 -> COM11.",
                        _currentLat, _currentLon);
                    lblSensorStatus.ForeColor = ColSuccess;
                    pnlStatusIndicator.BackColor = ColSuccess;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo aplicar la ubicación: " + ex.Message,
                    "FakeGPS", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnEmitNmea_Click(object sender, EventArgs e)
        {
            if (_isEmitting)
            {
                StopNmeaEmission();
                btnEmitNmea.Text = "📡 Emitir por COM10 (NMEA)";
                btnEmitNmea.BackColor = Color.FromArgb(241, 245, 249);
            }
            else
            {
                StartNmeaEmission();
            }
        }

        private void StartNmeaEmission()
        {
            try
            {
                _serialPort = new SerialPort("COM10", 4800, Parity.None, 8, StopBits.One)
                {
                    NewLine = "\r\n",
                    Encoding = Encoding.ASCII
                };
                _serialPort.Open();

                _isEmitting = true;
                _emitterThread = new Thread(NmeaEmitWorker) { IsBackground = true };
                _emitterThread.Start();

                btnEmitNmea.Text = "⏹️ Detener Emisión COM10";
                btnEmitNmea.BackColor = Color.FromArgb(254, 226, 226); // Light red
            }
            catch (Exception ex)
            {
                MessageBox.Show("No se pudo abrir COM10: " + ex.Message + "\nVerifica que com0com esté activo.");
            }
        }

        private void StopNmeaEmission()
        {
            _isEmitting = false;
            if (_serialPort != null)
            {
                try { if (_serialPort.IsOpen) _serialPort.Close(); } catch { }
                _serialPort.Dispose();
                _serialPort = null;
            }
        }

        private void NmeaEmitWorker()
        {
            var inv = CultureInfo.InvariantCulture;
            while (_isEmitting && _serialPort != null && _serialPort.IsOpen)
            {
                double lat, lon;
                lock (_nmeaLock)
                {
                    lat = _currentLat;
                    lon = _currentLon;
                }

                DateTime utc = DateTime.UtcNow;
                string time = utc.ToString("HHmmss", inv) + ".00";
                string date = utc.ToString("ddMMyy", inv);

                string latStr, latHem, lonStr, lonHem;
                ToNmeaCoord(lat, true, out latStr, out latHem);
                ToNmeaCoord(lon, false, out lonStr, out lonHem);

                string gga = WrapNmea(string.Format(inv, "GPGGA,{0},{1},{2},{3},{4},1,08,0.9,25.0,M,0.0,M,,", time, latStr, latHem, lonStr, lonHem));
                string rmc = WrapNmea(string.Format(inv, "GPRMC,{0},A,{1},{2},{3},{4},0.0,0.0,{5},,,A", time, latStr, latHem, lonStr, lonHem, date));
                string gsa = WrapNmea("GPGSA,A,3,02,05,09,12,15,18,21,25,,,,,1.6,0.9,1.3");

                try
                {
                    _serialPort.WriteLine(gga);
                    _serialPort.WriteLine(rmc);
                    _serialPort.WriteLine(gsa);
                }
                catch { break; }

                Thread.Sleep(1000);
            }
        }

        private static void ToNmeaCoord(double val, bool isLat, out string text, out string hem)
        {
            hem = isLat ? (val >= 0 ? "N" : "S") : (val >= 0 ? "E" : "W");
            double abs = Math.Abs(val);
            int deg = (int)Math.Floor(abs);
            double min = (abs - deg) * 60.0;
            string degFmt = isLat ? "00" : "000";
            text = deg.ToString(degFmt, CultureInfo.InvariantCulture) + min.ToString("00.0000", CultureInfo.InvariantCulture);
        }

        private static string WrapNmea(string body)
        {
            byte cs = 0;
            foreach (char c in body) cs ^= (byte)c;
            return "$" + body + "*" + cs.ToString("X2", CultureInfo.InvariantCulture);
        }

        private void CheckCurrentWindowsLocationAsync()
        {
            lblSensorStatus.Text = "Consultando sensor de Windows 11...";
            lblSensorStatus.ForeColor = ColPrimary;
            pnlStatusIndicator.BackColor = Color.Orange;

            ThreadPool.QueueUserWorkItem(state => {
                string statusText = "";
                Color statusColor = ColTextDark;
                Color indColor = ColSuccess;

                try
                {
                    using (var watcher = new GeoCoordinateWatcher(GeoPositionAccuracy.High))
                    {
                        var evt = new ManualResetEvent(false);
                        watcher.PositionChanged += (s, e) => {
                            if (!e.Position.Location.IsUnknown && !double.IsNaN(e.Position.Location.Latitude))
                                evt.Set();
                        };
                        watcher.Start();
                        evt.WaitOne(3000);

                        var loc = watcher.Position.Location;
                        if (loc.IsUnknown || double.IsNaN(loc.Latitude))
                        {
                            statusText = "Windows reporta: Estado " + watcher.Status.ToString() + " (Pulsa la mira en Google Maps)";
                            statusColor = Color.DarkOrange;
                            indColor = Color.Orange;
                        }
                        else
                        {
                            statusText = string.Format(CultureInfo.InvariantCulture,
                                "Windows Activo: Lat {0:F6}, Lon {1:F6} (Precisión: {2:F0}m)",
                                loc.Latitude, loc.Longitude, loc.HorizontalAccuracy);
                            statusColor = ColTextDark;
                            indColor = ColSuccess;
                        }
                    }
                }
                catch (Exception ex)
                {
                    statusText = "Error al leer sensor: " + ex.Message;
                    statusColor = Color.Red;
                    indColor = Color.Red;
                }

                this.Invoke((Action)(() => {
                    lblSensorStatus.Text = statusText;
                    lblSensorStatus.ForeColor = statusColor;
                    pnlStatusIndicator.BackColor = indColor;
                }));
            });
        }
    }
}
