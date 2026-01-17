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
        public static GameService Instance { get; private set; }
        
        private SocketManager _socket;
        
        // Current user state
        public UserProfile CurrentUser { get; private set; }
        public bool IsLoggedIn => CurrentUser != null;
        
        // Current game state
        public string CurrentTableId { get; private set; }
        public TableState CurrentTableState { get; private set; }
        public int MySeatIndex { get; private set; } = -1;
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
        
        public event Action<WorldMapState> OnWorldMapReceived;
        public event Action<List<BossListItem>> OnBossesReceived;
        public event Action<AdventureSession> OnAdventureStarted;
        public event Action<AdventureResult> OnAdventureComplete;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        private void Start()
        {
            _socket = SocketManager.Instance;
            if (_socket == null)
            {
                var socketObj = new GameObject("SocketManager");
                _socket = socketObj.AddComponent<SocketManager>();
            }
            
            // Subscribe to socket events
            _socket.OnTableState += HandleTableState;
            _socket.OnPlayerAction += HandlePlayerAction;
            _socket.OnPlayerJoined += HandlePlayerJoined;
            _socket.OnPlayerLeft += HandlePlayerLeft;
            _socket.OnHandResult += HandleHandResult;
            _socket.OnTableInvite += HandleTableInvite;
            _socket.OnAdventureResult += HandleAdventureResult;
            _socket.OnWorldMapState += HandleWorldMapState;
        }
        
        private void OnDestroy()
        {
            if (_socket != null)
            {
                _socket.OnTableState -= HandleTableState;
                _socket.OnPlayerAction -= HandlePlayerAction;
                _socket.OnPlayerJoined -= HandlePlayerJoined;
                _socket.OnPlayerLeft -= HandlePlayerLeft;
                _socket.OnHandResult -= HandleHandResult;
                _socket.OnTableInvite -= HandleTableInvite;
                _socket.OnAdventureResult -= HandleAdventureResult;
                _socket.OnWorldMapState -= HandleWorldMapState;
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
                if (response.success)
                {
                    OnTableCreated?.Invoke(response.table);
                    callback?.Invoke(true, response.tableId);
                }
                else
                {
                    callback?.Invoke(false, response.error);
                }
            });
        }
        
        public void JoinTable(string tableId, int? preferredSeat = null, string password = null, 
            Action<bool, string> callback = null)
        {
            var data = new { tableId, seatIndex = preferredSeat, password };
            
            _socket.Emit<JoinTableResponse>("join_table", data, response =>
            {
                if (response.success)
                {
                    CurrentTableId = tableId;
                    MySeatIndex = response.seatIndex;
                    CurrentTableState = response.state;
                    OnTableJoined?.Invoke(response.state);
                    callback?.Invoke(true, null);
                }
                else
                {
                    callback?.Invoke(false, response.error);
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
        
        public void SendAdventureAction(string action, int? amount = null)
        {
            var handResult = new { winner = "pending", action, amount };
            _socket.Emit("adventure_action", new { handResult });
        }
        
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
    
    #region Response Classes
    
    [Serializable]
    public class SimpleResponse
    {
        public bool success;
        public string error;
    }
    
    [Serializable]
    public class RegisterResponse
    {
        public bool success;
        public string error;
        public string oderId;
        public UserProfile profile;
    }
    
    [Serializable]
    public class LoginResponse
    {
        public bool success;
        public string error;
        public string oderId;
        public UserProfile profile;
    }
    
    [Serializable]
    public class GetTablesResponse
    {
        public bool success;
        public List<TableInfo> tables;
    }
    
    [Serializable]
    public class CreateTableResponse
    {
        public bool success;
        public string error;
        public string tableId;
        public TableInfo table;
    }
    
    [Serializable]
    public class JoinTableResponse
    {
        public bool success;
        public string error;
        public int seatIndex;
        public TableState state;
    }
    
    [Serializable]
    public class ActionResponse
    {
        public bool success;
        public string error;
        public string action;
        public int? amount;
    }
    
    [Serializable]
    public class SearchUsersResponse
    {
        public bool success;
        public List<UserSearchResult> users;
    }
    
    [Serializable]
    public class UserSearchResult
    {
        public string id;
        public string username;
        public bool isOnline;
    }
    
    [Serializable]
    public class GetWorldMapResponse
    {
        public bool success;
        public WorldMapState mapState;
    }
    
    [Serializable]
    public class GetBossesResponse
    {
        public bool success;
        public string areaId;
        public List<BossListItem> bosses;
    }
    
    [Serializable]
    public class StartAdventureResponse
    {
        public bool success;
        public string error;
        public AdventureSession session;
    }
    
    #endregion
}


