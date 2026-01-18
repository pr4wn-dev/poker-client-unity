using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PokerClient.Networking;
using System.Collections.Generic;

namespace PokerClient.UI.Components
{
    /// <summary>
    /// Friends list panel - view friends, send requests, invite to tables.
    /// </summary>
    public class FriendsPanel : MonoBehaviour
    {
        private RectTransform _rect;
        private CanvasGroup _canvasGroup;
        private GameService _gameService;
        
        private Transform _friendsListContainer;
        private Transform _requestsContainer;
        private TMP_InputField _searchInput;
        private Transform _searchResultsContainer;
        private TextMeshProUGUI _statusText;
        
        private List<GameObject> _friendItems = new List<GameObject>();
        private List<GameObject> _requestItems = new List<GameObject>();
        private List<GameObject> _searchItems = new List<GameObject>();
        
        private Button _closeButton;
        
        public System.Action OnClose;
        
        public static FriendsPanel Create(Transform parent)
        {
            var go = new GameObject("FriendsPanel");
            go.transform.SetParent(parent, false);
            var panel = go.AddComponent<FriendsPanel>();
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
            contentRect.anchorMin = new Vector2(0.2f, 0.1f);
            contentRect.anchorMax = new Vector2(0.8f, 0.9f);
            contentRect.sizeDelta = Vector2.zero;
            
            BuildHeader(content.transform);
            BuildSearchSection(content.transform);
            BuildFriendsList(content.transform);
            BuildRequestsList(content.transform);
            
            gameObject.SetActive(false);
        }
        
        private void BuildHeader(Transform parent)
        {
            var theme = Theme.Current;
            
            var header = UIFactory.CreatePanel(parent, "Header", Color.clear);
            var headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 0.92f);
            headerRect.anchorMax = Vector2.one;
            headerRect.sizeDelta = Vector2.zero;
            
            var title = UIFactory.CreateTitle(header.transform, "Title", "FRIENDS", 32f);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.05f, 0);
            titleRect.anchorMax = new Vector2(0.7f, 1);
            titleRect.sizeDelta = Vector2.zero;
            title.color = theme.accentColor;
            
