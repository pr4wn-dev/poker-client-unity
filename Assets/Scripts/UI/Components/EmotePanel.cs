using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PokerClient.Networking;
using System.Collections.Generic;

namespace PokerClient.UI.Components
{
    /// <summary>
    /// Quick emote/chat panel for expressing emotions and phrases during play.
    /// </summary>
    public class EmotePanel : MonoBehaviour
    {
        private RectTransform _rect;
        private GameService _gameService;
        
        private Transform _emotesContainer;
        private Transform _phrasesContainer;
        private Button _toggleButton;
        private TextMeshProUGUI _toggleText;
        
        private bool _isExpanded = false;
        
        // Standard poker emotes/phrases
        private static readonly string[] EMOTES = new string[]
        {
            "😄", "😎", "🤔", "😢", "😡", "👍", "👎", "🔥", "💀", "🎲"
        };
        
        private static readonly string[] PHRASES = new string[]
        {
            "Nice hand!",
            "Good game",
            "Thank you",
            "Sorry",
            "Wow!",
            "Lucky...",
            "All in!",
            "Check",
            "Let's go!",
            "GG"
        };
        
        public System.Action<string> OnEmoteSent;
        
        public static EmotePanel Create(Transform parent)
        {
            var go = new GameObject("EmotePanel");
            go.transform.SetParent(parent, false);
            var panel = go.AddComponent<EmotePanel>();
            panel.Initialize();
            return panel;
        }
        
        private void Initialize()
        {
            var theme = Theme.Current;
            
            _rect = gameObject.AddComponent<RectTransform>();
            _rect.anchorMin = new Vector2(1, 0);
            _rect.anchorMax = new Vector2(1, 0);
            _rect.pivot = new Vector2(1, 0);
            _rect.anchoredPosition = new Vector2(-10, 10);
            _rect.sizeDelta = new Vector2(50, 50);
            
            // Toggle button (always visible)
            _toggleButton = CreateToggleButton();
            
            // Expanded panel (hidden by default)
            BuildExpandedPanel();
            
            _gameService = GameService.Instance;
        }
        
        private Button CreateToggleButton()
        {
            var theme = Theme.Current;
            
            var btnObj = new GameObject("ToggleBtn");
            btnObj.transform.SetParent(transform, false);
            var btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.anchorMin = Vector2.zero;
            btnRect.anchorMax = Vector2.one;
            btnRect.sizeDelta = Vector2.zero;
            
            var img = btnObj.AddComponent<Image>();
            img.color = theme.primaryColor;
            
            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(TogglePanel);
            
            _toggleText = UIFactory.CreateText(btnObj.transform, "Icon", "💬", 24f, Color.white);
            _toggleText.alignment = TextAlignmentOptions.Center;
            var textRect = _toggleText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            
            return btn;
        }
        
