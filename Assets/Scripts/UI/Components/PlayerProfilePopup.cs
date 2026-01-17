using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PokerClient.Networking;

namespace PokerClient.UI.Components
{
    /// <summary>
    /// Player profile popup - shows stats, avatar, and actions (add friend, invite, etc.)
    /// </summary>
    public class PlayerProfilePopup : MonoBehaviour
    {
        private RectTransform _rect;
        private CanvasGroup _canvasGroup;
        private GameService _gameService;
        
        private TextMeshProUGUI _usernameText;
        private TextMeshProUGUI _levelText;
        private TextMeshProUGUI _xpText;
        private Image _avatarImage;
        
        private TextMeshProUGUI _handsPlayedText;
        private TextMeshProUGUI _handsWonText;
        private TextMeshProUGUI _biggestPotText;
        private TextMeshProUGUI _winRateText;
        
        private Button _addFriendButton;
        private Button _inviteButton;
        private Button _closeButton;
        
        private string _targetUserId;
        
        public System.Action OnClose;
        
        public static PlayerProfilePopup Create(Transform parent)
        {
            var go = new GameObject("PlayerProfilePopup");
            go.transform.SetParent(parent, false);
            var popup = go.AddComponent<PlayerProfilePopup>();
            popup.Initialize();
            return popup;
        }
        
        private void Initialize()
        {
            var theme = Theme.Current;
            
            _rect = gameObject.AddComponent<RectTransform>();
            _rect.anchorMin = Vector2.zero;
            _rect.anchorMax = Vector2.one;
            _rect.sizeDelta = Vector2.zero;
            
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
            // Click outside to close
            var bgBtn = gameObject.AddComponent<Button>();
            bgBtn.onClick.AddListener(Hide);
            bgBtn.targetGraphic = gameObject.AddComponent<Image>();
            bgBtn.targetGraphic.color = new Color(0, 0, 0, 0.7f);
            
            // Profile card
            var card = UIFactory.CreatePanel(transform, "Card", theme.cardPanelColor);
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.25f, 0.2f);
            cardRect.anchorMax = new Vector2(0.75f, 0.8f);
            cardRect.sizeDelta = Vector2.zero;
            
            BuildHeader(card.transform);
            BuildStats(card.transform);
            BuildActions(card.transform);
            
            gameObject.SetActive(false);
        }
        
        private void BuildHeader(Transform parent)
        {
            var theme = Theme.Current;
            
            var header = UIFactory.CreatePanel(parent, "Header", Color.clear);
            var headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 0.7f);
            headerRect.anchorMax = Vector2.one;
            headerRect.sizeDelta = Vector2.zero;
            
            // Avatar
            var avatarHolder = UIFactory.CreatePanel(header.transform, "Avatar", theme.backgroundColor);
            var avatarRect = avatarHolder.GetComponent<RectTransform>();
            avatarRect.anchorMin = new Vector2(0.03f, 0.1f);
            avatarRect.anchorMax = new Vector2(0.25f, 0.9f);
            avatarRect.sizeDelta = Vector2.zero;
            _avatarImage = avatarHolder.GetComponent<Image>();
            
            // Username
            _usernameText = UIFactory.CreateTitle(header.transform, "Username", "Username", 28f);
            var usernameRect = _usernameText.GetComponent<RectTransform>();
            usernameRect.anchorMin = new Vector2(0.28f, 0.5f);
            usernameRect.anchorMax = new Vector2(0.9f, 0.95f);
            usernameRect.sizeDelta = Vector2.zero;
            _usernameText.color = theme.accentColor;
            
            // Level
            _levelText = UIFactory.CreateText(header.transform, "Level", "Level 1", 20f, theme.textPrimary);
            var levelRect = _levelText.GetComponent<RectTransform>();
            levelRect.anchorMin = new Vector2(0.28f, 0.25f);
            levelRect.anchorMax = new Vector2(0.5f, 0.5f);
            levelRect.sizeDelta = Vector2.zero;
            
            // XP
            _xpText = UIFactory.CreateText(header.transform, "XP", "0 XP", 16f, theme.textSecondary);
            var xpRect = _xpText.GetComponent<RectTransform>();
            xpRect.anchorMin = new Vector2(0.28f, 0.05f);
            xpRect.anchorMax = new Vector2(0.5f, 0.25f);
            xpRect.sizeDelta = Vector2.zero;
            
