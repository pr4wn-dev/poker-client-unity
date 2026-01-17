using System;
using System.Collections.Generic;
using UnityEngine;

namespace PokerClient.Networking
{
    /// <summary>
    /// High-level game service API. 
    /// Use this class for all server communication - it wraps SocketManager with clean interfaces.
    /// </summary>
    public class GameService : MonoBehaviour
    {
        private static GameService _instance;
        private static readonly object _lock = new object();
        private static bool _applicationIsQuitting = false;
        
        public static GameService Instance
        {
            get
            {
                if (_applicationIsQuitting)
                {
                    Debug.LogWarning("[GameService] Instance requested after application quit - returning null");
                    return null;
                }
                
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        // Try to find existing instance
                        _instance = FindObjectOfType<GameService>();
                        Debug.Log($"[GameService] NEW CODE v2 - FindObjectOfType returned: {_instance != null}");
                        
                        if (_instance == null)
                        {
                            // Create new instance
                            var go = new GameObject("GameService");
                            _instance = go.AddComponent<GameService>();
                            Debug.Log("[GameService] NEW CODE v2 - Created fresh instance");
                        }
                    }
                    return _instance;
                }
            }
        }
        
        public static bool HasInstance => _instance != null;
        
        private SocketManager _socket;
        private bool _isInitialized = false;
        
        // Current user state - Static so it survives scene changes
        private static UserProfile _currentUser;
        private static string _currentTableId;
        private static TableState _currentTableState;
        private static int _mySeatIndex = -1;
        
        public UserProfile CurrentUser { get => _currentUser; private set => _currentUser = value; }
        public bool IsLoggedIn => CurrentUser != null;
        
        // Current game state
        public string CurrentTableId { get => _currentTableId; private set => _currentTableId = value; }
        public TableState CurrentTableState { get => _currentTableState; private set => _currentTableState = value; }
        public int MySeatIndex { get => _mySeatIndex; private set => _mySeatIndex = value; }
        public bool IsInGame => !string.IsNullOrEmpty(CurrentTableId);
        
        // Events for UI to subscribe to
        public event Action<UserProfile> OnLoginSuccess;
        public event Action<string> OnLoginFailed;
        public event Action OnLogout;
        
        public event Action<List<TableInfo>> OnTablesReceived;
        public event Action<TableInfo> OnTableCreated;
        public event Action<TableState> OnTableJoined;
        public event Action OnTableLeft;
        public event Action<TableState> OnTableStateUpdate;
        public event Action<string, string, int?> OnPlayerActionReceived; // playerId, action, amount
        public event Action<string, string, int> OnPlayerJoinedTable; // playerId, name, seat
        public event Action<string> OnPlayerLeftTable; // playerId
        public event Action<HandResultData> OnHandComplete;
        public event Action<TableInviteData> OnInviteReceived;
        public event Action<string, string, string> OnChatMessageReceived; // playerId, username, message
        
        public event Action<WorldMapState> OnWorldMapReceived;
        public event Action<List<BossListItem>> OnBossesReceived;
        public event Action<AdventureSession> OnAdventureStarted;
        public event Action<AdventureResult> OnAdventureComplete;
        
        private void Awake()
        {
            // Singleton check
            if (_instance != null && _instance != this)
            {
                Debug.Log("[GameService] Duplicate instance destroyed");
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Initialize socket immediately in Awake
            InitializeSocket();
        }
        
        private void InitializeSocket()
        {
            if (_isInitialized) return;
            
            _socket = SocketManager.Instance;
            
            if (_socket != null)
            {
                // Subscribe to socket events
                _socket.OnTableState += HandleTableState;
                _socket.OnPlayerAction += HandlePlayerAction;
                _socket.OnPlayerJoined += HandlePlayerJoined;
                _socket.OnPlayerLeft += HandlePlayerLeft;
                _socket.OnHandResult += HandleHandResult;
                _socket.OnTableInvite += HandleTableInvite;
                _socket.OnChatMessage += HandleChatMessage;
                _socket.OnAdventureResult += HandleAdventureResult;
                _socket.OnWorldMapState += HandleWorldMapState;
                
                _isInitialized = true;
                Debug.Log("[GameService] Initialized successfully");
            }
            else
            {
                Debug.LogError("[GameService] Failed to get SocketManager instance!");
            }
        }
        
        private void OnApplicationQuit()
        {
            _applicationIsQuitting = true;
        }
        
        private void OnDestroy()
        {
            // Only clear instance if this is THE instance
            if (_instance == this)
            {
                if (_socket != null)
                {
                    _socket.OnTableState -= HandleTableState;
                    _socket.OnPlayerAction -= HandlePlayerAction;
                    _socket.OnPlayerJoined -= HandlePlayerJoined;
                    _socket.OnPlayerLeft -= HandlePlayerLeft;
                    _socket.OnHandResult -= HandleHandResult;
                    _socket.OnTableInvite -= HandleTableInvite;
                    _socket.OnChatMessage -= HandleChatMessage;
                    _socket.OnAdventureResult -= HandleAdventureResult;
                    _socket.OnWorldMapState -= HandleWorldMapState;
                }
                
                // Don't null out _instance - it might be needed during scene transitions
                // _instance = null;
            }
        }
        
        #region Authentication
        
        public void Connect(string serverUrl = null)
        {
            _socket.Connect(serverUrl);
        }
        
        public void Register(string username, string password, string email, Action<bool, string> callback)
        {
            _socket.Emit<RegisterResponse>("register", new { username, password, email }, response =>
            {
                if (response.success)
                {
                    CurrentUser = response.profile;
                    OnLoginSuccess?.Invoke(CurrentUser);
                    callback?.Invoke(true, null);
                }
                else
                {
                    OnLoginFailed?.Invoke(response.error);
                    callback?.Invoke(false, response.error);
                }
            });
        }
        
        public void Login(string username, string password, Action<bool, string> callback)
        {
            _socket.Emit<LoginResponse>("login", new { username, password }, response =>
            {
                if (response.success)
                {
                    CurrentUser = response.profile;
                    OnLoginSuccess?.Invoke(CurrentUser);
                    callback?.Invoke(true, null);
                }
                else
                {
                    OnLoginFailed?.Invoke(response.error);
                    callback?.Invoke(false, response.error);
                }
            });
        }
        
        public void Logout()
        {
            _socket.Emit("logout");
            CurrentUser = null;
            CurrentTableId = null;
            CurrentTableState = null;
            MySeatIndex = -1;
            OnLogout?.Invoke();
        }
        
        #endregion
        
        #region Lobby & Tables
        
        public void GetTables(Action<List<TableInfo>> callback = null)
        {
            _socket.Emit<GetTablesResponse>("get_tables", null, response =>
            {
                if (response.success)
                {
                    OnTablesReceived?.Invoke(response.tables);
                    callback?.Invoke(response.tables);
                }
            });
        }
        
        public void CreateTable(string name, int maxPlayers = 9, int smallBlind = 50, int bigBlind = 100, 
            bool isPrivate = false, string password = null, Action<bool, string> callback = null)
        {
            var data = new
            {
                name,
                maxPlayers,
                smallBlind,
                bigBlind,
                isPrivate,
                password
            };
            
            _socket.Emit<CreateTableResponse>("create_table", data, response =>
            {
                Debug.Log($"[GameService] CreateTable callback - response null: {response == null}, success: {response?.success}, tableId: {response?.tableId}");
                
                if (response != null && response.success)
                {
                    Debug.Log($"[GameService] Table created: {response.tableId}, now joining...");
                    OnTableCreated?.Invoke(response.table);
                    
                    // Auto-join the table we just created
                    JoinTable(response.tableId, 0, password, (joinSuccess, joinError) =>
                    {
                        if (joinSuccess)
                        {
                            Debug.Log($"[GameService] Auto-joined table {response.tableId}");
                            callback?.Invoke(true, response.tableId);
                        }
                        else
                        {
                            Debug.LogError($"[GameService] Failed to auto-join: {joinError}");
                            callback?.Invoke(false, joinError);
                        }
                    });
                }
                else
                {
                    Debug.LogError($"[GameService] CreateTable failed - response null: {response == null}, error: {response?.error}");
                    callback?.Invoke(false, response?.error ?? "Create table failed");
                }
            });
        }
        
        public void JoinTable(string tableId, int? preferredSeat = null, string password = null, 
            Action<bool, string> callback = null)
        {
            Debug.Log($"[GameService] JoinTable called for tableId: {tableId}");
            var data = new { tableId, seatIndex = preferredSeat, password };
            
            _socket.Emit<JoinTableResponse>("join_table", data, response =>
            {
                Debug.Log($"[GameService] JoinTable response: success={response?.success}, seatIndex={response?.seatIndex}");
                if (response != null && response.success)
                {
                    CurrentTableId = tableId;
                    MySeatIndex = response.seatIndex;
                    CurrentTableState = response.state;
                    Debug.Log($"[GameService] Joined! CurrentTableId={CurrentTableId}, IsInGame={IsInGame}");
                    OnTableJoined?.Invoke(response.state);
                    callback?.Invoke(true, null);
                }
                else
                {
                    Debug.LogError($"[GameService] JoinTable failed: {response?.error}");
                    callback?.Invoke(false, response?.error ?? "Join failed");
                }
            });
        }
        
        public void LeaveTable(Action<bool> callback = null)
        {
            _socket.Emit<SimpleResponse>("leave_table", null, response =>
            {
                if (response.success)
                {
                    CurrentTableId = null;
                    CurrentTableState = null;
                    MySeatIndex = -1;
                    OnTableLeft?.Invoke();
                }
                callback?.Invoke(response.success);
            });
        }
        
        /// <summary>
        /// Check if user has an active table session to reconnect to
        /// </summary>
        public void CheckActiveSession(Action<bool, string, string> callback)
        {
            _socket.Emit<ActiveSessionResponse>("check_active_session", new { }, response =>
            {
                if (response.success && response.hasActiveSession)
                {
                    callback?.Invoke(true, response.tableId, response.tableName);
                }
                else
                {
                    callback?.Invoke(false, null, null);
                }
            });
        }
        
        /// <summary>
        /// Reconnect to an existing table session after disconnect
        /// </summary>
        public void ReconnectToTable(string tableId = null, Action<bool, TableState, string> callback = null)
        {
            _socket.Emit<ReconnectResponse>("reconnect_to_table", new { tableId }, response =>
            {
                if (response.success)
                {
                    CurrentTableId = response.tableId;
                    CurrentTableState = response.state;
                    
                    // Find my seat
                    if (response.state?.seats != null && CurrentUser != null)
                    {
                        for (int i = 0; i < response.state.seats.Count; i++)
                        {
                            if (response.state.seats[i]?.playerId == CurrentUser.id)
                            {
                                MySeatIndex = i;
                                break;
                            }
                        }
                    }
                    
                    Debug.Log($"Reconnected to table: {response.tableName}");
                    OnTableJoined?.Invoke(response.state);
                    OnTableStateUpdate?.Invoke(response.state);
                }
                
                callback?.Invoke(response.success, response.state, response.error);
            });
        }
        
        // Events for reconnection
        public event Action<string, string> OnDisconnectedFromTable;  // tableId, reason
        public event Action<string> OnPlayerDisconnected;  // playerId
        public event Action<string> OnPlayerReconnected;   // playerId
        
        #endregion
        
        #region Sit Out
        
        private bool _isSittingOut = false;
        public bool IsSittingOut => _isSittingOut;
        
        /// <summary>
        /// Sit out from the current hand (skip next hands until back)
        /// </summary>
        public void SitOut(Action<bool, string> callback = null)
        {
            _socket.Emit<SimpleResponse>("sit_out", new { }, response =>
            {
                if (response.success)
                {
                    _isSittingOut = true;
                    OnSitOutChanged?.Invoke(true);
                }
                callback?.Invoke(response.success, response.error);
            });
        }
        
        /// <summary>
        /// Return to active play after sitting out
        /// </summary>
        public void SitBack(Action<bool, string> callback = null)
        {
            _socket.Emit<SimpleResponse>("sit_back", new { }, response =>
            {
                if (response.success)
                {
                    _isSittingOut = false;
                    OnSitOutChanged?.Invoke(false);
                }
                callback?.Invoke(response.success, response.error);
            });
        }
        
        /// <summary>
        /// Toggle sit out status
        /// </summary>
        public void ToggleSitOut(Action<bool, string> callback = null)
        {
            if (_isSittingOut)
                SitBack(callback);
            else
                SitOut(callback);
        }
        
        public event Action<bool> OnSitOutChanged;
        public event Action<string, bool> OnPlayerSitOutChanged;  // playerId, isSittingOut
        
        #endregion
        
        #region Invites
        
        public void InvitePlayer(string oderId, Action<bool, string> callback = null)
        {
            if (string.IsNullOrEmpty(CurrentTableId))
            {
                callback?.Invoke(false, "Not at a table");
                return;
            }
            
            _socket.Emit<SimpleResponse>("invite_to_table", new { oderId, tableId = CurrentTableId }, response =>
            {
                callback?.Invoke(response.success, response.error);
            });
        }
        
        public void SearchUsers(string query, Action<List<UserSearchResult>> callback)
        {
            _socket.Emit<SearchUsersResponse>("search_users", new { query }, response =>
            {
                callback?.Invoke(response.success ? response.users : new List<UserSearchResult>());
            });
        }
        
        /// <summary>
        /// Get friends list with online status
        /// </summary>
        public void GetFriends(Action<List<FriendInfo>> callback)
        {
            _socket.Emit<FriendsResponse>("get_friends", new { }, response =>
            {
                callback?.Invoke(response.success ? response.friends : new List<FriendInfo>());
            });
        }
        
        /// <summary>
        /// Send a friend request to another player
        /// </summary>
        public void SendFriendRequest(string userId, Action<bool, string> callback = null)
        {
            _socket.Emit<GenericResponse>("send_friend_request", new { targetUserId = userId }, response =>
            {
                callback?.Invoke(response.success, response.error);
            });
        }
        
        /// <summary>
        /// Accept a pending friend request
        /// </summary>
        public void AcceptFriendRequest(string userId, Action<bool, string> callback = null)
        {
            _socket.Emit<GenericResponse>("accept_friend_request", new { fromUserId = userId }, response =>
            {
                callback?.Invoke(response.success, response.error);
            });
        }
        
        /// <summary>
        /// Decline a pending friend request
        /// </summary>
        public void DeclineFriendRequest(string userId, Action<bool, string> callback = null)
        {
            _socket.Emit<GenericResponse>("decline_friend_request", new { fromUserId = userId }, response =>
            {
                callback?.Invoke(response.success, response.error);
            });
        }
        
        /// <summary>
        /// Remove a friend
        /// </summary>
        public void RemoveFriend(string userId, Action<bool, string> callback = null)
        {
            _socket.Emit<GenericResponse>("remove_friend", new { friendUserId = userId }, response =>
            {
                callback?.Invoke(response.success, response.error);
            });
        }
        
        /// <summary>
        /// Get pending friend requests
        /// </summary>
        public void GetFriendRequests(Action<List<FriendRequestInfo>> callback)
        {
            _socket.Emit<FriendRequestsResponse>("get_friend_requests", new { }, response =>
            {
                callback?.Invoke(response.success ? response.requests : new List<FriendRequestInfo>());
            });
        }
        
        // Events for friends
        public event Action<FriendInfo> OnFriendOnline;
        public event Action<string> OnFriendOffline;
        public event Action<FriendRequestInfo> OnFriendRequestReceived;
        
        #endregion
        
        #region Leaderboards
        
        /// <summary>
        /// Get leaderboard entries for a category
        /// </summary>
        public void GetLeaderboard(string category, Action<List<LeaderboardEntry>> callback)
        {
            _socket.Emit<LeaderboardResponse>("get_leaderboard", new { category }, response =>
            {
                callback?.Invoke(response.success ? response.entries : new List<LeaderboardEntry>());
            });
        }
        
        #endregion
        
        #region Daily Rewards
        
        /// <summary>
        /// Get current daily reward status
        /// </summary>
        public void GetDailyRewardStatus(Action<DailyRewardResponse> callback)
        {
            _socket.Emit<DailyRewardResponse>("get_daily_reward_status", new { }, response =>
            {
                callback?.Invoke(response);
            });
        }
        
        /// <summary>
        /// Claim today's daily reward
        /// </summary>
        public void ClaimDailyReward(Action<ClaimDailyRewardResponse> callback)
        {
            _socket.Emit<ClaimDailyRewardResponse>("claim_daily_reward", new { }, response =>
            {
                callback?.Invoke(response);
            });
        }
        
        #endregion
        
        #region Achievements
        
        /// <summary>
        /// Get all achievements and unlock status
        /// </summary>
        public void GetAchievements(Action<AchievementsResponse> callback)
        {
            _socket.Emit<AchievementsResponse>("get_achievements", new { }, response =>
            {
                callback?.Invoke(response);
            });
        }
        
        /// <summary>
        /// Unlock an achievement (called when conditions are met)
        /// </summary>
        public void UnlockAchievement(string achievementId, Action<bool, int> callback = null)
        {
            _socket.Emit<UnlockAchievementResponse>("unlock_achievement", new { achievementId }, response =>
            {
                callback?.Invoke(response.success, response.xpAwarded);
            });
        }
        
        public event Action<string, int> OnAchievementUnlocked;  // achievementId, xpAwarded
        
        #endregion
        
        #region Game Actions
        
        public void Fold()
        {
            SendAction("fold");
        }
        
        public void Check()
        {
            SendAction("check");
        }
        
        public void Call()
        {
            SendAction("call");
        }
        
        public void Bet(int amount)
        {
            SendAction("bet", amount);
        }
        
        public void Raise(int amount)
        {
            SendAction("raise", amount);
        }
        
        public void AllIn()
        {
            SendAction("allin");
        }
        
        /// <summary>
        /// Request a rebuy (add chips to your stack at the table)
        /// </summary>
        public void Rebuy(int amount, System.Action<bool, int, int, string> callback = null)
        {
            _socket.Emit<RebuyResponse>("rebuy", new { amount }, response =>
            {
                if (response.success)
                {
                    Debug.Log($"Rebuy successful: +{amount} chips, new stack: {response.newTableStack}");
                }
                else
                {
                    Debug.LogError($"Rebuy failed: {response.error}");
                }
                callback?.Invoke(response.success, response.newTableStack, response.accountBalance, response.error);
            });
        }
        
        private void SendAction(string action, int? amount = null)
        {
            var data = amount.HasValue 
                ? new { action, amount = amount.Value }
                : (object)new { action };
            
            _socket.Emit<ActionResponse>("action", data, response =>
            {
                if (!response.success)
                {
                    Debug.LogError($"Action failed: {response.error}");
                }
            });
        }
        
        public void SendChat(string message)
        {
            _socket.Emit("chat", new { message });
        }
        
        #endregion
        
        #region Adventure Mode
        
        public void GetWorldMap(Action<WorldMapState> callback = null)
        {
            _socket.Emit<GetWorldMapResponse>("get_world_map", null, response =>
            {
                if (response.success)
                {
                    OnWorldMapReceived?.Invoke(response.mapState);
                    callback?.Invoke(response.mapState);
                }
            });
        }
        
        public void GetAreaBosses(string areaId, Action<List<BossListItem>> callback = null)
        {
            _socket.Emit<GetBossesResponse>("get_area_bosses", new { areaId }, response =>
            {
                if (response.success)
                {
                    OnBossesReceived?.Invoke(response.bosses);
                    callback?.Invoke(response.bosses);
                }
            });
        }
        
        public void StartAdventure(string bossId, Action<bool, string, AdventureSession> callback = null)
        {
            _socket.Emit<StartAdventureResponse>("start_adventure", new { bossId }, response =>
            {
                if (response.success)
                {
                    OnAdventureStarted?.Invoke(response.session);
                    callback?.Invoke(true, null, response.session);
                }
                else
                {
                    callback?.Invoke(false, response.error, null);
                }
            });
        }
        
        public void ForfeitAdventure(Action<bool> callback = null)
        {
            _socket.Emit<SimpleResponse>("forfeit_adventure", null, response =>
            {
                callback?.Invoke(response.success);
            });
        }
        
        /// <summary>
        /// Send a poker action in adventure mode
        /// </summary>
        public void SendAdventureAction(string action, int amount = 0, 
            System.Action<AdventureActionResponse> callback = null)
        {
            _socket.Emit<AdventureActionResponse>("adventure_action", new { action, amount }, response =>
            {
                if (response.success)
                {
                    // Check for boss actions
                    if (response.bossActions != null)
                    {
                        foreach (var bossAction in response.bossActions)
                        {
                            OnBossAction?.Invoke(bossAction);
                        }
                    }
                    
                    // Update hand state
                    if (response.state != null)
                    {
                        OnAdventureHandUpdate?.Invoke(response.state);
                    }
                    
                    // Check for hand complete
                    if (response.handComplete)
                    {
                        OnAdventureHandComplete?.Invoke(response);
                    }
                    
                    // Check for game end
                    if (response.status == "victory" || response.status == "defeat")
                    {
                        OnAdventureGameEnd?.Invoke(response);
                    }
                }
                else
                {
                    Debug.LogError($"Adventure action failed: {response.error}");
                }
                
                callback?.Invoke(response);
            });
        }
        
        /// <summary>
        /// Request next hand in adventure mode
        /// </summary>
        public void RequestNextAdventureHand(System.Action<AdventureHandState> callback = null)
        {
            _socket.Emit<AdventureNextHandResponse>("adventure_next_hand", new { }, response =>
            {
                if (response.success && response.hand != null)
                {
                    OnAdventureHandUpdate?.Invoke(response.hand);
                }
                callback?.Invoke(response.hand);
            });
        }
        
        // Adventure events
        public event System.Action<BossActionInfo> OnBossAction;
        public event System.Action<AdventureHandState> OnAdventureHandUpdate;
        public event System.Action<AdventureActionResponse> OnAdventureHandComplete;
        public event System.Action<AdventureActionResponse> OnAdventureGameEnd;
        
        #endregion
        
        #region Event Handlers
        
        private void HandleTableState(TableState state)
        {
            CurrentTableState = state;
            OnTableStateUpdate?.Invoke(state);
        }
        
        private void HandlePlayerAction(PlayerActionData data)
        {
            OnPlayerActionReceived?.Invoke(data.playerId, data.action, data.amount);
        }
        
        private void HandlePlayerJoined(PlayerJoinedData data)
        {
            OnPlayerJoinedTable?.Invoke(data.playerId, data.name, data.seatIndex);
        }
        
        private void HandlePlayerLeft(string oderId)
        {
            OnPlayerLeftTable?.Invoke(oderId);
        }
        
        private void HandleHandResult(HandResultData data)
        {
            OnHandComplete?.Invoke(data);
        }
        
        private void HandleTableInvite(TableInviteData data)
        {
            OnInviteReceived?.Invoke(data);
        }
        
        private void HandleChatMessage(ChatMessageData data)
        {
            OnChatMessageReceived?.Invoke(data.playerId, data.name, data.message);
        }
        
        private void HandleAdventureResult(AdventureResult result)
        {
            OnAdventureComplete?.Invoke(result);
        }
        
        private void HandleWorldMapState(WorldMapState state)
        {
            OnWorldMapReceived?.Invoke(state);
        }
        
        #endregion
    }
}


