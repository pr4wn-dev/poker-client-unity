using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PokerClient.UI.Components;
using PokerClient.Networking;
using System.Collections.Generic;

namespace PokerClient.UI.Scenes
{
    /// <summary>
    /// Adventure Map scene - World map with areas and bosses.
    /// Shows areas, boss challenges, and player progression.
    /// </summary>
    public class AdventureMapScene : MonoBehaviour
    {
        [Header("Scene References")]
        [SerializeField] private Canvas canvas;
        
        [Header("Panels")]
        [SerializeField] private GameObject mapPanel;
        [SerializeField] private GameObject areaDetailPanel;
        [SerializeField] private GameObject bossDetailPanel;
        
        [Header("Player Info")]
        [SerializeField] private TextMeshProUGUI playerLevelText;
        [SerializeField] private TextMeshProUGUI playerXPText;
        [SerializeField] private Image xpProgressFill;
        [SerializeField] private TextMeshProUGUI playerChipsText;
        
        [Header("Area Nodes")]
        [SerializeField] private List<AreaNode> areaNodes = new List<AreaNode>();
        
        private WorldMapState _mapState;
        private AreaInfo _selectedArea;
        private BossListItem _selectedBoss;
        
        private void Start()
        {
            BuildScene();
            
            // TODO: Load map state from server
            // For now, create mock data
            CreateMockMapState();
        }
        
        private void BuildScene()
        {
            var theme = Theme.Current;
            
            // Ensure canvas exists
            if (canvas == null)
            {
                var canvasObj = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObj.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                
                var scaler = canvasObj.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
            }
            
            // Background (world map background)
            var bg = UIFactory.CreatePanel(canvas.transform, "Background", new Color(0.05f, 0.08f, 0.12f));
            UIFactory.FillParent(bg.GetComponent<RectTransform>());
            
            // === TOP BAR ===
            BuildTopBar(canvas.transform);
            
            // === PLAYER INFO BAR ===
            BuildPlayerInfoBar(canvas.transform);
            
            // === MAP AREA ===
            BuildMapArea(canvas.transform);
            
            // === AREA DETAIL PANEL (Right side popup) ===
            BuildAreaDetailPanel(canvas.transform);
            
            // === BOSS DETAIL PANEL ===
            BuildBossDetailPanel(canvas.transform);
        }
        
        private void BuildTopBar(Transform parent)
        {
            var theme = Theme.Current;
            
            var topBar = UIFactory.CreatePanel(parent, "TopBar", theme.panelColor);
            var topRect = topBar.GetComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0, 1);
            topRect.anchorMax = new Vector2(1, 1);
            topRect.pivot = new Vector2(0.5f, 1);
            topRect.anchoredPosition = Vector2.zero;
            topRect.sizeDelta = new Vector2(0, 60);
            
            // Back button
            var backBtn = UIFactory.CreateSecondaryButton(topBar.transform, "BackBtn", "← BACK", OnBackClick, 100, 40);
            var backRect = backBtn.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0, 0.5f);
            backRect.anchorMax = new Vector2(0, 0.5f);
            backRect.anchoredPosition = new Vector2(70, 0);
            
