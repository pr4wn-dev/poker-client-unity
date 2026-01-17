using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

namespace PokerClient.UI
{
    /// <summary>
    /// Extension methods for UI components
    /// </summary>
    public static class UIExtensions
    {
        /// <summary>
        /// Get LayoutElement, adding it if it doesn't exist
        /// </summary>
        public static LayoutElement GetOrAddLayoutElement(this GameObject go)
        {
            var le = go.GetComponent<LayoutElement>();
            if (le == null)
                le = go.AddComponent<LayoutElement>();
            return le;
        }
        
        public static LayoutElement GetOrAddLayoutElement(this Component comp)
        {
            return comp.gameObject.GetOrAddLayoutElement();
        }
    }
    
    /// <summary>
    /// Factory for creating UI elements with consistent styling.
    /// All visuals are created programmatically - easy to swap for custom assets later.
    /// </summary>
    public static class UIFactory
    {
        #region Panels & Containers
        
        /// <summary>
        /// Create a styled panel
        /// </summary>
        public static GameObject CreatePanel(Transform parent, string name, Color? color = null)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            panel.transform.SetParent(parent, false);
            
            var image = panel.GetComponent<Image>();
            image.color = color ?? Theme.Current.panelColor;
            image.raycastTarget = true;
            
            return panel;
        }
        
        /// <summary>
        /// Create a panel with rounded corners (uses sliced sprite technique)
        /// </summary>
        public static GameObject CreateRoundedPanel(Transform parent, string name, Color? color = null, float radius = 8f)
        {
            var panel = CreatePanel(parent, name, color);
            // Note: For actual rounded corners, you'd assign a 9-sliced sprite here
            // For now, we use solid colors - swap the sprite reference later
            return panel;
        }
        
        /// <summary>
        /// Create a horizontal layout group
        /// </summary>
        public static GameObject CreateHorizontalGroup(Transform parent, string name, float spacing = 10f, 
            TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var group = new GameObject(name, typeof(RectTransform), typeof(HorizontalLayoutGroup));
            group.transform.SetParent(parent, false);
            
            var layout = group.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = alignment;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            
            return group;
        }
        
        /// <summary>
        /// Create a vertical layout group
        /// </summary>
        public static GameObject CreateVerticalGroup(Transform parent, string name, float spacing = 10f,
            TextAnchor alignment = TextAnchor.UpperCenter)
        {
            var group = new GameObject(name, typeof(RectTransform), typeof(VerticalLayoutGroup));
            group.transform.SetParent(parent, false);
            
            var layout = group.GetComponent<VerticalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = alignment;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
            
            return group;
        }
        
        #endregion
        
        #region Text
        
        /// <summary>
        /// Create styled text (uses TextMeshPro)
        /// </summary>
        public static TextMeshProUGUI CreateText(Transform parent, string name, string text, 
            float fontSize = 18f, Color? color = null, TextAlignmentOptions alignment = TextAlignmentOptions.Center)
        {
            var textObj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(parent, false);
            
            var tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color ?? Theme.Current.textPrimary;
            tmp.alignment = alignment;
            tmp.fontStyle = FontStyles.Normal;
            
            return tmp;
        }
        
        /// <summary>
        /// Create a title/header text
        /// </summary>
        public static TextMeshProUGUI CreateTitle(Transform parent, string name, string text, float fontSize = 32f)
        {
            var tmp = CreateText(parent, name, text, fontSize, Theme.Current.textPrimary);
            tmp.fontStyle = FontStyles.Bold;
            return tmp;
        }
        
        #endregion
        
        #region Buttons
        
