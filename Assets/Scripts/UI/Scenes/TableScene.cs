using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using PokerClient.Core;
using PokerClient.UI;
using PokerClient.UI.Components;
using PokerClient.Networking;
using System.Collections.Generic;
using System;

namespace PokerClient.UI.Scenes
{
    /// <summary>
    /// The actual poker table where gameplay happens.
    /// Displays cards, players, chips, and action buttons.
    /// </summary>
    public class TableScene : MonoBehaviour
    {
        [Header("Table Layout")]
        private PokerTableView _tableView;
        
        [Header("Action Panel")]
        private GameObject actionPanel;
        private Button foldButton;
        private Button checkButton;
        private Button callButton;
        private Button betButton;
        private Button raiseButton;
        private Button allInButton;
        private Slider betSlider;
        private TextMeshProUGUI betAmountText;
        private TextMeshProUGUI callAmountText;
        
        [Header("Info Panel")]
        private TextMeshProUGUI potText;
        private TextMeshProUGUI phaseText;
        private TextMeshProUGUI timerText;
        private TextMeshProUGUI blindTimerText;
        
        [Header("My Chips Display")]
        private GameObject _myChipsPanel;
        private TextMeshProUGUI _myChipsText;
        private int _lastDisplayedChips = -1;
        
        [Header("Menu")]
        private GameObject menuPanel;
        private Button leaveButton;
        private Button inviteButton;
        private Button chatButton;
        
        [Header("Result Panel")]
        private GameObject resultPanel;
        private TextMeshProUGUI resultText;
        
        private Canvas _canvas;
        private GameService _gameService;
        private TableState _currentState;
        private bool _isMyTurn = false;
        private int _minBet = 0;
        private int _maxBet = 0;
        private int _callAmount = 0;
        private int _mySeatIndex = -1;
        
        // Bot UI
        private bool _isTableCreator = false;
        private bool _isPracticeMode = false;
        private bool _isSimulation = false;
        private Button _addBotsButton;
        private GameObject _botPanel;
        private GameObject _botApprovalPopup;
        private int _pendingBotSeat = -1;
        
        // Countdown UI
        private GameObject _countdownOverlay;
        private TextMeshProUGUI _countdownNumber;
        private TextMeshProUGUI _countdownLabel;
        private int _lastCountdownValue = -1;
        
        // Ready-Up UI
        private GameObject _startGameButton;
        private GameObject _readyOverlay;
        private TextMeshProUGUI _readyTimerText;
        private TextMeshProUGUI _readyCountText;
        private bool _hasClickedReady = false;
        
        // Action Announcement
        private GameObject _actionAnnouncement;
        private TextMeshProUGUI _actionText;
        private string _lastActionPlayerId = null;
        
        // Local turn timer (counts down between server updates)
        private float _localTurnTimeRemaining = 0f;
        private bool _isGamePhaseActive = false;
        private Color _timerNormalColor;
        private Color _timerUrgentColor;
        
        // Local blind timer (counts down between server updates)
        private float _localBlindTimeRemaining = -1f;
        private bool _blindIncreaseEnabled = false;
        private int _currentBlindLevel = 1;
        private int _currentSmallBlind = 0;
        private int _currentBigBlind = 0;
        
        // Track phase changes to show phase announcements
        private string _previousPhase = "";
        private bool _playedReadyToRumble = false; // Track if we've played the ready to rumble sound
        private float _rumbleStartTime = 0f; // When rumble started playing
        private const float RUMBLE_DURATION = 7f; // Ready to Rumble audio is 7 seconds
        
        private void Start()
        {
            _gameService = GameService.Instance;
            if (_gameService == null || !_gameService.IsInGame)
            {
                Debug.LogError($"Not in a game! Going back to lobby. IsInGame={_gameService?.IsInGame}, TableId={_gameService?.CurrentTableId}");
                SceneManager.LoadScene("LobbyScene");
                return;
            }
            
            // Subscribe to events
            _gameService.OnTableStateUpdate += OnTableStateUpdate;
            _gameService.OnPlayerActionReceived += OnPlayerActionReceived;
            _gameService.OnPlayerJoinedTable += OnPlayerJoinedTable;
            _gameService.OnPlayerLeftTable += OnPlayerLeftTable;
            _gameService.OnSpectatorJoined += OnSpectatorJoined;
            _gameService.OnSpectatorLeft += OnSpectatorLeft;
            _gameService.OnHandComplete += OnHandComplete;
            _gameService.OnPlayerEliminated += OnPlayerEliminated;
            _gameService.OnGameOver += OnGameOver;
            _gameService.OnTableLeft += OnTableLeft;
            
            // Subscribe to bot events
            if (SocketManager.Instance != null)
            {
                SocketManager.Instance.OnBotInvitePending += OnBotInvitePending;
                SocketManager.Instance.OnBotJoined += OnBotJoined;
                SocketManager.Instance.OnBotRejected += OnBotRejected;
            }
            
            BuildScene();
            
            // Start table music
            AudioManager.Instance?.PlayTableMusic();
            
            // Apply initial state if we have it
            if (_gameService.CurrentTableState != null)
            {
                OnTableStateUpdate(_gameService.CurrentTableState);
            }
            
            // Initialize timer colors
            _timerNormalColor = Theme.Current.textPrimary;
            _timerUrgentColor = Theme.Current.dangerColor;
        }
        
        private void Update()
        {
            // Local countdown for turn timer (smoother than waiting for server updates)
            if (_isGamePhaseActive && _localTurnTimeRemaining > 0 && timerText != null)
            {
                _localTurnTimeRemaining -= Time.deltaTime;
                
                int displaySeconds = Mathf.CeilToInt(_localTurnTimeRemaining);
                if (displaySeconds < 0) displaySeconds = 0;
                
                timerText.text = displaySeconds.ToString();
                timerText.gameObject.SetActive(true);
                
                // Pulsing effect when 10 seconds or less
                if (_localTurnTimeRemaining <= 10f)
                {
                    // Pulse between normal and urgent colors
                    float pulse = (Mathf.Sin(Time.time * 6f) + 1f) / 2f; // 0-1 oscillating
                    timerText.color = Color.Lerp(_timerNormalColor, _timerUrgentColor, pulse);
                    
                    // Scale pulse for extra urgency
                    float scale = 1f + (0.15f * pulse);
                    timerText.transform.localScale = new Vector3(scale, scale, 1f);
                }
                else
                {
                    timerText.color = _timerNormalColor;
                    timerText.transform.localScale = Vector3.one;
                }
            }
            else if (timerText != null && timerText.gameObject.activeSelf && !_isGamePhaseActive)
            {
                timerText.gameObject.SetActive(false);
                timerText.transform.localScale = Vector3.one;
            }
            
            // Local countdown for blind timer (smoother UI updates)
            if (_blindIncreaseEnabled && _localBlindTimeRemaining > 0 && blindTimerText != null)
            {
                _localBlindTimeRemaining -= Time.deltaTime;
                
                int totalSeconds = Mathf.CeilToInt(_localBlindTimeRemaining);
                if (totalSeconds < 0) totalSeconds = 0;
                
                int minutes = totalSeconds / 60;
                int seconds = totalSeconds % 60;
                
                blindTimerText.text = $"Blinds: {_currentSmallBlind}/{_currentBigBlind} (Lv.{_currentBlindLevel}) - Next in {minutes}:{seconds:D2}";
                blindTimerText.gameObject.SetActive(true);
                
                // Color warning when under 1 minute
                if (totalSeconds <= 60)
                {
                    blindTimerText.color = Theme.Current.accentColor;
                }
                else
                {
                    blindTimerText.color = Theme.Current.textSecondary;
                }
            }
        }
        
        private void OnDestroy()
        {
            if (_gameService != null)
            {
                _gameService.OnTableStateUpdate -= OnTableStateUpdate;
                _gameService.OnPlayerActionReceived -= OnPlayerActionReceived;
                _gameService.OnPlayerJoinedTable -= OnPlayerJoinedTable;
                _gameService.OnPlayerLeftTable -= OnPlayerLeftTable;
                _gameService.OnSpectatorJoined -= OnSpectatorJoined;
                _gameService.OnSpectatorLeft -= OnSpectatorLeft;
                _gameService.OnHandComplete -= OnHandComplete;
                _gameService.OnPlayerEliminated -= OnPlayerEliminated;
                _gameService.OnGameOver -= OnGameOver;
                _gameService.OnTableLeft -= OnTableLeft;
            }
            
            // Unsubscribe from bot events
            if (SocketManager.Instance != null)
            {
                SocketManager.Instance.OnBotInvitePending -= OnBotInvitePending;
                SocketManager.Instance.OnBotJoined -= OnBotJoined;
                SocketManager.Instance.OnBotRejected -= OnBotRejected;
            }
        }
        
        private void OnBotInvitePending(BotInvitePendingData data)
        {
            Debug.Log($"Bot invite pending: {data.botName} by {data.invitedBy}");
            
            // If we're not the creator, show approval popup
            if (!_isTableCreator)
            {
                ShowBotApprovalPopup(data.botName, data.seatIndex);
            }
        }
        
        private void OnBotJoined(BotJoinedData data)
        {
            Debug.Log($"Bot joined: {data.botName} at seat {data.seatIndex}");
            // Table state will update automatically
        }
        
