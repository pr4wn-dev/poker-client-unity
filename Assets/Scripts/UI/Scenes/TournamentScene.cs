using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using PokerClient.UI;
using PokerClient.Networking;
using System.Collections.Generic;

namespace PokerClient.UI.Scenes
{
    /// <summary>
    /// Tournament lobby scene - view and join tournaments
    /// </summary>
    public class TournamentScene : MonoBehaviour
    {
        private Canvas _canvas;
        private GameService _gameService;
        
        // UI Elements
        private GameObject _loadingPanel;
        private GameObject _listPanel;
        private GameObject _detailPanel;
        private TextMeshProUGUI _titleText;
        private Transform _tournamentListContainer;
        private List<GameObject> _tournamentItems = new List<GameObject>();
        
        // Detail panel
        private TextMeshProUGUI _detailTitleText;
        private TextMeshProUGUI _detailTypeText;
        private TextMeshProUGUI _detailStatusText;
        private TextMeshProUGUI _detailPlayersText;
        private TextMeshProUGUI _detailBlindsText;
        private TextMeshProUGUI _detailEntryText;
        private TextMeshProUGUI _detailPrizeText;
        private TextMeshProUGUI _detailRequirementsText;
        private Button _registerButton;
        private Button _unregisterButton;
        private Button _backButton;
        
        // Current state
        private List<TournamentInfo> _tournaments = new List<TournamentInfo>();
        private TournamentInfo _selectedTournament;
        private bool _isRegistered;
        
        private void Start()
        {
            _gameService = GameService.Instance;
            if (_gameService == null)
            {
                Debug.LogError("GameService not found!");
                SceneManager.LoadScene("MainMenuScene");
                return;
            }
            
            BuildScene();
            LoadTournaments();
        }
        
        private void BuildScene()
        {
            _canvas = FindObjectOfType<Canvas>();
            if (_canvas == null)
            {
                var canvasObj = new GameObject("Canvas");
                _canvas = canvasObj.AddComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                canvasObj.AddComponent<GraphicRaycaster>();
            }
            
            var theme = Theme.Current;
            
            // Background
            var bg = UIFactory.CreatePanel(_canvas.transform, "Background", theme.backgroundColor);
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            
            BuildHeader();
            BuildListPanel();
            BuildDetailPanel();
            BuildLoadingPanel();
            
            // Initial state
            _detailPanel.SetActive(false);
            _loadingPanel.SetActive(true);
        }
        
        private void BuildHeader()
        {
            var theme = Theme.Current;
            
            var header = UIFactory.CreatePanel(_canvas.transform, "Header", theme.cardPanelColor);
            var headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 0.9f);
            headerRect.anchorMax = Vector2.one;
            headerRect.sizeDelta = Vector2.zero;
            
            _titleText = UIFactory.CreateTitle(header.transform, "Title", "TOURNAMENTS", 42f);
            var titleRect = _titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.05f, 0);
            titleRect.anchorMax = new Vector2(0.5f, 1);
            titleRect.sizeDelta = Vector2.zero;
            _titleText.alignment = TextAlignmentOptions.MidlineLeft;
            _titleText.color = theme.accentColor;
            
