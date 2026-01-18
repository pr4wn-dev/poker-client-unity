using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using PokerClient.Core;
using PokerClient.UI;
using PokerClient.UI.Components;
using PokerClient.Networking;
using System.Collections.Generic;

namespace PokerClient.UI.Scenes
{
    /// <summary>
    /// Multiplayer Lobby - Browse, create, and join tables.
    /// Also handles inviting friends by username/tag.
    /// </summary>
    public class LobbyScene : MonoBehaviour
    {
        [Header("UI Panels")]
        private GameObject tableListPanel;
        private GameObject createTablePanel;
        private GameObject invitePanel;
        private GameObject loadingPanel;
        
        [Header("Table List")]
        private Transform tableListContainer;
        private List<GameObject> tableListItems = new List<GameObject>();
        
        [Header("Create Table Form")]
        private TMP_InputField tableNameInput;
        private TMP_InputField passwordInput;
        private Slider maxPlayersSlider;
        private Slider smallBlindSlider;
        private Slider buyInSlider;
        private Slider turnTimeSlider;
        private Slider roundTimerSlider;
        private Toggle privateToggle;
        private Toggle practiceModeToggle;
        private TextMeshProUGUI maxPlayersValue;
        private TextMeshProUGUI blindsValue;
        private TextMeshProUGUI buyInValue;
        private TextMeshProUGUI turnTimeValue;
        private TextMeshProUGUI roundTimerValue;
        
        [Header("Invite Form")]
        private TMP_InputField inviteSearchInput;
        private Transform searchResultsContainer;
        
        [Header("Status")]
        private TextMeshProUGUI statusText;
        private TextMeshProUGUI playerInfoText;
        
        private GameService _gameService;
        private Canvas _canvas;
        
        private void Start()
        {
            _gameService = GameService.Instance;
            if (_gameService == null)
            {
                Debug.LogError("GameService not found! Going back to main menu.");
                SceneManager.LoadScene("MainMenuScene");
                return;
            }
            
            _gameService.OnTablesReceived += OnTablesReceived;
            _gameService.OnTableJoined += OnTableJoined;
            _gameService.OnInviteReceived += OnInviteReceived;
            
            BuildScene();
            RefreshTableList();
            
            // Play lobby music
            AudioManager.Instance?.PlayLobbyMusic();
        }
        
        private void OnDestroy()
        {
            if (_gameService != null)
            {
                _gameService.OnTablesReceived -= OnTablesReceived;
                _gameService.OnTableJoined -= OnTableJoined;
                _gameService.OnInviteReceived -= OnInviteReceived;
            }
        }
        
        private void BuildScene()
        {
            // Get or create canvas
            _canvas = FindObjectOfType<Canvas>();
            if (_canvas == null)
            {
                var canvasObj = new GameObject("Canvas");
                _canvas = canvasObj.AddComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1080, 1920); // Mobile-friendly
                scaler.matchWidthOrHeight = 0f;
                canvasObj.AddComponent<GraphicRaycaster>();
            }
            
            var theme = Theme.Current;
            
            // Background
            var bg = UIFactory.CreatePanel(_canvas.transform, "Background", theme.backgroundColor);
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            
            // Main Content Area (build BEFORE header so header renders on top)
            BuildTableListPanel();
            BuildCreateTablePanel();
            BuildInvitePanel();
            BuildLoadingPanel();
            
            // Header (build LAST so it renders on top of content)
            BuildHeader();
            
            // Show table list by default
            ShowTableListPanel();
        }
        
        private void BuildHeader()
        {
            var theme = Theme.Current;
            
            // Taller header to fit button row inside
            var header = UIFactory.CreatePanel(_canvas.transform, "Header", theme.cardPanelColor);
            var headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.sizeDelta = new Vector2(0, 120); // Taller to fit buttons
            headerRect.anchoredPosition = Vector2.zero;
            
            // Back button - in upper left
            var backBtn = UIFactory.CreateButton(header.transform, "BackBtn", "← Back", OnBackClick);
            var backRect = backBtn.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0, 1);
            backRect.anchorMax = new Vector2(0, 1);
            backRect.pivot = new Vector2(0, 1);
            backRect.anchoredPosition = new Vector2(10, -8);
            backRect.sizeDelta = new Vector2(90, 40);
            
