using UnityEngine;
using UnityEngine.UI;
using PokerClient.Networking;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PokerClient.UI
{
    /// <summary>
    /// Friends panel - manage friends, send/accept requests, invite to games
    /// </summary>
    public class FriendsUI : MonoBehaviour
    {
        [Header("Tabs")]
        [SerializeField] private Button friendsTabButton;
        [SerializeField] private Button requestsTabButton;
        [SerializeField] private Button searchTabButton;
        
        [Header("Friends List")]
        [SerializeField] private GameObject friendsListPanel;
        [SerializeField] private Transform friendsListContainer;
        [SerializeField] private GameObject friendRowPrefab;
        [SerializeField] private TMPro.TextMeshProUGUI friendsCountText;
        
        [Header("Friend Requests")]
        [SerializeField] private GameObject requestsPanel;
        [SerializeField] private Transform requestsListContainer;
        [SerializeField] private GameObject requestRowPrefab;
        [SerializeField] private TMPro.TextMeshProUGUI requestsCountText;
        
        [Header("Search")]
        [SerializeField] private GameObject searchPanel;
        [SerializeField] private TMPro.TMP_InputField searchInput;
        [SerializeField] private Button searchButton;
        [SerializeField] private Transform searchResultsContainer;
        [SerializeField] private GameObject searchResultRowPrefab;
        
        [Header("Table Invites")]
        [SerializeField] private GameObject invitesPanel;
        [SerializeField] private Transform invitesListContainer;
        [SerializeField] private GameObject inviteRowPrefab;
        
        private PokerNetworkManager _network;
        private List<PublicProfile> _friends;
        private List<FriendRequest> _requests;
        private List<TableInvite> _tableInvites;
        
        private void Start()
        {
            _network = PokerNetworkManager.Instance;
            
            // Tab buttons
            friendsTabButton?.onClick.AddListener(ShowFriendsTab);
            requestsTabButton?.onClick.AddListener(ShowRequestsTab);
            searchTabButton?.onClick.AddListener(ShowSearchTab);
            searchButton?.onClick.AddListener(() => SearchUsersAsync());
            
            // Subscribe to events
            // _network.OnFriendRequestReceived += OnFriendRequestReceived;
            // _network.OnTableInviteReceived += OnTableInviteReceived;
            // _network.OnFriendOnline += OnFriendStatusChanged;
            // _network.OnFriendOffline += OnFriendStatusChanged;
        }
        
        private void OnEnable()
        {
            ShowFriendsTab();
            LoadFriendsAsync();
            LoadRequestsAsync();
            LoadTableInvitesAsync();
        }
        
        #region Tab Navigation
        
        private void ShowFriendsTab()
        {
            friendsListPanel?.SetActive(true);
            requestsPanel?.SetActive(false);
            searchPanel?.SetActive(false);
        }
        
        private void ShowRequestsTab()
        {
            friendsListPanel?.SetActive(false);
            requestsPanel?.SetActive(true);
            searchPanel?.SetActive(false);
        }
        
        private void ShowSearchTab()
        {
            friendsListPanel?.SetActive(false);
            requestsPanel?.SetActive(false);
            searchPanel?.SetActive(true);
            searchInput.text = "";
            ClearSearchResults();
        }
        
        #endregion
        
        #region Friends List
        
        private async void LoadFriendsAsync()
        {
            // TODO: Implement when network methods are ready
            // var response = await _network.GetFriendsAsync();
            // _friends = response.friends;
            // PopulateFriendsList();
            
            await Task.Yield();
            Debug.Log("[Friends] Loading friends list...");
        }
        
        private void PopulateFriendsList()
        {
            foreach (Transform child in friendsListContainer)
            {
                Destroy(child.gameObject);
            }
            
            if (_friends == null || _friends.Count == 0)
            {
                if (friendsCountText) friendsCountText.text = "No friends yet";
                return;
            }
            
            if (friendsCountText) friendsCountText.text = $"{_friends.Count} friend(s)";
            
            // Sort by online status
            _friends.Sort((a, b) => b.isOnline.CompareTo(a.isOnline));
            
            foreach (var friend in _friends)
            {
                var row = Instantiate(friendRowPrefab, friendsListContainer);
                var friendRow = row.GetComponent<FriendRowUI>();
                friendRow?.Setup(friend, OnInviteFriend, OnRemoveFriend);
            }
        }
        
        private void OnInviteFriend(PublicProfile friend)
        {
            Debug.Log($"[Friends] Inviting {friend.username} to table...");
            // TODO: Show table selection or invite to current table
        }
        
        private async void OnRemoveFriend(PublicProfile friend)
        {
            // TODO: Confirm dialog
            // await _network.RemoveFriendAsync(friend.id);
            await Task.Yield();
            LoadFriendsAsync();
        }
        
        #endregion
        
        #region Friend Requests
        
        private async void LoadRequestsAsync()
        {
            // TODO: Implement when network methods are ready
            await Task.Yield();
            Debug.Log("[Friends] Loading friend requests...");
        }
        
        private void PopulateRequestsList()
        {
            foreach (Transform child in requestsListContainer)
            {
                Destroy(child.gameObject);
            }
            
            if (_requests == null || _requests.Count == 0)
            {
                if (requestsCountText) requestsCountText.text = "No pending requests";
                return;
            }
            
            if (requestsCountText) requestsCountText.text = $"{_requests.Count} request(s)";
            
            foreach (var request in _requests)
            {
                var row = Instantiate(requestRowPrefab, requestsListContainer);
                var requestRow = row.GetComponent<FriendRequestRowUI>();
                requestRow?.Setup(request, OnAcceptRequest, OnDeclineRequest);
            }
        }
        
        private async void OnAcceptRequest(FriendRequest request)
        {
            // await _network.AcceptFriendRequestAsync(request.fromUserId);
            await Task.Yield();
            LoadRequestsAsync();
            LoadFriendsAsync();
        }
        
        private async void OnDeclineRequest(FriendRequest request)
        {
            // await _network.DeclineFriendRequestAsync(request.fromUserId);
            await Task.Yield();
            LoadRequestsAsync();
        }
        
        #endregion
        
        #region Search
        
        private async void SearchUsersAsync()
        {
            var query = searchInput.text;
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
                return;
            
            // TODO: Implement when network methods are ready
            // var results = await _network.SearchUsersAsync(query);
            // PopulateSearchResults(results);
            
            await Task.Yield();
            Debug.Log($"[Friends] Searching for: {query}");
        }
        
        private void PopulateSearchResults(List<PublicProfile> results)
        {
            ClearSearchResults();
            
            foreach (var user in results)
            {
                var row = Instantiate(searchResultRowPrefab, searchResultsContainer);
                var resultRow = row.GetComponent<SearchResultRowUI>();
                resultRow?.Setup(user, OnSendFriendRequest);
            }
        }
        
        private void ClearSearchResults()
        {
            foreach (Transform child in searchResultsContainer)
            {
                Destroy(child.gameObject);
            }
        }
        
        private async void OnSendFriendRequest(PublicProfile user)
        {
            // await _network.SendFriendRequestAsync(user.id);
            await Task.Yield();
            Debug.Log($"[Friends] Sent friend request to {user.username}");
        }
        
        #endregion
        
        #region Table Invites
        
        private async void LoadTableInvitesAsync()
        {
            // TODO: Implement when network methods are ready
            await Task.Yield();
        }
        
        private void OnFriendRequestReceived(FriendRequestEvent evt)
        {
            LoadRequestsAsync();
        }
        
        private void OnTableInviteReceived(TableInviteEvent evt)
        {
            LoadTableInvitesAsync();
            // TODO: Show notification
        }
        
        private void OnFriendStatusChanged(string oderId)
        {
            LoadFriendsAsync();
        }
        
        #endregion
    }
    
    /// <summary>
    /// UI row for a friend in the list
    /// </summary>
    public class FriendRowUI : MonoBehaviour
    {
        [SerializeField] private TMPro.TextMeshProUGUI usernameText;
        [SerializeField] private TMPro.TextMeshProUGUI statusText;
        [SerializeField] private GameObject onlineIndicator;
        [SerializeField] private Button inviteButton;
        [SerializeField] private Button removeButton;
        
        public void Setup(PublicProfile friend, System.Action<PublicProfile> onInvite, System.Action<PublicProfile> onRemove)
        {
            if (usernameText) usernameText.text = friend.username;
            if (statusText) statusText.text = friend.isOnline ? "Online" : "Offline";
            if (onlineIndicator) onlineIndicator.SetActive(friend.isOnline);
            
            inviteButton?.onClick.AddListener(() => onInvite?.Invoke(friend));
            inviteButton?.gameObject.SetActive(friend.isOnline);
            
            removeButton?.onClick.AddListener(() => onRemove?.Invoke(friend));
        }
    }
    
    /// <summary>
    /// UI row for a friend request
    /// </summary>
    public class FriendRequestRowUI : MonoBehaviour
    {
        [SerializeField] private TMPro.TextMeshProUGUI usernameText;
        [SerializeField] private Button acceptButton;
        [SerializeField] private Button declineButton;
        
        public void Setup(FriendRequest request, System.Action<FriendRequest> onAccept, System.Action<FriendRequest> onDecline)
        {
            if (usernameText) usernameText.text = request.fromUsername;
            
            acceptButton?.onClick.AddListener(() => onAccept?.Invoke(request));
            declineButton?.onClick.AddListener(() => onDecline?.Invoke(request));
        }
    }
    
    /// <summary>
    /// UI row for a search result
    /// </summary>
    public class SearchResultRowUI : MonoBehaviour
    {
        [SerializeField] private TMPro.TextMeshProUGUI usernameText;
        [SerializeField] private TMPro.TextMeshProUGUI levelText;
        [SerializeField] private Button addButton;
        
        public void Setup(PublicProfile user, System.Action<PublicProfile> onAdd)
        {
            if (usernameText) usernameText.text = user.username;
            if (levelText) levelText.text = $"Lv. {user.highestLevel}";
            
            addButton?.onClick.AddListener(() => onAdd?.Invoke(user));
        }
    }
}






