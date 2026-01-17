using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PokerClient.Networking;
using System.Collections.Generic;

namespace PokerClient.UI.Components
{
    /// <summary>
    /// Chat panel for table communication.
    /// Shows messages and allows sending new ones.
    /// </summary>
    public class ChatPanel : MonoBehaviour
    {
        private RectTransform _rect;
        private GameService _gameService;
        
        private ScrollRect _scrollRect;
        private Transform _messageContainer;
        private TMP_InputField _inputField;
        private Button _sendButton;
        private Button _toggleButton;
        private TextMeshProUGUI _toggleText;
        
        private List<GameObject> _messages = new List<GameObject>();
        private const int MAX_MESSAGES = 50;
        
        private bool _isExpanded = true;
        private float _expandedHeight = 250f;
        private float _collapsedHeight = 40f;
        
        public static ChatPanel Create(Transform parent)
        {
            var go = new GameObject("ChatPanel");
            go.transform.SetParent(parent, false);
            var panel = go.AddComponent<ChatPanel>();
            panel.Initialize();
            return panel;
        }
        
        private void Initialize()
        {
            var theme = Theme.Current;
            
            _rect = gameObject.AddComponent<RectTransform>();
            _rect.anchorMin = new Vector2(0, 0);
            _rect.anchorMax = new Vector2(0, 0);
            _rect.pivot = new Vector2(0, 0);
            _rect.anchoredPosition = new Vector2(10, 10);
            _rect.sizeDelta = new Vector2(350, _expandedHeight);
            
            var bg = gameObject.AddComponent<Image>();
            bg.color = new Color(theme.cardPanelColor.r, theme.cardPanelColor.g, theme.cardPanelColor.b, 0.9f);
            
            BuildHeader();
            BuildMessageArea();
            BuildInputArea();
            
            // Subscribe to chat events
            _gameService = GameService.Instance;
            if (_gameService != null)
            {
                _gameService.OnChatMessageReceived += HandleChatMessage;
            }
        }
        
        private void OnDestroy()
        {
            if (_gameService != null)
            {
                _gameService.OnChatMessageReceived -= HandleChatMessage;
            }
        }
        
        private void HandleChatMessage(string playerId, string username, string message)
        {
            AddMessage(username ?? playerId, message);
        }
        
        private void BuildHeader()
        {
            var theme = Theme.Current;
            
            var header = UIFactory.CreatePanel(transform, "Header", Color.clear);
            var headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = Vector2.one;
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.sizeDelta = new Vector2(0, 35);
            headerRect.anchoredPosition = Vector2.zero;
            
            var title = UIFactory.CreateText(header.transform, "Title", "Chat", 16f, theme.textPrimary);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.05f, 0);
            titleRect.anchorMax = new Vector2(0.7f, 1);
            titleRect.sizeDelta = Vector2.zero;
            title.fontStyle = FontStyles.Bold;
            
            _toggleButton = UIFactory.CreateButton(header.transform, "Toggle", "−", OnToggleClick).GetComponent<Button>();
            var toggleRect = _toggleButton.GetComponent<RectTransform>();
            toggleRect.anchorMin = new Vector2(0.85f, 0.1f);
            toggleRect.anchorMax = new Vector2(0.98f, 0.9f);
            toggleRect.sizeDelta = Vector2.zero;
            _toggleButton.GetComponent<Image>().color = theme.cardPanelColor;
            _toggleText = _toggleButton.GetComponentInChildren<TextMeshProUGUI>();
        }
        
        private void BuildMessageArea()
        {
            var theme = Theme.Current;
            
            var messageArea = UIFactory.CreatePanel(transform, "MessageArea", Color.clear);
            var areaRect = messageArea.GetComponent<RectTransform>();
            areaRect.anchorMin = Vector2.zero;
            areaRect.anchorMax = Vector2.one;
            areaRect.offsetMin = new Vector2(5, 45);
            areaRect.offsetMax = new Vector2(-5, -35);
            
            // Scroll view
            var scrollView = new GameObject("ScrollView");
            scrollView.transform.SetParent(messageArea.transform, false);
            var scrollViewRect = scrollView.AddComponent<RectTransform>();
            scrollViewRect.anchorMin = Vector2.zero;
            scrollViewRect.anchorMax = Vector2.one;
            scrollViewRect.sizeDelta = Vector2.zero;
            
            _scrollRect = scrollView.AddComponent<ScrollRect>();
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.scrollSensitivity = 20;
            
            // Viewport
            var viewport = UIFactory.CreatePanel(scrollView.transform, "Viewport", Color.clear);
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            _scrollRect.viewport = viewportRect;
            
            // Content
            var content = UIFactory.CreatePanel(viewport.transform, "Content", Color.clear);
            _messageContainer = content.transform;
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            contentRect.sizeDelta = new Vector2(0, 0);
            
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 3;
            vlg.padding = new RectOffset(5, 5, 5, 5);
            vlg.childControlHeight = false;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            
            var csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            _scrollRect.content = contentRect;
        }
        
