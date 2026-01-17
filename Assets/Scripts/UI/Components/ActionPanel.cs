using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using PokerClient.UI;

namespace PokerClient.UI.Components
{
    /// <summary>
    /// Player action panel with Fold, Check/Call, Bet/Raise buttons and slider.
    /// </summary>
    public class ActionPanel : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button foldButton;
        [SerializeField] private Button checkCallButton;
        [SerializeField] private Button betRaiseButton;
        [SerializeField] private Button allInButton;
        
        [Header("Bet Controls")]
        [SerializeField] private Slider betSlider;
        [SerializeField] private TMP_InputField betInput;
        [SerializeField] private TextMeshProUGUI betAmountText;
        
        [Header("Quick Bet Buttons")]
        [SerializeField] private Button halfPotButton;
        [SerializeField] private Button potButton;
        [SerializeField] private Button doublePotButton;
        
        [Header("Text Components")]
        [SerializeField] private TextMeshProUGUI checkCallText;
        [SerializeField] private TextMeshProUGUI betRaiseText;
        
        private int _minBet;
        private int _maxBet;
        private int _currentBet;
        private int _callAmount;
        private int _potSize;
        private bool _canCheck;
        
        // Events
        public UnityEvent OnFold = new UnityEvent();
        public UnityEvent OnCheck = new UnityEvent();
        public UnityEvent<int> OnCall = new UnityEvent<int>();
        public UnityEvent<int> OnBet = new UnityEvent<int>();
        public UnityEvent<int> OnRaise = new UnityEvent<int>();
        public UnityEvent<int> OnAllIn = new UnityEvent<int>();
        
        private void Awake()
        {
            if (foldButton == null)
                BuildActionPanel();
            
            SetupEventListeners();
        }
        
        private void BuildActionPanel()
        {
            var theme = Theme.Current;
            var rect = GetComponent<RectTransform>();
            if (rect == null) rect = gameObject.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(500, 120);
            
            // Background panel
            var bg = UIFactory.CreatePanel(transform, "Background", theme.panelColor);
            UIFactory.FillParent(bg.GetComponent<RectTransform>());
            
            // Top row: Quick bet buttons and slider
            var topRow = UIFactory.CreateHorizontalGroup(bg.transform, "TopRow", 10);
            var topRect = topRow.GetComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0, 0.55f);
            topRect.anchorMax = new Vector2(1, 1);
            topRect.offsetMin = new Vector2(10, 5);
            topRect.offsetMax = new Vector2(-10, -5);
            
            // Bet amount display
            betAmountText = UIFactory.CreateText(topRow.transform, "BetAmount", "0", 18f, theme.secondaryColor);
            betAmountText.fontStyle = FontStyles.Bold;
            var betAmountRect = betAmountText.GetComponent<RectTransform>();
            betAmountRect.sizeDelta = new Vector2(80, 40);
            
            // Bet slider
            var sliderObj = new GameObject("BetSlider", typeof(RectTransform), typeof(Slider));
            sliderObj.transform.SetParent(topRow.transform, false);
            betSlider = sliderObj.GetComponent<Slider>();
            var sliderRect = sliderObj.GetComponent<RectTransform>();
            sliderRect.sizeDelta = new Vector2(200, 30);
            
            // Slider background
            var sliderBg = UIFactory.CreatePanel(sliderObj.transform, "Background", theme.cardPanelColor);
            var sliderBgRect = sliderBg.GetComponent<RectTransform>();
            UIFactory.FillParent(sliderBgRect);
            sliderBgRect.offsetMin = new Vector2(0, 10);
            sliderBgRect.offsetMax = new Vector2(0, -10);
            
            // Slider fill
            var sliderFill = UIFactory.CreatePanel(sliderObj.transform, "Fill", theme.primaryColor);
            var sliderFillRect = sliderFill.GetComponent<RectTransform>();
            sliderFillRect.anchorMin = Vector2.zero;
            sliderFillRect.anchorMax = new Vector2(0.5f, 1);
            sliderFillRect.offsetMin = new Vector2(0, 10);
            sliderFillRect.offsetMax = new Vector2(0, -10);
            
