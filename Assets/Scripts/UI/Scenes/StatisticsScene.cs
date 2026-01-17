using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using PokerClient.Networking;

namespace PokerClient.UI.Scenes
{
    /// <summary>
    /// Player statistics scene showing detailed stats and graphs.
    /// </summary>
    public class StatisticsScene : MonoBehaviour
    {
        private GameService _gameService;
        
        private TextMeshProUGUI usernameText;
        private TextMeshProUGUI levelText;
        
        // Stat displays
        private TextMeshProUGUI handsPlayedText;
        private TextMeshProUGUI handsWonText;
        private TextMeshProUGUI winRateText;
        private TextMeshProUGUI totalWinningsText;
        private TextMeshProUGUI biggestPotText;
        private TextMeshProUGUI tournamentWinsText;
        private TextMeshProUGUI adventureProgressText;
        private TextMeshProUGUI playtimeText;
        
        private void Start()
        {
            _gameService = GameService.Instance;
            BuildScene();
            LoadStats();
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
            BuildProfileCard();
            BuildStatsGrid();
            BuildProgressBars();
        }
        
        private void BuildHeader()
        {
            var theme = Theme.Current;
            
            var header = UIFactory.CreatePanel(transform, "Header", theme.cardPanelColor);
            var headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 0.9f);
            headerRect.anchorMax = Vector2.one;
            headerRect.sizeDelta = Vector2.zero;
            
            var title = UIFactory.CreateTitle(header.transform, "Title", "📊 MY STATISTICS", 42f);
            title.color = theme.accentColor;
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.03f, 0);
            titleRect.anchorMax = new Vector2(0.5f, 1);
            titleRect.sizeDelta = Vector2.zero;
            
