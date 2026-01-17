using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PokerClient.Networking;
using System.Collections.Generic;

namespace PokerClient.UI.Components
{
    /// <summary>
    /// Achievements/badges panel showing unlocked and locked achievements.
    /// </summary>
    public class AchievementsPanel : MonoBehaviour
    {
        private RectTransform _rect;
        private CanvasGroup _canvasGroup;
        private GameService _gameService;
        
        private Transform achievementsContainer;
        private TextMeshProUGUI progressText;
        private Button closeButton;
        
        private List<GameObject> _achievementCards = new List<GameObject>();
        
        // Achievement definitions
        private static readonly Achievement[] ACHIEVEMENTS = new Achievement[]
        {
            // Beginner achievements
            new Achievement { id = "first_win", name = "First Victory", description = "Win your first hand", icon = "🏆", category = "Beginner", xpReward = 100 },
            new Achievement { id = "play_10", name = "Getting Started", description = "Play 10 hands", icon = "🃏", category = "Beginner", xpReward = 50 },
            new Achievement { id = "join_table", name = "Table Talk", description = "Join your first table", icon = "🪑", category = "Beginner", xpReward = 25 },
            
            // Skill achievements
            new Achievement { id = "win_50", name = "Winning Streak", description = "Win 50 hands", icon = "🔥", category = "Skill", xpReward = 250 },
            new Achievement { id = "all_in_win", name = "All or Nothing", description = "Win with an all-in", icon = "💪", category = "Skill", xpReward = 150 },
            new Achievement { id = "royal_flush", name = "Royal Flush", description = "Get a royal flush", icon = "👑", category = "Skill", xpReward = 1000 },
            new Achievement { id = "bluff_master", name = "Bluff Master", description = "Win with high card 10 times", icon = "🎭", category = "Skill", xpReward = 300 },
            
            // Wealth achievements
            new Achievement { id = "chips_10k", name = "Stacking Up", description = "Accumulate 10,000 chips", icon = "💰", category = "Wealth", xpReward = 100 },
            new Achievement { id = "chips_100k", name = "High Roller", description = "Accumulate 100,000 chips", icon = "💎", category = "Wealth", xpReward = 500 },
            new Achievement { id = "chips_1m", name = "Millionaire", description = "Accumulate 1,000,000 chips", icon = "🏦", category = "Wealth", xpReward = 2000 },
            new Achievement { id = "big_pot", name = "Jackpot!", description = "Win a pot over 50,000", icon = "🎰", category = "Wealth", xpReward = 400 },
            
            // Social achievements
            new Achievement { id = "add_friend", name = "Social Butterfly", description = "Add your first friend", icon = "🤝", category = "Social", xpReward = 75 },
            new Achievement { id = "friends_10", name = "Popular Player", description = "Have 10 friends", icon = "👥", category = "Social", xpReward = 200 },
            new Achievement { id = "chat_100", name = "Chatterbox", description = "Send 100 chat messages", icon = "💬", category = "Social", xpReward = 150 },
            
            // Adventure achievements
            new Achievement { id = "first_boss", name = "Boss Slayer", description = "Defeat your first boss", icon = "👹", category = "Adventure", xpReward = 200 },
            new Achievement { id = "area_complete", name = "Area Champion", description = "Complete all bosses in an area", icon = "🏅", category = "Adventure", xpReward = 500 },
            new Achievement { id = "adventure_win_streak", name = "Unstoppable", description = "Defeat 5 bosses in a row", icon = "⚡", category = "Adventure", xpReward = 750 },
            
            // Tournament achievements
            new Achievement { id = "tournament_entry", name = "Tournament Player", description = "Enter your first tournament", icon = "🎟️", category = "Tournaments", xpReward = 100 },
            new Achievement { id = "tournament_win", name = "Tournament Champion", description = "Win a tournament", icon = "🏆", category = "Tournaments", xpReward = 1000 },
            new Achievement { id = "final_table", name = "Final Table", description = "Reach the final table", icon = "🎯", category = "Tournaments", xpReward = 400 },
        };
        
