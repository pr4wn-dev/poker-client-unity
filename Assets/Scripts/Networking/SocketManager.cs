using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
#if SOCKET_IO_AVAILABLE
using SocketIOClient;
// SocketIOUnity class is in global namespace, no using needed
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
    /// IMPORTANT: SOCKET_IO_AVAILABLE must be defined in Scripting Define Symbols.
    /// This connects to the real server - no mock mode, no fallbacks.
    /// </summary>
    public class SocketManager : MonoBehaviour
    {
        private static SocketManager _instance;
        private static readonly object _lock = new object();
        private static bool _applicationIsQuitting = false;
        
        public static SocketManager Instance
        {
            get
            {
                if (_applicationIsQuitting)
                {
                    Debug.LogWarning("[SocketManager] Instance requested after application quit - returning null");
                    return null;
                }
                
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        // Try to find existing instance first
                        _instance = FindObjectOfType<SocketManager>();
                        
                        if (_instance == null)
                        {
                            // Create new instance
                            var go = new GameObject("SocketManager");
                            _instance = go.AddComponent<SocketManager>();
                            // Note: DontDestroyOnLoad is called in Awake
                        }
                    }
                    return _instance;
                }
            }
        }
        
        public static bool HasInstance => _instance != null;
        
        [Header("Server Configuration")]
        // Use network IP for phone testing, localhost for Unity Editor
        [SerializeField] private string serverUrl = "http://localhost:3000";
        [SerializeField] private bool autoConnect = false;
        
        [Header("Status")]
        [SerializeField] private bool isConnected = false;
        [SerializeField] private string connectionStatus = "Disconnected";
        
        public bool IsConnected => isConnected;
        public string ConnectionStatus => connectionStatus;
        
        /// <summary>
        /// Check if socket is fully ready (both flag and actual connection)
        /// </summary>
        public bool IsSocketReady
        {
            get
            {
                bool flagReady = isConnected;
                bool socketReady = _socket?.Connected ?? false;
                return flagReady && socketReady;
            }
        }
        
        // Events
        public event Action OnConnected;
        public event Action OnDisconnected;
        public event Action<string> OnError;
        
        // Game events
        public event Action<TableState> OnTableState;
        public event Action<PlayerActionData> OnPlayerAction;
        public event Action<PlayerJoinedData> OnPlayerJoined;
        public event Action<string> OnPlayerLeft;
        public event Action<string, string> OnSpectatorJoined; // userId, name
        public event Action<string> OnSpectatorLeft; // userId
        public event Action<ChatMessageData> OnChatMessage;
        public event Action<HandResultData> OnHandResult;
        public event Action<GameOverData> OnGameOver;
        public event Action<PlayerEliminatedData> OnPlayerEliminated;
        public event Action<TableInviteData> OnTableInvite;
        
        // Bot events
        public event Action<BotInvitePendingData> OnBotInvitePending;
        public event Action<BotJoinedData> OnBotJoined;
        public event Action<BotRejectedData> OnBotRejected;
        
        // Adventure events
        public event Action<AdventureResult> OnAdventureResult;
        public event Action<WorldMapState> OnWorldMapState;
        
        // Callback storage
        private Dictionary<string, Action<JObject>> _pendingCallbacks = new Dictionary<string, Action<JObject>>();
        private int _callbackId = 0;
        
        #if SOCKET_IO_AVAILABLE
        private SocketIOUnity _socket;
        #endif
        
        private void Awake()
        {
            // Singleton check
            if (_instance != null && _instance != this)
            {
                Debug.Log("[SocketManager] Duplicate instance destroyed");
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[SocketManager] Initialized");
        }
        
        private void Start()
        {
            if (autoConnect)
            {
                Connect();
            }
        }
        
        private void OnApplicationQuit()
        {
            _applicationIsQuitting = true;
        }
        
        private void OnDestroy()
        {
            if (_instance == this)
            {
                Disconnect();
                // Don't null out during scene transitions
                // _instance = null;
            }
        }
        
        #region Connection
        
        public void Connect(string url = null)
        {
            if (!string.IsNullOrEmpty(url))
                serverUrl = url;
            
            #if SOCKET_IO_AVAILABLE
            ConnectSocketIO();
            #else
            // Socket.IO not installed - this is a build error, not a runtime fallback
            Debug.LogError("[SocketManager] SOCKET_IO_AVAILABLE not defined! Add SOCKET_IO_AVAILABLE to Scripting Define Symbols in Player Settings.");
            connectionStatus = "ERROR: Socket.IO not configured";
            OnError?.Invoke("Socket.IO not available. Check Scripting Define Symbols.");
            #endif
        }
        
        #if SOCKET_IO_AVAILABLE
        private async void ConnectSocketIO()
        {
            try
            {
                connectionStatus = "Connecting...";
                
                var uri = new Uri(serverUrl);
                _socket = new SocketIOUnity(uri, new SocketIOOptions
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
        
        /// <summary>
        /// Parse socket response using JsonUtility (workaround for GetValue<T> not working - Issue #1)
        /// </summary>
        private T ParseResponse<T>(SocketIOResponse response) where T : class
        {
            try
            {
                var jsonStr = response.GetValue<object>()?.ToString();
                if (string.IsNullOrEmpty(jsonStr)) return null;
                return JsonUtility.FromJson<T>(jsonStr);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SocketManager] Failed to parse response as {typeof(T).Name}: {e.Message}");
                return null;
            }
        }
        
        private void RegisterEventListeners()
        {
            // Table events - using ParseResponse to work around GetValue<T> issues (Issue #1)
            _socket.On("table_state", response =>
            {
                var state = ParseResponse<TableState>(response);
                if (state != null) UnityMainThread.Execute(() => OnTableState?.Invoke(state));
            });
            
            _socket.On("player_action", response =>
            {
                var data = ParseResponse<PlayerActionData>(response);
                if (data != null) UnityMainThread.Execute(() => OnPlayerAction?.Invoke(data));
            });
            
            _socket.On("player_joined", response =>
            {
                var data = ParseResponse<PlayerJoinedData>(response);
                if (data != null) UnityMainThread.Execute(() => OnPlayerJoined?.Invoke(data));
            });
            
            _socket.On("player_left", response =>
            {
                // player_left sends just a string (player ID)
                var data = response.GetValue<string>();
                UnityMainThread.Execute(() => OnPlayerLeft?.Invoke(data));
            });
            
            _socket.On("spectator_joined", response =>
            {
                var data = ParseResponse<SpectatorEventData>(response);
                if (data != null) UnityMainThread.Execute(() => OnSpectatorJoined?.Invoke(data.userId, data.name));
            });
            
            _socket.On("spectator_left", response =>
            {
                var data = ParseResponse<SpectatorEventData>(response);
                if (data != null) UnityMainThread.Execute(() => OnSpectatorLeft?.Invoke(data.userId));
            });
            
            _socket.On("chat", response =>
            {
                var data = ParseResponse<ChatMessageData>(response);
                if (data != null) UnityMainThread.Execute(() => OnChatMessage?.Invoke(data));
            });
            
            _socket.On("hand_result", response =>
            {
                var data = ParseResponse<HandResultData>(response);
                if (data != null) UnityMainThread.Execute(() => OnHandResult?.Invoke(data));
            });
            
            _socket.On("game_over", response =>
            {
                var data = ParseResponse<GameOverData>(response);
                if (data != null) UnityMainThread.Execute(() => OnGameOver?.Invoke(data));
            });
            
            _socket.On("player_eliminated", response =>
            {
                var data = ParseResponse<PlayerEliminatedData>(response);
                if (data != null) UnityMainThread.Execute(() => OnPlayerEliminated?.Invoke(data));
            });
            
            _socket.On("table_invite_received", response =>
            {
                var data = ParseResponse<TableInviteData>(response);
                if (data != null) UnityMainThread.Execute(() => OnTableInvite?.Invoke(data));
            });
            
            // Bot events
            _socket.On("bot_invite_pending", response =>
            {
                var data = ParseResponse<BotInvitePendingData>(response);
                if (data != null) UnityMainThread.Execute(() => OnBotInvitePending?.Invoke(data));
            });
            
            _socket.On("bot_joined", response =>
            {
                var data = ParseResponse<BotJoinedData>(response);
                if (data != null) UnityMainThread.Execute(() => OnBotJoined?.Invoke(data));
            });
            
            _socket.On("bot_rejected", response =>
            {
                var data = ParseResponse<BotRejectedData>(response);
                if (data != null) UnityMainThread.Execute(() => OnBotRejected?.Invoke(data));
            });
            
            // Adventure events
            _socket.On("adventure_result", response =>
            {
                var data = ParseResponse<AdventureResult>(response);
                if (data != null) UnityMainThread.Execute(() => OnAdventureResult?.Invoke(data));
            });
            
            _socket.On("world_map_state", response =>
            {
                var data = ParseResponse<WorldMapState>(response);
                if (data != null) UnityMainThread.Execute(() => OnWorldMapState?.Invoke(data));
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
        /// Send an event to the server with callback
        /// Server responds via eventName_response event
        /// </summary>
        public void Emit<T>(string eventName, object data, Action<T> callback) where T : class
        {
            #if SOCKET_IO_AVAILABLE
            if (_socket == null)
            {
                Debug.LogError($"[SocketManager] Cannot emit {eventName}: Socket not initialized");
                UnityMainThread.Execute(() => callback?.Invoke(null));
                return;
            }
            
            // Listen for the response event
            string responseEvent = eventName + "_response";
            
            void OnResponse(SocketIOResponse response)
            {
                // Unsubscribe immediately after receiving
                _socket.Off(responseEvent);
                
                try
                {
                    Debug.Log($"[SocketManager] Received {responseEvent}");
                    // Get JSON and parse with Unity's JsonUtility
                    var obj = response.GetValue<object>();
                    string jsonStr = obj?.ToString() ?? "{}";
                    Debug.Log($"[SocketManager] Raw response JSON: {jsonStr}");
                    
                    if (string.IsNullOrEmpty(jsonStr) || jsonStr == "{}")
                    {
                        Debug.LogError($"[SocketManager] Empty or invalid JSON response for {responseEvent}");
                        UnityMainThread.Execute(() => callback?.Invoke(null));
                        return;
                    }
                    
                    var result = JsonUtility.FromJson<T>(jsonStr);
                    if (result == null)
                    {
                        Debug.LogError($"[SocketManager] JsonUtility.FromJson returned null for {responseEvent}. JSON was: {jsonStr}");
                        UnityMainThread.Execute(() => callback?.Invoke(null));
                        return;
                    }
                    
                    Debug.Log($"[SocketManager] Successfully parsed {responseEvent}");
                    UnityMainThread.Execute(() => callback?.Invoke(result));
                }
                catch (Exception e)
                {
                    Debug.LogError($"[SocketManager] Failed to parse {responseEvent}: {e.Message}");
                    Debug.LogError($"[SocketManager] Stack trace: {e.StackTrace}");
                    var obj = response.GetValue<object>();
                    string jsonStr = obj?.ToString() ?? "{}";
                    Debug.LogError($"[SocketManager] Failed JSON was: {jsonStr}");
                    UnityMainThread.Execute(() => callback?.Invoke(null));
                }
            }
            
            // Check if socket is connected before emitting
            // Note: SocketIOUnity uses _socket.Connected property
            // But we also need to check our isConnected flag to ensure initialization is complete
            bool socketConnected = _socket?.Connected ?? false;
            bool flagConnected = isConnected;
            
            // Both must be true for us to emit
            if (_socket == null || !socketConnected || !flagConnected)
            {
                Debug.LogWarning($"[SocketManager] Cannot emit {eventName}: Socket not ready. isConnected={flagConnected}, socket.Connected={socketConnected}, _socket={_socket != null}");
                UnityMainThread.Execute(() => callback?.Invoke(null));
                return;
            }
            
            Debug.Log($"[SocketManager] Listening for {responseEvent} and emitting {eventName}");
            _socket.On(responseEvent, OnResponse);
            _socket?.EmitAsync(eventName, data);
            Debug.Log($"[SocketManager] Emitted {eventName}");
            #endif
        }
        
        /// <summary>
        /// Send an event without callback
        /// </summary>
        public void Emit(string eventName, object data = null)
        {
            #if SOCKET_IO_AVAILABLE
            if (data != null)
                _socket?.EmitAsync(eventName, data);
            else
                _socket?.EmitAsync(eventName);
            #endif
        }
        
        #endregion
    }
    // NOTE: Event data classes moved to NetworkModels.cs (Issue #26)
    
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



