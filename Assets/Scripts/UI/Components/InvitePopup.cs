using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using PokerClient.Networking;
using System.Collections.Generic;

namespace PokerClient.UI.Components
{
    /// <summary>
    /// Popup for receiving table invites from friends.
    /// Shows invite details and allows accept/decline.
    /// </summary>
    public class InvitePopup : MonoBehaviour
    {
        public static InvitePopup Instance { get; private set; }
        
        private RectTransform _rect;
        private CanvasGroup _canvasGroup;
        private GameService _gameService;
        
        private Transform _invitePanel;
        private TextMeshProUGUI _fromText;
        private TextMeshProUGUI _tableNameText;
        private TextMeshProUGUI _blindsText;
        private TextMeshProUGUI _playersText;
        private Button _acceptButton;
        private Button _declineButton;
        private TextMeshProUGUI _timerText;
        
        private TableInvite _currentInvite;
        private Queue<TableInvite> _pendingInvites = new Queue<TableInvite>();
        private float _inviteTimeout = 30f;
        private float _timeRemaining;
        
        public System.Action<TableInvite> OnInviteAccepted;
        public System.Action<TableInvite> OnInviteDeclined;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            Initialize();
        }
        
        private void Initialize()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 995;
            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            gameObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            gameObject.AddComponent<GraphicRaycaster>();
            
            var theme = Theme.Current;
            
            // Container (top-right corner)
            var container = new GameObject("Container");
            container.transform.SetParent(transform, false);
            _rect = container.AddComponent<RectTransform>();
            _rect.anchorMin = new Vector2(1, 1);
            _rect.anchorMax = new Vector2(1, 1);
            _rect.pivot = new Vector2(1, 1);
            _rect.anchoredPosition = new Vector2(-20, -20);
            _rect.sizeDelta = new Vector2(350, 180);
            
            _canvasGroup = container.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0;
            _canvasGroup.blocksRaycasts = false;
            
            _invitePanel = container.transform;
            
            // Background
            var bg = container.AddComponent<Image>();
            bg.color = theme.cardPanelColor;
            
            // Border
            var outline = container.AddComponent<Outline>();
            outline.effectColor = theme.primaryColor;
            outline.effectDistance = new Vector2(2, 2);
            
            BuildContent(container.transform);
            
