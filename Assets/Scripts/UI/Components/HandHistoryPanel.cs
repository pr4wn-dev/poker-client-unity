using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PokerClient.Networking;
using System.Collections.Generic;

namespace PokerClient.UI.Components
{
    /// <summary>
    /// Panel that displays hand history during and after gameplay.
    /// Shows actions, pot changes, and final results.
    /// </summary>
    public class HandHistoryPanel : MonoBehaviour
    {
        private RectTransform _rect;
        private GameObject _content;
        private Transform _entriesContainer;
        private ScrollRect _scrollRect;
        private List<GameObject> _entries = new List<GameObject>();
        
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _handNumberText;
        private Button _toggleButton;
        private Button _clearButton;
        
        private bool _isExpanded = false;
        private int _currentHandNumber = 0;
        private List<HandHistoryEntry> _history = new List<HandHistoryEntry>();
        
        public static HandHistoryPanel Create(Transform parent)
        {
            var go = new GameObject("HandHistoryPanel");
            go.transform.SetParent(parent, false);
            var panel = go.AddComponent<HandHistoryPanel>();
            panel.Initialize();
            return panel;
        }
        
        private void Initialize()
        {
            var theme = Theme.Current;
            
            _rect = gameObject.AddComponent<RectTransform>();
            _rect.anchorMin = new Vector2(1, 0);
            _rect.anchorMax = new Vector2(1, 0.5f);
            _rect.pivot = new Vector2(1, 0);
            _rect.anchoredPosition = new Vector2(-10, 10);
            _rect.sizeDelta = new Vector2(300, 40);
            
            var bg = gameObject.AddComponent<Image>();
            bg.color = theme.cardPanelColor;
            
            BuildHeader();
            BuildContent();
            
            // Start collapsed
            Collapse();
        }
        
        private void BuildHeader()
        {
            var theme = Theme.Current;
            
            var header = UIFactory.CreatePanel(transform, "Header", Color.clear);
            var headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = Vector2.one;
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.sizeDelta = new Vector2(0, 40);
            headerRect.anchoredPosition = Vector2.zero;
            
            // Title
            _titleText = UIFactory.CreateText(header.transform, "Title", "Hand History", 16f, theme.textPrimary);
            var titleRect = _titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.05f, 0);
            titleRect.anchorMax = new Vector2(0.5f, 1);
            titleRect.sizeDelta = Vector2.zero;
            _titleText.fontStyle = FontStyles.Bold;
            
            // Hand number
            _handNumberText = UIFactory.CreateText(header.transform, "HandNum", "#1", 14f, theme.accentColor);
            var numRect = _handNumberText.GetComponent<RectTransform>();
            numRect.anchorMin = new Vector2(0.5f, 0);
            numRect.anchorMax = new Vector2(0.7f, 1);
            numRect.sizeDelta = Vector2.zero;
            _handNumberText.alignment = TextAlignmentOptions.Center;
            
            // Toggle button
            _toggleButton = UIFactory.CreateButton(header.transform, "Toggle", "▼", OnToggleClick).GetComponent<Button>();
            var toggleRect = _toggleButton.GetComponent<RectTransform>();
            toggleRect.anchorMin = new Vector2(0.75f, 0.15f);
            toggleRect.anchorMax = new Vector2(0.95f, 0.85f);
            toggleRect.sizeDelta = Vector2.zero;
            _toggleButton.GetComponent<Image>().color = theme.primaryColor;
        }
        
        private void BuildContent()
        {
            var theme = Theme.Current;
            
            _content = UIFactory.CreatePanel(transform, "Content", Color.clear);
            var contentRect = _content.GetComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.offsetMin = new Vector2(5, 5);
            contentRect.offsetMax = new Vector2(-5, -40);
            
            // Scroll view
            var scrollView = new GameObject("ScrollView");
            scrollView.transform.SetParent(_content.transform, false);
            var scrollViewRect = scrollView.AddComponent<RectTransform>();
            scrollViewRect.anchorMin = Vector2.zero;
            scrollViewRect.anchorMax = Vector2.one;
            scrollViewRect.sizeDelta = Vector2.zero;
            
            _scrollRect = scrollView.AddComponent<ScrollRect>();
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            
            // Viewport
            var viewport = UIFactory.CreatePanel(scrollView.transform, "Viewport", Color.clear);
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            _scrollRect.viewport = viewportRect;
            
            // Entries container
            var entries = UIFactory.CreatePanel(viewport.transform, "Entries", Color.clear);
            _entriesContainer = entries.transform;
            var entriesRect = entries.GetComponent<RectTransform>();
            entriesRect.anchorMin = new Vector2(0, 1);
            entriesRect.anchorMax = new Vector2(1, 1);
            entriesRect.pivot = new Vector2(0.5f, 1);
            entriesRect.sizeDelta = new Vector2(0, 0);
            
            var vlg = entries.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 3;
            vlg.padding = new RectOffset(5, 5, 5, 5);
            vlg.childControlHeight = false;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            
            var csf = entries.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            _scrollRect.content = entriesRect;
            
            // Clear button at bottom
            _clearButton = UIFactory.CreateButton(_content.transform, "Clear", "Clear", OnClearClick).GetComponent<Button>();
            var clearRect = _clearButton.GetComponent<RectTransform>();
            clearRect.anchorMin = new Vector2(0.7f, 0);
            clearRect.anchorMax = new Vector2(0.98f, 0.1f);
            clearRect.sizeDelta = Vector2.zero;
            _clearButton.GetComponent<Image>().color = theme.dangerColor;
        }
        
        #region Public Methods
        
