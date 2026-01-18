using UnityEngine;
using PokerClient.Networking;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PokerClient.Adventure
{
    /// <summary>
    /// Controls Adventure mode gameplay
    /// </summary>
    public class AdventureController : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private Transform levelSelectContainer;
        [SerializeField] private GameObject levelButtonPrefab;
        [SerializeField] private AdventureBattleUI battleUI;
        
        private PokerNetworkManager _network;
        private List<LevelInfo> _levels;
        private AdventureSession _currentSession;
        
        public bool IsInBattle => _currentSession != null;
        public AdventureSession CurrentSession => _currentSession;
        
        private void Start()
        {
            _network = PokerNetworkManager.Instance;
            
            // Subscribe to adventure events
            // _network.OnAdventureStateUpdated += HandleAdventureState;
            // _network.OnAdventureResult += HandleAdventureResult;
            // _network.OnBossTaunt += HandleBossTaunt;
        }
        
        /// <summary>
        /// Load and display level select screen
        /// </summary>
        public async Task LoadLevelSelectAsync()
        {
            // TODO: Implement when server events are connected
            // var response = await _network.GetLevelsAsync();
            // _levels = response.levels;
            // PopulateLevelButtons();
            
            Debug.Log("[Adventure] Loading level select...");
        }
        
        /// <summary>
        /// Start adventure battle at specified level
        /// </summary>
        public async Task StartBattleAsync(int level)
        {
            Debug.Log($"[Adventure] Starting battle at level {level}");
            
            // TODO: Implement when server events are connected
            // var response = await _network.StartAdventureAsync(level);
            // if (response.success)
            // {
            //     _currentSession = response.session;
            //     battleUI?.ShowBattle(_currentSession);
            // }
        }
        
        /// <summary>
        /// Perform action during adventure battle
        /// </summary>
        public async Task PerformActionAsync(PokerAction action, int amount = 0)
        {
            if (!IsInBattle)
            {
                Debug.LogWarning("[Adventure] Not in battle!");
                return;
            }
            
            // TODO: Implement when server events are connected
            // var response = await _network.AdventureActionAsync(action, amount);
            // HandleActionResponse(response);
        }
        
        /// <summary>
        /// Forfeit current battle
        /// </summary>
        public async Task ForfeitBattleAsync()
        {
            if (!IsInBattle) return;
            
            // TODO: Implement when server events are connected
            // await _network.ForfeitAdventureAsync();
            
            _currentSession = null;
            Debug.Log("[Adventure] Battle forfeited");
        }
        
        #region Event Handlers
        
        private void HandleAdventureState(AdventureSession session)
        {
            _currentSession = session;
            battleUI?.UpdateState(session);
        }
        
        private void HandleAdventureResult(AdventureResult result)
        {
            _currentSession = null;
            
            if (result.status == "victory")
            {
                Debug.Log($"[Adventure] Victory! Earned {result.rewards.coins} coins");
                ShowVictoryScreen(result);
            }
            else if (result.status == "defeat")
            {
                Debug.Log($"[Adventure] Defeat: {result.message}");
                ShowDefeatScreen(result);
            }
        }
        
        private void HandleBossTaunt(string taunt)
        {
            battleUI?.ShowBossTaunt(taunt);
        }
        
        #endregion
        
        #region UI Helpers
        
        private void PopulateLevelButtons()
        {
            // Clear existing buttons
            foreach (Transform child in levelSelectContainer)
            {
                Destroy(child.gameObject);
            }
            
            // Create button for each level
            foreach (var level in _levels)
            {
                var button = Instantiate(levelButtonPrefab, levelSelectContainer);
                var levelButton = button.GetComponent<LevelButtonUI>();
                levelButton?.Setup(level, () => StartBattleAsync(level.level));
            }
        }
        
        private void ShowVictoryScreen(AdventureResult result)
        {
            // TODO: Show victory UI with rewards
        }
        
        private void ShowDefeatScreen(AdventureResult result)
        {
            // TODO: Show defeat UI with boss message
        }
        
        #endregion
    }
    
    /// <summary>
    /// UI for adventure battle screen
    /// </summary>
    public class AdventureBattleUI : MonoBehaviour
    {
        [Header("Boss Display")]
        [SerializeField] private TMPro.TextMeshProUGUI bossNameText;
        [SerializeField] private TMPro.TextMeshProUGUI bossChipsText;
        [SerializeField] private TMPro.TextMeshProUGUI bossTauntText;
        [SerializeField] private UnityEngine.UI.Image bossAvatar;
        
        [Header("Player Display")]
        [SerializeField] private TMPro.TextMeshProUGUI playerChipsText;
        
        [Header("Game State")]
        [SerializeField] private TMPro.TextMeshProUGUI levelText;
        [SerializeField] private TMPro.TextMeshProUGUI handsPlayedText;
        
        public void ShowBattle(AdventureSession session)
        {
            gameObject.SetActive(true);
            UpdateState(session);
        }
        
        public void UpdateState(AdventureSession session)
        {
            if (session.boss != null)
            {
                if (bossNameText) bossNameText.text = session.boss.name;
                if (bossChipsText) bossChipsText.text = $"${session.boss.chips:N0}";
            }
            
            if (playerChipsText) playerChipsText.text = $"${session.userChips:N0}";
            if (levelText) levelText.text = $"Level {session.level}";
            if (handsPlayedText) handsPlayedText.text = $"Hands: {session.handsPlayed}";
        }
        
        public void ShowBossTaunt(string taunt)
        {
            if (bossTauntText)
            {
                bossTauntText.text = taunt;
                // TODO: Animate taunt display
            }
        }
        
        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// UI for level select button
    /// </summary>
    public class LevelButtonUI : MonoBehaviour
    {
        [SerializeField] private TMPro.TextMeshProUGUI levelText;
        [SerializeField] private TMPro.TextMeshProUGUI bossNameText;
        [SerializeField] private TMPro.TextMeshProUGUI difficultyText;
        [SerializeField] private GameObject defeatedIcon;
        [SerializeField] private GameObject lockedIcon;
        [SerializeField] private UnityEngine.UI.Button button;
        
        private System.Action _onClick;
        
        public void Setup(LevelInfo level, System.Action onClick)
        {
            _onClick = onClick;
            
            if (levelText) levelText.text = $"Level {level.level}";
            if (bossNameText) bossNameText.text = level.bossName;
            if (difficultyText) difficultyText.text = level.difficulty;
            if (defeatedIcon) defeatedIcon.SetActive(level.isDefeated);
            if (lockedIcon) lockedIcon.SetActive(!level.isUnlocked);
            
            if (button)
            {
                button.interactable = level.isUnlocked;
                button.onClick.AddListener(() => _onClick?.Invoke());
            }
        }
    }
}





