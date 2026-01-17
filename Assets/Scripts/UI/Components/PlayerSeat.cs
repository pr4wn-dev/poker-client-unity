using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PokerClient.Networking;
using PokerClient.UI;
using System.Collections.Generic;

namespace PokerClient.UI.Components
{
    /// <summary>
    /// Visual representation of a player seat at the poker table.
    /// Shows avatar, name, chips, cards, and current action/status.
    /// </summary>
    public class PlayerSeat : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Image avatarImage;
        [SerializeField] private Image avatarBorder;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI chipsText;
        [SerializeField] private TextMeshProUGUI actionText;
        [SerializeField] private TextMeshProUGUI betText;
        [SerializeField] private GameObject dealerButton;
        [SerializeField] private GameObject cardsContainer;
        [SerializeField] private ChipStack betChips;
        [SerializeField] private Image statusOverlay;
        
        private List<CardVisual> _cards = new List<CardVisual>();
        private int _seatIndex;
        private string _oderId;
        private bool _isEmpty = true;
        private bool _isCurrentTurn;
        private bool _isFolded;
        private bool _isAllIn;
        
        public int SeatIndex => _seatIndex;
        public string PlayerId => _oderId;
        public bool IsEmpty => _isEmpty;
        public bool IsCurrentTurn => _isCurrentTurn;
        
        private void Awake()
        {
            if (avatarImage == null)
                BuildSeatVisual();
        }
        
        private void BuildSeatVisual()
        {
            var theme = Theme.Current;
            var rect = GetComponent<RectTransform>();
            if (rect == null) rect = gameObject.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(120, 140);
            
            // Main container panel
            var container = UIFactory.CreatePanel(transform, "Container", theme.panelColor);
            var containerRect = container.GetComponent<RectTransform>();
            UIFactory.FillParent(containerRect);
            
            // Avatar border (shows turn status)
            var borderObj = UIFactory.CreatePanel(container.transform, "AvatarBorder", theme.playerWaiting);
            avatarBorder = borderObj.GetComponent<Image>();
            var borderRect = borderObj.GetComponent<RectTransform>();
            borderRect.anchorMin = new Vector2(0.5f, 1);
            borderRect.anchorMax = new Vector2(0.5f, 1);
            borderRect.pivot = new Vector2(0.5f, 1);
            borderRect.anchoredPosition = new Vector2(0, -5);
            borderRect.sizeDelta = new Vector2(theme.avatarSize + 6, theme.avatarSize + 6);
            
            // Avatar
            avatarImage = UIFactory.CreateAvatar(borderObj.transform, "Avatar", theme.avatarSize);
            var avatarRect = avatarImage.GetComponent<RectTransform>();
            UIFactory.Center(avatarRect, new Vector2(theme.avatarSize, theme.avatarSize));
            
            // Name text
            nameText = UIFactory.CreateText(container.transform, "Name", "Empty", 12f, theme.textSecondary);
            var nameRect = nameText.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 1);
            nameRect.anchorMax = new Vector2(1, 1);
            nameRect.pivot = new Vector2(0.5f, 1);
            nameRect.anchoredPosition = new Vector2(0, -75);
            nameRect.sizeDelta = new Vector2(0, 18);
            
            // Chips text
            chipsText = UIFactory.CreateText(container.transform, "Chips", "", 11f, theme.secondaryColor);
            var chipsRect = chipsText.GetComponent<RectTransform>();
            chipsRect.anchorMin = new Vector2(0, 1);
            chipsRect.anchorMax = new Vector2(1, 1);
            chipsRect.pivot = new Vector2(0.5f, 1);
            chipsRect.anchoredPosition = new Vector2(0, -92);
            chipsRect.sizeDelta = new Vector2(0, 16);
            
            // Action text (shows FOLD, CALL, RAISE, etc.)
            actionText = UIFactory.CreateText(container.transform, "Action", "", 11f, theme.textPrimary);
            actionText.fontStyle = FontStyles.Bold;
            var actionRect = actionText.GetComponent<RectTransform>();
            actionRect.anchorMin = new Vector2(0, 0);
            actionRect.anchorMax = new Vector2(1, 0);
            actionRect.pivot = new Vector2(0.5f, 0);
            actionRect.anchoredPosition = new Vector2(0, 5);
            actionRect.sizeDelta = new Vector2(0, 18);
            
