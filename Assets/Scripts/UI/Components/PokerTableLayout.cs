using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using PokerClient.Networking;

namespace PokerClient.UI.Components
{
    /// <summary>
    /// Poker table layout with seats arranged in an oval pattern.
    /// Handles positioning of players, community cards, and pot.
    /// </summary>
    public class PokerTableLayout : MonoBehaviour
    {
        [Header("=== SWAP THIS FOR CUSTOM TABLE ===")]
        [Tooltip("Assign a custom table sprite")]
        public Sprite customTableSprite;
        
        [Header("Configuration")]
        [SerializeField] private int maxSeats = 9;
        
        [Header("Components")]
        [SerializeField] private Image tableImage;
        [SerializeField] private Image tableBorder;
        [SerializeField] private GameObject communityCardsContainer;
        [SerializeField] private TextMeshProUGUI potText;
        [SerializeField] private ChipStack potChips;
        
        private List<PlayerSeat> _seats = new List<PlayerSeat>();
        private List<CardVisual> _communityCards = new List<CardVisual>();
        
        public IReadOnlyList<PlayerSeat> Seats => _seats;
        
        // Seat positions as normalized coordinates (0-1) relative to table
        // Arranged in oval pattern for landscape view
        private static readonly Vector2[] SEAT_POSITIONS_9 = new Vector2[]
        {
            new Vector2(0.5f, 0.02f),   // 0: Bottom center (player's seat typically)
            new Vector2(0.15f, 0.1f),   // 1: Bottom left
            new Vector2(0.02f, 0.35f),  // 2: Left lower
            new Vector2(0.02f, 0.65f),  // 3: Left upper
            new Vector2(0.15f, 0.9f),   // 4: Top left
            new Vector2(0.5f, 0.98f),   // 5: Top center
            new Vector2(0.85f, 0.9f),   // 6: Top right
            new Vector2(0.98f, 0.65f),  // 7: Right upper
            new Vector2(0.98f, 0.35f),  // 8: Right lower
        };
        
        private static readonly Vector2[] SEAT_POSITIONS_6 = new Vector2[]
        {
            new Vector2(0.5f, 0.02f),   // 0: Bottom center
            new Vector2(0.1f, 0.25f),   // 1: Left lower
            new Vector2(0.1f, 0.75f),   // 2: Left upper
            new Vector2(0.5f, 0.98f),   // 3: Top center
            new Vector2(0.9f, 0.75f),   // 4: Right upper
            new Vector2(0.9f, 0.25f),   // 5: Right lower
        };
        
        private void Awake()
        {
            if (tableImage == null)
                BuildTable();
        }
        
        private void BuildTable()
        {
            var theme = Theme.Current;
            var rect = GetComponent<RectTransform>();
            if (rect == null) rect = gameObject.AddComponent<RectTransform>();
            
            // Table border (wood frame)
            var borderObj = UIFactory.CreatePanel(transform, "TableBorder", theme.tableBorderColor);
            tableBorder = borderObj.GetComponent<Image>();
            var borderRect = borderObj.GetComponent<RectTransform>();
            UIFactory.FillParent(borderRect, 10);
            
            // Table felt
            var tableObj = UIFactory.CreatePanel(borderObj.transform, "TableFelt", theme.tableColor);
            tableImage = tableObj.GetComponent<Image>();
            var tableRect = tableObj.GetComponent<RectTransform>();
            UIFactory.FillParent(tableRect, 8);
            
            // Inner table area (darker)
            var innerObj = UIFactory.CreatePanel(tableObj.transform, "InnerTable", theme.potAreaColor);
            var innerRect = innerObj.GetComponent<RectTransform>();
            innerRect.anchorMin = new Vector2(0.2f, 0.2f);
            innerRect.anchorMax = new Vector2(0.8f, 0.8f);
            innerRect.offsetMin = Vector2.zero;
            innerRect.offsetMax = Vector2.zero;
            
            // Community cards container
            communityCardsContainer = UIFactory.CreateHorizontalGroup(innerObj.transform, "CommunityCards", 8);
            var ccRect = communityCardsContainer.GetComponent<RectTransform>();
            ccRect.anchorMin = new Vector2(0.5f, 0.55f);
            ccRect.anchorMax = new Vector2(0.5f, 0.55f);
            ccRect.pivot = new Vector2(0.5f, 0.5f);
            ccRect.anchoredPosition = Vector2.zero;
            ccRect.sizeDelta = new Vector2(400, 100);
            
            // Pot display
            var potContainer = UIFactory.CreateVerticalGroup(innerObj.transform, "PotContainer", 5);
            var potContainerRect = potContainer.GetComponent<RectTransform>();
            potContainerRect.anchorMin = new Vector2(0.5f, 0.25f);
            potContainerRect.anchorMax = new Vector2(0.5f, 0.25f);
            potContainerRect.pivot = new Vector2(0.5f, 0.5f);
            potContainerRect.anchoredPosition = Vector2.zero;
            potContainerRect.sizeDelta = new Vector2(150, 80);
            
            potText = UIFactory.CreateText(potContainer.transform, "PotText", "POT: 0", 16f, theme.textPrimary);
            potText.fontStyle = FontStyles.Bold;
            var potTextRect = potText.GetComponent<RectTransform>();
            potTextRect.sizeDelta = new Vector2(150, 25);
            
            potChips = ChipStack.Create(potContainer.transform, 0);
            
            // Create seats
            CreateSeats();
        }
        
