using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace PokerClient.UI.Components
{
    /// <summary>
    /// Reusable confirmation dialog for Yes/No prompts.
    /// </summary>
    public class ConfirmDialog : MonoBehaviour
    {
        public static ConfirmDialog Instance { get; private set; }
        
        private RectTransform _rect;
        private CanvasGroup _canvasGroup;
        
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _messageText;
        private Button _confirmButton;
        private Button _cancelButton;
        private TextMeshProUGUI _confirmButtonText;
        private TextMeshProUGUI _cancelButtonText;
        
        private Action _onConfirm;
        private Action _onCancel;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            Initialize();
        }
        
        private void Initialize()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 997;
            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            gameObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            gameObject.AddComponent<GraphicRaycaster>();
            
            // Container
            var container = new GameObject("Container");
            container.transform.SetParent(transform, false);
            _rect = container.AddComponent<RectTransform>();
            _rect.anchorMin = Vector2.zero;
            _rect.anchorMax = Vector2.one;
            _rect.sizeDelta = Vector2.zero;
            
            _canvasGroup = container.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0;
            _canvasGroup.blocksRaycasts = false;
            
            // Background
            var bg = container.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.8f);
            
            var theme = Theme.Current;
            
            // Dialog panel
            var dialog = UIFactory.CreatePanel(container.transform, "Dialog", theme.cardPanelColor);
            var dialogRect = dialog.GetComponent<RectTransform>();
            dialogRect.anchorMin = new Vector2(0.3f, 0.35f);
            dialogRect.anchorMax = new Vector2(0.7f, 0.65f);
            dialogRect.sizeDelta = Vector2.zero;
            
            // Title
            _titleText = UIFactory.CreateTitle(dialog.transform, "Title", "Confirm", 28f);
            _titleText.color = theme.textPrimary;
            _titleText.alignment = TextAlignmentOptions.Center;
            var titleRect = _titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.05f, 0.7f);
            titleRect.anchorMax = new Vector2(0.95f, 0.95f);
            titleRect.sizeDelta = Vector2.zero;
            
            // Message
            _messageText = UIFactory.CreateText(dialog.transform, "Message", "Are you sure?", 18f, theme.textSecondary);
            _messageText.alignment = TextAlignmentOptions.Center;
            _messageText.enableWordWrapping = true;
            var msgRect = _messageText.GetComponent<RectTransform>();
            msgRect.anchorMin = new Vector2(0.05f, 0.35f);
            msgRect.anchorMax = new Vector2(0.95f, 0.7f);
            msgRect.sizeDelta = Vector2.zero;
            
            // Buttons
            var btnContainer = UIFactory.CreatePanel(dialog.transform, "Buttons", Color.clear);
            var btnRect = btnContainer.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.1f, 0.08f);
            btnRect.anchorMax = new Vector2(0.9f, 0.32f);
            btnRect.sizeDelta = Vector2.zero;
            
            var hlg = btnContainer.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 30;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            
            // Cancel button
            var cancelGo = UIFactory.CreateButton(btnContainer.transform, "Cancel", "CANCEL", OnCancelClick);
            cancelGo.GetComponent<RectTransform>().sizeDelta = new Vector2(140, 50);
            _cancelButton = cancelGo.GetComponent<Button>();
            _cancelButton.GetComponent<Image>().color = theme.dangerColor;
            _cancelButtonText = _cancelButton.GetComponentInChildren<TextMeshProUGUI>();
            
            // Confirm button
            var confirmGo = UIFactory.CreateButton(btnContainer.transform, "Confirm", "CONFIRM", OnConfirmClick);
            confirmGo.GetComponent<RectTransform>().sizeDelta = new Vector2(140, 50);
            _confirmButton = confirmGo.GetComponent<Button>();
            _confirmButton.GetComponent<Image>().color = theme.successColor;
            _confirmButtonText = _confirmButton.GetComponentInChildren<TextMeshProUGUI>();
        }
        
        /// <summary>
        /// Show a confirmation dialog.
        /// </summary>
        public static void Show(string title, string message, Action onConfirm, Action onCancel = null, 
            string confirmText = "CONFIRM", string cancelText = "CANCEL")
        {
            if (Instance == null)
            {
                var go = new GameObject("ConfirmDialog");
                go.AddComponent<ConfirmDialog>();
            }
            
            Instance.ShowDialog(title, message, onConfirm, onCancel, confirmText, cancelText);
        }
        
        /// <summary>
        /// Show a simple yes/no dialog.
        /// </summary>
        public static void ShowYesNo(string message, Action onYes, Action onNo = null)
        {
            Show("Confirm", message, onYes, onNo, "YES", "NO");
        }
        
        /// <summary>
        /// Show a danger confirmation (red confirm button).
        /// </summary>
        public static void ShowDanger(string title, string message, Action onConfirm, Action onCancel = null)
        {
            Show(title, message, onConfirm, onCancel, "DELETE", "CANCEL");
            if (Instance != null)
            {
                Instance._confirmButton.GetComponent<Image>().color = Theme.Current.dangerColor;
            }
        }
        
        private void ShowDialog(string title, string message, Action onConfirm, Action onCancel, 
            string confirmText, string cancelText)
        {
            _titleText.text = title;
            _messageText.text = message;
            _confirmButtonText.text = confirmText;
            _cancelButtonText.text = cancelText;
            
            _onConfirm = onConfirm;
            _onCancel = onCancel;
            
            // Reset confirm button color
            _confirmButton.GetComponent<Image>().color = Theme.Current.successColor;
            
            _canvasGroup.alpha = 1;
            _canvasGroup.blocksRaycasts = true;
        }
        
        private void Hide()
        {
            _canvasGroup.alpha = 0;
            _canvasGroup.blocksRaycasts = false;
            
            _onConfirm = null;
            _onCancel = null;
        }
        
        private void OnConfirmClick()
        {
            _onConfirm?.Invoke();
            Hide();
        }
        
        private void OnCancelClick()
        {
            _onCancel?.Invoke();
            Hide();
        }
    }
}