        private void BuildInputArea()
        {
            var theme = Theme.Current;
            
            var inputArea = UIFactory.CreatePanel(transform, "InputArea", Color.clear);
            var areaRect = inputArea.GetComponent<RectTransform>();
            areaRect.anchorMin = Vector2.zero;
            areaRect.anchorMax = new Vector2(1, 0);
            areaRect.pivot = new Vector2(0.5f, 0);
            areaRect.sizeDelta = new Vector2(0, 40);
            areaRect.anchoredPosition = Vector2.zero;
            
            // Input field
            var inputObj = new GameObject("Input");
            inputObj.transform.SetParent(inputArea.transform, false);
            var inputRect = inputObj.AddComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0, 0.1f);
            inputRect.anchorMax = new Vector2(0.75f, 0.9f);
            inputRect.sizeDelta = Vector2.zero;
            inputRect.offsetMin = new Vector2(5, 0);
            inputRect.offsetMax = new Vector2(0, 0);
            
            var inputBg = inputObj.AddComponent<Image>();
            inputBg.color = theme.backgroundColor;
            
            _inputField = inputObj.AddComponent<TMP_InputField>();
            _inputField.textViewport = inputRect;
            _inputField.placeholder = CreatePlaceholder(inputObj.transform);
            _inputField.onSubmit.AddListener(OnSubmit);
            
            var inputText = UIFactory.CreateText(inputObj.transform, "Text", "", 14f, theme.textPrimary);
            inputText.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            inputText.GetComponent<RectTransform>().anchorMax = Vector2.one;
            inputText.GetComponent<RectTransform>().offsetMin = new Vector2(5, 0);
            inputText.GetComponent<RectTransform>().offsetMax = new Vector2(-5, 0);
            _inputField.textComponent = inputText;
            
            // Send button
            _sendButton = UIFactory.CreateButton(inputArea.transform, "Send", "→", OnSendClick).GetComponent<Button>();
            var sendRect = _sendButton.GetComponent<RectTransform>();
            sendRect.anchorMin = new Vector2(0.77f, 0.1f);
            sendRect.anchorMax = new Vector2(0.98f, 0.9f);
            sendRect.sizeDelta = Vector2.zero;
            _sendButton.GetComponent<Image>().color = theme.primaryColor;
        }
        
        private Graphic CreatePlaceholder(Transform parent)
        {
            var placeholder = UIFactory.CreateText(parent, "Placeholder", "Type message...", 14f, Theme.Current.textSecondary);
            var rect = placeholder.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(5, 0);
            rect.offsetMax = new Vector2(-5, 0);
            return placeholder;  // Return the TextMeshProUGUI (which is a Graphic)
        }
        
        public void AddMessage(ChatMessage message)
        {
            AddMessage(message.name ?? message.playerId, message.message);
        }
        
        public void AddMessage(string sender, string text)
        {
            var theme = Theme.Current;
            
            var msgObj = UIFactory.CreatePanel(_messageContainer, $"Msg_{_messages.Count}", Color.clear);
            msgObj.GetOrAddComponent<LayoutElement>().preferredHeight = 22;
            _messages.Add(msgObj);
            
            bool isSystem = string.IsNullOrEmpty(sender);
            bool isMe = _gameService?.CurrentUser?.username == sender;
            
            string displayText = isSystem 
                ? $"<color=#888888><i>{text}</i></color>"
                : $"<color=#{(isMe ? "88CCFF" : "FFCC88")}>{sender}:</color> {text}";
            
            var msgText = UIFactory.CreateText(msgObj.transform, "Text", displayText, 13f, theme.textPrimary);
            msgText.richText = true;
            msgText.enableWordWrapping = true;
            var textRect = msgText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            
            // Remove old messages if too many
            while (_messages.Count > MAX_MESSAGES)
            {
                Destroy(_messages[0]);
                _messages.RemoveAt(0);
            }
            
            // Scroll to bottom
            Canvas.ForceUpdateCanvases();
            _scrollRect.verticalNormalizedPosition = 0;
        }
        
        public void AddSystemMessage(string text)
        {
            AddMessage(null, text);
        }
        
        private void OnSendClick()
        {
            SendMessage();
        }
        
        private void OnSubmit(string text)
        {
            SendMessage();
        }
        
        private void SendMessage()
        {
            string text = _inputField.text?.Trim();
            if (string.IsNullOrEmpty(text)) return;
            
            _gameService?.SendChat(text);
            _inputField.text = "";
            _inputField.ActivateInputField();
        }
        
        private void OnToggleClick()
        {
            if (_isExpanded)
                Collapse();
            else
                Expand();
        }
        
        public void Expand()
        {
            _isExpanded = true;
            _rect.sizeDelta = new Vector2(_rect.sizeDelta.x, _expandedHeight);
            _toggleText.text = "−";
        }
        
        public void Collapse()
        {
            _isExpanded = false;
            _rect.sizeDelta = new Vector2(_rect.sizeDelta.x, _collapsedHeight);
            _toggleText.text = "+";
        }
        
        public void Clear()
        {
            foreach (var msg in _messages)
            {
                Destroy(msg);
            }
            _messages.Clear();
        }
    }
}

