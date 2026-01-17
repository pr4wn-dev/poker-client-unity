using UnityEngine;
using PokerClient.Networking;
using System.Collections.Generic;

namespace PokerClient.Game
{
    /// <summary>
    /// Controls the visual representation of the poker table
    /// </summary>
    public class TableController : MonoBehaviour
    {
        [Header("Seat Positions")]
        [SerializeField] private List<Transform> seatPositions;
        [SerializeField] private List<PlayerSeatUI> seatUIs;
        
        [Header("Community Cards")]
        [SerializeField] private Transform communityCardsParent;
        [SerializeField] private List<CardUI> communityCardUIs;
        
        [Header("Pot Display")]
        [SerializeField] private TMPro.TextMeshProUGUI potText;
        
        [Header("Prefabs")]
        [SerializeField] private GameObject cardPrefab;
        
        private TableState _currentState;
        
        public void UpdateTable(TableState state)
        {
            _currentState = state;
            
            UpdatePot(state.pot);
            UpdateCommunityCards(state.communityCards);
            UpdateSeats(state.seats, state.dealerIndex, state.currentPlayerIndex);
        }
        
        private void UpdatePot(int pot)
        {
            if (potText != null)
            {
                potText.text = pot > 0 ? $"${pot:N0}" : "";
            }
        }
        
        private void UpdateCommunityCards(List<Card> cards)
        {
            if (communityCardUIs == null) return;
            
            for (int i = 0; i < communityCardUIs.Count; i++)
            {
                if (i < cards?.Count)
                {
                    communityCardUIs[i].SetCard(cards[i]);
                    communityCardUIs[i].gameObject.SetActive(true);
                }
                else
                {
                    communityCardUIs[i].gameObject.SetActive(false);
                }
            }
        }
        
        private void UpdateSeats(List<SeatInfo> seats, int dealerIndex, int currentPlayerIndex)
        {
            if (seatUIs == null) return;
            
            for (int i = 0; i < seatUIs.Count && i < seats.Count; i++)
            {
                var seat = seats[i];
                var ui = seatUIs[i];
                
                if (seat == null || seat.IsEmpty)
                {
                    ui.SetEmpty();
                }
                else
                {
                    ui.SetPlayer(
                        seat.name,
                        seat.chips,
                        seat.currentBet,
                        seat.cards,
                        isDealer: i == dealerIndex,
                        isCurrentPlayer: i == currentPlayerIndex,
                        isFolded: seat.isFolded,
                        isAllIn: seat.isAllIn
                    );
                }
            }
        }
        
        public SeatInfo GetSeatAtPosition(int index)
        {
            if (_currentState?.seats == null || index < 0 || index >= _currentState.seats.Count)
                return null;
            return _currentState.seats[index];
        }
    }
    
    /// <summary>
    /// UI component for a single player seat
    /// Attach to each seat UI element
    /// </summary>
    public class PlayerSeatUI : MonoBehaviour
    {
        [SerializeField] private TMPro.TextMeshProUGUI nameText;
        [SerializeField] private TMPro.TextMeshProUGUI chipsText;
        [SerializeField] private TMPro.TextMeshProUGUI betText;
        [SerializeField] private GameObject dealerButton;
        [SerializeField] private GameObject turnIndicator;
        [SerializeField] private List<CardUI> holeCards;
        [SerializeField] private CanvasGroup canvasGroup;
        
        [SerializeField] private GameObject emptyState;
        [SerializeField] private GameObject occupiedState;
        
        public void SetEmpty()
        {
            if (emptyState) emptyState.SetActive(true);
            if (occupiedState) occupiedState.SetActive(false);
        }
        
        public void SetPlayer(string name, int chips, int currentBet, List<Card> cards,
            bool isDealer, bool isCurrentPlayer, bool isFolded, bool isAllIn)
        {
            if (emptyState) emptyState.SetActive(false);
            if (occupiedState) occupiedState.SetActive(true);
            
            if (nameText) nameText.text = name;
            if (chipsText) chipsText.text = $"${chips:N0}";
            if (betText) betText.text = currentBet > 0 ? $"${currentBet:N0}" : "";
            
            if (dealerButton) dealerButton.SetActive(isDealer);
            if (turnIndicator) turnIndicator.SetActive(isCurrentPlayer);
            
            // Update cards
            if (holeCards != null)
            {
                for (int i = 0; i < holeCards.Count; i++)
                {
                    if (i < cards?.Count)
                    {
                        holeCards[i].SetCard(cards[i]);
                        holeCards[i].gameObject.SetActive(!isFolded);
                    }
                    else
                    {
                        holeCards[i].gameObject.SetActive(false);
                    }
                }
            }
            
            // Dim folded players
            if (canvasGroup)
            {
                canvasGroup.alpha = isFolded ? 0.5f : 1f;
            }
        }
    }
    
    /// <summary>
    /// UI component for a single card
    /// </summary>
    public class CardUI : MonoBehaviour
    {
        [SerializeField] private UnityEngine.UI.Image cardImage;
        [SerializeField] private TMPro.TextMeshProUGUI rankText;
        [SerializeField] private TMPro.TextMeshProUGUI suitText;
        [SerializeField] private GameObject faceDown;
        [SerializeField] private GameObject faceUp;
        
        public void SetCard(Card card)
        {
            if (card == null || card.IsHidden)
            {
                ShowFaceDown();
            }
            else
            {
                ShowFaceUp(card);
            }
        }
        
        private void ShowFaceDown()
        {
            if (faceDown) faceDown.SetActive(true);
            if (faceUp) faceUp.SetActive(false);
        }
        
        private void ShowFaceUp(Card card)
        {
            if (faceDown) faceDown.SetActive(false);
            if (faceUp) faceUp.SetActive(true);
            
            if (rankText) rankText.text = card.rank;
            if (suitText)
            {
                suitText.text = card.GetSuit() switch
                {
                    CardSuit.Hearts => "♥",
                    CardSuit.Diamonds => "♦",
                    CardSuit.Clubs => "♣",
                    CardSuit.Spades => "♠",
                    _ => ""
                };
                
                // Red for hearts/diamonds, black for clubs/spades
                suitText.color = card.GetSuit() is CardSuit.Hearts or CardSuit.Diamonds
                    ? Color.red
                    : Color.black;
            }
        }
    }
}



