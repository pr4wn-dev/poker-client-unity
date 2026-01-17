using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#if SOCKET_IO_AVAILABLE
using SIO = global::SocketIOUnity;
#endif

namespace PokerClient.Networking
{
    /// <summary>
    /// Socket.IO Manager - Handles WebSocket connection to the poker server.
    /// 
    /// IMPORTANT: This requires the SocketIOUnity package:
    /// Install via: https://github.com/itisnajim/SocketIOUnity
    /// Or use NuGet: Install-Package SocketIOClient
    /// 
    /// If you don't have Socket.IO installed yet, this will use a mock implementation
    /// that simulates server responses for testing.
    /// </summary>
    public class SocketManager : MonoBehaviour
    {
        private static SocketManager _instance;
        public static SocketManager Instance 
        { 
            get 
            {
                if (_instance == null)
                {
                    _instance = FindAnyObjectByType<SocketManager>(FindObjectsInactive.Include);
                    if (_instance == null)
                    {
                        Debug.Log("[SocketManager] Creating new instance on demand");
                        var go = new GameObject("SocketManager");
                        _instance = go.AddComponent<SocketManager>();
                        DontDestroyOnLoad(go);
                        // Auto-connect
                        _instance.Connect();
                    }
                }
                return _instance;
            }
        }
        
        [Header("Server Configuration")]
        [SerializeField] private string serverUrl = "http://localhost:3000";
        [SerializeField] private bool autoConnect = true;
        [SerializeField] private bool useMockMode = false; // REAL SERVER CONNECTION
        
        [Header("Status")]
        [SerializeField] private bool isConnected = false;
        [SerializeField] private string connectionStatus = "Disconnected";
        
        public bool IsConnected => isConnected;
        public string ConnectionStatus => connectionStatus;
        
        // Events
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string> OnError;
        
        // Game events
        public event Action<TableState> OnTableState;
        public event Action<PlayerActionData> OnPlayerAction;
        public event Action<PlayerJoinedData> OnPlayerJoined;
        public event Action<string> OnPlayerLeft;
        public event Action<ChatMessageData> OnChatMessage;
        public event Action<HandResultData> OnHandResult;
        public event Action<TableInviteData> OnTableInvite;
        
        // Adventure events
        public event Action<AdventureResult> OnAdventureResult;
        public event Action<WorldMapState> OnWorldMapState;
        
        // Callback storage
        private Dictionary<string, Action<JObject>> _pendingCallbacks = new Dictionary<string, Action<JObject>>();
        private int _callbackId = 0;
        
        #if SOCKET_IO_AVAILABLE
        private SIO _socket;
        #endif
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log($"[SocketManager] Instance set: {GetInstanceID()}");
        }
        
        private void OnDestroy()
        {
            if (_instance == this)
            {
                Debug.Log("[SocketManager] Instance being destroyed, clearing reference");
                _instance = null;
            }
            Disconnect();
        }
        
        private void Start()
        {
            if (autoConnect)
            {
                Connect();
            }
        }
        
        #region Connection
        
        public void Connect(string url = null)
        {
            if (!string.IsNullOrEmpty(url))
                serverUrl = url;
            
            if (useMockMode)
            {
                Debug.Log("[SocketManager] Running in MOCK MODE - no server connection");
                isConnected = true;
                connectionStatus = "Connected (Mock)";
                OnConnected?.Invoke();
                return;
            }
            
            #if SOCKET_IO_AVAILABLE
            ConnectSocketIO();
            #else
            Debug.LogWarning("[SocketManager] Socket.IO native not available. Using fallback connection.");
            // Still try to connect - the mock handlers will be used if needed
            isConnected = true;
            connectionStatus = "Connected";
            OnConnected?.Invoke();
            #endif
        }
        
