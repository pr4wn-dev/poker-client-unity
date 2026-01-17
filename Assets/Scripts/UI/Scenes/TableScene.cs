using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using PokerClient.UI;
using PokerClient.UI.Components;
using PokerClient.Networking;
using System.Collections.Generic;

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
        
        private void Start()
        {
            _gameService = GameService.Instance;
            Debug.Log($"[TableScene] Start - GameService: {_gameService != null}, IsInGame: {_gameService?.IsInGame}, CurrentTableId: {_gameService?.CurrentTableId}");
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
            _gameService.OnHandComplete += OnHandComplete;
            _gameService.OnTableLeft += OnTableLeft;
            
            BuildScene();
            
            // Apply initial state if we have it
            if (_gameService.CurrentTableState != null)
            {
                OnTableStateUpdate(_gameService.CurrentTableState);
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
                _gameService.OnHandComplete -= OnHandComplete;
                _gameService.OnTableLeft -= OnTableLeft;
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
            
            // Table View (the main poker table with seats)
            BuildTableView();
            
            // Top Info Bar
            BuildTopBar();
            
            // Action Panel (bottom)
            BuildActionPanel();
            
            // Side Menu
            BuildSideMenu();
            
            // Result Panel (shown after hands)
            BuildResultPanel();
            
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
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
        }
        
        private void BuildTopBar()
        {
            var theme = Theme.Current;
            
            var topBar = UIFactory.CreatePanel(_canvas.transform, "TopBar", theme.cardPanelColor);
            var topRect = topBar.GetComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0, 1);
            topRect.anchorMax = new Vector2(1, 1);
            topRect.pivot = new Vector2(0.5f, 1);
            topRect.sizeDelta = new Vector2(0, 70);
            topRect.anchoredPosition = Vector2.zero;
            
            // Menu button (left)
            var menuBtn = UIFactory.CreateButton(topBar.transform, "MenuBtn", "☰", () => menuPanel.SetActive(!menuPanel.activeSelf));
            var menuRect = menuBtn.GetComponent<RectTransform>();
            menuRect.anchorMin = new Vector2(0, 0.5f);
            menuRect.anchorMax = new Vector2(0, 0.5f);
            menuRect.pivot = new Vector2(0, 0.5f);
            menuRect.anchoredPosition = new Vector2(20, 0);
            menuRect.sizeDelta = new Vector2(60, 50);
            
            // Pot (center-left)
            potText = UIFactory.CreateTitle(topBar.transform, "PotText", "Pot: 0", 28f);
            var potRect = potText.GetComponent<RectTransform>();
            potRect.anchorMin = new Vector2(0.3f, 0.5f);
            potRect.anchorMax = new Vector2(0.3f, 0.5f);
            potRect.sizeDelta = new Vector2(250, 50);
            potText.color = theme.accentColor;
            
            // Phase (center)
            phaseText = UIFactory.CreateTitle(topBar.transform, "PhaseText", "Waiting...", 24f);
            var phaseRect = phaseText.GetComponent<RectTransform>();
            phaseRect.anchorMin = new Vector2(0.5f, 0.5f);
            phaseRect.anchorMax = new Vector2(0.5f, 0.5f);
            phaseRect.sizeDelta = new Vector2(200, 50);
            phaseText.alignment = TextAlignmentOptions.Center;
            
            // Timer (center-right)
            timerText = UIFactory.CreateTitle(topBar.transform, "TimerText", "", 32f);
            var timerRect = timerText.GetComponent<RectTransform>();
            timerRect.anchorMin = new Vector2(0.7f, 0.5f);
            timerRect.anchorMax = new Vector2(0.7f, 0.5f);
            timerRect.sizeDelta = new Vector2(100, 50);
            timerText.color = theme.dangerColor;
            timerText.alignment = TextAlignmentOptions.Center;
        }
        
        private void BuildActionPanel()
        {
            var theme = Theme.Current;
            
            actionPanel = UIFactory.CreatePanel(_canvas.transform, "ActionPanel", theme.cardPanelColor);
            var panelRect = actionPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0);
            panelRect.anchorMax = new Vector2(1, 0);
            panelRect.pivot = new Vector2(0.5f, 0);
            panelRect.sizeDelta = new Vector2(0, 120);
            panelRect.anchoredPosition = Vector2.zero;
            
            var hlg = actionPanel.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 15;
            hlg.padding = new RectOffset(30, 30, 15, 15);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childForceExpandWidth = false;
            
            // Fold Button
            foldButton = UIFactory.CreateButton(actionPanel.transform, "FoldBtn", "FOLD", OnFoldClick).GetComponent<Button>();
            foldButton.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 70);
            foldButton.GetComponent<Image>().color = theme.dangerColor;
            
            // Check Button
            checkButton = UIFactory.CreateButton(actionPanel.transform, "CheckBtn", "CHECK", OnCheckClick).GetComponent<Button>();
            checkButton.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 70);
            checkButton.GetComponent<Image>().color = theme.successColor;
            
            // Call Button
            var callContainer = UIFactory.CreatePanel(actionPanel.transform, "CallContainer", Color.clear);
            callContainer.GetComponent<LayoutElement>().preferredWidth = 150;
            
            callButton = UIFactory.CreateButton(callContainer.transform, "CallBtn", "CALL", OnCallClick).GetComponent<Button>();
            var callRect = callButton.GetComponent<RectTransform>();
            callRect.anchorMin = new Vector2(0, 0.2f);
            callRect.anchorMax = new Vector2(1, 1);
            callRect.sizeDelta = Vector2.zero;
            callButton.GetComponent<Image>().color = theme.primaryColor;
            
            callAmountText = UIFactory.CreateText(callContainer.transform, "CallAmount", "", 16f, theme.textSecondary);
            var callAmtRect = callAmountText.GetComponent<RectTransform>();
            callAmtRect.anchorMin = new Vector2(0, 0);
            callAmtRect.anchorMax = new Vector2(1, 0.3f);
            callAmtRect.sizeDelta = Vector2.zero;
            callAmountText.alignment = TextAlignmentOptions.Center;
            
            // Bet/Raise Slider Section
            var betSection = UIFactory.CreatePanel(actionPanel.transform, "BetSection", Color.clear);
            betSection.GetComponent<LayoutElement>().preferredWidth = 400;
            
            // Slider row
            var sliderRow = UIFactory.CreatePanel(betSection.transform, "SliderRow", Color.clear);
            var sliderRect = sliderRow.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0, 0.5f);
            sliderRect.anchorMax = new Vector2(1, 1);
            sliderRect.sizeDelta = Vector2.zero;
            
            betSlider = CreateBetSlider(sliderRow.transform);
            betSlider.onValueChanged.AddListener(OnBetSliderChanged);
            
            // Amount display
            betAmountText = UIFactory.CreateTitle(betSection.transform, "BetAmount", "0", 24f);
            var amtRect = betAmountText.GetComponent<RectTransform>();
            amtRect.anchorMin = new Vector2(0, 0);
            amtRect.anchorMax = new Vector2(1, 0.5f);
            amtRect.sizeDelta = Vector2.zero;
            betAmountText.alignment = TextAlignmentOptions.Center;
            betAmountText.color = theme.accentColor;
            
            // Bet Button
            betButton = UIFactory.CreateButton(actionPanel.transform, "BetBtn", "BET", OnBetClick).GetComponent<Button>();
            betButton.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 70);
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
            
            inviteButton = UIFactory.CreateButton(menuPanel.transform, "InviteBtn", "Invite Player", OnInviteClick).GetComponent<Button>();
            inviteButton.GetComponent<LayoutElement>().preferredHeight = 50;
            
            chatButton = UIFactory.CreateButton(menuPanel.transform, "ChatBtn", "Chat", OnChatClick).GetComponent<Button>();
            chatButton.GetComponent<LayoutElement>().preferredHeight = 50;
            
            leaveButton = UIFactory.CreateButton(menuPanel.transform, "LeaveBtn", "Leave Table", OnLeaveClick).GetComponent<Button>();
            leaveButton.GetComponent<LayoutElement>().preferredHeight = 50;
            leaveButton.GetComponent<Image>().color = theme.dangerColor;
            
            menuPanel.SetActive(false);
        }
        
        private void BuildResultPanel()
        {
            var theme = Theme.Current;
            
            resultPanel = UIFactory.CreatePanel(_canvas.transform, "ResultPanel", new Color(0, 0, 0, 0.8f));
            var resultRect = resultPanel.GetComponent<RectTransform>();
            resultRect.anchorMin = new Vector2(0.25f, 0.35f);
            resultRect.anchorMax = new Vector2(0.75f, 0.65f);
            resultRect.sizeDelta = Vector2.zero;
            
            resultText = UIFactory.CreateTitle(resultPanel.transform, "ResultText", "", 36f);
            resultText.alignment = TextAlignmentOptions.Center;
            
            // Auto-hide after 3 seconds
            resultPanel.SetActive(false);
        }
        
        #region Event Handlers
        
        private void OnTableStateUpdate(TableState state)
        {
            _currentState = state;
            
            // Update pot
            potText.text = $"Pot: {ChipStack.FormatChipValue((int)state.pot)}";
            
            // Update phase
            phaseText.text = GetPhaseDisplayName(state.phase);
            
            // Update table view
            _tableView.UpdateFromState(state);
            
            // Check if it's my turn
            string myId = _gameService.CurrentUser?.id;
            _isMyTurn = state.currentPlayerId == myId;
            
            if (_isMyTurn && state.phase != "waiting" && state.phase != "showdown")
            {
                ShowActionButtons(state);
            }
            else
            {
                actionPanel.SetActive(false);
            }
            
            // Update timer
            if (state.turnTimeRemaining.HasValue && state.turnTimeRemaining.Value > 0)
            {
                timerText.text = Mathf.CeilToInt(state.turnTimeRemaining.Value).ToString();
                timerText.gameObject.SetActive(true);
            }
            else
            {
                timerText.gameObject.SetActive(false);
            }
        }
        
        private void ShowActionButtons(TableState state)
        {
            actionPanel.SetActive(true);
            
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
            
            // Update slider
            int minRaise = hasBet ? currentBet * 2 : _minBet;
            betSlider.minValue = minRaise;
            betSlider.maxValue = myChips;
            betSlider.value = minRaise;
            OnBetSliderChanged(minRaise);
            
            // All-in always available
            allInButton.gameObject.SetActive(true);
        }
        
        private void OnPlayerActionReceived(string oderId, string action, int? amount)
        {
            // Show action animation on the player seat
            _tableView.ShowPlayerAction(oderId, action, amount);
        }
        
        private void OnPlayerJoinedTable(string oderId, string name, int seatIndex)
        {
            Debug.Log($"{name} joined at seat {seatIndex}");
        }
        
        private void OnPlayerLeftTable(string oderId)
        {
            Debug.Log($"Player {oderId} left");
        }
        
        private void OnHandComplete(HandResultData result)
        {
            // Show result
            resultText.text = $"{result.winnerName} wins with {result.handName}!\n+{ChipStack.FormatChipValue(result.potAmount)}";
            resultPanel.SetActive(true);
            
            // Auto-hide after 3 seconds
            StartCoroutine(HideResultAfterDelay());
        }
        
        private System.Collections.IEnumerator HideResultAfterDelay()
        {
            yield return new WaitForSeconds(3f);
            resultPanel.SetActive(false);
        }
        
        private void OnTableLeft()
        {
            SceneManager.LoadScene("LobbyScene");
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
        
        #endregion
        
        #region Helpers
        
        private string GetPhaseDisplayName(string phase)
        {
            return phase?.ToLower() switch
            {
                "waiting" => "Waiting for Players",
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
            
            // Background
            var bg = UIFactory.CreatePanel(sliderObj.transform, "Background", theme.backgroundColor);
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = new Vector2(0, 0.35f);
            bgRect.anchorMax = new Vector2(1, 0.65f);
            bgRect.sizeDelta = Vector2.zero;
            
            // Fill Area
            var fillArea = new GameObject("FillArea");
            fillArea.transform.SetParent(sliderObj.transform, false);
            var fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0, 0.35f);
            fillAreaRect.anchorMax = new Vector2(1, 0.65f);
            fillAreaRect.sizeDelta = Vector2.zero;
            
            var fill = UIFactory.CreatePanel(fillArea.transform, "Fill", theme.accentColor);
            var fillRect = fill.GetComponent<RectTransform>();
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
            
            var handle = UIFactory.CreatePanel(handleArea.transform, "Handle", Color.white);
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(25, 40);
            slider.handleRect = handleRect;
            slider.targetGraphic = handle.GetComponent<Image>();
            
            return slider;
        }
        
        #endregion
    }
}


