using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using PokerClient.Core;
using PokerClient.UI;
using PokerClient.UI.Components;
using PokerClient.Networking;

namespace PokerClient.UI.Scenes
{
    /// <summary>
    /// Main Menu scene - Entry point of the game.
    /// Builds the entire menu UI programmatically.
    /// </summary>
    public class MainMenuScene : MonoBehaviour
    {
        [Header("Server Configuration")]
        // Use network IP for phone testing, localhost for Unity Editor only
        [SerializeField] private string serverUrl = "http://192.168.1.23:3000";
        
        // Known servers are loaded from Resources/known_servers.json
        // This file is automatically updated when you discover new servers in the Unity Editor
        // When you build an APK, all discovered servers are baked in!
        private const string KNOWN_SERVERS_RESOURCE = "known_servers";
        
        [Header("Scene References")]
        [SerializeField] private Canvas canvas;
        
        [Header("Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private GameObject registerPanel;
        [SerializeField] private GameObject loadingPanel;
        
        [Header("Login Components")]
        [SerializeField] private TMP_InputField usernameInput;
        [SerializeField] private TMP_InputField passwordInput;
        [SerializeField] private TMP_InputField emailInput;
        [SerializeField] private TMP_InputField regUsernameInput;
        [SerializeField] private TMP_InputField regPasswordInput;
        [SerializeField] private TextMeshProUGUI errorText;
        [SerializeField] private TextMeshProUGUI regErrorText;
        [SerializeField] private TextMeshProUGUI loadingText;
        
        [Header("Player Info")]
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private TextMeshProUGUI playerChipsText;
        [SerializeField] private TextMeshProUGUI playerLevelText;
        [SerializeField] private Image xpProgressBar;
        
        private bool _isLoggedIn = false;
        private GameService _gameService;
        
        // Server settings
        private GameObject serverSettingsPanel;
        private TMP_InputField serverUrlInput;
        private TextMeshProUGUI _scanStatusText;
        private bool _isScanning = false;
        private const string SERVER_URL_KEY = "ServerUrl";
        private const string SAVED_SERVERS_KEY = "SavedServers"; // JSON list of known servers
        
        // Auto-connect UI
        private GameObject _connectionPanel;
        private TextMeshProUGUI _connectionStatusText;
        private bool _autoConnecting = false;
        
        private void Start()
        {
            // Load saved server URL if exists
            if (PlayerPrefs.HasKey(SERVER_URL_KEY))
            {
                serverUrl = PlayerPrefs.GetString(SERVER_URL_KEY);
            }
            
            // Build scene first (includes connection panel)
            BuildScene();
            
            // Play menu music
            AudioManager.Instance?.PlayMenuMusic();
            
            // Initialize game service reference
            _gameService = GameService.Instance;
            if (_gameService == null)
            {
                Debug.LogError("[MainMenu] Failed to get GameService instance!");
                return;
            }
            
            // Subscribe to events
            _gameService.OnLoginSuccess += OnLoginSuccessHandler;
            _gameService.OnLoginFailed += OnLoginFailedHandler;
            
            // Check if already logged in (e.g., returning from another scene)
            if (_gameService.IsLoggedIn)
            {
                _isLoggedIn = true;
                
                var profile = _gameService.CurrentUser;
                int xp = profile?.adventureProgress?.xp ?? 0;
                int xpNext = profile?.adventureProgress?.xpToNextLevel ?? 100;
                float xpProgress = xpNext > 0 ? (float)xp / xpNext : 0;
                
                UpdatePlayerInfo(
                    profile?.username ?? "Player", 
                    (int)(profile?.chips ?? 0), 
                    profile?.adventureProgress?.level ?? 1, 
                    xpProgress
                );
                
                ShowMainMenu();
            }
            else
            {
                // Start auto-connection process
                StartCoroutine(AutoConnectToServer());
            }
        }
        
        private System.Collections.IEnumerator AutoConnectToServer()
        {
            _autoConnecting = true;
            ShowConnectionPanel();
            
            // STEP 1: Try last known working server first
            if (!string.IsNullOrEmpty(serverUrl))
            {
                UpdateConnectionStatus($"Trying last server...");
                yield return new WaitForSeconds(0.3f);
                
                bool lastServerWorks = false;
                yield return StartCoroutine(TestServerConnection(serverUrl, (success) => lastServerWorks = success));
                
                if (lastServerWorks)
                {
                    UpdateConnectionStatus($"✓ Connected!");
                    yield return new WaitForSeconds(0.5f);
                    ConnectAndShowLogin(serverUrl);
                    yield break;
                }
            }
            
            // STEP 2: Scan local network
            UpdateConnectionStatus("Scanning local network...");
            yield return new WaitForSeconds(0.3f);
            
            string localIP = GetLocalIPAddress();
            if (!string.IsNullOrEmpty(localIP))
            {
                string[] parts = localIP.Split('.');
                if (parts.Length >= 4)
                {
                    string baseIP = $"{parts[0]}.{parts[1]}.{parts[2]}";
                    
                    for (int i = 1; i <= 50; i++)
                    {
                        string testIP = $"{baseIP}.{i}";
                        string testUrl = $"http://{testIP}:3000";
                        
                        UpdateConnectionStatus($"Scanning {testIP}...");
                        
                        bool found = false;
                        yield return StartCoroutine(TestServerConnection(testUrl, (success) => found = success));
                        
                        if (found)
                        {
                            UpdateConnectionStatus($"✓ Found server at {testIP}!");
                            yield return StartCoroutine(SaveServerWithPublicIP(testUrl));
                            yield return new WaitForSeconds(0.5f);
                            ConnectAndShowLogin(testUrl);
                            yield break;
                        }
                        
                        yield return null;
                    }
                }
            }
            
            // STEP 3: Check saved remote servers
            UpdateConnectionStatus("Checking saved servers...");
            yield return new WaitForSeconds(0.3f);
            
            var savedServers = GetSavedServers();
            foreach (var server in savedServers)
            {
                if (string.IsNullOrEmpty(server.publicIP)) continue;
                
                string remoteUrl = $"http://{server.publicIP}:{server.port}";
                UpdateConnectionStatus($"Trying {server.name}...");
                
                bool found = false;
                yield return StartCoroutine(TestServerConnection(remoteUrl, (success) => found = success));
                
                if (found)
                {
                    UpdateConnectionStatus($"✓ Connected to {server.name}!");
                    yield return new WaitForSeconds(0.5f);
                    ConnectAndShowLogin(remoteUrl);
                    yield break;
                }
                
                yield return null;
            }
            
            // STEP 4: No server found - show manual entry
            UpdateConnectionStatus("No server found");
            yield return new WaitForSeconds(1f);
            _autoConnecting = false;
            HideConnectionPanel();
            ShowServerSettings();
        }
        
        private void ConnectAndShowLogin(string url)
        {
            serverUrl = url;
            PlayerPrefs.SetString(SERVER_URL_KEY, url);
            PlayerPrefs.Save();
            
            _gameService.Connect(url);
            _autoConnecting = false;
            HideConnectionPanel();
            ShowLoginPanel();
        }
        
        private void ShowConnectionPanel()
        {
            if (_connectionPanel != null)
                _connectionPanel.SetActive(true);
            if (loginPanel != null)
                loginPanel.SetActive(false);
            if (registerPanel != null)
                registerPanel.SetActive(false);
            if (mainPanel != null)
                mainPanel.SetActive(false);
        }
        
        private void HideConnectionPanel()
        {
            if (_connectionPanel != null)
                _connectionPanel.SetActive(false);
        }
        
        private void UpdateConnectionStatus(string status)
        {
            Debug.Log($"[AutoConnect] {status}");
            if (_connectionStatusText != null)
                _connectionStatusText.text = status;
        }
        
        private System.Collections.IEnumerator CheckConnectionStatus()
        {
            yield return new WaitForSeconds(1f);
            
            // Don't change panel if already logged in
            if (_isLoggedIn) yield break;
            
            if (SocketManager.Instance != null && SocketManager.Instance.IsConnected)
            {
                HideLoading();
                // Only show login if not already logged in
                if (!_gameService.IsLoggedIn)
                {
                    ShowLoginPanel();
                }
            }
            else
            {
                // Still connecting - wait a bit more then show login anyway
                yield return new WaitForSeconds(2f);
                HideLoading();
                if (!_gameService.IsLoggedIn)
                {
                    ShowLoginPanel();
                }
            }
        }
        
        private void OnDestroy()
        {
            if (_gameService != null)
            {
                _gameService.OnLoginSuccess -= OnLoginSuccessHandler;
                _gameService.OnLoginFailed -= OnLoginFailedHandler;
            }
        }
        
        private void BuildScene()
        {
            var theme = Theme.Current;
            
            // Ensure canvas exists
            if (canvas == null)
            {
                var canvasObj = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObj.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                
                var scaler = canvasObj.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920); // Mobile-friendly (portrait)
                scaler.matchWidthOrHeight = 0f; // Match width for consistent sizing
            }
            
            // Background
            var bg = UIFactory.CreatePanel(canvas.transform, "Background", theme.backgroundColor);
            UIFactory.FillParent(bg.GetComponent<RectTransform>());
            
            // === LOGIN PANEL ===
            BuildLoginPanel(canvas.transform);
            
            // === REGISTER PANEL ===
            BuildRegisterPanel(canvas.transform);
            
            // === MAIN MENU PANEL ===
            BuildMainPanel(canvas.transform);
            
            // === CONNECTION PANEL (auto-connect status) ===
            BuildConnectionPanel();
            
            // === LOADING PANEL (on top) ===
            BuildLoadingPanel();
            
            // === SERVER SETTINGS PANEL (on top of everything) ===
            BuildServerSettingsPanel();
        }
        
        private void BuildConnectionPanel()
        {
            var theme = Theme.Current;
            
            // Full-screen panel with nice gradient-like background
            _connectionPanel = UIFactory.CreatePanel(canvas.transform, "ConnectionPanel", theme.backgroundColor);
            var panelRect = _connectionPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            
            // Center content container
            var content = UIFactory.CreatePanel(_connectionPanel.transform, "Content", Color.clear);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.1f, 0.3f);
            contentRect.anchorMax = new Vector2(0.9f, 0.7f);
            contentRect.sizeDelta = Vector2.zero;
            
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 30;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            
            // Title
            var title = UIFactory.CreateTitle(content.transform, "Title", "POKER", 48f);
            title.alignment = TextAlignmentOptions.Center;
            title.color = theme.accentColor;
            var titleLE = title.gameObject.AddComponent<LayoutElement>();
            titleLE.preferredHeight = 60;
            
            // Subtitle
            var subtitle = UIFactory.CreateText(content.transform, "Subtitle", "Finding Server...", 24f);
            subtitle.alignment = TextAlignmentOptions.Center;
            subtitle.color = theme.textSecondary;
            var subLE = subtitle.gameObject.AddComponent<LayoutElement>();
            subLE.preferredHeight = 40;
            
            // Status text (the main updating text)
            _connectionStatusText = UIFactory.CreateText(content.transform, "Status", "Initializing...", 20f);
            _connectionStatusText.alignment = TextAlignmentOptions.Center;
            _connectionStatusText.color = theme.textPrimary;
            var statusLE = _connectionStatusText.gameObject.AddComponent<LayoutElement>();
            statusLE.preferredHeight = 30;
            
            // Loading dots animation indicator
            var dots = UIFactory.CreateText(content.transform, "Dots", "• • •", 28f);
            dots.alignment = TextAlignmentOptions.Center;
            dots.color = theme.primaryColor;
            var dotsLE = dots.gameObject.AddComponent<LayoutElement>();
            dotsLE.preferredHeight = 40;
            
            _connectionPanel.SetActive(false);
        }
        