            // Cards container (positioned above the seat)
            cardsContainer = new GameObject("Cards", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            cardsContainer.transform.SetParent(transform, false);
            var cardsRect = cardsContainer.GetComponent<RectTransform>();
            cardsRect.anchorMin = new Vector2(0.5f, 1);
            cardsRect.anchorMax = new Vector2(0.5f, 1);
            cardsRect.pivot = new Vector2(0.5f, 0);
            cardsRect.anchoredPosition = new Vector2(0, 5);
            cardsRect.sizeDelta = new Vector2(100, 60);
            
            var cardsLayout = cardsContainer.GetComponent<HorizontalLayoutGroup>();
            cardsLayout.spacing = -20; // Overlap cards slightly
            cardsLayout.childAlignment = TextAnchor.MiddleCenter;
            
            // Dealer button
            dealerButton = UIFactory.CreatePanel(transform, "DealerButton", theme.secondaryColor);
            var dbRect = dealerButton.GetComponent<RectTransform>();
            dbRect.anchorMin = new Vector2(1, 1);
            dbRect.anchorMax = new Vector2(1, 1);
            dbRect.pivot = new Vector2(0.5f, 0.5f);
            dbRect.anchoredPosition = new Vector2(5, -20);
            dbRect.sizeDelta = new Vector2(24, 24);
            
            var dbText = UIFactory.CreateText(dealerButton.transform, "D", "D", 12f, theme.backgroundColor);
            dbText.fontStyle = FontStyles.Bold;
            var dbTextRect = dbText.GetComponent<RectTransform>();
            UIFactory.FillParent(dbTextRect);
            
            dealerButton.SetActive(false);
            
            // Status overlay (for folded, disconnected, etc.)
            var overlayObj = UIFactory.CreatePanel(container.transform, "Overlay", new Color(0, 0, 0, 0.5f));
            statusOverlay = overlayObj.GetComponent<Image>();
            UIFactory.FillParent(overlayObj.GetComponent<RectTransform>());
            statusOverlay.raycastTarget = false;
            overlayObj.SetActive(false);
            
            // Bet chips (positioned in front of seat toward table center)
            betChips = ChipStack.Create(transform, 0);
            var betChipsRect = betChips.GetComponent<RectTransform>();
            betChipsRect.anchorMin = new Vector2(0.5f, 0);
            betChipsRect.anchorMax = new Vector2(0.5f, 0);
            betChipsRect.pivot = new Vector2(0.5f, 1);
            betChipsRect.anchoredPosition = new Vector2(0, -10);
            
            SetEmpty();
        }
        
        /// <summary>
        /// Set seat as empty
        /// </summary>
        public void SetEmpty()
        {
            _isEmpty = true;
            _oderId = null;
            
            nameText.text = "Empty";
            nameText.color = Theme.Current.textMuted;
            chipsText.text = "";
            actionText.text = "";
            avatarImage.color = Theme.Current.buttonDisabled;
            avatarBorder.color = Theme.Current.playerWaiting;
            dealerButton.SetActive(false);
            statusOverlay.gameObject.SetActive(false);
            betChips.SetValue(0);
            
            ClearCards();
        }
        
        /// <summary>
        /// Set player data for this seat
        /// </summary>
        public void SetPlayer(SeatInfo seat)
        {
            if (seat == null)
            {
                SetEmpty();
                return;
            }
            
            _isEmpty = false;
            _seatIndex = seat.index;
            _oderId = seat.playerId;
            _isFolded = seat.isFolded;
            _isAllIn = seat.isAllIn;
            
            nameText.text = seat.GetDisplayName();
            nameText.color = Theme.Current.textPrimary;
            chipsText.text = ChipStack.FormatChipValue(seat.chips);
            
            // Set avatar - use bot avatar or player avatar
            if (SpriteManager.Instance != null)
            {
                Sprite avatar = seat.isBot 
                    ? SpriteManager.Instance.GetBotAvatar(seat.name)
                    : SpriteManager.Instance.GetAvatar(seat.avatarId ?? "default_1");
                    
                if (avatar != null)
                {
                    avatarImage.sprite = avatar;
                    avatarImage.color = Color.white;
                }
                else
                {
                    avatarImage.sprite = null;
                    avatarImage.color = seat.isBot ? Theme.Current.accentColor : Theme.Current.buttonSecondary;
                }
            }
            else
            {
                avatarImage.color = seat.isBot ? Theme.Current.accentColor : Theme.Current.buttonSecondary;
            }
            
            // Set bet
            betChips.SetValue(seat.currentBet);
            
            // Update status
            UpdateStatus(seat.isFolded, seat.isAllIn, seat.isConnected);
            
            // Update cards
            if (seat.cards != null && seat.cards.Count > 0)
            {
                SetCards(seat.cards);
            }
        }
        
