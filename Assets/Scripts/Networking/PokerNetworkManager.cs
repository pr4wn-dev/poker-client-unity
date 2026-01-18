using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

// NOTE: You need to install SocketIOClient package
// https://github.com/doghappy/socket.io-client-csharp
// Install via NuGet: dotnet add package SocketIOClient
// Or add to your project via Unity Package Manager

#if SOCKETIO_INSTALLED
using SocketIOClient;
#endif

namespace PokerClient.Networking
{
    /// <summary>
    /// Manages WebSocket connection to the poker server
    /// Singleton pattern for global access
    /// </summary>
    public class PokerNetworkManager : MonoBehaviour
    {
        #region Singleton
        
        private static PokerNetworkManager _instance;
        public static PokerNetworkManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("PokerNetworkManager");
                    _instance = go.AddComponent<PokerNetworkManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }
        
        #endregion
        
        #region Configuration
        
        [Header("Server Configuration")]
        [SerializeField] private string serverUrl = "http://localhost:3000";
        
        #endregion
        
        #region Events
        
        // Connection events
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string> OnError;
        
        // Lobby events
        public event Action<TableInfo> OnTableCreated;
        
        // Table events
        public event Action<PlayerJoinedEvent> OnPlayerJoined;
        public event Action<PlayerLeftEvent> OnPlayerLeft;
        public event Action<string> OnPlayerDisconnected;
        public event Action<PlayerActionEvent> OnPlayerAction;
        public event Action<TableState> OnTableStateUpdated;
        public event Action<ChatMessage> OnChatReceived;
        
        #endregion
        
        #region State
        
        public string PlayerId { get; private set; }
        public string CurrentTableId { get; private set; }
        public bool IsConnected { get; private set; }
        
        private TableState _currentTableState;
        public TableState CurrentTableState => _currentTableState;
        
        #endregion
        
#if SOCKETIO_INSTALLED
        private SocketIOUnity _socket;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        #region Connection
        
        public async Task<bool> ConnectAsync(string url = null)
        {
            if (url != null) serverUrl = url;
            
            try
            {
                var uri = new Uri(serverUrl);
                _socket = new SocketIOUnity(uri);
                
                SetupEventListeners();
                
                await _socket.ConnectAsync();
                IsConnected = true;
                
                Debug.Log($"[PokerNetwork] Connected to {serverUrl}");
                OnConnected?.Invoke();
                
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[PokerNetwork] Connection failed: {ex.Message}");
                OnError?.Invoke(ex.Message);
                return false;
            }
        }
        
        public async Task DisconnectAsync()
        {
            if (_socket != null)
            {
                await _socket.DisconnectAsync();
                _socket.Dispose();
                _socket = null;
            }
            IsConnected = false;
            PlayerId = null;
            CurrentTableId = null;
            OnDisconnected?.Invoke();
        }
        
        private void SetupEventListeners()
        {
            _socket.OnConnected += (sender, e) =>
            {
                UnityMainThreadDispatcher.Enqueue(() => OnConnected?.Invoke());
            };
            
            _socket.OnDisconnected += (sender, e) =>
            {
                IsConnected = false;
                UnityMainThreadDispatcher.Enqueue(() => OnDisconnected?.Invoke());
            };
            
            _socket.OnError += (sender, e) =>
            {
                UnityMainThreadDispatcher.Enqueue(() => OnError?.Invoke(e));
            };
            
            // Table events
            _socket.On(PokerEvents.TableCreated, response =>
            {
                var data = response.GetValue<TableInfo>();
                UnityMainThreadDispatcher.Enqueue(() => OnTableCreated?.Invoke(data));
            });
            
            _socket.On(PokerEvents.PlayerJoined, response =>
            {
                var data = response.GetValue<PlayerJoinedEvent>();
                UnityMainThreadDispatcher.Enqueue(() => OnPlayerJoined?.Invoke(data));
            });
            
            _socket.On(PokerEvents.PlayerLeft, response =>
            {
                var data = response.GetValue<PlayerLeftEvent>();
                UnityMainThreadDispatcher.Enqueue(() => OnPlayerLeft?.Invoke(data));
            });
            
            _socket.On(PokerEvents.PlayerDisconnected, response =>
            {
                var data = response.GetValue<PlayerLeftEvent>();
                UnityMainThreadDispatcher.Enqueue(() => OnPlayerDisconnected?.Invoke(data.playerId));
            });
            
            _socket.On(PokerEvents.PlayerAction, response =>
            {
                var data = response.GetValue<PlayerActionEvent>();
                UnityMainThreadDispatcher.Enqueue(() => OnPlayerAction?.Invoke(data));
            });
            
            _socket.On(PokerEvents.TableState, response =>
            {
                var data = response.GetValue<TableState>();
                _currentTableState = data;
                UnityMainThreadDispatcher.Enqueue(() => OnTableStateUpdated?.Invoke(data));
            });
            
            _socket.On(PokerEvents.ChatMessage, response =>
            {
                var data = response.GetValue<ChatMessage>();
                UnityMainThreadDispatcher.Enqueue(() => OnChatReceived?.Invoke(data));
            });
        }
        