        private void BuildServerSettingsPanel()
        {
            var theme = Theme.Current;
            
            // Semi-transparent overlay
            serverSettingsPanel = UIFactory.CreatePanel(canvas.transform, "ServerSettingsPanel", new Color(0, 0, 0, 0.85f));
            var overlayRect = serverSettingsPanel.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;
            
            // Dialog box - fixed size, centered
            var dialog = UIFactory.CreatePanel(serverSettingsPanel.transform, "Dialog", theme.panelColor);
            var dialogRect = dialog.GetComponent<RectTransform>();
            dialogRect.anchorMin = new Vector2(0.5f, 0.5f);
            dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
            dialogRect.pivot = new Vector2(0.5f, 0.5f);
            dialogRect.sizeDelta = new Vector2(300, 280);
            
            var layout = dialog.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 8;
            layout.padding = new RectOffset(15, 15, 15, 15);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandHeight = false;
            
            // Title
            var title = UIFactory.CreateTitle(dialog.transform, "Title", "Server Settings", 20f);
            var titleLayout = title.gameObject.AddComponent<LayoutElement>();
            titleLayout.preferredHeight = 28;
            titleLayout.minWidth = 260;
            
            // Server URL input
            serverUrlInput = UIFactory.CreateInputField(dialog.transform, "ServerUrl", "http://192.168.1.23:3000", 260, 38);
            serverUrlInput.text = serverUrl;
            serverUrlInput.contentType = TMP_InputField.ContentType.Standard;
            var urlLayout = serverUrlInput.gameObject.AddComponent<LayoutElement>();
            urlLayout.preferredHeight = 38;
            urlLayout.minWidth = 260;
            
            // Scan status text
            _scanStatusText = UIFactory.CreateText(dialog.transform, "Status", "Tap SCAN to find server", 11f, theme.textSecondary);
            var statusLayout = _scanStatusText.gameObject.AddComponent<LayoutElement>();
            statusLayout.preferredHeight = 18;
            statusLayout.minWidth = 260;
            
            // Scan button - big and prominent
            var scanBtn = UIFactory.CreatePrimaryButton(dialog.transform, "Scan", "🔍 SCAN NETWORK", StartNetworkScan, 260, 40);
            var scanLayout = scanBtn.gameObject.AddComponent<LayoutElement>();
            scanLayout.preferredHeight = 40;
            scanLayout.minWidth = 260;
            
            // Button row
            var buttonRow = UIFactory.CreateHorizontalGroup(dialog.transform, "Buttons", 10);
            var buttonRowLayout = buttonRow.AddComponent<LayoutElement>();
            buttonRowLayout.preferredHeight = 36;
            buttonRowLayout.minWidth = 260;
            var rowGroup = buttonRow.GetComponent<HorizontalLayoutGroup>();
            rowGroup.childControlWidth = false;
            rowGroup.childForceExpandWidth = false;
            rowGroup.childAlignment = TextAnchor.MiddleCenter;
            
            // Cancel button
            var cancelBtn = UIFactory.CreateSecondaryButton(buttonRow.transform, "Cancel", "CANCEL", HideServerSettings, 90, 32);
            
            // Save button
            var saveBtn = UIFactory.CreatePrimaryButton(buttonRow.transform, "Save", "SAVE", SaveServerSettings, 90, 32);
            
            serverSettingsPanel.SetActive(false);
        }
        