        public void StartNewHand(int handNumber)
        {
            _currentHandNumber = handNumber;
            _handNumberText.text = $"#{handNumber}";
            
            // Add separator
            if (_history.Count > 0)
            {
                AddEntry(new HandHistoryEntry
                {
                    type = EntryType.Separator,
                    message = $"--- Hand #{handNumber} ---"
                });
            }
            
            AddEntry(new HandHistoryEntry
            {
                type = EntryType.Phase,
                message = $"Hand #{handNumber} started"
            });
        }
        
        public void AddPhase(string phase)
        {
            AddEntry(new HandHistoryEntry
            {
                type = EntryType.Phase,
                message = $"*** {phase.ToUpper()} ***"
            });
        }
        
        public void AddAction(string playerName, string action, int? amount = null)
        {
            string message = amount.HasValue && amount.Value > 0
                ? $"{playerName} {action}s {amount.Value}"
                : $"{playerName} {action}s";
            
            AddEntry(new HandHistoryEntry
            {
                type = GetActionType(action),
                message = message,
                playerName = playerName,
                action = action,
                amount = amount
            });
        }
        
        public void AddCommunityCards(List<Card> cards)
        {
            string cardsStr = string.Join(" ", cards.ConvertAll(c => c?.ToString() ?? "?"));
            AddEntry(new HandHistoryEntry
            {
                type = EntryType.Cards,
                message = $"Board: {cardsStr}"
            });
        }
        
        public void AddPotUpdate(int potAmount)
        {
            AddEntry(new HandHistoryEntry
            {
                type = EntryType.Pot,
                message = $"Pot: {potAmount:N0}"
            });
        }
        
        public void AddShowdown(string playerName, string handName)
        {
            AddEntry(new HandHistoryEntry
            {
                type = EntryType.Showdown,
                message = $"{playerName} shows {handName}"
            });
        }
        
        public void AddWinner(string playerName, int amount, string handName = null)
        {
            string message = handName != null
                ? $"{playerName} wins {amount:N0} with {handName}"
                : $"{playerName} wins {amount:N0}";
            
            AddEntry(new HandHistoryEntry
            {
                type = EntryType.Winner,
                message = message,
                playerName = playerName,
                amount = amount
            });
        }
        
        public void Expand()
        {
            _isExpanded = true;
            _rect.sizeDelta = new Vector2(300, 300);
            _content.SetActive(true);
            _toggleButton.GetComponentInChildren<TextMeshProUGUI>().text = "▲";
        }
        
        public void Collapse()
        {
            _isExpanded = false;
            _rect.sizeDelta = new Vector2(300, 40);
            _content.SetActive(false);
            _toggleButton.GetComponentInChildren<TextMeshProUGUI>().text = "▼";
        }
        
        public void Clear()
        {
            _history.Clear();
            foreach (var entry in _entries)
            {
                Destroy(entry);
            }
            _entries.Clear();
        }
        
        #endregion
        
        #region Private Methods
        
        private void AddEntry(HandHistoryEntry entry)
        {
            _history.Add(entry);
            CreateEntryUI(entry);
            
            // Auto-scroll to bottom
            Canvas.ForceUpdateCanvases();
            _scrollRect.verticalNormalizedPosition = 0;
        }
        
        private void CreateEntryUI(HandHistoryEntry entry)
        {
            var theme = Theme.Current;
            
            var entryObj = UIFactory.CreatePanel(_entriesContainer, $"Entry_{_entries.Count}", Color.clear);
            entryObj.GetOrAddComponent<LayoutElement>().preferredHeight = 22;
            _entries.Add(entryObj);
            
            var text = UIFactory.CreateText(entryObj.transform, "Text", entry.message, 13f, GetEntryColor(entry.type));
            var textRect = text.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            
            if (entry.type == EntryType.Phase || entry.type == EntryType.Separator)
            {
                text.fontStyle = FontStyles.Bold;
                text.alignment = TextAlignmentOptions.Center;
            }
        }
        
        private Color GetEntryColor(EntryType type)
        {
            var theme = Theme.Current;
            return type switch
            {
                EntryType.Action => theme.textPrimary,
                EntryType.Fold => theme.dangerColor,
                EntryType.Bet => theme.accentColor,
                EntryType.Check => theme.successColor,
                EntryType.Call => theme.primaryColor,
                EntryType.AllIn => new Color(1f, 0.3f, 0.6f),
                EntryType.Phase => theme.textSecondary,
                EntryType.Cards => theme.accentColor,
                EntryType.Pot => theme.accentColor,
                EntryType.Showdown => theme.textPrimary,
                EntryType.Winner => theme.successColor,
                EntryType.Separator => theme.textSecondary,
                _ => theme.textPrimary
            };
        }
        
        private EntryType GetActionType(string action)
        {
            return action?.ToLower() switch
            {
                "fold" => EntryType.Fold,
                "check" => EntryType.Check,
                "call" => EntryType.Call,
                "bet" or "raise" => EntryType.Bet,
                "allin" or "all-in" => EntryType.AllIn,
                _ => EntryType.Action
            };
        }
        
        private void OnToggleClick()
        {
            if (_isExpanded)
                Collapse();
            else
                Expand();
        }
        
        private void OnClearClick()
        {
            Clear();
        }
        
        #endregion
    }
    
    public enum EntryType
    {
        Action,
        Fold,
        Check,
        Call,
        Bet,
        AllIn,
        Phase,
        Cards,
        Pot,
        Showdown,
        Winner,
        Separator
    }
    
    public class HandHistoryEntry
    {
        public EntryType type;
        public string message;
        public string playerName;
        public string action;
        public int? amount;
        public long timestamp;
        
        public HandHistoryEntry()
        {
            timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }
}

