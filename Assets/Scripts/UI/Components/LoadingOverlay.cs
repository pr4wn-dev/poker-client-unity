using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace PokerClient.UI.Components
{
    /// <summary>
    /// Full-screen loading overlay with spinner and message.
    /// </summary>
    public class LoadingOverlay : MonoBehaviour
    {
        public static LoadingOverlay Instance { get; private set; }
        
        private CanvasGroup _canvasGroup;
        private TextMeshProUGUI _messageText;
        private Image _spinnerImage;
        private RectTransform _spinnerRect;
        
        private bool _isShowing = false;
        private Coroutine _spinCoroutine;
        
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
            var canvas = GetComponent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 998;
                gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                gameObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
                gameObject.AddComponent<GraphicRaycaster>();
            }
            
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0;
            _canvasGroup.blocksRaycasts = false;
            
            // Background
            var bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.85f);
            
            var rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            
            // Spinner container
            var spinnerContainer = new GameObject("SpinnerContainer");
            spinnerContainer.transform.SetParent(transform, false);
            _spinnerRect = spinnerContainer.AddComponent<RectTransform>();
            _spinnerRect.anchorMin = new Vector2(0.5f, 0.5f);
            _spinnerRect.anchorMax = new Vector2(0.5f, 0.5f);
            _spinnerRect.sizeDelta = new Vector2(80, 80);
            _spinnerRect.anchoredPosition = new Vector2(0, 30);
            
            _spinnerImage = spinnerContainer.AddComponent<Image>();
            _spinnerImage.color = new Color(1f, 0.85f, 0.2f);
            
            // Create a simple spinner shape (circle with gap)
            // In real implementation, use a spinner sprite
            
            // Message text
            var msgObj = new GameObject("Message");
            msgObj.transform.SetParent(transform, false);
            var msgRect = msgObj.AddComponent<RectTransform>();
            msgRect.anchorMin = new Vector2(0.2f, 0.35f);
            msgRect.anchorMax = new Vector2(0.8f, 0.45f);
            msgRect.sizeDelta = Vector2.zero;
            
            _messageText = msgObj.AddComponent<TextMeshProUGUI>();
            _messageText.text = "Loading...";
            _messageText.fontSize = 24;
            _messageText.alignment = TextAlignmentOptions.Center;
            _messageText.color = Color.white;
            
            gameObject.SetActive(false);
        }
        
        public static void Show(string message = "Loading...")
        {
            Instance?.ShowOverlay(message);
        }
        
        public static void Hide()
        {
            Instance?.HideOverlay();
        }
        
        public static void UpdateMessage(string message)
        {
            if (Instance != null && Instance._messageText != null)
            {
                Instance._messageText.text = message;
            }
        }
        
        private void ShowOverlay(string message)
        {
            if (_isShowing) return;
            _isShowing = true;
            
            _messageText.text = message;
            gameObject.SetActive(true);
            
            StartCoroutine(FadeIn());
            _spinCoroutine = StartCoroutine(SpinAnimation());
        }
        
        private void HideOverlay()
        {
            if (!_isShowing) return;
            _isShowing = false;
            
            if (_spinCoroutine != null)
            {
                StopCoroutine(_spinCoroutine);
            }
            
            StartCoroutine(FadeOut());
        }
        
        private IEnumerator FadeIn()
        {
            float duration = 0.2f;
            _canvasGroup.blocksRaycasts = true;
            
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                _canvasGroup.alpha = t / duration;
                yield return null;
            }
            _canvasGroup.alpha = 1;
        }
        
        private IEnumerator FadeOut()
        {
            float duration = 0.2f;
            
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                _canvasGroup.alpha = 1 - (t / duration);
                yield return null;
            }
            _canvasGroup.alpha = 0;
            _canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }
        
        private IEnumerator SpinAnimation()
        {
            while (_isShowing)
            {
                _spinnerRect.Rotate(0, 0, -360 * Time.deltaTime);
                yield return null;
            }
        }
    }
}



