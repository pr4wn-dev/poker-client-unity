using UnityEngine;
using UnityEngine.UI;
using PokerClient.Networking;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PokerClient.UI
{
    /// <summary>
    /// Multiplayer lobby - browse tables, create tables, join games
    /// </summary>
    public class MultiplayerLobbyUI : MonoBehaviour
    {
        [Header("Table List")]
        [SerializeField] private Transform tableListContainer;
        [SerializeField] private GameObject tableRowPrefab;
        [SerializeField] private Button refreshButton;
        [SerializeField] private TMPro.TextMeshProUGUI tableCountText;
        
        [Header("Create Table Panel")]
        [SerializeField] private GameObject createTablePanel;
        [SerializeField] private TMPro.TMP_InputField tableNameInput;
        [SerializeField] private TMPro.TMP_InputField passwordInput;
        [SerializeField] private TMPro.TMP_Dropdown maxPlayersDropdown;
        [SerializeField] private TMPro.TMP_Dropdown blindsDropdown;
        [SerializeField] private TMPro.TMP_Dropdown houseRulesDropdown;
        [SerializeField] private Toggle privateToggle;
        [SerializeField] private Button createButton;
        [SerializeField] private Button cancelCreateButton;
        
        [Header("Join Table Panel")]
        [SerializeField] private GameObject joinPasswordPanel;
        [SerializeField] private TMPro.TMP_InputField joinPasswordInput;
        [SerializeField] private Button joinConfirmButton;
        [SerializeField] private Button joinCancelButton;
        
        [Header("Invite Friends Panel")]
        [SerializeField] private GameObject inviteFriendsPanel;
        [SerializeField] private Transform friendsListContainer;
        [SerializeField] private GameObject friendRowPrefab;
        
        private PokerNetworkManager _network;
        private List<TableInfo> _tables;
        private string _pendingJoinTableId;
        
        private void Start()
        {
            _network = PokerNetworkManager.Instance;
            
            // Button listeners
            refreshButton?.onClick.AddListener(() => RefreshTablesAsync());
            createButton?.onClick.AddListener(() => CreateTableAsync());
            cancelCreateButton?.onClick.AddListener(() => createTablePanel?.SetActive(false));
            joinConfirmButton?.onClick.AddListener(() => JoinWithPasswordAsync());
            joinCancelButton?.onClick.AddListener(() => joinPasswordPanel?.SetActive(false));
            
            // Subscribe to events
            _network.OnTableCreated += OnTableCreated;
        }
        
        private void OnEnable()
        {
            RefreshTablesAsync();
        }
        
        private void OnDestroy()
        {
            if (_network != null)
            {
                _network.OnTableCreated -= OnTableCreated;
            }
        }
        
        #region Table List
        
        public async void RefreshTablesAsync()
        {
            refreshButton.interactable = false;
            
            // TODO: Implement when network methods are ready
            // _tables = await _network.GetTablesAsync();
            // PopulateTableList();
            
            Debug.Log("[Lobby] Refreshing tables...");
            
            await Task.Delay(500);  // Simulate network delay
            refreshButton.interactable = true;
        }
        
        private void PopulateTableList()
        {
            // Clear existing rows
            foreach (Transform child in tableListContainer)
            {
                Destroy(child.gameObject);
            }
            
            if (_tables == null || _tables.Count == 0)
            {
                if (tableCountText) tableCountText.text = "No tables available";
                return;
            }
            
            if (tableCountText) tableCountText.text = $"{_tables.Count} table(s) found";
            
            foreach (var table in _tables)
            {
                var row = Instantiate(tableRowPrefab, tableListContainer);
                var tableRow = row.GetComponent<TableRowUI>();
                tableRow?.Setup(table, OnJoinClicked, OnSpectateClicked);
            }
        }
        
        private void OnTableCreated(TableInfo table)
        {
            // Add new table to list
            _tables ??= new List<TableInfo>();
            _tables.Insert(0, table);
            PopulateTableList();
        }
        
        #endregion
        
        #region Join Table
        
        private void OnJoinClicked(TableInfo table)
        {
            if (table.hasPassword)
            {
                _pendingJoinTableId = table.id;
                joinPasswordInput.text = "";
                joinPasswordPanel?.SetActive(true);
            }
            else
            {
                JoinTableAsync(table.id, null, false);
            }
        }
        
        private void OnSpectateClicked(TableInfo table)
        {
            if (table.hasPassword)
            {
                _pendingJoinTableId = table.id;
                joinPasswordInput.text = "";
                joinPasswordPanel?.SetActive(true);
                // TODO: Mark as spectator join
            }
            else
            {
                JoinTableAsync(table.id, null, true);
            }
        }
        
        private async void JoinWithPasswordAsync()
        {
            var password = joinPasswordInput.text;
            joinPasswordPanel?.SetActive(false);
            
            await JoinTableAsync(_pendingJoinTableId, password, false);
        }
        
        private async Task JoinTableAsync(string tableId, string password, bool asSpectator)
        {
            Debug.Log($"[Lobby] Joining table {tableId} (spectator: {asSpectator})");
            
            // TODO: Implement when network methods support password/spectator
            // var response = await _network.JoinTableAsync(tableId, null, password, asSpectator);
            // if (!response.success)
            // {
            //     ShowError(response.error);
            // }
        }
        
        #endregion
        
        #region Create Table
        
        public void ShowCreateTablePanel()
        {
            tableNameInput.text = "";
            passwordInput.text = "";
            privateToggle.isOn = false;
            createTablePanel?.SetActive(true);
        }
        
        private async void CreateTableAsync()
        {
            var tableName = tableNameInput.text;
            if (string.IsNullOrWhiteSpace(tableName))
            {
                tableName = $"Table {Random.Range(1000, 9999)}";
            }
            
            var password = privateToggle.isOn ? passwordInput.text : null;
            var maxPlayers = GetMaxPlayersSelection();
            var (smallBlind, bigBlind) = GetBlindsSelection();
            var houseRules = GetHouseRulesSelection();
            
            createButton.interactable = false;
            
            // TODO: Implement when network methods are ready
            // var response = await _network.CreateTableAsync(
            //     tableName, maxPlayers, smallBlind, bigBlind, 
            //     privateToggle.isOn, password, houseRules
            // );
            
            Debug.Log($"[Lobby] Creating table: {tableName}");
            
            await Task.Delay(500);  // Simulate network delay
            
            createButton.interactable = true;
            createTablePanel?.SetActive(false);
        }
        
        private int GetMaxPlayersSelection()
        {
            return maxPlayersDropdown?.value switch
            {
                0 => 2,  // Heads up
                1 => 6,  // 6-max
                2 => 9,  // Full ring
                _ => 9
            };
        }
        
        private (int small, int big) GetBlindsSelection()
        {
            return blindsDropdown?.value switch
            {
                0 => (25, 50),
                1 => (50, 100),
                2 => (100, 200),
                3 => (250, 500),
                4 => (500, 1000),
                _ => (50, 100)
            };
        }
        
        private string GetHouseRulesSelection()
        {
            return houseRulesDropdown?.value switch
            {
                0 => HouseRulesPresets.Standard,
                1 => HouseRulesPresets.NoLimit,
                2 => HouseRulesPresets.PotLimit,
                3 => HouseRulesPresets.ShortDeck,
                4 => HouseRulesPresets.BombPot,
                _ => HouseRulesPresets.Standard
            };
        }
        
        #endregion
        
        #region Invite Friends
        
        public void ShowInviteFriendsPanel()
        {
            inviteFriendsPanel?.SetActive(true);
            LoadFriendsListAsync();
        }
        
        private async void LoadFriendsListAsync()
        {
            // TODO: Load friends list and populate
            await Task.Yield();
        }
        
        #endregion
        
        private void ShowError(string message)
        {
            Debug.LogError($"[Lobby] Error: {message}");
            // TODO: Show error popup
        }
    }
    
    /// <summary>
    /// UI component for a table row in the list
    /// </summary>
    public class TableRowUI : MonoBehaviour
    {
        [SerializeField] private TMPro.TextMeshProUGUI tableNameText;
        [SerializeField] private TMPro.TextMeshProUGUI playersText;
        [SerializeField] private TMPro.TextMeshProUGUI blindsText;
        [SerializeField] private TMPro.TextMeshProUGUI rulesText;
        [SerializeField] private GameObject passwordIcon;
        [SerializeField] private GameObject inProgressIcon;
        [SerializeField] private Button joinButton;
        [SerializeField] private Button spectateButton;
        
        private TableInfo _table;
        private System.Action<TableInfo> _onJoin;
        private System.Action<TableInfo> _onSpectate;
        
        public void Setup(TableInfo table, System.Action<TableInfo> onJoin, System.Action<TableInfo> onSpectate)
        {
            _table = table;
            _onJoin = onJoin;
            _onSpectate = onSpectate;
            
            if (tableNameText) tableNameText.text = table.name;
            if (playersText) playersText.text = $"{table.playerCount}/{table.maxPlayers}";
            if (blindsText) blindsText.text = $"${table.smallBlind}/${table.bigBlind}";
            if (rulesText) rulesText.text = table.houseRulesPreset;
            if (passwordIcon) passwordIcon.SetActive(table.hasPassword);
            if (inProgressIcon) inProgressIcon.SetActive(table.gameStarted);
            
            // Can only join if not full and game hasn't started
            bool canJoin = table.playerCount < table.maxPlayers && !table.gameStarted;
            if (joinButton)
            {
                joinButton.interactable = canJoin;
                joinButton.onClick.AddListener(() => _onJoin?.Invoke(_table));
            }
            
            // Can spectate if allowed
            if (spectateButton)
            {
                spectateButton.gameObject.SetActive(table.allowSpectators);
                spectateButton.onClick.AddListener(() => _onSpectate?.Invoke(_table));
            }
        }
    }
}