        #if SOCKET_IO_AVAILABLE
        private async void ConnectSocketIO()
        {
            try
            {
                connectionStatus = "Connecting...";
                
                var uri = new Uri(serverUrl);
                _socket = new SIO(uri, new SocketIOClient.SocketIOOptions
                {
                    Transport = SocketIOClient.Transport.TransportProtocol.WebSocket
                });
                
                _socket.OnConnected += (sender, e) =>
                {
                    Debug.Log("[SocketManager] Connected to server");
                    isConnected = true;
                    connectionStatus = "Connected";
                    UnityMainThread.Execute(() => OnConnected?.Invoke());
                };
                
                _socket.OnDisconnected += (sender, e) =>
                {
                    Debug.Log("[SocketManager] Disconnected from server");
                    isConnected = false;
                    connectionStatus = "Disconnected";
                    UnityMainThread.Execute(() => OnDisconnected?.Invoke());
                };
                
                _socket.OnError += (sender, e) =>
                {
                    Debug.LogError($"[SocketManager] Error: {e}");
                    UnityMainThread.Execute(() => OnError?.Invoke(e));
                };
                
                // Register event listeners
                RegisterEventListeners();
                
                await _socket.ConnectAsync();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SocketManager] Connection failed: {e.Message}");
                connectionStatus = "Connection Failed";
                OnError?.Invoke(e.Message);
            }
        }
        
        private void RegisterEventListeners()
        {
            // Table events
            _socket.On("table_state", response =>
            {
                var state = response.GetValue<TableState>();
                UnityMainThread.Execute(() => OnTableState?.Invoke(state));
            });
            
            _socket.On("player_action", response =>
            {
                var data = response.GetValue<PlayerActionData>();
                UnityMainThread.Execute(() => OnPlayerAction?.Invoke(data));
            });
            
            _socket.On("player_joined", response =>
            {
                var data = response.GetValue<PlayerJoinedData>();
                UnityMainThread.Execute(() => OnPlayerJoined?.Invoke(data));
            });
            
            _socket.On("player_left", response =>
            {
                var data = response.GetValue<string>();
                UnityMainThread.Execute(() => OnPlayerLeft?.Invoke(data));
            });
            
            _socket.On("chat", response =>
            {
                var data = response.GetValue<ChatMessageData>();
                UnityMainThread.Execute(() => OnChatMessage?.Invoke(data));
            });
            
            _socket.On("hand_result", response =>
            {
                var data = response.GetValue<HandResultData>();
                UnityMainThread.Execute(() => OnHandResult?.Invoke(data));
            });
            
            _socket.On("table_invite_received", response =>
            {
                var data = response.GetValue<TableInviteData>();
                UnityMainThread.Execute(() => OnTableInvite?.Invoke(data));
            });
            
            // Adventure events
            _socket.On("adventure_result", response =>
            {
                var data = response.GetValue<AdventureResult>();
                UnityMainThread.Execute(() => OnAdventureResult?.Invoke(data));
            });
            
            _socket.On("world_map_state", response =>
            {
                var data = response.GetValue<WorldMapState>();
                UnityMainThread.Execute(() => OnWorldMapState?.Invoke(data));
            });
        }
        #endif
        
        public void Disconnect()
        {
            #if SOCKET_IO_AVAILABLE
            _socket?.DisconnectAsync();
            #endif
            
            isConnected = false;
            connectionStatus = "Disconnected";
        }
        
        #endregion
        
        #region Emit Methods
        
        /// <summary>
        /// Send an event to the server with callback using response events
        /// </summary>
        public void Emit<T>(string eventName, object data, Action<T> callback) where T : class
        {
            if (useMockMode)
            {
                HandleMockRequest(eventName, data, callback);
                return;
            }
            
            #if SOCKET_IO_AVAILABLE
            // Make sure we're connected
            if (_socket == null)
            {
                Debug.LogWarning($"[SocketManager] Not connected, trying to connect before emit: {eventName}");
                Connect();
                // Wait a bit for connection - for now just log error
                if (_socket == null)
                {
                    Debug.LogError($"[SocketManager] Still not connected, cannot emit: {eventName}");
                    callback?.Invoke(null);
                    return;
                }
            }
            
            Debug.Log($"[SocketManager] Emitting: {eventName}");
            
            // Listen for response event
            string responseEvent = eventName + "_response";
            Action<SocketIOClient.SocketIOResponse> handler = null;
            handler = (response) =>
            {
                Debug.Log($"[SocketManager] === GOT RESPONSE: {responseEvent} ===");
                _socket.Off(responseEvent);
                
                try
                {
                    // Get JSON string from the response object
                    var obj = response.GetValue<object>();
                    string jsonStr = obj.ToString();
                    Debug.Log($"[SocketManager] JSON: {jsonStr}");
                    
                    // Use Unity's JsonUtility to deserialize
                    var result = JsonUtility.FromJson<T>(jsonStr);
                    Debug.Log($"[SocketManager] Parsed successfully!");
                    
                    UnityMainThread.Execute(() => callback?.Invoke(result));
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[SocketManager] Parse error: {ex.Message}");
                    UnityMainThread.Execute(() => callback?.Invoke(null));
                }
            };
            _socket.On(responseEvent, handler);
            
            // Emit the request
            _socket.Emit(eventName, data);
            #endif
        }
        
        /// <summary>
        /// Send an event without callback
        /// </summary>
        public void Emit(string eventName, object data = null)
        {
            if (useMockMode)
            {
                Debug.Log($"[SocketManager] Mock emit: {eventName}");
                return;
            }
            
            #if SOCKET_IO_AVAILABLE
            if (data != null)
                _socket?.EmitAsync(eventName, data);
            else
                _socket?.EmitAsync(eventName);
            #endif
        }
        
        #endregion
        
        #region Mock Server (For Testing Without Real Server)
        
        private void HandleMockRequest<T>(string eventName, object data, Action<T> callback) where T : class
        {
            Debug.Log($"[Mock] Handling: {eventName}");
            
            // Simulate network delay
            StartCoroutine(MockDelay(() =>
            {
                object response = eventName switch
                {
                    "register" => MockRegister(data),
                    "login" => MockLogin(data),
                    "get_tables" => MockGetTables(),
                    "create_table" => MockCreateTable(data),
                    "join_table" => MockJoinTable(data),
                    "get_world_map" => MockGetWorldMap(),
                    "get_area_bosses" => MockGetAreaBosses(data),
                    "start_adventure" => MockStartAdventure(data),
                    _ => new { success = true }
                };
                
                var json = JsonConvert.SerializeObject(response);
                var result = JsonConvert.DeserializeObject<T>(json);
                callback?.Invoke(result);
            }));
        }
        
        private System.Collections.IEnumerator MockDelay(Action action)
        {
            yield return new WaitForSeconds(0.3f);
            action?.Invoke();
        }
        
        private object MockRegister(object data)
        {
            var response = new RegisterResponse
            {
                success = true,
                playerId = Guid.NewGuid().ToString(),
                profile = new UserProfile
                {
                    id = Guid.NewGuid().ToString(),
                    username = "Player",
                    chips = 10000
                }
            };
            return response;
        }
        
        private object MockLogin(object data)
        {
            var response = new LoginResponse
            {
                success = true,
                userId = Guid.NewGuid().ToString(),
                profile = new UserProfile
                {
                    id = Guid.NewGuid().ToString(),
                    username = "Player",
                    chips = 10000
                }
            };
            return response;
        }
        
        private object MockGetTables()
        {
            return new
            {
                success = true,
                tables = new[]
                {
                    new { id = "table1", name = "Beginner's Luck", playerCount = 3, maxPlayers = 9, smallBlind = 50, bigBlind = 100 },
                    new { id = "table2", name = "High Rollers", playerCount = 5, maxPlayers = 6, smallBlind = 100, bigBlind = 200 },
                    new { id = "table3", name = "Pro League", playerCount = 2, maxPlayers = 9, smallBlind = 500, bigBlind = 1000 }
                }
            };
        }
        
        private object MockCreateTable(object data)
        {
            return new
            {
                success = true,
                tableId = Guid.NewGuid().ToString(),
                table = new
                {
                    id = Guid.NewGuid().ToString(),
                    name = "My Table",
                    playerCount = 1,
                    maxPlayers = 9
                }
            };
        }
        
        private object MockJoinTable(object data)
        {
            return new
            {
                success = true,
                seatIndex = 0,
                state = CreateMockTableState()
            };
        }
        
        private object MockGetWorldMap()
        {
            return new
            {
                success = true,
                mapState = new
                {
                    playerLevel = 3,
                    playerXP = 500,
                    xpProgress = 50,
                    xpForNextLevel = 1000,
                    maxLevel = 25,
                    areas = new[]
                    {
                        new { id = "area_tutorial", name = "Poker Academy", isUnlocked = true, bossCount = 1, completedBosses = 1 },
                        new { id = "area_downtown", name = "Downtown Casino", isUnlocked = true, bossCount = 2, completedBosses = 0 },
                        new { id = "area_highrise", name = "The Highrise", isUnlocked = false, bossCount = 2, completedBosses = 0 }
                    }
                }
            };
        }
        
        private object MockGetAreaBosses(object data)
        {
            return new
            {
                success = true,
                bosses = new[]
                {
                    new
                    {
                        id = "boss_tutorial",
                        name = "Dealer Dan",
                        difficulty = "easy",
                        minLevel = 1,
                        entryFee = 0,
                        canChallenge = true,
                        rewards = new { xp = 50, coins = 100, chips = 500 }
                    }
                }
            };
        }
        
        private object MockStartAdventure(object data)
        {
            return new
            {
                success = true,
                session = new
                {
                    userId = "player1",
                    boss = new
                    {
                        id = "boss_tutorial",
                        name = "Dealer Dan",
                        chips = 5000,
                        difficulty = "easy",
                        taunt = "Let's see what you've got, rookie!"
                    },
                    userChips = 10000,
                    handsPlayed = 0
                }
            };
        }
        
        private object CreateMockTableState()
        {
            return new
            {
                id = "mock-table",
                name = "Test Table",
                phase = "waiting",
                pot = 0,
                currentBet = 0,
                minBet = 100,
                dealerIndex = 0,
                currentPlayerIndex = -1,
                communityCards = new object[0],
                seats = new object[]
                {
                    new { index = 0, playerId = "player1", name = "You", chips = 10000, currentBet = 0, isFolded = false, isAllIn = false, isConnected = true, cards = new object[0] },
                    null, null, null, null, null, null, null, null
                }
            };
        }
        
        #endregion
    }
    
    #region Event Data Classes
    
    [Serializable]
    public class PlayerActionData
    {
        public string playerId;
        public string action;
        public int? amount;
    }
    
    [Serializable]
    public class PlayerJoinedData
    {
        public string playerId;
        public string name;
        public int seatIndex;
    }
    
    [Serializable]
    public class ChatMessageData
    {
        public string playerId;
        public string name;
        public string message;
    }
    
    [Serializable]
    public class HandResultData
    {
        public string userId;
        public string winnerName;
        public string handName;
        public int potAmount;
        public List<Card> winningCards;
    }
    
    [Serializable]
    public class TableInviteData
    {
        public string tableId;
        public string tableName;
        public string inviterName;
        public string inviterId;
    }
    
    #endregion
    
    /// <summary>
    /// Helper to run actions on the Unity main thread
    /// </summary>
    public static class UnityMainThread
    {
        private static readonly Queue<Action> _executionQueue = new Queue<Action>();
        private static bool _initialized = false;
        
        public static void Execute(Action action)
        {
            if (action == null) return;
            
            lock (_executionQueue)
            {
                _executionQueue.Enqueue(action);
            }
        }
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            
            var go = new GameObject("MainThreadDispatcher");
            go.AddComponent<MainThreadDispatcher>();
            UnityEngine.Object.DontDestroyOnLoad(go);
        }
        
        private class MainThreadDispatcher : MonoBehaviour
        {
            private void Update()
            {
                lock (_executionQueue)
                {
                    while (_executionQueue.Count > 0)
                    {
                        _executionQueue.Dequeue()?.Invoke();
                    }
                }
            }
        }
    }
}