        public System.Action OnClose;
        
        public static AchievementsPanel Create(Transform parent)
        {
            var go = new GameObject("AchievementsPanel");
            go.transform.SetParent(parent, false);
            var panel = go.AddComponent<AchievementsPanel>();
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
            bg.color = new Color(0, 0, 0, 0.85f);
            
            // Main panel
            var panel = UIFactory.CreatePanel(transform, "Panel", theme.cardPanelColor);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.08f, 0.05f);
            panelRect.anchorMax = new Vector2(0.92f, 0.95f);
            panelRect.sizeDelta = Vector2.zero;
            
            BuildHeader(panel.transform);
            BuildAchievementsList(panel.transform);
            
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
            
            var title = UIFactory.CreateTitle(header.transform, "Title", "🏆 ACHIEVEMENTS", 36f);
            title.color = theme.accentColor;
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.02f, 0);
            titleRect.anchorMax = new Vector2(0.4f, 1);
            titleRect.sizeDelta = Vector2.zero;
            
            progressText = UIFactory.CreateText(header.transform, "Progress", "0 / 20 Unlocked", 20f, theme.textSecondary);
            var progRect = progressText.GetComponent<RectTransform>();
            progRect.anchorMin = new Vector2(0.5f, 0.2f);
            progRect.anchorMax = new Vector2(0.85f, 0.8f);
            progRect.sizeDelta = Vector2.zero;
            progressText.alignment = TextAlignmentOptions.Center;
            