        private void ShowServerSettings()
        {
            if (serverUrlInput != null)
                serverUrlInput.text = serverUrl;
            serverSettingsPanel?.SetActive(true);
        }
        
        private void HideServerSettings()
        {
            serverSettingsPanel?.SetActive(false);
        }
        
        private void SaveServerSettings()
        {
            if (serverUrlInput != null && !string.IsNullOrEmpty(serverUrlInput.text))
            {
                serverUrl = serverUrlInput.text.Trim();
                
                // Save to PlayerPrefs
                PlayerPrefs.SetString(SERVER_URL_KEY, serverUrl);
                PlayerPrefs.Save();
                
                Debug.Log($"[MainMenu] Server URL saved: {serverUrl}");
                
                // Connect to new server
                HideServerSettings();
                ConnectAndShowLogin(serverUrl);
            }
        }
        
        private void StartNetworkScan()
        {
            if (_isScanning) return;
            StartCoroutine(ScanForServer());
        }
        
        private System.Collections.IEnumerator ScanForServer()
        {
            _isScanning = true;
            if (_scanStatusText != null)
                _scanStatusText.text = "Scanning local network...";
            
            // STEP 1: Scan local network
            string localIP = GetLocalIPAddress();
            if (!string.IsNullOrEmpty(localIP))
            {
                string[] parts = localIP.Split('.');
                if (parts.Length >= 4)
                {
                    string baseIP = $"{parts[0]}.{parts[1]}.{parts[2]}";
                    Debug.Log($"[MainMenu] Scanning local network: {baseIP}.x");
                    
                    for (int i = 1; i <= 50; i++)
                    {
                        string testIP = $"{baseIP}.{i}";
                        string testUrl = $"http://{testIP}:3000";
                        
                        if (_scanStatusText != null)
                            _scanStatusText.text = $"Local: {testIP}...";
                        
                        bool found = false;
                        yield return StartCoroutine(TestServerConnection(testUrl, (success) => found = success));
                        
                        if (found)
                        {
                            Debug.Log($"[MainMenu] Found local server at: {testUrl}");
                            // Save this server with its public IP
                            yield return StartCoroutine(SaveServerWithPublicIP(testUrl));
                            
                            if (serverUrlInput != null)
                                serverUrlInput.text = testUrl;
                            if (_scanStatusText != null)
                                _scanStatusText.text = $"✓ Found server at {testIP}!";
                            _isScanning = false;
                            yield break;
                        }
                        
                        yield return null;
                    }
                }
            }
            
            // STEP 2: Check saved remote servers
            if (_scanStatusText != null)
                _scanStatusText.text = "Checking saved servers...";
            
            var savedServers = GetSavedServers();
            Debug.Log($"[MainMenu] Checking {savedServers.Count} saved remote servers");
            
            foreach (var server in savedServers)
            {
                if (string.IsNullOrEmpty(server.publicIP)) continue;
                
                string remoteUrl = $"http://{server.publicIP}:{server.port}";
                if (_scanStatusText != null)
                    _scanStatusText.text = $"Remote: {server.name}...";
                
                bool found = false;
                yield return StartCoroutine(TestServerConnection(remoteUrl, (success) => found = success));
                
                if (found)
                {
                    Debug.Log($"[MainMenu] Found remote server: {remoteUrl} ({server.name})");
                    if (serverUrlInput != null)
                        serverUrlInput.text = remoteUrl;
                    if (_scanStatusText != null)
                        _scanStatusText.text = $"✓ Found {server.name}!";
                    _isScanning = false;
                    yield break;
                }
                
                yield return null;
            }
            
            if (_scanStatusText != null)
                _scanStatusText.text = "No server found. Enter manually.";
            _isScanning = false;
        }
        
        // Saved server info
        [System.Serializable]
        private class SavedServer
        {
            public string name;
            public string localIP;
            public string publicIP;
            public int port;
            public long lastSeen;
        }
        
