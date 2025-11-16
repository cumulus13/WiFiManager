using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WiFiManager
{
    public class NotificationConfig
    {
        [JsonPropertyName("enableWindowsToast")]
        public bool EnableWindowsToast { get; set; } = true;

        [JsonPropertyName("enableGrowl")]
        public bool EnableGrowl { get; set; } = true;

        [JsonPropertyName("growlHosts")]
        public List<GrowlHost> GrowlHosts { get; set; } = new()
        {
            new GrowlHost { Host = "127.0.0.1", Port = 23053, Enabled = true }
        };

        [JsonPropertyName("iconPath")]
        public string? IconPath { get; set; }

        [JsonPropertyName("connectionTimeout")]
        public int ConnectionTimeout { get; set; } = 1000;

        [JsonPropertyName("notificationPriorities")]
        public Dictionary<string, int> NotificationPriorities { get; set; } = new()
        {
            { "WiFi Connected", 0 },
            { "WiFi Disconnected", 2 },
            { "WiFi Changed", 0 },
            { "New WiFi Network", 0 },
            { "Signal Changed", -1 }
        };

        [JsonPropertyName("stickyNotifications")]
        public Dictionary<string, bool> StickyNotifications { get; set; } = new()
        {
            { "WiFi Connected", false },
            { "WiFi Disconnected", true },   // Sticky for important notifications
            { "WiFi Changed", false },
            { "New WiFi Network", false },
            { "Signal Changed", false }
        };

        [JsonPropertyName("defaultSticky")]
        public bool DefaultSticky { get; set; } = false;

        // Track loaded config path
        [JsonIgnore]
        public string? LoadedFrom { get; private set; }

        public static NotificationConfig Load(string? path = null)
        {
            List<string> validFiles;

            if (!string.IsNullOrWhiteSpace(path))
            {
                validFiles = new List<string> { path };
            }
            else
            {
                var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var exeDir = AppContext.BaseDirectory;

                validFiles = new List<string>
                {
                    Path.Combine(userProfile, ".wifimgr", "wifimgr.json"),
                    Path.Combine(userProfile, ".wifimgr", "config.json"),
                    Path.Combine(appData, ".wifimgr", "wifimgr.json"),
                    Path.Combine(appData, ".wifimgr", "config.json"),
                    Path.Combine(exeDir, "wifimgr.json"),
                    Path.Combine(exeDir, "config.json"),
                    Path.Combine(exeDir, ".wifimgr", "wifimgr.json"),
                    Path.Combine(exeDir, ".wifimgr", "config.json"),
                    Path.Combine(exeDir, "config", "wifimgr.json"),
                    Path.Combine(exeDir, "config", "config.json")
                };
            }

            string? configPath = null;
            foreach (var file in validFiles)
            {
                Console.WriteLine($"[🔍] Try loading config from: {file}");
                if (File.Exists(file))
                {
                    configPath = file;
                    break;
                }
            }

            if (configPath == null)
            {
                Console.WriteLine($"[⚠️] Config file not found, using default config.");
                var defaultConfig = new NotificationConfig();
                var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                defaultConfig.LoadedFrom = Path.Combine(userProfile, ".wifimgr", "wifimgr.json");
                return defaultConfig;
            }

            try
            {
                string json = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<NotificationConfig>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true
                });

                if (config != null)
                {
                    config.LoadedFrom = configPath;
                    Console.WriteLine($"[✅] Config successfully loaded from: {configPath}");
                    Console.WriteLine($"    - Windows Toast: {(config.EnableWindowsToast ? "ON" : "OFF")}");
                    Console.WriteLine($"    - Growl: {(config.EnableGrowl ? "ON" : "OFF")}");
                    Console.WriteLine($"    - Growl Hosts: {config.GrowlHosts.Count}");
                    return config;
                }

                Console.WriteLine($"[⚠️] Failed to parse config, using default config.");
                var fallbackConfig = new NotificationConfig();
                var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                fallbackConfig.LoadedFrom = Path.Combine(userProfile, ".wifimgr", "wifimgr.json");
                return fallbackConfig;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[❌] Failed to load config: {ex.Message}");
                var errorConfig = new NotificationConfig();
                var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                errorConfig.LoadedFrom = Path.Combine(userProfile, ".wifimgr", "wifimgr.json");
                return errorConfig;
            }
        }

        public void Save(string? path = null)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                if (!string.IsNullOrWhiteSpace(LoadedFrom))
                {
                    path = LoadedFrom;
                }
                else
                {
                    var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    var wifimgrDir = Path.Combine(userProfile, ".wifimgr");

                    if (!Directory.Exists(wifimgrDir))
                    {
                        Directory.CreateDirectory(wifimgrDir);
                    }

                    path = Path.Combine(wifimgrDir, "wifimgr.json");
                }
            }

            try
            {
                var directory = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                File.WriteAllText(path, json);
                LoadedFrom = path;
                Console.WriteLine($"[✅] Config saved to: {path}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[❌] Failed to save config: {ex.Message}");
            }
        }
    }

    public class GrowlHost
    {
        [JsonPropertyName("host")]
        public string Host { get; set; } = "127.0.0.1";

        [JsonPropertyName("port")]
        public int Port { get; set; } = 23053;

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        public override string ToString()
        {
            var name = !string.IsNullOrEmpty(Name) ? $" ({Name})" : "";
            return $"{Host}:{Port}{name}";
        }
    }
}