        #endregion
        
        #region API Methods
        
        public Task<RegisterResponse> RegisterAsync(string playerName)
        {
            var tcs = new TaskCompletionSource<RegisterResponse>();
            var request = new RegisterRequest { playerName = playerName };
            
            _socket.Emit(PokerEvents.Register, response => 
            {
                try
                {
                    var result = response.GetValue<RegisterResponse>();
                    if (result.success)
                    {
                        PlayerId = result.playerId;
                        Debug.Log($"[PokerNetwork] Registered as {playerName} ({PlayerId})");
                    }
                    UnityMainThreadDispatcher.Enqueue(() => tcs.SetResult(result));
                }
                catch (Exception e)
                {
                    Debug.LogError($"[PokerNetwork] Register error: {e.Message}");
                    UnityMainThreadDispatcher.Enqueue(() => tcs.SetResult(new RegisterResponse { success = false, error = e.Message }));
                }
            }, request);
            
            return tcs.Task;
        }
        
        public Task<List<TableInfo>> GetTablesAsync()
        {
            var tcs = new TaskCompletionSource<List<TableInfo>>();
            
            _socket.Emit(PokerEvents.GetTables, response =>
            {
                try
                {
                    var result = response.GetValue<TablesResponse>();
                    UnityMainThreadDispatcher.Enqueue(() => tcs.SetResult(result.tables ?? new List<TableInfo>()));
                }
                catch (Exception e)
                {
                    Debug.LogError($"[PokerNetwork] GetTables error: {e.Message}");
                    UnityMainThreadDispatcher.Enqueue(() => tcs.SetResult(new List<TableInfo>()));
                }
            });
            
            return tcs.Task;
        }
        
        public Task<CreateTableResponse> CreateTableAsync(string name, int maxPlayers = 9, 
            int smallBlind = 50, int bigBlind = 100, bool isPrivate = false)
        {
            var tcs = new TaskCompletionSource<CreateTableResponse>();
            var request = new CreateTableRequest
            {
                name = name,
                maxPlayers = maxPlayers,
                smallBlind = smallBlind,
                bigBlind = bigBlind,
                isPrivate = isPrivate
            };
            
            _socket.Emit(PokerEvents.CreateTable, response =>
            {
                try
                {
                    var result = response.GetValue<CreateTableResponse>();
                    UnityMainThreadDispatcher.Enqueue(() => tcs.SetResult(result));
                }
                catch (Exception e)
                {
                    Debug.LogError($"[PokerNetwork] CreateTable error: {e.Message}");
                    UnityMainThreadDispatcher.Enqueue(() => tcs.SetResult(new CreateTableResponse { success = false, error = e.Message }));
                }
            }, request);
            
            return tcs.Task;
        }
        
        public Task<JoinTableResponse> JoinTableAsync(string tableId, int? seatIndex = null)
        {
            var tcs = new TaskCompletionSource<JoinTableResponse>();
            var request = new JoinTableRequest { tableId = tableId, seatIndex = seatIndex };
            
            _socket.Emit(PokerEvents.JoinTable, response =>
            {
                try
                {
                    var result = response.GetValue<JoinTableResponse>();
                    if (result.success)
                    {
                        CurrentTableId = tableId;
                        _currentTableState = result.state;
                        Debug.Log($"[PokerNetwork] Joined table {tableId} at seat {result.seatIndex}");
                    }
                    UnityMainThreadDispatcher.Enqueue(() => tcs.SetResult(result));
                }
                catch (Exception e)
                {
                    Debug.LogError($"[PokerNetwork] JoinTable error: {e.Message}");
                    UnityMainThreadDispatcher.Enqueue(() => tcs.SetResult(new JoinTableResponse { success = false, error = e.Message }));
                }
            }, request);
            
            return tcs.Task;
        }
        