        [System.Serializable]
        private class SavedServerList
        {
            public System.Collections.Generic.List<SavedServer> servers = new System.Collections.Generic.List<SavedServer>();
        }
        
        private System.Collections.Generic.List<SavedServer> GetSavedServers()
        {
            var allServers = new System.Collections.Generic.List<SavedServer>();
            
            // 1. Load from Resources (baked into build)
            try
            {
                var textAsset = Resources.Load<TextAsset>(KNOWN_SERVERS_RESOURCE);
                if (textAsset != null)
                {
                    var bakedList = JsonUtility.FromJson<SavedServerList>(textAsset.text);
                    if (bakedList?.servers != null)
                    {
                        allServers.AddRange(bakedList.servers);
                        Debug.Log($"[MainMenu] Loaded {bakedList.servers.Count} baked-in servers from Resources");
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[MainMenu] Failed to load baked servers: {e.Message}");
            }
            
            // 2. Load from PlayerPrefs (runtime discoveries)
            try
            {
                string json = PlayerPrefs.GetString(SAVED_SERVERS_KEY, "");
                if (!string.IsNullOrEmpty(json))
                {
                    var runtimeList = JsonUtility.FromJson<SavedServerList>(json);
                    if (runtimeList?.servers != null)
                    {
                        // Merge - add new servers, update existing ones
                        foreach (var server in runtimeList.servers)
                        {
                            var existing = allServers.Find(s => 
                                (!string.IsNullOrEmpty(s.publicIP) && s.publicIP == server.publicIP) ||
                                (!string.IsNullOrEmpty(s.localIP) && s.localIP == server.localIP));
                            
                            if (existing != null)
                            {
                                // Update with newer info
                                if (server.lastSeen > existing.lastSeen)
                                {
                                    existing.name = server.name;
                                    existing.localIP = server.localIP;
                                    existing.publicIP = server.publicIP;
                                    existing.lastSeen = server.lastSeen;
                                }
                            }
                            else
                            {
                                allServers.Add(server);
                            }
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[MainMenu] Failed to load runtime servers: {e.Message}");
            }
            
            // Sort by last seen (most recent first)
            allServers.Sort((a, b) => b.lastSeen.CompareTo(a.lastSeen));
            
            return allServers;
        }
        
        private void SaveServer(SavedServer server)
        {
            // Get current runtime servers (not baked ones)
            var servers = new System.Collections.Generic.List<SavedServer>();
            try
            {
                string json = PlayerPrefs.GetString(SAVED_SERVERS_KEY, "");
                if (!string.IsNullOrEmpty(json))
                {
                    var list = JsonUtility.FromJson<SavedServerList>(json);
                    servers = list?.servers ?? new System.Collections.Generic.List<SavedServer>();
                }
            }
            catch { }
            
            // Update existing or add new
            var existing = servers.Find(s => 
                (!string.IsNullOrEmpty(s.publicIP) && s.publicIP == server.publicIP) ||
                (!string.IsNullOrEmpty(s.localIP) && s.localIP == server.localIP));
            
            if (existing != null)
            {
                existing.localIP = server.localIP;
                existing.publicIP = server.publicIP;
                existing.name = server.name;
                existing.lastSeen = server.lastSeen;
            }
            else
            {
                servers.Add(server);
            }
            
            // Keep only the 20 most recent servers
            servers.Sort((a, b) => b.lastSeen.CompareTo(a.lastSeen));
            if (servers.Count > 20)
                servers.RemoveRange(20, servers.Count - 20);
            
            var serverList = new SavedServerList { servers = servers };
            string jsonOutput = JsonUtility.ToJson(serverList, true);
            
            // Save to PlayerPrefs (works everywhere)
            PlayerPrefs.SetString(SAVED_SERVERS_KEY, jsonOutput);
            PlayerPrefs.Save();
            
            // In Editor: Also save to Resources file so it's baked into builds!
            #if UNITY_EDITOR
            SaveToResourcesFile(serverList);
            #endif
            
            Debug.Log($"[MainMenu] Saved server: {server.name} (local: {server.localIP}, public: {server.publicIP})");
        }
        
        #if UNITY_EDITOR
        private void SaveToResourcesFile(SavedServerList serverList)
        {
            try
            {
                // Load existing baked servers and merge
                var bakedServers = new System.Collections.Generic.List<SavedServer>();
                var textAsset = Resources.Load<TextAsset>(KNOWN_SERVERS_RESOURCE);
                if (textAsset != null)
                {
                    var existing = JsonUtility.FromJson<SavedServerList>(textAsset.text);
                    if (existing?.servers != null)
                        bakedServers.AddRange(existing.servers);
                }
                
                // Merge new servers into baked list
                foreach (var server in serverList.servers)
                {
                    if (string.IsNullOrEmpty(server.publicIP) && string.IsNullOrEmpty(server.localIP))
                        continue;
                    
                    var existingBaked = bakedServers.Find(s => 
                        (!string.IsNullOrEmpty(s.publicIP) && s.publicIP == server.publicIP) ||
                        (!string.IsNullOrEmpty(s.localIP) && s.localIP == server.localIP));
                    
                    if (existingBaked != null)
                    {
                        existingBaked.name = server.name;
                        existingBaked.localIP = server.localIP;
                        existingBaked.publicIP = server.publicIP;
                        existingBaked.lastSeen = server.lastSeen;
                    }
                    else
                    {
                        bakedServers.Add(server);
                    }
                }
                
                // Sort and limit
                bakedServers.Sort((a, b) => b.lastSeen.CompareTo(a.lastSeen));
                if (bakedServers.Count > 20)
                    bakedServers.RemoveRange(20, bakedServers.Count - 20);
                
                // Write to file
                var finalList = new SavedServerList { servers = bakedServers };
                string jsonOutput = JsonUtility.ToJson(finalList, true);
                string filePath = System.IO.Path.Combine(Application.dataPath, "Resources", "known_servers.json");
                System.IO.File.WriteAllText(filePath, jsonOutput);
                
                Debug.Log($"[MainMenu] Updated Resources/known_servers.json with {bakedServers.Count} servers");
                
                // Refresh Unity's asset database
                UnityEditor.AssetDatabase.Refresh();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[MainMenu] Failed to save to Resources file: {e.Message}");
            }
        }
        #endif
        
        private System.Collections.IEnumerator SaveServerWithPublicIP(string localUrl)
        {
            // Extract IP from URL
            string ip = localUrl.Replace("http://", "").Replace("https://", "");
            int port = 3000;
            if (ip.Contains(":"))
            {
                var parts = ip.Split(':');
                ip = parts[0];
                int.TryParse(parts[1], out port);
            }
            
            // Fetch server info to get public IP
            string serverInfoUrl = $"http://{ip}:{port}/api/server-info";
            string publicIP = null;
            string serverName = "Poker Server";
            
            var infoTask = System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    using (var client = new System.Net.WebClient())
                    {
                        client.Headers.Add("User-Agent", "PokerClient/1.0");
                        string json = client.DownloadString(serverInfoUrl);
                        // Parse manually since we're on a background thread
                        if (json.Contains("\"publicIP\""))
                        {
                            int start = json.IndexOf("\"publicIP\":\"") + 12;
                            int end = json.IndexOf("\"", start);
                            if (start > 11 && end > start)
                                publicIP = json.Substring(start, end - start);
                        }
                        if (json.Contains("\"name\""))
                        {
                            int start = json.IndexOf("\"name\":\"") + 8;
                            int end = json.IndexOf("\"", start);
                            if (start > 7 && end > start)
                                serverName = json.Substring(start, end - start);
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.Log($"[MainMenu] Could not fetch server info: {e.Message}");
                }
            });
            
            while (!infoTask.IsCompleted)
                yield return null;
            
            // Save the server
            var server = new SavedServer
            {
                name = serverName,
                localIP = ip,
                publicIP = publicIP,
                port = port,
                lastSeen = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            
            SaveServer(server);
        }
        
        private System.Collections.IEnumerator TestServerConnection(string url, System.Action<bool> callback)
        {
            bool success = false;
            
            // Use Socket connection test instead of HTTP (avoids Android's HTTP security block)
            string ip = url.Replace("http://", "").Replace("https://", "");
            int port = 3000;
            
            // Parse IP and port
            if (ip.Contains(":"))
            {
                var parts = ip.Split(':');
                ip = parts[0];
                int.TryParse(parts[1], out port);
            }
            
            // Try TCP connection
            var connectTask = System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    using (var client = new System.Net.Sockets.TcpClient())
                    {
                        var result = client.BeginConnect(ip, port, null, null);
                        bool connected = result.AsyncWaitHandle.WaitOne(System.TimeSpan.FromMilliseconds(500));
                        if (connected)
                        {
                            client.EndConnect(result);
                            return true;
                        }
                    }
                }
                catch { }
                return false;
            });
            
            // Wait for the task to complete
            while (!connectTask.IsCompleted)
            {
                yield return null;
            }
            
            success = connectTask.Result;
            callback(success);
        }
        
        private string GetLocalIPAddress()
        {
            try
            {
                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        string ipStr = ip.ToString();
                        // Prefer 192.168.x.x or 10.x.x.x (common local networks)
                        if (ipStr.StartsWith("192.168.") || ipStr.StartsWith("10."))
                            return ipStr;
                    }
                }
                // Fallback to any IPv4
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        return ip.ToString();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[MainMenu] Failed to get local IP: {e.Message}");
            }
            return null;
        }
        