        private void OnBotRejected(BotRejectedData data)
        {
            Debug.Log($"Bot {data.botName} rejected by {data.rejectedBy}");
            _botApprovalPopup?.SetActive(false);
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
            
            // FULL-SCREEN BACKGROUND - covers entire canvas so no Unity blue shows through
            var fullBg = UIFactory.CreatePanel(_canvas.transform, "FullScreenBackground", theme.backgroundColor);
            var fullBgRect = fullBg.GetComponent<RectTransform>();
            fullBgRect.anchorMin = Vector2.zero;
            fullBgRect.anchorMax = Vector2.one;
            fullBgRect.sizeDelta = Vector2.zero;
            fullBg.transform.SetAsFirstSibling(); // Ensure it's behind everything
            
            // Table View (the main poker table with seats)
            BuildTableView();
            
            // Action Announcement (shows what player just did)
            BuildActionAnnouncement();
            
            // Start Game button (for table creator) - built early so dialogs appear on top
            BuildStartGameButton();
            
            // Top Info Bar
            BuildTopBar();
            
            // Action Panel (bottom)
            BuildActionPanel();
            
            // Side Menu (includes bot panel which should be on top of start button)
            BuildSideMenu();
            
            // Result Panel (shown after hands)
            BuildResultPanel();
            
            // My Chips Display - BUILD LAST so it's on top of everything
            BuildMyChipsPanel();
            
            // Countdown Overlay (shown before game starts)
            BuildCountdownOverlay();
            
            // Ready overlay (for ready-up phase) - on top of everything except dialogs
            BuildReadyOverlay();
            
            // Hide action panel initially
            actionPanel.SetActive(false);
        }
        
        private void BuildTableView()
        {
            var tableViewObj = new GameObject("PokerTableView");
            tableViewObj.transform.SetParent(_canvas.transform, false);
            _tableView = tableViewObj.AddComponent<PokerTableView>();
            _tableView.Initialize(9); // 9 max players
            
            var rect = tableViewObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 0.18f); // Leave MORE room for action panel at bottom
            rect.anchorMax = new Vector2(1, 0.88f); // Leave room for top bar
            rect.sizeDelta = Vector2.zero;
        }
        
        private void BuildActionAnnouncement()
        {
            var theme = Theme.Current;
            
            // Container positioned at center-top of the table area
            _actionAnnouncement = UIFactory.CreatePanel(_canvas.transform, "ActionAnnouncement", new Color(0, 0, 0, 0.7f));
            var rect = _actionAnnouncement.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.75f);
            rect.anchorMax = new Vector2(0.5f, 0.75f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(300, 40);
            rect.anchoredPosition = Vector2.zero;
            
            // Round corners effect (optional - just use padding)
            _actionAnnouncement.GetComponent<Image>().pixelsPerUnitMultiplier = 1;
            
            // Action text
            _actionText = UIFactory.CreateText(_actionAnnouncement.transform, "ActionText", "", 18f, Color.white);
            _actionText.alignment = TextAlignmentOptions.Center;
            _actionText.fontStyle = FontStyles.Bold;
            var textRect = _actionText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            
            // Start hidden
            _actionAnnouncement.SetActive(false);
        }
        
        private void ShowActionAnnouncement(string playerName, string action, int amount = 0)
        {
            if (_actionAnnouncement == null || _actionText == null) return;
            
            string message;
            Color actionColor = Color.white;
            bool isYou = playerName == "You";
            
            switch (action.ToLower())
            {
                case "fold":
                    message = isYou ? "You folded" : $"{playerName} folds";
                    actionColor = new Color(0.7f, 0.7f, 0.7f); // Gray
                    break;
                case "check":
                    message = isYou ? "You checked" : $"{playerName} checks";
                    actionColor = new Color(0.5f, 0.8f, 1f); // Light blue
                    break;
                case "call":
                    message = isYou 
                        ? (amount > 0 ? $"You called ${amount:N0}" : "You called")
                        : (amount > 0 ? $"{playerName} calls ${amount:N0}" : $"{playerName} calls");
                    actionColor = new Color(0.5f, 1f, 0.5f); // Light green
                    break;
                case "bet":
                    message = isYou ? $"You bet ${amount:N0}" : $"{playerName} bets ${amount:N0}";
                    actionColor = new Color(1f, 0.9f, 0.3f); // Yellow/gold
                    break;
                case "raise":
                    message = isYou ? $"You raised to ${amount:N0}" : $"{playerName} raises to ${amount:N0}";
                    actionColor = new Color(1f, 0.6f, 0.2f); // Orange
                    break;
                case "allin":
                case "all-in":
                case "all_in":
                    message = isYou ? $"You went ALL IN! ${amount:N0}" : $"{playerName} is ALL IN! ${amount:N0}";
                    actionColor = new Color(1f, 0.3f, 0.3f); // Red
                    break;
                default:
                    message = $"{playerName} {action}";
                    break;
            }
            
            _actionText.text = message;
            _actionText.color = actionColor;
            _actionAnnouncement.SetActive(true);
            
            // Auto-hide after 3 seconds
            CancelInvoke(nameof(HideActionAnnouncement));
            Invoke(nameof(HideActionAnnouncement), 3f);
        }
        
        private void HideActionAnnouncement()
        {
            if (_actionAnnouncement != null)
                _actionAnnouncement.SetActive(false);
        }
        
        private void ShowPhaseAnnouncement(string phase)
        {
            if (_actionAnnouncement == null || _actionText == null) return;
            
            string message;
            Color phaseColor;
            
            switch (phase.ToLower())
            {
                case "flop":
                    message = "--- FLOP ---";
                    phaseColor = new Color(0.3f, 0.8f, 1f); // Cyan
                    break;
                case "turn":
                    message = "--- TURN ---";
                    phaseColor = new Color(1f, 0.8f, 0.3f); // Gold
                    break;
                case "river":
                    message = "--- RIVER ---";
                    phaseColor = new Color(1f, 0.5f, 0.3f); // Orange-red
                    break;
                default:
                    return; // Don't show for other phases
            }
            
            _actionText.text = message;
            _actionText.color = phaseColor;
            _actionAnnouncement.SetActive(true);
            
            // Shorter display for phase announcements (1.5 seconds)
            CancelInvoke(nameof(HideActionAnnouncement));
            Invoke(nameof(HideActionAnnouncement), 1.5f);
        }
        
        private void BuildTopBar()
        {
            var theme = Theme.Current;
            
            // Slim, semi-transparent top bar - just menu and phase/timer
            var topBar = UIFactory.CreatePanel(_canvas.transform, "TopBar", new Color(0.1f, 0.1f, 0.1f, 0.7f));
            var topRect = topBar.GetComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0, 1);
            topRect.anchorMax = new Vector2(1, 1);
            topRect.pivot = new Vector2(0.5f, 1);
            topRect.sizeDelta = new Vector2(0, 45);
            topRect.anchoredPosition = Vector2.zero;
            
            // Menu button (left)
            var menuBtn = UIFactory.CreateButton(topBar.transform, "MenuBtn", "G��", () => menuPanel.SetActive(!menuPanel.activeSelf));
            var menuRect = menuBtn.GetComponent<RectTransform>();
            menuRect.anchorMin = new Vector2(0, 0.5f);
            menuRect.anchorMax = new Vector2(0, 0.5f);
            menuRect.pivot = new Vector2(0, 0.5f);
            menuRect.anchoredPosition = new Vector2(10, 0);
            menuRect.sizeDelta = new Vector2(45, 35);
            
            // Pot text - hidden, we use the one on the table
            potText = UIFactory.CreateText(topBar.transform, "PotText", "", 1f, Color.clear);
            potText.gameObject.SetActive(false);
            
            // Phase (center)
            phaseText = UIFactory.CreateTitle(topBar.transform, "PhaseText", "Waiting...", 18f);
            var phaseRect = phaseText.GetComponent<RectTransform>();
            phaseRect.anchorMin = new Vector2(0.5f, 0.5f);
            phaseRect.anchorMax = new Vector2(0.5f, 0.5f);
            phaseRect.sizeDelta = new Vector2(180, 35);
            phaseText.alignment = TextAlignmentOptions.Center;
            
            // Timer (right) - shows countdown during player turns
            timerText = UIFactory.CreateTitle(topBar.transform, "TimerText", "", 28f);
            var timerRect = timerText.GetComponent<RectTransform>();
            timerRect.anchorMin = new Vector2(1, 0.5f);
            timerRect.anchorMax = new Vector2(1, 0.5f);
            timerRect.pivot = new Vector2(1, 0.5f);
            timerRect.anchoredPosition = new Vector2(-15, 0);
            timerRect.sizeDelta = new Vector2(70, 40);
            timerText.color = theme.textPrimary;
            timerText.fontStyle = FontStyles.Bold;
            timerText.alignment = TextAlignmentOptions.Center;
            
