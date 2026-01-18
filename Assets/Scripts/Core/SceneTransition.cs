using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

namespace PokerClient.Core
{
    /// <summary>
    /// Scene transition manager for smooth fade transitions between scenes.
    /// </summary>
    public class SceneTransition : MonoBehaviour
    {
        public static SceneTransition Instance { get; private set; }
        
        private CanvasGroup _fadeOverlay;
        private Canvas _canvas;
        
        public float fadeDuration = 0.3f;
        public Color fadeColor = Color.black;
        
        private bool _isTransitioning = false;
        
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
            // Create canvas
            var canvasObj = new GameObject("TransitionCanvas");
            canvasObj.transform.SetParent(transform);
            _canvas = canvasObj.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 9999;
            canvasObj.AddComponent<CanvasScaler>();
            
            // Create fade overlay
            var overlayObj = new GameObject("FadeOverlay");
            overlayObj.transform.SetParent(canvasObj.transform, false);
            var rect = overlayObj.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            
            var img = overlayObj.AddComponent<Image>();
            img.color = fadeColor;
            img.raycastTarget = true;
            
            _fadeOverlay = overlayObj.AddComponent<CanvasGroup>();
            _fadeOverlay.alpha = 0;
            _fadeOverlay.blocksRaycasts = false;
            _fadeOverlay.interactable = false;
        }
        
        /// <summary>
        /// Transition to a scene with fade effect.
        /// </summary>
        public static void LoadScene(string sceneName)
        {
            if (Instance != null)
            {
                Instance.StartTransition(sceneName);
            }
            else
            {
                SceneManager.LoadScene(sceneName);
            }
        }
        
        /// <summary>
        /// Transition with custom fade duration.
        /// </summary>
        public static void LoadScene(string sceneName, float duration)
        {
            if (Instance != null)
            {
                Instance.fadeDuration = duration;
                Instance.StartTransition(sceneName);
            }
            else
            {
                SceneManager.LoadScene(sceneName);
            }
        }
        
        private void StartTransition(string sceneName)
        {
            if (_isTransitioning) return;
            StartCoroutine(TransitionCoroutine(sceneName));
        }
        
        private IEnumerator TransitionCoroutine(string sceneName)
        {
            _isTransitioning = true;
            _fadeOverlay.blocksRaycasts = true;
            
            // Fade out
            yield return FadeOut();
            
            // Load scene
            var asyncOp = SceneManager.LoadSceneAsync(sceneName);
            while (!asyncOp.isDone)
            {
                yield return null;
            }
            
            // Wait a frame for scene to initialize
            yield return null;
            
            // Fade in
            yield return FadeIn();
            
            _fadeOverlay.blocksRaycasts = false;
            _isTransitioning = false;
        }
        
        private IEnumerator FadeOut()
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                _fadeOverlay.alpha = elapsed / fadeDuration;
                yield return null;
            }
            _fadeOverlay.alpha = 1f;
        }
        
        private IEnumerator FadeIn()
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                _fadeOverlay.alpha = 1f - (elapsed / fadeDuration);
                yield return null;
            }
            _fadeOverlay.alpha = 0f;
        }
        
        /// <summary>
        /// Perform a quick flash effect (for emphasis).
        /// </summary>
        public static void Flash(Color color, float duration = 0.15f)
        {
            Instance?.StartCoroutine(Instance.FlashCoroutine(color, duration));
        }
        
        private IEnumerator FlashCoroutine(Color color, float duration)
        {
            var img = _fadeOverlay.GetComponent<Image>();
            var originalColor = img.color;
            img.color = color;
            
            _fadeOverlay.alpha = 0.8f;
            
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                _fadeOverlay.alpha = 0.8f * (1f - (elapsed / duration));
                yield return null;
            }
            
            _fadeOverlay.alpha = 0f;
            img.color = originalColor;
        }
    }
}



