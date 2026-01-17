using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PokerClient.UI;
using PokerClient.UI.Components;
using PokerClient.Networking;
using System.Collections.Generic;

namespace PokerClient.UI.Scenes
{
    /// <summary>
    /// Poker Table scene - The main gameplay scene.
    /// Displays the table, players, cards, and handles player actions.
    /// </summary>
    public class PokerTableScene : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private Canvas canvas;
        
        [Header("Table Components")]
        [SerializeField] private PokerTableLayout tableLayout;
        [SerializeField] private ActionPanel actionPanel;
        
        [Header("UI Panels")]
        [SerializeField] private GameObject topBar;
        [SerializeField] private GameObject chatPanel;
        [SerializeField] private GameObject menuPanel;
        
        [Header("Player Cards (Bottom)")]
        [SerializeField] private GameObject playerCardsContainer;
        [SerializeField] private List<CardVisual> playerCards = new List<CardVisual>();
        
        [Header("Game Info")]
        [SerializeField] private TextMeshProUGUI tableNameText;
        [SerializeField] private TextMeshProUGUI blindsText;
        [SerializeField] private TextMeshProUGUI phaseText;
        [SerializeField] private TextMeshProUGUI timerText;
        
        [Header("Hand Result")]
        [SerializeField] private GameObject handResultPanel;
        [SerializeField] private TextMeshProUGUI handResultText;
        [SerializeField] private TextMeshProUGUI winnerText;
        
        private string _currentTableId;
        private bool _isMyTurn;
        private int _mySeatIndex = -1;
        
        private void Start()
        {
            BuildScene();
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
            
            // === TOP BAR ===
            BuildTopBar(canvas.transform);
            
            // === POKER TABLE ===
            BuildPokerTable(canvas.transform);
            
            // === PLAYER'S CARDS (Large at bottom) ===
            BuildPlayerCards(canvas.transform);
            
            // === ACTION PANEL ===
            BuildActionPanel(canvas.transform);
            
            // === HAND RESULT POPUP ===
            BuildHandResultPanel(canvas.transform);
            
            // === MENU PANEL (Hidden) ===
            BuildMenuPanel(canvas.transform);
            
            // === CHAT PANEL ===
            BuildChatPanel(canvas.transform);
        }
        
        private void BuildTopBar(Transform parent)
        {
            var theme = Theme.Current;
            
            topBar = UIFactory.CreatePanel(parent, "TopBar", theme.panelColor);
            var topRect = topBar.GetComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0, 1);
            topRect.anchorMax = new Vector2(1, 1);
            topRect.pivot = new Vector2(0.5f, 1);
            topRect.anchoredPosition = Vector2.zero;
            topRect.sizeDelta = new Vector2(0, 60);
            