        private void BuildLoadingPanel()
        {
            var theme = Theme.Current;
            
            loadingPanel = UIFactory.CreatePanel(canvas.transform, "LoadingPanel", new Color(0, 0, 0, 0.8f));
            var panelRect = loadingPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            
            loadingText = UIFactory.CreateTitle(loadingPanel.transform, "LoadingText", "Loading...", 32f);
            loadingText.alignment = TextAlignmentOptions.Center;
            var textRect = loadingText.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = new Vector2(400, 60);
            textRect.anchoredPosition = Vector2.zero;
            
            loadingPanel.SetActive(false);
        }
        
        private void BuildLoginPanel(Transform parent)
        {
            var theme = Theme.Current;
            
            // Compact fixed size panel, centered
            loginPanel = UIFactory.CreatePanel(parent, "LoginPanel", theme.panelColor);
            var panelRect = loginPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(320, 340); // Compact size
            
            var layout = loginPanel.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 5;
            layout.padding = new RectOffset(15, 15, 12, 12);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            
            // Title
            var title = UIFactory.CreateTitle(loginPanel.transform, "Title", "POKER GAME", 28f);
            title.color = theme.secondaryColor;
            var titleLayout = title.gameObject.AddComponent<LayoutElement>();
            titleLayout.preferredHeight = 36;
            titleLayout.minWidth = 280;
            
            // Subtitle
            var subtitle = UIFactory.CreateText(loginPanel.transform, "Subtitle", "Login to Play", 12f, theme.textSecondary);
            var subLayout = subtitle.gameObject.AddComponent<LayoutElement>();
            subLayout.preferredHeight = 18;
            subLayout.minWidth = 280;
            
            // Username input
            usernameInput = UIFactory.CreateInputField(loginPanel.transform, "Username", "Username", 280, 38);
            var usernameLayout = usernameInput.gameObject.AddComponent<LayoutElement>();
            usernameLayout.preferredHeight = 38;
            usernameLayout.minWidth = 280;
            
            // Password input
            passwordInput = UIFactory.CreateInputField(loginPanel.transform, "Password", "Password", 280, 38, 
                TMP_InputField.ContentType.Password);
            var passwordLayout = passwordInput.gameObject.AddComponent<LayoutElement>();
            passwordLayout.preferredHeight = 38;
            passwordLayout.minWidth = 280;
            
            // Load saved credentials
            LoadSavedCredentials();
            
            // Error text
            errorText = UIFactory.CreateText(loginPanel.transform, "Error", "", 10f, theme.textDanger);
            var errorLayout = errorText.gameObject.AddComponent<LayoutElement>();
            errorLayout.preferredHeight = 14;
            errorLayout.minWidth = 280;
            
            // Login button
            var loginBtn = UIFactory.CreatePrimaryButton(loginPanel.transform, "LoginBtn", "LOGIN", OnLoginClick, 280, 40);
            var loginLayout = loginBtn.gameObject.AddComponent<LayoutElement>();
            loginLayout.preferredHeight = 40;
            loginLayout.minWidth = 280;
            
            // Button row for Register and Server
            var buttonRow = UIFactory.CreateHorizontalGroup(loginPanel.transform, "ButtonRow", 15);
            var rowLayout = buttonRow.AddComponent<LayoutElement>();
            rowLayout.preferredHeight = 32;
            var rowGroup = buttonRow.GetComponent<HorizontalLayoutGroup>();
            rowGroup.childControlWidth = false; // Don't stretch children
            rowGroup.childForceExpandWidth = false;
            rowGroup.childAlignment = TextAnchor.MiddleCenter;
            
            // Register link
            var registerBtn = UIFactory.CreateSecondaryButton(buttonRow.transform, "RegisterLink", "CREATE ACCOUNT", 
                ShowRegisterPanel, 140, 28);
        }
        
