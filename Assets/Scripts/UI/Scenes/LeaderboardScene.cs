using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using PokerClient.Networking;
using System.Collections.Generic;

namespace PokerClient.UI.Scenes
{
    /// <summary>
    /// Leaderboard scene showing top players across different categories.
    /// </summary>
    public class LeaderboardScene : MonoBehaviour
    {
        private GameService _gameService;
        
        private Transform tabsContainer;
        private Transform entriesContainer;
        private TextMeshProUGUI categoryTitle;
        private ScrollRect scrollRect;
        
        private List<GameObject> _entries = new List<GameObject>();
        
        private enum LeaderboardCategory { Chips, Wins, Level, BiggestPot, WinStreak }
        private LeaderboardCategory _currentCategory = LeaderboardCategory.Chips;
        
        private void Start()
        {
            _gameService = GameService.Instance;
            BuildScene();
            LoadLeaderboard(LeaderboardCategory.Chips);
        }
        
        private void BuildScene()
        {
            var theme = Theme.Current;
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            gameObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            gameObject.AddComponent<GraphicRaycaster>();
            
            // Background
            var bg = UIFactory.CreatePanel(transform, "Background", theme.backgroundColor);
            bg.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            bg.GetComponent<RectTransform>().anchorMax = Vector2.one;
            bg.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
            
            BuildHeader();
            BuildTabs();
            BuildLeaderboardList();
        }
        
        private void BuildHeader()
        {
            var theme = Theme.Current;
            
            var header = UIFactory.CreatePanel(transform, "Header", theme.cardPanelColor);
            var headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 0.9f);
            headerRect.anchorMax = Vector2.one;
            headerRect.sizeDelta = Vector2.zero;
            
            // Title
            var title = UIFactory.CreateTitle(header.transform, "Title", "LEADERBOARD", 42f);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.03f, 0);
            titleRect.anchorMax = new Vector2(0.5f, 1);
            titleRect.sizeDelta = Vector2.zero;
            title.color = theme.accentColor;
            
            // Category subtitle
            categoryTitle = UIFactory.CreateText(header.transform, "Category", "TOP CHIP LEADERS", 20f, theme.textSecondary);
            var catRect = categoryTitle.GetComponent<RectTransform>();
            catRect.anchorMin = new Vector2(0.5f, 0.1f);
            catRect.anchorMax = new Vector2(0.85f, 0.9f);
            catRect.sizeDelta = Vector2.zero;
            categoryTitle.alignment = TextAlignmentOptions.Center;
            
