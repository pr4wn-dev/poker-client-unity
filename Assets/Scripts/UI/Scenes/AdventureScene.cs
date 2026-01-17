using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using PokerClient.UI;
using PokerClient.UI.Components;
using PokerClient.Networking;
using System.Collections.Generic;

namespace PokerClient.UI.Scenes
{
    /// <summary>
    /// Adventure Mode - World map with areas, bosses, and progression.
    /// </summary>
    public class AdventureScene : MonoBehaviour
    {
        [Header("Panels")]
        private GameObject worldMapPanel;
        private GameObject areaDetailPanel;
        private GameObject bossDetailPanel;
        private GameObject loadingPanel;
        
        [Header("World Map")]
        private Transform areasContainer;
        private Dictionary<string, GameObject> areaButtons = new Dictionary<string, GameObject>();
        
        [Header("Player Stats")]
        private TextMeshProUGUI playerLevelText;
        private TextMeshProUGUI playerXPText;
        private TextMeshProUGUI playerCoinsText;
        private Image xpProgressBar;
        
        [Header("Area Detail")]
        private TextMeshProUGUI areaNameText;
        private TextMeshProUGUI areaDescriptionText;
        private Transform bossListContainer;
        
        [Header("Boss Detail")]
        private TextMeshProUGUI bossNameText;
        private TextMeshProUGUI bossDescriptionText;
        private TextMeshProUGUI bossRewardsText;
        private TextMeshProUGUI bossRequirementsText;
        private Button challengeButton;
        
        private Canvas _canvas;
        private GameService _gameService;
        private WorldMapState _currentMapState;
        private string _selectedAreaId;
        private BossListItem _selectedBoss;
        
        private void Start()
        {
            _gameService = GameService.Instance;
            if (_gameService == null)
            {
                Debug.LogError("GameService not found!");
                SceneManager.LoadScene("MainMenuScene");
                return;
            }
            
            _gameService.OnWorldMapReceived += OnWorldMapReceived;
            _gameService.OnBossesReceived += OnBossesReceived;
            _gameService.OnAdventureStarted += OnAdventureStarted;
            
            BuildScene();
            LoadWorldMap();
        }
        
        private void OnDestroy()
        {
            if (_gameService != null)
            {
                _gameService.OnWorldMapReceived -= OnWorldMapReceived;
                _gameService.OnBossesReceived -= OnBossesReceived;
                _gameService.OnAdventureStarted -= OnAdventureStarted;
            }
        }
        
        private void BuildScene()
        {
            _canvas = FindObjectOfType<Canvas>();
            if (_canvas == null)
            {
                var canvasObj = new GameObject("Canvas");
                _canvas = canvasObj.AddComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasObj.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
                canvasObj.AddComponent<GraphicRaycaster>();
            }
            
            var theme = Theme.Current;
            
            // Background
            var bg = UIFactory.CreatePanel(_canvas.transform, "Background", theme.backgroundColor);
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            
            BuildHeader();
            BuildWorldMapPanel();
            BuildAreaDetailPanel();
            BuildBossDetailPanel();
            BuildLoadingPanel();
            
            ShowWorldMap();
        }
        
        private void BuildHeader()
        {
            var theme = Theme.Current;
            
            var header = UIFactory.CreatePanel(_canvas.transform, "Header", theme.cardPanelColor);
            var headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 1);
            headerRect.anchorMax = new Vector2(1, 1);
            headerRect.pivot = new Vector2(0.5f, 1);
            headerRect.sizeDelta = new Vector2(0, 100);
            headerRect.anchoredPosition = Vector2.zero;
            
            // Back button
            var backBtn = UIFactory.CreateButton(header.transform, "BackBtn", "← Back", () => SceneManager.LoadScene("MainMenuScene"));
            var backRect = backBtn.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0, 0.5f);
            backRect.anchorMax = new Vector2(0, 0.5f);
            backRect.pivot = new Vector2(0, 0.5f);
            backRect.anchoredPosition = new Vector2(20, 0);
            backRect.sizeDelta = new Vector2(120, 50);
            
            // Title
            var title = UIFactory.CreateTitle(header.transform, "Title", "ADVENTURE MODE", 36f);
            title.alignment = TextAlignmentOptions.Center;
            
