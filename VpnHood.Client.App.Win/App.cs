﻿using VpnHood.Client.App.UI;
using System;
using System.Diagnostics;
using System.Windows.Forms;
using VpnHood.Common;
using VpnHood.Logging;
using System.Threading;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace VpnHood.Client.App
{
    internal class App : ApplicationContext
    {
        private bool _disposed = false;
        private readonly Mutex _mutex = new(false, typeof(Program).FullName);
        private NotifyIcon _notifyIcon;
        private VpnHoodApp _app;
        private VpnHoodAppUI _appUI;
        private WebViewWindow _webViewWindow;
        private FileSystemWatcher _fileSystemWatcher;
        private System.Windows.Forms.Timer _uiTimer;
        private DateTime? _updater_lastCheckTime;
        public int CheckIntervalMinutes { get; set; } = 1 * (24 * 60); // 1 day

        public App()
        {
        }

        public void Start(string[] args)
        {
            var openWindow = !args.Any(x => x.Equals("/nowindow", StringComparison.OrdinalIgnoreCase));
            var autoConnect = args.Any(x => x.Equals("/autoconnect", StringComparison.OrdinalIgnoreCase));
            var logToConsole = true;

            // Report current Version
            // Replace dot in version to prevent anonymouizer treat it as ip.
            VhLogger.Instance = VhLogger.CreateConsoleLogger();
            VhLogger.Instance.LogInformation($"{typeof(App).Assembly.ToString().Replace('.', ',')}");
            var appDataPath = new AppOptions().AppDataPath; // we use defaultPath
            var appCommandFilePath = Path.Combine(appDataPath, "appCommand.txt");

            // Make single instance
            // if you like to wait a few seconds in case that the instance is just shutting down
            if (!_mutex.WaitOne(TimeSpan.FromSeconds(0), false))
            {
                // open main window if app is already running and user run the app again
                if (openWindow)
                    File.WriteAllText(appCommandFilePath, "OpenMainWindow");
                VhLogger.Instance.LogInformation($"{nameof(App)} is already running!");
                return;
            }

            // configuring Windows Firewall
            try
            {
                OpenLocalFirewall(appDataPath);
            }
            catch { };

            // init app
            _app = VpnHoodApp.Init(new WinAppProvider(), new AppOptions() { LogToConsole = logToConsole });
            _appUI = VpnHoodAppUI.Init(new MemoryStream(Resource.SPA));

            // auto connect
            if (autoConnect && _app.UserSettings.DefaultClientProfileId != null &&
                _app.ClientProfileStore.ClientProfileItems.Any(x => x.ClientProfile.ClientProfileId == _app.UserSettings.DefaultClientProfileId))
                _app.Connect(_app.UserSettings.DefaultClientProfileId.Value).GetAwaiter();

            // create notification icon
            InitNotifyIcon();

            // Create webview if installed
            if (WebViewWindow.IsInstalled)
                _webViewWindow = new WebViewWindow(_appUI.Url, Path.Combine(_app.AppDataFolderPath, "Temp"));

            // MainWindow
            if (openWindow)
                OpenMainWindow();

            // Init command watcher for external command
            InitCommnadWatcher(appCommandFilePath);

            //Ui Timer
            InitUiTimer();

            // Message Loop
            Application.Run(this);
        }

        private void InitUiTimer()
        {
            _uiTimer = new System.Windows.Forms.Timer
            {
                Interval = 1000,
                Enabled = true
            };
            _uiTimer.Tick += (sender, e) => UpdateNotifyIconText();
            _uiTimer.Start();
        }

        private void UpdateNotifyIconText()
        {
            var stateName = _app.State.ConnectionState == AppConnectionState.None ? "Disconnected" : _app.State.ConnectionState.ToString();
            if (_notifyIcon != null)
            {
                _notifyIcon.Text = $"{AppUIResource.AppName} - {stateName}";
                if (_app.State.IsIdle) _notifyIcon.Icon = Resource.VpnDisconnectedIcon;
                else if (_app.State.ConnectionState == AppConnectionState.Connected) _notifyIcon.Icon = Resource.VpnConnectedIcon;
                else _notifyIcon.Icon = Resource.VpnConnectingIcon;
            }

            CheckForUpdate();
        }

        private void CheckForUpdate()
        {
            // read last check
            var lastCheckFilePath = Path.Combine(_app.AppDataFolderPath, "lastCheckUpdate");
            if (_updater_lastCheckTime == null)
            {
                _updater_lastCheckTime = DateTime.MinValue;
                if (File.Exists(lastCheckFilePath))
                    try { _updater_lastCheckTime = JsonSerializer.Deserialize<DateTime>(File.ReadAllText(lastCheckFilePath)); } catch { }
            }

            // check last update time
            if ((DateTime.Now - _updater_lastCheckTime).Value.TotalMinutes < CheckIntervalMinutes)
                return;

            // set checktime before chking filename
            _updater_lastCheckTime = DateTime.Now;

            // launch updater if exists
            var updaterFilePath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "updater.exe");
            if (!File.Exists(updaterFilePath))
            {
                VhLogger.Instance.LogWarning($"Could not find updater: {updaterFilePath}");
                return;
            }

            try
            {
                VhLogger.Instance.LogInformation("Cheking for new updates...");
                Process.Start(updaterFilePath, "/silent");
            }
            catch (Exception ex)
            {
                VhLogger.Instance.LogError(ex.Message);
            }
            finally
            {
                File.WriteAllText(lastCheckFilePath, JsonSerializer.Serialize(_updater_lastCheckTime));
            }
        }

        private void InitCommnadWatcher(string path)
        {
            _fileSystemWatcher = new FileSystemWatcher
            {
                Path = Path.GetDirectoryName(path),
                NotifyFilter = NotifyFilters.LastWrite,
                Filter = Path.GetFileName(path),
                IncludeSubdirectories = false,
                EnableRaisingEvents = true
            };

            _fileSystemWatcher.Changed += (sender, e) =>
            {
                try
                {
                    Thread.Sleep(100);
                    var cmd = File.ReadAllText(e.FullPath);
                    if (cmd == "OpenMainWindow")
                        OpenMainWindow();
                }
                catch { }
            };
        }

        public void OpenMainWindow()
        {
            if (_webViewWindow != null)
            {
                _webViewWindow.Show();
                return;
            }

            Process.Start(new ProcessStartInfo()
            {
                FileName = _appUI.Url,
                UseShellExecute = true,
                Verb = "open"
            });
        }

        private void InitNotifyIcon()
        {
            _notifyIcon = new NotifyIcon
            {
                Icon = Resource.VpnHoodIcon
            };
            _notifyIcon.MouseClick += (sender, e) =>
            {
                if (e.Button == MouseButtons.Left)
                    OpenMainWindow();
            };

            var menu = new ContextMenuStrip();
            menu.Items.Add(AppUIResource.Open, null, (sender, e) => OpenMainWindow());

            menu.Items.Add("-");
            var menuItem = menu.Items.Add("Connect");
            menuItem.Name = "connect";
            menuItem.Click += ConnectMenuItem_Click;

            menuItem = menu.Items.Add(AppUIResource.Disconnect);
            menuItem.Name = "disconnect";
            menuItem.Click += (sender, e) => _app.Disconnect(true);

            menu.Items.Add("-");
            menu.Items.Add(AppUIResource.Exit, null, (sender, e) => Application.Exit());
            menu.Opening += Menu_Opening;
            _notifyIcon.ContextMenuStrip = menu;
            _notifyIcon.Text = AppUIResource.AppName;
            _notifyIcon.Visible = true;
            UpdateNotifyIconText();
        }

        private void ConnectMenuItem_Click(object sender, EventArgs e)
        {
            if (_app.Settings.UserSettings.DefaultClientProfileId != null)
            {
                try
                {
                    _app.Connect(_app.Settings.UserSettings.DefaultClientProfileId.Value).GetAwaiter();
                }
                catch
                {
                    OpenMainWindow();
                }
            }
            else
            {
                OpenMainWindow();
            }
        }

        private void Menu_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var menu = (ContextMenuStrip)sender;
            menu.Items["connect"].Enabled = _app.IsIdle;
            menu.Items["disconnect"].Enabled = !_app.IsIdle && _app.State.ConnectionState != AppConnectionState.Disconnecting;
        }

        private static string FindExePath(string exe)
        {
            exe = Environment.ExpandEnvironmentVariables(exe);
            if (File.Exists(exe))
                return Path.GetFullPath(exe);

            if (Path.GetDirectoryName(exe) == string.Empty)
            {
                foreach (string test in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';'))
                {
                    string path = test.Trim();
                    if (!string.IsNullOrEmpty(path) && File.Exists(path = Path.Combine(path, exe)))
                        return Path.GetFullPath(path);
                }
            }
            throw new FileNotFoundException(new FileNotFoundException().Message, exe);
        }

        private static void OpenLocalFirewall(string appDataPath)
        {
            var lastFirewallConfig = Path.Combine(appDataPath, "lastFirewallConfig");
            var lastExeFile = File.Exists(lastFirewallConfig) ? File.ReadAllText(lastFirewallConfig) : null;
            var curExePath = Path.ChangeExtension(typeof(App).Assembly.Location, "exe");
            if (lastExeFile == curExePath)
                return;

            VhLogger.Instance.LogInformation($"Configuring Windows Defender Firewall...");
            var ruleName = "VpnHood";

            //dotnet exe
            var exePath = FindExePath("dotnet.exe");
            ProcessStartNoWindow("netsh", $"advfirewall firewall delete rule name=\"{ruleName}\" dir=in").WaitForExit();
            ProcessStartNoWindow("netsh", $"advfirewall firewall add rule  name=\"{ruleName}\" program=\"{exePath}\" protocol=TCP localport=any action=allow profile=private dir=in").WaitForExit();
            ProcessStartNoWindow("netsh", $"advfirewall firewall add rule  name=\"{ruleName}\" program=\"{exePath}\" protocol=UDP localport=any action=allow profile=private dir=in").WaitForExit();

            // vpnhood exe
            exePath = curExePath;
            if (File.Exists(exePath))
            {
                ProcessStartNoWindow("netsh", $"advfirewall firewall add rule  name=\"{ruleName}\" program=\"{exePath}\" protocol=TCP localport=any action=allow profile=private dir=in").WaitForExit();
                ProcessStartNoWindow("netsh", $"advfirewall firewall add rule  name=\"{ruleName}\" program=\"{exePath}\" protocol=UDP localport=any action=allow profile=private dir=in").WaitForExit();
            }

            // save firewall modified
            File.WriteAllText(lastFirewallConfig, curExePath);
        }

        private static Process ProcessStartNoWindow(string filename, string argument)
        {
            var processStart = new ProcessStartInfo(filename, argument)
            {
                CreateNoWindow = true
            };

            return Process.Start(processStart);
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                _notifyIcon?.Dispose();
                _appUI?.Dispose();
                _app?.Dispose();
                _fileSystemWatcher?.Dispose();
            }
            _disposed = true;

            // base
            base.Dispose(disposing);
        }
    }
}