            // Blind timer (below top bar, left side) - shows when blinds will increase
            blindTimerText = UIFactory.CreateText(_canvas.transform, "BlindTimerText", "", 14f, theme.textSecondary);
            var blindRect = blindTimerText.GetComponent<RectTransform>();
            blindRect.anchorMin = new Vector2(0, 1);
            blindRect.anchorMax = new Vector2(0, 1);
            blindRect.pivot = new Vector2(0, 1);
            blindRect.anchoredPosition = new Vector2(10, -50);
            blindRect.sizeDelta = new Vector2(200, 24);
            blindTimerText.alignment = TextAlignmentOptions.Left;
            blindTimerText.gameObject.SetActive(false); // Hidden by default (shown if blinds increase enabled)
        }
        
        private void BuildActionPanel()
        {
            var theme = Theme.Current;
            
            actionPanel = UIFactory.CreatePanel(_canvas.transform, "ActionPanel", theme.cardPanelColor);
            var panelRect = actionPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0);
            panelRect.anchorMax = new Vector2(1, 0);
            panelRect.pivot = new Vector2(0.5f, 0);
            panelRect.sizeDelta = new Vector2(0, 100); // Reduced height
            panelRect.anchoredPosition = Vector2.zero;
            
            // Ensure action panel renders on top with its own canvas
            var actionCanvas = actionPanel.AddComponent<Canvas>();
            actionCanvas.overrideSorting = true;
            actionCanvas.sortingOrder = 100;
            actionPanel.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            var hlg = actionPanel.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8; // Reduced spacing
            hlg.padding = new RectOffset(10, 10, 8, 8); // Reduced padding
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childForceExpandWidth = false;
            
            // Fold Button - smaller
            foldButton = UIFactory.CreateButton(actionPanel.transform, "FoldBtn", "FOLD", OnFoldClick).GetComponent<Button>();
            foldButton.GetComponent<RectTransform>().sizeDelta = new Vector2(70, 50);
            foldButton.GetComponent<Image>().color = theme.dangerColor;
            
            // Check Button - smaller
            checkButton = UIFactory.CreateButton(actionPanel.transform, "CheckBtn", "CHECK", OnCheckClick).GetComponent<Button>();
            checkButton.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 50);
            checkButton.GetComponent<Image>().color = theme.successColor;
            
            // Call Button - smaller
            var callContainer = UIFactory.CreatePanel(actionPanel.transform, "CallContainer", Color.clear);
            callContainer.GetOrAddComponent<LayoutElement>().preferredWidth = 90;
            
            callButton = UIFactory.CreateButton(callContainer.transform, "CallBtn", "CALL", OnCallClick).GetComponent<Button>();
            var callRect = callButton.GetComponent<RectTransform>();
            callRect.anchorMin = new Vector2(0, 0.25f);
            callRect.anchorMax = new Vector2(1, 1);
            callRect.sizeDelta = Vector2.zero;
            callButton.GetComponent<Image>().color = theme.primaryColor;
            
            callAmountText = UIFactory.CreateText(callContainer.transform, "CallAmount", "", 16f, theme.accentColor);
            callAmountText.fontStyle = FontStyles.Bold;
            var callAmtRect = callAmountText.GetComponent<RectTransform>();
            callAmtRect.anchorMin = new Vector2(0, 0);
            callAmtRect.anchorMax = new Vector2(1, 0.35f);
            callAmtRect.sizeDelta = Vector2.zero;
            callAmountText.alignment = TextAlignmentOptions.Center;
            
            // Bet/Raise Slider Section - more compact
            var betSection = UIFactory.CreatePanel(actionPanel.transform, "BetSection", Color.clear);
            betSection.GetOrAddComponent<LayoutElement>().preferredWidth = 200; // Reduced
            
            // Slider row
            var sliderRow = UIFactory.CreatePanel(betSection.transform, "SliderRow", Color.clear);
            var sliderRect = sliderRow.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0, 0.5f);
            sliderRect.anchorMax = new Vector2(1, 1);
            sliderRect.sizeDelta = Vector2.zero;
            
            betSlider = CreateBetSlider(sliderRow.transform);
            betSlider.onValueChanged.AddListener(OnBetSliderChanged);
            
            // Amount display - LARGE and clear so player can see bet amount
            betAmountText = UIFactory.CreateTitle(betSection.transform, "BetAmount", "0", 28f);
            var amtRect = betAmountText.GetComponent<RectTransform>();
            amtRect.anchorMin = new Vector2(0, 0);
            amtRect.anchorMax = new Vector2(1, 0.5f);
            amtRect.sizeDelta = Vector2.zero;
            betAmountText.alignment = TextAlignmentOptions.Center;
            betAmountText.fontStyle = FontStyles.Bold;
            betAmountText.color = theme.accentColor;
            
            // Bet Button - smaller
            betButton = UIFactory.CreateButton(actionPanel.transform, "BetBtn", "BET", OnBetClick).GetComponent<Button>();
            betButton.GetComponent<RectTransform>().sizeDelta = new Vector2(70, 50);
            betButton.GetComponent<Image>().color = theme.accentColor;
            
            // Raise Button
            raiseButton = UIFactory.CreateButton(actionPanel.transform, "RaiseBtn", "RAISE", OnRaiseClick).GetComponent<Button>();
            raiseButton.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 70);
            raiseButton.GetComponent<Image>().color = theme.accentColor;
            
            // All-In Button
            allInButton = UIFactory.CreateButton(actionPanel.transform, "AllInBtn", "ALL IN", OnAllInClick).GetComponent<Button>();
            allInButton.GetComponent<RectTransform>().sizeDelta = new Vector2(140, 70);
            allInButton.GetComponent<Image>().color = new Color(0.8f, 0.2f, 0.5f);
        }
        
        private void BuildMyChipsPanel()
        {
            var theme = Theme.Current;
            
            // Create a stylish chips display panel - bottom RIGHT, above action panel
            _myChipsPanel = UIFactory.CreatePanel(_canvas.transform, "MyChipsPanel", new Color(0.05f, 0.05f, 0.1f, 0.95f));
            var panelRect = _myChipsPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1, 0);  // Bottom-right anchor
            panelRect.anchorMax = new Vector2(1, 0);
            panelRect.pivot = new Vector2(1, 0);      // Right-aligned pivot
            panelRect.anchoredPosition = new Vector2(-10, 5); // 10px from right, 5px from bottom - AT THE BOTTOM
            panelRect.sizeDelta = new Vector2(170, 50);
            
            // Ensure it renders ABOVE everything - use high sorting order
            var chipsPanelCanvas = _myChipsPanel.AddComponent<Canvas>();
            chipsPanelCanvas.overrideSorting = true;
            chipsPanelCanvas.sortingOrder = 200; // Very high to ensure visibility
            _myChipsPanel.AddComponent<UnityEngine.UI.GraphicRaycaster>();
            
            // Make it the last sibling so it draws on top
            _myChipsPanel.transform.SetAsLastSibling();
            
            // Add a subtle gold border/glow effect
            var outline = _myChipsPanel.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = new Color(1f, 0.85f, 0.2f, 0.7f);
            outline.effectDistance = new Vector2(2, 2);
            
            // Chip icon/label - right aligned for bottom-right position
            var chipLabel = UIFactory.CreateText(_myChipsPanel.transform, "ChipLabel", "MY CHIPS", 10f, new Color(0.8f, 0.8f, 0.8f));
            var labelRect = chipLabel.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.55f);
            labelRect.anchorMax = new Vector2(1, 1);
            labelRect.sizeDelta = Vector2.zero;
            labelRect.offsetMin = new Vector2(8, 0);
            labelRect.offsetMax = new Vector2(-8, -3);
            chipLabel.alignment = TextAlignmentOptions.Right;
            chipLabel.fontStyle = FontStyles.Bold;
            
            // Large chip amount with gold color - right aligned
            _myChipsText = UIFactory.CreateTitle(_myChipsPanel.transform, "ChipAmount", "---", 24f);
            var amountRect = _myChipsText.GetComponent<RectTransform>();
            amountRect.anchorMin = new Vector2(0, 0);
            amountRect.anchorMax = new Vector2(1, 0.6f);
            amountRect.sizeDelta = Vector2.zero;
            amountRect.offsetMin = new Vector2(8, 3);
            amountRect.offsetMax = new Vector2(-8, 0);
            _myChipsText.color = new Color(1f, 0.85f, 0.2f); // Gold
            _myChipsText.fontStyle = FontStyles.Bold;
            _myChipsText.alignment = TextAlignmentOptions.Right;
            
            // Start visible with placeholder text so we can verify positioning
            _myChipsPanel.SetActive(true);
        }
        
        private void UpdateMyChipsDisplay(int chips)
        {
            if (_myChipsText == null || _myChipsPanel == null) return;
            
            _myChipsPanel.SetActive(true);
            
            // Animate chip changes
            if (_lastDisplayedChips >= 0 && chips != _lastDisplayedChips)
            {
                // Flash effect on change
                StartCoroutine(FlashChipsChange(chips > _lastDisplayedChips));
            }
            
            _myChipsText.text = ChipStack.FormatChipValueFull(chips);
            _lastDisplayedChips = chips;
        }
        
        private System.Collections.IEnumerator FlashChipsChange(bool isGain)
        {
            if (_myChipsText == null) yield break;
            
            Color flashColor = isGain ? new Color(0.3f, 1f, 0.3f) : new Color(1f, 0.3f, 0.3f);
            Color normalColor = new Color(1f, 0.85f, 0.2f);
            
            // Flash the color
            _myChipsText.color = flashColor;
            
            // Quick scale bump
            Vector3 originalScale = _myChipsText.transform.localScale;
            _myChipsText.transform.localScale = originalScale * 1.2f;
            
            yield return new WaitForSeconds(0.15f);
            
            // Animate back to normal
            float elapsed = 0f;
            float duration = 0.3f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                _myChipsText.color = Color.Lerp(flashColor, normalColor, t);
                _myChipsText.transform.localScale = Vector3.Lerp(originalScale * 1.2f, originalScale, t);
                yield return null;
            }
            
            _myChipsText.color = normalColor;
            _myChipsText.transform.localScale = originalScale;
        }
        
        private void BuildSideMenu()
        {
            var theme = Theme.Current;
            
            menuPanel = UIFactory.CreatePanel(_canvas.transform, "MenuPanel", theme.cardPanelColor);
            var menuRect = menuPanel.GetComponent<RectTransform>();
            menuRect.anchorMin = new Vector2(0, 0.3f);
            menuRect.anchorMax = new Vector2(0, 0.7f);
            menuRect.pivot = new Vector2(0, 0.5f);
            menuRect.sizeDelta = new Vector2(200, 0);
            menuRect.anchoredPosition = new Vector2(10, 0);
            
            var vlg = menuPanel.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10;
            vlg.padding = new RectOffset(15, 15, 20, 20);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            vlg.childControlHeight = false;
            
            // Add Bots button (only for table creator in practice mode)
            var addBotsBtn = UIFactory.CreateButton(menuPanel.transform, "AddBotsBtn", "Add Bots", OnAddBotsClick);
            addBotsBtn.GetOrAddComponent<LayoutElement>().preferredHeight = 50;
            addBotsBtn.GetComponent<Image>().color = theme.primaryColor;
            _addBotsButton = addBotsBtn.GetComponent<Button>();
            _addBotsButton.gameObject.SetActive(false); // Hidden by default, shown when state confirms creator + practice mode
            
            inviteButton = UIFactory.CreateButton(menuPanel.transform, "InviteBtn", "Invite Player", OnInviteClick).GetComponent<Button>();
            inviteButton.GetOrAddComponent<LayoutElement>().preferredHeight = 50;
            
            chatButton = UIFactory.CreateButton(menuPanel.transform, "ChatBtn", "Chat", OnChatClick).GetComponent<Button>();
            chatButton.GetOrAddComponent<LayoutElement>().preferredHeight = 50;
            
            leaveButton = UIFactory.CreateButton(menuPanel.transform, "LeaveBtn", "Leave Table", OnLeaveClick).GetComponent<Button>();
            leaveButton.GetOrAddComponent<LayoutElement>().preferredHeight = 50;
            leaveButton.GetComponent<Image>().color = theme.dangerColor;
            
            menuPanel.SetActive(false);
            
            // Build bot selection panel
            BuildBotPanel();
            
            // Build bot approval popup
            BuildBotApprovalPopup();
        }
        
        private void BuildBotPanel()
        {
            var theme = Theme.Current;
            
            _botPanel = UIFactory.CreatePanel(_canvas.transform, "BotPanel", new Color(0, 0, 0, 0.9f));
            var panelRect = _botPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            
            var innerPanel = UIFactory.CreatePanel(_botPanel.transform, "InnerPanel", theme.panelColor);
            var innerRect = innerPanel.GetComponent<RectTransform>();
            innerRect.anchorMin = new Vector2(0.5f, 0.5f);
            innerRect.anchorMax = new Vector2(0.5f, 0.5f);
            innerRect.sizeDelta = new Vector2(320, 380);
            
            var layout = innerPanel.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12;
            layout.padding = new RectOffset(20, 20, 20, 20);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            
            var title = UIFactory.CreateTitle(innerPanel.transform, "Title", "ADD BOT PLAYERS", 22f);
            title.GetComponent<RectTransform>().sizeDelta = new Vector2(280, 35);
            
            var subtitle = UIFactory.CreateText(innerPanel.transform, "Subtitle", 
                "Select a bot to invite.", 12f, theme.textSecondary);
            subtitle.GetComponent<RectTransform>().sizeDelta = new Vector2(280, 25);
            
            // Bot buttons
            var texBtn = UIFactory.CreateButton(innerPanel.transform, "TexBtn", "TEX (Aggressive)", () => OnInviteBot("tex"));
            texBtn.GetOrAddComponent<LayoutElement>().preferredHeight = 55;
            texBtn.GetComponent<Image>().color = theme.primaryColor;
            
            var larryBtn = UIFactory.CreateButton(innerPanel.transform, "LarryBtn", "LAZY LARRY (Passive)", () => OnInviteBot("lazy_larry"));
            larryBtn.GetOrAddComponent<LayoutElement>().preferredHeight = 55;
            larryBtn.GetComponent<Image>().color = theme.primaryColor;
            
            var picklesBtn = UIFactory.CreateButton(innerPanel.transform, "PicklesBtn", "PICKLES (Random)", () => OnInviteBot("pickles"));
            picklesBtn.GetOrAddComponent<LayoutElement>().preferredHeight = 55;
            picklesBtn.GetComponent<Image>().color = theme.primaryColor;
            
            var closeBtn = UIFactory.CreateButton(innerPanel.transform, "CloseBtn", "CLOSE", () => _botPanel.SetActive(false));
            closeBtn.GetOrAddComponent<LayoutElement>().preferredHeight = 45;
            
            _botPanel.SetActive(false);
        }
        
        private void BuildBotApprovalPopup()
        {
            var theme = Theme.Current;
            
            _botApprovalPopup = UIFactory.CreatePanel(_canvas.transform, "BotApprovalPopup", new Color(0, 0, 0, 0.85f));
            var panelRect = _botApprovalPopup.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            
            var innerPanel = UIFactory.CreatePanel(_botApprovalPopup.transform, "InnerPanel", theme.panelColor);
            var innerRect = innerPanel.GetComponent<RectTransform>();
            innerRect.anchorMin = new Vector2(0.5f, 0.5f);
            innerRect.anchorMax = new Vector2(0.5f, 0.5f);
            innerRect.sizeDelta = new Vector2(320, 180);
            
            var layout = innerPanel.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 15;
            layout.padding = new RectOffset(20, 20, 20, 20);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            
            var title = UIFactory.CreateTitle(innerPanel.transform, "Title", "BOT INVITE", 20f);
            title.GetComponent<RectTransform>().sizeDelta = new Vector2(280, 30);
            
            var message = UIFactory.CreateText(innerPanel.transform, "Message", 
                "A bot wants to join. Approve?", 14f, theme.textSecondary);
            message.GetComponent<RectTransform>().sizeDelta = new Vector2(280, 35);
            
            // Button row
            var buttonRow = new GameObject("ButtonRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            buttonRow.transform.SetParent(innerPanel.transform, false);
            buttonRow.GetComponent<RectTransform>().sizeDelta = new Vector2(280, 50);
            var hlg = buttonRow.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            
            var approveBtn = UIFactory.CreateButton(buttonRow.transform, "ApproveBtn", "APPROVE", OnApproveBot);
            approveBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 45);
            approveBtn.GetComponent<Image>().color = theme.successColor;
            
            var rejectBtn = UIFactory.CreateButton(buttonRow.transform, "RejectBtn", "G�� REJECT", OnRejectBot);
            rejectBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 45);
            rejectBtn.GetComponent<Image>().color = theme.dangerColor;
            
            _botApprovalPopup.SetActive(false);
        }
        
        private void BuildResultPanel()
        {
            // Result panel no longer used - we use the action announcement banner for wins
            // Keeping this method for backwards compatibility in case other code calls it
            resultPanel = new GameObject("ResultPanelDummy");
            resultPanel.transform.SetParent(_canvas.transform, false);
            resultPanel.SetActive(false);
            resultText = null;
        }
        
        private void BuildCountdownOverlay()
        {
            var theme = Theme.Current;
            
            // Semi-transparent overlay
            _countdownOverlay = UIFactory.CreatePanel(_canvas.transform, "CountdownOverlay", new Color(0, 0, 0, 0.6f));
            var overlayRect = _countdownOverlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = new Vector2(0.3f, 0.3f);
            overlayRect.anchorMax = new Vector2(0.7f, 0.7f);
            overlayRect.sizeDelta = Vector2.zero;
            
            // Add rounded corners effect with inner panel
            var innerPanel = UIFactory.CreatePanel(_countdownOverlay.transform, "Inner", new Color(0.1f, 0.15f, 0.2f, 0.95f));
            var innerRect = innerPanel.GetComponent<RectTransform>();
            innerRect.anchorMin = new Vector2(0.05f, 0.05f);
            innerRect.anchorMax = new Vector2(0.95f, 0.95f);
            innerRect.sizeDelta = Vector2.zero;
            
            // "GAME STARTING" label at top
            _countdownLabel = UIFactory.CreateTitle(innerPanel.transform, "Label", "GAME STARTING", 32f);
            _countdownLabel.color = theme.accentColor;
            _countdownLabel.alignment = TextAlignmentOptions.Center;
            var labelRect = _countdownLabel.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.65f);
            labelRect.anchorMax = new Vector2(1, 0.9f);
            labelRect.sizeDelta = Vector2.zero;
            
            // Big countdown number in center
            _countdownNumber = UIFactory.CreateTitle(innerPanel.transform, "Number", "5", 120f);
            _countdownNumber.color = Color.white;
            _countdownNumber.alignment = TextAlignmentOptions.Center;
            _countdownNumber.fontStyle = FontStyles.Bold;
            var numberRect = _countdownNumber.GetComponent<RectTransform>();
            numberRect.anchorMin = new Vector2(0, 0.15f);
            numberRect.anchorMax = new Vector2(1, 0.7f);
            numberRect.sizeDelta = Vector2.zero;
            
            // "Get Ready!" text at bottom
            var readyText = UIFactory.CreateText(innerPanel.transform, "Ready", "Get Ready!", 24f, theme.textSecondary);
            readyText.alignment = TextAlignmentOptions.Center;
            var readyRect = readyText.GetComponent<RectTransform>();
            readyRect.anchorMin = new Vector2(0, 0.02f);
            readyRect.anchorMax = new Vector2(1, 0.18f);
            readyRect.sizeDelta = Vector2.zero;
            
            _countdownOverlay.SetActive(false);
        }
        
        private void UpdateCountdownDisplay(int countdownValue)
        {
            Debug.Log($"[TableScene] UpdateCountdownDisplay called with: {countdownValue}, overlay exists: {_countdownOverlay != null}");
            
            if (countdownValue > 0)
            {
                Debug.Log($"[TableScene] Activating countdown overlay with value: {countdownValue}");
                _countdownOverlay.SetActive(true);
                _countdownNumber.text = countdownValue.ToString();
                
                // Play beep sound on each countdown change
                if (countdownValue != _lastCountdownValue)
                {
                    _lastCountdownValue = countdownValue;
                    
                    // Play countdown beep sound - but only AFTER Ready to Rumble finishes (7 seconds)
                    float timeSinceRumble = Time.time - _rumbleStartTime;
                    bool rumbleFinished = _playedReadyToRumble && timeSinceRumble >= RUMBLE_DURATION;
                    if (countdownValue > 0 && rumbleFinished)
                    {
                        Debug.Log($"[TableScene] Playing countdown beep for {countdownValue}");
                        if (Core.AudioManager.Instance != null)
                        {
                            Core.AudioManager.Instance.PlayCountdownBeep();
                        }
                        else
                        {
                            Debug.LogWarning("[TableScene] AudioManager.Instance is null - cannot play countdown beep");
                        }
                    }
                    
                    // Pulse animation effect - scale up then back
                    if (_countdownNumber != null)
                    {
                        _countdownNumber.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
                        StartCoroutine(AnimateCountdownPulse());
                    }
                }
            }
            else
            {
                _countdownOverlay.SetActive(false);
                _lastCountdownValue = -1;
            }
        }
        
        private System.Collections.IEnumerator AnimateCountdownPulse()
        {
            float duration = 0.2f;
            float elapsed = 0f;
            Vector3 startScale = new Vector3(1.2f, 1.2f, 1f);
            Vector3 endScale = Vector3.one;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                _countdownNumber.transform.localScale = Vector3.Lerp(startScale, endScale, t);
                yield return null;
            }
            
            _countdownNumber.transform.localScale = endScale;
        }
        
        private void BuildStartGameButton()
        {
            var theme = Theme.Current;
            
            // Big "START GAME" button for table creator - shown in center during waiting phase
            _startGameButton = UIFactory.CreatePanel(_canvas.transform, "StartGameButton", Color.clear);
            var btnRect = _startGameButton.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.35f, 0.45f);
            btnRect.anchorMax = new Vector2(0.65f, 0.55f);
            btnRect.sizeDelta = Vector2.zero;
            
            var btn = UIFactory.CreateButton(_startGameButton.transform, "Btn", "START GAME", OnStartGameClick);
            var innerRect = btn.GetComponent<RectTransform>();
            innerRect.anchorMin = Vector2.zero;
            innerRect.anchorMax = Vector2.one;
            innerRect.sizeDelta = Vector2.zero;
            btn.GetComponent<Image>().color = theme.successColor;
            
            // Get the text and make it bigger
            var text = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.fontSize = 32f;
                text.fontStyle = FontStyles.Bold;
            }
            
            _startGameButton.SetActive(false);
        }
        
        private void BuildReadyOverlay()
        {
            var theme = Theme.Current;
            
            // Ready overlay - shown during ready_up phase
            _readyOverlay = UIFactory.CreatePanel(_canvas.transform, "ReadyOverlay", new Color(0, 0, 0, 0.7f));
            var overlayRect = _readyOverlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = new Vector2(0.25f, 0.3f);
            overlayRect.anchorMax = new Vector2(0.75f, 0.7f);
            overlayRect.sizeDelta = Vector2.zero;
            
            var innerPanel = UIFactory.CreatePanel(_readyOverlay.transform, "Inner", theme.panelColor);
            var innerRect = innerPanel.GetComponent<RectTransform>();
            innerRect.anchorMin = new Vector2(0.05f, 0.05f);
            innerRect.anchorMax = new Vector2(0.95f, 0.95f);
            innerRect.sizeDelta = Vector2.zero;
            
            // Title
            var title = UIFactory.CreateTitle(innerPanel.transform, "Title", "GET READY!", 36f);
            title.color = theme.accentColor;
            title.alignment = TextAlignmentOptions.Center;
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0, 0.75f);
            titleRect.anchorMax = new Vector2(1, 0.95f);
            titleRect.sizeDelta = Vector2.zero;
            
            // Timer
            _readyTimerText = UIFactory.CreateTitle(innerPanel.transform, "Timer", "60", 48f);
            _readyTimerText.color = Color.white;
            _readyTimerText.alignment = TextAlignmentOptions.Center;
            var timerRect = _readyTimerText.GetComponent<RectTransform>();
            timerRect.anchorMin = new Vector2(0, 0.55f);
            timerRect.anchorMax = new Vector2(1, 0.75f);
            timerRect.sizeDelta = Vector2.zero;
            
            // Ready count
            _readyCountText = UIFactory.CreateText(innerPanel.transform, "Count", "0/0 players ready", 20f, theme.textSecondary);
            _readyCountText.alignment = TextAlignmentOptions.Center;
            var countRect = _readyCountText.GetComponent<RectTransform>();
            countRect.anchorMin = new Vector2(0, 0.42f);
            countRect.anchorMax = new Vector2(1, 0.55f);
            countRect.sizeDelta = Vector2.zero;
            
            // Big READY button
            var readyBtn = UIFactory.CreateButton(innerPanel.transform, "ReadyBtn", "I'M READY!", OnReadyClick);
            var readyBtnRect = readyBtn.GetComponent<RectTransform>();
            readyBtnRect.anchorMin = new Vector2(0.2f, 0.08f);
            readyBtnRect.anchorMax = new Vector2(0.8f, 0.35f);
            readyBtnRect.sizeDelta = Vector2.zero;
            readyBtn.GetComponent<Image>().color = theme.successColor;
            
            var readyText = readyBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (readyText != null)
            {
                readyText.fontSize = 28f;
                readyText.fontStyle = FontStyles.Bold;
            }
            
            _readyOverlay.SetActive(false);
        }
        
        private void OnStartGameClick()
        {
            Debug.Log($"[TableScene] START GAME CLICKED | isSimulation={_isSimulation}, isCreator={_isTableCreator}, mySeatIndex={_mySeatIndex}");
            
            // Disable button immediately to prevent double-clicks
            if (_startGameButton != null)
            {
                var btn = _startGameButton.GetComponentInChildren<Button>();
                if (btn != null) btn.interactable = false;
            }
            
            _gameService.StartGame((success, error) =>
            {
                if (success)
                {
                    Debug.Log("[TableScene] START GAME SUCCESS - ready-up phase should begin");
                }
                else
                {
                    Debug.LogError($"[TableScene] START GAME FAILED | Error: {error}");
                    // Re-enable button on failure
                    if (_startGameButton != null)
                    {
                        var btn = _startGameButton.GetComponentInChildren<Button>();
                        if (btn != null) btn.interactable = true;
                    }
                }
            });
        }
        
        private void OnReadyClick()
        {
            Debug.Log("[TableScene] Ready clicked!");
            _hasClickedReady = true;
            
            _gameService.PlayerReady((success, error) =>
            {
                if (!success)
                {
                    Debug.LogError($"Failed to ready up: {error}");
                    _hasClickedReady = false;
                }
            });
        }
        
        private void UpdateReadyUI(TableState state)
        {
            var myId = _gameService.CurrentUser?.id;
            bool isCreator = myId != null && state.creatorId == myId;
            
            // Show START GAME button for creator during waiting phase (NOT in simulation - auto-starts)
            bool showStartButton = state.phase == "waiting" && isCreator && state.totalPlayerCount >= 2 && !state.isSimulation;
            
            // ALWAYS log in waiting phase to debug simulation issue
            if (state.phase == "waiting")
            {
                Debug.Log($"[TableScene] WAITING PHASE | showStartButton={showStartButton}, isCreator={isCreator}, players={state.totalPlayerCount}, isSimulation={state.isSimulation}");
            }
            
            _startGameButton?.SetActive(showStartButton);
            
            // Show READY overlay during ready_up phase (not for bots, spectators, or if already ready)
            if (state.phase == "ready_up" || state.phase == "countdown")
            {
                var mySeat = state.seats?.Find(s => s?.playerId == myId);
                bool amISeated = mySeat != null && _mySeatIndex >= 0; // Spectators have no seat
                bool amIReady = mySeat?.isReady ?? false;
                bool amIBot = mySeat?.isBot ?? false;
                
                // Show overlay if I'm seated, not ready, not a bot, and haven't clicked ready
                bool showReadyOverlay = amISeated && !amIReady && !amIBot && !_hasClickedReady;
                _readyOverlay?.SetActive(showReadyOverlay);
                
                if (_readyTimerText != null && _readyCountText != null)
                {
                    if (state.phase == "ready_up")
                    {
                        _readyTimerText.text = state.readyUpTimeRemaining.ToString();
                    }
                    else
                    {
                        _readyTimerText.text = state.startCountdownRemaining.ToString();
                    }
                    _readyCountText.text = $"{state.readyPlayerCount}/{state.totalPlayerCount} players ready";
                }
            }
            else
            {
                _readyOverlay?.SetActive(false);
                _hasClickedReady = false; // Reset for next time
            }
        }
        
        #region Event Handlers
        
        private void OnTableStateUpdate(TableState state)
        {
            try
            {
                if (state == null) return;
                
                // COMPREHENSIVE STATE LOGGING - compare normal vs simulation
                Debug.Log($"[FULL-STATE] OnTableStateUpdate | phase={state.phase} | isSimulation={state.isSimulation} | " +
                    $"pot={state.pot} | turnTime={state.turnTimeRemaining} | blindTime={state.blindTimeRemaining} | " +
                    $"blindEnabled={state.blindIncreaseEnabled} | blindLevel={state.blindLevel} | " +
                    $"seatCount={state.seats?.Count ?? 0} | communityCards={state.communityCards?.Count ?? 0} | " +
                    $"currentPlayerId={state.currentPlayerId} | isSpectating={state.isSpectating}");
                
                // Phase transition logging
                if (_previousPhase != state.phase)
                {
                    Debug.Log($"[PHASE-CHANGE] {_previousPhase} → {state.phase} | " +
                        $"time={Time.time:F3} | pot={state.pot} | blindLevel={state.blindLevel}");
                }
                
                _currentState = state;
                
                // Check if current user is the table creator
                var myId = _gameService.CurrentUser?.id;
                _isTableCreator = myId != null && state.creatorId == myId;
                _isPracticeMode = state.practiceMode;
                _isSimulation = state.isSimulation;

                // Show Add Bots button only for table creator in practice mode (NOT in simulation - bots are auto-managed)
                if (_addBotsButton != null)
                {
                    _addBotsButton.gameObject.SetActive(_isTableCreator && _isPracticeMode && !_isSimulation);
                }
                
                // Update pot
                if (potText != null)
                    potText.text = $"Pot: {ChipStack.FormatChipValue((int)state.pot)}";
                
                // Update phase display
                if (phaseText != null)
                    phaseText.text = GetPhaseDisplayName(state.phase);
            
            // Detect phase change and show phase announcement
            // CRITICAL: Check for countdown START before updating _previousPhase
            // Check if countdown just started (phase changed to countdown)
            bool justStartedCountdown = (state.phase == "countdown" && _previousPhase != "countdown");
            
            // Play "Let's get ready to rumble!" IMMEDIATELY when countdown phase starts
            // This plays BEFORE the 10-second countdown begins
            if (justStartedCountdown && !_playedReadyToRumble)
            {
                Debug.Log($"[TableScene] Countdown phase just started - playing 'Ready to Rumble' sound BEFORE countdown begins");
                if (Core.AudioManager.Instance != null)
                {
                    Core.AudioManager.Instance.PlayReadyToRumble();
                    _playedReadyToRumble = true;
                    _rumbleStartTime = Time.time; // Mark as played so we don't play it again
                }
                else
                {
                    Debug.LogWarning("[TableScene] AudioManager.Instance is null - cannot play Ready to Rumble sound");
                }
            }
            
            if (!string.IsNullOrEmpty(state.phase) && state.phase != _previousPhase)
            {
                // Show phase announcement for game phases (flop, turn, river)
                // Skip preflop since it's the start of a new hand
                if (state.phase == "flop" || state.phase == "turn" || state.phase == "river")
                {
                    ShowPhaseAnnouncement(state.phase);
                }
                _previousPhase = state.phase;
            }
            
            // Handle countdown display based on phase
            if (state.phase == "countdown" && state.startCountdownRemaining > 0)
            {
                // Show final countdown overlay
                UpdateCountdownDisplay(state.startCountdownRemaining);
            }
            else
            {
                UpdateCountdownDisplay(0);
            }
            
            // Reset the flag when countdown ends
            if (state.phase != "countdown" && _playedReadyToRumble)
            {
                _playedReadyToRumble = false;
            }
            
            // Update ready-up UI (START GAME button, READY overlay)
            UpdateReadyUI(state);
            
            // Find my seat index for proper perspective rotation
            if (state.seats != null)
            {
                for (int i = 0; i < state.seats.Count; i++)
                {
                    if (state.seats[i]?.playerId == myId)
                    {
                        _mySeatIndex = i;
                        break;
                    }
                }
            }
            
            // Update table view with seat rotation
            _tableView?.UpdateFromState(state, _mySeatIndex);
            
            // Update My Chips display and check if I'm eliminated
            bool isEliminated = false;
            if (_mySeatIndex >= 0 && state.seats != null && _mySeatIndex < state.seats.Count)
            {
                var mySeat = state.seats[_mySeatIndex];
                if (mySeat != null)
                {
                    UpdateMyChipsDisplay((int)mySeat.chips);
                    // Check if I'm eliminated (out of chips)
                    isEliminated = mySeat.chips <= 0;
                }
            }
            
            // Check if it's my turn (myId already declared above)
            _isMyTurn = state.currentPlayerId == myId;
            
            // Only show action buttons during actual gameplay
            bool isGamePhase = state.phase == "preflop" || state.phase == "flop" || 
                               state.phase == "turn" || state.phase == "river";
            
            // CRITICAL FIX: Only show/hide action panel based on turn status
            // Don't hide it if it's still the player's turn - this prevents the panel
            // from disappearing while the player is adjusting the bet slider
            // Eliminated players can't bet, so always hide action panel for them
            if (_isMyTurn && isGamePhase && !isEliminated)
            {
                // It's my turn - show/update action buttons
                ShowActionButtons(state);
            }
            else if (actionPanel != null && (!_isMyTurn || isEliminated))
            {
                // Hide if it's NOT my turn OR if I'm eliminated
                // Don't hide if panel is already hidden (avoid unnecessary operations)
                if (actionPanel.activeSelf)
                {
                    actionPanel.SetActive(false);
                }
            }
            
            // CRITICAL: Ensure menu button is always accessible for eliminated players
            // Eliminated players should be able to leave table but can also spectate
            // Menu button should always be visible (it's part of top bar)
            
            // Sync local turn timer from server (for smooth countdown between updates)
            _isGamePhaseActive = isGamePhase && state.turnTimeRemaining > 0;
            
            // COMPREHENSIVE TIMER LOGGING - compare normal vs simulation
            Debug.Log($"[TIMER-STATE] Turn timer | phase={state.phase} | isSimulation={state.isSimulation} | " +
                $"isGamePhase={isGamePhase} | serverTurnTime={state.turnTimeRemaining} | " +
                $"localTurnTime={_localTurnTimeRemaining} | isGamePhaseActive={_isGamePhaseActive} | " +
                $"timerVisible={timerText?.gameObject.activeSelf}");
            
            if (_isGamePhaseActive)
            {
                // Sync from server - use server value as authoritative
                _localTurnTimeRemaining = state.turnTimeRemaining;
            }
            else
            {
                _localTurnTimeRemaining = 0f;
                if (timerText != null)
                {
                    timerText.gameObject.SetActive(false);
                    timerText.transform.localScale = Vector3.one;
                }
            }
            
            // Update blind timer display
            UpdateBlindTimerDisplay(state);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in OnTableStateUpdate: {e.Message}\n{e.StackTrace}");
            }
        }
        
        private void UpdateBlindTimerDisplay(TableState state)
        {
            // Sync local blind timer state from server
            _blindIncreaseEnabled = state.blindIncreaseEnabled;
            _currentBlindLevel = state.blindLevel;
            _currentSmallBlind = state.smallBlind;
            _currentBigBlind = state.bigBlind;
            
            // COMPREHENSIVE BLIND TIMER LOGGING - compare normal vs simulation
            Debug.Log($"[TIMER-STATE] Blind timer | phase={state.phase} | isSimulation={state.isSimulation} | " +
                $"blindIncreaseEnabled={state.blindIncreaseEnabled} | serverBlindTime={state.blindTimeRemaining} | " +
                $"localBlindTime={_localBlindTimeRemaining} | blindLevel={state.blindLevel} | " +
                $"blinds={state.smallBlind}/{state.bigBlind} | blindTimerVisible={blindTimerText?.gameObject.activeSelf}");
            
            if (state.blindIncreaseEnabled && state.blindTimeRemaining > 0)
            {
                // Sync from server - use server value as authoritative
                _localBlindTimeRemaining = state.blindTimeRemaining;
            }
            else if (!state.blindIncreaseEnabled)
            {
                _localBlindTimeRemaining = -1f;
                if (blindTimerText != null)
                {
                    blindTimerText.gameObject.SetActive(false);
                }
            }
            // Note: Update() handles the actual display now for smooth countdown
        }
        
        private void ShowActionButtons(TableState state)
        {
            // CRITICAL: Always show panel if it's the player's turn
            // Don't hide it if it's already visible (prevents flickering/disappearing)
            if (!actionPanel.activeSelf)
            {
                actionPanel.SetActive(true);
            }
            
            // Find my seat
            SeatInfo mySeat = null;
            foreach (var seat in state.seats)
            {
                if (seat != null && seat.playerId == _gameService.CurrentUser?.id)
                {
                    mySeat = seat;
                    break;
                }
            }
            
            if (mySeat == null) return;
            
            int myChips = (int)mySeat.chips;
            int currentBet = (int)state.currentBet;
            int myCurrentBet = (int)mySeat.currentBet;
            _callAmount = currentBet - myCurrentBet;
            _minBet = (int)state.minBet;
            _maxBet = myChips;
            
            // Check vs Call
            bool canCheck = _callAmount <= 0;
            checkButton.gameObject.SetActive(canCheck);
            callButton.transform.parent.gameObject.SetActive(!canCheck);
            
            if (!canCheck)
            {
                callAmountText.text = ChipStack.FormatChipValue(_callAmount);
            }
            
            // Bet vs Raise
            bool hasBet = currentBet > 0;
            betButton.gameObject.SetActive(!hasBet);
            raiseButton.gameObject.SetActive(hasBet);
            
            // Update slider - minimum must be at least the minimum valid raise/bet
            // For betting (no current bet): minimum is minBet (big blind)
            // For raising: minimum is currentBet + minRaise (total amount needed, not just the raise portion)
            int toCall = Math.Max(0, currentBet - myCurrentBet);
            int minRaiseAmount = state.minRaise > 0 ? state.minRaise : _minBet;
            int sliderMin = hasBet ? (toCall + minRaiseAmount) : _minBet; // For raise, need toCall + minRaise
            
            // CRITICAL FIX: Only update slider values if they've actually changed
            // This prevents the slider from resetting while the player is adjusting it
            if (betSlider.minValue != sliderMin)
            {
                betSlider.minValue = sliderMin;
            }
            if (betSlider.maxValue != myChips)
            {
                betSlider.maxValue = myChips;
            }
            
            // Only reset slider value if it's invalid (below min or above max)
            // Don't reset if player is actively adjusting it
            float currentValue = betSlider.value;
            if (currentValue < sliderMin || currentValue > myChips)
            {
                betSlider.value = sliderMin;
                OnBetSliderChanged(sliderMin);
            }
            else
            {
                // Just update the display text without changing slider value
                OnBetSliderChanged(currentValue);
            }
            
            // All-in always available
            allInButton.gameObject.SetActive(true);
        }
        
        private void OnPlayerActionReceived(string playerId, string action, int? amount)
        {
            // Show action animation on the player seat
            _tableView.ShowPlayerAction(playerId, action, amount);
            
            // Play sound for the action
            AudioManager.Instance?.PlayPokerAction(action, amount);
            
            // Show action announcement
            string playerName = GetPlayerName(playerId);
            ShowActionAnnouncement(playerName, action, amount ?? 0);
        }
        
        private string GetPlayerName(string playerId)
        {
            if (_currentState?.seats == null) return "Player";
            
            // Check if it's the current user
            if (playerId == _gameService.CurrentUser?.id)
                return "You";
            
            // Find player in seats
            foreach (var seat in _currentState.seats)
            {
                if (seat != null && seat.playerId == playerId)
                {
                    // Use name field (server sends 'name', not 'playerName')
                    string displayName = seat.name ?? seat.playerName ?? "Player";
                    return displayName; // Don't add [BOT] prefix here - keep it clean
                }
            }
            
            return "Player";
        }
        
        private void OnPlayerJoinedTable(string oderId, string name, int seatIndex)
        {
            try
            {
                Debug.Log($"{name} joined at seat {seatIndex}");
                
                // Don't show notification for yourself joining
                if (oderId == _gameService.CurrentUser?.id) return;
                
                // Show join notification
                string displayName = string.IsNullOrEmpty(name) ? "A player" : name;
                ShowPlayerNotification($"{displayName} joined the table", new Color(0.3f, 0.8f, 0.3f)); // Green
                
                // Play sound safely
                var audio = AudioManager.Instance;
                if (audio != null && audio.playerJoin != null)
                {
                    audio.PlaySFX(audio.playerJoin);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in OnPlayerJoinedTable: {e.Message}");
            }
        }
        
        private void OnPlayerLeftTable(string playerId)
        {
            try
            {
                Debug.Log($"Player {playerId} left");
                
                // Show leave notification (player name might not be available after they left)
                ShowPlayerNotification("A player left the table", new Color(0.7f, 0.7f, 0.7f)); // Gray
                
                // Play sound safely
                var audio = AudioManager.Instance;
                if (audio != null && audio.playerLeave != null)
                {
                    audio.PlaySFX(audio.playerLeave);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in OnPlayerLeftTable: {e.Message}");
            }
        }
        
        private void ShowPlayerNotification(string message, Color color)
        {
            try
            {
                if (_actionAnnouncement == null || _actionText == null) return;
                
                _actionText.text = message;
                _actionText.color = color;
                _actionAnnouncement.SetActive(true);
                
                // Auto-hide after 2.5 seconds
                CancelInvoke(nameof(HideActionAnnouncement));
                Invoke(nameof(HideActionAnnouncement), 2.5f);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in ShowPlayerNotification: {e.Message}");
            }
        }
        
        private void OnSpectatorJoined(string userId, string name)
        {
            try
            {
                // Don't show notification for yourself
                if (userId == _gameService.CurrentUser?.id) return;
                
                string displayName = string.IsNullOrEmpty(name) ? "Someone" : name;
                ShowPlayerNotification($"{displayName} is now spectating", new Color(0.5f, 0.7f, 1f)); // Light blue
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in OnSpectatorJoined: {e.Message}");
            }
        }
        
        private void OnSpectatorLeft(string userId)
        {
            try
            {
                // Don't need to show spectator left notifications - less important
                Debug.Log($"Spectator {userId} left");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in OnSpectatorLeft: {e.Message}");
            }
        }
        
        private void OnPlayerEliminated(PlayerEliminatedData data)
        {
            try
            {
                bool isMe = data.playerId == _gameService.CurrentUser?.id;
                
                if (isMe)
                {
                    // I'm eliminated - show notification and ensure menu is accessible
                    ShowActionAnnouncement("YOU", "ELIMINATED", 0);
                    AudioManager.Instance?.PlayHandLose();
                    
                    // Ensure menu button is always visible for eliminated players
                    // Menu button should already be visible, but make sure it's accessible
                    if (menuPanel != null && !menuPanel.activeSelf)
                    {
                        // Menu button will show menu when clicked
                    }
                }
                else
                {
                    // Another player eliminated - show notification
                    ShowActionAnnouncement(data.playerName ?? "Player", "ELIMINATED", 0);
                    AudioManager.Instance?.PlayHandLose();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in OnPlayerEliminated: {e.Message}\n{e.StackTrace}");
            }
        }
        
        private void OnHandComplete(HandResultData result)
        {
            bool iWon = result.GetWinnerId() == _gameService.CurrentUser?.id;
            
            // Play sounds
            if (iWon)
            {
                AudioManager.Instance?.PlayHandWin();
                AudioManager.Instance?.PlayChipWin();
            }
            else
            {
                AudioManager.Instance?.PlayChipWin();
            }
            
            // Show winner using the action announcement banner (same style as betting actions)
            string winnerName = iWon ? "YOU" : (result.winnerName ?? "Unknown");
            string handName = result.handName ?? "High Card";
            string potStr = ChipStack.FormatChipValue(result.potAmount);
            
            ShowWinnerAnnouncement(winnerName, handName, potStr, iWon);
        }
        
        private void ShowWinnerAnnouncement(string winnerName, string handName, string potAmount, bool isYou)
        {
            if (_actionAnnouncement == null || _actionText == null) return;
            
            // Build the message
            string message;
            Color winColor;
            
            if (isYou)
            {
                message = $"YOU WIN!\n{handName}\n+{potAmount}";
                winColor = new Color(0.3f, 1f, 0.5f); // Bright green
            }
            else
            {
                message = $"{winnerName} WINS!\n{handName}\n+{potAmount}";
                winColor = new Color(1f, 0.84f, 0f); // Gold
            }
            
            _actionText.text = message;
            _actionText.color = winColor;
            _actionAnnouncement.SetActive(true);
            
            // Show winner for longer (4 seconds instead of 3)
            CancelInvoke(nameof(HideActionAnnouncement));
            Invoke(nameof(HideActionAnnouncement), 4f);
        }
        
        private void OnTableLeft()
        {
            SceneManager.LoadScene("LobbyScene");
        }
        
        private void OnGameOver(GameOverData data)
        {
            // Hide action panel
            actionPanel.SetActive(false);
            
            // Play victory or defeat sound
            bool iWon = data.winnerId == _gameService.CurrentUser?.id;
            if (iWon)
            {
                AudioManager.Instance?.PlayVictoryMusic();
            }
            else
            {
                AudioManager.Instance?.PlayHandLose();
            }
            
            // Show game over popup
            ShowGameOverPopup(data);
        }
        
        private void ShowGameOverPopup(GameOverData data)
        {
            var theme = Theme.Current;
            
            // Create overlay
            var overlay = UIFactory.CreatePanel(_canvas.transform, "GameOverOverlay", new Color(0, 0, 0, 0.8f));
            var overlayRect = overlay.GetComponent<RectTransform>();
            overlayRect.anchorMin = Vector2.zero;
            overlayRect.anchorMax = Vector2.one;
            overlayRect.sizeDelta = Vector2.zero;
            
            // Create popup panel
            var popup = UIFactory.CreatePanel(overlay.transform, "GameOverPopup", theme.panelColor);
            var popupRect = popup.GetComponent<RectTransform>();
            popupRect.anchorMin = new Vector2(0.5f, 0.5f);
            popupRect.anchorMax = new Vector2(0.5f, 0.5f);
            popupRect.sizeDelta = new Vector2(500, 400);
            
            // Title
            bool iWon = data.winnerId == _gameService.CurrentUser?.id;
            string titleText = iWon ? "YOU WIN!" : "GAME OVER";
            var title = UIFactory.CreateTitle(popup.transform, "Title", titleText, 48f);
            title.color = iWon ? theme.textSuccess : theme.textPrimary;
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.85f);
            titleRect.anchorMax = new Vector2(0.5f, 0.85f);
            titleRect.anchoredPosition = Vector2.zero;
            
            // Winner info
            string winnerMsg = iWon ? 
                $"You collected all the chips!\n\nFinal Stack: {ChipStack.FormatChipValue(data.winnerChips)}" :
                $"{(data.isBot ? "[BOT] " : "")}{data.winnerName} wins!\n\nYou've been eliminated.";
            var info = UIFactory.CreateText(popup.transform, "Info", winnerMsg, 24f, theme.textSecondary);
            var infoRect = info.GetComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0.1f, 0.4f);
            infoRect.anchorMax = new Vector2(0.9f, 0.7f);
            infoRect.sizeDelta = Vector2.zero;
            info.alignment = TextAlignmentOptions.Center;
            
            // Leave Table button
            var leaveBtn = UIFactory.CreateButton(popup.transform, "LeaveBtn", "LEAVE TABLE", () =>
            {
                Destroy(overlay);
                _gameService.LeaveTable();
            }, theme.buttonPrimary);
            var leaveBtnRect = leaveBtn.GetComponent<RectTransform>();
            leaveBtnRect.anchorMin = new Vector2(0.2f, 0.1f);
            leaveBtnRect.anchorMax = new Vector2(0.8f, 0.25f);
            leaveBtnRect.sizeDelta = Vector2.zero;
        }
        
        #endregion
        
        #region Action Button Handlers
        
        private void OnFoldClick()
        {
            _gameService.Fold();
            actionPanel.SetActive(false);
        }
        
        private void OnCheckClick()
        {
            _gameService.Check();
            actionPanel.SetActive(false);
        }
        
        private void OnCallClick()
        {
            _gameService.Call();
            actionPanel.SetActive(false);
        }
        
        private void OnBetClick()
        {
            int amount = (int)betSlider.value;
            _gameService.Bet(amount);
            actionPanel.SetActive(false);
        }
        
        private void OnRaiseClick()
        {
            int amount = (int)betSlider.value;
            _gameService.Raise(amount);
            actionPanel.SetActive(false);
        }
        
        private void OnAllInClick()
        {
            _gameService.AllIn();
            actionPanel.SetActive(false);
        }
        
        private void OnBetSliderChanged(float value)
        {
            betAmountText.text = ChipStack.FormatChipValue((int)value);
        }
        
        #endregion
        
        #region Menu Handlers
        
        private void OnInviteClick()
        {
            // TODO: Show invite panel
            menuPanel.SetActive(false);
        }
        
        private void OnChatClick()
        {
            // TODO: Show chat panel
            menuPanel.SetActive(false);
        }
        
        private void OnLeaveClick()
        {
            _gameService.LeaveTable();
        }
        
        private void OnAddBotsClick()
        {
            if (!_isTableCreator)
            {
                Debug.LogWarning("Only the table creator can add bots");
                return;
            }
            menuPanel.SetActive(false);
            _botPanel.SetActive(true);
        }
        
        private void OnInviteBot(string botId)
        {
            _botPanel.SetActive(false);
            
            var tableId = _gameService.CurrentTableId;
            _gameService.InviteBot(tableId, botId, 1000, (success, seat, name, pending, error) =>
            {
                if (success)
                {
                    Debug.Log(pending ? $"Bot {name} invited - waiting for approval" : $"Bot {name} joined!");
                }
                else
                {
                    Debug.LogError($"Failed to invite bot: {error}");
                }
            });
        }
        
        private void OnApproveBot()
        {
            _botApprovalPopup.SetActive(false);
            
            if (_pendingBotSeat >= 0)
            {
                var tableId = _gameService.CurrentTableId;
                _gameService.ApproveBot(tableId, _pendingBotSeat, (success, error) =>
                {
                    if (!success) Debug.LogError($"Failed to approve bot: {error}");
                });
                _pendingBotSeat = -1;
            }
        }
        
        private void OnRejectBot()
        {
            _botApprovalPopup.SetActive(false);
            
            if (_pendingBotSeat >= 0)
            {
                var tableId = _gameService.CurrentTableId;
                _gameService.RejectBot(tableId, _pendingBotSeat, (success, error) =>
                {
                    if (!success) Debug.LogError($"Failed to reject bot: {error}");
                });
                _pendingBotSeat = -1;
            }
        }
        
        private void ShowBotApprovalPopup(string botName, int seatIndex)
        {
            _pendingBotSeat = seatIndex;
            
            // Update popup message
            var texts = _botApprovalPopup.GetComponentsInChildren<TextMeshProUGUI>();
            foreach (var t in texts)
            {
                if (t.name == "Message")
                {
                    t.text = $"{botName} wants to join. Approve?";
                    break;
                }
            }
            
            _botApprovalPopup.SetActive(true);
        }
        
        #endregion
        
        #region Helpers
        
        private string GetPhaseDisplayName(string phase)
        {
            return phase?.ToLower() switch
            {
                "waiting" => "Waiting for Players",
                "ready_up" => "Ready Up!",
                "countdown" => "Starting Soon...",
                "preflop" => "Pre-Flop",
                "flop" => "Flop",
                "turn" => "Turn",
                "river" => "River",
                "showdown" => "Showdown",
                _ => phase ?? "..."
            };
        }
        
        private Slider CreateBetSlider(Transform parent)
        {
            var sliderObj = new GameObject("BetSlider");
            sliderObj.transform.SetParent(parent, false);
            var sliderObjRect = sliderObj.AddComponent<RectTransform>();
            sliderObjRect.anchorMin = Vector2.zero;
            sliderObjRect.anchorMax = Vector2.one;
            sliderObjRect.sizeDelta = Vector2.zero;
            
            var slider = sliderObj.AddComponent<Slider>();
            slider.minValue = 0;
            slider.maxValue = 10000;
            slider.wholeNumbers = true;
            
            var theme = Theme.Current;
            
            // Background - thin track
            var bg = UIFactory.CreatePanel(sliderObj.transform, "Background", theme.backgroundColor);
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.4f);
            bgRect.anchorMax = new Vector2(1, 0.6f);
            bgRect.sizeDelta = Vector2.zero;
            
            // Fill Area - thin track
            var fillArea = new GameObject("FillArea");
            fillArea.transform.SetParent(sliderObj.transform, false);
            var fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0, 0.4f);
            fillAreaRect.anchorMax = new Vector2(1, 0.6f);
            fillAreaRect.offsetMin = new Vector2(10, 0);
            fillAreaRect.offsetMax = new Vector2(-10, 0);
            
            var fill = UIFactory.CreatePanel(fillArea.transform, "Fill", theme.accentColor);
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;
            slider.fillRect = fillRect;
            
            // Handle slide area - constrained to vertical center
            var handleArea = new GameObject("HandleSlideArea");
            handleArea.transform.SetParent(sliderObj.transform, false);
            var handleAreaRect = handleArea.AddComponent<RectTransform>();
            handleAreaRect.anchorMin = new Vector2(0, 0.5f);
            handleAreaRect.anchorMax = new Vector2(1, 0.5f);
            handleAreaRect.sizeDelta = new Vector2(-20, 0);
            handleAreaRect.anchoredPosition = Vector2.zero;
            
            // Handle - fixed size, centered vertically
            var handle = UIFactory.CreatePanel(handleArea.transform, "Handle", Color.white);
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0, 0.5f);
            handleRect.anchorMax = new Vector2(0, 0.5f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);
            handleRect.sizeDelta = new Vector2(18, 18);
            handleRect.anchoredPosition = Vector2.zero;
            
            slider.handleRect = handleRect;
            slider.targetGraphic = handle.GetComponent<Image>();
            
            return slider;
        }
        
        #endregion
    }
}