            // Back button
            var backBtn = UIFactory.CreateButton(header.transform, "Back", "← BACK", () => SceneManager.LoadScene("MainMenuScene"));
            var backRect = backBtn.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.88f, 0.2f);
            backRect.anchorMax = new Vector2(0.98f, 0.8f);
            backRect.sizeDelta = Vector2.zero;
        }
        
        private void BuildTabs()
        {
            var theme = Theme.Current;
            
            var tabs = UIFactory.CreatePanel(transform, "Tabs", Color.clear);
            tabsContainer = tabs.transform;
            var tabsRect = tabs.GetComponent<RectTransform>();
            tabsRect.anchorMin = new Vector2(0.02f, 0.82f);
            tabsRect.anchorMax = new Vector2(0.98f, 0.89f);
            tabsRect.sizeDelta = Vector2.zero;
            
            var hlg = tabs.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.childControlWidth = true;
            hlg.childForceExpandWidth = true;
            hlg.padding = new RectOffset(0, 0, 5, 5);
            
            CreateTabButton("CHIPS", LeaderboardCategory.Chips);
            CreateTabButton("🏅 WINS", LeaderboardCategory.Wins);
            CreateTabButton("LEVEL", LeaderboardCategory.Level);
            CreateTabButton("BIGGEST POT", LeaderboardCategory.BiggestPot);
            CreateTabButton("WIN STREAK", LeaderboardCategory.WinStreak);
        }
        
        private void CreateTabButton(string label, LeaderboardCategory category)
        {
            var theme = Theme.Current;
            var btn = UIFactory.CreateButton(tabsContainer, $"Tab_{category}", label, () => LoadLeaderboard(category));
            btn.GetComponent<Image>().color = theme.cardPanelColor;
        }
        
        private void BuildLeaderboardList()
        {
            var theme = Theme.Current;
            
            var listPanel = UIFactory.CreatePanel(transform, "List", theme.cardPanelColor);
            var listRect = listPanel.GetComponent<RectTransform>();
            listRect.anchorMin = new Vector2(0.1f, 0.02f);
            listRect.anchorMax = new Vector2(0.9f, 0.8f);
            listRect.sizeDelta = Vector2.zero;
            
            // Scroll view
            var scrollView = new GameObject("ScrollView");
            scrollView.transform.SetParent(listPanel.transform, false);
            var scrollViewRect = scrollView.AddComponent<RectTransform>();
            scrollViewRect.anchorMin = new Vector2(0.01f, 0.01f);
            scrollViewRect.anchorMax = new Vector2(0.99f, 0.99f);
            scrollViewRect.sizeDelta = Vector2.zero;
            
            scrollRect = scrollView.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            
            var viewport = UIFactory.CreatePanel(scrollView.transform, "Viewport", Color.clear);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            scrollRect.viewport = viewportRect;
            
            var content = UIFactory.CreatePanel(viewport.transform, "Content", Color.clear);
            entriesContainer = content.transform;
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 5;
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            
            var csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            scrollRect.content = contentRect;
            
            // Header row
            CreateHeaderRow();
        }
        
        private void CreateHeaderRow()
        {
            var theme = Theme.Current;
            
            var headerRow = UIFactory.CreatePanel(entriesContainer, "Header", Color.clear);
            headerRow.GetOrAddComponent<LayoutElement>().preferredHeight = 40;
            
            var hlg = headerRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.childControlWidth = true;
            hlg.childForceExpandWidth = true;
            
            CreateColumnHeader(headerRow.transform, "Rank", 0.1f);
            CreateColumnHeader(headerRow.transform, "Player", 0.4f);
            CreateColumnHeader(headerRow.transform, "Level", 0.15f);
            CreateColumnHeader(headerRow.transform, "Value", 0.25f);
        }
        
        private void CreateColumnHeader(Transform parent, string label, float flex)
        {
            var theme = Theme.Current;
            var text = UIFactory.CreateText(parent, $"Col_{label}", label, 16f, theme.textSecondary);
            text.fontStyle = FontStyles.Bold;
            text.GetOrAddComponent<LayoutElement>().flexibleWidth = flex;
        }
        
        private void LoadLeaderboard(LeaderboardCategory category)
        {
            _currentCategory = category;
            
            // Update tabs
            foreach (Transform tab in tabsContainer)
            {
                var img = tab.GetComponent<Image>();
                var isActive = tab.name.Contains(category.ToString());
                img.color = isActive ? Theme.Current.primaryColor : Theme.Current.cardPanelColor;
            }
            
            // Update title
            categoryTitle.text = category switch
            {
                LeaderboardCategory.Chips => "TOP CHIP LEADERS",
                LeaderboardCategory.Wins => "MOST HANDS WON",
                LeaderboardCategory.Level => "HIGHEST LEVELS",
                LeaderboardCategory.BiggestPot => "BIGGEST POTS WON",
                LeaderboardCategory.WinStreak => "LONGEST WIN STREAKS",
                _ => "LEADERBOARD"
            };
            
            // Clear existing entries (except header)
            foreach (var entry in _entries)
            {
                Destroy(entry);
            }
            _entries.Clear();
            
            // Load from server
            string serverCategory = category switch
            {
                LeaderboardCategory.Chips => "chips",
                LeaderboardCategory.Wins => "wins",
                LeaderboardCategory.Level => "level",
                LeaderboardCategory.BiggestPot => "biggest_pot",
                LeaderboardCategory.WinStreak => "wins",  // Use wins for now
                _ => "chips"
            };
            
            _gameService?.GetLeaderboard(serverCategory, entries =>
            {
                if (entries != null && entries.Count > 0)
                {
                    foreach (var entry in entries)
                    {
                        CreateLeaderboardEntry(entry.rank, entry.username, entry.level, entry.value, category);
                    }
                }
                else
                {
                    // Show mock data as fallback
                    LoadMockData(category);
                }
            });
        }
        
        private void LoadMockData(LeaderboardCategory category)
        {
            var mockData = new List<(string name, int level, int value)>
            {
                ("PokerKing99", 45, GetMockValue(1, category)),
                ("AllInAlice", 42, GetMockValue(2, category)),
                ("BluffMaster", 38, GetMockValue(3, category)),
                ("CardShark77", 35, GetMockValue(4, category)),
                ("LuckyDraw", 33, GetMockValue(5, category)),
                ("HighRoller", 31, GetMockValue(6, category)),
                ("RiverRat", 29, GetMockValue(7, category)),
                ("FishHunter", 27, GetMockValue(8, category)),
                ("NitNinja", 25, GetMockValue(9, category)),
                ("AggroAce", 23, GetMockValue(10, category))
            };
            
            for (int i = 0; i < mockData.Count; i++)
            {
                CreateLeaderboardEntry(i + 1, mockData[i].name, mockData[i].level, mockData[i].value, category);
            }
        }
        
        private int GetMockValue(int rank, LeaderboardCategory category)
        {
            return category switch
            {
                LeaderboardCategory.Chips => (11 - rank) * 1000000,
                LeaderboardCategory.Wins => (11 - rank) * 500,
                LeaderboardCategory.Level => 50 - (rank * 3),
                LeaderboardCategory.BiggestPot => (11 - rank) * 250000,
                LeaderboardCategory.WinStreak => 25 - (rank * 2),
                _ => 0
            };
        }
        
        private void CreateLeaderboardEntry(int rank, string playerName, int level, int value, LeaderboardCategory category)
        {
            var theme = Theme.Current;
            
            Color bgColor = rank switch
            {
                1 => new Color(1f, 0.85f, 0f, 0.3f),     // Gold
                2 => new Color(0.75f, 0.75f, 0.8f, 0.3f), // Silver
                3 => new Color(0.8f, 0.5f, 0.2f, 0.3f),   // Bronze
                _ => theme.backgroundColor
            };
            
            var entry = UIFactory.CreatePanel(entriesContainer, $"Entry_{rank}", bgColor);
            entry.GetOrAddComponent<LayoutElement>().preferredHeight = 55;
            _entries.Add(entry);
            
            var hlg = entry.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.padding = new RectOffset(15, 15, 5, 5);
            hlg.childControlWidth = true;
            hlg.childForceExpandWidth = true;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            
            // Rank
            string rankIcon = rank switch
            {
                1 => "🥇",
                2 => "🥈",
                3 => "🥉",
                _ => $"#{rank}"
            };
            var rankText = UIFactory.CreateText(entry.transform, "Rank", rankIcon, 24f, theme.textPrimary);
            rankText.GetOrAddComponent<LayoutElement>().flexibleWidth = 0.1f;
            rankText.alignment = TextAlignmentOptions.Center;
            
            // Player name
            var nameText = UIFactory.CreateText(entry.transform, "Name", playerName, 20f, theme.textPrimary);
            nameText.fontStyle = FontStyles.Bold;
            nameText.GetOrAddComponent<LayoutElement>().flexibleWidth = 0.4f;
            
            // Level
            var levelText = UIFactory.CreateText(entry.transform, "Level", $"Lv.{level}", 18f, theme.textSecondary);
            levelText.GetOrAddComponent<LayoutElement>().flexibleWidth = 0.15f;
            levelText.alignment = TextAlignmentOptions.Center;
            
            // Value
            string valueStr = category == LeaderboardCategory.WinStreak 
                ? value.ToString() 
                : value.ToString("N0");
            Color valueColor = category switch
            {
                LeaderboardCategory.Chips => theme.successColor,
                LeaderboardCategory.BiggestPot => theme.warningColor,
                _ => theme.accentColor
            };
            var valueText = UIFactory.CreateText(entry.transform, "Value", valueStr, 22f, valueColor);
            valueText.fontStyle = FontStyles.Bold;
            valueText.GetOrAddComponent<LayoutElement>().flexibleWidth = 0.25f;
            valueText.alignment = TextAlignmentOptions.Right;
        }
    }
}