        private void BuildExpandedPanel()
        {
            var theme = Theme.Current;
            
            var expandedPanel = UIFactory.CreatePanel(transform, "Expanded", theme.cardPanelColor);
            var expandedRect = expandedPanel.GetComponent<RectTransform>();
            expandedRect.anchorMin = new Vector2(1, 0);
            expandedRect.anchorMax = new Vector2(1, 0);
            expandedRect.pivot = new Vector2(1, 0);
            expandedRect.anchoredPosition = new Vector2(0, 55);
            expandedRect.sizeDelta = new Vector2(300, 200);
            
            // Tab buttons
            var tabs = UIFactory.CreatePanel(expandedPanel.transform, "Tabs", Color.clear);
            var tabsRect = tabs.GetComponent<RectTransform>();
            tabsRect.anchorMin = new Vector2(0, 0.85f);
            tabsRect.anchorMax = Vector2.one;
            tabsRect.sizeDelta = Vector2.zero;
            
            var hlg = tabs.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 5;
            hlg.childControlWidth = true;
            hlg.childForceExpandWidth = true;
            hlg.padding = new RectOffset(5, 5, 5, 0);
            
            var emotesTabBtn = UIFactory.CreateButton(tabs.transform, "EmotesTab", "Emotes", () => ShowTab(true)).GetComponent<Button>();
            var phrasesTabBtn = UIFactory.CreateButton(tabs.transform, "PhrasesTab", "Phrases", () => ShowTab(false)).GetComponent<Button>();
            
            // Emotes container
            var emotesHolder = UIFactory.CreatePanel(expandedPanel.transform, "Emotes", Color.clear);
            _emotesContainer = emotesHolder.transform;
            var emotesRect = emotesHolder.GetComponent<RectTransform>();
            emotesRect.anchorMin = new Vector2(0.02f, 0.02f);
            emotesRect.anchorMax = new Vector2(0.98f, 0.83f);
            emotesRect.sizeDelta = Vector2.zero;
            
            var emotesGrid = emotesHolder.AddComponent<GridLayoutGroup>();
            emotesGrid.cellSize = new Vector2(50, 50);
            emotesGrid.spacing = new Vector2(5, 5);
            emotesGrid.childAlignment = TextAnchor.UpperCenter;
            emotesGrid.padding = new RectOffset(5, 5, 5, 5);
            
            foreach (var emote in EMOTES)
            {
                CreateEmoteButton(emote);
            }
            
            // Phrases container
            var phrasesHolder = UIFactory.CreatePanel(expandedPanel.transform, "Phrases", Color.clear);
            _phrasesContainer = phrasesHolder.transform;
            var phrasesRect = phrasesHolder.GetComponent<RectTransform>();
            phrasesRect.anchorMin = new Vector2(0.02f, 0.02f);
            phrasesRect.anchorMax = new Vector2(0.98f, 0.83f);
            phrasesRect.sizeDelta = Vector2.zero;
            
            var phrasesVlg = phrasesHolder.AddComponent<VerticalLayoutGroup>();
            phrasesVlg.spacing = 3;
            phrasesVlg.childControlHeight = false;
            phrasesVlg.childControlWidth = true;
            phrasesVlg.padding = new RectOffset(5, 5, 5, 5);
            
            foreach (var phrase in PHRASES)
            {
                CreatePhraseButton(phrase);
            }
            
            phrasesHolder.SetActive(false);
            expandedPanel.SetActive(false);
        }
        
        private void CreateEmoteButton(string emote)
        {
            var theme = Theme.Current;
            
            var btn = UIFactory.CreateButton(_emotesContainer, $"Emote_{emote}", emote, () => SendEmote(emote));
            btn.GetComponent<Image>().color = theme.backgroundColor;
            btn.GetComponentInChildren<TextMeshProUGUI>().fontSize = 28;
        }
        
        private void CreatePhraseButton(string phrase)
        {
            var theme = Theme.Current;
            
            var btn = UIFactory.CreateButton(_phrasesContainer, $"Phrase_{phrase}", phrase, () => SendPhrase(phrase));
            btn.GetOrAddComponent<LayoutElement>().preferredHeight = 32;
            btn.GetComponent<Image>().color = theme.backgroundColor;
            btn.GetComponentInChildren<TextMeshProUGUI>().fontSize = 14;
        }
        
        private void TogglePanel()
        {
            _isExpanded = !_isExpanded;
            
            var expanded = transform.Find("Expanded");
            if (expanded != null)
            {
                expanded.gameObject.SetActive(_isExpanded);
            }
            
            _toggleText.text = _isExpanded ? "✕" : "💬";
        }
        
        private void ShowTab(bool showEmotes)
        {
            _emotesContainer.gameObject.SetActive(showEmotes);
            _phrasesContainer.gameObject.SetActive(!showEmotes);
        }
        
        private void SendEmote(string emote)
        {
            _gameService?.SendChat($"[EMOTE] {emote}");
            OnEmoteSent?.Invoke(emote);
            TogglePanel();
        }
        
        private void SendPhrase(string phrase)
        {
            _gameService?.SendChat(phrase);
            OnEmoteSent?.Invoke(phrase);
            TogglePanel();
        }
        
        public void Collapse()
        {
            if (_isExpanded)
            {
                TogglePanel();
            }
        }
    }
}