        private void BuildRegisterPanel(Transform parent)
        {
            var theme = Theme.Current;
            
            // Compact fixed size panel, centered
            registerPanel = UIFactory.CreatePanel(parent, "RegisterPanel", theme.panelColor);
            var panelRect = registerPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(320, 320); // Compact size
            
            var layout = registerPanel.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 4;
            layout.padding = new RectOffset(15, 15, 10, 10);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            
            // Title
            var title = UIFactory.CreateTitle(registerPanel.transform, "Title", "CREATE ACCOUNT", 22f);
            var titleLayout = title.gameObject.AddComponent<LayoutElement>();
            titleLayout.preferredHeight = 30;
            titleLayout.minWidth = 280;
            
            // Username
            regUsernameInput = UIFactory.CreateInputField(registerPanel.transform, "RegUsername", "Username", 280, 34);
            var usernameLayout = regUsernameInput.gameObject.AddComponent<LayoutElement>();
            usernameLayout.preferredHeight = 34;
            usernameLayout.minWidth = 280;
            
            // Email
            emailInput = UIFactory.CreateInputField(registerPanel.transform, "RegEmail", "Email (optional)", 280, 34,
                TMP_InputField.ContentType.EmailAddress);
            var emailLayout = emailInput.gameObject.AddComponent<LayoutElement>();
            emailLayout.preferredHeight = 34;
            emailLayout.minWidth = 280;
            
            // Password
            regPasswordInput = UIFactory.CreateInputField(registerPanel.transform, "RegPassword", "Password", 280, 34,
                TMP_InputField.ContentType.Password);
            var passLayout = regPasswordInput.gameObject.AddComponent<LayoutElement>();
            passLayout.preferredHeight = 34;
            passLayout.minWidth = 280;
            
            // Confirm Password
            var regConfirm = UIFactory.CreateInputField(registerPanel.transform, "RegConfirm", "Confirm Password", 280, 34,
                TMP_InputField.ContentType.Password);
            var confirmLayout = regConfirm.gameObject.AddComponent<LayoutElement>();
            confirmLayout.preferredHeight = 34;
            confirmLayout.minWidth = 280;
            
            // Error text
            regErrorText = UIFactory.CreateText(registerPanel.transform, "RegError", "", 10f, theme.textDanger);
            var errorLayout = regErrorText.gameObject.AddComponent<LayoutElement>();
            errorLayout.preferredHeight = 14;
            errorLayout.minWidth = 280;
            
            // Register button
            var registerBtn = UIFactory.CreatePrimaryButton(registerPanel.transform, "RegisterBtn", "CREATE", 
                OnRegisterClick, 280, 36);
            var regBtnLayout = registerBtn.gameObject.AddComponent<LayoutElement>();
            regBtnLayout.preferredHeight = 36;
            regBtnLayout.minWidth = 280;
            
            // Back button
            var backBtn = UIFactory.CreateSecondaryButton(registerPanel.transform, "BackBtn", "BACK",
                ShowLoginPanel, 100, 26);
            var backLayout = backBtn.gameObject.AddComponent<LayoutElement>();
            backLayout.preferredHeight = 26;
            backLayout.minWidth = 100;
            
            registerPanel.SetActive(false);
        }
        
        private void BuildMainPanel(Transform parent)
        {
            var theme = Theme.Current;
            
            mainPanel = UIFactory.CreatePanel(parent, "MainPanel", Color.clear);
            var panelRect = mainPanel.GetComponent<RectTransform>();
            UIFactory.FillParent(panelRect, 40);
            
            // === TOP BAR (Player Info) ===
            var topBar = UIFactory.CreatePanel(mainPanel.transform, "TopBar", theme.panelColor);
            var topRect = topBar.GetComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0, 1);
            topRect.anchorMax = new Vector2(1, 1);
            topRect.pivot = new Vector2(0.5f, 1);
            topRect.anchoredPosition = Vector2.zero;
            topRect.sizeDelta = new Vector2(0, 80);
            
            // Player avatar
            var avatar = UIFactory.CreateAvatar(topBar.transform, "Avatar", 60);
            var avatarRect = avatar.GetComponent<RectTransform>();
            avatarRect.anchorMin = new Vector2(0, 0.5f);
            avatarRect.anchorMax = new Vector2(0, 0.5f);
            avatarRect.anchoredPosition = new Vector2(50, 0);
            
            // Player name
            playerNameText = UIFactory.CreateText(topBar.transform, "PlayerName", "Player", 20f, 
                theme.textPrimary, TextAlignmentOptions.Left);
            playerNameText.fontStyle = FontStyles.Bold;
            var nameRect = playerNameText.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0.5f);
            nameRect.anchorMax = new Vector2(0, 0.5f);
            nameRect.pivot = new Vector2(0, 0.5f);
            nameRect.anchoredPosition = new Vector2(95, 12);
            nameRect.sizeDelta = new Vector2(200, 30);
            
            // Level and XP
            playerLevelText = UIFactory.CreateText(topBar.transform, "PlayerLevel", "Level 1", 14f,
                theme.textSecondary, TextAlignmentOptions.Left);
            var levelRect = playerLevelText.GetComponent<RectTransform>();
            levelRect.anchorMin = new Vector2(0, 0.5f);
            levelRect.anchorMax = new Vector2(0, 0.5f);
            levelRect.pivot = new Vector2(0, 0.5f);
            levelRect.anchoredPosition = new Vector2(95, -12);
            levelRect.sizeDelta = new Vector2(200, 20);
            
            // XP bar
            var (xpContainer, xpFill) = UIFactory.CreateProgressBar(topBar.transform, "XPBar", 150, 8, theme.primaryColor);
            xpProgressBar = xpFill;
            var xpRect = xpContainer.GetComponent<RectTransform>();
            xpRect.anchorMin = new Vector2(0, 0.5f);
            xpRect.anchorMax = new Vector2(0, 0.5f);
            xpRect.anchoredPosition = new Vector2(170, -25);
            
