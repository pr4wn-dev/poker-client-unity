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
            
            // Title - properly sized to fit container
            var title = UIFactory.CreateTitle(header.transform, "Title", "MULTIPLAYER LOBBY", 28f);
            title.alignment = TextAlignmentOptions.Center;
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.25f, 0);
            titleRect.anchorMax = new Vector2(0.75f, 1);
            titleRect.sizeDelta = Vector2.zero;
            
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
            tablesBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 45);
            
            var createBtn = UIFactory.CreateButton(buttonRow.transform, "CreateBtn", "Create Table", () => ShowCreateTablePanel());
            createBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 45);
            
            var tournamentsBtn = UIFactory.CreateButton(buttonRow.transform, "TournamentsBtn", "🏆 Tournaments", OnTournamentsClick);
            tournamentsBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(170, 45);
            tournamentsBtn.GetComponent<Image>().color = Theme.Current.accentColor;
            
            var refreshBtn = UIFactory.CreateButton(buttonRow.transform, "RefreshBtn", "↻", RefreshTableList);
            refreshBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(50, 45);
        }
        
        private void OnTournamentsClick()
        {
            SceneManager.LoadScene("TournamentScene");
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
            
            createTablePanel = UIFactory.CreatePanel(_canvas.transform, "CreateTablePanel", theme.panelColor);
            var panelRect = createTablePanel.GetComponent<RectTransform>();
            // Fixed centered size - compact
            UIFactory.Center(panelRect, new Vector2(380, 380));
            
            var vlg = createTablePanel.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8;
            vlg.padding = new RectOffset(20, 20, 15, 15);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = false;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            
            // Title
            var title = UIFactory.CreateTitle(createTablePanel.transform, "Title", "CREATE TABLE", 24f);
            title.color = theme.secondaryColor;
            title.GetOrAddComponent<LayoutElement>().preferredHeight = 32;
            
            // Table Name
            var nameLabel = UIFactory.CreateText(createTablePanel.transform, "NameLabel", "Table Name", 12f, theme.textSecondary);
            nameLabel.alignment = TextAlignmentOptions.Left;
            nameLabel.GetOrAddComponent<LayoutElement>().preferredHeight = 16;
            
            tableNameInput = UIFactory.CreateInputField(createTablePanel.transform, "TableName", "Enter table name...", 340, 36);
            tableNameInput.GetOrAddComponent<LayoutElement>().preferredHeight = 36;
            
            // Max Players Row - compact
            var playersRow = UIFactory.CreatePanel(createTablePanel.transform, "PlayersRow", Color.clear);
            playersRow.GetOrAddComponent<LayoutElement>().preferredHeight = 28;
            var playersHlg = playersRow.AddComponent<HorizontalLayoutGroup>();
            playersHlg.spacing = 8;
            playersHlg.childAlignment = TextAnchor.MiddleLeft;
            playersHlg.childControlWidth = false;
            playersHlg.childForceExpandWidth = false;
            
            var playersLabel = UIFactory.CreateText(playersRow.transform, "PlayersLabel", "Max Players:", 12f, theme.textSecondary);
            playersLabel.GetOrAddComponent<LayoutElement>().preferredWidth = 85;
            
            maxPlayersSlider = CreateSlider(playersRow.transform, 2, 9, 6);
            maxPlayersSlider.GetOrAddComponent<LayoutElement>().preferredWidth = 150;
            maxPlayersSlider.GetOrAddComponent<LayoutElement>().preferredHeight = 20;
            
            maxPlayersValue = UIFactory.CreateText(playersRow.transform, "Value", "6", 16f, theme.primaryColor);
            maxPlayersValue.fontStyle = FontStyles.Bold;
            maxPlayersValue.GetOrAddComponent<LayoutElement>().preferredWidth = 30;
            maxPlayersSlider.onValueChanged.AddListener(v => maxPlayersValue.text = ((int)v).ToString());
            
            // Blinds Row - compact
            var blindsRow = UIFactory.CreatePanel(createTablePanel.transform, "BlindsRow", Color.clear);
            blindsRow.GetOrAddComponent<LayoutElement>().preferredHeight = 28;
            var blindsHlg = blindsRow.AddComponent<HorizontalLayoutGroup>();
            blindsHlg.spacing = 8;
            blindsHlg.childAlignment = TextAnchor.MiddleLeft;
            blindsHlg.childControlWidth = false;
            blindsHlg.childForceExpandWidth = false;
            
            var blindsLabel = UIFactory.CreateText(blindsRow.transform, "BlindsLabel", "Blinds:", 12f, theme.textSecondary);
            blindsLabel.GetOrAddComponent<LayoutElement>().preferredWidth = 85;
            
            smallBlindSlider = CreateSlider(blindsRow.transform, 1, 6, 1);
            smallBlindSlider.GetOrAddComponent<LayoutElement>().preferredWidth = 150;
            smallBlindSlider.GetOrAddComponent<LayoutElement>().preferredHeight = 20;
            
            blindsValue = UIFactory.CreateText(blindsRow.transform, "Value", "25/50", 16f, theme.primaryColor);
            blindsValue.fontStyle = FontStyles.Bold;
            blindsValue.GetOrAddComponent<LayoutElement>().preferredWidth = 60;
            smallBlindSlider.onValueChanged.AddListener(UpdateBlindsDisplay);
            
            // Private Toggle Row - compact
            var privateRow = UIFactory.CreatePanel(createTablePanel.transform, "PrivateRow", Color.clear);
            privateRow.GetOrAddComponent<LayoutElement>().preferredHeight = 28;
            var privateHlg = privateRow.AddComponent<HorizontalLayoutGroup>();
            privateHlg.spacing = 10;
            privateHlg.childAlignment = TextAnchor.MiddleLeft;
            privateHlg.childControlWidth = false;
            privateHlg.childForceExpandWidth = false;
            
            var privateLabel = UIFactory.CreateText(privateRow.transform, "PrivateLabel", "Private:", 12f, theme.textSecondary);
            privateLabel.GetOrAddComponent<LayoutElement>().preferredWidth = 55;
            privateToggle = CreateToggle(privateRow.transform);
            privateToggle.onValueChanged.AddListener(v => passwordInput.gameObject.SetActive(v));
            
            // Password (hidden by default)
            passwordInput = UIFactory.CreateInputField(createTablePanel.transform, "Password", "Password", 340, 36,
                TMP_InputField.ContentType.Password);
            passwordInput.GetOrAddComponent<LayoutElement>().preferredHeight = 36;
            passwordInput.gameObject.SetActive(false);
            
            // Spacer
            var spacer = new GameObject("Spacer", typeof(RectTransform));
            spacer.transform.SetParent(createTablePanel.transform, false);
            spacer.GetComponent<RectTransform>().sizeDelta = new Vector2(340, 8);
            
            // Buttons
            var createBtn = UIFactory.CreatePrimaryButton(createTablePanel.transform, "Create", "CREATE TABLE", OnCreateTableClick, 340, 40);
            
            var cancelBtn = UIFactory.CreateSecondaryButton(createTablePanel.transform, "Cancel", "CANCEL", () => ShowTableListPanel(), 140, 32);
            
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
            bool isPrivate = privateToggle != null && privateToggle.isOn;
            string password = isPrivate ? passwordInput?.text : null;
            
            loadingPanel.SetActive(true);
            
            // Subscribe to OnTableJoined to load scene AFTER CurrentTableId is set
            _gameService.OnTableJoined -= OnTableJoinedForCreate;
            _gameService.OnTableJoined += OnTableJoinedForCreate;
            
            _gameService.CreateTable(name, maxPlayers, blinds.small, blinds.big, isPrivate, password, (success, result) =>
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
            sliderRect.sizeDelta = new Vector2(150, 20); // Fixed compact size
            
            var slider = sliderObj.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = defaultValue;
            slider.wholeNumbers = true;
            
            // Background - thin track
            var bgObj = UIFactory.CreatePanel(sliderObj.transform, "Background", Theme.Current.backgroundColor);
            var bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.4f);
            bgRect.anchorMax = new Vector2(1, 0.6f);
            bgRect.sizeDelta = Vector2.zero;
            
            // Fill area - thin track
            var fillArea = new GameObject("FillArea");
            fillArea.transform.SetParent(sliderObj.transform, false);
            var fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0, 0.4f);
            fillAreaRect.anchorMax = new Vector2(1, 0.6f);
            fillAreaRect.offsetMin = new Vector2(5, 0);
            fillAreaRect.offsetMax = new Vector2(-5, 0);
            
            var fillObj = UIFactory.CreatePanel(fillArea.transform, "Fill", Theme.Current.primaryColor);
            var fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            
            slider.fillRect = fillRect;
            
            // Handle - small circular
            var handleArea = new GameObject("HandleArea");
            handleArea.transform.SetParent(sliderObj.transform, false);
            var handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(5, 0);
            handleAreaRect.offsetMax = new Vector2(-5, 0);
            
            var handleObj = UIFactory.CreatePanel(handleArea.transform, "Handle", Color.white);
            var handleRect = handleObj.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(16, 16); // Small square handle
            
            slider.handleRect = handleRect;
            slider.targetGraphic = handleObj.GetComponent<Image>();
            
            return slider;
        }
        
        private Toggle CreateToggle(Transform parent)
        {
            var toggleObj = new GameObject("Toggle");
            toggleObj.transform.SetParent(parent, false);
            var le = toggleObj.AddComponent<LayoutElement>();
            le.preferredWidth = 24;
            le.preferredHeight = 24;
            
            var toggle = toggleObj.AddComponent<Toggle>();
            
            var bg = UIFactory.CreatePanel(toggleObj.transform, "Background", Theme.Current.backgroundColor);
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            
            var checkmark = UIFactory.CreatePanel(bg.transform, "Checkmark", Theme.Current.primaryColor);
            var checkRect = checkmark.GetComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.15f, 0.15f);
            checkRect.anchorMax = new Vector2(0.85f, 0.85f);
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