            _closeButton = UIFactory.CreateButton(header.transform, "Close", "✕", Hide).GetComponent<Button>();
            var closeRect = _closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.9f, 0.1f);
            closeRect.anchorMax = new Vector2(0.98f, 0.9f);
            closeRect.sizeDelta = Vector2.zero;
            _closeButton.GetComponent<Image>().color = theme.dangerColor;
        }
        
        private void BuildSearchSection(Transform parent)
        {
            var theme = Theme.Current;
            
            var section = UIFactory.CreatePanel(parent, "Search", Color.clear);
            var sectionRect = section.GetComponent<RectTransform>();
            sectionRect.anchorMin = new Vector2(0.02f, 0.78f);
            sectionRect.anchorMax = new Vector2(0.98f, 0.9f);
            sectionRect.sizeDelta = Vector2.zero;
            
            var label = UIFactory.CreateText(section.transform, "Label", "Add Friend:", 16f, theme.textPrimary);
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.5f);
            labelRect.anchorMax = new Vector2(0.2f, 1);
            labelRect.sizeDelta = Vector2.zero;
            
            // Search input
            var inputObj = new GameObject("SearchInput");
            inputObj.transform.SetParent(section.transform, false);
            var inputRect = inputObj.AddComponent<RectTransform>();
            inputRect.anchorMin = new Vector2(0.22f, 0.55f);
            inputRect.anchorMax = new Vector2(0.7f, 0.95f);
            inputRect.sizeDelta = Vector2.zero;
            
            inputObj.AddComponent<Image>().color = theme.backgroundColor;
            _searchInput = inputObj.AddComponent<TMP_InputField>();
            
            var inputText = UIFactory.CreateText(inputObj.transform, "Text", "", 16f, theme.textPrimary);
            var textRect = inputText.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.02f, 0);
            textRect.anchorMax = new Vector2(0.98f, 1);
            textRect.sizeDelta = Vector2.zero;
            _searchInput.textComponent = inputText;
            
            var searchBtn = UIFactory.CreateButton(section.transform, "Search", "Search", OnSearchClick);
            var btnRect = searchBtn.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0.72f, 0.55f);
            btnRect.anchorMax = new Vector2(0.98f, 0.95f);
            btnRect.sizeDelta = Vector2.zero;
            searchBtn.GetComponent<Image>().color = theme.primaryColor;
            
            // Search results
            _searchResultsContainer = UIFactory.CreatePanel(section.transform, "Results", Color.clear).transform;
            var resultsRect = _searchResultsContainer.GetComponent<RectTransform>();
            resultsRect.anchorMin = new Vector2(0, 0);
            resultsRect.anchorMax = new Vector2(1, 0.5f);
            resultsRect.sizeDelta = Vector2.zero;
        }
        
        private void BuildFriendsList(Transform parent)
        {
            var theme = Theme.Current;
            
            var section = UIFactory.CreatePanel(parent, "FriendsList", theme.backgroundColor);
            var sectionRect = section.GetComponent<RectTransform>();
            sectionRect.anchorMin = new Vector2(0.02f, 0.35f);
            sectionRect.anchorMax = new Vector2(0.98f, 0.76f);
            sectionRect.sizeDelta = Vector2.zero;
            
            var title = UIFactory.CreateText(section.transform, "Title", "Online Friends", 18f, theme.textPrimary);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.02f, 0.88f);
            titleRect.anchorMax = new Vector2(0.98f, 1);
            titleRect.sizeDelta = Vector2.zero;
            title.fontStyle = FontStyles.Bold;
            
            // Scroll container
            _friendsListContainer = UIFactory.CreatePanel(section.transform, "Container", Color.clear).transform;
            var containerRect = _friendsListContainer.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.01f, 0.01f);
            containerRect.anchorMax = new Vector2(0.99f, 0.86f);
            containerRect.sizeDelta = Vector2.zero;
            
            var vlg = _friendsListContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 5;
            vlg.padding = new RectOffset(5, 5, 5, 5);
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
        }
        
        private void BuildRequestsList(Transform parent)
        {
            var theme = Theme.Current;
            
            var section = UIFactory.CreatePanel(parent, "Requests", theme.backgroundColor);
            var sectionRect = section.GetComponent<RectTransform>();
            sectionRect.anchorMin = new Vector2(0.02f, 0.02f);
            sectionRect.anchorMax = new Vector2(0.98f, 0.33f);
            sectionRect.sizeDelta = Vector2.zero;
            
            var title = UIFactory.CreateText(section.transform, "Title", "Friend Requests", 18f, theme.textPrimary);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.02f, 0.85f);
            titleRect.anchorMax = new Vector2(0.98f, 1);
            titleRect.sizeDelta = Vector2.zero;
            title.fontStyle = FontStyles.Bold;
            
            _requestsContainer = UIFactory.CreatePanel(section.transform, "Container", Color.clear).transform;
            var containerRect = _requestsContainer.GetComponent<RectTransform>();
            containerRect.anchorMin = new Vector2(0.01f, 0.01f);
            containerRect.anchorMax = new Vector2(0.99f, 0.83f);
            containerRect.sizeDelta = Vector2.zero;
            
            var vlg = _requestsContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 5;
            vlg.padding = new RectOffset(5, 5, 5, 5);
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
        }
        
        public void Show()
        {
            _gameService = GameService.Instance;
            gameObject.SetActive(true);
            RefreshFriends();
        }
        
        public void Hide()
        {
            gameObject.SetActive(false);
            OnClose?.Invoke();
        }
        
        private void RefreshFriends()
        {
            // TODO: Call GameService.GetFriends()
            _statusText?.SetText("Loading...");
        }
        
        private void OnSearchClick()
        {
            string query = _searchInput.text?.Trim();
            if (string.IsNullOrEmpty(query)) return;
            
            // TODO: Call GameService.SearchUsers()
            Debug.Log($"Searching for: {query}");
        }
    }
}