        public Task<ActionResponse> LeaveTableAsync()
        {
            var tcs = new TaskCompletionSource<ActionResponse>();
            
            _socket.Emit(PokerEvents.LeaveTable, response =>
            {
                try
                {
                    var result = response.GetValue<ActionResponse>();
                    if (result.success)
                    {
                        CurrentTableId = null;
                        _currentTableState = null;
                    }
                    UnityMainThreadDispatcher.Enqueue(() => tcs.SetResult(result));
                }
                catch (Exception e)
                {
                    Debug.LogError($"[PokerNetwork] LeaveTable error: {e.Message}");
                    UnityMainThreadDispatcher.Enqueue(() => tcs.SetResult(new ActionResponse { success = false, error = e.Message }));
                }
            });
            
            return tcs.Task;
        }
        
        public Task<ActionResponse> SendActionAsync(PokerAction action, int amount = 0)
        {
            var tcs = new TaskCompletionSource<ActionResponse>();
            var request = new ActionRequest
            {
                action = ActionStrings.FromAction(action),
                amount = amount
            };
            
            _socket.Emit(PokerEvents.Action, response =>
            {
                try
                {
                    var result = response.GetValue<ActionResponse>();
                    UnityMainThreadDispatcher.Enqueue(() => tcs.SetResult(result));
                }
                catch (Exception e)
                {
                    Debug.LogError($"[PokerNetwork] Action error: {e.Message}");
                    UnityMainThreadDispatcher.Enqueue(() => tcs.SetResult(new ActionResponse { success = false, error = e.Message }));
                }
            }, request);
            
            return tcs.Task;
        }
        
        public async Task SendChatAsync(string message)
        {
            await _socket.EmitAsync(PokerEvents.Chat, new { message });
        }
        
        #endregion
        
        #region Helper Methods
        
        public SeatInfo GetMySeaxt()
        {
            return _currentTableState?.FindPlayer(PlayerId);
        }
        
        public bool IsMyTurn()
        {
            if (_currentTableState == null || string.IsNullOrEmpty(PlayerId))
                return false;
                
            var currentPlayer = _currentTableState.GetCurrentPlayer();
            return currentPlayer?.playerId == PlayerId;
        }
        
        public int GetCallAmount()
        {
            var mySeat = GetMySeaxt();
            if (mySeat == null || _currentTableState == null)
                return 0;
            return _currentTableState.currentBet - mySeat.currentBet;
        }
        
        #endregion
        
        private void OnDestroy()
        {
            DisconnectAsync().ConfigureAwait(false);
        }
#else
        // Placeholder implementation when SocketIO is not installed
        private void Awake()
        {
            Debug.LogWarning("[PokerNetwork] SocketIOClient package not installed! " +
                "Install from: https://github.com/doghappy/socket.io-client-csharp");
        }
        
        public Task<bool> ConnectAsync(string url = null)
        {
            Debug.LogError("SocketIOClient not installed");
            return Task.FromResult(false);
        }
        
        public Task<ActionResponse> SendActionAsync(PokerAction action, int amount = 0)
        {
            return Task.FromResult(new ActionResponse { success = false, error = "Not connected" });
        }
        
        public bool IsMyTurn()
        {
            return false;
        }
        
        public SeatInfo GetMySeat()
        {
            return null;
        }
        
        public int GetCallAmount()
        {
            return 0;
        }
#endif
    }
    
    /// <summary>
    /// Helper to dispatch actions to Unity main thread
    /// </summary>
    public class UnityMainThreadDispatcher : MonoBehaviour
    {
        private static UnityMainThreadDispatcher _instance;
        private static readonly Queue<Action> _queue = new Queue<Action>();
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (_instance == null)
            {
                var go = new GameObject("UnityMainThreadDispatcher");
                _instance = go.AddComponent<UnityMainThreadDispatcher>();
                DontDestroyOnLoad(go);
            }
        }
        
        public static void Enqueue(Action action)
        {
            lock (_queue)
            {
                _queue.Enqueue(action);
            }
        }
        
        private void Update()
        {
            lock (_queue)
            {
                while (_queue.Count > 0)
                {
                    _queue.Dequeue()?.Invoke();
                }
            }
        }
    }
}






