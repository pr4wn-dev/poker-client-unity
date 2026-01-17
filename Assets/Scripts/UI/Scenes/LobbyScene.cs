using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
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
        private Toggle privateToggle;
        private TextMeshProUGUI maxPlayersValue;
        private TextMeshProUGUI blindsValue;
        
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
                canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasObj.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
                canvasObj.AddComponent<GraphicRaycaster>();
            }
            
            var theme = Theme.Current;
            
            // Background
            var bg = UIFactory.CreatePanel(_canvas.transform, "Background", theme.backgroundColor);
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            
            // Header
            BuildHeader();
            
            // Main Content Area
            BuildTableListPanel();
            BuildCreateTablePanel();
            BuildInvitePanel();
            BuildLoadingPanel();
            
            // Show table list by default
            ShowTableListPanel();
        }
        
        private void BuildHeader()
        {
            var theme = Theme.Current;
            
            var header = UIFactory.CreatePanel(_canvas.transform, "Header", theme.cardPanelColor);
            var headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.sizeDelta = new Vector2(0, 80);
            headerRect.anchoredPosition = Vector2.zero;
            
            // Back button
            var backBtn = UIFactory.CreateButton(header.transform, "BackBtn", "← Back", OnBackClick);
            var backRect = backBtn.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0, 0.5f);
            backRect.anchorMax = new Vector2(0, 0.5f);
            backRect.pivot = new Vector2(0, 0.5f);
            backRect.anchoredPosition = new Vector2(20, 0);
            backRect.sizeDelta = new Vector2(120, 50);
            
            // Title
            var title = UIFactory.CreateTitle(header.transform, "Title", "MULTIPLAYER LOBBY", 36f);
            title.alignment = TextAlignmentOptions.Center;
            
            // Player Info
            playerInfoText = UIFactory.CreateText(header.transform, "PlayerInfo", "", 18f, theme.textSecondary);
            var infoRect = playerInfoText.GetComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(1, 0.5f);
            infoRect.anchorMax = new Vector2(1, 0.5f);
            infoRect.pivot = new Vector2(1, 0.5f);
            infoRect.anchoredPosition = new Vector2(-20, 0);
            infoRect.sizeDelta = new Vector2(300, 40);
            playerInfoText.alignment = TextAlignmentOptions.Right;
            
            if (_gameService?.CurrentUser != null)
            {
                playerInfoText.text = $"{_gameService.CurrentUser.username} | {ChipStack.FormatChipValue((int)_gameService.CurrentUser.chips)}";
            }
            
            // Button row
            var buttonRow = UIFactory.CreatePanel(header.transform, "ButtonRow", Color.clear);
            var rowRect = buttonRow.GetComponent<RectTransform>();
            rowRect.anchorMin = new Vector2(0.5f, 0);
            rowRect.anchorMax = new Vector2(0.5f, 0);
            rowRect.pivot = new Vector2(0.5f, 1);
            rowRect.anchoredPosition = new Vector2(0, -10);
            rowRect.sizeDelta = new Vector2(600, 50);
            
            var hlg = buttonRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childForceExpandWidth = false;
            
            var tablesBtn = UIFactory.CreateButton(buttonRow.transform, "TablesBtn", "Browse Tables", () => ShowTableListPanel());
            tablesBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(180, 45);
            
            var createBtn = UIFactory.CreateButton(buttonRow.transform, "CreateBtn", "Create Table", () => ShowCreateTablePanel());
            createBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(180, 45);
            
            var refreshBtn = UIFactory.CreateButton(buttonRow.transform, "RefreshBtn", "↻ Refresh", RefreshTableList);
            refreshBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 45);
        }
        
        private void BuildTableListPanel()
        {
            var theme = Theme.Current;
            
            tableListPanel = UIFactory.CreatePanel(_canvas.transform, "TableListPanel", theme.backgroundColor);
            var panelRect = tableListPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.1f, 0.1f);
            panelRect.anchorMax = new Vector2(0.9f, 0.85f);
            panelRect.sizeDelta = Vector2.zero;
            
            // Scroll view
            var scrollView = new GameObject("ScrollView");
            scrollView.transform.SetParent(tableListPanel.transform, false);
            var scrollRect = scrollView.AddComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.sizeDelta = Vector2.zero;
            
            var scrollComponent = scrollView.AddComponent<ScrollRect>();
            scrollComponent.horizontal = false;
            
            // Viewport
            var viewport = UIFactory.CreatePanel(scrollView.transform, "Viewport", Color.clear);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            scrollComponent.viewport = viewportRect;
            
            // Content
            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var contentRect = content.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 0);
            scrollComponent.content = contentRect;
            
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10;
            vlg.padding = new RectOffset(20, 20, 20, 20);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = false;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            
            var csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            tableListContainer = content.transform;
            
            // Status text
            statusText = UIFactory.CreateText(tableListPanel.transform, "Status", "Loading tables...", 24f, theme.textSecondary);
            statusText.alignment = TextAlignmentOptions.Center;
        }
        
        private void BuildCreateTablePanel()
        {
            var theme = Theme.Current;
            
            createTablePanel = UIFactory.CreatePanel(_canvas.transform, "CreateTablePanel", theme.cardPanelColor);
            var panelRect = createTablePanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.25f, 0.2f);
            panelRect.anchorMax = new Vector2(0.75f, 0.8f);
            panelRect.sizeDelta = Vector2.zero;
            
            var vlg = createTablePanel.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 20;
            vlg.padding = new RectOffset(40, 40, 40, 40);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = false;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            
            // Title
            var title = UIFactory.CreateTitle(createTablePanel.transform, "Title", "CREATE TABLE", 32f);
            title.GetComponent<LayoutElement>().preferredHeight = 50;
            
            // Table Name
            var nameLabel = UIFactory.CreateText(createTablePanel.transform, "NameLabel", "Table Name:", 18f, theme.textSecondary);
            nameLabel.GetComponent<LayoutElement>().preferredHeight = 25;
            
            tableNameInput = UIFactory.CreateInputField(createTablePanel.transform, "TableName", "Enter table name...");
            tableNameInput.GetComponent<LayoutElement>().preferredHeight = 50;
            
            // Max Players Slider
            var playersLabel = UIFactory.CreateText(createTablePanel.transform, "PlayersLabel", "Max Players:", 18f, theme.textSecondary);
            playersLabel.GetComponent<LayoutElement>().preferredHeight = 25;
            
            var playersRow = UIFactory.CreatePanel(createTablePanel.transform, "PlayersRow", Color.clear);
            playersRow.GetComponent<LayoutElement>().preferredHeight = 40;
            var playersHlg = playersRow.AddComponent<HorizontalLayoutGroup>();
            playersHlg.spacing = 10;
            playersHlg.childAlignment = TextAnchor.MiddleCenter;
            
            maxPlayersSlider = CreateSlider(playersRow.transform, 2, 9, 6);
            maxPlayersValue = UIFactory.CreateText(playersRow.transform, "Value", "6", 24f, theme.primaryColor);
            maxPlayersValue.GetComponent<LayoutElement>().preferredWidth = 50;
            maxPlayersSlider.onValueChanged.AddListener(v => maxPlayersValue.text = ((int)v).ToString());
            
            // Blinds Slider
            var blindsLabel = UIFactory.CreateText(createTablePanel.transform, "BlindsLabel", "Blinds:", 18f, theme.textSecondary);
            blindsLabel.GetComponent<LayoutElement>().preferredHeight = 25;
            
            var blindsRow = UIFactory.CreatePanel(createTablePanel.transform, "BlindsRow", Color.clear);
            blindsRow.GetComponent<LayoutElement>().preferredHeight = 40;
            var blindsHlg = blindsRow.AddComponent<HorizontalLayoutGroup>();
            blindsHlg.spacing = 10;
            blindsHlg.childAlignment = TextAnchor.MiddleCenter;
            
            smallBlindSlider = CreateSlider(blindsRow.transform, 1, 6, 1); // 1=25/50, 6=5000/10000
            blindsValue = UIFactory.CreateText(blindsRow.transform, "Value", "25/50", 24f, theme.primaryColor);
            blindsValue.GetComponent<LayoutElement>().preferredWidth = 150;
            smallBlindSlider.onValueChanged.AddListener(UpdateBlindsDisplay);
            
            // Private Toggle
            var privateRow = UIFactory.CreatePanel(createTablePanel.transform, "PrivateRow", Color.clear);
            privateRow.GetComponent<LayoutElement>().preferredHeight = 50;
            var privateHlg = privateRow.AddComponent<HorizontalLayoutGroup>();
            privateHlg.spacing = 20;
            privateHlg.childAlignment = TextAnchor.MiddleCenter;
            
            var privateLabel = UIFactory.CreateText(privateRow.transform, "PrivateLabel", "Private Table:", 18f, theme.textSecondary);
            privateToggle = CreateToggle(privateRow.transform);
            privateToggle.onValueChanged.AddListener(v => passwordInput.gameObject.SetActive(v));
            
            // Password
            passwordInput = UIFactory.CreateInputField(createTablePanel.transform, "Password", "Password (optional)");
            passwordInput.contentType = TMP_InputField.ContentType.Password;
            passwordInput.GetComponent<LayoutElement>().preferredHeight = 50;
            passwordInput.gameObject.SetActive(false);
            
            // Buttons
            var buttonRow = UIFactory.CreatePanel(createTablePanel.transform, "ButtonRow", Color.clear);
            buttonRow.GetComponent<LayoutElement>().preferredHeight = 60;
            var btnHlg = buttonRow.AddComponent<HorizontalLayoutGroup>();
            btnHlg.spacing = 20;
            btnHlg.childAlignment = TextAnchor.MiddleCenter;
            btnHlg.childControlWidth = false;
            btnHlg.childForceExpandWidth = false;
            
            var cancelBtn = UIFactory.CreateButton(buttonRow.transform, "Cancel", "Cancel", () => ShowTableListPanel());
            cancelBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(150, 50);
            
            var createBtn = UIFactory.CreateButton(buttonRow.transform, "Create", "Create", OnCreateTableClick);
            createBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(150, 50);
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
            
            var vlg = invitePanel.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 15;
            vlg.padding = new RectOffset(30, 30, 30, 30);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = false;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            
            var title = UIFactory.CreateTitle(invitePanel.transform, "Title", "INVITE PLAYER", 28f);
            title.GetComponent<LayoutElement>().preferredHeight = 40;
            
            var searchRow = UIFactory.CreatePanel(invitePanel.transform, "SearchRow", Color.clear);
            searchRow.GetComponent<LayoutElement>().preferredHeight = 50;
            var hlg = searchRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.childControlWidth = true;
            hlg.childForceExpandWidth = true;
            
            inviteSearchInput = UIFactory.CreateInputField(searchRow.transform, "Search", "Enter username...");
            var searchBtn = UIFactory.CreateButton(searchRow.transform, "SearchBtn", "Search", OnSearchUsers);
            searchBtn.GetComponent<LayoutElement>().preferredWidth = 100;
            
            // Results container
            var resultsPanel = UIFactory.CreatePanel(invitePanel.transform, "Results", theme.backgroundColor);
            resultsPanel.GetComponent<LayoutElement>().preferredHeight = 150;
            searchResultsContainer = resultsPanel.transform;
            
            var closeBtn = UIFactory.CreateButton(invitePanel.transform, "Close", "Close", () => invitePanel.SetActive(false));
            closeBtn.GetComponent<LayoutElement>().preferredHeight = 45;
            
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
                var item = CreateTableListItem(table);
                tableListItems.Add(item);
            }
        }
        
        private GameObject CreateTableListItem(TableInfo table)
        {
            var theme = Theme.Current;
            
            var item = UIFactory.CreatePanel(tableListContainer, $"Table_{table.id}", theme.cardPanelColor);
            var itemRect = item.GetComponent<RectTransform>();
            itemRect.sizeDelta = new Vector2(0, 80);
            item.GetComponent<LayoutElement>().preferredHeight = 80;
            
            var hlg = item.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20;
            hlg.padding = new RectOffset(20, 20, 10, 10);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childForceExpandWidth = false;
            
            // Table Name
            var nameText = UIFactory.CreateTitle(item.transform, "Name", table.name, 24f);
            nameText.GetComponent<LayoutElement>().preferredWidth = 300;
            
            // Players
            var playersText = UIFactory.CreateText(item.transform, "Players", $"{table.playerCount}/{table.maxPlayers} Players", 18f, theme.textSecondary);
            playersText.GetComponent<LayoutElement>().preferredWidth = 150;
            
            // Blinds
            var blindsText = UIFactory.CreateText(item.transform, "Blinds", $"{table.smallBlind}/{table.bigBlind}", 18f, theme.accentColor);
            blindsText.GetComponent<LayoutElement>().preferredWidth = 150;
            
            // Lock icon for private
            if (table.isPrivate)
            {
                var lockText = UIFactory.CreateText(item.transform, "Lock", "🔒", 24f, theme.dangerColor);
                lockText.GetComponent<LayoutElement>().preferredWidth = 40;
            }
            
            // Spacer
            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(item.transform, false);
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1;
            
            // Join Button
            var joinBtn = UIFactory.CreateButton(item.transform, "Join", "JOIN", () => OnJoinTableClick(table));
            joinBtn.GetComponent<LayoutElement>().preferredWidth = 100;
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
            bool isPrivate = privateToggle != null && privateToggle.isOn;
            string password = isPrivate ? passwordInput?.text : null;
            
            loadingPanel.SetActive(true);
            
            _gameService.CreateTable(name, maxPlayers, blinds.small, blinds.big, isPrivate, password, (success, result) =>
            {
                loadingPanel.SetActive(false);
                if (success)
                {
                    // Auto-join the table we just created
                    _gameService.JoinTable(result, null, password, (joinSuccess, error) =>
                    {
                        if (joinSuccess)
                        {
                            SceneManager.LoadScene("TableScene");
                        }
                    });
                }
            });
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
            row.GetComponent<LayoutElement>().preferredHeight = 40;
            
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            
            var nameText = UIFactory.CreateText(row.transform, "Name", user.username, 18f, theme.textPrimary);
            nameText.GetComponent<LayoutElement>().flexibleWidth = 1;
            
            var statusText = UIFactory.CreateText(row.transform, "Status", user.isOnline ? "●" : "○", 18f, 
                user.isOnline ? theme.successColor : theme.textSecondary);
            statusText.GetComponent<LayoutElement>().preferredWidth = 30;
            
            var inviteBtn = UIFactory.CreateButton(row.transform, "Invite", "Invite", () => OnInviteUser(user.id));
            inviteBtn.GetComponent<LayoutElement>().preferredWidth = 80;
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
            sliderObj.AddComponent<LayoutElement>().flexibleWidth = 1;
            
            var slider = sliderObj.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = defaultValue;
            slider.wholeNumbers = true;
            
            // Background
            var bgObj = UIFactory.CreatePanel(sliderObj.transform, "Background", Theme.Current.backgroundColor);
            var bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.25f);
            bgRect.anchorMax = new Vector2(1, 0.75f);
            bgRect.sizeDelta = Vector2.zero;
            
            // Fill
            var fillArea = new GameObject("FillArea");
            fillArea.transform.SetParent(sliderObj.transform, false);
            var fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1, 0.75f);
            fillAreaRect.sizeDelta = Vector2.zero;
            
            var fillObj = UIFactory.CreatePanel(fillArea.transform, "Fill", Theme.Current.primaryColor);
            var fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            
            slider.fillRect = fillRect;
            
            // Handle
            var handleArea = new GameObject("HandleArea");
            handleArea.transform.SetParent(sliderObj.transform, false);
            var handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.sizeDelta = Vector2.zero;
            
            var handleObj = UIFactory.CreatePanel(handleArea.transform, "Handle", Color.white);
            var handleRect = handleObj.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 30);
            
            slider.handleRect = handleRect;
            slider.targetGraphic = handleObj.GetComponent<Image>();
            
            return slider;
        }
        
        private Toggle CreateToggle(Transform parent)
        {
            var toggleObj = new GameObject("Toggle");
            toggleObj.transform.SetParent(parent, false);
            toggleObj.AddComponent<LayoutElement>().preferredWidth = 50;
            
            var toggle = toggleObj.AddComponent<Toggle>();
            
            var bg = UIFactory.CreatePanel(toggleObj.transform, "Background", Theme.Current.backgroundColor);
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.sizeDelta = new Vector2(40, 40);
            
            var checkmark = UIFactory.CreatePanel(bg.transform, "Checkmark", Theme.Current.primaryColor);
            var checkRect = checkmark.GetComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.1f, 0.1f);
            checkRect.anchorMax = new Vector2(0.9f, 0.9f);
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
        
        #endregion
    }
}