            // Player Stats (right side)
            var statsPanel = UIFactory.CreatePanel(header.transform, "StatsPanel", Color.clear);
            var statsRect = statsPanel.GetComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(1, 0);
            statsRect.anchorMax = new Vector2(1, 1);
            statsRect.pivot = new Vector2(1, 0.5f);
            statsRect.anchoredPosition = new Vector2(-20, 0);
            statsRect.sizeDelta = new Vector2(350, 0);
            
            var vlg = statsPanel.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 5;
            vlg.padding = new RectOffset(10, 10, 10, 10);
            vlg.childAlignment = TextAnchor.MiddleRight;
            vlg.childControlHeight = false;
            vlg.childForceExpandHeight = false;
            
            // Level & XP row
            var levelRow = UIFactory.CreatePanel(statsPanel.transform, "LevelRow", Color.clear);
            levelRow.GetComponent<LayoutElement>().preferredHeight = 30;
            var levelHlg = levelRow.AddComponent<HorizontalLayoutGroup>();
            levelHlg.spacing = 20;
            levelHlg.childAlignment = TextAnchor.MiddleRight;
            
            playerLevelText = UIFactory.CreateText(levelRow.transform, "Level", "Level 1", 22f, theme.primaryColor);
            playerLevelText.fontStyle = FontStyles.Bold;
            playerLevelText.GetComponent<LayoutElement>().preferredWidth = 100;
            
            playerXPText = UIFactory.CreateText(levelRow.transform, "XP", "0 / 100 XP", 18f, theme.textSecondary);
            playerXPText.GetComponent<LayoutElement>().preferredWidth = 150;
            
            // XP Progress bar
            var xpBarBg = UIFactory.CreatePanel(statsPanel.transform, "XPBarBg", theme.backgroundColor);
            xpBarBg.GetComponent<LayoutElement>().preferredHeight = 15;
            var xpBgRect = xpBarBg.GetComponent<RectTransform>();
            
            var xpBarFill = UIFactory.CreatePanel(xpBarBg.transform, "XPBarFill", theme.primaryColor);
            xpProgressBar = xpBarFill.GetComponent<Image>();
            var xpFillRect = xpBarFill.GetComponent<RectTransform>();
            xpFillRect.anchorMin = Vector2.zero;
            xpFillRect.anchorMax = new Vector2(0.5f, 1);
            xpFillRect.sizeDelta = Vector2.zero;
            
