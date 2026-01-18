using UnityEngine;
using UnityEngine.SceneManagement;
using PokerClient.Networking;
using PokerClient.UI;

namespace PokerClient.Core
{
    /// <summary>
    /// Central game manager - Handles game state, scene transitions, and persistence.
    /// This is a singleton that persists across scenes.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        
        [Header("Configuration")]
        [SerializeField] private GameTheme gameTheme;
        [SerializeField] private string serverAddress = "localhost";
        [SerializeField] private int serverPort = 3000;
        
        [Header("Current State")]
        [SerializeField] private bool isLoggedIn;
        [SerializeField] private string currentUserId;
        [SerializeField] private string currentUsername;
        
        // Player data
        public UserProfile CurrentUser { get; private set; }
        public int Chips => CurrentUser?.chips ?? 0;
        public int Level => 1; // TODO: Calculate from XP
        public int XP => 0; // TODO: Get from server
        
        // Scene names
        public const string SCENE_MAIN_MENU = "MainMenu";
        public const string SCENE_LOBBY = "Lobby";
        public const string SCENE_TABLE = "PokerTable";
        public const string SCENE_ADVENTURE_MAP = "AdventureMap";
        public const string SCENE_BOSS_BATTLE = "BossBattle";
        
        private void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Apply theme
            if (gameTheme != null)
            {
                Theme.Current = gameTheme;
            }
            
            Initialize();
        }
        
        private void Initialize()
        {
            Debug.Log("[GameManager] Initializing...");
            
            // TODO: Initialize network manager
            // TODO: Load saved settings
            // TODO: Check for saved login session
        }
        
        #region Authentication
        
        /// <summary>
        /// Login with username and password
        /// </summary>
        public void Login(string username, string password, System.Action<bool, string> callback)
        {
            Debug.Log($"[GameManager] Logging in as {username}...");
            
            // TODO: Send login request to server
            // For now, simulate success
            isLoggedIn = true;
            currentUsername = username;
            
            CurrentUser = new UserProfile
            {
                id = "mock-id",
                username = username,
                chips = 10000
            };
            
            callback?.Invoke(true, null);
        }
        
        /// <summary>
        /// Register new account
        /// </summary>
        public void Register(string username, string password, string email, System.Action<bool, string> callback)
        {
            Debug.Log($"[GameManager] Registering {username}...");
            
            // TODO: Send register request to server
            callback?.Invoke(true, null);
        }
        
        /// <summary>
        /// Logout current user
        /// </summary>
        public void Logout()
        {
            Debug.Log("[GameManager] Logging out...");
            
            isLoggedIn = false;
            currentUserId = null;
            currentUsername = null;
            CurrentUser = null;
            
            LoadScene(SCENE_MAIN_MENU);
        }
        
        public bool IsLoggedIn => isLoggedIn;
        
        #endregion
        
        #region Scene Management
        
        /// <summary>
        /// Load a scene by name
        /// </summary>
        public void LoadScene(string sceneName)
        {
            Debug.Log($"[GameManager] Loading scene: {sceneName}");
            SceneManager.LoadScene(sceneName);
        }
        
        /// <summary>
        /// Load scene asynchronously with loading screen
        /// </summary>
        public AsyncOperation LoadSceneAsync(string sceneName)
        {
            Debug.Log($"[GameManager] Loading scene async: {sceneName}");
            return SceneManager.LoadSceneAsync(sceneName);
        }
        
        // Convenience methods
        public void GoToMainMenu() => LoadScene(SCENE_MAIN_MENU);
        public void GoToLobby() => LoadScene(SCENE_LOBBY);
        public void GoToAdventureMap() => LoadScene(SCENE_ADVENTURE_MAP);
        
        /// <summary>
        /// Join a poker table
        /// </summary>
        public void JoinTable(string tableId)
        {
            Debug.Log($"[GameManager] Joining table: {tableId}");
            // TODO: Store table ID, then load table scene
            LoadScene(SCENE_TABLE);
        }
        
        /// <summary>
        /// Start a boss battle
        /// </summary>
        public void StartBossBattle(string bossId)
        {
            Debug.Log($"[GameManager] Starting boss battle: {bossId}");
            // TODO: Store boss ID, then load battle scene
            LoadScene(SCENE_BOSS_BATTLE);
        }
        
        #endregion
        
        #region Data Management
        
        /// <summary>
        /// Update player chips (local cache)
        /// </summary>
        public void UpdateChips(int newChips)
        {
            if (CurrentUser != null)
            {
                CurrentUser.chips = newChips;
            }
        }
        
        /// <summary>
        /// Refresh player data from server
        /// </summary>
        public void RefreshPlayerData(System.Action callback = null)
        {
            Debug.Log("[GameManager] Refreshing player data...");
            // TODO: Fetch from server
            callback?.Invoke();
        }
        
        #endregion
        
        #region Settings
        
        /// <summary>
        /// Save game settings
        /// </summary>
        public void SaveSettings()
        {
            // TODO: Save to PlayerPrefs or file
        }
        
        /// <summary>
        /// Load game settings
        /// </summary>
        public void LoadSettings()
        {
            // TODO: Load from PlayerPrefs or file
        }
        
        #endregion
    }
}