            var backBtn = UIFactory.CreateButton(header.transform, "Back", "← BACK", () => SceneManager.LoadScene("MainMenuScene"));
            var backRect = backBtn.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.88f, 0.2f);
            backRect.anchorMax = new Vector2(0.98f, 0.8f);
            backRect.sizeDelta = Vector2.zero;
        }
        
        private void BuildProfileCard()
        {
            var theme = Theme.Current;
            
            var card = UIFactory.CreatePanel(transform, "ProfileCard", theme.cardPanelColor);
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.02f, 0.7f);
            cardRect.anchorMax = new Vector2(0.35f, 0.88f);
            cardRect.sizeDelta = Vector2.zero;
            
            // Avatar
            var avatar = UIFactory.CreatePanel(card.transform, "Avatar", theme.backgroundColor);
            var avatarRect = avatar.GetComponent<RectTransform>();
            avatarRect.anchorMin = new Vector2(0.03f, 0.1f);
            avatarRect.anchorMax = new Vector2(0.25f, 0.9f);
            avatarRect.sizeDelta = Vector2.zero;
            
            var avatarIcon = UIFactory.CreateText(avatar.transform, "Icon", "👤", 42f, Color.white);
            avatarIcon.alignment = TextAlignmentOptions.Center;
            var iconRect = avatarIcon.GetComponent<RectTransform>();
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.sizeDelta = Vector2.zero;
            
            // Username
            usernameText = UIFactory.CreateTitle(card.transform, "Username", "Loading...", 28f);
            usernameText.color = theme.textPrimary;
            var usernameRect = usernameText.GetComponent<RectTransform>();
            usernameRect.anchorMin = new Vector2(0.28f, 0.5f);
            usernameRect.anchorMax = new Vector2(0.98f, 0.9f);
            usernameRect.sizeDelta = Vector2.zero;
            
            // Level
            levelText = UIFactory.CreateText(card.transform, "Level", "Level 1", 20f, theme.accentColor);
            levelText.fontStyle = FontStyles.Bold;
            var levelRect = levelText.GetComponent<RectTransform>();
            levelRect.anchorMin = new Vector2(0.28f, 0.1f);
            levelRect.anchorMax = new Vector2(0.98f, 0.5f);
            levelRect.sizeDelta = Vector2.zero;
        }
        
        private void BuildStatsGrid()
        {
            var theme = Theme.Current;
            
            var grid = UIFactory.CreatePanel(transform, "StatsGrid", Color.clear);
            var gridRect = grid.GetComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0.02f, 0.25f);
            gridRect.anchorMax = new Vector2(0.98f, 0.68f);
            gridRect.sizeDelta = Vector2.zero;
            
            var glg = grid.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(280, 120);
            glg.spacing = new Vector2(20, 20);
            glg.padding = new RectOffset(20, 20, 10, 10);
            glg.childAlignment = TextAnchor.UpperCenter;
            
            handsPlayedText = CreateStatCard(grid.transform, "Hands Played", "0", "🃏");
            handsWonText = CreateStatCard(grid.transform, "Hands Won", "0", "🏆");
            winRateText = CreateStatCard(grid.transform, "Win Rate", "0%", "📈");
            totalWinningsText = CreateStatCard(grid.transform, "Total Winnings", "0", "💰");
            biggestPotText = CreateStatCard(grid.transform, "Biggest Pot", "0", "💎");
            tournamentWinsText = CreateStatCard(grid.transform, "Tournament Wins", "0", "🏅");
            adventureProgressText = CreateStatCard(grid.transform, "Bosses Defeated", "0", "👹");
            playtimeText = CreateStatCard(grid.transform, "Playtime", "0h", "⏱️");
        }
        
        private TextMeshProUGUI CreateStatCard(Transform parent, string label, string value, string icon)
        {
            var theme = Theme.Current;
            
            var card = UIFactory.CreatePanel(parent, $"Stat_{label}", theme.cardPanelColor);
            
            var vlg = card.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8;
            vlg.padding = new RectOffset(15, 15, 15, 15);
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            
            // Icon + Label
            var headerText = UIFactory.CreateText(card.transform, "Header", $"{icon} {label}", 16f, theme.textSecondary);
            headerText.GetOrAddComponent<LayoutElement>().preferredHeight = 25;
            headerText.alignment = TextAlignmentOptions.Center;
            
            // Value
            var valueText = UIFactory.CreateTitle(card.transform, "Value", value, 32f);
            valueText.GetOrAddComponent<LayoutElement>().preferredHeight = 45;
            valueText.alignment = TextAlignmentOptions.Center;
            valueText.color = theme.textPrimary;
            
            return valueText;
        }
        
        private void BuildProgressBars()
        {
            var theme = Theme.Current;
            
            var progressPanel = UIFactory.CreatePanel(transform, "Progress", theme.cardPanelColor);
            var panelRect = progressPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.02f, 0.02f);
            panelRect.anchorMax = new Vector2(0.98f, 0.22f);
            panelRect.sizeDelta = Vector2.zero;
            
            var vlg = progressPanel.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 15;
            vlg.padding = new RectOffset(30, 30, 20, 20);
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            
            CreateProgressBar(progressPanel.transform, "Level Progress", 0.65f, theme.accentColor, "Level 25 → 26");
            CreateProgressBar(progressPanel.transform, "Daily Challenge", 0.33f, theme.successColor, "3/9 hands won");
            CreateProgressBar(progressPanel.transform, "Weekly Goal", 0.80f, theme.warningColor, "8000/10000 chips earned");
        }
        
        private void CreateProgressBar(Transform parent, string label, float progress, Color barColor, string details)
        {
            var theme = Theme.Current;
            
            var row = UIFactory.CreatePanel(parent, $"Progress_{label}", Color.clear);
            row.GetOrAddComponent<LayoutElement>().preferredHeight = 40;
            
            // Label
            var labelText = UIFactory.CreateText(row.transform, "Label", label, 16f, theme.textPrimary);
            labelText.fontStyle = FontStyles.Bold;
            var labelRect = labelText.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.5f);
            labelRect.anchorMax = new Vector2(0.2f, 1);
            labelRect.sizeDelta = Vector2.zero;
            
            // Bar background
            var barBg = UIFactory.CreatePanel(row.transform, "BarBg", theme.backgroundColor);
            var barBgRect = barBg.GetComponent<RectTransform>();
            barBgRect.anchorMin = new Vector2(0.22f, 0.25f);
            barBgRect.anchorMax = new Vector2(0.85f, 0.75f);
            barBgRect.sizeDelta = Vector2.zero;
            
            // Bar fill
            var barFill = UIFactory.CreatePanel(barBg.transform, "Fill", barColor);
            var fillRect = barFill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(progress, 1);
            fillRect.sizeDelta = Vector2.zero;
            
            // Details
            var detailsText = UIFactory.CreateText(row.transform, "Details", details, 14f, theme.textSecondary);
            var detailsRect = detailsText.GetComponent<RectTransform>();
            detailsRect.anchorMin = new Vector2(0.87f, 0);
            detailsRect.anchorMax = new Vector2(1f, 1);
            detailsRect.sizeDelta = Vector2.zero;
            detailsText.alignment = TextAlignmentOptions.Right;
        }
        
        private void LoadStats()
        {
            var user = GameService.CurrentUser;
            if (user == null)
            {
                usernameText.text = "Not logged in";
                return;
            }
            
            usernameText.text = user.username;
            levelText.text = $"Level {user.level}";
            
            handsPlayedText.text = user.handsPlayed.ToString("N0");
            handsWonText.text = user.handsWon.ToString("N0");
            
            float winRate = user.handsPlayed > 0 ? (float)user.handsWon / user.handsPlayed * 100 : 0;
            winRateText.text = $"{winRate:F1}%";
            
            totalWinningsText.text = user.totalWinnings.ToString("N0");
            biggestPotText.text = user.biggestPot.ToString("N0");
            tournamentWinsText.text = user.tournamentsWon.ToString("N0");
            
            // Adventure progress
            var progress = user.adventureProgress;
            if (progress != null)
            {
                adventureProgressText.text = progress.bossesDefeated.ToString("N0");
            }
            
            // Playtime (mock for now)
            playtimeText.text = "24h 35m";
        }
    }
}