        /// <summary>
        /// Set whether this seat is the current turn
        /// </summary>
        public void SetCurrentTurn(bool isCurrent)
        {
            _isCurrentTurn = isCurrent;
            avatarBorder.color = isCurrent ? Theme.Current.playerActive : Theme.Current.playerWaiting;
        }
        
        /// <summary>
        /// Set dealer button visibility
        /// </summary>
        public void SetDealer(bool isDealer)
        {
            dealerButton.SetActive(isDealer);
        }
        
        /// <summary>
        /// Show the last action taken
        /// </summary>
        public void ShowAction(string action, int? amount = null)
        {
            if (string.IsNullOrEmpty(action))
            {
                actionText.text = "";
                return;
            }
            
            actionText.text = amount.HasValue ? $"{action.ToUpper()} {ChipStack.FormatChipValue(amount.Value)}" : action.ToUpper();
            
            // Color based on action
            actionText.color = action.ToLower() switch
            {
                "fold" => Theme.Current.textMuted,
                "check" => Theme.Current.textSecondary,
                "call" => Theme.Current.textPrimary,
                "bet" or "raise" => Theme.Current.textWarning,
                "all in" or "allin" => Theme.Current.accentColor,
                _ => Theme.Current.textPrimary
            };
        }
        
        /// <summary>
        /// Update player status
        /// </summary>
        public void UpdateStatus(bool folded, bool allIn, bool connected)
        {
            _isFolded = folded;
            _isAllIn = allIn;
            
            if (!connected)
            {
                statusOverlay.gameObject.SetActive(true);
                statusOverlay.color = new Color(0, 0, 0, 0.6f);
            }
            else if (folded)
            {
                statusOverlay.gameObject.SetActive(true);
                statusOverlay.color = new Color(0, 0, 0, 0.4f);
            }
            else
            {
                statusOverlay.gameObject.SetActive(false);
            }
            
            if (allIn)
            {
                avatarBorder.color = Theme.Current.playerAllIn;
            }
        }
        
        /// <summary>
        /// Show as winner
        /// </summary>
        public void ShowWinner(int potWon)
        {
            avatarBorder.color = Theme.Current.playerWinner;
            actionText.text = $"WINS {ChipStack.FormatChipValue(potWon)}";
            actionText.color = Theme.Current.playerWinner;
        }
        
        /// <summary>
        /// Set the player's hole cards
        /// </summary>
        public void SetCards(List<Card> cards)
        {
            ClearCards();
            
            if (cards == null) return;
            
            foreach (var card in cards)
            {
                var cardVisual = CardVisual.Create(cardsContainer.transform);
                var cardRect = cardVisual.GetComponent<RectTransform>();
                cardRect.sizeDelta = new Vector2(45, 65); // Smaller cards for seat
                
                if (card != null && !card.IsHidden)
                {
                    cardVisual.SetCard(card);
                }
                else
                {
                    cardVisual.SetFaceDown(true);
                }
                
                _cards.Add(cardVisual);
            }
        }
        
        /// <summary>
        /// Clear all cards
        /// </summary>
        public void ClearCards()
        {
            foreach (var card in _cards)
            {
                if (card != null)
                    Destroy(card.gameObject);
            }
            _cards.Clear();
        }
        
        /// <summary>
        /// Create a new player seat
        /// </summary>
        public static PlayerSeat Create(Transform parent, int seatIndex)
        {
            var seatObj = new GameObject($"Seat_{seatIndex}", typeof(RectTransform), typeof(PlayerSeat));
            seatObj.transform.SetParent(parent, false);
            
            var seat = seatObj.GetComponent<PlayerSeat>();
            seat._seatIndex = seatIndex;
            
            return seat;
        }
    }
}


