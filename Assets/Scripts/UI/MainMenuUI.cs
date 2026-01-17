using UnityEngine;
using UnityEngine.SceneManagement;
using PokerClient.Networking;

namespace PokerClient.UI
{
    /// <summary>
    /// Main menu controller - mode selection and navigation
    /// </summary>
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject modeSelectPanel;
        [SerializeField] private GameObject multiplayerLobbyPanel;
        [SerializeField] private GameObject adventureLevelSelectPanel;
        [SerializeField] private GameObject friendsPanel;
        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] private GameObject settingsPanel;
        
        [Header("User Display")]
        [SerializeField] private TMPro.TextMeshProUGUI usernameText;
        [SerializeField] private TMPro.TextMeshProUGUI chipsText;
        [SerializeField] private TMPro.TextMeshProUGUI adventureCoinsText;
        [SerializeField] private TMPro.TextMeshProUGUI levelText;
        
        [Header("Notifications")]
        [SerializeField] private GameObject friendRequestBadge;
        [SerializeField] private TMPro.TextMeshProUGUI friendRequestCountText;
        [SerializeField] private GameObject tableInviteBadge;
        
        private PokerNetworkManager _network;
        
        private void Start()
        {
            _network = PokerNetworkManager.Instance;
            
            // Subscribe to events
            _network.OnTableStateUpdated += OnJoinedTable;
            
            ShowModeSelect();
            UpdateUserDisplay();
        }
        
        private void OnDestroy()
        {
            if (_network != null)
            {
                _network.OnTableStateUpdated -= OnJoinedTable;
            }
        }
        
        #region Navigation
        
        public void ShowModeSelect()
        {
            HideAllPanels();
            modeSelectPanel?.SetActive(true);
        }
        
        public void OnAdventureModeClicked()
        {
            HideAllPanels();
            adventureLevelSelectPanel?.SetActive(true);
        }
        
        public void OnMultiplayerModeClicked()
        {
            HideAllPanels();
            multiplayerLobbyPanel?.SetActive(true);
        }
        
        public void OnFriendsClicked()
        {
            HideAllPanels();
            friendsPanel?.SetActive(true);
        }
        
        public void OnInventoryClicked()
        {
            HideAllPanels();
            inventoryPanel?.SetActive(true);
        }
        
        public void OnSettingsClicked()
        {
            HideAllPanels();
            settingsPanel?.SetActive(true);
        }
        
        public void OnBackClicked()
        {
            ShowModeSelect();
        }
        
        private void HideAllPanels()
        {
            mainPanel?.SetActive(false);
            modeSelectPanel?.SetActive(false);
            multiplayerLobbyPanel?.SetActive(false);
            adventureLevelSelectPanel?.SetActive(false);
            friendsPanel?.SetActive(false);
            inventoryPanel?.SetActive(false);
            settingsPanel?.SetActive(false);
        }
        
        #endregion
        
        #region User Display
        
        private void UpdateUserDisplay()
        {
            // TODO: Get actual user data from network manager
            if (usernameText) usernameText.text = "Player";
            if (chipsText) chipsText.text = "$10,000";
            if (adventureCoinsText) adventureCoinsText.text = "0";
            if (levelText) levelText.text = "Lv. 1";
        }
        
        public void UpdateNotifications(int friendRequests, bool hasTableInvite)
        {
            if (friendRequestBadge)
            {
                friendRequestBadge.SetActive(friendRequests > 0);
                if (friendRequestCountText)
                    friendRequestCountText.text = friendRequests.ToString();
            }
            
            if (tableInviteBadge)
            {
                tableInviteBadge.SetActive(hasTableInvite);
            }
        }
        
        #endregion
        
        #region Event Handlers
        
        private void OnJoinedTable(TableState state)
        {
            // Transition to game scene
            // SceneManager.LoadScene("GameScene");
            Debug.Log($"[MainMenu] Joined table: {state.name}");
        }
        
        #endregion
    }
}