            // Coins
            playerCoinsText = UIFactory.CreateText(statsPanel.transform, "Coins", "🪙 0 Coins", 18f, theme.accentColor);
            playerCoinsText.GetComponent<LayoutElement>().preferredHeight = 25;
            playerCoinsText.alignment = TextAlignmentOptions.Right;
        }
        
        private void BuildWorldMapPanel()
        {
            var theme = Theme.Current;
            
            worldMapPanel = UIFactory.CreatePanel(_canvas.transform, "WorldMapPanel", Color.clear);
            var panelRect = worldMapPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.05f, 0.05f);
            panelRect.anchorMax = new Vector2(0.95f, 0.85f);
            panelRect.sizeDelta = Vector2.zero;
            
            // Map background (could be an image)
            var mapBg = UIFactory.CreatePanel(worldMapPanel.transform, "MapBackground", new Color(0.15f, 0.2f, 0.15f));
            var mapBgRect = mapBg.GetComponent<RectTransform>();
            mapBgRect.anchorMin = Vector2.zero;
            mapBgRect.anchorMax = Vector2.one;
            mapBgRect.sizeDelta = Vector2.zero;
            
            areasContainer = worldMapPanel.transform;
            
            // Areas will be created dynamically when data arrives
        }
        
        private void BuildAreaDetailPanel()
        {
            var theme = Theme.Current;
            
            areaDetailPanel = UIFactory.CreatePanel(_canvas.transform, "AreaDetailPanel", theme.cardPanelColor);
            var panelRect = areaDetailPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.1f, 0.1f);
            panelRect.anchorMax = new Vector2(0.9f, 0.85f);
            panelRect.sizeDelta = Vector2.zero;
            
            var vlg = areaDetailPanel.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 15;
            vlg.padding = new RectOffset(30, 30, 30, 30);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = false;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            
            // Back button
            var backBtn = UIFactory.CreateButton(areaDetailPanel.transform, "BackBtn", "← Back to Map", () => ShowWorldMap());
            backBtn.GetComponent<LayoutElement>().preferredHeight = 45;
            
            // Area name
            areaNameText = UIFactory.CreateTitle(areaDetailPanel.transform, "AreaName", "Area Name", 36f);
            areaNameText.GetComponent<LayoutElement>().preferredHeight = 50;
            
            // Area description
            areaDescriptionText = UIFactory.CreateText(areaDetailPanel.transform, "AreaDesc", "", 18f, theme.textSecondary);
            areaDescriptionText.GetComponent<LayoutElement>().preferredHeight = 60;
            areaDescriptionText.alignment = TextAlignmentOptions.Center;
            
            // Bosses title
            var bossesTitle = UIFactory.CreateTitle(areaDetailPanel.transform, "BossesTitle", "BOSSES", 24f);
            bossesTitle.GetComponent<LayoutElement>().preferredHeight = 35;
            
            // Boss list container
            var bossListScroll = UIFactory.CreatePanel(areaDetailPanel.transform, "BossListScroll", theme.backgroundColor);
            bossListScroll.GetComponent<LayoutElement>().flexibleHeight = 1;
            
            bossListContainer = bossListScroll.transform;
            
            areaDetailPanel.SetActive(false);
        }
        
        private void BuildBossDetailPanel()
        {
            var theme = Theme.Current;
            
            bossDetailPanel = UIFactory.CreatePanel(_canvas.transform, "BossDetailPanel", new Color(0, 0, 0, 0.9f));
            var panelRect = bossDetailPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            
            // Content panel
            var content = UIFactory.CreatePanel(bossDetailPanel.transform, "Content", theme.cardPanelColor);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.2f, 0.15f);
            contentRect.anchorMax = new Vector2(0.8f, 0.85f);
            contentRect.sizeDelta = Vector2.zero;
            
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 20;
            vlg.padding = new RectOffset(40, 40, 40, 40);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = false;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            
            // Boss name
            bossNameText = UIFactory.CreateTitle(content.transform, "BossName", "Boss Name", 42f);
            bossNameText.GetComponent<LayoutElement>().preferredHeight = 60;
            bossNameText.color = theme.dangerColor;
            
            // Description
            bossDescriptionText = UIFactory.CreateText(content.transform, "BossDesc", "", 20f, theme.textSecondary);
            bossDescriptionText.GetComponent<LayoutElement>().preferredHeight = 80;
            bossDescriptionText.alignment = TextAlignmentOptions.Center;
            
            // Requirements
            bossRequirementsText = UIFactory.CreateText(content.transform, "Requirements", "", 18f, theme.textSecondary);
            bossRequirementsText.GetComponent<LayoutElement>().preferredHeight = 40;
            bossRequirementsText.alignment = TextAlignmentOptions.Center;
            
            // Rewards
            var rewardsTitle = UIFactory.CreateTitle(content.transform, "RewardsTitle", "REWARDS", 24f);
            rewardsTitle.GetComponent<LayoutElement>().preferredHeight = 35;
            rewardsTitle.color = theme.accentColor;
            
            bossRewardsText = UIFactory.CreateText(content.transform, "Rewards", "", 20f, theme.accentColor);
            bossRewardsText.GetComponent<LayoutElement>().preferredHeight = 60;
            bossRewardsText.alignment = TextAlignmentOptions.Center;
            
            // Buttons
            var buttonRow = UIFactory.CreatePanel(content.transform, "ButtonRow", Color.clear);
            buttonRow.GetComponent<LayoutElement>().preferredHeight = 60;
            var hlg = buttonRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 30;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childForceExpandWidth = false;
            
            var cancelBtn = UIFactory.CreateButton(buttonRow.transform, "Cancel", "Cancel", () => bossDetailPanel.SetActive(false));
            cancelBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(150, 50);
            
            challengeButton = UIFactory.CreateButton(buttonRow.transform, "Challenge", "CHALLENGE", OnChallengeClick).GetComponent<Button>();
            challengeButton.GetComponent<RectTransform>().sizeDelta = new Vector2(180, 50);
            challengeButton.GetComponent<Image>().color = theme.primaryColor;
            
            bossDetailPanel.SetActive(false);
        }
        
        private void BuildLoadingPanel()
        {
            loadingPanel = UIFactory.CreatePanel(_canvas.transform, "LoadingPanel", new Color(0, 0, 0, 0.7f));
            var panelRect = loadingPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;
            
            var loadingText = UIFactory.CreateTitle(loadingPanel.transform, "LoadingText", "Loading...", 36f);
            loadingText.alignment = TextAlignmentOptions.Center;
            
            loadingPanel.SetActive(false);
        }
        
        #region Navigation
        
        private void ShowWorldMap()
        {
            worldMapPanel.SetActive(true);
            areaDetailPanel.SetActive(false);
            bossDetailPanel.SetActive(false);
        }
        
        private void ShowAreaDetail(string areaId)
        {
            _selectedAreaId = areaId;
            worldMapPanel.SetActive(false);
            areaDetailPanel.SetActive(true);
            bossDetailPanel.SetActive(false);
            
            // Get area info
            if (_currentMapState != null)
            {
                var area = _currentMapState.areas?.Find(a => a.id == areaId);
                if (area != null)
                {
                    areaNameText.text = area.name;
                    areaDescriptionText.text = area.isUnlocked ? "Challenge the bosses here!" : $"🔒 {area.unlockReason}";
                }
            }
            
            // Load bosses for this area
            _gameService.GetAreaBosses(areaId);
        }
        
        private void ShowBossDetail(BossListItem boss)
        {
            _selectedBoss = boss;
            
            bossNameText.text = boss.name;
            bossDescriptionText.text = $"Difficulty: {boss.difficulty}";
            bossRequirementsText.text = boss.canChallenge 
                ? $"Entry Fee: {boss.entryFee} coins" 
                : $"🔒 Requires Level {boss.minLevel}";
            
            if (boss.rewards != null)
            {
                bossRewardsText.text = $"+{boss.rewards.xp} XP  |  +{boss.rewards.coins} Coins  |  +{boss.rewards.chips} Chips";
            }
            
            challengeButton.interactable = boss.canChallenge;
            
            bossDetailPanel.SetActive(true);
        }
        
        #endregion
        
        #region Data Loading
        
        private void LoadWorldMap()
        {
            loadingPanel.SetActive(true);
            _gameService.GetWorldMap();
        }
        
        private void OnWorldMapReceived(WorldMapState state)
        {
            loadingPanel.SetActive(false);
            _currentMapState = state;
            
            // Update player stats
            playerLevelText.text = $"Level {state.playerLevel}";
            playerXPText.text = $"{state.playerXP} / {state.xpForNextLevel} XP";
            
            float progress = state.xpForNextLevel > 0 ? (float)state.playerXP / state.xpForNextLevel : 0;
            var xpRect = xpProgressBar.GetComponent<RectTransform>();
            xpRect.anchorMax = new Vector2(Mathf.Clamp01(progress), 1);
            
            // Create area buttons
            CreateAreaButtons(state.areas);
        }
        
        private void CreateAreaButtons(List<AreaInfo> areas)
        {
            // Clear existing
            foreach (var btn in areaButtons.Values)
            {
                Destroy(btn);
            }
            areaButtons.Clear();
            
            if (areas == null) return;
            
            var theme = Theme.Current;
            
            // Predefined positions for areas on the map
            var positions = new List<Vector2>
            {
                new Vector2(0.15f, 0.3f),  // Poker Academy
                new Vector2(0.35f, 0.5f),  // Downtown Casino
                new Vector2(0.55f, 0.7f),  // The Highrise
                new Vector2(0.7f, 0.4f),   // The Underground
                new Vector2(0.85f, 0.6f),  // Golden Yacht
                new Vector2(0.5f, 0.2f),   // Private Island
                new Vector2(0.25f, 0.75f), // The Penthouse
                new Vector2(0.9f, 0.25f),  // Mystery Lounge
            };
            
            for (int i = 0; i < areas.Count && i < positions.Count; i++)
            {
                var area = areas[i];
                var pos = positions[i];
                
                var btn = CreateAreaButton(area, pos);
                areaButtons[area.id] = btn;
            }
        }
        
        private GameObject CreateAreaButton(AreaInfo area, Vector2 position)
        {
            var theme = Theme.Current;
            
            Color btnColor = area.isUnlocked ? theme.primaryColor : new Color(0.4f, 0.4f, 0.4f);
            
            var button = UIFactory.CreatePanel(areasContainer, $"Area_{area.id}", btnColor);
            var rect = button.GetComponent<RectTransform>();
            rect.anchorMin = position;
            rect.anchorMax = position;
            rect.sizeDelta = new Vector2(180, 80);
            
            var btn = button.AddComponent<Button>();
            btn.onClick.AddListener(() => OnAreaClick(area.id));
            btn.interactable = area.isUnlocked;
            
            var vlg = button.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(10, 10, 5, 5);
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlHeight = false;
            
            // Area name
            var nameText = UIFactory.CreateText(button.transform, "Name", area.name, 18f, Color.white);
            nameText.GetComponent<LayoutElement>().preferredHeight = 25;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.fontStyle = FontStyles.Bold;
            
            // Progress
            string progressStr = area.isUnlocked 
                ? $"{area.completedBosses}/{area.bossCount} Bosses"
                : "🔒 Locked";
            var progressText = UIFactory.CreateText(button.transform, "Progress", progressStr, 14f, 
                area.isUnlocked ? theme.textSecondary : theme.dangerColor);
            progressText.GetComponent<LayoutElement>().preferredHeight = 20;
            progressText.alignment = TextAlignmentOptions.Center;
            
            return button;
        }
        
        private void OnAreaClick(string areaId)
        {
            ShowAreaDetail(areaId);
        }
        
        private void OnBossesReceived(List<BossListItem> bosses)
        {
            // Clear existing
            foreach (Transform child in bossListContainer)
            {
                if (child.name != "BossListScroll")
                    Destroy(child.gameObject);
            }
            
            if (bosses == null || bosses.Count == 0)
            {
                var noData = UIFactory.CreateText(bossListContainer, "NoData", "No bosses found", 20f, Theme.Current.textSecondary);
                noData.alignment = TextAlignmentOptions.Center;
                return;
            }
            
            foreach (var boss in bosses)
            {
                CreateBossListItem(boss);
            }
        }
        
        private void CreateBossListItem(BossListItem boss)
        {
            var theme = Theme.Current;
            
            var item = UIFactory.CreatePanel(bossListContainer, $"Boss_{boss.id}", theme.cardPanelColor);
            item.GetComponent<LayoutElement>().preferredHeight = 80;
            
            var hlg = item.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20;
            hlg.padding = new RectOffset(20, 20, 10, 10);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childForceExpandWidth = false;
            
            // Boss name
            var nameText = UIFactory.CreateTitle(item.transform, "Name", boss.name, 24f);
            nameText.GetComponent<LayoutElement>().preferredWidth = 250;
            nameText.color = boss.canChallenge ? theme.textPrimary : theme.textSecondary;
            
            // Difficulty
            Color diffColor = boss.difficulty?.ToLower() switch
            {
                "easy" => theme.successColor,
                "medium" => theme.accentColor,
                "hard" => theme.dangerColor,
                "legendary" => new Color(0.8f, 0.2f, 0.8f),
                _ => theme.textSecondary
            };
            var diffText = UIFactory.CreateText(item.transform, "Difficulty", boss.difficulty?.ToUpper() ?? "UNKNOWN", 16f, diffColor);
            diffText.GetComponent<LayoutElement>().preferredWidth = 120;
            diffText.fontStyle = FontStyles.Bold;
            
            // Requirements
            var reqText = UIFactory.CreateText(item.transform, "Req", $"Lv.{boss.minLevel} | {boss.entryFee} coins", 16f, theme.textSecondary);
            reqText.GetComponent<LayoutElement>().preferredWidth = 180;
            
            // Spacer
            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(item.transform, false);
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1;
            
            // Challenge button
            var challengeBtn = UIFactory.CreateButton(item.transform, "Challenge", boss.canChallenge ? "CHALLENGE" : "🔒", 
                () => ShowBossDetail(boss));
            challengeBtn.GetComponent<LayoutElement>().preferredWidth = 140;
            challengeBtn.GetComponent<Button>().interactable = boss.canChallenge;
            
            if (boss.canChallenge)
            {
                challengeBtn.GetComponent<Image>().color = theme.primaryColor;
            }
        }
        
        #endregion
        
        #region Actions
        
        private void OnChallengeClick()
        {
            if (_selectedBoss == null) return;
            
            loadingPanel.SetActive(true);
            bossDetailPanel.SetActive(false);
            
            _gameService.StartAdventure(_selectedBoss.id, (success, error, session) =>
            {
                loadingPanel.SetActive(false);
                
                if (!success)
                {
                    Debug.LogError($"Failed to start adventure: {error}");
                    return;
                }
                
                // Adventure started - go to adventure battle scene
                // For now, just log it
                Debug.Log($"Adventure started against {_selectedBoss.name}!");
            });
        }
        
        private void OnAdventureStarted(AdventureSession session)
        {
            // Go to adventure battle scene
            SceneManager.LoadScene("AdventureBattleScene");
        }
        
        #endregion
    }
}