            // Title
            var title = UIFactory.CreateTitle(topBar.transform, "Title", "ADVENTURE MAP", 28f);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.sizeDelta = new Vector2(300, 40);
        }
        
        private void BuildPlayerInfoBar(Transform parent)
        {
            var theme = Theme.Current;
            
            var infoBar = UIFactory.CreatePanel(parent, "PlayerInfoBar", theme.cardPanelColor);
            var infoRect = infoBar.GetComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0, 1);
            infoRect.anchorMax = new Vector2(1, 1);
            infoRect.pivot = new Vector2(0.5f, 1);
            infoRect.anchoredPosition = new Vector2(0, -65);
            infoRect.sizeDelta = new Vector2(0, 50);
            
            // Level
            playerLevelText = UIFactory.CreateText(infoBar.transform, "Level", "Level 1", 18f,
                theme.textPrimary, TextAlignmentOptions.Left);
            playerLevelText.fontStyle = FontStyles.Bold;
            var levelRect = playerLevelText.GetComponent<RectTransform>();
            levelRect.anchorMin = new Vector2(0, 0.5f);
            levelRect.anchorMax = new Vector2(0, 0.5f);
            levelRect.pivot = new Vector2(0, 0.5f);
            levelRect.anchoredPosition = new Vector2(20, 0);
            levelRect.sizeDelta = new Vector2(100, 30);
            
            // XP Progress
            var (xpContainer, xpFill) = UIFactory.CreateProgressBar(infoBar.transform, "XPBar", 200, 16, theme.primaryColor);
            xpProgressFill = xpFill;
            var xpRect = xpContainer.GetComponent<RectTransform>();
            xpRect.anchorMin = new Vector2(0, 0.5f);
            xpRect.anchorMax = new Vector2(0, 0.5f);
            xpRect.anchoredPosition = new Vector2(180, 0);
            
            // XP Text
            playerXPText = UIFactory.CreateText(infoBar.transform, "XP", "0 / 100 XP", 12f,
                theme.textSecondary, TextAlignmentOptions.Left);
            var xpTextRect = playerXPText.GetComponent<RectTransform>();
            xpTextRect.anchorMin = new Vector2(0, 0.5f);
            xpTextRect.anchorMax = new Vector2(0, 0.5f);
            xpTextRect.anchoredPosition = new Vector2(400, 0);
            xpTextRect.sizeDelta = new Vector2(150, 25);
            
            // Chips
            playerChipsText = UIFactory.CreateText(infoBar.transform, "Chips", "10,000", 20f,
                theme.secondaryColor, TextAlignmentOptions.Right);
            playerChipsText.fontStyle = FontStyles.Bold;
            var chipsRect = playerChipsText.GetComponent<RectTransform>();
            chipsRect.anchorMin = new Vector2(1, 0.5f);
            chipsRect.anchorMax = new Vector2(1, 0.5f);
            chipsRect.pivot = new Vector2(1, 0.5f);
            chipsRect.anchoredPosition = new Vector2(-20, 0);
            chipsRect.sizeDelta = new Vector2(150, 30);
        }
        
        private void BuildMapArea(Transform parent)
        {
            var theme = Theme.Current;
            
            mapPanel = new GameObject("MapPanel", typeof(RectTransform));
            mapPanel.transform.SetParent(parent, false);
            var mapRect = mapPanel.GetComponent<RectTransform>();
            mapRect.anchorMin = new Vector2(0, 0);
            mapRect.anchorMax = new Vector2(0.65f, 1);
            mapRect.offsetMin = new Vector2(20, 20);
            mapRect.offsetMax = new Vector2(-10, -120);
            
            // Create area nodes based on predefined positions
            CreateAreaNodes();
        }
        
        private void CreateAreaNodes()
        {
            // Area positions (normalized 0-1 within map panel)
            var areaConfigs = new[]
            {
                ("area_tutorial", "Poker Academy", new Vector2(0.1f, 0.3f), true),
                ("area_downtown", "Downtown Casino", new Vector2(0.25f, 0.5f), true),
                ("area_highrise", "The Highrise", new Vector2(0.45f, 0.6f), false),
                ("area_underground", "Underground", new Vector2(0.35f, 0.25f), false),
                ("area_yacht", "Golden Yacht", new Vector2(0.65f, 0.4f), false),
                ("area_island", "Private Island", new Vector2(0.85f, 0.3f), false),
                ("area_penthouse", "The Penthouse", new Vector2(0.75f, 0.75f), false),
                ("area_secret_lounge", "???", new Vector2(0.1f, 0.8f), false),
            };
            
            foreach (var (id, name, pos, unlocked) in areaConfigs)
            {
                var node = AreaNode.Create(mapPanel.transform, id, name, pos, unlocked);
                node.OnClick += OnAreaNodeClick;
                areaNodes.Add(node);
            }
            
            // Draw connection lines between areas
            DrawAreaConnections();
        }
        
        private void DrawAreaConnections()
        {
            // Connections between areas (visual paths)
            var connections = new[]
            {
                (0, 1), // Tutorial -> Downtown
                (1, 2), // Downtown -> Highrise
                (1, 3), // Downtown -> Underground
                (2, 4), // Highrise -> Yacht
                (3, 4), // Underground -> Yacht
                (4, 5), // Yacht -> Island
                (5, 6), // Island -> Penthouse
            };
            
            // TODO: Draw lines between connected nodes
            // For now, connections are implied by node positions
        }
        
        private void BuildAreaDetailPanel(Transform parent)
        {
            var theme = Theme.Current;
            
            areaDetailPanel = UIFactory.CreatePanel(parent, "AreaDetailPanel", theme.panelColor);
            var panelRect = areaDetailPanel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.67f, 0);
            panelRect.anchorMax = new Vector2(1, 1);
            panelRect.offsetMin = new Vector2(0, 20);
            panelRect.offsetMax = new Vector2(-20, -120);
            
            var layout = areaDetailPanel.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10;
            layout.padding = new RectOffset(20, 20, 20, 20);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            
            // Area title
            var areaTitle = UIFactory.CreateTitle(areaDetailPanel.transform, "AreaTitle", "SELECT AN AREA", 24f);
            var titleRect = areaTitle.GetComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(0, 40);
            
            // Description
            var desc = UIFactory.CreateText(areaDetailPanel.transform, "Description", 
                "Click on an area node to see details", 14f, theme.textSecondary);
            var descRect = desc.GetComponent<RectTransform>();
            descRect.sizeDelta = new Vector2(0, 60);
            
            UIFactory.CreateDivider(areaDetailPanel.transform, "Divider", true, 250, 1);
            
            // Bosses label
            var bossesLabel = UIFactory.CreateText(areaDetailPanel.transform, "BossesLabel", 
                "BOSSES", 16f, theme.textPrimary, TextAlignmentOptions.Left);
            bossesLabel.fontStyle = FontStyles.Bold;
            var bossesLabelRect = bossesLabel.GetComponent<RectTransform>();
            bossesLabelRect.sizeDelta = new Vector2(0, 25);
            
            // Boss list container
            var bossListContainer = UIFactory.CreateVerticalGroup(areaDetailPanel.transform, "BossList", 10);
            var bossListRect = bossListContainer.GetComponent<RectTransform>();
            bossListRect.sizeDelta = new Vector2(0, 300);
            
            // Tournaments label
            var tournamentsLabel = UIFactory.CreateText(areaDetailPanel.transform, "TournamentsLabel",
                "TOURNAMENTS", 16f, theme.textPrimary, TextAlignmentOptions.Left);
            tournamentsLabel.fontStyle = FontStyles.Bold;
            var tournamentsLabelRect = tournamentsLabel.GetComponent<RectTransform>();
            tournamentsLabelRect.sizeDelta = new Vector2(0, 25);
        }
        
        private void BuildBossDetailPanel(Transform parent)
        {
            var theme = Theme.Current;
            
            bossDetailPanel = UIFactory.CreatePanel(parent, "BossDetailPanel", new Color(0, 0, 0, 0.9f));
            var panelRect = bossDetailPanel.GetComponent<RectTransform>();
            UIFactory.FillParent(panelRect);
            
            var innerPanel = UIFactory.CreatePanel(bossDetailPanel.transform, "Inner", theme.panelColor);
            var innerRect = innerPanel.GetComponent<RectTransform>();
            UIFactory.Center(innerRect, new Vector2(500, 450));
            
            var layout = innerPanel.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 12;
            layout.padding = new RectOffset(30, 30, 25, 25);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            
            // Boss name
            var bossName = UIFactory.CreateTitle(innerPanel.transform, "BossName", "Boss Name", 28f);
            var nameRect = bossName.GetComponent<RectTransform>();
            nameRect.sizeDelta = new Vector2(0, 40);
            
            // Difficulty
            var difficulty = UIFactory.CreateText(innerPanel.transform, "Difficulty", "EASY", 14f, theme.textSuccess);
            var diffRect = difficulty.GetComponent<RectTransform>();
            diffRect.sizeDelta = new Vector2(0, 25);
            
            // Description
            var description = UIFactory.CreateText(innerPanel.transform, "Description", 
                "A friendly dealer who will teach you the ropes.", 16f, theme.textSecondary);
            var descRect = description.GetComponent<RectTransform>();
            descRect.sizeDelta = new Vector2(0, 50);
            
            UIFactory.CreateDivider(innerPanel.transform, "Divider", true, 300, 1);
            
            // Requirements section
            var reqLabel = UIFactory.CreateText(innerPanel.transform, "ReqLabel", "REQUIREMENTS", 14f,
                theme.textPrimary, TextAlignmentOptions.Left);
            reqLabel.fontStyle = FontStyles.Bold;
            
            var reqText = UIFactory.CreateText(innerPanel.transform, "Requirements", 
                "Level 1\nEntry Fee: 0 chips", 14f, theme.textSecondary, TextAlignmentOptions.Left);
            var reqTextRect = reqText.GetComponent<RectTransform>();
            reqTextRect.sizeDelta = new Vector2(0, 45);
            
            // Rewards section
            var rewardLabel = UIFactory.CreateText(innerPanel.transform, "RewardLabel", "REWARDS", 14f,
                theme.textPrimary, TextAlignmentOptions.Left);
            rewardLabel.fontStyle = FontStyles.Bold;
            
            var rewardText = UIFactory.CreateText(innerPanel.transform, "Rewards",
                "+50 XP\n+100 Coins\n+500 Chips", 14f, theme.secondaryColor, TextAlignmentOptions.Left);
            var rewardTextRect = rewardText.GetComponent<RectTransform>();
            rewardTextRect.sizeDelta = new Vector2(0, 55);
            
            // Buttons
            var buttonRow = UIFactory.CreateHorizontalGroup(innerPanel.transform, "Buttons", 20);
            var buttonRect = buttonRow.GetComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(0, 55);
            
            var challengeBtn = UIFactory.CreatePrimaryButton(buttonRow.transform, "ChallengeBtn", "CHALLENGE", 
                OnChallengeBoss, 180, 50);
            var closeBtn = UIFactory.CreateSecondaryButton(buttonRow.transform, "CloseBtn", "CLOSE",
                () => bossDetailPanel.SetActive(false), 120, 50);
            
            bossDetailPanel.SetActive(false);
        }
        
        #region State Updates
        
        public void UpdateMapState(WorldMapState state)
        {
            _mapState = state;
            
            // Update player info
            playerLevelText.text = $"Level {state.playerLevel}";
            playerXPText.text = $"{state.playerXP} / {state.xpForNextLevel ?? 0} XP";
            
            // Update XP bar
            var xpRect = xpProgressFill.GetComponent<RectTransform>();
            xpRect.anchorMax = new Vector2(Mathf.Clamp01(state.xpProgress / 100f), 1);
            
            // Update area nodes
            if (state.areas != null)
            {
                foreach (var area in state.areas)
                {
                    var node = areaNodes.Find(n => n.AreaId == area.id);
                    if (node != null)
                    {
                        node.SetUnlocked(area.isUnlocked);
                        node.SetProgress(area.completedBosses, area.bossCount);
                    }
                }
            }
        }
        
        private void CreateMockMapState()
        {
            // Mock data for testing
            _mapState = new WorldMapState
            {
                playerLevel = 5,
                playerXP = 850,
                xpProgress = 35,
                xpForNextLevel = 1000,
                maxLevel = 25
            };
            
            playerLevelText.text = $"Level {_mapState.playerLevel}";
            playerXPText.text = $"{_mapState.playerXP} / {_mapState.xpForNextLevel} XP";
            playerChipsText.text = "25,000";
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnBackClick()
        {
            Debug.Log("Back to main menu");
            // TODO: Return to main menu
        }
        
        private void OnAreaNodeClick(string areaId)
        {
            Debug.Log($"Area clicked: {areaId}");
            _selectedArea = _mapState?.areas?.Find(a => a.id == areaId);
            
            // Update area detail panel
            // TODO: Populate with actual area data
        }
        
        private void OnChallengeBoss()
        {
            if (_selectedBoss == null) return;
            
            Debug.Log($"Challenging boss: {_selectedBoss.id}");
            // TODO: Start adventure session
        }
        
        #endregion
    }
    
    /// <summary>
    /// Visual node for an area on the world map
    /// </summary>
    public class AreaNode : MonoBehaviour
    {
        public string AreaId { get; private set; }
        public string AreaName { get; private set; }
        public bool IsUnlocked { get; private set; }
        
        public System.Action<string> OnClick;
        
        [SerializeField] private Image nodeImage;
        [SerializeField] private Image borderImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI progressText;
        [SerializeField] private Image lockIcon;
        
        public void SetUnlocked(bool unlocked)
        {
            IsUnlocked = unlocked;
            
            if (lockIcon != null)
                lockIcon.gameObject.SetActive(!unlocked);
            
            var color = unlocked ? Theme.Current.primaryColor : Theme.Current.buttonDisabled;
            if (nodeImage != null)
                nodeImage.color = color;
        }
        
        public void SetProgress(int completed, int total)
        {
            if (progressText != null)
            {
                progressText.text = $"{completed}/{total}";
                progressText.color = completed >= total ? Theme.Current.textSuccess : Theme.Current.textSecondary;
            }
        }
        
        public static AreaNode Create(Transform parent, string id, string name, Vector2 normalizedPos, bool unlocked)
        {
            var theme = Theme.Current;
            
            var nodeObj = new GameObject($"Area_{id}", typeof(RectTransform), typeof(AreaNode), typeof(Button));
            nodeObj.transform.SetParent(parent, false);
            
            var rect = nodeObj.GetComponent<RectTransform>();
            rect.anchorMin = normalizedPos;
            rect.anchorMax = normalizedPos;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(100, 80);
            
            var node = nodeObj.GetComponent<AreaNode>();
            node.AreaId = id;
            node.AreaName = name;
            node.IsUnlocked = unlocked;
            
            // Node background
            var bgObj = UIFactory.CreatePanel(nodeObj.transform, "Background", unlocked ? theme.primaryColor : theme.buttonDisabled);
            node.nodeImage = bgObj.GetComponent<Image>();
            var bgRect = bgObj.GetComponent<RectTransform>();
            UIFactory.Center(bgRect, new Vector2(70, 70));
            
            // Border
            var borderObj = UIFactory.CreatePanel(bgObj.transform, "Border", theme.textPrimary);
            node.borderImage = borderObj.GetComponent<Image>();
            UIFactory.FillParent(borderObj.GetComponent<RectTransform>());
            borderObj.transform.SetAsFirstSibling();
            
            var innerBg = UIFactory.CreatePanel(bgObj.transform, "InnerBg", unlocked ? theme.primaryColor : theme.buttonDisabled);
            var innerRect = innerBg.GetComponent<RectTransform>();
            UIFactory.FillParent(innerRect, 3);
            
            // Name text
            node.nameText = UIFactory.CreateText(nodeObj.transform, "Name", name, 11f, theme.textPrimary);
            var nameRect = node.nameText.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0.5f, 0);
            nameRect.anchorMax = new Vector2(0.5f, 0);
            nameRect.pivot = new Vector2(0.5f, 1);
            nameRect.anchoredPosition = new Vector2(0, -5);
            nameRect.sizeDelta = new Vector2(100, 20);
            
            // Lock icon (if locked)
            if (!unlocked)
            {
                var lockObj = UIFactory.CreateText(bgObj.transform, "Lock", "🔒", 24f, theme.textPrimary);
                node.lockIcon = lockObj.GetComponent<Image>();
            }
            
            // Button setup
            var button = nodeObj.GetComponent<Button>();
            button.onClick.AddListener(() => node.OnClick?.Invoke(node.AreaId));
            
            return node;
        }
    }
}