            // Close button
            _closeButton = UIFactory.CreateButton(header.transform, "Close", "✕", Hide).GetComponent<Button>();
            var closeRect = _closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.9f, 0.7f);
            closeRect.anchorMax = new Vector2(0.98f, 0.95f);
            closeRect.sizeDelta = Vector2.zero;
            _closeButton.GetComponent<Image>().color = theme.dangerColor;
        }
        
        private void BuildStats(Transform parent)
        {
            var theme = Theme.Current;
            
            var statsPanel = UIFactory.CreatePanel(parent, "Stats", theme.backgroundColor);
            var statsRect = statsPanel.GetComponent<RectTransform>();
            statsRect.anchorMin = new Vector2(0.02f, 0.22f);
            statsRect.anchorMax = new Vector2(0.98f, 0.68f);
            statsRect.sizeDelta = Vector2.zero;
            
            var glg = statsPanel.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(200, 80);
            glg.spacing = new Vector2(20, 10);
            glg.padding = new RectOffset(20, 20, 20, 20);
            glg.childAlignment = TextAnchor.MiddleCenter;
            
            _handsPlayedText = CreateStatItem(statsPanel.transform, "Hands Played", "0");
            _handsWonText = CreateStatItem(statsPanel.transform, "Hands Won", "0");
            _biggestPotText = CreateStatItem(statsPanel.transform, "Biggest Pot", "0");
            _winRateText = CreateStatItem(statsPanel.transform, "Win Rate", "0%");
        }
        
        private TextMeshProUGUI CreateStatItem(Transform parent, string label, string value)
        {
            var theme = Theme.Current;
            
            var item = UIFactory.CreatePanel(parent, $"Stat_{label}", theme.cardPanelColor);
            
            var labelText = UIFactory.CreateText(item.transform, "Label", label, 14f, theme.textSecondary);
            labelText.alignment = TextAlignmentOptions.Center;
            var labelRect = labelText.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0.55f);
            labelRect.anchorMax = new Vector2(1, 0.9f);
            labelRect.sizeDelta = Vector2.zero;
            
            var valueText = UIFactory.CreateTitle(item.transform, "Value", value, 26f);
            valueText.alignment = TextAlignmentOptions.Center;
            var valueRect = valueText.GetComponent<RectTransform>();
            valueRect.anchorMin = new Vector2(0, 0.1f);
            valueRect.anchorMax = new Vector2(1, 0.55f);
            valueRect.sizeDelta = Vector2.zero;
            valueText.color = theme.textPrimary;
            
            return valueText;
        }
        
        private void BuildActions(Transform parent)
        {
            var theme = Theme.Current;
            
            var actionsPanel = UIFactory.CreatePanel(parent, "Actions", Color.clear);
            var actionsRect = actionsPanel.GetComponent<RectTransform>();
            actionsRect.anchorMin = new Vector2(0.05f, 0.03f);
            actionsRect.anchorMax = new Vector2(0.95f, 0.18f);
            actionsRect.sizeDelta = Vector2.zero;
            
            var hlg = actionsPanel.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 30;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            
            _addFriendButton = UIFactory.CreateButton(actionsPanel.transform, "AddFriend", "ADD FRIEND", OnAddFriendClick).GetComponent<Button>();
            _addFriendButton.GetComponent<RectTransform>().sizeDelta = new Vector2(180, 55);
            _addFriendButton.GetComponent<Image>().color = theme.primaryColor;
            
            _inviteButton = UIFactory.CreateButton(actionsPanel.transform, "Invite", "INVITE TO TABLE", OnInviteClick).GetComponent<Button>();
            _inviteButton.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 55);
            _inviteButton.GetComponent<Image>().color = theme.successColor;
        }
        
        public void Show(string userId, string username = null)
        {
            _targetUserId = userId;
            _gameService = GameService.Instance;
            
            // Set temporary username
            _usernameText.text = username ?? userId;
            
            // Reset stats
            _handsPlayedText.text = "-";
            _handsWonText.text = "-";
            _biggestPotText.text = "-";
            _winRateText.text = "-";
            _levelText.text = "Level ?";
            _xpText.text = "";
            
            gameObject.SetActive(true);
            
            // TODO: Load profile from server
            // _gameService.GetPlayerProfile(userId, OnProfileLoaded);
        }
        
        public void ShowCurrentUser()
        {
            var user = GameService.Instance?.CurrentUser;
            if (user == null)
            {
                Debug.LogWarning("No current user");
                return;
            }
            
            _targetUserId = user.id;
            _usernameText.text = user.username;
            _levelText.text = $"Level {user.level}";
            _xpText.text = $"{user.xp} XP";
            
            _handsPlayedText.text = user.handsPlayed.ToString("N0");
            _handsWonText.text = user.handsWon.ToString("N0");
            _biggestPotText.text = user.biggestPot.ToString("N0");
            
            float winRate = user.handsPlayed > 0 ? (float)user.handsWon / user.handsPlayed * 100 : 0;
            _winRateText.text = $"{winRate:F1}%";
            
            // Hide action buttons for self
            _addFriendButton.gameObject.SetActive(false);
            _inviteButton.gameObject.SetActive(false);
            
            gameObject.SetActive(true);
        }
        
        public void Hide()
        {
            gameObject.SetActive(false);
            OnClose?.Invoke();
        }
        
        private void OnAddFriendClick()
        {
            if (string.IsNullOrEmpty(_targetUserId)) return;
            
            // TODO: Call GameService.SendFriendRequest
            Debug.Log($"Sending friend request to {_targetUserId}");
            _addFriendButton.interactable = false;
            _addFriendButton.GetComponentInChildren<TextMeshProUGUI>().text = "REQUEST SENT";
        }
        
        private void OnInviteClick()
        {
            if (string.IsNullOrEmpty(_targetUserId)) return;
            
            // Check if at a table
            if (string.IsNullOrEmpty(GameService.Instance?.CurrentTableId))
            {
                Debug.LogWarning("Not at a table");
                return;
            }
            
            _gameService?.InvitePlayer(_targetUserId);
            _inviteButton.interactable = false;
            _inviteButton.GetComponentInChildren<TextMeshProUGUI>().text = "INVITED";
        }
    }
}

