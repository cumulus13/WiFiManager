using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace WiFiManager
{
    public class WiFiManagerCLI
    {
        private readonly WiFiService _wifiService;
        private readonly NotificationService _notificationService;
        private Dictionary<string, int> _networkSignals = new(); // Track signal strength per network
        private HashSet<string> _seenNetworks = new(); // Track networks we've already announced

        public WiFiManagerCLI()
        {
            _wifiService = new WiFiService();
            _notificationService = new NotificationService();
        }

        public void TestNotification()
        {
            _notificationService.TestNotification();
        }

        public void ListInterfaces()
        {
            ColorConsole.WriteHeader("📡 WiFi Interfaces");
            Console.WriteLine();

            var interfaces = _wifiService.GetInterfaces();
            
            if (interfaces.Count == 0)
            {
                ColorConsole.WriteWarning("⚠️  No WiFi interfaces found");
                return;
            }

            var activeInterface = _wifiService.GetActiveInterface();
            
            ColorConsole.WriteSuccess($"Found {interfaces.Count} interface(s):");
            Console.WriteLine();

            for (int i = 0; i < interfaces.Count; i++)
            {
                var iface = interfaces[i];
                var isActive = activeInterface != null && iface.Guid == activeInterface.Guid;
                var activeIcon = isActive ? "✅ " : "   ";
                var stateIcon = iface.IsConnected ? "🟢" : "⚪";
                
                Console.Write($"{activeIcon}[{i}] {stateIcon} ");
                
                if (isActive)
                    ColorConsole.WriteColored(iface.Description, ConsoleColor.Green);
                else
                    Console.Write(iface.Description);
                
                ColorConsole.WriteColored($" ({iface.State})", ConsoleColor.Gray);
                Console.WriteLine();
            }
            Console.WriteLine();
            
            if (interfaces.Count > 1)
            {
                ColorConsole.WriteInfo("💡 Use 'wifimgr interface <index>' to switch");
                Console.WriteLine();
            }
        }

        public void SwitchInterface(int index)
        {
            var interfaces = _wifiService.GetInterfaces();
            
            if (index < 0 || index >= interfaces.Count)
            {
                ColorConsole.WriteError($"❌ Invalid interface index. Available: 0-{interfaces.Count - 1}");
                return;
            }

            if (_wifiService.SetActiveInterface(index))
            {
                var iface = interfaces[index];
                ColorConsole.WriteSuccess($"✅ Switched to interface: {iface.Description}");
            }
            else
            {
                ColorConsole.WriteError("❌ Failed to switch interface");
            }
        }

        public async Task ScanNetworks()
        {
            try
            {
                var activeInterface = _wifiService.GetActiveInterface();
                if (activeInterface == null)
                {
                    ColorConsole.WriteError("❌ No active WiFi interface");
                    return;
                }

                ColorConsole.WriteHeader($"🔍 Scanning on: {activeInterface.Description}");
                Console.WriteLine();

                var networks = _wifiService.ScanNetworks();
                
                if (networks.Count == 0)
                {
                    ColorConsole.WriteWarning("⚠️  No networks found");
                    ColorConsole.WriteInfo("💡 Try: 1) Check WiFi is enabled  2) Move closer to AP  3) Scan again");
                    return;
                }

                ColorConsole.WriteSuccess($"Found {networks.Count} network(s):");
                Console.WriteLine();

                foreach (var network in networks.OrderByDescending(n => n.SignalQuality))
                {
                    var signalIcon = GetSignalIcon(network.SignalQuality);
                    var securityIcon = network.IsSecure ? "🔒" : "🔓";
                    var connectedIcon = network.IsConnected ? "✅ " : "   ";

                    Console.Write(connectedIcon);
                    ColorConsole.WriteColored($"{signalIcon} {securityIcon} ", ConsoleColor.Yellow);
                    
                    if (network.IsConnected)
                        ColorConsole.WriteColored(network.SSID, ConsoleColor.Green);
                    else
                        Console.Write(network.SSID);
                    
                    ColorConsole.WriteColored($" ({network.SignalQuality}%)", ConsoleColor.Gray);
                    Console.WriteLine();
                }
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                ColorConsole.WriteError($"❌ Scan failed: {ex.Message}");
            }

            await Task.CompletedTask;
        }

        public void ListProfiles()
        {
            ColorConsole.WriteHeader("📋 Saved WiFi Profiles");
            Console.WriteLine();

            var profiles = _wifiService.GetProfiles();
            
            if (profiles.Count == 0)
            {
                ColorConsole.WriteWarning("⚠️  No saved profiles");
                return;
            }

            ColorConsole.WriteSuccess($"Found {profiles.Count} profile(s):");
            Console.WriteLine();

            foreach (var profile in profiles)
            {
                Console.WriteLine($"  🔹 {profile}");
            }
            Console.WriteLine();
        }

        public async Task Connect(string ssid)
        {
            ColorConsole.WriteInfo($"🔌 Connecting to '{ssid}'...");
            
            var hasProfile = _wifiService.HasProfile(ssid);
            
            if (!hasProfile)
            {
                var networks = _wifiService.ScanNetworks();
                var network = networks.FirstOrDefault(n => n.SSID == ssid);
                
                if (network == null)
                {
                    ColorConsole.WriteError($"❌ Network '{ssid}' not found");
                    return;
                }

                if (network.IsSecure)
                {
                    Console.Write("🔑 Enter password: ");
                    var password = ReadPassword();
                    Console.WriteLine();
                    
                    if (string.IsNullOrEmpty(password))
                    {
                        ColorConsole.WriteError("❌ Password cannot be empty");
                        return;
                    }

                    ColorConsole.WriteInfo("💾 Creating profile...");
                    if (!_wifiService.CreateProfile(ssid, password))
                    {
                        ColorConsole.WriteError("❌ Failed to create profile");
                        return;
                    }
                    ColorConsole.WriteSuccess("✅ Profile created");
                }
            }

            if (_wifiService.Connect(ssid))
            {
                ColorConsole.WriteSuccess($"✅ Connected to '{ssid}'");
            }
            else
            {
                ColorConsole.WriteError($"❌ Failed to connect to '{ssid}'");
            }

            await Task.CompletedTask;
        }

        public void Disconnect()
        {
            ColorConsole.WriteInfo("⛔ Disconnecting...");
            
            if (_wifiService.Disconnect())
            {
                ColorConsole.WriteSuccess("✅ Disconnected");
            }
            else
            {
                ColorConsole.WriteError("❌ Failed to disconnect");
            }
        }

        public void ShowStatus()
        {
            ColorConsole.WriteHeader("📊 WiFi Status");
            Console.WriteLine();

            // Debug: show active interface
            var activeInterface = _wifiService.GetActiveInterface();
            if (activeInterface != null)
            {
                ColorConsole.WriteColored("Interface: ", ConsoleColor.Gray);
                Console.WriteLine($"{activeInterface.Description} ({activeInterface.State})");
            }

            var status = _wifiService.GetConnectionStatus();
            
            if (status == null)
            {
                ColorConsole.WriteWarning("⚠️  Not connected on active interface");
                
                // Try to find any connected interface
                var interfaces = _wifiService.GetInterfaces();
                var connectedInterface = interfaces.FirstOrDefault(i => i.IsConnected);
                
                if (connectedInterface != null && activeInterface?.Guid != connectedInterface.Guid)
                {
                    ColorConsole.WriteInfo($"💡 Found connection on: {connectedInterface.Description}");
                    ColorConsole.WriteInfo($"   Use 'wifimgr interface {interfaces.IndexOf(connectedInterface)}' to switch");
                }
                
                Console.WriteLine();
                return;
            }

            var signalIcon = GetSignalIcon(status.SignalQuality);
            
            ColorConsole.WriteColored("Status:   ", ConsoleColor.Gray);
            ColorConsole.WriteSuccess("✅ Connected");
            
            ColorConsole.WriteColored("Network:  ", ConsoleColor.Gray);
            Console.WriteLine(status.SSID);
            
            ColorConsole.WriteColored("Signal:   ", ConsoleColor.Gray);
            Console.WriteLine($"{signalIcon} {status.SignalQuality}%");
            
            ColorConsole.WriteColored("Security: ", ConsoleColor.Gray);
            Console.WriteLine(status.IsSecure ? "🔒 Secured" : "🔓 Open");
            
            Console.WriteLine();
        }

        public void DeleteProfile(string ssid)
        {
            ColorConsole.WriteInfo($"🗑️  Deleting profile '{ssid}'...");
            
            if (_wifiService.DeleteProfile(ssid))
            {
                ColorConsole.WriteSuccess($"✅ Profile '{ssid}' deleted");
            }
            else
            {
                ColorConsole.WriteError($"❌ Failed to delete profile '{ssid}'");
            }
        }

        public async Task StartMonitoring()
        {
            ColorConsole.WriteHeader("👁️  WiFi Monitoring Mode");
            
            // Show active interface
            var activeInterface = _wifiService.GetActiveInterface();
            if (activeInterface != null)
            {
                var statusIcon = activeInterface.IsConnected ? "🟢" : "⚪";
                ColorConsole.WriteInfo($"Monitoring: {statusIcon} {activeInterface.Description}\n");
                
                // If not connected, suggest switching to connected interface
                if (!activeInterface.IsConnected)
                {
                    var interfaces = _wifiService.GetInterfaces();
                    var connectedInterface = interfaces.FirstOrDefault(i => i.IsConnected);
                    if (connectedInterface != null)
                    {
                        ColorConsole.WriteWarning($"⚠️  Active interface is disconnected");
                        ColorConsole.WriteInfo($"💡 Use 'wifimgr interface {interfaces.IndexOf(connectedInterface)}' to switch to {connectedInterface.Description}");
                        Console.WriteLine();
                    }
                }
            }
            
            ColorConsole.WriteInfo("Press Ctrl+C to stop...");
            Console.WriteLine();

            // Check initial connection status
            var lastStatus = _wifiService.GetConnectionStatus();
            if (lastStatus != null)
            {
                ColorConsole.WriteSuccess($"✅ Currently connected to '{lastStatus.SSID}' ({lastStatus.SignalQuality}%)\n");
                Console.WriteLine();
            }
            else
            {
                ColorConsole.WriteWarning("⚠️  Not connected to any WiFi network\n");
                Console.WriteLine();
            }
            
            // Initialize network signals tracking - scan in background
            ColorConsole.WriteInfo("🔍 Scanning for networks...\n\n");
            var initialNetworks = _wifiService.ScanNetworks();
            
            if (initialNetworks.Count == 0)
            {
                ColorConsole.WriteWarning("⚠️  No networks found. Make sure WiFi is enabled.");
                Console.WriteLine();
            }
            else
            {
                ColorConsole.WriteSuccess($"Found {initialNetworks.Count} network(s):\n");
                foreach (var network in initialNetworks.OrderByDescending(n => n.SignalQuality))
                {
                    var signalIcon = GetSignalIcon(network.SignalQuality);
                    var securityIcon = network.IsSecure ? "🔒" : "🔓";
                    Console.WriteLine($"  {signalIcon} {securityIcon} {network.SSID} ({network.SignalQuality}%)");
                    
                    _networkSignals[network.SSID] = network.SignalQuality;
                    _seenNetworks.Add(network.SSID);
                }
                Console.WriteLine();
            }

            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                Console.WriteLine();
                ColorConsole.WriteWarning("⛔ Stopping monitor...\n");
                Environment.Exit(0);
            };

            int scanCount = 0;
            while (true)
            {
                try
                {
                    scanCount++;
                    
                    var currentStatus = _wifiService.GetConnectionStatus();
                    
                    // Check connection status change
                    if (lastStatus == null && currentStatus != null)
                    {
                        var msg = $"✅ Connected to '{currentStatus.SSID}' ({currentStatus.SignalQuality}%)\n";
                        ColorConsole.WriteSuccess(msg);
                        Console.WriteLine();
                        _notificationService.ShowNotification("WiFi Connected", $"Connected to {currentStatus.SSID}\n");
                    }
                    else if (lastStatus != null && currentStatus == null)
                    {
                        var msg = $"⛔ Disconnected from '{lastStatus.SSID}'\n";
                        ColorConsole.WriteWarning(msg);
                        Console.WriteLine();
                        _notificationService.ShowNotification("WiFi Disconnected", $"Disconnected from {lastStatus.SSID}\n");
                    }
                    else if (lastStatus != null && currentStatus != null && lastStatus.SSID != currentStatus.SSID)
                    {
                        var msg = $"🔄 Switched from '{lastStatus.SSID}' to '{currentStatus.SSID}'\n";
                        ColorConsole.WriteInfo(msg);
                        Console.WriteLine();
                        _notificationService.ShowNotification("WiFi Changed", $"Switched to {currentStatus.SSID}\n");
                    }
                    else if (lastStatus != null && currentStatus != null && lastStatus.SSID == currentStatus.SSID)
                    {
                        // Check for significant signal change (±5% or more)
                        var signalDiff = Math.Abs(currentStatus.SignalQuality - lastStatus.SignalQuality);
                        if (signalDiff >= 5)
                        {
                            var trend = currentStatus.SignalQuality > lastStatus.SignalQuality ? "📈" : "📉";
                            var msg = $"{trend} Signal: {lastStatus.SignalQuality}% → {currentStatus.SignalQuality}% ({currentStatus.SSID})";
                            
                            if (currentStatus.SignalQuality > lastStatus.SignalQuality)
                                ColorConsole.WriteSuccess(msg);
                            else
                                ColorConsole.WriteWarning(msg);
                            
                            Console.WriteLine();
                            
                            // Send notification for significant changes (≥10%)
                            if (signalDiff >= 10)
                            {
                                var trendText = currentStatus.SignalQuality > lastStatus.SignalQuality ? "improved" : "degraded";
                                _notificationService.ShowNotification(
                                    "Signal Changed", 
                                    $"{currentStatus.SSID}: {lastStatus.SignalQuality}% → {currentStatus.SignalQuality}% ({trendText})"
                                );
                            }
                        }
                    }

                    lastStatus = currentStatus;

                    // Scan for networks every 3rd iteration (every 15 seconds)
                    if (scanCount % 3 == 0)
                    {
                        var currentNetworks = _wifiService.ScanNetworks();
                        
                        foreach (var network in currentNetworks)
                        {
                            if (!_seenNetworks.Contains(network.SSID))
                            {
                                // New network detected (first time ever seen)
                                _seenNetworks.Add(network.SSID);
                                _networkSignals[network.SSID] = network.SignalQuality;
                                
                                var msg = $"🆕 New network: '{network.SSID}' ({network.SignalQuality}%)";
                                ColorConsole.WriteColored(msg, ConsoleColor.Cyan);
                                Console.WriteLine();
                                _notificationService.ShowNotification("New WiFi Network", $"{network.SSID} ({network.SignalQuality}%)");
                            }
                            else if (_networkSignals.ContainsKey(network.SSID))
                            {
                                // Check for signal change in known networks
                                var oldSignal = _networkSignals[network.SSID];
                                var signalDiff = Math.Abs(network.SignalQuality - oldSignal);
                                
                                // Only show for background networks if change is ≥15%
                                if (signalDiff >= 15 && (currentStatus == null || currentStatus.SSID != network.SSID))
                                {
                                    var trend = network.SignalQuality > oldSignal ? "📈" : "📉";
                                    var msg = $"{trend} '{network.SSID}': {oldSignal}% → {network.SignalQuality}%";
                                    ColorConsole.WriteColored(msg, ConsoleColor.DarkGray);
                                    Console.WriteLine();
                                }
                                
                                _networkSignals[network.SSID] = network.SignalQuality;
                            }
                        }
                    }

                    await Task.Delay(5000); // Check connection status every 5 seconds
                }
                catch (Exception ex)
                {
                    ColorConsole.WriteError($"❌ Monitor error: {ex.Message}");
                    Console.WriteLine();
                    await Task.Delay(5000);
                }
            }
        }

        private string GetSignalIcon(int quality)
        {
            if (quality >= 75) return "📶";
            if (quality >= 50) return "📶";
            if (quality >= 25) return "📡";
            return "📡";
        }

        private string ReadPassword()
        {
            var password = "";
            ConsoleKeyInfo key;

            do
            {
                key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                {
                    password = password[..^1];
                    Console.Write("\b \b");
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    password += key.KeyChar;
                    Console.Write("*");
                }
            } while (key.Key != ConsoleKey.Enter);

            return password;
        }
    }
}