            // Slider handle
            var handle = UIFactory.CreatePanel(sliderObj.transform, "Handle", theme.textPrimary);
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 30);
            
            betSlider.fillRect = sliderFillRect;
            betSlider.handleRect = handleRect;
            betSlider.minValue = 0;
            betSlider.maxValue = 100;
            betSlider.wholeNumbers = true;
            
            // Quick bet buttons
            halfPotButton = UIFactory.CreateSecondaryButton(topRow.transform, "HalfPot", "1/2", null, 60, 35);
            potButton = UIFactory.CreateSecondaryButton(topRow.transform, "Pot", "POT", null, 60, 35);
            doublePotButton = UIFactory.CreateSecondaryButton(topRow.transform, "2xPot", "2x", null, 60, 35);
            
            // Bottom row: Action buttons
            var bottomRow = UIFactory.CreateHorizontalGroup(bg.transform, "BottomRow", 15);
            var bottomRect = bottomRow.GetComponent<RectTransform>();
            bottomRect.anchorMin = new Vector2(0, 0);
            bottomRect.anchorMax = new Vector2(1, 0.5f);
            bottomRect.offsetMin = new Vector2(10, 5);
            bottomRect.offsetMax = new Vector2(-10, -5);
            
            // Fold button
            foldButton = UIFactory.CreateDangerButton(bottomRow.transform, "Fold", "FOLD", null, 100, 45);
            
            // Check/Call button
            checkCallButton = UIFactory.CreateSecondaryButton(bottomRow.transform, "CheckCall", "CHECK", null, 120, 45);
            checkCallText = checkCallButton.GetComponentInChildren<TextMeshProUGUI>();
            
            // Bet/Raise button
            betRaiseButton = UIFactory.CreatePrimaryButton(bottomRow.transform, "BetRaise", "BET", null, 120, 45);
            betRaiseText = betRaiseButton.GetComponentInChildren<TextMeshProUGUI>();
            
            // All-in button
            allInButton = UIFactory.CreateButton(bottomRow.transform, "AllIn", "ALL IN", null, 
                theme.playerAllIn, 100, 45);
        }
        
        private void SetupEventListeners()
        {
            foldButton?.onClick.AddListener(() => OnFold?.Invoke());
            
            checkCallButton?.onClick.AddListener(() =>
            {
                if (_canCheck)
                    OnCheck?.Invoke();
                else
                    OnCall?.Invoke(_callAmount);
            });
            
            betRaiseButton?.onClick.AddListener(() =>
            {
                int amount = Mathf.RoundToInt(betSlider.value);
                if (_callAmount > 0)
                    OnRaise?.Invoke(amount);
                else
                    OnBet?.Invoke(amount);
            });
            
            allInButton?.onClick.AddListener(() => OnAllIn?.Invoke(_maxBet));
            
            betSlider?.onValueChanged.AddListener(UpdateBetDisplay);
            
            halfPotButton?.onClick.AddListener(() => SetBetAmount(_potSize / 2));
            potButton?.onClick.AddListener(() => SetBetAmount(_potSize));
            doublePotButton?.onClick.AddListener(() => SetBetAmount(_potSize * 2));
        }
        
        /// <summary>
        /// Configure the action panel for current game state
        /// </summary>
        public void Configure(int minBet, int maxBet, int callAmount, int potSize, bool canCheck)
        {
            _minBet = minBet;
            _maxBet = maxBet;
            _callAmount = callAmount;
            _potSize = potSize;
            _canCheck = canCheck;
            
            // Update slider
            betSlider.minValue = minBet;
            betSlider.maxValue = maxBet;
            betSlider.value = minBet;
            
            // Update check/call button
            if (canCheck)
            {
                checkCallText.text = "CHECK";
            }
            else
            {
                checkCallText.text = $"CALL {ChipStack.FormatChipValue(callAmount)}";
            }
            
            // Update bet/raise button
            betRaiseText.text = callAmount > 0 ? "RAISE" : "BET";
            
            UpdateBetDisplay(minBet);
        }
        
        /// <summary>
        /// Set bet amount from slider or quick buttons
        /// </summary>
        public void SetBetAmount(int amount)
        {
            amount = Mathf.Clamp(amount, _minBet, _maxBet);
            betSlider.value = amount;
            UpdateBetDisplay(amount);
        }
        
        private void UpdateBetDisplay(float value)
        {
            _currentBet = Mathf.RoundToInt(value);
            betAmountText.text = ChipStack.FormatChipValue(_currentBet);
        }
        
        /// <summary>
        /// Show/hide the panel
        /// </summary>
        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
        
        /// <summary>
        /// Enable/disable all buttons
        /// </summary>
        public void SetInteractable(bool interactable)
        {
            foldButton.interactable = interactable;
            checkCallButton.interactable = interactable;
            betRaiseButton.interactable = interactable;
            allInButton.interactable = interactable;
            betSlider.interactable = interactable;
            halfPotButton.interactable = interactable;
            potButton.interactable = interactable;
            doublePotButton.interactable = interactable;
        }
        
        /// <summary>
        /// Create action panel
        /// </summary>
        public static ActionPanel Create(Transform parent)
        {
            var panelObj = new GameObject("ActionPanel", typeof(RectTransform), typeof(ActionPanel));
            panelObj.transform.SetParent(parent, false);
            return panelObj.GetComponent<ActionPanel>();
        }
    }
}



