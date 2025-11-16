using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace WiFiManager
{
    public enum WLAN_INTERFACE_STATE
    {
        wlan_interface_state_not_ready = 0,
        wlan_interface_state_connected = 1,
        wlan_interface_state_ad_hoc_network_formed = 2,
        wlan_interface_state_disconnecting = 3,
        wlan_interface_state_disconnected = 4,
        wlan_interface_state_associating = 5,
        wlan_interface_state_discovering = 6,
        wlan_interface_state_authenticating = 7
    }

    public class WiFiInterface
    {
        public Guid Guid { get; set; }
        public string Name { get; set; } = "";
        public string Description { get; set; } = "";
        public WLAN_INTERFACE_STATE State { get; set; }
        public bool IsConnected => State == WLAN_INTERFACE_STATE.wlan_interface_state_connected;
    }

    public class WiFiNetwork
    {
        public string SSID { get; set; } = "";
        public int SignalQuality { get; set; }
        public bool IsSecure { get; set; }
        public bool IsConnected { get; set; }
    }

    public class WiFiService
    {
        private IntPtr _clientHandle = IntPtr.Zero;
        private Guid _interfaceGuid;
        private List<WiFiInterface> _interfaces = new();

        public WiFiService()
        {
            Initialize();
        }

        private void Initialize()
        {
            uint negotiatedVersion;
            var result = WlanOpenHandle(2, IntPtr.Zero, out negotiatedVersion, out _clientHandle);
            
            if (result != 0)
            {
                throw new Exception($"Failed to open WLAN handle. Error: {result}");
            }

            RefreshInterfaces();
            
            if (_interfaces.Count > 0)
            {
                // Default to first connected interface, or just first interface
                var connectedInterface = _interfaces.FirstOrDefault(i => i.IsConnected);
                _interfaceGuid = connectedInterface?.Guid ?? _interfaces[0].Guid;
            }
        }

        private void RefreshInterfaces()
        {
            _interfaces.Clear();
            
            try
            {
                IntPtr interfaceListPtr;
                var result = WlanEnumInterfaces(_clientHandle, IntPtr.Zero, out interfaceListPtr);
                
                if (result != 0) return;

                var interfaceList = Marshal.PtrToStructure<WLAN_INTERFACE_INFO_LIST>(interfaceListPtr);
                var currentPtr = new IntPtr(interfaceListPtr.ToInt64() + 
                    Marshal.OffsetOf<WLAN_INTERFACE_INFO_LIST>("InterfaceInfo").ToInt64());

                for (int i = 0; i < interfaceList.dwNumberOfItems; i++)
                {
                    try
                    {
                        var interfaceInfo = Marshal.PtrToStructure<WLAN_INTERFACE_INFO>(currentPtr);
                        
                        _interfaces.Add(new WiFiInterface
                        {
                            Guid = interfaceInfo.InterfaceGuid,
                            Name = $"WiFi {i + 1}",
                            Description = interfaceInfo.strInterfaceDescription ?? "Unknown",
                            State = interfaceInfo.isState
                        });

                        currentPtr = new IntPtr(currentPtr.ToInt64() + Marshal.SizeOf<WLAN_INTERFACE_INFO>());
                    }
                    catch
                    {
                        // Skip malformed interface
                    }
                }

                WlanFreeMemory(interfaceListPtr);
            }
            catch
            {
                // Failed to enumerate interfaces
            }
        }

        public List<WiFiInterface> GetInterfaces()
        {
            RefreshInterfaces();
            return _interfaces;
        }

        public bool SetActiveInterface(int index)
        {
            if (index < 0 || index >= _interfaces.Count)
                return false;
            
            _interfaceGuid = _interfaces[index].Guid;
            return true;
        }

        public bool SetActiveInterface(Guid guid)
        {
            var iface = _interfaces.FirstOrDefault(i => i.Guid == guid);
            if (iface == null)
                return false;
            
            _interfaceGuid = guid;
            return true;
        }

        public WiFiInterface? GetActiveInterface()
        {
            // Refresh to get latest state
            RefreshInterfaces();
            return _interfaces.FirstOrDefault(i => i.Guid == _interfaceGuid);
        }

        public List<WiFiNetwork> ScanNetworks()
        {
            var networks = new List<WiFiNetwork>();

            try
            {
                // Trigger scan
                var scanResult = WlanScan(_clientHandle, ref _interfaceGuid, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
                if (scanResult != 0)
                {
                    return networks; // Scan failed, return empty
                }

                System.Threading.Thread.Sleep(2000); // Wait for scan to complete

                IntPtr networkListPtr;
                var result = WlanGetAvailableNetworkList(_clientHandle, ref _interfaceGuid, 0, IntPtr.Zero, out networkListPtr);
                
                if (result != 0 || networkListPtr == IntPtr.Zero)
                {
                    return networks;
                }

                try
                {
                    var networkList = Marshal.PtrToStructure<WLAN_AVAILABLE_NETWORK_LIST>(networkListPtr);
                    
                    if (networkList.dwNumberOfItems == 0)
                    {
                        WlanFreeMemory(networkListPtr);
                        return networks;
                    }

                    var currentPtr = new IntPtr(networkListPtr.ToInt64() + 
                        Marshal.OffsetOf<WLAN_AVAILABLE_NETWORK_LIST>("Network").ToInt64());

                    for (int i = 0; i < networkList.dwNumberOfItems; i++)
                    {
                        try
                        {
                            var network = Marshal.PtrToStructure<WLAN_AVAILABLE_NETWORK>(currentPtr);
                            
                            // Critical: Validate SSID length BEFORE any operation
                            if (network.dot11Ssid.SSIDLength > 0 && network.dot11Ssid.SSIDLength <= 32)
                            {
                                try
                                {
                                    // Safe SSID extraction
                                    var ssidBytes = new byte[network.dot11Ssid.SSIDLength];
                                    Buffer.BlockCopy(network.dot11Ssid.SSID, 0, ssidBytes, 0, (int)network.dot11Ssid.SSIDLength);
                                    
                                    var ssid = Encoding.UTF8.GetString(ssidBytes).Trim('\0').Trim();
                                    
                                    if (!string.IsNullOrWhiteSpace(ssid))
                                    {
                                        // Check for duplicates
                                        if (!networks.Any(n => n.SSID.Equals(ssid, StringComparison.OrdinalIgnoreCase)))
                                        {
                                            networks.Add(new WiFiNetwork
                                            {
                                                SSID = ssid,
                                                SignalQuality = Math.Min(100, Math.Max(0, (int)network.wlanSignalQuality)),
                                                IsSecure = network.bSecurityEnabled,
                                                IsConnected = (network.dwFlags & 1) != 0
                                            });
                                        }
                                    }
                                }
                                catch
                                {
                                    // Failed to parse this SSID, skip it
                                }
                            }

                            currentPtr = new IntPtr(currentPtr.ToInt64() + Marshal.SizeOf<WLAN_AVAILABLE_NETWORK>());
                        }
                        catch
                        {
                            // Skip malformed network entry
                            try
                            {
                                currentPtr = new IntPtr(currentPtr.ToInt64() + Marshal.SizeOf<WLAN_AVAILABLE_NETWORK>());
                            }
                            catch
                            {
                                break; // Can't continue
                            }
                        }
                    }

                    WlanFreeMemory(networkListPtr);
                }
                catch
                {
                    if (networkListPtr != IntPtr.Zero)
                    {
                        try { WlanFreeMemory(networkListPtr); } catch { }
                    }
                }
            }
            catch
            {
                // Return empty list on any error
            }

            return networks;
        }

        public WiFiNetwork? GetConnectionStatus()
        {
            try
            {
                IntPtr dataPtr = IntPtr.Zero;
                uint dataSize = 0;
                IntPtr opcodeValueType = IntPtr.Zero;
                
                var result = WlanQueryInterface(_clientHandle, ref _interfaceGuid, 
                    WLAN_INTF_OPCODE.wlan_intf_opcode_current_connection, 
                    IntPtr.Zero, out dataSize, out dataPtr, out opcodeValueType);
                
                // Error codes:
                // 0 = success
                // 1 = invalid parameter
                // 1168 (0x490) = Element not found (not connected)
                if (result != 0)
                {
                    return null;
                }

                if (dataPtr == IntPtr.Zero || dataSize == 0)
                {
                    return null;
                }

                try
                {
                    var connection = Marshal.PtrToStructure<WLAN_CONNECTION_ATTRIBUTES>(dataPtr);
                    
                    if (connection.isState != WLAN_INTERFACE_STATE.wlan_interface_state_connected)
                    {
                        return null;
                    }

                    // Validate SSID length
                    if (connection.wlanAssociationAttributes.dot11Ssid.SSIDLength == 0 || 
                        connection.wlanAssociationAttributes.dot11Ssid.SSIDLength > 32)
                    {
                        return null;
                    }

                    var ssidBytes = new byte[connection.wlanAssociationAttributes.dot11Ssid.SSIDLength];
                    Buffer.BlockCopy(connection.wlanAssociationAttributes.dot11Ssid.SSID, 0, 
                        ssidBytes, 0, (int)connection.wlanAssociationAttributes.dot11Ssid.SSIDLength);
                    
                    var ssid = Encoding.UTF8.GetString(ssidBytes).Trim('\0').Trim();

                    if (string.IsNullOrWhiteSpace(ssid))
                    {
                        return null;
                    }

                    return new WiFiNetwork
                    {
                        SSID = ssid,
                        SignalQuality = Math.Min(100, Math.Max(0, (int)connection.wlanAssociationAttributes.wlanSignalQuality)),
                        IsSecure = connection.wlanSecurityAttributes.bSecurityEnabled,
                        IsConnected = true
                    };
                }
                finally
                {
                    if (dataPtr != IntPtr.Zero)
                    {
                        WlanFreeMemory(dataPtr);
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        public WiFiNetwork? GetConnectionStatusForInterface(Guid interfaceGuid)
        {
            try
            {
                IntPtr dataPtr = IntPtr.Zero;
                uint dataSize = 0;
                IntPtr opcodeValueType = IntPtr.Zero;
                
                var guid = interfaceGuid;
                var result = WlanQueryInterface(_clientHandle, ref guid, 
                    WLAN_INTF_OPCODE.wlan_intf_opcode_current_connection, 
                    IntPtr.Zero, out dataSize, out dataPtr, out opcodeValueType);
                
                if (result != 0 || dataPtr == IntPtr.Zero)
                {
                    return null;
                }

                try
                {
                    var connection = Marshal.PtrToStructure<WLAN_CONNECTION_ATTRIBUTES>(dataPtr);
                    
                    if (connection.isState != WLAN_INTERFACE_STATE.wlan_interface_state_connected)
                    {
                        return null;
                    }

                    if (connection.wlanAssociationAttributes.dot11Ssid.SSIDLength == 0 || 
                        connection.wlanAssociationAttributes.dot11Ssid.SSIDLength > 32)
                    {
                        return null;
                    }

                    var ssidBytes = new byte[connection.wlanAssociationAttributes.dot11Ssid.SSIDLength];
                    Buffer.BlockCopy(connection.wlanAssociationAttributes.dot11Ssid.SSID, 0, 
                        ssidBytes, 0, (int)connection.wlanAssociationAttributes.dot11Ssid.SSIDLength);
                    
                    var ssid = Encoding.UTF8.GetString(ssidBytes).Trim('\0').Trim();

                    if (string.IsNullOrWhiteSpace(ssid))
                    {
                        return null;
                    }

                    return new WiFiNetwork
                    {
                        SSID = ssid,
                        SignalQuality = Math.Min(100, Math.Max(0, (int)connection.wlanAssociationAttributes.wlanSignalQuality)),
                        IsSecure = connection.wlanSecurityAttributes.bSecurityEnabled,
                        IsConnected = true
                    };
                }
                finally
                {
                    if (dataPtr != IntPtr.Zero)
                    {
                        WlanFreeMemory(dataPtr);
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        public List<string> GetProfiles()
        {
            var profiles = new List<string>();
            
            try
            {
                IntPtr profileListPtr = IntPtr.Zero;
                
                var result = WlanGetProfileList(_clientHandle, ref _interfaceGuid, IntPtr.Zero, out profileListPtr);
                
                if (result != 0 || profileListPtr == IntPtr.Zero)
                {
                    return profiles;
                }

                try
                {
                    var profileList = Marshal.PtrToStructure<WLAN_PROFILE_INFO_LIST>(profileListPtr);
                    var currentPtr = new IntPtr(profileListPtr.ToInt64() + 
                        Marshal.OffsetOf<WLAN_PROFILE_INFO_LIST>("ProfileInfo").ToInt64());

                    for (int i = 0; i < profileList.dwNumberOfItems; i++)
                    {
                        try
                        {
                            var profileInfo = Marshal.PtrToStructure<WLAN_PROFILE_INFO>(currentPtr);
                            if (!string.IsNullOrWhiteSpace(profileInfo.strProfileName))
                            {
                                profiles.Add(profileInfo.strProfileName);
                            }
                        }
                        catch
                        {
                            // Skip malformed profile
                        }
                        
                        currentPtr = new IntPtr(currentPtr.ToInt64() + Marshal.SizeOf<WLAN_PROFILE_INFO>());
                    }

                    WlanFreeMemory(profileListPtr);
                }
                catch
                {
                    if (profileListPtr != IntPtr.Zero)
                    {
                        try { WlanFreeMemory(profileListPtr); } catch { }
                    }
                }
            }
            catch
            {
                // Return empty list on error
            }
            
            return profiles;
        }

        public bool HasProfile(string ssid)
        {
            try
            {
                return GetProfiles().Any(p => p.Equals(ssid, StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return false;
            }
        }

        public bool CreateProfile(string ssid, string password)
        {
            try
            {
                // Escape XML special characters
                var escapedSsid = System.Security.SecurityElement.Escape(ssid) ?? ssid;
                var escapedPassword = System.Security.SecurityElement.Escape(password) ?? password;
                
                var profileXml = $@"<?xml version=""1.0""?>
<WLANProfile xmlns=""http://www.microsoft.com/networking/WLAN/profile/v1"">
    <name>{escapedSsid}</name>
    <SSIDConfig>
        <SSID>
            <name>{escapedSsid}</name>
        </SSID>
    </SSIDConfig>
    <connectionType>ESS</connectionType>
    <connectionMode>auto</connectionMode>
    <MSM>
        <security>
            <authEncryption>
                <authentication>WPA2PSK</authentication>
                <encryption>AES</encryption>
                <useOneX>false</useOneX>
            </authEncryption>
            <sharedKey>
                <keyType>passPhrase</keyType>
                <protected>false</protected>
                <keyMaterial>{escapedPassword}</keyMaterial>
            </sharedKey>
        </security>
    </MSM>
</WLANProfile>";

                uint reasonCode = 0;
                var result = WlanSetProfile(_clientHandle, ref _interfaceGuid, 0, profileXml, 
                    null, true, IntPtr.Zero, out reasonCode);
                
                return result == 0;
            }
            catch
            {
                return false;
            }
        }

        public bool Connect(string ssid)
        {
            try
            {
                var connectionParams = new WLAN_CONNECTION_PARAMETERS
                {
                    wlanConnectionMode = WLAN_CONNECTION_MODE.wlan_connection_mode_profile,
                    strProfile = ssid,
                    pDot11Ssid = IntPtr.Zero,
                    pDesiredBssidList = IntPtr.Zero,
                    dot11BssType = DOT11_BSS_TYPE.dot11_BSS_type_infrastructure,
                    dwFlags = 0
                };

                var result = WlanConnect(_clientHandle, ref _interfaceGuid, ref connectionParams, IntPtr.Zero);
                return result == 0;
            }
            catch
            {
                return false;
            }
        }

        public bool Disconnect()
        {
            try
            {
                var result = WlanDisconnect(_clientHandle, ref _interfaceGuid, IntPtr.Zero);
                return result == 0;
            }
            catch
            {
                return false;
            }
        }

        public bool DeleteProfile(string ssid)
        {
            try
            {
                var result = WlanDeleteProfile(_clientHandle, ref _interfaceGuid, ssid, IntPtr.Zero);
                return result == 0;
            }
            catch
            {
                return false;
            }
        }

        ~WiFiService()
        {
            try
            {
                if (_clientHandle != IntPtr.Zero)
                {
                    WlanCloseHandle(_clientHandle, IntPtr.Zero);
                    _clientHandle = IntPtr.Zero;
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        // P/Invoke declarations
        [DllImport("wlanapi.dll")]
        private static extern uint WlanOpenHandle(uint clientVersion, IntPtr pReserved, out uint negotiatedVersion, out IntPtr clientHandle);

        [DllImport("wlanapi.dll")]
        private static extern uint WlanCloseHandle(IntPtr clientHandle, IntPtr pReserved);

        [DllImport("wlanapi.dll")]
        private static extern uint WlanEnumInterfaces(IntPtr clientHandle, IntPtr pReserved, out IntPtr ppInterfaceList);

        [DllImport("wlanapi.dll")]
        private static extern uint WlanScan(IntPtr clientHandle, ref Guid pInterfaceGuid, IntPtr pDot11Ssid, IntPtr pIeData, IntPtr pReserved);

        [DllImport("wlanapi.dll")]
        private static extern uint WlanGetAvailableNetworkList(IntPtr clientHandle, ref Guid pInterfaceGuid, uint dwFlags, IntPtr pReserved, out IntPtr ppAvailableNetworkList);

        [DllImport("wlanapi.dll")]
        private static extern uint WlanQueryInterface(IntPtr clientHandle, ref Guid pInterfaceGuid, WLAN_INTF_OPCODE OpCode, IntPtr pReserved, out uint pdwDataSize, out IntPtr ppData, out IntPtr pWlanOpcodeValueType);

        [DllImport("wlanapi.dll")]
        private static extern uint WlanGetProfileList(IntPtr clientHandle, ref Guid pInterfaceGuid, IntPtr pReserved, out IntPtr ppProfileList);

        [DllImport("wlanapi.dll")]
        private static extern uint WlanSetProfile(IntPtr clientHandle, ref Guid pInterfaceGuid, uint dwFlags, [MarshalAs(UnmanagedType.LPWStr)] string strProfileXml, [MarshalAs(UnmanagedType.LPWStr)] string? strAllUserProfileSecurity, bool bOverwrite, IntPtr pReserved, out uint pdwReasonCode);

        [DllImport("wlanapi.dll")]
        private static extern uint WlanConnect(IntPtr clientHandle, ref Guid pInterfaceGuid, ref WLAN_CONNECTION_PARAMETERS pConnectionParameters, IntPtr pReserved);

        [DllImport("wlanapi.dll")]
        private static extern uint WlanDisconnect(IntPtr clientHandle, ref Guid pInterfaceGuid, IntPtr pReserved);

        [DllImport("wlanapi.dll")]
        private static extern uint WlanDeleteProfile(IntPtr clientHandle, ref Guid pInterfaceGuid, [MarshalAs(UnmanagedType.LPWStr)] string strProfileName, IntPtr pReserved);

        [DllImport("wlanapi.dll")]
        private static extern void WlanFreeMemory(IntPtr pMemory);

        // Structures
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WLAN_INTERFACE_INFO
        {
            public Guid InterfaceGuid;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string strInterfaceDescription;
            public WLAN_INTERFACE_STATE isState;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WLAN_INTERFACE_INFO_LIST
        {
            public uint dwNumberOfItems;
            public uint dwIndex;
            public WLAN_INTERFACE_INFO InterfaceInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DOT11_SSID
        {
            public uint SSIDLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] SSID;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WLAN_AVAILABLE_NETWORK
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string strProfileName;
            public DOT11_SSID dot11Ssid;
            public DOT11_BSS_TYPE dot11BssType;
            public uint uNumberOfBssids;
            public bool bNetworkConnectable;
            public uint wlanNotConnectableReason;
            public uint uNumberOfPhyTypes;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public uint[] dot11PhyTypes;
            public bool bMorePhyTypes;
            public uint wlanSignalQuality;
            public bool bSecurityEnabled;
            public DOT11_AUTH_ALGORITHM dot11DefaultAuthAlgorithm;
            public DOT11_CIPHER_ALGORITHM dot11DefaultCipherAlgorithm;
            public uint dwFlags;
            public uint dwReserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WLAN_AVAILABLE_NETWORK_LIST
        {
            public uint dwNumberOfItems;
            public uint dwIndex;
            public WLAN_AVAILABLE_NETWORK Network;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WLAN_CONNECTION_ATTRIBUTES
        {
            public WLAN_INTERFACE_STATE isState;
            public WLAN_CONNECTION_MODE wlanConnectionMode;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string strProfileName;
            public WLAN_ASSOCIATION_ATTRIBUTES wlanAssociationAttributes;
            public WLAN_SECURITY_ATTRIBUTES wlanSecurityAttributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WLAN_ASSOCIATION_ATTRIBUTES
        {
            public DOT11_SSID dot11Ssid;
            public DOT11_BSS_TYPE dot11BssType;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public byte[] dot11Bssid;
            public DOT11_PHY_TYPE dot11PhyType;
            public uint uDot11PhyIndex;
            public uint wlanSignalQuality;
            public uint ulRxRate;
            public uint ulTxRate;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WLAN_SECURITY_ATTRIBUTES
        {
            public bool bSecurityEnabled;
            public bool bOneXEnabled;
            public DOT11_AUTH_ALGORITHM dot11AuthAlgorithm;
            public DOT11_CIPHER_ALGORITHM dot11CipherAlgorithm;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WLAN_PROFILE_INFO
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string strProfileName;
            public uint dwFlags;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct WLAN_PROFILE_INFO_LIST
        {
            public uint dwNumberOfItems;
            public uint dwIndex;
            public WLAN_PROFILE_INFO ProfileInfo;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WLAN_CONNECTION_PARAMETERS
        {
            public WLAN_CONNECTION_MODE wlanConnectionMode;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string strProfile;
            public IntPtr pDot11Ssid;
            public IntPtr pDesiredBssidList;
            public DOT11_BSS_TYPE dot11BssType;
            public uint dwFlags;
        }

        // Enums
        private enum DOT11_BSS_TYPE
        {
            dot11_BSS_type_infrastructure = 1,
            dot11_BSS_type_independent = 2,
            dot11_BSS_type_any = 3
        }

        private enum DOT11_PHY_TYPE
        {
            dot11_phy_type_unknown = 0,
            dot11_phy_type_any = 0,
            dot11_phy_type_fhss = 1,
            dot11_phy_type_dsss = 2,
            dot11_phy_type_irbaseband = 3,
            dot11_phy_type_ofdm = 4,
            dot11_phy_type_hrdsss = 5,
            dot11_phy_type_erp = 6,
            dot11_phy_type_ht = 7,
            dot11_phy_type_vht = 8
        }

        private enum DOT11_AUTH_ALGORITHM
        {
            DOT11_AUTH_ALGO_80211_OPEN = 1,
            DOT11_AUTH_ALGO_80211_SHARED_KEY = 2,
            DOT11_AUTH_ALGO_WPA = 3,
            DOT11_AUTH_ALGO_WPA_PSK = 4,
            DOT11_AUTH_ALGO_WPA_NONE = 5,
            DOT11_AUTH_ALGO_RSNA = 6,
            DOT11_AUTH_ALGO_RSNA_PSK = 7
        }

        private enum DOT11_CIPHER_ALGORITHM
        {
            DOT11_CIPHER_ALGO_NONE = 0,
            DOT11_CIPHER_ALGO_WEP40 = 1,
            DOT11_CIPHER_ALGO_TKIP = 2,
            DOT11_CIPHER_ALGO_CCMP = 4,
            DOT11_CIPHER_ALGO_WEP104 = 5,
            DOT11_CIPHER_ALGO_WPA_USE_GROUP = 256,
            DOT11_CIPHER_ALGO_RSN_USE_GROUP = 256,
            DOT11_CIPHER_ALGO_WEP = 257
        }

        private enum WLAN_CONNECTION_MODE
        {
            wlan_connection_mode_profile = 0,
            wlan_connection_mode_temporary_profile = 1,
            wlan_connection_mode_discovery_secure = 2,
            wlan_connection_mode_discovery_unsecure = 3,
            wlan_connection_mode_auto = 4,
            wlan_connection_mode_invalid = 5
        }

        private enum WLAN_INTF_OPCODE
        {
            wlan_intf_opcode_autoconf_start = 0,
            wlan_intf_opcode_autoconf_enabled,
            wlan_intf_opcode_background_scan_enabled,
            wlan_intf_opcode_media_streaming_mode,
            wlan_intf_opcode_radio_state,
            wlan_intf_opcode_bss_type,
            wlan_intf_opcode_interface_state,
            wlan_intf_opcode_current_connection,
            wlan_intf_opcode_channel_number,
            wlan_intf_opcode_supported_infrastructure_auth_cipher_pairs,
            wlan_intf_opcode_supported_adhoc_auth_cipher_pairs,
            wlan_intf_opcode_supported_country_or_region_string_list,
            wlan_intf_opcode_current_operation_mode,
            wlan_intf_opcode_supported_safe_mode,
            wlan_intf_opcode_certified_safe_mode,
            wlan_intf_opcode_hosted_network_capable,
            wlan_intf_opcode_management_frame_protection_capable,
            wlan_intf_opcode_autoconf_end = 0x0fffffff,
            wlan_intf_opcode_msm_start = 0x10000100,
            wlan_intf_opcode_statistics,
            wlan_intf_opcode_rssi,
            wlan_intf_opcode_msm_end = 0x1fffffff,
            wlan_intf_opcode_security_start = 0x20010000,
            wlan_intf_opcode_security_end = 0x2fffffff,
            wlan_intf_opcode_ihv_start = 0x30000000,
            wlan_intf_opcode_ihv_end = 0x3fffffff
        }
    }
}