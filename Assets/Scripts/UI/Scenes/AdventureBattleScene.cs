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
    /// Adventure Battle Scene - Heads-up poker against a boss
    /// </summary>
    public class AdventureBattleScene : MonoBehaviour
    {
        private Canvas _canvas;
        private GameService _gameService;
        
        // Boss info
        private TextMeshProUGUI bossNameText;
        private TextMeshProUGUI bossChipsText;
        private TextMeshProUGUI bossTauntText;
        private Image bossAvatar;
        private List<GameObject> bossCardSlots = new List<GameObject>();
        
        // Player info
        private TextMeshProUGUI playerChipsText;
        private List<GameObject> playerCardSlots = new List<GameObject>();
        
        // Table
        private TextMeshProUGUI potText;
        private TextMeshProUGUI phaseText;
        private List<GameObject> communityCardSlots = new List<GameObject>();
        
        // Action Panel
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
        
        // Result Panel
        private GameObject resultPanel;
        private TextMeshProUGUI resultTitleText;
        private TextMeshProUGUI resultDetailsText;
        private Button continueButton;
        private Button forfeitButton;
        
        // Game Over Panel
        private GameObject gameOverPanel;
        private TextMeshProUGUI gameOverTitleText;
        private TextMeshProUGUI gameOverDetailsText;
        private Button returnButton;
        
        // Current state
        private AdventureHandState _currentState;
        private bool _waitingForResponse = false;
        
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
            
            // Get initial hand state from server (should have been started when adventure started)
            // For now, request the current state
            RequestCurrentState();
        }
        
        private void RequestCurrentState()
        {
            // The adventure should already have a hand in progress
            // We'll handle state updates via events
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
            var bg = UIFactory.CreatePanel(_canvas.transform, "Background", new Color(0.1f, 0.15f, 0.1f));
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            
            BuildBossArea();
            BuildTableArea();
            BuildPlayerArea();
            BuildActionPanel();
            BuildResultPanel();
            BuildGameOverPanel();
            
            // Hide panels initially
            actionPanel.SetActive(false);
            resultPanel.SetActive(false);
            gameOverPanel.SetActive(false);
        }
        
        private void BuildBossArea()
        {
            var theme = Theme.Current;
            
            var bossArea = UIFactory.CreatePanel(_canvas.transform, "BossArea", Color.clear);
            var bossRect = bossArea.GetComponent<RectTransform>();
            bossRect.anchorMin = new Vector2(0, 0.7f);
            bossRect.anchorMax = new Vector2(1, 1);
            bossRect.sizeDelta = Vector2.zero;
            
            // Boss info panel
            var infoPanel = UIFactory.CreatePanel(bossArea.transform, "BossInfo", theme.cardPanelColor);
            var infoRect = infoPanel.GetComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0.3f, 0.1f);
            infoRect.anchorMax = new Vector2(0.7f, 0.9f);
            infoRect.sizeDelta = Vector2.zero;
            
            var hlg = infoPanel.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20;
            hlg.padding = new RectOffset(20, 20, 10, 10);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            
            // Boss avatar placeholder
            var avatarHolder = UIFactory.CreatePanel(infoPanel.transform, "Avatar", theme.dangerColor);
            avatarHolder.GetOrAddComponent<LayoutElement>().preferredWidth = 80;
            avatarHolder.GetOrAddComponent<LayoutElement>().preferredHeight = 80;
            bossAvatar = avatarHolder.GetComponent<Image>();
            
            // Boss name and chips
            var textPanel = UIFactory.CreatePanel(infoPanel.transform, "TextPanel", Color.clear);
            textPanel.GetOrAddComponent<LayoutElement>().flexibleWidth = 1;
            var vlg = textPanel.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 5;
            vlg.childAlignment = TextAnchor.MiddleLeft;
            
            bossNameText = UIFactory.CreateTitle(textPanel.transform, "BossName", "Boss", 28f);
            bossNameText.color = theme.dangerColor;
            
            bossChipsText = UIFactory.CreateText(textPanel.transform, "BossChips", "Chips: 0", 20f, theme.accentColor);
            
            // Boss cards area
            var cardsPanel = UIFactory.CreatePanel(infoPanel.transform, "BossCards", Color.clear);
            cardsPanel.GetOrAddComponent<LayoutElement>().preferredWidth = 160;
            var cardsHlg = cardsPanel.AddComponent<HorizontalLayoutGroup>();
            cardsHlg.spacing = 10;
            cardsHlg.childAlignment = TextAnchor.MiddleCenter;
            
            for (int i = 0; i < 2; i++)
            {
                var card = CreateCardSlot(cardsPanel.transform, $"BossCard{i}");
                bossCardSlots.Add(card);
            }
            
            // Taunt text
            bossTauntText = UIFactory.CreateText(bossArea.transform, "Taunt", "", 18f, theme.textSecondary);
            var tauntRect = bossTauntText.GetComponent<RectTransform>();
            tauntRect.anchorMin = new Vector2(0.2f, 0);
            tauntRect.anchorMax = new Vector2(0.8f, 0.15f);
            tauntRect.sizeDelta = Vector2.zero;
            bossTauntText.alignment = TextAlignmentOptions.Center;
            bossTauntText.fontStyle = FontStyles.Italic;
        }
        
        private void BuildTableArea()
        {
            var theme = Theme.Current;
            
            var tableArea = UIFactory.CreatePanel(_canvas.transform, "TableArea", new Color(0.1f, 0.3f, 0.15f));
            var tableRect = tableArea.GetComponent<RectTransform>();
            tableRect.anchorMin = new Vector2(0.1f, 0.35f);
            tableRect.anchorMax = new Vector2(0.9f, 0.7f);
            tableRect.sizeDelta = Vector2.zero;
            
            // Pot display
            potText = UIFactory.CreateTitle(tableArea.transform, "Pot", "Pot: 0", 32f);
            var potRect = potText.GetComponent<RectTransform>();
            potRect.anchorMin = new Vector2(0.4f, 0.7f);
            potRect.anchorMax = new Vector2(0.6f, 0.95f);
            potRect.sizeDelta = Vector2.zero;
            potText.alignment = TextAlignmentOptions.Center;
            potText.color = theme.accentColor;
            
            // Phase display
            phaseText = UIFactory.CreateText(tableArea.transform, "Phase", "Waiting...", 20f, theme.textSecondary);
            var phaseRect = phaseText.GetComponent<RectTransform>();
            phaseRect.anchorMin = new Vector2(0.4f, 0.55f);
            phaseRect.anchorMax = new Vector2(0.6f, 0.7f);
            phaseRect.sizeDelta = Vector2.zero;
            phaseText.alignment = TextAlignmentOptions.Center;
            
            // Community cards
            var commCards = UIFactory.CreatePanel(tableArea.transform, "CommunityCards", Color.clear);
            var commRect = commCards.GetComponent<RectTransform>();
            commRect.anchorMin = new Vector2(0.2f, 0.15f);
            commRect.anchorMax = new Vector2(0.8f, 0.55f);
            commRect.sizeDelta = Vector2.zero;
            
            var commHlg = commCards.AddComponent<HorizontalLayoutGroup>();
            commHlg.spacing = 15;
            commHlg.childAlignment = TextAnchor.MiddleCenter;
            commHlg.childControlWidth = false;
            commHlg.childForceExpandWidth = false;
            
            for (int i = 0; i < 5; i++)
            {
                var card = CreateCardSlot(commCards.transform, $"CommCard{i}");
                communityCardSlots.Add(card);
            }
        }
        
        private void BuildPlayerArea()
        {
            var theme = Theme.Current;
            
            var playerArea = UIFactory.CreatePanel(_canvas.transform, "PlayerArea", Color.clear);
            var playerRect = playerArea.GetComponent<RectTransform>();
            playerRect.anchorMin = new Vector2(0, 0.15f);
            playerRect.anchorMax = new Vector2(1, 0.35f);
            playerRect.sizeDelta = Vector2.zero;
            
            // Player info
            var infoPanel = UIFactory.CreatePanel(playerArea.transform, "PlayerInfo", theme.cardPanelColor);
            var infoRect = infoPanel.GetComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0.3f, 0.1f);
            infoRect.anchorMax = new Vector2(0.7f, 0.9f);
            infoRect.sizeDelta = Vector2.zero;
            
            var hlg = infoPanel.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20;
            hlg.padding = new RectOffset(20, 20, 10, 10);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            
            // Player cards
            var cardsPanel = UIFactory.CreatePanel(infoPanel.transform, "PlayerCards", Color.clear);
            cardsPanel.GetOrAddComponent<LayoutElement>().preferredWidth = 160;
            var cardsHlg = cardsPanel.AddComponent<HorizontalLayoutGroup>();
            cardsHlg.spacing = 10;
            cardsHlg.childAlignment = TextAnchor.MiddleCenter;
            
            for (int i = 0; i < 2; i++)
            {
                var card = CreateCardSlot(cardsPanel.transform, $"PlayerCard{i}");
                playerCardSlots.Add(card);
            }
            
            // Player chips
            var textPanel = UIFactory.CreatePanel(infoPanel.transform, "TextPanel", Color.clear);
            textPanel.GetOrAddComponent<LayoutElement>().flexibleWidth = 1;
            
            playerChipsText = UIFactory.CreateTitle(textPanel.transform, "PlayerChips", "Your Chips: 0", 24f);
            playerChipsText.color = theme.primaryColor;
            playerChipsText.alignment = TextAlignmentOptions.Center;
        }
        
        private void BuildActionPanel()
        {
            var theme = Theme.Current;
            
            actionPanel = UIFactory.CreatePanel(_canvas.transform, "ActionPanel", theme.cardPanelColor);
            var panelRect = actionPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0, 0);
            panelRect.anchorMax = new Vector2(1, 0.15f);
            panelRect.sizeDelta = Vector2.zero;
            
            var hlg = actionPanel.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 15;
            hlg.padding = new RectOffset(30, 30, 15, 15);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childForceExpandWidth = false;
            
            // Fold
            foldButton = UIFactory.CreateButton(actionPanel.transform, "Fold", "FOLD", OnFoldClick).GetComponent<Button>();
            foldButton.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 60);
            foldButton.GetComponent<Image>().color = theme.dangerColor;
            
            // Check
            checkButton = UIFactory.CreateButton(actionPanel.transform, "Check", "CHECK", OnCheckClick).GetComponent<Button>();
            checkButton.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 60);
            checkButton.GetComponent<Image>().color = theme.successColor;
            
            // Call
            var callContainer = UIFactory.CreatePanel(actionPanel.transform, "CallContainer", Color.clear);
            callContainer.GetOrAddComponent<LayoutElement>().preferredWidth = 120;
            
            callButton = UIFactory.CreateButton(callContainer.transform, "Call", "CALL", OnCallClick).GetComponent<Button>();
            var callRect = callButton.GetComponent<RectTransform>();
            callRect.anchorMin = new Vector2(0, 0.3f);
            callRect.anchorMax = new Vector2(1, 1);
            callRect.sizeDelta = Vector2.zero;
            callButton.GetComponent<Image>().color = theme.primaryColor;
            
            callAmountText = UIFactory.CreateText(callContainer.transform, "CallAmount", "", 14f, theme.textSecondary);
            var callAmtRect = callAmountText.GetComponent<RectTransform>();
            callAmtRect.anchorMin = new Vector2(0, 0);
            callAmtRect.anchorMax = new Vector2(1, 0.3f);
            callAmtRect.sizeDelta = Vector2.zero;
            callAmountText.alignment = TextAlignmentOptions.Center;
            
            // Bet slider and amount
            var betSection = UIFactory.CreatePanel(actionPanel.transform, "BetSection", Color.clear);
            betSection.GetOrAddComponent<LayoutElement>().preferredWidth = 250;
            
            betAmountText = UIFactory.CreateTitle(betSection.transform, "BetAmount", "0", 20f);
            var amtRect = betAmountText.GetComponent<RectTransform>();
            amtRect.anchorMin = new Vector2(0, 0.6f);
            amtRect.anchorMax = new Vector2(1, 1);
            amtRect.sizeDelta = Vector2.zero;
            betAmountText.alignment = TextAlignmentOptions.Center;
            betAmountText.color = theme.accentColor;
            
            betSlider = CreateSimpleSlider(betSection.transform);
            var sliderRect = betSlider.GetComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.05f, 0.2f);
            sliderRect.anchorMax = new Vector2(0.95f, 0.5f);
            sliderRect.sizeDelta = Vector2.zero;
            betSlider.onValueChanged.AddListener(OnBetSliderChanged);
            
            // Bet
            betButton = UIFactory.CreateButton(actionPanel.transform, "Bet", "BET", OnBetClick).GetComponent<Button>();
            betButton.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 60);
            betButton.GetComponent<Image>().color = theme.accentColor;
            
            // Raise
            raiseButton = UIFactory.CreateButton(actionPanel.transform, "Raise", "RAISE", OnRaiseClick).GetComponent<Button>();
            raiseButton.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 60);
            raiseButton.GetComponent<Image>().color = theme.accentColor;
            
            // All-In
            allInButton = UIFactory.CreateButton(actionPanel.transform, "AllIn", "ALL IN", OnAllInClick).GetComponent<Button>();
            allInButton.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 60);
            allInButton.GetComponent<Image>().color = new Color(0.8f, 0.2f, 0.5f);
        }
        
        private void BuildResultPanel()
        {
            var theme = Theme.Current;
            
            resultPanel = UIFactory.CreatePanel(_canvas.transform, "ResultPanel", new Color(0, 0, 0, 0.85f));
            var panelRect = resultPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            
            var content = UIFactory.CreatePanel(resultPanel.transform, "Content", theme.cardPanelColor);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.25f, 0.3f);
            contentRect.anchorMax = new Vector2(0.75f, 0.7f);
            contentRect.sizeDelta = Vector2.zero;
            
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 20;
            vlg.padding = new RectOffset(30, 30, 30, 30);
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlHeight = false;
            
            resultTitleText = UIFactory.CreateTitle(content.transform, "Title", "Hand Complete", 36f);
            resultTitleText.GetOrAddComponent<LayoutElement>().preferredHeight = 50;
            resultTitleText.alignment = TextAlignmentOptions.Center;
            
            resultDetailsText = UIFactory.CreateText(content.transform, "Details", "", 20f, theme.textSecondary);
            resultDetailsText.GetOrAddComponent<LayoutElement>().preferredHeight = 80;
            resultDetailsText.alignment = TextAlignmentOptions.Center;
            
            var buttonRow = UIFactory.CreatePanel(content.transform, "Buttons", Color.clear);
            buttonRow.GetOrAddComponent<LayoutElement>().preferredHeight = 60;
            var btnHlg = buttonRow.AddComponent<HorizontalLayoutGroup>();
            btnHlg.spacing = 30;
            btnHlg.childAlignment = TextAnchor.MiddleCenter;
            
            continueButton = UIFactory.CreateButton(buttonRow.transform, "Continue", "NEXT HAND", OnContinueClick).GetComponent<Button>();
            continueButton.GetComponent<RectTransform>().sizeDelta = new Vector2(180, 50);
            continueButton.GetComponent<Image>().color = theme.primaryColor;
            
            forfeitButton = UIFactory.CreateButton(buttonRow.transform, "Forfeit", "FORFEIT", OnForfeitClick).GetComponent<Button>();
            forfeitButton.GetComponent<RectTransform>().sizeDelta = new Vector2(140, 50);
            forfeitButton.GetComponent<Image>().color = theme.dangerColor;
        }
        
        private void BuildGameOverPanel()
        {
            var theme = Theme.Current;
            
            gameOverPanel = UIFactory.CreatePanel(_canvas.transform, "GameOverPanel", new Color(0, 0, 0, 0.9f));
            var panelRect = gameOverPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            
            var content = UIFactory.CreatePanel(gameOverPanel.transform, "Content", theme.cardPanelColor);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.2f, 0.2f);
            contentRect.anchorMax = new Vector2(0.8f, 0.8f);
            contentRect.sizeDelta = Vector2.zero;
            
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 25;
            vlg.padding = new RectOffset(40, 40, 40, 40);
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlHeight = false;
            
            gameOverTitleText = UIFactory.CreateTitle(content.transform, "Title", "GAME OVER", 48f);
            gameOverTitleText.GetOrAddComponent<LayoutElement>().preferredHeight = 70;
            gameOverTitleText.alignment = TextAlignmentOptions.Center;
            
            gameOverDetailsText = UIFactory.CreateText(content.transform, "Details", "", 22f, theme.textSecondary);
            gameOverDetailsText.GetOrAddComponent<LayoutElement>().preferredHeight = 150;
            gameOverDetailsText.alignment = TextAlignmentOptions.Center;
            
            returnButton = UIFactory.CreateButton(content.transform, "Return", "RETURN TO MAP", OnReturnClick).GetComponent<Button>();
            returnButton.GetOrAddComponent<LayoutElement>().preferredHeight = 60;
            returnButton.GetOrAddComponent<LayoutElement>().preferredWidth = 220;
            returnButton.GetComponent<Image>().color = theme.primaryColor;
        }
        
        private GameObject CreateCardSlot(Transform parent, string name)
        {
            var theme = Theme.Current;
            var card = UIFactory.CreatePanel(parent, name, theme.cardPanelColor);
            card.GetOrAddComponent<LayoutElement>().preferredWidth = 70;
            card.GetOrAddComponent<LayoutElement>().preferredHeight = 100;
            
            var text = UIFactory.CreateTitle(card.transform, "CardText", "?", 24f);
            text.alignment = TextAlignmentOptions.Center;
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            
            return card;
        }
        
        private Slider CreateSimpleSlider(Transform parent)
        {
            var theme = Theme.Current;
            
            var sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(parent, false);
            var rect = sliderObj.AddComponent<RectTransform>();
            
            var slider = sliderObj.AddComponent<Slider>();
            slider.minValue = 0;
            slider.maxValue = 10000;
            slider.wholeNumbers = true;
            
            // Background
            var bgObj = UIFactory.CreatePanel(sliderObj.transform, "Background", theme.backgroundColor);
            var bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            
            // Fill
            var fillObj = UIFactory.CreatePanel(sliderObj.transform, "Fill", theme.accentColor);
            var fillRect = fillObj.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0.5f, 1);
            fillRect.sizeDelta = Vector2.zero;
            slider.fillRect = fillRect;
            
            // Handle
            var handleObj = UIFactory.CreatePanel(sliderObj.transform, "Handle", Color.white);
            var handleRect = handleObj.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 30);
            slider.handleRect = handleRect;
            slider.targetGraphic = handleObj.GetComponent<Image>();
            
            return slider;
        }
        
        #region UI Updates
        
        public void UpdateFromState(AdventureHandState state)
        {
            _currentState = state;
            
            // Update pot
            potText.text = $"Pot: {state.pot}";
            
            // Update phase
            phaseText.text = GetPhaseDisplayName(state.phase);
            
            // Update chips
            playerChipsText.text = $"Your Chips: {state.playerChips}";
            bossChipsText.text = $"Chips: {state.bossChips}";
            
            // Update player cards
            UpdateCardSlots(playerCardSlots, state.playerCards);
            
            // Update boss cards (hidden until showdown)
            if (state.bossCards != null && state.bossCards.Count > 0)
            {
                UpdateCardSlots(bossCardSlots, state.bossCards);
            }
            else
            {
                // Show card backs
                foreach (var slot in bossCardSlots)
                {
                    var text = slot.GetComponentInChildren<TextMeshProUGUI>();
                    if (text) text.text = "🂠";
                }
            }
            
            // Update community cards
            UpdateCommunityCards(state.communityCards);
            
            // Show/hide action panel
            if (state.isPlayerTurn && !state.isHandComplete)
            {
                ShowActionButtons(state);
            }
            else
            {
                actionPanel.SetActive(false);
            }
            
            // Check for hand complete
            if (state.isHandComplete)
            {
                ShowHandResult(state);
            }
        }
        
        private void UpdateCardSlots(List<GameObject> slots, List<Card> cards)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                var text = slots[i].GetComponentInChildren<TextMeshProUGUI>();
                if (text == null) continue;
                
                if (cards != null && i < cards.Count && cards[i] != null && !cards[i].IsHidden)
                {
                    text.text = cards[i].ToString();
                    text.color = GetCardColor(cards[i]);
                }
                else
                {
                    text.text = "?";
                    text.color = Theme.Current.textSecondary;
                }
            }
        }
        
        private void UpdateCommunityCards(List<Card> cards)
        {
            for (int i = 0; i < communityCardSlots.Count; i++)
            {
                var text = communityCardSlots[i].GetComponentInChildren<TextMeshProUGUI>();
                if (text == null) continue;
                
                if (cards != null && i < cards.Count && cards[i] != null)
                {
                    text.text = cards[i].ToString();
                    text.color = GetCardColor(cards[i]);
                    communityCardSlots[i].SetActive(true);
                }
                else
                {
                    communityCardSlots[i].SetActive(i < GetExpectedCommunityCards(_currentState?.phase));
                    text.text = "";
                }
            }
        }
        
        private int GetExpectedCommunityCards(string phase)
        {
            return phase?.ToLower() switch
            {
                "flop" => 3,
                "turn" => 4,
                "river" => 5,
                "showdown" => 5,
                _ => 0
            };
        }
        
        private Color GetCardColor(Card card)
        {
            if (card == null) return Color.white;
            var suit = card.GetSuit();
            return (suit == CardSuit.Hearts || suit == CardSuit.Diamonds) 
                ? new Color(0.8f, 0.2f, 0.2f) 
                : Color.black;
        }
        
        private string GetPhaseDisplayName(string phase)
        {
            return phase?.ToLower() switch
            {
                "waiting" => "Waiting...",
                "preflop" => "Pre-Flop",
                "flop" => "Flop",
                "turn" => "Turn",
                "river" => "River",
                "showdown" => "Showdown",
                _ => phase ?? "..."
            };
        }
        
        private void ShowActionButtons(AdventureHandState state)
        {
            actionPanel.SetActive(true);
            
            int toCall = state.currentBet - state.playerBet;
            bool canCheck = toCall <= 0;
            bool hasBet = state.currentBet > 0;
            
            checkButton.gameObject.SetActive(canCheck);
            callButton.transform.parent.gameObject.SetActive(!canCheck);
            
            if (!canCheck)
            {
                callAmountText.text = toCall.ToString();
            }
            
            betButton.gameObject.SetActive(!hasBet);
            raiseButton.gameObject.SetActive(hasBet);
            
            // Update slider
            int minBet = hasBet ? state.currentBet * 2 : state.minRaise;
            betSlider.minValue = minBet;
            betSlider.maxValue = state.playerChips;
            betSlider.value = minBet;
            OnBetSliderChanged(minBet);
            
            // Disable buttons if waiting
            bool canAct = !_waitingForResponse;
            foldButton.interactable = canAct;
            checkButton.interactable = canAct;
            callButton.interactable = canAct;
            betButton.interactable = canAct;
            raiseButton.interactable = canAct;
            allInButton.interactable = canAct;
        }
        
        private void ShowHandResult(AdventureHandState state)
        {
            actionPanel.SetActive(false);
            resultPanel.SetActive(true);
            
            string winnerText = state.winner switch
            {
                "player" => "You Won!",
                "boss" => "Boss Wins",
                "tie" => "Split Pot",
                _ => "Hand Complete"
            };
            
            resultTitleText.text = winnerText;
            resultTitleText.color = state.winner == "player" ? Theme.Current.successColor : 
                                    state.winner == "boss" ? Theme.Current.dangerColor : 
                                    Theme.Current.textPrimary;
            
            string details = $"Your hand: {state.playerHandResult?.name ?? "?"}\n";
            details += $"Boss hand: {state.bossHandResult?.name ?? "?"}";
            resultDetailsText.text = details;
        }
        
        public void ShowGameOver(AdventureResult result)
        {
            actionPanel.SetActive(false);
            resultPanel.SetActive(false);
            gameOverPanel.SetActive(true);
            
            bool victory = result.status == "victory";
            gameOverTitleText.text = victory ? "VICTORY!" : "DEFEAT";
            gameOverTitleText.color = victory ? Theme.Current.successColor : Theme.Current.dangerColor;
            
            if (victory)
            {
                string details = $"You defeated {result.boss?.name}!\n\n";
                if (result.rewards != null)
                {
                    details += $"+{result.rewards.xp} XP\n";
                    details += $"+{result.rewards.coins} Coins\n";
                    if (result.rewards.chips > 0)
                        details += $"+{result.rewards.chips} Chips\n";
                    if (result.rewards.items?.Count > 0)
                        details += $"\nItems received: {result.rewards.items.Count}";
                }
                gameOverDetailsText.text = details;
            }
            else
            {
                gameOverDetailsText.text = $"{result.boss?.name} wins!\n\n\"{result.message}\"\n\n" +
                                          $"Entry fee lost: {result.entryFeeLost}\n" +
                                          $"Consolation XP: +{result.consolationXP}";
            }
        }
        
        #endregion
        
        #region Action Handlers
        
        private void OnFoldClick() => SendAction("fold");
        private void OnCheckClick() => SendAction("check");
        private void OnCallClick() => SendAction("call");
        private void OnBetClick() => SendAction("bet", (int)betSlider.value);
        private void OnRaiseClick() => SendAction("raise", (int)betSlider.value);
        private void OnAllInClick() => SendAction("allin");
        
        private void OnBetSliderChanged(float value)
        {
            betAmountText.text = ((int)value).ToString();
        }
        
        private void SendAction(string action, int amount = 0)
        {
            if (_waitingForResponse) return;
            _waitingForResponse = true;
            
            // Disable buttons
            actionPanel.SetActive(false);
            
            _gameService.SendAdventureAction(action, amount, response =>
            {
                _waitingForResponse = false;
                
                if (response.success)
                {
                    // Handle boss actions
                    if (response.bossActions != null)
                    {
                        foreach (var bossAction in response.bossActions)
                        {
                            ShowBossAction(bossAction);
                        }
                    }
                    
                    // Update state
                    if (response.state != null)
                    {
                        UpdateFromState(response.state);
                    }
                    
                    // Check for game end
                    if (response.status == "victory" || response.status == "defeat")
                    {
                        ShowGameOver(new AdventureResult
                        {
                            status = response.status,
                            boss = response.boss,
                            rewards = response.rewards,
                            message = response.message,
                            entryFeeLost = response.entryFeeLost,
                            consolationXP = response.consolationXP
                        });
                    }
                }
                else
                {
                    Debug.LogError($"Action failed: {response.error}");
                    // Re-enable action panel
                    if (_currentState != null && _currentState.isPlayerTurn)
                    {
                        ShowActionButtons(_currentState);
                    }
                }
            });
        }
        
        private void ShowBossAction(BossActionInfo action)
        {
            if (action.taunt != null)
            {
                bossTauntText.text = action.taunt;
            }
            Debug.Log($"[AdventureBattle] Boss: {action.action} {action.amount}");
        }
        
        private void OnContinueClick()
        {
            resultPanel.SetActive(false);
            
            // Request next hand
            _gameService.RequestNextAdventureHand(state =>
            {
                if (state != null)
                {
                    UpdateFromState(state);
                }
            });
        }
        
        private void OnForfeitClick()
        {
            resultPanel.SetActive(false);
            
            _gameService.ForfeitAdventure(success =>
            {
                SceneManager.LoadScene("AdventureScene");
            });
        }
        
        private void OnReturnClick()
        {
            SceneManager.LoadScene("AdventureScene");
        }
        
        #endregion
    }
    
    // NOTE: AdventureHandState and HandResultInfo are defined in NetworkModels.cs
}