            closeButton = UIFactory.CreateButton(header.transform, "Close", "✕", Hide).GetComponent<Button>();
            var closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.93f, 0.15f);
            closeRect.anchorMax = new Vector2(0.98f, 0.85f);
            closeRect.sizeDelta = Vector2.zero;
            closeButton.GetComponent<Image>().color = theme.dangerColor;
        }
        
        private void BuildAchievementsList(Transform parent)
        {
            var theme = Theme.Current;
            
            var scrollPanel = UIFactory.CreatePanel(parent, "ScrollPanel", Color.clear);
            var scrollRect = scrollPanel.GetComponent<RectTransform>();
            scrollRect.anchorMin = new Vector2(0.01f, 0.01f);
            scrollRect.anchorMax = new Vector2(0.99f, 0.9f);
            scrollRect.sizeDelta = Vector2.zero;
            
            // Scroll view
            var scrollView = new GameObject("ScrollView");
            scrollView.transform.SetParent(scrollPanel.transform, false);
            var scrollViewRect = scrollView.AddComponent<RectTransform>();
            scrollViewRect.anchorMin = Vector2.zero;
            scrollViewRect.anchorMax = Vector2.one;
            scrollViewRect.sizeDelta = Vector2.zero;
            
            var scroll = scrollView.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            
            var viewport = UIFactory.CreatePanel(scrollView.transform, "Viewport", Color.clear);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            scroll.viewport = viewportRect;
            
            var content = UIFactory.CreatePanel(viewport.transform, "Content", Color.clear);
            achievementsContainer = content.transform;
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            
            var glg = content.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(350, 90);
            glg.spacing = new Vector2(15, 15);
            glg.padding = new RectOffset(20, 20, 20, 20);
            glg.childAlignment = TextAnchor.UpperCenter;
            
            var csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            scroll.content = contentRect;
        }
        
        public void Show(List<string> unlockedIds = null)
        {
            _gameService = GameService.Instance;
            gameObject.SetActive(true);
            
            // Load from server
            _gameService?.GetAchievements(response =>
            {
                // Clear existing
                foreach (var card in _achievementCards)
                {
                    Destroy(card);
                }
                _achievementCards.Clear();
                
                List<string> ids = response?.success == true ? response.unlockedIds : (unlockedIds ?? new List<string>());
                
                int unlocked = 0;
                foreach (var achievement in ACHIEVEMENTS)
                {
                    bool isUnlocked = ids.Contains(achievement.id);
                    if (isUnlocked) unlocked++;
                    CreateAchievementCard(achievement, isUnlocked);
                }
                
                progressText.text = $"{unlocked} / {ACHIEVEMENTS.Length} Unlocked";
            });
        }
        
        public void Hide()
        {
            gameObject.SetActive(false);
            OnClose?.Invoke();
        }
        
        private void CreateAchievementCard(Achievement achievement, bool isUnlocked)
        {
            var theme = Theme.Current;
            
            Color bgColor = isUnlocked ? theme.successColor * 0.3f : theme.backgroundColor;
            
            var card = UIFactory.CreatePanel(achievementsContainer, $"Achievement_{achievement.id}", bgColor);
            _achievementCards.Add(card);
            
            if (!isUnlocked)
            {
                var cg = card.AddComponent<CanvasGroup>();
                cg.alpha = 0.6f;
            }
            
            // Icon
            var iconPanel = UIFactory.CreatePanel(card.transform, "Icon", isUnlocked ? theme.successColor : theme.cardPanelColor);
            var iconRect = iconPanel.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.02f, 0.1f);
            iconRect.anchorMax = new Vector2(0.18f, 0.9f);
            iconRect.sizeDelta = Vector2.zero;
            
            var iconText = UIFactory.CreateText(iconPanel.transform, "Emoji", achievement.icon, 32f, Color.white);
            iconText.alignment = TextAlignmentOptions.Center;
            var emojiRect = iconText.GetComponent<RectTransform>();
            emojiRect.anchorMin = Vector2.zero;
            emojiRect.anchorMax = Vector2.one;
            emojiRect.sizeDelta = Vector2.zero;
            
            // Name
            var nameText = UIFactory.CreateText(card.transform, "Name", achievement.name, 18f, theme.textPrimary);
            nameText.fontStyle = FontStyles.Bold;
            var nameRect = nameText.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.2f, 0.55f);
            nameRect.anchorMax = new Vector2(0.85f, 0.95f);
            nameRect.sizeDelta = Vector2.zero;
            
            // Description
            var descText = UIFactory.CreateText(card.transform, "Desc", achievement.description, 14f, theme.textSecondary);
            var descRect = descText.GetComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0.2f, 0.25f);
            descRect.anchorMax = new Vector2(0.85f, 0.55f);
            descRect.sizeDelta = Vector2.zero;
            
            // Category
            var catText = UIFactory.CreateText(card.transform, "Category", achievement.category, 11f, theme.textSecondary);
            var catRect = catText.GetComponent<RectTransform>();
            catRect.anchorMin = new Vector2(0.2f, 0.05f);
            catRect.anchorMax = new Vector2(0.5f, 0.25f);
            catRect.sizeDelta = Vector2.zero;
            
            // XP Reward
            string rewardText = isUnlocked ? "✓ UNLOCKED" : $"+{achievement.xpReward} XP";
            Color rewardColor = isUnlocked ? theme.successColor : theme.accentColor;
            var xpText = UIFactory.CreateText(card.transform, "XP", rewardText, 14f, rewardColor);
            xpText.fontStyle = FontStyles.Bold;
            var xpRect = xpText.GetComponent<RectTransform>();
            xpRect.anchorMin = new Vector2(0.7f, 0.05f);
            xpRect.anchorMax = new Vector2(0.98f, 0.35f);
            xpRect.sizeDelta = Vector2.zero;
            xpText.alignment = TextAlignmentOptions.Right;
        }
    }
    
    [System.Serializable]
    public class Achievement
    {
        public string id;
        public string name;
        public string description;
        public string icon;
        public string category;
        public int xpReward;
    }
}