        /// <summary>
        /// Create a styled button
        /// </summary>
        public static Button CreateButton(Transform parent, string name, string text, 
            UnityAction onClick = null, Color? color = null, float width = 150f, float height = 50f)
        {
            var buttonObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), 
                typeof(Image), typeof(Button));
            buttonObj.transform.SetParent(parent, false);
            
            var rect = buttonObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);
            
            var image = buttonObj.GetComponent<Image>();
            image.color = color ?? Theme.Current.buttonPrimary;
            
            var button = buttonObj.GetComponent<Button>();
            
            // Set up color transitions
            var colors = button.colors;
            colors.normalColor = color ?? Theme.Current.buttonPrimary;
            colors.highlightedColor = Theme.Current.buttonPrimaryHover;
            colors.pressedColor = Theme.Current.buttonPrimary * 0.8f;
            colors.disabledColor = Theme.Current.buttonDisabled;
            button.colors = colors;
            
            if (onClick != null)
                button.onClick.AddListener(onClick);
            
            // Add text
            var textComponent = CreateText(buttonObj.transform, "Text", text, 18f, Theme.Current.textPrimary);
            var textRect = textComponent.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            
            return button;
        }
        
        /// <summary>
        /// Create a primary action button (green)
        /// </summary>
        public static Button CreatePrimaryButton(Transform parent, string name, string text, 
            UnityAction onClick = null, float width = 150f, float height = 50f)
        {
            return CreateButton(parent, name, text, onClick, Theme.Current.buttonPrimary, width, height);
        }
        
        /// <summary>
        /// Create a secondary button (gray)
        /// </summary>
        public static Button CreateSecondaryButton(Transform parent, string name, string text,
            UnityAction onClick = null, float width = 150f, float height = 50f)
        {
            return CreateButton(parent, name, text, onClick, Theme.Current.buttonSecondary, width, height);
        }
        
        /// <summary>
        /// Create a danger button (red)
        /// </summary>
        public static Button CreateDangerButton(Transform parent, string name, string text,
            UnityAction onClick = null, float width = 150f, float height = 50f)
        {
            return CreateButton(parent, name, text, onClick, Theme.Current.buttonDanger, width, height);
        }
        
        #endregion
        
        #region Input Fields
        
        /// <summary>
        /// Create a styled input field
        /// </summary>
        public static TMP_InputField CreateInputField(Transform parent, string name, string placeholder = "",
            float width = 250f, float height = 45f, TMP_InputField.ContentType contentType = TMP_InputField.ContentType.Standard)
        {
            var inputObj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), 
                typeof(Image), typeof(TMP_InputField));
            inputObj.transform.SetParent(parent, false);
            
            var rect = inputObj.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(width, height);
            
            var image = inputObj.GetComponent<Image>();
            image.color = Theme.Current.cardPanelColor;
            
            // Text area
            var textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
            textArea.transform.SetParent(inputObj.transform, false);
            var textAreaRect = textArea.GetComponent<RectTransform>();
            textAreaRect.anchorMin = Vector2.zero;
            textAreaRect.anchorMax = Vector2.one;
            textAreaRect.offsetMin = new Vector2(10, 5);
            textAreaRect.offsetMax = new Vector2(-10, -5);
            
            // Placeholder
            var placeholderText = CreateText(textArea.transform, "Placeholder", placeholder, 16f, 
                Theme.Current.textMuted, TextAlignmentOptions.Left);
            var placeholderRect = placeholderText.GetComponent<RectTransform>();
            placeholderRect.anchorMin = Vector2.zero;
            placeholderRect.anchorMax = Vector2.one;
            placeholderRect.offsetMin = Vector2.zero;
            placeholderRect.offsetMax = Vector2.zero;
            
            // Input text
            var inputText = CreateText(textArea.transform, "Text", "", 16f, 
                Theme.Current.textPrimary, TextAlignmentOptions.Left);
            var inputTextRect = inputText.GetComponent<RectTransform>();
            inputTextRect.anchorMin = Vector2.zero;
            inputTextRect.anchorMax = Vector2.one;
            inputTextRect.offsetMin = Vector2.zero;
            inputTextRect.offsetMax = Vector2.zero;
            
            var inputField = inputObj.GetComponent<TMP_InputField>();
            inputField.textViewport = textAreaRect;
            inputField.textComponent = inputText;
            inputField.placeholder = placeholderText;
            inputField.contentType = contentType;
            
            return inputField;
        }
        
        #endregion
        
        #region Specialized Elements
        
        /// <summary>
        /// Create a progress bar (for XP, loading, etc.)
        /// </summary>
        public static (GameObject container, Image fill) CreateProgressBar(Transform parent, string name,
            float width = 200f, float height = 20f, Color? fillColor = null)
        {
            var container = CreatePanel(parent, name, Theme.Current.cardPanelColor);
            var containerRect = container.GetComponent<RectTransform>();
            containerRect.sizeDelta = new Vector2(width, height);
            
            var fill = CreatePanel(container.transform, "Fill", fillColor ?? Theme.Current.primaryColor);
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0.5f, 1f); // 50% filled by default
            fillRect.offsetMin = new Vector2(2, 2);
            fillRect.offsetMax = new Vector2(-2, -2);
            
            return (container, fill.GetComponent<Image>());
        }
        
        /// <summary>
        /// Create an avatar placeholder (circle)
        /// </summary>
        public static Image CreateAvatar(Transform parent, string name, float size = 60f, Color? color = null)
        {
            var avatar = CreatePanel(parent, name, color ?? Theme.Current.buttonSecondary);
            var rect = avatar.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(size, size);
            
            // Note: For circular avatars, assign a circular sprite here
            // Or use a mask component with a circular mask sprite
            
            return avatar.GetComponent<Image>();
        }
        
        /// <summary>
        /// Create a divider line
        /// </summary>
        public static GameObject CreateDivider(Transform parent, string name, bool horizontal = true,
            float length = 200f, float thickness = 2f)
        {
            var divider = CreatePanel(parent, name, Theme.Current.textMuted * 0.5f);
            var rect = divider.GetComponent<RectTransform>();
            
            if (horizontal)
                rect.sizeDelta = new Vector2(length, thickness);
            else
                rect.sizeDelta = new Vector2(thickness, length);
            
            return divider;
        }
        
        #endregion
        
        #region Layout Helpers
        
        /// <summary>
        /// Set RectTransform to fill parent
        /// </summary>
        public static void FillParent(RectTransform rect, float padding = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(padding, padding);
            rect.offsetMax = new Vector2(-padding, -padding);
        }
        
        /// <summary>
        /// Set RectTransform anchors and position
        /// </summary>
        public static void SetAnchors(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, 
            Vector2 pivot, Vector2 position, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
        
        /// <summary>
        /// Center an element
        /// </summary>
        public static void Center(RectTransform rect, Vector2 size)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
        }
        
        #endregion
    }
}


