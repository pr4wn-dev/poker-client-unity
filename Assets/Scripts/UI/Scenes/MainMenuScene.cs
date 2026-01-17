using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PokerClient.UI.Components;

namespace PokerClient.UI.Scenes
{
    /// <summary>
    /// Main Menu scene - Entry point of the game.
    /// Builds the entire menu UI programmatically.
    /// </summary>
    public class MainMenuScene : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private Canvas canvas;
        
        [Header("Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject loginPanel;
        [SerializeField] private GameObject registerPanel;
        
        [Header("Login Components")]
        [SerializeField] private TMP_InputField usernameInput;
        [SerializeField] private TMP_InputField passwordInput;
        [SerializeField] private TextMeshProUGUI errorText;
        
        [Header("Player Info")]
        [SerializeField] private TextMeshProUGUI playerNameText;
        [SerializeField] private TextMeshProUGUI playerChipsText;
        [SerializeField] private TextMeshProUGUI playerLevelText;
        [SerializeField] private Image xpProgressBar;
        
        private bool _isLoggedIn = false;
        
        private void Start()
        {
            BuildScene();
            ShowLoginPanel();
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
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
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
        }
        
        private void BuildLoginPanel(Transform parent)
        {
            var theme = Theme.Current;
            
            loginPanel = UIFactory.CreatePanel(parent, "LoginPanel", theme.panelColor);
            var panelRect = loginPanel.GetComponent<RectTransform>();
            UIFactory.Center(panelRect, new Vector2(400, 450));
            
            var layout = loginPanel.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 15;
            layout.padding = new RectOffset(30, 30, 30, 30);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            
            // Title
            var title = UIFactory.CreateTitle(loginPanel.transform, "Title", "POKER GAME", 42f);
            title.color = theme.secondaryColor;
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(340, 60);
            
            // Subtitle
            var subtitle = UIFactory.CreateText(loginPanel.transform, "Subtitle", "Login to Play", 18f, theme.textSecondary);
            var subRect = subtitle.GetComponent<RectTransform>();
            subRect.sizeDelta = new Vector2(340, 30);
            
            // Spacer
            var spacer = new GameObject("Spacer", typeof(RectTransform));
            spacer.transform.SetParent(loginPanel.transform, false);
            spacer.GetComponent<RectTransform>().sizeDelta = new Vector2(340, 20);
            
            // Username input
            usernameInput = UIFactory.CreateInputField(loginPanel.transform, "Username", "Username", 340, 50);
            
            // Password input
            passwordInput = UIFactory.CreateInputField(loginPanel.transform, "Password", "Password", 340, 50, 
                TMP_InputField.ContentType.Password);
            
            // Error text
            errorText = UIFactory.CreateText(loginPanel.transform, "Error", "", 14f, theme.textDanger);
            var errorRect = errorText.GetComponent<RectTransform>();
            errorRect.sizeDelta = new Vector2(340, 25);
            
            // Login button
            var loginBtn = UIFactory.CreatePrimaryButton(loginPanel.transform, "LoginBtn", "LOGIN", OnLoginClick, 340, 55);
            
            // Divider
            UIFactory.CreateDivider(loginPanel.transform, "Divider", true, 200, 1);
            
            // Register link
            var registerBtn = UIFactory.CreateSecondaryButton(loginPanel.transform, "RegisterLink", "CREATE ACCOUNT", 
                ShowRegisterPanel, 200, 40);
        }
        
        private void BuildRegisterPanel(Transform parent)
        {
            var theme = Theme.Current;
            
            registerPanel = UIFactory.CreatePanel(parent, "RegisterPanel", theme.panelColor);
            var panelRect = registerPanel.GetComponent<RectTransform>();
            UIFactory.Center(panelRect, new Vector2(400, 520));
            
            var layout = registerPanel.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12;
            layout.padding = new RectOffset(30, 30, 25, 25);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            
            // Title
            var title = UIFactory.CreateTitle(registerPanel.transform, "Title", "CREATE ACCOUNT", 32f);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(340, 50);
            
            // Username
            var regUsername = UIFactory.CreateInputField(registerPanel.transform, "RegUsername", "Username", 340, 50);
            
            // Email
            var regEmail = UIFactory.CreateInputField(registerPanel.transform, "RegEmail", "Email (optional)", 340, 50,
                TMP_InputField.ContentType.EmailAddress);
            
            // Password
            var regPassword = UIFactory.CreateInputField(registerPanel.transform, "RegPassword", "Password", 340, 50,
                TMP_InputField.ContentType.Password);
            
            // Confirm Password
            var regConfirm = UIFactory.CreateInputField(registerPanel.transform, "RegConfirm", "Confirm Password", 340, 50,
                TMP_InputField.ContentType.Password);
            
            // Error text
            var regError = UIFactory.CreateText(registerPanel.transform, "RegError", "", 14f, theme.textDanger);
            var errorRect = regError.GetComponent<RectTransform>();
            errorRect.sizeDelta = new Vector2(340, 25);
            
            // Register button
            var registerBtn = UIFactory.CreatePrimaryButton(registerPanel.transform, "RegisterBtn", "CREATE ACCOUNT", 
                OnRegisterClick, 340, 55);
            
            // Back button
            var backBtn = UIFactory.CreateSecondaryButton(registerPanel.transform, "BackBtn", "BACK TO LOGIN",
                ShowLoginPanel, 200, 40);
            
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
            
            // TODO: Call network manager to login
            Debug.Log($"Login attempt: {username}");
            
            // For now, simulate successful login
            _isLoggedIn = true;
            UpdatePlayerInfo("Player", 10000, 1, 0);
            ShowMainMenu();
        }
        
        private void OnRegisterClick()
        {
            // TODO: Implement registration
            Debug.Log("Register clicked");
        }
        
        private void OnAdventureClick()
        {
            Debug.Log("Adventure mode selected");
            // TODO: Load Adventure scene
            // SceneManager.LoadScene("Adventure");
        }
        
        private void OnMultiplayerClick()
        {
            Debug.Log("Multiplayer mode selected");
            // TODO: Load Lobby scene
            // SceneManager.LoadScene("Lobby");
        }
        
        private void OnInventoryClick()
        {
            Debug.Log("Inventory clicked");
        }
        
        private void OnFriendsClick()
        {
            Debug.Log("Friends clicked");
        }
        
        private void OnSettingsClick()
        {
            Debug.Log("Settings clicked");
        }
        
        #endregion
        
        #region UI Updates
        
        public void ShowError(string message)
        {
            if (errorText != null)
                errorText.text = message;
        }
        
        public void ClearError()
        {
            if (errorText != null)
                errorText.text = "";
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
    }
}

