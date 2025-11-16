using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;
using System.IO;
using System.Linq;

namespace WiFiManager
{
    public class NotificationService
    {
        private const string AppId = "WiFiManager";
        private NotificationConfig _config;
        private readonly List<GrowlConnection> _growlConnections = new();
        private string? _appIconPath;
        private string? _appIconData;
        private readonly object _sendLock = new object(); // <-- ADD THIS LINE!

        public NotificationService(string? configPath = null)
        {
            _config = NotificationConfig.Load(configPath);
            LoadAppIcon();
            InitializeGrowl();
        }

        private void LoadAppIcon()
        {
            try
            {
                // Cek custom icon dari config dulu
                if (!string.IsNullOrEmpty(_config.IconPath) && File.Exists(_config.IconPath))
                {
                    _appIconPath = _config.IconPath;
                }
                else
                {
                    // Cari icon di beberapa lokasi default
                    var possiblePaths = new[]
                    {
                        Path.Combine(AppContext.BaseDirectory, "icon.png"),
                        Path.Combine(AppContext.BaseDirectory, "wifi.png"),
                        Path.Combine(AppContext.BaseDirectory, "wifimgr.png"),
                        Path.Combine(AppContext.BaseDirectory, "app.ico"),
                        Path.Combine(AppContext.BaseDirectory, "wifi.ico"),
                        Path.Combine(AppContext.BaseDirectory, "wifimgr.ico"),
                        Path.Combine(AppContext.BaseDirectory, "assets", "icon.png"),
                        Path.Combine(AppContext.BaseDirectory, "assets", "wifi.png"),
                        Path.Combine(AppContext.BaseDirectory, "assets", "wifimgr.png"),
                        Path.Combine(AppContext.BaseDirectory, "resources", "icon.png"),
                        Path.Combine(AppContext.BaseDirectory, "resources", "wifi.png"),
                        Path.Combine(AppContext.BaseDirectory, "resources", "wifimgr.png")
                    };

                    foreach (var path in possiblePaths)
                    {
                        if (File.Exists(path))
                        {
                            _appIconPath = path;
                            break;
                        }
                    }
                }

                // Convert to base64 for Growl
                if (!string.IsNullOrEmpty(_appIconPath) && File.Exists(_appIconPath))
                {
                    var fileInfo = new FileInfo(_appIconPath);
                    
                    // Validate file size (max 500KB)
                    if (fileInfo.Length > 500 * 1024)
                    {
                        ColorConsole.WriteWarning($"[⚠️] Icon too large ({fileInfo.Length / 1024}KB), max 500KB. Skipping icon.");
                        _appIconPath = null;
                        return;
                    }
                    
                    var iconBytes = File.ReadAllBytes(_appIconPath);
                    _appIconData = Convert.ToBase64String(iconBytes);
                    Debug.WriteLine($"Icon loaded: {_appIconPath}");
                }
                else
                {
                    Debug.WriteLine("No icon found, using default");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to load icon: {ex.Message}");
            }
        }

        private void InitializeGrowl()
        {
            if (!_config.EnableGrowl)
            {
                ColorConsole.WriteWarning("⚠️  Growl notification: DISABLED (via config)");
                return;
            }

            if (_config.GrowlHosts == null || _config.GrowlHosts.Count == 0)
            {
                ColorConsole.WriteWarning("⚠️  Growl notification: DISABLED (no hosts configured)");
                return;
            }

            Console.WriteLine();
            ColorConsole.WriteHeader("🔌 Connecting to Growl hosts...");

            foreach (var host in _config.GrowlHosts.Where(h => h.Enabled))
            {
                var connection = new GrowlConnection(host);
                
                if (connection.CheckAvailability(_config.ConnectionTimeout))
                {
                    RegisterWithGrowl(connection).Wait();
                    _growlConnections.Add(connection);
                    ColorConsole.WriteSuccess($"✅ Connected to {connection.Host}");
                    Console.WriteLine();
                }
                else
                {
                    ColorConsole.WriteWarning($"⚠️  Cannot connect to {connection.Host}");
                    Console.WriteLine();
                }
            }

            if (_growlConnections.Count == 0)
            {
                ColorConsole.WriteWarning("⚠️  Growl notification: DISABLED (no hosts available)");
                Console.WriteLine();
            }
            else
            {
                ColorConsole.WriteSuccess($"📢 Growl notification: ENABLED ({_growlConnections.Count} host(s))");
                Console.WriteLine();
            }
        }

        private async Task RegisterWithGrowl(GrowlConnection connection)
        {
            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(connection.Host.Host, connection.Host.Port);
                using var stream = client.GetStream();

                var message = new StringBuilder();
                message.AppendLine("GNTP/1.0 REGISTER NONE");
                message.AppendLine($"Application-Name: {AppId}");

                if (!string.IsNullOrEmpty(_appIconData))
                {
                    message.AppendLine($"Application-Icon: data:image/png;base64,{_appIconData}");
                }

                message.AppendLine("Notifications-Count: 5");
                message.AppendLine();

                var notificationTypes = new[]
                {
                    "WiFi Connected",
                    "WiFi Disconnected",
                    "WiFi Changed",
                    "New WiFi Network",
                    "Signal Changed"
                };

                foreach (var type in notificationTypes)
                {
                    message.AppendLine($"Notification-Name: {type}");
                    message.AppendLine($"Notification-Display-Name: {type}");
                    message.AppendLine("Notification-Enabled: True");
                    if (!string.IsNullOrEmpty(_appIconData))
                    {
                        message.AppendLine($"Notification-Icon: data:image/png;base64,{_appIconData}");
                    }
                    message.AppendLine();
                }

                var data = Encoding.UTF8.GetBytes(message.ToString());
                await stream.WriteAsync(data, 0, data.Length);

                var buffer = new byte[1024];
                var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                var response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                if (!response.Contains("GNTP/1.0 -OK"))
                {
                    Debug.WriteLine($"Growl registration response: {response}");
                    connection.IsAvailable = false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Growl registration error on {connection.Host}: {ex.Message}");
                connection.IsAvailable = false;
            }
        }

        public void ShowNotification(string title, string message, string? iconPath = null)
        {
            // Use lock to prevent race conditions
            lock (_sendLock)
            {
                try
                {
                    var icon = iconPath ?? _appIconPath;

                    // Windows Toast
                    if (_config.EnableWindowsToast)
                    {
                        ShowWindowsNotification(title, message, icon);
                    }

                    // Growl
                    if (_config.EnableGrowl && _growlConnections.Count > 0)
                    {
                        foreach (var connection in _growlConnections.Where(c => c.IsAvailable).ToList())
                        {
                            _ = SendGrowlNotification(connection, title, message, icon);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Notification error: {ex.Message}");
                }
            }
        }

        private void ShowWindowsNotification(string title, string message, string? iconPath = null)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;

            try
            {
                var escapedTitle = EscapeForPowerShell(title);
                var escapedMessage = EscapeForPowerShell(message);

                var imageTag = "";
                if (!string.IsNullOrEmpty(iconPath) && File.Exists(iconPath))
                {
                    var escapedIconPath = iconPath.Replace("\\", "\\\\");
                    imageTag = $@"<image placement=""appLogoOverride"" src=""file:///{escapedIconPath}""/>";
                }

                var script = $@"
[Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null
[Windows.UI.Notifications.ToastNotification, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null
[Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType = WindowsRuntime] | Out-Null

$xml = @'
<toast>
    <visual>
        <binding template=""ToastGeneric"">
            {imageTag}
            <text>{escapedTitle}</text>
            <text>{escapedMessage}</text>
        </binding>
    </visual>
    <audio src=""ms-winsoundevent:Notification.Default""/>
</toast>
'@

try {{
    $XmlDocument = [Windows.Data.Xml.Dom.XmlDocument]::new()
    $XmlDocument.LoadXml($xml)
    $toast = [Windows.UI.Notifications.ToastNotification]::new($XmlDocument)
    [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier('{AppId}').Show($toast)
}} catch {{
    # Silently fail
}}
";

                var psi = new ProcessStartInfo
                {
                    FileName = "powershell.exe",
                    Arguments = $"-NoProfile -NonInteractive -WindowStyle Hidden -Command \"{script}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var process = Process.Start(psi);
                if (process != null)
                {
                    process.WaitForExit(2000);
                }
            }
            catch
            {
                try { Console.Beep(800, 200); } catch { }
            }
        }

        private async Task SendGrowlNotification(GrowlConnection connection, string title, string message, string? iconPath = null)
        {
            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(connection.Host.Host, connection.Host.Port);
                using var stream = client.GetStream();

                var notificationType = title;
                var priority = _config.NotificationPriorities.TryGetValue(title, out var p) ? p : 0;
                
                // Get sticky setting from config
                var isSticky = _config.StickyNotifications.TryGetValue(title, out var sticky) 
                    ? sticky 
                    : _config.DefaultSticky;

                var notification = new StringBuilder();
                notification.AppendLine("GNTP/1.0 NOTIFY NONE");
                notification.AppendLine($"Application-Name: {AppId}");
                notification.AppendLine($"Notification-Name: {notificationType}");
                notification.AppendLine($"Notification-Title: {title}");
                notification.AppendLine($"Notification-Text: {message}");
                notification.AppendLine($"Notification-Priority: {priority}");
                notification.AppendLine($"Notification-Sticky: {(isSticky ? "True" : "False")}");

                // Add icon
                bool iconAdded = false;

                // Untuk localhost, gunakan file path (lebih efisien)
                if ((connection.Host.Host == "127.0.0.1" || connection.Host.Host == "localhost") 
                    && !string.IsNullOrEmpty(_appIconPath) 
                    && File.Exists(_appIconPath))
                {
                    var filePath = _appIconPath.Replace("\\", "/");
                    notification.AppendLine($"Notification-Icon: file:///{filePath}");
                    iconAdded = true;
                }

                // Untuk remote host atau jika file path gagal, gunakan base64
                if (!iconAdded)
                {
                    string? iconToUse = iconPath ?? _appIconPath;

                    if (!string.IsNullOrEmpty(iconToUse) && File.Exists(iconToUse))
                    {
                        try
                        {
                            var iconBytes = File.ReadAllBytes(iconToUse);
                            var iconBase64 = Convert.ToBase64String(iconBytes);
                            var mediaType = GetMediaType(iconToUse);
                            notification.AppendLine($"Notification-Icon: data:{mediaType};base64,{iconBase64}");
                        }
                        catch (Exception iconEx)
                        {
                            Debug.WriteLine($"Failed to add icon: {iconEx.Message}");
                        }
                    }
                }

                notification.AppendLine();

                var data = Encoding.UTF8.GetBytes(notification.ToString());
                await stream.WriteAsync(data, 0, data.Length);

                var buffer = new byte[4096];
                var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                var response = Encoding.UTF8.GetString(buffer, 0, bytesRead);

                if (!response.Contains("GNTP/1.0 -OK"))
                {
                    Debug.WriteLine($"Growl notify response from {connection.Host}: {response}");
                    connection.IsAvailable = false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Growl send error to {connection.Host}: {ex.Message}");
                connection.IsAvailable = false;
            }
        }

        private string GetMediaType(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return "image/png";

            var extension = Path.GetExtension(filePath).ToLowerInvariant();

            return extension switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".ico" => "image/x-icon",
                _ => "image/png"
            };
        }

        private string EscapeForPowerShell(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }

        public void TestNotification()
        {
            ColorConsole.WriteHeader("🔔 Testing Notifications");
            Console.WriteLine();

            if (_config.EnableWindowsToast)
            {
                ColorConsole.WriteInfo("Testing Windows Toast...");
                ShowWindowsNotification("WiFi Manager Test", "Windows notification is working! 🎉", _appIconPath);
                System.Threading.Thread.Sleep(1000);
            }

            if (_config.EnableGrowl && _growlConnections.Count > 0)
            {
                ColorConsole.WriteInfo($"Testing Growl ({_growlConnections.Count} host(s))...");
                Console.WriteLine();
                
                foreach (var connection in _growlConnections.Where(c => c.IsAvailable))
                {
                    SendGrowlNotification(connection, "WiFi Manager Test", $"Growl notification from {connection.Host} is working! 🎉", _appIconPath).Wait();
                    
                    if (connection.IsAvailable)
                    {
                        ColorConsole.WriteSuccess($"✅ Growl test sent to {connection.Host}");
                        Console.WriteLine();
                    }
                    else
                    {
                        ColorConsole.WriteError($"❌ Growl test failed for {connection.Host}");
                        Console.WriteLine();
                    }
                }
            }
            else if (!_config.EnableGrowl)
            {
                ColorConsole.WriteWarning("⚠️  Growl is disabled in config");
            }
            else
            {
                ColorConsole.WriteWarning("⚠️  Growl is not available");
                ColorConsole.WriteInfo("💡 To enable Growl:");
                ColorConsole.WriteInfo("   1. Install Growl for Windows");
                ColorConsole.WriteInfo("   2. Make sure it's running");
                ColorConsole.WriteInfo("   3. Enable GNTP in Growl settings (default port 23053)");
                ColorConsole.WriteInfo("   4. Configure in wifimgr.json");
            }

            Console.WriteLine();
            ColorConsole.WriteSuccess("✅ Notification test completed");
            Console.WriteLine();

            if (!string.IsNullOrEmpty(_appIconPath))
            {
                ColorConsole.WriteInfo($"📁 Icon: {_appIconPath}");
            }
        }

        public void ReloadConfig(string? configPath = null)
        {
            _config = NotificationConfig.Load(configPath);
            _growlConnections.Clear();
            LoadAppIcon();
            InitializeGrowl();
        }
    }

    internal class GrowlConnection
    {
        public GrowlHost Host { get; }
        public bool IsAvailable { get; set; }

        public GrowlConnection(GrowlHost host)
        {
            Host = host;
            IsAvailable = false;
        }

        public bool CheckAvailability(int timeout)
        {
            try
            {
                using var client = new TcpClient();
                var connectTask = client.ConnectAsync(Host.Host, Host.Port);

                if (connectTask.Wait(timeout))
                {
                    IsAvailable = true;
                    return true;
                }

                IsAvailable = false;
                return false;
            }
            catch
            {
                IsAvailable = false;
                return false;
            }
        }
    }
}