        private void CreateSeats()
        {
            var positions = maxSeats <= 6 ? SEAT_POSITIONS_6 : SEAT_POSITIONS_9;
            var seatsToCreate = Mathf.Min(maxSeats, positions.Length);
            
            for (int i = 0; i < seatsToCreate; i++)
            {
                var seat = PlayerSeat.Create(transform, i);
                var seatRect = seat.GetComponent<RectTransform>();
                
                // Position based on normalized coordinates
                seatRect.anchorMin = positions[i];
                seatRect.anchorMax = positions[i];
                seatRect.pivot = new Vector2(0.5f, 0.5f);
                seatRect.anchoredPosition = Vector2.zero;
                
                _seats.Add(seat);
            }
        }
        
        /// <summary>
        /// Update the table state from server data
        /// </summary>
        public void UpdateState(TableState state)
        {
            if (state == null) return;
            
            // Update pot
            SetPot(state.pot);
            
            // Update community cards
            SetCommunityCards(state.communityCards);
            
            // Update seats
            if (state.seats != null)
            {
                for (int i = 0; i < _seats.Count && i < state.seats.Count; i++)
                {
                    var seatInfo = state.seats[i];
                    if (seatInfo != null)
                    {
                        _seats[i].SetPlayer(seatInfo);
                        _seats[i].SetDealer(state.dealerIndex == i);
                        _seats[i].SetCurrentTurn(state.currentPlayerIndex == i);
                    }
                    else
                    {
                        _seats[i].SetEmpty();
                    }
                }
            }
        }
        
        /// <summary>
        /// Set the pot amount
        /// </summary>
        public void SetPot(int amount)
        {
            potText.text = $"POT: {ChipStack.FormatChipValue(amount)}";
            potChips.SetValue(amount);
        }
        
        /// <summary>
        /// Set community cards
        /// </summary>
        public void SetCommunityCards(List<Card> cards)
        {
            // Clear existing cards
            foreach (var card in _communityCards)
            {
                if (card != null)
                    Destroy(card.gameObject);
            }
            _communityCards.Clear();
            
            if (cards == null) return;
            
            // Create new cards
            foreach (var cardData in cards)
            {
                var cardVisual = CardVisual.Create(communityCardsContainer.transform);
                var cardRect = cardVisual.GetComponent<RectTransform>();
                cardRect.sizeDelta = new Vector2(Theme.Current.cardWidth, Theme.Current.cardHeight);
                
                if (cardData != null && !cardData.IsHidden)
                {
                    cardVisual.SetCard(cardData);
                }
                else
                {
                    cardVisual.SetFaceDown(true);
                }
                
                _communityCards.Add(cardVisual);
            }
        }
        
        /// <summary>
        /// Get seat by index
        /// </summary>
        public PlayerSeat GetSeat(int index)
        {
            if (index >= 0 && index < _seats.Count)
                return _seats[index];
            return null;
        }
        
        /// <summary>
        /// Get seat by player ID
        /// </summary>
        public PlayerSeat GetSeatByPlayerId(string oderId)
        {
            return _seats.Find(s => s.PlayerId == oderId);
        }
        
        /// <summary>
        /// Clear all seats and community cards
        /// </summary>
        public void Clear()
        {
            foreach (var seat in _seats)
            {
                seat.SetEmpty();
            }
            
            SetCommunityCards(null);
            SetPot(0);
        }
        
        /// <summary>
        /// Create a new poker table layout
        /// </summary>
        public static PokerTableLayout Create(Transform parent, int maxSeats = 9)
        {
            var tableObj = new GameObject("PokerTable", typeof(RectTransform), typeof(PokerTableLayout));
            tableObj.transform.SetParent(parent, false);
            
            var table = tableObj.GetComponent<PokerTableLayout>();
            table.maxSeats = maxSeats;
            
            return table;
        }
    }
}


