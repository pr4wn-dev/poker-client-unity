using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PokerClient.Networking;

namespace PokerClient.UI.Components
{
    /// <summary>
    /// Rebuy/Add Chips panel for adding more chips to your table stack.
    /// </summary>
    public class RebuyPanel : MonoBehaviour
    {
        private RectTransform _rect;
        private CanvasGroup _canvasGroup;
        private GameService _gameService;
        
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _accountBalanceText;
        private TextMeshProUGUI _tableStackText;
        private TextMeshProUGUI _amountText;
        private Slider _amountSlider;
        private Button _rebuyButton;
        private Button _cancelButton;
        private TextMeshProUGUI _errorText;
        
        private int _accountBalance;
        private int _tableStack;
        private int _minBuyIn;
        private int _maxBuyIn;
        
        public System.Action OnRebuySuccess;
        public System.Action OnCancel;
        
        public static RebuyPanel Create(Transform parent)
        {
            var go = new GameObject("RebuyPanel");
            go.transform.SetParent(parent, false);
            var panel = go.AddComponent<RebuyPanel>();
            panel.Initialize();
            return panel;
        }
        
        private void Initialize()
        {
            var theme = Theme.Current;
            
            _rect = gameObject.AddComponent<RectTransform>();
            _rect.anchorMin = Vector2.zero;
            _rect.anchorMax = Vector2.one;
            _rect.sizeDelta = Vector2.zero;
            
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
            // Dimmed background
            var bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.8f);
            
            // Content panel
            var content = UIFactory.CreatePanel(transform, "Content", theme.cardPanelColor);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.25f, 0.25f);
            contentRect.anchorMax = new Vector2(0.75f, 0.75f);
            contentRect.sizeDelta = Vector2.zero;
            
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 20;
            vlg.padding = new RectOffset(30, 30, 30, 30);
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            
            // Title
            _titleText = UIFactory.CreateTitle(content.transform, "Title", "ADD CHIPS", 32f);
            _titleText.GetOrAddComponent<LayoutElement>().preferredHeight = 45;
            _titleText.alignment = TextAlignmentOptions.Center;
            _titleText.color = theme.accentColor;
            
            // Account balance
            _accountBalanceText = UIFactory.CreateText(content.transform, "AccountBalance", 
                "Account Balance: 0", 18f, theme.textPrimary);
            _accountBalanceText.GetOrAddComponent<LayoutElement>().preferredHeight = 30;
            _accountBalanceText.alignment = TextAlignmentOptions.Center;
            
            // Table stack
            _tableStackText = UIFactory.CreateText(content.transform, "TableStack", 
                "Current Stack: 0", 18f, theme.textPrimary);
            _tableStackText.GetOrAddComponent<LayoutElement>().preferredHeight = 30;
            _tableStackText.alignment = TextAlignmentOptions.Center;
            
            // Amount display
            _amountText = UIFactory.CreateTitle(content.transform, "Amount", "0", 36f);
            _amountText.GetOrAddComponent<LayoutElement>().preferredHeight = 50;
            _amountText.alignment = TextAlignmentOptions.Center;
            _amountText.color = theme.successColor;
            
            // Slider
            var sliderContainer = UIFactory.CreatePanel(content.transform, "SliderContainer", Color.clear);
            sliderContainer.GetOrAddComponent<LayoutElement>().preferredHeight = 40;
            
            _amountSlider = CreateSlider(sliderContainer.transform);
            _amountSlider.onValueChanged.AddListener(OnSliderChanged);
            
            // Quick amount buttons
            var quickRow = UIFactory.CreatePanel(content.transform, "QuickButtons", Color.clear);
            quickRow.GetOrAddComponent<LayoutElement>().preferredHeight = 45;
            var hlg = quickRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 15;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            
            CreateQuickButton(quickRow.transform, "Min", () => SetAmount(_minBuyIn));
            CreateQuickButton(quickRow.transform, "50%", () => SetAmount(Mathf.RoundToInt(_maxBuyIn * 0.5f)));
            CreateQuickButton(quickRow.transform, "75%", () => SetAmount(Mathf.RoundToInt(_maxBuyIn * 0.75f)));
            CreateQuickButton(quickRow.transform, "Max", () => SetAmount(_maxBuyIn));
            
            // Error text
            _errorText = UIFactory.CreateText(content.transform, "Error", "", 16f, theme.dangerColor);
            _errorText.GetOrAddComponent<LayoutElement>().preferredHeight = 25;
            _errorText.alignment = TextAlignmentOptions.Center;
            _errorText.gameObject.SetActive(false);
            