            // Back button
            var backBtn = UIFactory.CreateButton(header.transform, "Back", "← BACK", OnBackToLobby);
            var backRect = backBtn.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.85f, 0.2f);
            backRect.anchorMax = new Vector2(0.98f, 0.8f);
            backRect.sizeDelta = Vector2.zero;
            backBtn.GetComponent<Image>().color = theme.dangerColor;
        }
        
        private void BuildListPanel()
        {
            var theme = Theme.Current;
            
            _listPanel = UIFactory.CreatePanel(_canvas.transform, "ListPanel", Color.clear);
            var panelRect = _listPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.02f, 0.02f);
            panelRect.anchorMax = new Vector2(0.48f, 0.88f);
            panelRect.sizeDelta = Vector2.zero;
            
            // Scroll view
            var scrollView = new GameObject("ScrollView");
            scrollView.transform.SetParent(_listPanel.transform, false);
            var scrollRect = scrollView.AddComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.sizeDelta = Vector2.zero;
            
            var scroll = scrollView.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            
            // Viewport
            var viewport = UIFactory.CreatePanel(scrollView.transform, "Viewport", Color.clear);
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            scroll.viewport = viewportRect;
            
            // Content
            var content = UIFactory.CreatePanel(viewport.transform, "Content", Color.clear);
            _tournamentListContainer = content.transform;
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 0);
            
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10;
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.childControlHeight = false;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            
            var csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            scroll.content = contentRect;
        }
        
        private void BuildDetailPanel()
        {
            var theme = Theme.Current;
            
            _detailPanel = UIFactory.CreatePanel(_canvas.transform, "DetailPanel", theme.cardPanelColor);
            var panelRect = _detailPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.52f, 0.02f);
            panelRect.anchorMax = new Vector2(0.98f, 0.88f);
            panelRect.sizeDelta = Vector2.zero;
            
            var vlg = _detailPanel.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 15;
            vlg.padding = new RectOffset(30, 30, 30, 30);
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlHeight = false;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            
            // Tournament name
            _detailTitleText = UIFactory.CreateTitle(_detailPanel.transform, "Title", "Tournament Name", 32f);
            _detailTitleText.GetOrAddComponent<LayoutElement>().preferredHeight = 45;
            _detailTitleText.color = theme.accentColor;
            
            // Type and status row
            var row1 = CreateDetailRow("TypeStatus", "");
            _detailTypeText = row1.Find("Value").GetComponent<TextMeshProUGUI>();
            
            // Players
            var row2 = CreateDetailRow("Players", "Registered: 0/10");
            _detailPlayersText = row2.Find("Value").GetComponent<TextMeshProUGUI>();
            
            // Entry fee
            var row3 = CreateDetailRow("Entry", "Entry Fee: 0");
            _detailEntryText = row3.Find("Value").GetComponent<TextMeshProUGUI>();
            
            // Prize pool
            var row4 = CreateDetailRow("Prize", "Prize Pool: 0");
            _detailPrizeText = row4.Find("Value").GetComponent<TextMeshProUGUI>();
            
            // Blinds
            var row5 = CreateDetailRow("Blinds", "Blinds: 50/100");
            _detailBlindsText = row5.Find("Value").GetComponent<TextMeshProUGUI>();
            
            // Status
            var row6 = CreateDetailRow("Status", "Status: Registering");
            _detailStatusText = row6.Find("Value").GetComponent<TextMeshProUGUI>();
            
            // Requirements
            _detailRequirementsText = UIFactory.CreateText(_detailPanel.transform, "Requirements", "", 16f, theme.textSecondary);
            _detailRequirementsText.GetOrAddComponent<LayoutElement>().preferredHeight = 60;
            
            // Spacer
            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(_detailPanel.transform, false);
            spacer.AddComponent<RectTransform>();
            spacer.AddComponent<LayoutElement>().flexibleHeight = 1;
            
            // Buttons row
            var buttonsRow = UIFactory.CreatePanel(_detailPanel.transform, "Buttons", Color.clear);
            buttonsRow.GetOrAddComponent<LayoutElement>().preferredHeight = 60;
            var btnHlg = buttonsRow.AddComponent<HorizontalLayoutGroup>();
            btnHlg.spacing = 20;
            btnHlg.childAlignment = TextAnchor.MiddleCenter;
            btnHlg.childControlWidth = false;
            btnHlg.childForceExpandWidth = false;
            
            _registerButton = UIFactory.CreateButton(buttonsRow.transform, "Register", "REGISTER", OnRegisterClick).GetComponent<Button>();
            _registerButton.GetComponent<RectTransform>().sizeDelta = new Vector2(180, 50);
            _registerButton.GetComponent<Image>().color = theme.primaryColor;
            
            _unregisterButton = UIFactory.CreateButton(buttonsRow.transform, "Unregister", "UNREGISTER", OnUnregisterClick).GetComponent<Button>();
            _unregisterButton.GetComponent<RectTransform>().sizeDelta = new Vector2(180, 50);
            _unregisterButton.GetComponent<Image>().color = theme.dangerColor;
            
            _backButton = UIFactory.CreateButton(buttonsRow.transform, "Close", "CLOSE", () => _detailPanel.SetActive(false)).GetComponent<Button>();
            _backButton.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 50);
        }
        
        private Transform CreateDetailRow(string name, string defaultValue)
        {
            var theme = Theme.Current;
            
            var row = UIFactory.CreatePanel(_detailPanel.transform, $"Row_{name}", Color.clear);
            row.GetOrAddComponent<LayoutElement>().preferredHeight = 30;
            
            var text = UIFactory.CreateText(row.transform, "Value", defaultValue, 18f, theme.textPrimary);
            text.name = "Value";
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            
            return row.transform;
        }
        
        private void BuildLoadingPanel()
        {
            var theme = Theme.Current;
            
            _loadingPanel = UIFactory.CreatePanel(_canvas.transform, "LoadingPanel", new Color(0, 0, 0, 0.7f));
            var panelRect = _loadingPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            
            var text = UIFactory.CreateTitle(_loadingPanel.transform, "Loading", "Loading tournaments...", 28f);
            text.alignment = TextAlignmentOptions.Center;
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.3f, 0.4f);
            textRect.anchorMax = new Vector2(0.7f, 0.6f);
            textRect.sizeDelta = Vector2.zero;
        }
        
        private void LoadTournaments()
        {
            _loadingPanel.SetActive(true);
            
            // TODO: Call GameService to get tournaments
            // For now, show empty list
            _loadingPanel.SetActive(false);
            
            // Mock data for testing UI
            _tournaments = new List<TournamentInfo>
            {
                new TournamentInfo
                {
                    id = "t1",
                    name = "Daily Freeroll",
                    type = "freeroll",
                    status = "registering",
                    registeredCount = 45,
                    maxPlayers = 100,
                    startingChips = 5000,
                    entryFee = 0,
                    prizePool = 10000,
                    xpPrizePool = 500,
                    minLevel = 1
                },
                new TournamentInfo
                {
                    id = "t2",
                    name = "High Roller Sit & Go",
                    type = "sit_n_go",
                    status = "registering",
                    registeredCount = 6,
                    maxPlayers = 9,
                    startingChips = 10000,
                    entryFee = 5000,
                    prizePool = 45000,
                    minLevel = 10
                },
                new TournamentInfo
                {
                    id = "t3",
                    name = "Weekend Special",
                    type = "scheduled",
                    status = "registering",
                    registeredCount = 156,
                    maxPlayers = 500,
                    startingChips = 15000,
                    entryFee = 1000,
                    prizePool = 500000,
                    minLevel = 5
                }
            };
            
            UpdateTournamentList();
        }
        
        private void UpdateTournamentList()
        {
            // Clear existing items
            foreach (var item in _tournamentItems)
            {
                Destroy(item);
            }
            _tournamentItems.Clear();
            
            // Create new items
            foreach (var tournament in _tournaments)
            {
                var item = CreateTournamentItem(tournament);
                _tournamentItems.Add(item);
            }
        }
        
        private GameObject CreateTournamentItem(TournamentInfo tournament)
        {
            var theme = Theme.Current;
            
            var item = UIFactory.CreatePanel(_tournamentListContainer, $"Tournament_{tournament.id}", theme.cardPanelColor);
            item.GetOrAddComponent<LayoutElement>().preferredHeight = 100;
            
            var btn = item.AddComponent<Button>();
            btn.targetGraphic = item.GetComponent<Image>();
            btn.onClick.AddListener(() => SelectTournament(tournament));
            
            // Name
            var nameText = UIFactory.CreateTitle(item.transform, "Name", tournament.name, 22f);
            var nameRect = nameText.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.02f, 0.55f);
            nameRect.anchorMax = new Vector2(0.7f, 0.95f);
            nameRect.sizeDelta = Vector2.zero;
            nameText.color = theme.textPrimary;
            
            // Type badge
            var typeText = UIFactory.CreateText(item.transform, "Type", GetTypeName(tournament.type), 14f, theme.primaryColor);
            var typeRect = typeText.GetComponent<RectTransform>();
            typeRect.anchorMin = new Vector2(0.75f, 0.6f);
            typeRect.anchorMax = new Vector2(0.98f, 0.9f);
            typeRect.sizeDelta = Vector2.zero;
            typeText.alignment = TextAlignmentOptions.Right;
            typeText.fontStyle = FontStyles.Bold;
            
            // Players
            var playersText = UIFactory.CreateText(item.transform, "Players", 
                $"{tournament.registeredCount}/{tournament.maxPlayers} players", 16f, theme.textSecondary);
            var playersRect = playersText.GetComponent<RectTransform>();
            playersRect.anchorMin = new Vector2(0.02f, 0.1f);
            playersRect.anchorMax = new Vector2(0.4f, 0.45f);
            playersRect.sizeDelta = Vector2.zero;
            
            // Prize pool
            var prizeText = UIFactory.CreateText(item.transform, "Prize", 
                $"Prize: {tournament.prizePool:N0}", 16f, theme.accentColor);
            var prizeRect = prizeText.GetComponent<RectTransform>();
            prizeRect.anchorMin = new Vector2(0.42f, 0.1f);
            prizeRect.anchorMax = new Vector2(0.7f, 0.45f);
            prizeRect.sizeDelta = Vector2.zero;
            
            // Entry fee
            var entryText = UIFactory.CreateText(item.transform, "Entry", 
                tournament.entryFee > 0 ? $"Entry: {tournament.entryFee:N0}" : "FREE", 16f, 
                tournament.entryFee > 0 ? theme.textSecondary : theme.successColor);
            var entryRect = entryText.GetComponent<RectTransform>();
            entryRect.anchorMin = new Vector2(0.72f, 0.1f);
            entryRect.anchorMax = new Vector2(0.98f, 0.45f);
            entryRect.sizeDelta = Vector2.zero;
            entryText.alignment = TextAlignmentOptions.Right;
            
            return item;
        }
        
        private string GetTypeName(string type)
        {
            return type?.ToLower() switch
            {
                "sit_n_go" or "sitngo" => "Sit & Go",
                "scheduled" => "Scheduled",
                "freeroll" => "Freeroll",
                "satellite" => "Satellite",
                _ => type ?? "Tournament"
            };
        }
        
        private string GetStatusName(string status)
        {
            return status?.ToLower() switch
            {
                "registering" => "Registering",
                "starting" => "Starting Soon",
                "in_progress" => "In Progress",
                "final_table" => "Final Table",
                "completed" => "Completed",
                "cancelled" => "Cancelled",
                _ => status ?? "Unknown"
            };
        }
        
        private void SelectTournament(TournamentInfo tournament)
        {
            _selectedTournament = tournament;
            _detailPanel.SetActive(true);
            
            var theme = Theme.Current;
            
            _detailTitleText.text = tournament.name;
            _detailTypeText.text = GetTypeName(tournament.type);
            _detailStatusText.text = $"Status: {GetStatusName(tournament.status)}";
            _detailPlayersText.text = $"Players: {tournament.registeredCount}/{tournament.maxPlayers}";
            _detailEntryText.text = tournament.entryFee > 0 ? $"Entry Fee: {tournament.entryFee:N0} chips" : "Entry: FREE";
            _detailPrizeText.text = $"Prize Pool: {tournament.prizePool:N0} chips";
            _detailBlindsText.text = $"Starting Chips: {tournament.startingChips:N0}";
            
            string reqs = "";
            if (tournament.minLevel > 1)
                reqs += $"• Minimum Level: {tournament.minLevel}\n";
            if (tournament.minChips > 0)
                reqs += $"• Minimum Chips: {tournament.minChips:N0}\n";
            if (tournament.requiredItems?.Count > 0)
                reqs += $"• Required Items: {tournament.requiredItems.Count}\n";
            if (tournament.sidePotRequired)
                reqs += $"• Side pot item required\n";
            
            _detailRequirementsText.text = string.IsNullOrEmpty(reqs) ? "No special requirements" : reqs;
            
            // Check if registered
            _isRegistered = false; // TODO: Check with server
            UpdateRegisterButtons();
        }
        
        private void UpdateRegisterButtons()
        {
            bool canRegister = _selectedTournament?.status == "registering";
            
            _registerButton.gameObject.SetActive(!_isRegistered && canRegister);
            _unregisterButton.gameObject.SetActive(_isRegistered && canRegister);
        }
        
        private void OnRegisterClick()
        {
            if (_selectedTournament == null) return;
            
            Debug.Log($"Registering for tournament: {_selectedTournament.name}");
            // TODO: Call GameService to register
            _isRegistered = true;
            UpdateRegisterButtons();
        }
        
        private void OnUnregisterClick()
        {
            if (_selectedTournament == null) return;
            
            Debug.Log($"Unregistering from tournament: {_selectedTournament.name}");
            // TODO: Call GameService to unregister
            _isRegistered = false;
            UpdateRegisterButtons();
        }
        
        private void OnBackToLobby()
        {
            SceneManager.LoadScene("LobbyScene");
        }
    }
}