            // Back button
            var backBtn = UIFactory.CreateSecondaryButton(topBar.transform, "BackBtn", "← LEAVE", OnLeaveTable, 100, 40);
            var backRect = backBtn.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0, 0.5f);
            backRect.anchorMax = new Vector2(0, 0.5f);
            backRect.anchoredPosition = new Vector2(70, 0);
            
            // Table name
            tableNameText = UIFactory.CreateText(topBar.transform, "TableName", "Table Name", 20f, 
                theme.textPrimary, TextAlignmentOptions.Center);
            tableNameText.fontStyle = FontStyles.Bold;
            var nameRect = tableNameText.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.5f, 0.5f);
            nameRect.anchorMax = new Vector2(0.5f, 0.5f);
            nameRect.sizeDelta = new Vector2(300, 30);
            
            // Blinds info
            blindsText = UIFactory.CreateText(topBar.transform, "Blinds", "Blinds: 50/100", 14f,
                theme.textSecondary, TextAlignmentOptions.Center);
            var blindsRect = blindsText.GetComponent<RectTransform>();
            blindsRect.anchorMin = new Vector2(0.5f, 0.5f);
            blindsRect.anchorMax = new Vector2(0.5f, 0.5f);
            blindsRect.anchoredPosition = new Vector2(0, -15);
            blindsRect.sizeDelta = new Vector2(200, 20);
            
            // Phase text
            phaseText = UIFactory.CreateText(topBar.transform, "Phase", "WAITING", 16f,
                theme.secondaryColor, TextAlignmentOptions.Right);
            phaseText.fontStyle = FontStyles.Bold;
            var phaseRect = phaseText.GetComponent<RectTransform>();
            phaseRect.anchorMin = new Vector2(1, 0.5f);
            phaseRect.anchorMax = new Vector2(1, 0.5f);
            phaseRect.pivot = new Vector2(1, 0.5f);
            phaseRect.anchoredPosition = new Vector2(-20, 0);
            phaseRect.sizeDelta = new Vector2(150, 30);
            
            // Menu button
            var menuBtn = UIFactory.CreateSecondaryButton(topBar.transform, "MenuBtn", "≡", OnMenuClick, 45, 40);
            var menuRect = menuBtn.GetComponent<RectTransform>();
            menuRect.anchorMin = new Vector2(1, 0.5f);
            menuRect.anchorMax = new Vector2(1, 0.5f);
            menuRect.anchoredPosition = new Vector2(-180, 0);
        }
        
        private void BuildPokerTable(Transform parent)
        {
            var theme = Theme.Current;
            
            // Table container
            var tableContainer = new GameObject("TableContainer", typeof(RectTransform));
            tableContainer.transform.SetParent(parent, false);
            var containerRect = tableContainer.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.05f, 0.22f);
            containerRect.anchorMax = new Vector2(0.95f, 0.92f);
            containerRect.offsetMin = Vector2.zero;
            containerRect.offsetMax = Vector2.zero;
            
            // Create the poker table
            tableLayout = PokerTableLayout.Create(tableContainer.transform, 9);
            var tableRect = tableLayout.GetComponent<RectTransform>();
            UIFactory.FillParent(tableRect);
        }
        
        private void BuildPlayerCards(Transform parent)
        {
            var theme = Theme.Current;
            
            // Container for player's own cards (shown larger at bottom)
            playerCardsContainer = UIFactory.CreatePanel(parent, "PlayerCards", theme.cardPanelColor);
            var containerRect = playerCardsContainer.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.5f, 0);
            containerRect.anchorMax = new Vector2(0.5f, 0);
            containerRect.pivot = new Vector2(0.5f, 0);
            containerRect.anchoredPosition = new Vector2(0, 130);
            containerRect.sizeDelta = new Vector2(200, 120);
            
            var layout = playerCardsContainer.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.padding = new RectOffset(15, 15, 10, 10);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            
            // Create two card slots
            for (int i = 0; i < 2; i++)
            {
                var card = CardVisual.Create(playerCardsContainer.transform);
                var cardRect = card.GetComponent<RectTransform>();
                cardRect.sizeDelta = new Vector2(theme.cardWidth, theme.cardHeight);
                card.SetFaceDown(true);
                playerCards.Add(card);
            }
        }
        
        private void BuildActionPanel(Transform parent)
        {
            actionPanel = ActionPanel.Create(parent);
            var panelRect = actionPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0);
            panelRect.anchorMax = new Vector2(0.5f, 0);
            panelRect.pivot = new Vector2(0.5f, 0);
            panelRect.anchoredPosition = new Vector2(0, 10);
            panelRect.sizeDelta = new Vector2(550, 115);
            
            // Connect events
            actionPanel.OnFold.AddListener(OnFold);
            actionPanel.OnCheck.AddListener(OnCheck);
            actionPanel.OnCall.AddListener(OnCall);
            actionPanel.OnBet.AddListener(OnBet);
            actionPanel.OnRaise.AddListener(OnRaise);
            actionPanel.OnAllIn.AddListener(OnAllIn);
            
            // Initially hidden until it's player's turn
            actionPanel.SetVisible(false);
        }
        
        private void BuildHandResultPanel(Transform parent)
        {
            var theme = Theme.Current;
            
            handResultPanel = UIFactory.CreatePanel(parent, "HandResult", new Color(0, 0, 0, 0.85f));
            var panelRect = handResultPanel.GetComponent<RectTransform>();
            UIFactory.Center(panelRect, new Vector2(400, 200));
            
            var layout = handResultPanel.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 15;
            layout.padding = new RectOffset(30, 30, 30, 30);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            
            // Winner text
            winnerText = UIFactory.CreateTitle(handResultPanel.transform, "Winner", "Player Wins!", 28f);
            winnerText.color = theme.playerWinner;
            var winnerRect = winnerText.GetComponent<RectTransform>();
            winnerRect.sizeDelta = new Vector2(340, 40);
            
            // Hand result
            handResultText = UIFactory.CreateText(handResultPanel.transform, "Hand", "Royal Flush", 22f, theme.textPrimary);
            var handRect = handResultText.GetComponent<RectTransform>();
            handRect.sizeDelta = new Vector2(340, 35);
            
            // Pot won
            var potText = UIFactory.CreateText(handResultPanel.transform, "Pot", "+10,000", 24f, theme.secondaryColor);
            potText.fontStyle = FontStyles.Bold;
            var potRect = potText.GetComponent<RectTransform>();
            potRect.sizeDelta = new Vector2(340, 35);
            
            handResultPanel.SetActive(false);
        }
        
        private void BuildMenuPanel(Transform parent)
        {
            var theme = Theme.Current;
            
            menuPanel = UIFactory.CreatePanel(parent, "MenuPanel", new Color(0, 0, 0, 0.9f));
            var panelRect = menuPanel.GetComponent<RectTransform>();
            UIFactory.FillParent(panelRect);
            
            var innerPanel = UIFactory.CreatePanel(menuPanel.transform, "InnerPanel", theme.panelColor);
            var innerRect = innerPanel.GetComponent<RectTransform>();
            UIFactory.Center(innerRect, new Vector2(300, 350));
            
            var layout = innerPanel.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 15;
            layout.padding = new RectOffset(30, 30, 30, 30);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            
            var title = UIFactory.CreateTitle(innerPanel.transform, "Title", "MENU", 28f);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(240, 45);
            
            UIFactory.CreateDivider(innerPanel.transform, "Divider", true, 200, 1);
            
            UIFactory.CreateSecondaryButton(innerPanel.transform, "SitOutBtn", "SIT OUT", OnSitOut, 200, 45);
            UIFactory.CreateSecondaryButton(innerPanel.transform, "SettingsBtn", "SETTINGS", null, 200, 45);
            UIFactory.CreateDangerButton(innerPanel.transform, "LeaveBtn", "LEAVE TABLE", OnLeaveTable, 200, 45);
            
            var closeBtn = UIFactory.CreateSecondaryButton(innerPanel.transform, "CloseBtn", "CLOSE", 
                () => menuPanel.SetActive(false), 150, 40);
            
            menuPanel.SetActive(false);
        }
        
        private void BuildChatPanel(Transform parent)
        {
            var theme = Theme.Current;
            
            chatPanel = UIFactory.CreatePanel(parent, "ChatPanel", theme.panelColor);
            var panelRect = chatPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1, 0);
            panelRect.anchorMax = new Vector2(1, 0);
            panelRect.pivot = new Vector2(1, 0);
            panelRect.anchoredPosition = new Vector2(-10, 130);
            panelRect.sizeDelta = new Vector2(250, 200);
            
            // Chat header
            var header = UIFactory.CreateText(chatPanel.transform, "Header", "CHAT", 12f, 
                theme.textSecondary, TextAlignmentOptions.Left);
            var headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = new Vector2(0, 25);
            headerRect.offsetMin = new Vector2(10, 0);
            
            // Chat messages area (scrollable)
            var messagesArea = UIFactory.CreatePanel(chatPanel.transform, "Messages", theme.cardPanelColor);
            var messagesRect = messagesArea.GetComponent<RectTransform>();
            messagesRect.anchorMin = new Vector2(0, 0);
            messagesRect.anchorMax = new Vector2(1, 1);
            messagesRect.offsetMin = new Vector2(5, 35);
            messagesRect.offsetMax = new Vector2(-5, -25);
            
            // Chat input
            var chatInput = UIFactory.CreateInputField(chatPanel.transform, "ChatInput", "Type message...", 0, 30);
            var inputRect = chatInput.GetComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0, 0);
            inputRect.anchorMax = new Vector2(1, 0);
            inputRect.pivot = new Vector2(0.5f, 0);
            inputRect.anchoredPosition = Vector2.zero;
            inputRect.sizeDelta = new Vector2(-10, 30);
        }
        
        #region Game State Updates
        
        /// <summary>
        /// Update the table state from server
        /// </summary>
        public void UpdateTableState(TableState state)
        {
            if (state == null) return;
            
            _currentTableId = state.id;
            
            // Update header
            tableNameText.text = state.name ?? "Poker Table";
            phaseText.text = state.phase?.ToUpper() ?? "WAITING";
            
            // Update table layout
            tableLayout.UpdateState(state);
            
            // Update player's own cards if we have a seat
            if (_mySeatIndex >= 0 && state.seats != null && _mySeatIndex < state.seats.Count)
            {
                var mySeat = state.seats[_mySeatIndex];
                if (mySeat?.cards != null)
                {
                    UpdatePlayerCards(mySeat.cards);
                }
            }
            
            // Check if it's my turn
            bool isMyTurn = state.currentPlayerIndex == _mySeatIndex && _mySeatIndex >= 0;
            SetMyTurn(isMyTurn, state);
        }
        
        /// <summary>
        /// Update the player's hole cards display
        /// </summary>
        public void UpdatePlayerCards(List<Card> cards)
        {
            for (int i = 0; i < playerCards.Count; i++)
            {
                if (cards != null && i < cards.Count && cards[i] != null && !cards[i].IsHidden)
                {
                    playerCards[i].SetCard(cards[i]);
                }
                else
                {
                    playerCards[i].SetFaceDown(true);
                }
            }
        }
        
        /// <summary>
        /// Set whether it's the player's turn
        /// </summary>
        public void SetMyTurn(bool isMyTurn, TableState state = null)
        {
            _isMyTurn = isMyTurn;
            actionPanel.SetVisible(isMyTurn);
            
            if (isMyTurn && state != null)
            {
                // Configure action panel based on game state
                int minBet = state.minBet;
                int maxBet = 10000; // TODO: Get from player's chips
                int callAmount = state.currentBet;
                int potSize = state.pot;
                bool canCheck = callAmount == 0;
                
                actionPanel.Configure(minBet, maxBet, callAmount, potSize, canCheck);
            }
        }
        
        /// <summary>
        /// Show hand result popup
        /// </summary>
        public void ShowHandResult(string winnerName, string handName, int potAmount)
        {
            winnerText.text = $"{winnerName} Wins!";
            handResultText.text = handName;
            handResultPanel.SetActive(true);
            
            // Auto-hide after 3 seconds
            Invoke(nameof(HideHandResult), 3f);
        }
        
        public void HideHandResult()
        {
            handResultPanel.SetActive(false);
        }
        
        /// <summary>
        /// Set the player's seat index
        /// </summary>
        public void SetMySeat(int seatIndex)
        {
            _mySeatIndex = seatIndex;
        }
        
        #endregion
        
        #region Action Handlers
        
        private void OnFold()
        {
            Debug.Log("Player folds");
            // TODO: Send fold action to server
        }
        
        private void OnCheck()
        {
            Debug.Log("Player checks");
            // TODO: Send check action to server
        }
        
        private void OnCall(int amount)
        {
            Debug.Log($"Player calls {amount}");
            // TODO: Send call action to server
        }
        
        private void OnBet(int amount)
        {
            Debug.Log($"Player bets {amount}");
            // TODO: Send bet action to server
        }
        
        private void OnRaise(int amount)
        {
            Debug.Log($"Player raises to {amount}");
            // TODO: Send raise action to server
        }
        
        private void OnAllIn(int amount)
        {
            Debug.Log($"Player goes all-in: {amount}");
            // TODO: Send all-in action to server
        }
        
        private void OnLeaveTable()
        {
            Debug.Log("Leaving table");
            // TODO: Send leave table to server and return to lobby
            menuPanel.SetActive(false);
        }
        
        private void OnSitOut()
        {
            Debug.Log("Sitting out");
            menuPanel.SetActive(false);
        }
        
        private void OnMenuClick()
        {
            menuPanel.SetActive(true);
        }
        
        #endregion
    }
}