            // Chips display
            playerChipsText = UIFactory.CreateText(topBar.transform, "Chips", "10,000", 22f,
                theme.secondaryColor, TextAlignmentOptions.Right);
            playerChipsText.fontStyle = FontStyles.Bold;
            var chipsRect = playerChipsText.GetComponent<RectTransform>();
            chipsRect.anchorMin = new Vector2(1, 0.5f);
            chipsRect.anchorMax = new Vector2(1, 0.5f);
            chipsRect.pivot = new Vector2(1, 0.5f);
            chipsRect.anchoredPosition = new Vector2(-30, 0);
            chipsRect.sizeDelta = new Vector2(200, 40);
            
            // === CENTER CONTENT (Mode Selection) ===
            var centerContent = UIFactory.CreateVerticalGroup(mainPanel.transform, "CenterContent", 30);
            var centerRect = centerContent.GetComponent<RectTransform>();
            UIFactory.Center(centerRect, new Vector2(600, 400));
            
            // Game title
            var gameTitle = UIFactory.CreateTitle(centerContent.transform, "GameTitle", "POKER", 72f);
            gameTitle.color = theme.secondaryColor;
            var gameTitleRect = gameTitle.GetComponent<RectTransform>();
            gameTitleRect.sizeDelta = new Vector2(400, 90);
            
            // Mode buttons container
            var modesContainer = UIFactory.CreateHorizontalGroup(centerContent.transform, "Modes", 40);
            var modesRect = modesContainer.GetComponent<RectTransform>();
            modesRect.sizeDelta = new Vector2(600, 200);
            
            // Adventure Mode button
            var adventurePanel = CreateModeButton(modesContainer.transform, "Adventure", 
                "ADVENTURE", "Battle bosses\nEarn XP & Items", theme.primaryColor, OnAdventureClick);
            
            // Multiplayer Mode button  
            var multiplayerPanel = CreateModeButton(modesContainer.transform, "Multiplayer",
                "MULTIPLAYER", "Play with friends\nTournaments", theme.accentColor, OnMultiplayerClick);
            
            // === BOTTOM BAR (Quick Actions) ===
            var bottomBar = UIFactory.CreateHorizontalGroup(mainPanel.transform, "BottomBar", 20);
            var bottomRect = bottomBar.GetComponent<RectTransform>();
            bottomRect.anchorMin = new Vector2(0.5f, 0);
            bottomRect.anchorMax = new Vector2(0.5f, 0);
            bottomRect.pivot = new Vector2(0.5f, 0);
            bottomRect.anchoredPosition = new Vector2(0, 20);
            bottomRect.sizeDelta = new Vector2(500, 50);
            
            UIFactory.CreateSecondaryButton(bottomBar.transform, "ShopBtn", "SHOP", OnShopClick, 100, 45);
            UIFactory.CreateSecondaryButton(bottomBar.transform, "InventoryBtn", "INVENTORY", OnInventoryClick, 120, 45);
            UIFactory.CreateSecondaryButton(bottomBar.transform, "FriendsBtn", "FRIENDS", OnFriendsClick, 120, 45);
            UIFactory.CreateSecondaryButton(bottomBar.transform, "SettingsBtn", "SETTINGS", OnSettingsClick, 120, 45);
            
