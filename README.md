# 📡 WiFi Manager CLI

WiFi Manager CLI (`wifimgr`) adalah aplikasi command-line untuk mengelola koneksi WiFi di Windows dengan tampilan berwarna dan dukungan emoji.

## ✨ Fitur

- 🔍 **Scan** jaringan WiFi yang tersedia
- 🔌 **Connect** ke jaringan dengan password otomatis disimpan
- 📋 **List** profile WiFi yang tersimpan
- ⛔ **Disconnect** dari jaringan aktif
- 📊 **Status** koneksi saat ini
- 👁️ **Monitor** mode dengan notifikasi real-time
- 🗑️ **Delete** profile yang tidak digunakan
- 🔔 **Notifikasi** Windows Toast & Growl support
- 🆕 **Deteksi** AP baru secara otomatis

## 🚀 Instalasi

### Prasyarat
- Windows 10/11
- .NET 8.0 SDK

### Build & Run

```bash
# Clone atau copy semua file ke folder project
# Struktur folder:
# WiFiManager/
# ├── Program.cs
# ├── WiFiManagerCLI.cs
# ├── WiFiService.cs
# ├── ColorConsole.cs
# ├── NotificationService.cs
# └── WiFiManager.csproj

# Build project
dotnet build -c Release

# Atau publish sebagai executable tunggal
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true

# Executable akan ada di:
# bin/Release/net8.0/win-x64/publish/wifimgr.exe
```

### Menambahkan ke PATH (Opsional)

Untuk menjalankan `wifimgr` dari mana saja:

1. Copy `wifimgr.exe` ke folder, misalnya `C:\Tools\`
2. Tambahkan folder tersebut ke System PATH:
   - Tekan `Win + X` → System → Advanced system settings
   - Environment Variables → System variables → Path → Edit
   - New → Masukkan `C:\Tools\`
   - OK → OK → OK
3. Restart terminal
4. Sekarang bisa jalankan `wifimgr` dari mana saja!

## 📖 Cara Penggunaan

### Scan Jaringan
```bash
wifimgr scan
```
Output:
```
🔍 Scanning for WiFi networks...

Found 5 network(s):

✅ 📶 🔒 MyHomeWiFi (95%)
    📶 🔒 NeighborWiFi (78%)
    📶 🔓 FreeWiFi (65%)
    📡 🔒 CoffeeShop (45%)
    📡 🔒 Mobile_AP (32%)
```

### Connect ke Jaringan
```bash
# Jika profile sudah ada
wifimgr connect MyHomeWiFi

# Jika belum ada, akan diminta password
wifimgr connect NewNetwork
# 🔑 Enter password: ********
# 💾 Creating profile...
# ✅ Profile created
# ✅ Connected to 'NewNetwork'
```

### Lihat Status Koneksi
```bash
wifimgr status
```
Output:
```
📊 WiFi Status

Status:   ✅ Connected
Network:  MyHomeWiFi
Signal:   📶 95%
Security: 🔒 Secured
```

### List Profile Tersimpan
```bash
wifimgr list
```
Output:
```
📋 Saved WiFi Profiles

Found 3 profile(s):

  🔹 MyHomeWiFi
  🔹 OfficeWiFi
  🔹 CafeWiFi
```

### Monitor Mode
```bash
wifimgr monitor
```
Fitur monitor:
- ✅ Deteksi koneksi/diskoneksi otomatis
- 🆕 Notifikasi jika ada AP baru
- 🔔 Windows Toast Notification
- 📢 Growl notification (jika terinstal)
- Tekan `Ctrl+C` untuk stop

Output:
```
👁️  WiFi Monitoring Mode
Press Ctrl+C to stop...

✅ Connected to 'MyHomeWiFi'
🆕 New network detected: 'GuestWiFi' (67%)
⛔ Disconnected from WiFi
✅ Connected to 'OfficeWiFi'
```

### Disconnect
```bash
wifimgr disconnect
```

### Hapus Profile
```bash
wifimgr delete OldNetwork
```

### Help
```bash
wifimgr help
```

## 🔧 Teknologi

- **Native WiFi API** (wlanapi.dll) untuk kontrol WiFi
- **P/Invoke** untuk interop dengan Windows API
- **Windows Toast Notifications** untuk notifikasi modern
- **Growl Protocol (GNTP)** untuk notifikasi ke Growl
- **Console Colors & Emoji** untuk UI yang menarik

## ⚠️ Catatan Penting

1. **Administrator Rights**: Beberapa operasi mungkin memerlukan hak administrator
2. **Windows Only**: Aplikasi ini hanya untuk Windows (menggunakan wlanapi.dll)
3. **WiFi Adapter**: Pastikan WiFi adapter terdeteksi dan aktif
4. **Security**: Password disimpan dalam Windows Credential Manager dengan enkripsi

## 🐛 Troubleshooting

### "Failed to open WLAN handle"
- Pastikan WiFi adapter aktif
- Jalankan sebagai Administrator

### Notifikasi tidak muncul
- Windows 10/11: Periksa Settings → System → Notifications
- Growl: Pastikan Growl terinstal dan berjalan

### Scan tidak menemukan jaringan
- Pastikan WiFi tidak dalam Airplane Mode
- Coba disable/enable WiFi adapter
- Tunggu beberapa detik setelah enable WiFi

## 📝 Lisensi

MIT License - Feel free to use and modify!

## 🤝 Kontribusi

Pull requests welcome! Untuk perubahan besar, buka issue terlebih dahulu.

## 📞 Support

Jika ada masalah atau pertanyaan, silakan buat issue di repository.

---

Made with ❤️ using .NET 8.0