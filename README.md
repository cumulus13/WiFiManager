# 📡 WiFi Manager CLI (wifimgr)

A powerful command-line WiFi manager for Windows with colorful UI, emoji support, and multi-platform notifications (Windows Toast & Growl).

![Version](https://img.shields.io/badge/version-1.0.0-blue.svg)
![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)
![Platform](https://img.shields.io/badge/platform-Windows-blue.svg)
![License](https://img.shields.io/badge/license-MIT-green.svg)

## ✨ Features

- 🔍 **Network Scanning** - Scan available WiFi networks with signal strength
- 🔌 **Smart Connect** - Auto-save credentials and quick reconnect
- 📋 **Profile Management** - List, create, and delete WiFi profiles
- ⛔ **Quick Disconnect** - Disconnect from current network
- 📊 **Status Monitor** - Real-time connection status with signal quality
- 👁️ **Live Monitoring** - Monitor WiFi with change notifications
- 🆕 **New AP Detection** - Automatic notification for new networks
- 🔔 **Dual Notifications** - Windows Toast & Growl support
- 📶 **Signal Tracking** - Track signal strength changes
- 🖥️ **Multi-Interface** - Support multiple WiFi adapters
- 🎨 **Colorful UI** - Rich console colors with emoji icons

## 📋 Table of Contents

- [Installation](#-installation)
- [Configuration](#-configuration)
- [Usage](#-usage)
- [Commands](#-commands)
- [Notifications](#-notifications)
- [Troubleshooting](#-troubleshooting)
- [Development](#-development)
- [Contributing](#-contributing)
- [License](#-license)
- [Support](#-support)

## 🚀 Installation

### Prerequisites

- **Windows 10/11** (64-bit)
- **.NET 8.0 Runtime** or SDK
- **WiFi Adapter** enabled
- *Optional*: Growl for Windows (for Growl notifications)

### Download

**Option 1: Download Pre-built Binary**
```bash
# Download from releases page
https://github.com/cumulus13/wifimanager/releases

# Extract and run
wifimgr.exe --help
```

**Option 2: Build from Source**
```bash
# Clone repository
git clone https://github.com/cumulus13/wifimanager.git
cd wifimanager

# Build
dotnet build -c Release

# Or publish as single executable
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true

# Executable will be in:
# bin\Release\net8.0\win-x64\publish\wifimgr.exe
```

### Add to PATH (Recommended)

**Windows 10/11:**
1. Copy `wifimgr.exe` to `C:\Tools\` (or any folder)
2. Press `Win + X` → System → Advanced system settings
3. Environment Variables → System variables → Path → Edit → New
4. Add `C:\Tools\`
5. Click OK → OK → OK
6. Restart terminal
7. Run `wifimgr` from anywhere!

**PowerShell Quick Setup:**
```powershell
# Create tools directory
New-Item -ItemType Directory -Force -Path "C:\Tools"

# Copy executable
Copy-Item "wifimgr.exe" -Destination "C:\Tools\"

# Add to PATH (requires admin)
[Environment]::SetEnvironmentVariable("Path", $env:Path + ";C:\Tools", [System.EnvironmentVariableTarget]::Machine)
```

## ⚙️ Configuration

### Config File Locations

wifimgr searches for `wifimgr.json` or `config.json` in these locations (priority order):

1. `%USERPROFILE%\.wifimgr\wifimgr.json`
2. `%USERPROFILE%\.wifimgr\config.json`
3. `%APPDATA%\.wifimgr\wifimgr.json`
4. `%APPDATA%\.wifimgr\config.json`
5. `{ExeDirectory}\wifimgr.json`
6. `{ExeDirectory}\config.json`
7. `{ExeDirectory}\.wifimgr\wifimgr.json`
8. `{ExeDirectory}\config\wifimgr.json`

### Configuration Format

**Full Config Example (`wifimgr.json`):**
```json
{
  "enableWindowsToast": true,
  "enableGrowl": true,
  "growlHosts": [
    {
      "host": "127.0.0.1",
      "port": 23053,
      "enabled": true,
      "name": "Local Growl"
    },
    {
      "host": "192.168.1.100",
      "port": 23053,
      "enabled": true,
      "name": "Remote PC"
    },
    {
      "host": "laptop.local",
      "port": 23053,
      "enabled": false,
      "name": "Laptop (Disabled)"
    }
  ],
  "iconPath": "C:\\path\\to\\custom\\icon.png",
  "connectionTimeout": 1000,
  "notificationPriorities": {
    "WiFi Connected": 0,
    "WiFi Disconnected": 2,
    "WiFi Changed": 0,
    "New WiFi Network": 0,
    "Signal Changed": -1
  },
  "stickyNotifications": {
    "WiFi Connected": false,
    "WiFi Disconnected": true,
    "WiFi Changed": false,
    "New WiFi Network": true,
    "Signal Changed": false
  },
  "defaultSticky": false
}
```

### Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `enableWindowsToast` | boolean | `true` | Enable Windows Toast notifications |
| `enableGrowl` | boolean | `true` | Enable Growl notifications |
| `growlHosts` | array | `[{"host":"127.0.0.1","port":23053}]` | List of Growl hosts |
| `iconPath` | string | `null` | Custom icon path for notifications |
| `connectionTimeout` | number | `1000` | Growl connection timeout (ms) |
| `notificationPriorities` | object | see example | Priority per notification type (-2 to 2) |
| `stickyNotifications` | object | see example | Sticky setting per notification type |
| `defaultSticky` | boolean | `false` | Default sticky if type not configured |

### Icon Setup

Place icon file in one of these locations (auto-detected):
- `{ExeDirectory}\icon.png`
- `{ExeDirectory}\wifi.png`
- `{ExeDirectory}\wifimgr.png`
- `{ExeDirectory}\app.ico`
- `{ExeDirectory}\assets\icon.png`
- `{ExeDirectory}\resources\icon.png`

Or specify custom path in config:
```json
{
  "iconPath": "C:\\MyIcons\\custom.png"
}
```

**Supported formats:** PNG, JPG, ICO, GIF, BMP (PNG recommended, max 100KB)

## 📖 Usage

### Basic Commands

```bash
# Show help
wifimgr help

# List WiFi interfaces
wifimgr interfaces

# Switch to interface
wifimgr interface 0

# Scan networks
wifimgr scan

# Connect to network
wifimgr connect MyWiFi

# Disconnect
wifimgr disconnect

# Show status
wifimgr status

# List profiles
wifimgr list

# Delete profile
wifimgr delete OldNetwork

# Monitor mode
wifimgr monitor

# Test notifications
wifimgr test-notif
```

## 📚 Commands

### `interfaces` - List WiFi Interfaces

List all available WiFi adapters with their status.

```bash
wifimgr interfaces
```

**Output:**
```
📡 WiFi Interfaces

Found 2 interface(s):

✅ [0] 🟢 Intel(R) Wi-Fi 6 AX201 160MHz (connected)
   [1] ⚪ Realtek RTL8822CE 802.11ac (disconnected)

💡 Use 'wifimgr interface <index>' to switch
```

**Icons:**
- ✅ = Active interface
- 🟢 = Connected
- ⚪ = Disconnected

---

### `interface <N>` - Switch Interface

Switch to a specific WiFi interface.

```bash
wifimgr interface 1
```

**Output:**
```
✅ Switched to interface: Realtek RTL8822CE 802.11ac
```

---

### `scan` - Scan Networks

Scan for available WiFi networks with signal strength.

```bash
wifimgr scan
```

**Output:**
```
🔍 Scanning on: Intel(R) Wi-Fi 6 AX201 160MHz

Found 8 network(s):

✅ 📶 🔒 MyHomeWiFi (95%)
    📶 🔒 NeighborWiFi (82%)
    📶 🔓 FreeWiFi (75%)
    📶 🔒 CoffeeShop (68%)
    📡 🔒 Mobile_AP (45%)
    📡 🔒 OfficeGuest (38%)
    📡 🔒 Hotel_WiFi (25%)
    📡 🔓 PublicWiFi (18%)
```

**Icons:**
- ✅ = Currently connected
- 📶 = Strong signal (≥50%)
- 📡 = Weak signal (<50%)
- 🔒 = Secured (password required)
- 🔓 = Open (no password)

---

### `connect <SSID>` - Connect to Network

Connect to a WiFi network. If profile doesn't exist, you'll be prompted for password.

```bash
# Connect to saved network
wifimgr connect MyHomeWiFi

# Connect to new secured network
wifimgr connect NewNetwork
# 🔑 Enter password: ********
# 💾 Creating profile...
# ✅ Profile created
# ✅ Connected to 'NewNetwork'

# Connect to open network
wifimgr connect FreeWiFi
# ✅ Connected to 'FreeWiFi'
```

---

### `disconnect` - Disconnect

Disconnect from current WiFi network.

```bash
wifimgr disconnect
```

**Output:**
```
⛔ Disconnecting...
✅ Disconnected
```

---

### `status` - Connection Status

Show current WiFi connection status with details.

```bash
wifimgr status
```

**Output:**
```
📊 WiFi Status

Interface: Intel(R) Wi-Fi 6 AX201 160MHz (connected)
Status:    ✅ Connected
Network:   MyHomeWiFi
Signal:    📶 95%
Security:  🔒 Secured
```

**When disconnected:**
```
📊 WiFi Status

Interface: Intel(R) Wi-Fi 6 AX201 160MHz (disconnected)
⚠️  Not connected on active interface

💡 Found connection on: Realtek RTL8822CE 802.11ac
   Use 'wifimgr interface 1' to switch
```

---

### `list` - List Profiles

List all saved WiFi profiles.

```bash
wifimgr list
```

**Output:**
```
📋 Saved WiFi Profiles

Found 5 profile(s):

  🔹 MyHomeWiFi
  🔹 OfficeWiFi
  🔹 CafeWiFi
  🔹 AirportWiFi
  🔹 HotelGuest
```

---

### `delete <SSID>` - Delete Profile

Delete a saved WiFi profile.

```bash
wifimgr delete OldNetwork
```

**Output:**
```
🗑️  Deleting profile 'OldNetwork'...
✅ Profile 'OldNetwork' deleted
```

---

### `monitor` - Monitor Mode

Start real-time WiFi monitoring with notifications.

```bash
wifimgr monitor
```

**Features:**
- ✅ Connection/disconnection detection
- 🆕 New network discovery
- 📈📉 Signal strength changes
- 🔔 Desktop notifications
- 🔄 Network switching detection

**Output:**
```
👁️  WiFi Monitoring Mode
Monitoring: 🟢 Intel(R) Wi-Fi 6 AX201 160MHz

Press Ctrl+C to stop...

✅ Currently connected to 'MyHomeWiFi' (95%)

🔍 Scanning for networks...

Found 8 network(s):
  📶 🔒 MyHomeWiFi (95%)
  📶 🔒 NeighborWiFi (82%)
  📶 🔓 FreeWiFi (75%)
  ...

[Events will appear here in real-time]

📉 Signal: 95% → 88% (MyHomeWiFi)
🆕 New network: 'MobileHotspot' (67%)
🔄 Switched from 'MyHomeWiFi' to 'OfficeWiFi'
⛔ Disconnected from 'OfficeWiFi'
✅ Connected to 'MyHomeWiFi' (92%)
```

**Press Ctrl+C to stop monitoring**

---

### `test-notif` - Test Notifications

Test notification systems (Windows Toast & Growl).

```bash
wifimgr test-notif
```

**Output:**
```
🔔 Testing Notifications

Testing Windows Toast...
Testing Growl (2 host(s))...
✅ Growl test sent to 127.0.0.1:23053 (Local Growl)
✅ Growl test sent to 192.168.1.100:23053 (Remote PC)

✅ Notification test completed

📁 Icon: C:\Tools\wifimgr.png
```

**If Growl is not available:**
```
⚠️  Growl is not available
💡 To enable Growl:
   1. Install Growl for Windows
   2. Make sure it's running
   3. Enable GNTP in Growl settings (default port 23053)
   4. Configure in wifimgr.json
```

---

## 🔔 Notifications

### Windows Toast Notifications

**Automatic** - Shows for:
- WiFi Connected
- WiFi Disconnected
- WiFi Network Changed
- New WiFi Network Detected
- Signal Strength Changes (≥10%)

**Requirements:**
- Windows 10/11
- Notifications enabled in Settings → System → Notifications

**Features:**
- Native Windows 10/11 style
- Action Center integration
- Custom icon support
- Auto-dismiss or sticky

### Growl Notifications

**Multiple Hosts Support** - Send to:
- Local Growl (127.0.0.1)
- Remote PCs over network
- Multiple Growl instances

**Configuration:**
```json
{
  "growlHosts": [
    {
      "host": "127.0.0.1",
      "port": 23053,
      "enabled": true,
      "name": "Local"
    },
    {
      "host": "192.168.1.100",
      "port": 23053,
      "enabled": true,
      "name": "Desktop PC"
    }
  ]
}
```

**Priority Levels:**
- `-2` = Very Low
- `-1` = Low
- `0` = Normal
- `1` = High
- `2` = Emergency

**Sticky Notifications:**
```json
{
  "stickyNotifications": {
    "WiFi Disconnected": true,    // Stays until dismissed
    "WiFi Connected": false,       // Auto-dismiss
    "New WiFi Network": true       // Stays until dismissed
  }
}
```

## 🐛 Troubleshooting

### Common Issues

#### 1. "Failed to open WLAN handle"

**Cause:** WiFi adapter not detected or disabled

**Solutions:**
- ✅ Enable WiFi adapter in Device Manager
- ✅ Run as Administrator
- ✅ Restart Windows WLAN service:
  ```powershell
  net stop wlansvc
  net start wlansvc
  ```
- ✅ Check if WiFi drivers are installed

#### 2. Notifications Not Appearing

**Windows Toast:**
- ✅ Check Settings → System → Notifications → Enable notifications
- ✅ Enable notifications for "WiFiManager" app
- ✅ Disable Focus Assist / Do Not Disturb
- ✅ Run as Administrator

**Growl:**
- ✅ Install Growl for Windows
- ✅ Make sure Growl is running (check system tray)
- ✅ Enable GNTP in Growl Settings
- ✅ Set port to 23053 (default)
- ✅ Check firewall (allow port 23053)
- ✅ For remote: Enable network listener in Growl

#### 3. Scan Returns No Networks

**Solutions:**
- ✅ Disable Airplane Mode
- ✅ Wait 5-10 seconds after enabling WiFi
- ✅ Try `wifimgr scan` multiple times
- ✅ Move closer to access point
- ✅ Check if other devices can see networks
- ✅ Restart WiFi adapter:
  ```bash
  netsh interface set interface "Wi-Fi" disabled
  netsh interface set interface "Wi-Fi" enabled
  ```

#### 4. "Failed to connect"

**Solutions:**
- ✅ Check password is correct
- ✅ Delete old profile: `wifimgr delete NetworkName`
- ✅ Try connecting again
- ✅ Check network security type (WPA2/WPA3)
- ✅ Network might be hidden (try SSID exactly as shown)
- ✅ Router might have MAC filtering enabled

#### 5. Icon Not Showing in Notifications

**Solutions:**
- ✅ Place icon file in exe directory
- ✅ Use PNG format (recommended)
- ✅ Keep file size < 100KB
- ✅ Check icon path in config
- ✅ Verify file permissions

#### 6. Multiple Interfaces Confusion

**Solutions:**
- ✅ Use `wifimgr interfaces` to see all adapters
- ✅ Switch to connected interface: `wifimgr interface 0`
- ✅ The active interface is marked with ✅

### Debug Tips

**Enable Verbose Logging:**
```bash
# Set environment variable
set WIFIMGR_DEBUG=1
wifimgr monitor
```

**Check Windows Event Viewer:**
```
eventvwr.msc → Windows Logs → System
Filter by: WLAN-AutoConfig
```

**Test WiFi Adapter:**
```bash
# PowerShell
Get-NetAdapter | Where-Object {$_.InterfaceDescription -like "*Wi-Fi*"}
```

## 🔧 Development

### Project Structure

```
WiFiManager/
├── Program.cs                 # Entry point & CLI routing
├── WiFiManagerCLI.cs          # CLI commands implementation
├── WiFiService.cs             # Native WiFi API wrapper
├── NotificationService.cs     # Toast & Growl notifications
├── NotificationConfig.cs      # Configuration management
├── ColorConsole.cs            # Console color utilities
├── WiFiManager.csproj         # Project configuration
└── README.md                  # Documentation
```

### Building

**Debug Build:**
```bash
dotnet build
```

**Release Build:**
```bash
dotnet build -c Release
```

**Single File Executable:**
```bash
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

**Self-Contained (no .NET required):**
```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

### Technology Stack

- **.NET 8.0** - Modern .NET runtime
- **C# 12** - Latest C# features
- **Native WiFi API** - Windows wlanapi.dll
- **P/Invoke** - Interop with Windows APIs
- **Windows.UI.Notifications** - Toast notifications
- **GNTP Protocol** - Growl notifications
- **TCP Sockets** - Network communication

### Code Architecture

**WiFiService.cs** - Low-level WiFi operations
- P/Invoke declarations
- WLAN API wrappers
- Interface management
- Network scanning
- Profile CRUD operations

**NotificationService.cs** - Notification handling
- Windows Toast via PowerShell
- Growl GNTP protocol
- Multiple host support
- Icon embedding (base64 & file path)

**NotificationConfig.cs** - Configuration
- JSON deserialization
- Multi-location search
- Priority & sticky settings
- Auto-save location tracking

## 🤝 Contributing

Contributions are welcome! Here's how:

1. **Fork** the repository
2. **Create** a feature branch: `git checkout -b feature/amazing-feature`
3. **Commit** changes: `git commit -m 'Add amazing feature'`
4. **Push** to branch: `git push origin feature/amazing-feature`
5. **Open** a Pull Request

### Contribution Guidelines

- ✅ Follow existing code style
- ✅ Add comments for complex logic
- ✅ Test on Windows 10 & 11
- ✅ Update README if needed
- ✅ Keep commits clean and descriptive

### Reporting Bugs

**Create an issue with:**
- Windows version
- .NET version
- WiFi adapter model
- Steps to reproduce
- Error messages / screenshots
- Expected vs actual behavior

## 📄 License

MIT License

Copyright (c) 2024 Hadi Cahyadi

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.

## 💖 Support

If you find this project helpful, consider supporting:

[![Buy Me a Coffee](https://img.shields.io/badge/Buy%20Me%20a%20Coffee-donate-yellow.svg)](https://www.buymeacoffee.com/cumulus13)
[![Ko-fi](https://img.shields.io/badge/Ko--fi-donate-ff5e5b.svg)](https://ko-fi.com/cumulus13)
[![Patreon](https://img.shields.io/badge/Patreon-support-red.svg)](https://www.patreon.com/cumulus13)

## 📞 Contact

**Hadi Cahyadi**

- 📧 Email: [cumulus13@gmail.com](mailto:cumulus13@gmail.com)
- 🐙 GitHub: [@cumulus13](https://github.com/cumulus13)
- 🐛 Issues: [GitHub Issues](https://github.com/cumulus13/wifimanager/issues)
- 💬 Discussions: [GitHub Discussions](https://github.com/cumulus13/wifimanager/discussions)

---

**Made with ❤️ and ☕ by Hadi Cahyadi**

*Star ⭐ this repo if you find it useful!*