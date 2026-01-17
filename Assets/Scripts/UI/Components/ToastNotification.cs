using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

namespace PokerClient.UI.Components
{
    /// <summary>
    /// Toast notification system for displaying temporary messages.
    /// </summary>
    public class ToastNotification : MonoBehaviour
    {
        public static ToastNotification Instance { get; private set; }
        
        private RectTransform _container;
        private Queue<ToastData> _pendingToasts = new Queue<ToastData>();
        private List<GameObject> _activeToasts = new List<GameObject>();
        private const int MAX_VISIBLE = 3;
        
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
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                var canvasObj = new GameObject("ToastCanvas");
                canvasObj.transform.SetParent(transform);
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 999;
                canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasObj.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
                canvasObj.AddComponent<GraphicRaycaster>();
            }
            
            var container = new GameObject("ToastContainer");
            container.transform.SetParent(canvas.transform, false);
            _container = container.AddComponent<RectTransform>();
            _container.anchorMin = new Vector2(0.3f, 0.85f);
            _container.anchorMax = new Vector2(0.7f, 0.98f);
            _container.sizeDelta = Vector2.zero;
            
            var vlg = container.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 5;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = false;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
        }
        
        public static void Show(string message, ToastType type = ToastType.Info, float duration = 3f)
        {
            Instance?.EnqueueToast(new ToastData { message = message, type = type, duration = duration });
        }
        
        public static void Success(string message) => Show(message, ToastType.Success);
        public static void Error(string message) => Show(message, ToastType.Error, 5f);
        public static void Warning(string message) => Show(message, ToastType.Warning, 4f);
        public static void Info(string message) => Show(message, ToastType.Info);
        
        private void EnqueueToast(ToastData data)
        {
            _pendingToasts.Enqueue(data);
            ProcessQueue();
        }
        
        private void ProcessQueue()
        {
            while (_pendingToasts.Count > 0 && _activeToasts.Count < MAX_VISIBLE)
            {
                var data = _pendingToasts.Dequeue();
                ShowToast(data);
            }
        }
        
        private void ShowToast(ToastData data)
        {
            var toast = CreateToastObject(data);
            _activeToasts.Add(toast);
            StartCoroutine(AnimateToast(toast, data.duration));
        }
        
        private GameObject CreateToastObject(ToastData data)
        {
            var toast = UIFactory.CreatePanel(_container, "Toast", GetBackgroundColor(data.type));
            toast.GetOrAddComponent<LayoutElement>().preferredHeight = 50;
            
            var cg = toast.AddComponent<CanvasGroup>();
            cg.alpha = 0;
            
            var icon = GetIcon(data.type);
            var text = UIFactory.CreateText(toast.transform, "Message", $"{icon} {data.message}", 18f, Color.white);
            text.alignment = TextAlignmentOptions.Center;
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            
            return toast;
        }
        
        private IEnumerator AnimateToast(GameObject toast, float duration)
        {
            var cg = toast.GetComponent<CanvasGroup>();
            var rect = toast.GetComponent<RectTransform>();
            
            // Fade in
            float fadeTime = 0.2f;
            for (float t = 0; t < fadeTime; t += Time.deltaTime)
            {
                cg.alpha = t / fadeTime;
                yield return null;
            }
            cg.alpha = 1;
            
            // Wait
            yield return new WaitForSeconds(duration);
            
            // Fade out
            for (float t = 0; t < fadeTime; t += Time.deltaTime)
            {
                cg.alpha = 1 - (t / fadeTime);
                yield return null;
            }
            
            _activeToasts.Remove(toast);
            Destroy(toast);
            
            ProcessQueue();
        }
        
        private Color GetBackgroundColor(ToastType type)
        {
            return type switch
            {
                ToastType.Success => new Color(0.2f, 0.7f, 0.3f, 0.95f),
                ToastType.Error => new Color(0.8f, 0.2f, 0.2f, 0.95f),
                ToastType.Warning => new Color(0.9f, 0.6f, 0.1f, 0.95f),
                _ => new Color(0.2f, 0.4f, 0.7f, 0.95f)
            };
        }
        
        private string GetIcon(ToastType type)
        {
            return type switch
            {
                ToastType.Success => "✓",
                ToastType.Error => "✕",
                ToastType.Warning => "⚠",
                _ => "ℹ"
            };
        }
    }
    
    public enum ToastType
    {
        Info,
        Success,
        Warning,
        Error
    }
    
    public struct ToastData
    {
        public string message;
        public ToastType type;
        public float duration;
    }
}