            _gameService = GameService.Instance;
        }
        
        private void BuildContent(Transform parent)
        {
            var theme = Theme.Current;
            
            var vlg = parent.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8;
            vlg.padding = new RectOffset(15, 15, 12, 12);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            
            // Header with timer
            var header = UIFactory.CreatePanel(parent, "Header", Color.clear);
            header.GetOrAddComponent<LayoutElement>().preferredHeight = 28;
            
            var titleText = UIFactory.CreateText(header.transform, "Title", "📩 TABLE INVITE", 18f, theme.accentColor);
            titleText.fontStyle = FontStyles.Bold;
            var titleRect = titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = Vector2.zero;
            titleRect.anchorMax = new Vector2(0.7f, 1);
            titleRect.sizeDelta = Vector2.zero;
            
            _timerText = UIFactory.CreateText(header.transform, "Timer", "30s", 16f, theme.warningColor);
            var timerRect = _timerText.GetComponent<RectTransform>();
            timerRect.anchorMin = new Vector2(0.7f, 0);
            timerRect.anchorMax = Vector2.one;
            timerRect.sizeDelta = Vector2.zero;
            _timerText.alignment = TextAlignmentOptions.Right;
            
            // From
            _fromText = UIFactory.CreateText(parent, "From", "From: PlayerName", 16f, theme.textPrimary);
            _fromText.GetOrAddComponent<LayoutElement>().preferredHeight = 22;
            
            // Table name
            _tableNameText = UIFactory.CreateText(parent, "Table", "Table: Friendly Game", 14f, theme.textSecondary);
            _tableNameText.GetOrAddComponent<LayoutElement>().preferredHeight = 20;
            
            // Blinds
            _blindsText = UIFactory.CreateText(parent, "Blinds", "Blinds: $10/$20", 14f, theme.textSecondary);
            _blindsText.GetOrAddComponent<LayoutElement>().preferredHeight = 20;
            
            // Players
            _playersText = UIFactory.CreateText(parent, "Players", "Players: 3/6", 14f, theme.textSecondary);
            _playersText.GetOrAddComponent<LayoutElement>().preferredHeight = 20;
            
            // Buttons
            var btnRow = UIFactory.CreatePanel(parent, "Buttons", Color.clear);
            btnRow.GetOrAddComponent<LayoutElement>().preferredHeight = 42;
            var hlg = btnRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 15;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            
            _declineButton = UIFactory.CreateButton(btnRow.transform, "Decline", "DECLINE", OnDeclineClick).GetComponent<Button>();
            _declineButton.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 38);
            _declineButton.GetComponent<Image>().color = theme.dangerColor;
            
            _acceptButton = UIFactory.CreateButton(btnRow.transform, "Accept", "JOIN", OnAcceptClick).GetComponent<Button>();
            _acceptButton.GetComponent<RectTransform>().sizeDelta = new Vector2(120, 38);
            _acceptButton.GetComponent<Image>().color = theme.successColor;
        }
        
        private void Update()
        {
            if (_currentInvite != null && _canvasGroup.alpha > 0)
            {
                _timeRemaining -= Time.deltaTime;
                _timerText.text = $"{Mathf.CeilToInt(_timeRemaining)}s";
                
                if (_timeRemaining <= 0)
                {
                    OnDeclineClick();
                }
            }
        }
        
        /// <summary>
        /// Show an incoming invite
        /// </summary>
        public static void ShowInvite(TableInvite invite)
        {
            if (Instance == null)
            {
                var go = new GameObject("InvitePopup");
                go.AddComponent<InvitePopup>();
            }
            
            Instance.QueueInvite(invite);
        }
        
        private void QueueInvite(TableInvite invite)
        {
            if (_currentInvite != null)
            {
                _pendingInvites.Enqueue(invite);
            }
            else
            {
                ShowInviteInternal(invite);
            }
        }
        
        private void ShowInviteInternal(TableInvite invite)
        {
            _currentInvite = invite;
            _timeRemaining = _inviteTimeout;
            
            _fromText.text = $"From: {invite.fromUsername}";
            _tableNameText.text = $"Table: {invite.tableName}";
            _blindsText.text = $"Blinds: ${invite.smallBlind}/${invite.bigBlind}";
            _playersText.text = $"Players: {invite.currentPlayers}/{invite.maxPlayers}";
            
            _canvasGroup.alpha = 1;
            _canvasGroup.blocksRaycasts = true;
            
            // Play notification sound
            // AudioManager.Instance?.PlaySFX("notification");
        }
        
        private void Hide()
        {
            _canvasGroup.alpha = 0;
            _canvasGroup.blocksRaycasts = false;
            _currentInvite = null;
            
            // Show next invite if any
            if (_pendingInvites.Count > 0)
            {
                ShowInviteInternal(_pendingInvites.Dequeue());
            }
        }
        
        private void OnAcceptClick()
        {
            if (_currentInvite == null) return;
            
            var invite = _currentInvite;
            Hide();
            
            OnInviteAccepted?.Invoke(invite);
            
            // Join the table
            _gameService?.JoinTable(invite.tableId, invite.password, (success, state, error) =>
            {
                if (success)
                {
                    ToastNotification.Success($"Joined {invite.tableName}!");
                    SceneManager.LoadScene("TableScene");
                }
                else
                {
                    ToastNotification.Error(error ?? "Failed to join table");
                }
            });
        }
        
        private void OnDeclineClick()
        {
            var invite = _currentInvite;
            Hide();
            
            if (invite != null)
            {
                OnInviteDeclined?.Invoke(invite);
                ToastNotification.Info("Invite declined");
            }
        }
        
        /// <summary>
        /// Clear all pending invites
        /// </summary>
        public static void ClearAll()
        {
            if (Instance != null)
            {
                Instance._pendingInvites.Clear();
                Instance.Hide();
            }
        }
    }
    
    [System.Serializable]
    public class TableInvite
    {
        public string inviteId;
        public string tableId;
        public string tableName;
        public string fromUserId;
        public string fromUsername;
        public int smallBlind;
        public int bigBlind;
        public int currentPlayers;
        public int maxPlayers;
        public string password;
        public string timestamp;
    }
}