            mainPanel.SetActive(false);
        }
        
        private GameObject CreateModeButton(Transform parent, string name, string title, string description, 
            Color color, UnityEngine.Events.UnityAction onClick)
        {
            var theme = Theme.Current;
            
            var panel = UIFactory.CreatePanel(parent, name, theme.cardPanelColor);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.sizeDelta = new Vector2(250, 180);
            
            // Add button component
            var button = panel.AddComponent<Button>();
            button.onClick.AddListener(onClick);
            
            var colors = button.colors;
            colors.normalColor = theme.cardPanelColor;
            colors.highlightedColor = color * 0.3f + theme.cardPanelColor * 0.7f;
            colors.pressedColor = color * 0.5f + theme.cardPanelColor * 0.5f;
            button.colors = colors;
            
            // Color accent bar at top
            var accentBar = UIFactory.CreatePanel(panel.transform, "Accent", color);
            var accentRect = accentBar.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0, 1);
            accentRect.anchorMax = new Vector2(1, 1);
            accentRect.pivot = new Vector2(0.5f, 1);
            accentRect.anchoredPosition = Vector2.zero;
            accentRect.sizeDelta = new Vector2(0, 6);
            
            // Title
            var titleText = UIFactory.CreateTitle(panel.transform, "Title", title, 24f);
            titleText.color = color;
            var titleRect = titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.65f);
            titleRect.anchorMax = new Vector2(0.5f, 0.65f);
            titleRect.sizeDelta = new Vector2(220, 40);
            
            // Description
            var descText = UIFactory.CreateText(panel.transform, "Description", description, 14f, theme.textSecondary);
            var descRect = descText.GetComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0.5f, 0.3f);
            descRect.anchorMax = new Vector2(0.5f, 0.3f);
            descRect.sizeDelta = new Vector2(220, 50);
            
            return panel;
        }
        
        #region Panel Navigation
        
        public void ShowLoginPanel()
        {
            loginPanel?.SetActive(true);
            registerPanel?.SetActive(false);
            mainPanel?.SetActive(false);
            ClearError();
        }
        
        public void ShowRegisterPanel()
        {
            loginPanel?.SetActive(false);
            registerPanel?.SetActive(true);
            mainPanel?.SetActive(false);
        }
        
        public void ShowMainMenu()
        {
            loginPanel?.SetActive(false);
            registerPanel?.SetActive(false);
            mainPanel?.SetActive(true);
        }
        
        #endregion
        
        #region Button Handlers
        
        private void OnLoginClick()
        {
            string username = usernameInput?.text;
            string password = passwordInput?.text;
            
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowError("Please enter username and password");
                return;
            }
            
            ClearError();
            ShowLoading("Logging in...");
            Debug.Log($"[MainMenu] Attempting login for: {username}");
            
            // Start a timeout coroutine to prevent infinite loading
            StartCoroutine(LoginTimeout());
            
            _gameService.Login(username, password, (success, error) =>
            {
                Debug.Log($"[MainMenu] Login callback received - success: {success}, error: {error}");
                _loginCallbackReceived = true;
                HideLoading();
                if (success)
                {
                    Debug.Log("[MainMenu] Login successful, saving credentials");
                    // Save credentials for next time
                    SaveCredentials(username, password);
                }
                else
                {
                    Debug.Log($"[MainMenu] Login failed: {error}");
                    ShowError(error ?? "Login failed");
                }
            });
        }
        
        private bool _loginCallbackReceived = false;
        
        private System.Collections.IEnumerator LoginTimeout()
        {
            _loginCallbackReceived = false;
            yield return new WaitForSeconds(10f); // 10 second timeout
            
            if (!_loginCallbackReceived && loadingPanel != null && loadingPanel.activeSelf)
            {
                Debug.LogWarning("[MainMenu] Login timeout - no response received");
                HideLoading();
                ShowError("Connection timeout. Check server address.");
            }
        }
        
        private void OnRegisterClick()
        {
            string username = regUsernameInput?.text ?? usernameInput?.text;
            string password = regPasswordInput?.text ?? passwordInput?.text;
            string email = emailInput?.text ?? "";
            
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ShowError("Please enter username and password");
                return;
            }
            
            ClearError();
            ShowLoading("Creating account...");
            
            _gameService.Register(username, password, email, (success, error) =>
            {
                HideLoading();
                if (success)
                {
                    Debug.Log("[MainMenu] Registration successful!");
                    // Show success message - auto-login will redirect to main menu via OnLoginSuccessHandler
                    ShowSuccess("Account created! Logging you in...");
                }
                else
                {
                    Debug.Log($"[MainMenu] Registration failed: {error}");
                    ShowError(error ?? "Registration failed");
                }
            });
        }
        
        private void OnLoginSuccessHandler(UserProfile profile)
        {
            _isLoggedIn = true;
            HideLoading();
            
            int xp = profile.adventureProgress?.xp ?? 0;
            int xpNext = profile.adventureProgress?.xpToNextLevel ?? 100;
            float xpProgress = xpNext > 0 ? (float)xp / xpNext : 0;
            
            UpdatePlayerInfo(
                profile.username, 
                (int)profile.chips, 
                profile.adventureProgress?.level ?? 1, 
                xpProgress
            );
            
            ShowMainMenu();
        }
        
        private void OnLoginFailedHandler(string error)
        {
            HideLoading();
            ShowError(error ?? "Login failed");
        }
        
        private void OnAdventureClick()
        {
            Debug.Log("Adventure mode selected");
            SceneManager.LoadScene("AdventureScene");
        }
        
        private void OnMultiplayerClick()
        {
            Debug.Log("Multiplayer mode selected");
            SceneManager.LoadScene("LobbyScene");
        }
        
        private void OnInventoryClick()
        {
            Debug.Log("Inventory clicked");
            // TODO: Show inventory popup
        }
        
        private void OnFriendsClick()
        {
            Debug.Log("Friends clicked");
            // TODO: Show friends popup
        }
        
        private void OnSettingsClick()
        {
            Debug.Log("Settings clicked");
            SettingsScene.OpenSettings("MainMenuScene");
        }
        
        private void OnShopClick()
        {
            Debug.Log("Shop clicked");
            SceneManager.LoadScene("ShopScene");
        }
        
        #endregion
        
        #region UI Updates
        
        public void ShowError(string message)
        {
            // Show error on the currently active panel
            if (registerPanel != null && registerPanel.activeSelf && regErrorText != null)
            {
                regErrorText.text = message;
                regErrorText.color = Theme.Current.textDanger;
            }
            else if (errorText != null)
            {
                errorText.text = message;
                errorText.color = Theme.Current.textDanger;
            }
        }
        
        public void ShowSuccess(string message)
        {
            // Show success message on the currently active panel
            if (registerPanel != null && registerPanel.activeSelf && regErrorText != null)
            {
                regErrorText.text = message;
                regErrorText.color = Theme.Current.textSuccess;
            }
            else if (errorText != null)
            {
                errorText.text = message;
                errorText.color = Theme.Current.textSuccess;
            }
        }
        
        public void ClearError()
        {
            if (errorText != null)
                errorText.text = "";
            if (regErrorText != null)
                regErrorText.text = "";
        }
        
        public void ShowLoading(string message = "Loading...")
        {
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(true);
                if (loadingText != null)
                    loadingText.text = message;
            }
        }
        
        public void HideLoading()
        {
            if (loadingPanel != null)
                loadingPanel.SetActive(false);
        }
        
        public void UpdatePlayerInfo(string name, int chips, int level, float xpProgress)
        {
            if (playerNameText != null)
                playerNameText.text = name;
            
            if (playerChipsText != null)
                playerChipsText.text = ChipStack.FormatChipValue(chips);
            
            if (playerLevelText != null)
                playerLevelText.text = $"Level {level}";
            
            if (xpProgressBar != null)
            {
                var rect = xpProgressBar.GetComponent<RectTransform>();
                rect.anchorMax = new Vector2(Mathf.Clamp01(xpProgress), 1);
            }
        }
        
        #endregion
        
        #region Credential Storage
        
        private const string PREF_USERNAME = "SavedUsername";
        private const string PREF_PASSWORD = "SavedPassword";
        
        private void LoadSavedCredentials()
        {
            string savedUsername = PlayerPrefs.GetString(PREF_USERNAME, "");
            string savedPassword = PlayerPrefs.GetString(PREF_PASSWORD, "");
            
            if (!string.IsNullOrEmpty(savedUsername) && usernameInput != null)
            {
                usernameInput.text = savedUsername;
            }
            
            if (!string.IsNullOrEmpty(savedPassword) && passwordInput != null)
            {
                passwordInput.text = savedPassword;
            }
            
            if (!string.IsNullOrEmpty(savedUsername))
            {
            }
        }
        
        private void SaveCredentials(string username, string password)
        {
            PlayerPrefs.SetString(PREF_USERNAME, username);
            PlayerPrefs.SetString(PREF_PASSWORD, password);
            PlayerPrefs.Save();
        }
        
        /// <summary>
        /// Call this to clear saved credentials (e.g., on logout)
        /// </summary>
        public void ClearSavedCredentials()
        {
            PlayerPrefs.DeleteKey(PREF_USERNAME);
            PlayerPrefs.DeleteKey(PREF_PASSWORD);
            PlayerPrefs.Save();
        }
        
        #endregion
    }
}