            // Buttons
            var buttonRow = UIFactory.CreatePanel(content.transform, "Buttons", Color.clear);
            buttonRow.GetOrAddComponent<LayoutElement>().preferredHeight = 55;
            var btnHlg = buttonRow.AddComponent<HorizontalLayoutGroup>();
            btnHlg.spacing = 30;
            btnHlg.childAlignment = TextAnchor.MiddleCenter;
            btnHlg.childControlWidth = false;
            
            _rebuyButton = UIFactory.CreateButton(buttonRow.transform, "Rebuy", "ADD CHIPS", OnRebuyClick).GetComponent<Button>();
            _rebuyButton.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 50);
            _rebuyButton.GetComponent<Image>().color = theme.primaryColor;
            
            _cancelButton = UIFactory.CreateButton(buttonRow.transform, "Cancel", "CANCEL", OnCancelClick).GetComponent<Button>();
            _cancelButton.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 50);
            _cancelButton.GetComponent<Image>().color = theme.dangerColor;
            
            gameObject.SetActive(false);
        }
        
        private Slider CreateSlider(Transform parent)
        {
            var theme = Theme.Current;
            
            var sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(parent, false);
            var rect = sliderObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            
            var slider = sliderObj.AddComponent<Slider>();
            slider.minValue = 0;
            slider.maxValue = 100000;
            slider.wholeNumbers = true;
            
            var bgImg = UIFactory.CreatePanel(sliderObj.transform, "Background", theme.backgroundColor);
            var bgRect = bgImg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            
            var fill = UIFactory.CreatePanel(sliderObj.transform, "Fill", theme.primaryColor);
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0.5f, 1);
            fillRect.sizeDelta = Vector2.zero;
            slider.fillRect = fillRect;
            
            var handle = UIFactory.CreatePanel(sliderObj.transform, "Handle", Color.white);
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 35);
            slider.handleRect = handleRect;
            slider.targetGraphic = handle.GetComponent<Image>();
            
            return slider;
        }
        
        private void CreateQuickButton(Transform parent, string label, System.Action onClick)
        {
            var btn = UIFactory.CreateButton(parent, $"Quick{label}", label, () => onClick?.Invoke());
            btn.GetComponent<RectTransform>().sizeDelta = new Vector2(70, 40);
            btn.GetComponent<Image>().color = Theme.Current.cardPanelColor;
        }
        
        public void Show(int accountBalance, int tableStack, int minBuyIn, int maxBuyIn)
        {
            _gameService = GameService.Instance;
            
            _accountBalance = accountBalance;
            _tableStack = tableStack;
            _minBuyIn = Mathf.Max(minBuyIn, 1);
            _maxBuyIn = Mathf.Min(maxBuyIn - tableStack, accountBalance);
            
            if (_maxBuyIn <= 0)
            {
                Debug.LogWarning("Cannot rebuy - at max or no chips");
                return;
            }
            
            _accountBalanceText.text = $"Account Balance: {accountBalance:N0}";
            _tableStackText.text = $"Current Stack: {tableStack:N0}";
            
            _amountSlider.minValue = _minBuyIn;
            _amountSlider.maxValue = _maxBuyIn;
            _amountSlider.value = _minBuyIn;
            
            OnSliderChanged(_minBuyIn);
            
            _errorText.gameObject.SetActive(false);
            gameObject.SetActive(true);
        }
        
        public void Hide()
        {
            gameObject.SetActive(false);
        }
        
        private void SetAmount(int amount)
        {
            amount = Mathf.Clamp(amount, _minBuyIn, _maxBuyIn);
            _amountSlider.value = amount;
        }
        
        private void OnSliderChanged(float value)
        {
            int amount = Mathf.RoundToInt(value);
            _amountText.text = $"+{amount:N0}";
        }
        
        private void OnRebuyClick()
        {
            int amount = Mathf.RoundToInt(_amountSlider.value);
            
            _rebuyButton.interactable = false;
            _errorText.gameObject.SetActive(false);
            
            _gameService?.Rebuy(amount, (success, newStack, balance, error) =>
            {
                _rebuyButton.interactable = true;
                
                if (success)
                {
                    Hide();
                    OnRebuySuccess?.Invoke();
                }
                else
                {
                    _errorText.text = error ?? "Rebuy failed";
                    _errorText.gameObject.SetActive(true);
                }
            });
        }
        
        private void OnCancelClick()
        {
            Hide();
            OnCancel?.Invoke();
        }
    }
}

