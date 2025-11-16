using System;
using System.Threading;
using System.Threading.Tasks;

namespace WiFiManager
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            
            var manager = new WiFiManagerCLI();
            
            if (args.Length > 0)
            {
                switch (args[0].ToLower())
                {
                    case "scan":
                        await manager.ScanNetworks();
                        break;
                    case "interfaces":
                    case "iface":
                        manager.ListInterfaces();
                        break;
                    case "interface":
                        if (args.Length > 1 && int.TryParse(args[1], out int ifaceIndex))
                            manager.SwitchInterface(ifaceIndex);
                        else
                            ColorConsole.WriteError("❌ Usage: wifimgr interface <index>");
                        break;
                    case "list":
                        manager.ListProfiles();
                        break;
                    case "connect":
                        if (args.Length > 1)
                            await manager.Connect(args[1]);
                        else
                            ColorConsole.WriteError("❌ Usage: wifimgr connect <SSID>");
                        break;
                    case "disconnect":
                        manager.Disconnect();
                        break;
                    case "status":
                        manager.ShowStatus();
                        break;
                    case "monitor":
                        await manager.StartMonitoring();
                        break;
                    case "test-notification":
                    case "test-notif":
                        manager.TestNotification();
                        break;
                    case "delete":
                        if (args.Length > 1)
                            manager.DeleteProfile(args[1]);
                        else
                            ColorConsole.WriteError("❌ Usage: wifimgr delete <SSID>");
                        break;
                    case "help":
                    case "--help":
                    case "-h":
                        ShowHelp();
                        break;
                    default:
                        ColorConsole.WriteError($"❌ Unknown command: {args[0]}");
                        ShowHelp();
                        break;
                }
            }
            else
            {
                ShowHelp();
            }
        }

        static void ShowHelp()
        {
            ColorConsole.WriteHeader("📡 WiFi Manager CLI");
            Console.WriteLine();
            ColorConsole.WriteInfo("Usage: wifimgr <command> [options]");
            Console.WriteLine();
            ColorConsole.WriteSuccess("Commands:");
            Console.WriteLine("  interfaces    - 📡 List all WiFi interfaces");
            Console.WriteLine("  interface <N> - 🔄 Switch to interface N");
            Console.WriteLine("  scan          - 🔍 Scan for available networks");
            Console.WriteLine("  list          - 📋 List saved profiles");
            Console.WriteLine("  connect <SSID> - 🔌 Connect to a network");
            Console.WriteLine("  disconnect    - ⛔ Disconnect from current network");
            Console.WriteLine("  status        - 📊 Show connection status");
            Console.WriteLine("  monitor       - 👁️  Monitor WiFi (with notifications)");
            Console.WriteLine("  test-notif    - 🔔 Test notifications (Toast & Growl)");
            Console.WriteLine("  delete <SSID> - 🗑️  Delete saved profile");
            Console.WriteLine("  help          - ❓ Show this help");
            Console.WriteLine();
        }
    }
}