            // Title - in upper center
            var title = UIFactory.CreateTitle(header.transform, "Title", "MULTIPLAYER LOBBY", 22f);
            title.alignment = TextAlignmentOptions.Center;
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.25f, 0.55f);
            titleRect.anchorMax = new Vector2(0.75f, 1);
            titleRect.sizeDelta = Vector2.zero;
            
            // Player Info - in upper right
            playerInfoText = UIFactory.CreateText(header.transform, "PlayerInfo", "", 14f, theme.textSecondary);
            var infoRect = playerInfoText.GetComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(1, 1);
            infoRect.anchorMax = new Vector2(1, 1);
            infoRect.pivot = new Vector2(1, 1);
            infoRect.anchoredPosition = new Vector2(-10, -12);
            infoRect.sizeDelta = new Vector2(200, 30);
            playerInfoText.alignment = TextAlignmentOptions.Right;
            
            if (_gameService?.CurrentUser != null)
            {
                playerInfoText.text = $"{_gameService.CurrentUser.username} | {ChipStack.FormatChipValue((int)_gameService.CurrentUser.chips)}";
            }
            
            // Button row - in lower half of header
            var buttonRow = UIFactory.CreatePanel(header.transform, "ButtonRow", Color.clear);
            var rowRect = buttonRow.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0, 0);
            rowRect.anchorMax = new Vector2(1, 0.55f);
            rowRect.sizeDelta = Vector2.zero;
            
            var hlg = buttonRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.padding = new RectOffset(15, 15, 0, 5);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childForceExpandWidth = false;
            
            // Visible buttons with proper sizing
            var tablesBtn = UIFactory.CreateButton(buttonRow.transform, "TablesBtn", "BROWSE", () => ShowTableListPanel());
            tablesBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 45);
            
            var createBtn = UIFactory.CreateButton(buttonRow.transform, "CreateBtn", "CREATE", () => ShowCreateTablePanel());
            createBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 45);
            createBtn.GetComponent<Image>().color = theme.primaryColor;
            
            var refreshBtn = UIFactory.CreateButton(buttonRow.transform, "RefreshBtn", "REFRESH", RefreshTableList);
            refreshBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 45);
        }
        
        private void OnTournamentsClick()
        {
            SceneManager.LoadScene("TournamentScene");
        }
        
        private void BuildTableListPanel()
        {
            var theme = Theme.Current;
            
            // Position below header (header is 120px, ~6% of 1920)
            tableListPanel = UIFactory.CreatePanel(_canvas.transform, "TableListPanel", theme.backgroundColor);
            var panelRect = tableListPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.02f, 0.05f);
            panelRect.anchorMax = new Vector2(0.98f, 0.92f); // Leave room for 120px header at top
            panelRect.sizeDelta = Vector2.zero;
            
            // Content container - simple vertical list, positioned below header
            var content = new GameObject("Content");
            content.transform.SetParent(tableListPanel.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(0, 0);
            contentRect.offsetMax = new Vector2(0, -130); // Leave room for header
            
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10;
            vlg.padding = new RectOffset(20, 20, 20, 20);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = false;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            
            tableListContainer = content.transform;
            
            // Status text - positioned in center, only visible when no tables
            statusText = UIFactory.CreateText(tableListPanel.transform, "Status", "Loading tables...", 24f, theme.textSecondary);
            statusText.alignment = TextAlignmentOptions.Center;
            var statusRect = statusText.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.1f, 0.4f);
            statusRect.anchorMax = new Vector2(0.9f, 0.6f);
            statusRect.sizeDelta = Vector2.zero;
            
            // Make sure status doesn't block interactions
            statusText.raycastTarget = false;
        }
        
        private void BuildCreateTablePanel()
        {
            var theme = Theme.Current;
            
            createTablePanel = UIFactory.CreatePanel(_canvas.transform, "CreateTablePanel", theme.panelColor);
            var panelRect = createTablePanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(320, 435); // Taller for turn time + round timer sliders
            panelRect.anchoredPosition = Vector2.zero;
            
            // Ensure this panel renders on top of header
            var panelCanvas = createTablePanel.AddComponent<Canvas>();
            panelCanvas.overrideSorting = true;
            panelCanvas.sortingOrder = 50;
            createTablePanel.AddComponent<GraphicRaycaster>();
            
            float y = -20; // Start from top with padding
            float contentWidth = 280; // 320 - 40 padding
            float leftPad = 20;
            
            // Title
            var title = UIFactory.CreateTitle(createTablePanel.transform, "Title", "CREATE TABLE", 20f);
            title.color = theme.secondaryColor;
            title.alignment = TextAlignmentOptions.Center;
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 1);
            titleRect.anchorMax = new Vector2(1, 1);
            titleRect.pivot = new Vector2(0.5f, 1);
            titleRect.anchoredPosition = new Vector2(0, y);
            titleRect.sizeDelta = new Vector2(0, 25);
            y -= 30;
            
            // Table Name Input
            tableNameInput = UIFactory.CreateInputField(createTablePanel.transform, "TableName", "Table name...", contentWidth, 32);
            var inputRect = tableNameInput.GetComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0, 1);
            inputRect.anchorMax = new Vector2(0, 1);
            inputRect.pivot = new Vector2(0, 1);
            inputRect.anchoredPosition = new Vector2(leftPad, y);
            inputRect.sizeDelta = new Vector2(contentWidth, 32);
            y -= 40;
            
            // Max Players Row
            var playersLabel = UIFactory.CreateText(createTablePanel.transform, "PlayersLabel", "Players:", 12f, theme.textSecondary);
            var plRect = playersLabel.GetComponent<RectTransform>();
            plRect.anchorMin = new Vector2(0, 1);
            plRect.anchorMax = new Vector2(0, 1);
            plRect.pivot = new Vector2(0, 0.5f);
            plRect.anchoredPosition = new Vector2(leftPad, y - 12);
            plRect.sizeDelta = new Vector2(50, 24);
            
            maxPlayersSlider = CreateSlider(createTablePanel.transform, 2, 9, 6);
            var sliderRect = maxPlayersSlider.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0, 1);
            sliderRect.anchorMax = new Vector2(0, 1);
            sliderRect.pivot = new Vector2(0, 0.5f);
            sliderRect.anchoredPosition = new Vector2(leftPad + 55, y - 12);
            sliderRect.sizeDelta = new Vector2(140, 20);
            
            maxPlayersValue = UIFactory.CreateText(createTablePanel.transform, "PlayersValue", "6", 14f, theme.primaryColor);
            maxPlayersValue.fontStyle = FontStyles.Bold;
            var pvRect = maxPlayersValue.GetComponent<RectTransform>();
            pvRect.anchorMin = new Vector2(0, 1);
            pvRect.anchorMax = new Vector2(0, 1);
            pvRect.pivot = new Vector2(0, 0.5f);
            pvRect.anchoredPosition = new Vector2(leftPad + 200, y - 12);
            pvRect.sizeDelta = new Vector2(60, 24);
            maxPlayersSlider.onValueChanged.AddListener(v => maxPlayersValue.text = ((int)v).ToString());
            y -= 32;
            
            // Blinds Row
            var blindsLabel = UIFactory.CreateText(createTablePanel.transform, "BlindsLabel", "Blinds:", 12f, theme.textSecondary);
            var blRect = blindsLabel.GetComponent<RectTransform>();
            blRect.anchorMin = new Vector2(0, 1);
            blRect.anchorMax = new Vector2(0, 1);
            blRect.pivot = new Vector2(0, 0.5f);
            blRect.anchoredPosition = new Vector2(leftPad, y - 12);
            blRect.sizeDelta = new Vector2(50, 24);
            
            smallBlindSlider = CreateSlider(createTablePanel.transform, 1, 6, 1);
            var bsRect = smallBlindSlider.GetComponent<RectTransform>();
            bsRect.anchorMin = new Vector2(0, 1);
            bsRect.anchorMax = new Vector2(0, 1);
            bsRect.pivot = new Vector2(0, 0.5f);
            bsRect.anchoredPosition = new Vector2(leftPad + 55, y - 12);
            bsRect.sizeDelta = new Vector2(140, 20);
            
            blindsValue = UIFactory.CreateText(createTablePanel.transform, "BlindsValue", "25/50", 14f, theme.primaryColor);
            blindsValue.fontStyle = FontStyles.Bold;
            var bvRect = blindsValue.GetComponent<RectTransform>();
            bvRect.anchorMin = new Vector2(0, 1);
            bvRect.anchorMax = new Vector2(0, 1);
            bvRect.pivot = new Vector2(0, 0.5f);
            bvRect.anchoredPosition = new Vector2(leftPad + 200, y - 12);
            bvRect.sizeDelta = new Vector2(60, 24);
            smallBlindSlider.onValueChanged.AddListener(UpdateBlindsDisplay);
            y -= 32;
            
            // Buy-In Row
            var buyInLabel = UIFactory.CreateText(createTablePanel.transform, "BuyInLabel", "Buy-In:", 12f, theme.textSecondary);
            var biLRect = buyInLabel.GetComponent<RectTransform>();
            biLRect.anchorMin = new Vector2(0, 1);
            biLRect.anchorMax = new Vector2(0, 1);
            biLRect.pivot = new Vector2(0, 0.5f);
            biLRect.anchoredPosition = new Vector2(leftPad, y - 12);
            biLRect.sizeDelta = new Vector2(50, 24);
            
            buyInSlider = CreateSlider(createTablePanel.transform, 1, 8, 5); // 1=1M, 5=20M, 8=100M
            var biSRect = buyInSlider.GetComponent<RectTransform>();
            biSRect.anchorMin = new Vector2(0, 1);
            biSRect.anchorMax = new Vector2(0, 1);
            biSRect.pivot = new Vector2(0, 0.5f);
            biSRect.anchoredPosition = new Vector2(leftPad + 55, y - 12);
            biSRect.sizeDelta = new Vector2(140, 20);
            
            buyInValue = UIFactory.CreateText(createTablePanel.transform, "BuyInValue", "20M", 14f, theme.primaryColor);
            buyInValue.fontStyle = FontStyles.Bold;
            var biVRect = buyInValue.GetComponent<RectTransform>();
            biVRect.anchorMin = new Vector2(0, 1);
            biVRect.anchorMax = new Vector2(0, 1);
            biVRect.pivot = new Vector2(0, 0.5f);
            biVRect.anchoredPosition = new Vector2(leftPad + 200, y - 12);
            biVRect.sizeDelta = new Vector2(60, 24);
            buyInSlider.onValueChanged.AddListener(UpdateBuyInDisplay);
            y -= 32;
            
            // Private Row
            var privateLabel = UIFactory.CreateText(createTablePanel.transform, "PrivateLabel", "Private:", 12f, theme.textSecondary);
            var prRect = privateLabel.GetComponent<RectTransform>();
            prRect.anchorMin = new Vector2(0, 1);
            prRect.anchorMax = new Vector2(0, 1);
            prRect.pivot = new Vector2(0, 0.5f);
            prRect.anchoredPosition = new Vector2(leftPad, y - 12);
            prRect.sizeDelta = new Vector2(50, 24);
            
            privateToggle = CreateToggle(createTablePanel.transform);
            var toggleRect = privateToggle.GetComponent<RectTransform>();
            toggleRect.anchorMin = new Vector2(0, 1);
            toggleRect.anchorMax = new Vector2(0, 1);
            toggleRect.pivot = new Vector2(0, 0.5f);
            toggleRect.anchoredPosition = new Vector2(leftPad + 55, y - 12);
            toggleRect.sizeDelta = new Vector2(20, 20); // Small checkbox
            privateToggle.onValueChanged.AddListener(v => passwordInput.gameObject.SetActive(v));
            y -= 32;
            
            // Practice Mode Row (allows players without enough chips to play for fun)
            var practiceLabel = UIFactory.CreateText(createTablePanel.transform, "PracticeLabel", "Practice:", 12f, theme.textSecondary);
            var pmRect = practiceLabel.GetComponent<RectTransform>();
            pmRect.anchorMin = new Vector2(0, 1);
            pmRect.anchorMax = new Vector2(0, 1);
            pmRect.pivot = new Vector2(0, 0.5f);
            pmRect.anchoredPosition = new Vector2(leftPad, y - 12);
            pmRect.sizeDelta = new Vector2(50, 24);
            
            practiceModeToggle = CreateToggle(createTablePanel.transform);
            var pmToggleRect = practiceModeToggle.GetComponent<RectTransform>();
            pmToggleRect.anchorMin = new Vector2(0, 1);
            pmToggleRect.anchorMax = new Vector2(0, 1);
            pmToggleRect.pivot = new Vector2(0, 0.5f);
            pmToggleRect.anchoredPosition = new Vector2(leftPad + 55, y - 12);
            pmToggleRect.sizeDelta = new Vector2(20, 20);
            
            // Help text for practice mode
            var practiceHelp = UIFactory.CreateText(createTablePanel.transform, "PracticeHelp", "(no $ needed)", 10f, theme.textSecondary);
            var phRect = practiceHelp.GetComponent<RectTransform>();
            phRect.anchorMin = new Vector2(0, 1);
            phRect.anchorMax = new Vector2(0, 1);
            phRect.pivot = new Vector2(0, 0.5f);
            phRect.anchoredPosition = new Vector2(leftPad + 80, y - 12);
            phRect.sizeDelta = new Vector2(100, 24);
            y -= 32;
            
            // Turn Time Row
            var turnTimeLabel = UIFactory.CreateText(createTablePanel.transform, "TurnTimeLabel", "Turn Time:", 12f, theme.textSecondary);
            var ttlRect = turnTimeLabel.GetComponent<RectTransform>();
            ttlRect.anchorMin = new Vector2(0, 1);
            ttlRect.anchorMax = new Vector2(0, 1);
            ttlRect.pivot = new Vector2(0, 0.5f);
            ttlRect.anchoredPosition = new Vector2(leftPad, y - 12);
            ttlRect.sizeDelta = new Vector2(60, 24);
            
            turnTimeSlider = CreateSlider(createTablePanel.transform, 1, 6, 4); // 1=5s, 2=10s, 3=15s, 4=20s (default), 5=30s, 6=60s
            var ttsRect = turnTimeSlider.GetComponent<RectTransform>();
            ttsRect.anchorMin = new Vector2(0, 1);
            ttsRect.anchorMax = new Vector2(0, 1);
            ttsRect.pivot = new Vector2(0, 0.5f);
            ttsRect.anchoredPosition = new Vector2(leftPad + 70, y - 12);
            ttsRect.sizeDelta = new Vector2(125, 20);
            
            turnTimeValue = UIFactory.CreateText(createTablePanel.transform, "TurnTimeValue", "20s", 14f, theme.primaryColor);
            turnTimeValue.fontStyle = FontStyles.Bold;
            var ttvRect = turnTimeValue.GetComponent<RectTransform>();
            ttvRect.anchorMin = new Vector2(0, 1);
            ttvRect.anchorMax = new Vector2(0, 1);
            ttvRect.pivot = new Vector2(0, 0.5f);
            ttvRect.anchoredPosition = new Vector2(leftPad + 200, y - 12);
            ttvRect.sizeDelta = new Vector2(60, 24);
            turnTimeSlider.onValueChanged.AddListener(UpdateTurnTimeDisplay);
            y -= 32;
            
            // Round Timer Row (blind increase interval)
            var roundTimerLabel = UIFactory.CreateText(createTablePanel.transform, "RoundTimerLabel", "Round Timer:", 12f, theme.textSecondary);
            var rtlRect = roundTimerLabel.GetComponent<RectTransform>();
            rtlRect.anchorMin = new Vector2(0, 1);
            rtlRect.anchorMax = new Vector2(0, 1);
            rtlRect.pivot = new Vector2(0, 0.5f);
            rtlRect.anchoredPosition = new Vector2(leftPad, y - 12);
            rtlRect.sizeDelta = new Vector2(75, 24);
            
            roundTimerSlider = CreateSlider(createTablePanel.transform, 0, 6, 0); // 0=OFF, 1=5min, 2=10min, 3=15min, 4=20min (default), 5=30min, 6=60min
            var rtsRect = roundTimerSlider.GetComponent<RectTransform>();
            rtsRect.anchorMin = new Vector2(0, 1);
            rtsRect.anchorMax = new Vector2(0, 1);
            rtsRect.pivot = new Vector2(0, 0.5f);
            rtsRect.anchoredPosition = new Vector2(leftPad + 85, y - 12);
            rtsRect.sizeDelta = new Vector2(110, 20);
            
            roundTimerValue = UIFactory.CreateText(createTablePanel.transform, "RoundTimerValue", "OFF", 14f, theme.primaryColor);
            roundTimerValue.fontStyle = FontStyles.Bold;
            var rtvRect = roundTimerValue.GetComponent<RectTransform>();
            rtvRect.anchorMin = new Vector2(0, 1);
            rtvRect.anchorMax = new Vector2(0, 1);
            rtvRect.pivot = new Vector2(0, 0.5f);
            rtvRect.anchoredPosition = new Vector2(leftPad + 200, y - 12);
            rtvRect.sizeDelta = new Vector2(60, 24);
            roundTimerSlider.onValueChanged.AddListener(UpdateRoundTimerDisplay);
            y -= 32;
            
            // Password (hidden by default)
            passwordInput = UIFactory.CreateInputField(createTablePanel.transform, "Password", "Password", contentWidth, 32,
                TMP_InputField.ContentType.Password);
            var pwRect = passwordInput.GetComponent<RectTransform>();
            pwRect.anchorMin = new Vector2(0, 1);
            pwRect.anchorMax = new Vector2(0, 1);
            pwRect.pivot = new Vector2(0, 1);
            pwRect.anchoredPosition = new Vector2(leftPad, y);
            pwRect.sizeDelta = new Vector2(contentWidth, 32);
            passwordInput.gameObject.SetActive(false);
            
            // Buttons at bottom
            var cancelBtn = UIFactory.CreateButton(createTablePanel.transform, "Cancel", "CANCEL", () => ShowTableListPanel());
            var cancelRect = cancelBtn.GetComponent<RectTransform>();
            cancelRect.anchorMin = new Vector2(0.5f, 0);
            cancelRect.anchorMax = new Vector2(0.5f, 0);
            cancelRect.pivot = new Vector2(1, 0);
            cancelRect.anchoredPosition = new Vector2(-5, 15);
            cancelRect.sizeDelta = new Vector2(90, 35);
            
            var createBtn = UIFactory.CreateButton(createTablePanel.transform, "Create", "CREATE", OnCreateTableClick);
            var createRect = createBtn.GetComponent<RectTransform>();
            createRect.anchorMin = new Vector2(0.5f, 0);
            createRect.anchorMax = new Vector2(0.5f, 0);
            createRect.pivot = new Vector2(0, 0);
            createRect.anchoredPosition = new Vector2(5, 15);
            createRect.sizeDelta = new Vector2(110, 35);
            createBtn.GetComponent<Image>().color = theme.primaryColor;
            
            createTablePanel.SetActive(false);
        }
        
        private void BuildInvitePanel()
        {
            var theme = Theme.Current;
            
            invitePanel = UIFactory.CreatePanel(_canvas.transform, "InvitePanel", theme.cardPanelColor);
            var panelRect = invitePanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.3f, 0.3f);
            panelRect.anchorMax = new Vector2(0.7f, 0.7f);
            panelRect.sizeDelta = Vector2.zero;
            
            // Ensure this panel renders on top
            var panelCanvas = invitePanel.AddComponent<Canvas>();
            panelCanvas.overrideSorting = true;
            panelCanvas.sortingOrder = 50;
            invitePanel.AddComponent<GraphicRaycaster>();
            
            var vlg = invitePanel.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 15;
            vlg.padding = new RectOffset(30, 30, 30, 30);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = false;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            
            var title = UIFactory.CreateTitle(invitePanel.transform, "Title", "INVITE PLAYER", 28f);
            title.GetOrAddComponent<LayoutElement>().preferredHeight = 40;
            
            var searchRow = UIFactory.CreatePanel(invitePanel.transform, "SearchRow", Color.clear);
            searchRow.GetOrAddComponent<LayoutElement>().preferredHeight = 50;
            var hlg = searchRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.childControlWidth = true;
            hlg.childForceExpandWidth = true;
            
            inviteSearchInput = UIFactory.CreateInputField(searchRow.transform, "Search", "Enter username...");
            var searchBtn = UIFactory.CreateButton(searchRow.transform, "SearchBtn", "Search", OnSearchUsers);
            searchBtn.GetOrAddComponent<LayoutElement>().preferredWidth = 100;
            
            // Results container
            var resultsPanel = UIFactory.CreatePanel(invitePanel.transform, "Results", theme.backgroundColor);
            resultsPanel.GetOrAddComponent<LayoutElement>().preferredHeight = 150;
            searchResultsContainer = resultsPanel.transform;
            
            var closeBtn = UIFactory.CreateButton(invitePanel.transform, "Close", "Close", () => invitePanel.SetActive(false));
            closeBtn.GetOrAddComponent<LayoutElement>().preferredHeight = 45;
            
            invitePanel.SetActive(false);
        }
        
        private void BuildLoadingPanel()
        {
            var theme = Theme.Current;
            
            loadingPanel = UIFactory.CreatePanel(_canvas.transform, "LoadingPanel", new Color(0, 0, 0, 0.7f));
            var panelRect = loadingPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            
            // Ensure this panel renders on top of everything
            var panelCanvas = loadingPanel.AddComponent<Canvas>();
            panelCanvas.overrideSorting = true;
            panelCanvas.sortingOrder = 100;
            loadingPanel.AddComponent<GraphicRaycaster>();
            
            var loadingText = UIFactory.CreateTitle(loadingPanel.transform, "LoadingText", "Loading...", 36f);
            loadingText.alignment = TextAlignmentOptions.Center;
            
            loadingPanel.SetActive(false);
        }
        
        #region Panel Navigation
        
        private void ShowTableListPanel()
        {
            tableListPanel?.SetActive(true);
            createTablePanel?.SetActive(false);
            invitePanel?.SetActive(false);
        }
        
        private void ShowCreateTablePanel()
        {
            tableListPanel?.SetActive(false);
            createTablePanel?.SetActive(true);
            invitePanel?.SetActive(false);
        }
        
        public void ShowInvitePanel()
        {
            invitePanel?.SetActive(true);
        }
        
        #endregion
        
        #region Network Handlers
        
        private void RefreshTableList()
        {
            statusText.text = "Loading tables...";
            statusText.gameObject.SetActive(true);
            _gameService.GetTables();
        }
        
        private void OnTablesReceived(List<TableInfo> tables)
        {
            Debug.Log($"[LobbyScene] OnTablesReceived: {tables?.Count ?? 0} tables");
            
            // Clear existing items
            foreach (var item in tableListItems)
            {
                Destroy(item);
            }
            tableListItems.Clear();
            
            if (tables == null || tables.Count == 0)
            {
                statusText.text = "No tables found. Create one!";
                statusText.gameObject.SetActive(true);
                return;
            }
            
            statusText.gameObject.SetActive(false);
            
            foreach (var table in tables)
            {
                Debug.Log($"[LobbyScene] Creating item for table: {table?.name}, id: {table?.id}");
                var item = CreateTableListItem(table);
                if (item != null)
                    tableListItems.Add(item);
            }
            
            Debug.Log($"[LobbyScene] Created {tableListItems.Count} table items");
            
            // Force layout rebuild
            if (tableListContainer != null)
            {
                Canvas.ForceUpdateCanvases();
                UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(tableListContainer.GetComponent<RectTransform>());
            }
        }
        
        private GameObject CreateTableListItem(TableInfo table)
        {
            if (tableListContainer == null)
            {
                Debug.LogError("[LobbyScene] tableListContainer is null!");
                return null;
            }
            
            if (table == null)
            {
                Debug.LogError("[LobbyScene] table is null!");
                return null;
            }
            
            Debug.Log($"[LobbyScene] CreateTableListItem: name={table.name}, container={tableListContainer.name}");
            
            var theme = Theme.Current;
            
            var item = UIFactory.CreatePanel(tableListContainer, $"Table_{table.id}", theme.cardPanelColor);
            var itemRect = item.GetComponent<RectTransform>();
            itemRect.sizeDelta = new Vector2(0, 80);
            item.GetOrAddComponent<LayoutElement>().preferredHeight = 80;
            
            var hlg = item.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20;
            hlg.padding = new RectOffset(20, 20, 10, 10);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childForceExpandWidth = false;
            
            // Table Name
            var nameText = UIFactory.CreateTitle(item.transform, "Name", table.name, 24f);
            nameText.GetOrAddComponent<LayoutElement>().preferredWidth = 300;
            
            // Players
            var playersText = UIFactory.CreateText(item.transform, "Players", $"{table.playerCount}/{table.maxPlayers} Players", 18f, theme.textSecondary);
            playersText.GetOrAddComponent<LayoutElement>().preferredWidth = 150;
            
            // Blinds
            var blindsText = UIFactory.CreateText(item.transform, "Blinds", $"{table.smallBlind}/{table.bigBlind}", 18f, theme.accentColor);
            blindsText.GetOrAddComponent<LayoutElement>().preferredWidth = 150;
            
            // Lock icon for private
            if (table.isPrivate)
            {
                var lockText = UIFactory.CreateText(item.transform, "Lock", "🔒", 24f, theme.dangerColor);
                lockText.GetOrAddComponent<LayoutElement>().preferredWidth = 40;
            }
            
            // Spacer
            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(item.transform, false);
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1;
            
            // Join Button
            var joinBtn = UIFactory.CreateButton(item.transform, "Join", "JOIN", () => OnJoinTableClick(table));
            joinBtn.GetOrAddComponent<LayoutElement>().preferredWidth = 100;
            joinBtn.GetComponent<Image>().color = theme.primaryColor;
            
            return item;
        }
        
        private void OnJoinTableClick(TableInfo table)
        {
            loadingPanel.SetActive(true);
            
            string password = null;
            if (table.isPrivate)
            {
                // TODO: Show password input dialog
            }
            
            _gameService.JoinTable(table.id, null, password, (success, error) =>
            {
                loadingPanel.SetActive(false);
                if (!success)
                {
                    statusText.text = $"Failed to join: {error}";
                    statusText.gameObject.SetActive(true);
                }
            });
        }
        
        private void OnTableJoined(TableState state)
        {
            loadingPanel.SetActive(false);
            SceneManager.LoadScene("TableScene");
        }
        
        private void OnCreateTableClick()
        {
            string name = tableNameInput?.text;
            if (string.IsNullOrEmpty(name))
            {
                name = $"{_gameService.CurrentUser?.username}'s Table";
            }
            
            int maxPlayers = maxPlayersSlider != null ? (int)maxPlayersSlider.value : 6;
            var blinds = GetBlindsFromSlider((int)smallBlindSlider.value);
            int buyIn = buyInSlider != null ? GetBuyInFromSlider((int)buyInSlider.value) : 20000000;
            bool isPrivate = privateToggle != null && privateToggle.isOn;
            string password = isPrivate ? passwordInput?.text : null;
            bool practiceMode = practiceModeToggle != null && practiceModeToggle.isOn;
            int turnTimeLimit = turnTimeSlider != null ? GetTurnTimeFromSlider((int)turnTimeSlider.value) : 20000;
            int blindIncreaseInterval = roundTimerSlider != null ? GetRoundTimerFromSlider((int)roundTimerSlider.value) : 0;
            
            loadingPanel.SetActive(true);
            
            // Subscribe to OnTableJoined to load scene AFTER CurrentTableId is set
            _gameService.OnTableJoined -= OnTableJoinedForCreate;
            _gameService.OnTableJoined += OnTableJoinedForCreate;
            
            _gameService.CreateTable(name, maxPlayers, blinds.small, blinds.big, buyIn, isPrivate, password, practiceMode, turnTimeLimit, blindIncreaseInterval, (success, result) =>
            {
                if (!success)
                {
                    loadingPanel.SetActive(false);
                    _gameService.OnTableJoined -= OnTableJoinedForCreate;
                    Debug.LogError($"[LobbyScene] Failed to create/join table: {result}");
                }
                // If success, OnTableJoined event will fire and load the scene
            });
        }
        
        private void OnTableJoinedForCreate(TableState state)
        {
            _gameService.OnTableJoined -= OnTableJoinedForCreate;
            loadingPanel.SetActive(false);
            SceneManager.LoadScene("TableScene");
        }
        
        private void OnSearchUsers()
        {
            string query = inviteSearchInput?.text;
            if (string.IsNullOrEmpty(query)) return;
            
            _gameService.SearchUsers(query, users =>
            {
                // Clear old results
                foreach (Transform child in searchResultsContainer)
                {
                    Destroy(child.gameObject);
                }
                
                foreach (var user in users)
                {
                    CreateUserSearchResult(user);
                }
            });
        }
        
        private void CreateUserSearchResult(UserSearchResult user)
        {
            var theme = Theme.Current;
            
            var row = UIFactory.CreatePanel(searchResultsContainer, $"User_{user.id}", Color.clear);
            row.GetOrAddComponent<LayoutElement>().preferredHeight = 40;
            
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            
            var nameText = UIFactory.CreateText(row.transform, "Name", user.username, 18f, theme.textPrimary);
            nameText.GetOrAddComponent<LayoutElement>().flexibleWidth = 1;
            
            var statusText = UIFactory.CreateText(row.transform, "Status", user.isOnline ? "●" : "○", 18f, 
                user.isOnline ? theme.successColor : theme.textSecondary);
            statusText.GetOrAddComponent<LayoutElement>().preferredWidth = 30;
            
            var inviteBtn = UIFactory.CreateButton(row.transform, "Invite", "Invite", () => OnInviteUser(user.id));
            inviteBtn.GetOrAddComponent<LayoutElement>().preferredWidth = 80;
        }
        
        private void OnInviteUser(string oderId)
        {
            _gameService.InvitePlayer(oderId, (success, error) =>
            {
                if (success)
                {
                    invitePanel.SetActive(false);
                }
            });
        }
        
        private void OnInviteReceived(TableInviteData invite)
        {
            // Show invite notification
            Debug.Log($"Received invite from {invite.inviterName} to join {invite.tableName}");
            // TODO: Show toast/popup
        }
        
        #endregion
        
        #region Helpers
        
        private void OnBackClick()
        {
            SceneManager.LoadScene("MainMenuScene");
        }
        
        private Slider CreateSlider(Transform parent, float min, float max, float defaultValue)
        {
            var sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(parent, false);
            var sliderRect = sliderObj.AddComponent<RectTransform>();
            sliderRect.sizeDelta = new Vector2(150, 20);
            
            var slider = sliderObj.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = defaultValue;
            slider.wholeNumbers = true;
            
            // Background - thin track
            var bgObj = UIFactory.CreatePanel(sliderObj.transform, "Background", Theme.Current.backgroundColor);
            var bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.35f);
            bgRect.anchorMax = new Vector2(1, 0.65f);
            bgRect.sizeDelta = Vector2.zero;
            
            // Fill area - thin track
            var fillArea = new GameObject("FillArea");
            fillArea.transform.SetParent(sliderObj.transform, false);
            var fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0, 0.35f);
            fillAreaRect.anchorMax = new Vector2(1, 0.65f);
            fillAreaRect.offsetMin = new Vector2(8, 0);
            fillAreaRect.offsetMax = new Vector2(-8, 0);
            
            var fillObj = UIFactory.CreatePanel(fillArea.transform, "Fill", Theme.Current.primaryColor);
            var fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            
            slider.fillRect = fillRect;
            
            // Handle slide area - constrained to center
            var handleArea = new GameObject("HandleSlideArea");
            handleArea.transform.SetParent(sliderObj.transform, false);
            var handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = new Vector2(0, 0.5f);
            handleAreaRect.anchorMax = new Vector2(1, 0.5f);
            handleAreaRect.sizeDelta = new Vector2(-16, 0); // Inset for handle width
            handleAreaRect.anchoredPosition = Vector2.zero;
            
            // Handle - fixed size, centered vertically
            var handleObj = UIFactory.CreatePanel(handleArea.transform, "Handle", Color.white);
            var handleRect = handleObj.GetComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0, 0.5f);
            handleRect.anchorMax = new Vector2(0, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.sizeDelta = new Vector2(14, 14);
            handleRect.anchoredPosition = Vector2.zero;
            
            slider.handleRect = handleRect;
            slider.targetGraphic = handleObj.GetComponent<Image>();
            
            return slider;
        }
        
        private Toggle CreateToggle(Transform parent)
        {
            var toggleObj = new GameObject("Toggle");
            toggleObj.transform.SetParent(parent, false);
            var rect = toggleObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(20, 20); // Fixed small size
            
            var toggle = toggleObj.AddComponent<Toggle>();
            
            var bg = UIFactory.CreatePanel(toggleObj.transform, "Background", Theme.Current.backgroundColor);
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            
            var checkmark = UIFactory.CreatePanel(bg.transform, "Checkmark", Theme.Current.primaryColor);
            var checkRect = checkmark.GetComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.2f, 0.2f);
            checkRect.anchorMax = new Vector2(0.8f, 0.8f);
            checkRect.sizeDelta = Vector2.zero;
            
            toggle.targetGraphic = bg.GetComponent<Image>();
            toggle.graphic = checkmark.GetComponent<Image>();
            
            return toggle;
        }
        
        private void UpdateBlindsDisplay(float value)
        {
            var blinds = GetBlindsFromSlider((int)value);
            blindsValue.text = $"{blinds.small}/{blinds.big}";
        }
        
        private (int small, int big) GetBlindsFromSlider(int level)
        {
            return level switch
            {
                1 => (25, 50),
                2 => (50, 100),
                3 => (100, 200),
                4 => (250, 500),
                5 => (500, 1000),
                6 => (5000, 10000),
                _ => (50, 100)
            };
        }
        
        private void UpdateBuyInDisplay(float value)
        {
            var buyIn = GetBuyInFromSlider((int)value);
            buyInValue.text = FormatBuyIn(buyIn);
        }
        
        private int GetBuyInFromSlider(int level)
        {
            return level switch
            {
                1 => 1000000,      // 1M
                2 => 2000000,      // 2M
                3 => 5000000,      // 5M
                4 => 10000000,     // 10M
                5 => 20000000,     // 20M (default)
                6 => 50000000,     // 50M
                7 => 75000000,     // 75M
                8 => 100000000,    // 100M
                _ => 20000000      // Default 20M
            };
        }
        
        private string FormatBuyIn(int amount)
        {
            if (amount >= 1000000)
                return $"{amount / 1000000}M";
            if (amount >= 1000)
                return $"{amount / 1000}K";
            return amount.ToString();
        }
        
        private void UpdateTurnTimeDisplay(float value)
        {
            var turnTime = GetTurnTimeFromSlider((int)value);
            turnTimeValue.text = $"{turnTime / 1000}s";
        }
        
        private int GetTurnTimeFromSlider(int level)
        {
            return level switch
            {
                1 => 5000,      // 5 seconds
                2 => 10000,     // 10 seconds
                3 => 15000,     // 15 seconds
                4 => 20000,     // 20 seconds (default)
                5 => 30000,     // 30 seconds
                6 => 60000,     // 60 seconds
                _ => 20000      // Default 20 seconds
            };
        }
        
        private void UpdateRoundTimerDisplay(float value)
        {
            var roundTime = GetRoundTimerFromSlider((int)value);
            if (roundTime == 0)
            {
                roundTimerValue.text = "OFF";
            }
            else
            {
                roundTimerValue.text = $"{roundTime / 60000}m";
            }
        }
        
        private int GetRoundTimerFromSlider(int level)
        {
            // Returns milliseconds - 0 means disabled
            return level switch
            {
                0 => 0,             // OFF (blinds never increase)
                1 => 5 * 60000,     // 5 minutes
                2 => 10 * 60000,    // 10 minutes
                3 => 15 * 60000,    // 15 minutes
                4 => 20 * 60000,    // 20 minutes
                5 => 30 * 60000,    // 30 minutes
                6 => 60 * 60000,    // 60 minutes
                _ => 0              // Default OFF
            };
        }
        
        #endregion
    }
}


