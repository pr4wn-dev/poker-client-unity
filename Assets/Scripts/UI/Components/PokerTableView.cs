using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PokerClient.Networking;
using PokerClient.UI;
using PokerClient.Core;
using System.Collections.Generic;
using System.Collections;

namespace PokerClient.UI.Components
{
    /// <summary>
    /// Visual representation of the poker table with seats, community cards, and pot.
    /// </summary>
    public class PokerTableView : MonoBehaviour
    {
        private List<PlayerSeatView> _seats = new List<PlayerSeatView>();
        private List<CardView> _communityCards = new List<CardView>();
        private TextMeshProUGUI _potText;
        private TextMeshProUGUI _dealerButtonText;
        private int _maxPlayers;
        
        private RectTransform _rect;
        private GameObject _tableCenter;
        
        public void Initialize(int maxPlayers)
        {
            _maxPlayers = maxPlayers;
            _rect = gameObject.AddComponent<RectTransform>();
            
            var theme = Theme.Current;
            
            // Background
            var bg = UIFactory.CreatePanel(transform, "TableBackground", theme.backgroundColor);
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            
            // Table felt (center oval)
            _tableCenter = CreateTableFelt();
            
            // Community cards area
            CreateCommunityCardsArea();
            
            // Pot display
            CreatePotDisplay();
            
            // Create player seats
            CreateSeats(maxPlayers);
        }
        
        private GameObject CreateTableFelt()
        {
            var felt = UIFactory.CreatePanel(transform, "TableFelt", new Color(0.1f, 0.4f, 0.2f));
            var feltRect = felt.GetComponent<RectTransform>();
            feltRect.anchorMin = new Vector2(0.12f, 0.18f);
            feltRect.anchorMax = new Vector2(0.88f, 0.78f);
            feltRect.sizeDelta = Vector2.zero;
            
            // Add rounded appearance (could use sprite with rounded corners)
            
            return felt;
        }
        
        private void CreateCommunityCardsArea()
        {
            var cardsArea = UIFactory.CreatePanel(_tableCenter.transform, "CommunityCards", Color.clear);
            var cardsRect = cardsArea.GetComponent<RectTransform>();
            cardsRect.anchorMin = new Vector2(0.2f, 0.35f);
            cardsRect.anchorMax = new Vector2(0.8f, 0.65f);
            cardsRect.sizeDelta = Vector2.zero;
            
            var hlg = cardsArea.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8; // Slightly tighter to fit larger cards
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childForceExpandWidth = false;
            
            // Create 5 card slots - LARGER cards for better visibility on mobile
            var communityCardSize = new Vector2(72, 100); // Bigger than default theme size (55x77)
            for (int i = 0; i < 5; i++)
            {
                var cardView = CardView.Create(cardsArea.transform, $"CommunityCard{i}", communityCardSize);
                cardView.SetEmpty();
                _communityCards.Add(cardView);
            }
        }
        
        private void CreatePotDisplay()
        {
            var theme = Theme.Current;
            
            var potPanel = UIFactory.CreatePanel(_tableCenter.transform, "PotPanel", new Color(0, 0, 0, 0.5f));
            var potRect = potPanel.GetComponent<RectTransform>();
            potRect.anchorMin = new Vector2(0.35f, 0.78f);  // Moved up to avoid covering community cards
            potRect.anchorMax = new Vector2(0.65f, 0.93f);
            potRect.sizeDelta = Vector2.zero;
            
            _potText = UIFactory.CreateTitle(potPanel.transform, "PotText", "Pot: 0", 28f);
            _potText.alignment = TextAlignmentOptions.Center;
            _potText.color = theme.accentColor;
            var textRect = _potText.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
        }
        
        private void CreateSeats(int count)
        {
            // Position seats around the table
            // Positions are normalized (0-1) relative to the canvas
            var seatPositions = GetSeatPositions(count);
            
            for (int i = 0; i < count; i++)
            {
                var seatView = PlayerSeatView.Create(transform, $"Seat{i}", i);
                seatView.SetPosition(seatPositions[i]);
                _seats.Add(seatView);
            }
        }
        
        private List<Vector2> GetSeatPositions(int count)
        {
            // Standard poker table layout for different player counts
            // Positions are anchor positions (0-1)
            // Well-spaced for mobile - NO overlaps
            
            // 9-player layout - side players pushed to corners
            if (count >= 9)
            {
                return new List<Vector2>
                {
                    new Vector2(0.50f, 0.12f),  // 0: Bottom center (player)
                    new Vector2(0.25f, 0.15f),  // 1: Bottom left
                    new Vector2(0.06f, 0.28f),  // 2: Left lower
                    new Vector2(0.06f, 0.78f),  // 3: Left upper (HIGHER)
                    new Vector2(0.25f, 0.90f),  // 4: Top left
                    new Vector2(0.75f, 0.90f),  // 5: Top right
                    new Vector2(0.94f, 0.78f),  // 6: Right upper (HIGHER)
                    new Vector2(0.94f, 0.28f),  // 7: Right lower
                    new Vector2(0.75f, 0.15f),  // 8: Bottom right
                };
            }
            else if (count >= 6)
            {
                return new List<Vector2>
                {
                    new Vector2(0.50f, 0.12f),  // Bottom center
                    new Vector2(0.06f, 0.28f),  // Left lower
                    new Vector2(0.06f, 0.78f),  // Top left (HIGHER)
                    new Vector2(0.50f, 0.90f),  // Top center
                    new Vector2(0.94f, 0.78f),  // Top right (HIGHER)
                    new Vector2(0.94f, 0.28f),  // Right lower
                };
            }
            else
            {
                // Heads-up or small game
                return new List<Vector2>
                {
                    new Vector2(0.50f, 0.12f),  // Bottom (you)
                    new Vector2(0.50f, 0.82f),  // Top (opponent)
                };
            }
        }
        
        public void UpdateFromState(TableState state, int mySeatIndex = -1)
        {
            if (state == null) return;
            
            // Log for comparing normal vs simulation
            Debug.Log($"[TABLE-VIEW] UpdateFromState | phase={state.phase} | isSimulation={state.isSimulation} | " +
                $"mySeatIndex={mySeatIndex} | seatCount={state.seats?.Count ?? 0} | " +
                $"communityCards={state.communityCards?.Count ?? 0}");
            
            // Update pot
            _potText.text = $"Pot: {ChipStack.FormatChipValue((int)state.pot)}";
            
            // Update community cards
            UpdateCommunityCards(state.communityCards);
            
            // Update seats
            if (_seats == null || state.seats == null) return;
            
            // Use maxPlayers for rotation to avoid wrapping issues
            int maxSeats = _maxPlayers;
            
            for (int visualIndex = 0; visualIndex < _seats.Count; visualIndex++)
            {
                if (_seats[visualIndex] == null) continue;
                
                // Rotate seats so player's seat is always at visual position 0 (bottom center)
                // If mySeatIndex is 3, then:
                //   visual 0 -> server seat 3 (me)
                //   visual 1 -> server seat 4
                //   visual 2 -> server seat 5
                //   etc.
                int serverSeatIndex;
                if (mySeatIndex >= 0 && maxSeats > 0)
                {
                    serverSeatIndex = (visualIndex + mySeatIndex) % maxSeats;
                }
                else
                {
                    serverSeatIndex = visualIndex;
                }
                
                // Check bounds and if seat has a player
                if (serverSeatIndex < state.seats.Count && state.seats[serverSeatIndex] != null && 
                    !string.IsNullOrEmpty(state.seats[serverSeatIndex].playerId))
                {
                    bool isCurrentTurn = state.currentPlayerId == state.seats[serverSeatIndex].playerId;
                    bool isDealer = state.dealerIndex == serverSeatIndex;
                    _seats[visualIndex].UpdateFromState(state.seats[serverSeatIndex], isCurrentTurn, isDealer);
                }
                else
                {
                    _seats[visualIndex].SetEmpty();
                }
            }
        }
        
        private void UpdateCommunityCards(List<Card> cards)
        {
            for (int i = 0; i < _communityCards.Count; i++)
            {
                if (cards != null && i < cards.Count && cards[i] != null)
                {
                    _communityCards[i].SetCard(cards[i]);
                }
                else
                {
                    _communityCards[i].SetEmpty();
                }
            }
        }
        
        public void ShowPlayerAction(string oderId, string action, int? amount)
        {
            foreach (var seat in _seats)
            {
                if (seat.PlayerId == oderId)
                {
                    seat.ShowAction(action, amount);
                    break;
                }
            }
        }
    }
    
    /// <summary>
    /// Visual representation of a player seat.
    /// </summary>
    public class PlayerSeatView : MonoBehaviour
    {
        public string PlayerId { get; private set; }
        public int SeatIndex { get; private set; }
        
        private Image _background;
        private TextMeshProUGUI _nameText;
        private TextMeshProUGUI _chipsText;
        private TextMeshProUGUI _actionText;
        private TextMeshProUGUI _betText;
        private ChipStack _betChips;
        private List<CardView> _holeCards = new List<CardView>();
        private GameObject _dealerButton;
        private Image _turnIndicator;
        
        private RectTransform _rect;
        
        public static PlayerSeatView Create(Transform parent, string name, int seatIndex)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<PlayerSeatView>();
            view.Initialize(seatIndex);
            return view;
        }
        
        private void Initialize(int seatIndex)
        {
            SeatIndex = seatIndex;
            
            _rect = gameObject.AddComponent<RectTransform>();
            _rect.sizeDelta = new Vector2(100, 110); // Bigger seats
            
            var theme = Theme.Current;
            
            // Background
            _background = gameObject.AddComponent<Image>();
            _background.color = theme.cardPanelColor;
            
            // Turn indicator (border)
            var turnBorder = UIFactory.CreatePanel(transform, "TurnIndicator", Color.clear);
            var borderRect = turnBorder.GetComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.sizeDelta = new Vector2(4, 4);
            _turnIndicator = turnBorder.GetComponent<Image>();
            
            // Name at top - manually positioned
            _nameText = UIFactory.CreateText(transform, "Name", "Empty", 11f, theme.textSecondary);
            var nameRect = _nameText.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 1);
            nameRect.anchorMax = new Vector2(1, 1);
            nameRect.pivot = new Vector2(0.5f, 1);
            nameRect.anchoredPosition = new Vector2(0, -4);
            nameRect.sizeDelta = new Vector2(0, 16);
            _nameText.alignment = TextAlignmentOptions.Center;
            
            // Hole cards - Use LARGER cards for seat 0 (player's own seat)
            // Seat 0 is always the player's perspective after rotation
            bool isPlayerSeat = (seatIndex == 0);
            Vector2 cardSize = isPlayerSeat ? new Vector2(105, 147) : new Vector2(70, 98);
            float xSpacing = isPlayerSeat ? 42 : 28;
            
            // Chips - position differently for player seat vs other seats
            _chipsText = UIFactory.CreateText(transform, "Chips", "", 12f, new Color(1f, 0.85f, 0.2f));
            var chipsRect = _chipsText.GetComponent<RectTransform>();
            _chipsText.fontStyle = FontStyles.Bold;
            
            if (isPlayerSeat)
            {
                // Player's seat chips - hidden here, shown in dedicated panel above action bar
                _chipsText.gameObject.SetActive(false);
            }
            else
            {
                // Other players: chips below name as before
                chipsRect.anchorMin = new Vector2(0, 1);
                chipsRect.anchorMax = new Vector2(1, 1);
                chipsRect.pivot = new Vector2(0.5f, 1);
                chipsRect.anchoredPosition = new Vector2(0, -20);
                chipsRect.sizeDelta = new Vector2(0, 16);
                _chipsText.alignment = TextAlignmentOptions.Center;
            }
            
            for (int i = 0; i < 2; i++)
            {
                var cardView = CardView.Create(transform, $"Card{i}", cardSize);
                var cardRect = cardView.GetComponent<RectTransform>();
                // Force the size directly
                cardRect.sizeDelta = cardSize;
                cardRect.anchorMin = new Vector2(0.5f, 0);
                cardRect.anchorMax = new Vector2(0.5f, 0);
                cardRect.pivot = new Vector2(0.5f, 0.3f); // Higher pivot = cards sit higher
                float xOffset = (i == 0) ? -xSpacing : xSpacing;
                cardRect.anchoredPosition = new Vector2(xOffset, isPlayerSeat ? -15 : 0);
                cardView.SetHidden();
                _holeCards.Add(cardView);
            }
            
            // Action text above cards - manually positioned
            _actionText = UIFactory.CreateText(transform, "Action", "", 9f, theme.primaryColor);
            var actionRect = _actionText.GetComponent<RectTransform>();
            actionRect.anchorMin = new Vector2(0, 0);
            actionRect.anchorMax = new Vector2(1, 0);
            actionRect.pivot = new Vector2(0.5f, 0);
            actionRect.anchoredPosition = new Vector2(0, 48);
            actionRect.sizeDelta = new Vector2(0, 14);
            _actionText.alignment = TextAlignmentOptions.Center;
            _actionText.fontStyle = FontStyles.Bold;
            _actionText.gameObject.SetActive(false);
            
            // Bet chips - positioned outside seat toward table center
            _betChips = ChipStack.Create(transform, 0);
            var betChipsRect = _betChips.GetComponent<RectTransform>();
            betChipsRect.anchorMin = new Vector2(0.5f, 0.5f);
            betChipsRect.anchorMax = new Vector2(0.5f, 0.5f);
            betChipsRect.pivot = new Vector2(0.5f, 0.5f);
            betChipsRect.anchoredPosition = new Vector2(50, 0);
            betChipsRect.localScale = new Vector3(0.8f, 0.8f, 1f);
            
            // Bet text
            _betText = UIFactory.CreateText(transform, "Bet", "", 10f, theme.accentColor);
            var betRect = _betText.GetComponent<RectTransform>();
            betRect.anchorMin = new Vector2(0.5f, 0);
            betRect.anchorMax = new Vector2(0.5f, 0);
            betRect.pivot = new Vector2(0.5f, 1);
            betRect.anchoredPosition = new Vector2(0, -30);
            betRect.sizeDelta = new Vector2(60, 16);
            _betText.alignment = TextAlignmentOptions.Center;
            _betText.gameObject.SetActive(false);
            
            // Dealer button
            _dealerButton = UIFactory.CreatePanel(transform, "DealerButton", Color.white);
            var dbRect = _dealerButton.GetComponent<RectTransform>();
            dbRect.anchorMin = new Vector2(1, 1);
            dbRect.anchorMax = new Vector2(1, 1);
            dbRect.pivot = new Vector2(1, 1);
            dbRect.anchoredPosition = new Vector2(3, 3);
            dbRect.sizeDelta = new Vector2(20, 20);
            
            var dbText = UIFactory.CreateText(_dealerButton.transform, "D", "D", 11f, Color.black);
            dbText.alignment = TextAlignmentOptions.Center;
            var dbTextRect = dbText.GetComponent<RectTransform>();
            dbTextRect.anchorMin = Vector2.zero;
            dbTextRect.anchorMax = Vector2.one;
            dbTextRect.sizeDelta = Vector2.zero;
            
            _dealerButton.SetActive(false);
            
            SetEmpty();
        }
        
        public void SetPosition(Vector2 normalizedPosition)
        {
            _rect.anchorMin = normalizedPosition;
            _rect.anchorMax = normalizedPosition;
            _rect.pivot = new Vector2(0.5f, 0.5f);
            _rect.anchoredPosition = Vector2.zero;
        }
        
        public void SetEmpty()
        {
            PlayerId = null;
            _nameText.text = "Empty";
            _nameText.color = Theme.Current.textSecondary;
            _chipsText.text = "";
            _actionText.gameObject.SetActive(false);
            _betText.gameObject.SetActive(false);
            _betChips.SetValue(0);
            _dealerButton.SetActive(false);
            _turnIndicator.color = Color.clear;
            
            foreach (var card in _holeCards)
            {
                card.SetEmpty();
            }
            
            _background.color = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        }
        
        public void UpdateFromState(SeatInfo info, bool isCurrentTurn, bool isDealer)
        {
            // Log for comparing normal vs simulation card positions
            Debug.Log($"[SEAT-VIEW] UpdateFromState | player={info?.name ?? "null"} | " +
                $"cardCount={info?.cards?.Count ?? 0} | isFolded={info?.isFolded} | isAllIn={info?.isAllIn} | " +
                $"seatPos={GetComponent<RectTransform>()?.anchoredPosition} | " +
                $"seatSize={GetComponent<RectTransform>()?.sizeDelta}");
            
            if (info == null)
            {
                SetEmpty();
                return;
            }
            
            PlayerId = info.playerId;
            _nameText.text = info.GetDisplayName();
            _nameText.color = info.isFolded ? Theme.Current.textSecondary : Theme.Current.textPrimary;
            _chipsText.text = ChipStack.FormatChipValueFull((int)info.chips);
            
            // Hole cards - log each card position
            Debug.Log($"[SEAT-VIEW] Updating cards | player={info.name} | cards={info.cards?.Count ?? 0}");
            if (info.cards != null && info.cards.Count > 0)
            {
                for (int i = 0; i < _holeCards.Count && i < info.cards.Count; i++)
                {
                    if (info.cards[i] != null)
                    {
                        _holeCards[i].SetCard(info.cards[i]);
                    }
                    else
                    {
                        _holeCards[i].SetHidden();
                    }
                }
            }
            else
            {
                foreach (var card in _holeCards)
                {
                    card.SetHidden();
                }
            }
            
            // Current bet - show chip stack and text
            if (info.currentBet > 0)
            {
                _betChips.SetValue((int)info.currentBet);
                _betText.text = ChipStack.FormatChipValue((int)info.currentBet);
                _betText.gameObject.SetActive(true);
            }
            else
            {
                _betChips.SetValue(0);
                _betText.gameObject.SetActive(false);
            }
            
            // Turn indicator
            _turnIndicator.color = isCurrentTurn ? Theme.Current.primaryColor : Color.clear;
            
            // Dealer button
            _dealerButton.SetActive(isDealer);
            
            // Folded state
            if (info.isFolded)
            {
                _background.color = new Color(0.15f, 0.15f, 0.15f, 0.7f);
                foreach (var card in _holeCards)
                {
                    card.SetEmpty();
                }
            }
            else if (info.isAllIn)
            {
                _background.color = new Color(0.5f, 0.2f, 0.2f, 0.8f);
            }
            else
            {
                _background.color = Theme.Current.cardPanelColor;
            }
        }
        
        public void ShowAction(string action, int? amount)
        {
            string text = action.ToUpper();
            if (amount.HasValue && amount.Value > 0)
            {
                text += $" {ChipStack.FormatChipValue(amount.Value)}";
            }
            
            _actionText.text = text;
            _actionText.gameObject.SetActive(true);
            
            // Color based on action
            _actionText.color = action.ToLower() switch
            {
                "fold" => Theme.Current.dangerColor,
                "check" => Theme.Current.successColor,
                "call" => Theme.Current.primaryColor,
                "bet" or "raise" => Theme.Current.accentColor,
                "allin" or "all-in" => new Color(0.8f, 0.2f, 0.5f),
                _ => Theme.Current.textPrimary
            };
            
            // Auto-hide after 2 seconds
            StartCoroutine(HideActionAfterDelay());
        }
        
        private System.Collections.IEnumerator HideActionAfterDelay()
        {
            yield return new WaitForSeconds(2f);
            _actionText.gameObject.SetActive(false);
        }
    }
    
    /// <summary>
    /// Visual representation of a playing card.
    /// </summary>
    public class CardView : MonoBehaviour
    {
        private Image _background;
        private TextMeshProUGUI _rankText;
        private TextMeshProUGUI _suitText;
        private RectTransform _rect;
        private bool _wasEmpty = true;
        private bool _wasHidden = false;
        private Coroutine _animationCoroutine = null; // Track running animation
        private Vector3 _originalPosition; // Store original position to prevent drift during animation
        private bool _hasOriginalPosition = false;
        
        public static CardView Create(Transform parent, string name, Vector2? size = null)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var view = go.AddComponent<CardView>();
            view.Initialize(size ?? new Vector2(Theme.Current.cardWidth, Theme.Current.cardHeight));
            return view;
        }
        
        private void Initialize(Vector2 size)
        {
            _rect = gameObject.AddComponent<RectTransform>();
            _rect.sizeDelta = size;
            
            var layout = gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = size.x;
            layout.preferredHeight = size.y;
            layout.minWidth = size.x;
            layout.minHeight = size.y;
            
            // Card background - NO preserveAspect so cards fill the full size
            _background = gameObject.AddComponent<Image>();
            _background.color = Color.white;
            _background.preserveAspect = false;
            _background.type = Image.Type.Simple;
            
            // Rank (top-left) - smaller text for compact cards
            _rankText = UIFactory.CreateText(transform, "Rank", "", 12f, Color.black);
            var rankRect = _rankText.GetComponent<RectTransform>();
            rankRect.anchorMin = new Vector2(0.05f, 0.65f);
            rankRect.anchorMax = new Vector2(0.95f, 0.95f);
            rankRect.sizeDelta = Vector2.zero;
            _rankText.alignment = TextAlignmentOptions.TopLeft;
            _rankText.fontStyle = FontStyles.Bold;
            
            // Suit (center) - smaller text
            _suitText = UIFactory.CreateText(transform, "Suit", "", 18f, Color.black);
            var suitRect = _suitText.GetComponent<RectTransform>();
            suitRect.anchorMin = new Vector2(0, 0.15f);
            suitRect.anchorMax = new Vector2(1, 0.65f);
            suitRect.sizeDelta = Vector2.zero;
            _suitText.alignment = TextAlignmentOptions.Center;
            
            SetEmpty();
        }
        
        public void SetCard(Card card)
        {
            // Log card position for debugging normal vs simulation differences
            Debug.Log($"[CARD-VIEW] SetCard | card={(card != null ? $"{card.rank}{card.suit}" : "null")} | " +
                $"hidden={card?.IsHidden ?? true} | pos={_rect?.anchoredPosition} | size={_rect?.sizeDelta} | " +
                $"scale={_rect?.localScale} | rotation={_rect?.localRotation.eulerAngles} | " +
                $"parent={transform.parent?.name ?? "null"}");
            
            if (card == null || card.IsHidden)
            {
                SetHidden();
                return;
            }
            
            // CRITICAL: Stop any running animation before updating the card
            // This prevents cards from getting stuck mid-animation
            if (_animationCoroutine != null)
            {
                StopCoroutine(_animationCoroutine);
                _animationCoroutine = null;
                // Reset to ORIGINAL position if animation was interrupted
                // This prevents the position drift bug where mid-animation positions accumulate
                if (_rect != null)
                {
                    _rect.localScale = Vector3.one;
                    _rect.localRotation = Quaternion.identity;
                    // CRITICAL FIX: Reset to original position to prevent drift
                    if (_hasOriginalPosition)
                    {
                        _rect.anchoredPosition = _originalPosition;
                    }
                }
            }
            
            // Store original position if not already stored
            if (!_hasOriginalPosition && _rect != null)
            {
                _originalPosition = _rect.anchoredPosition;
                _hasOriginalPosition = true;
            }
            
            bool wasEmpty = _wasEmpty;
            bool wasHidden = _wasHidden;
            gameObject.SetActive(true);
            
            // Try to use sprite from SpriteManager
            if (SpriteManager.Instance != null)
            {
                var cardSprite = SpriteManager.Instance.GetCardSprite(card.rank, card.suit);
                if (cardSprite != null)
                {
                    _background.sprite = cardSprite;
                    _background.color = Color.white;
                    _rankText.gameObject.SetActive(false);
                    _suitText.gameObject.SetActive(false);
                    
                    // Play sound and animate if card is being revealed
                    if (wasEmpty || wasHidden)
                    {
                        PlayCardRevealSoundAndAnimation();
                    }
                    
                    _wasEmpty = false;
                    _wasHidden = false;
                    return;
                }
            }
            
            // Fallback to text rendering
            _background.sprite = null;
            _background.color = Color.white;
            
            string rank = card.rank?.ToUpper() ?? "?";
            string suit = GetSuitSymbol(card.suit);
            Color suitColor = GetSuitColor(card.suit);
            
            _rankText.text = rank;
            _rankText.color = suitColor;
            _rankText.gameObject.SetActive(true);
            
            _suitText.text = suit;
            _suitText.color = suitColor;
            _suitText.gameObject.SetActive(true);
            
            // Play sound and animate if card is being revealed
            if (wasEmpty || wasHidden)
            {
                PlayCardRevealSoundAndAnimation();
            }
            
            _wasEmpty = false;
            _wasHidden = false;
        }
        
        private void PlayCardRevealSoundAndAnimation()
        {
            // Play card flip/deal sound
            if (Core.AudioManager.Instance != null)
            {
                Core.AudioManager.Instance.PlayCardFlip();
            }
            
            // Animate card appearance - slide in from top, flip, and scale up
            if (_rect != null)
            {
                // Stop any existing animation first
                if (_animationCoroutine != null)
                {
                    StopCoroutine(_animationCoroutine);
                }
                _animationCoroutine = StartCoroutine(AnimateCardReveal());
            }
        }
        
        private System.Collections.IEnumerator AnimateCardReveal()
        {
            // CRITICAL FIX: Use stored original position as target, NOT current position
            // This prevents the drift bug where mid-animation positions would accumulate
            Vector3 targetPosition = _hasOriginalPosition ? _originalPosition : _rect.anchoredPosition;
            Vector3 targetScale = Vector3.one; // Always animate to normal size (1,1,1)
            
            // Start animation: card slides down from above, flips, and scales up
            float duration = 0.4f;
            float elapsed = 0f;
            
            // Start position: slightly above and rotated
            Vector3 startPos = targetPosition + new Vector3(0, 100f, 0);
            Vector3 startScale = new Vector3(0.3f, 0.3f, 1f);
            Quaternion startRotation = Quaternion.Euler(0, 90f, 0); // Flipped 90 degrees
            Quaternion endRotation = Quaternion.identity;
            
            // Set initial animation state
            _rect.anchoredPosition = startPos;
            _rect.localScale = startScale;
            _rect.localRotation = startRotation;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // Ease out cubic for smooth animation
                float easedT = 1f - Mathf.Pow(1f - t, 3f);
                
                // Interpolate position (slide down) - use targetPosition
                _rect.anchoredPosition = Vector3.Lerp(startPos, targetPosition, easedT);
                
                // Interpolate scale (grow from small)
                _rect.localScale = Vector3.Lerp(startScale, targetScale, easedT);
                
                // Interpolate rotation (flip)
                _rect.localRotation = Quaternion.Slerp(startRotation, endRotation, easedT);
                
                yield return null;
            }
            
            // Ensure final state is exact (no floating point errors)
            _rect.anchoredPosition = targetPosition;
            _rect.localScale = targetScale;
            _rect.localRotation = endRotation;
            
            // Clear the animation coroutine reference
            _animationCoroutine = null;
        }
        
        public void SetHidden()
        {
            _wasHidden = true;
            gameObject.SetActive(true);
            _rankText.gameObject.SetActive(false);
            _suitText.gameObject.SetActive(false);
            
            // Try to use card back sprite
            if (SpriteManager.Instance != null)
            {
                var cardBack = SpriteManager.Instance.GetCardBack();
                if (cardBack != null)
                {
                    _background.sprite = cardBack;
                    _background.color = Color.white;
                    return;
                }
            }
            
            // Fallback to blue color
            _background.sprite = null;
            _background.color = new Color(0.2f, 0.3f, 0.6f);
        }
        
        public void SetEmpty()
        {
            _wasEmpty = true;
            _wasHidden = false;
            gameObject.SetActive(true);
            _background.sprite = null;
            _background.color = new Color(0.3f, 0.3f, 0.3f, 0.3f);
            _background.preserveAspect = false;
            _rankText.gameObject.SetActive(false);
            _suitText.gameObject.SetActive(false);
            // DO NOT reset sizeDelta here - let the caller control the size
            
            // Reset transform for animation
            if (_rect != null)
            {
                _rect.localScale = Vector3.one;
                _rect.localRotation = Quaternion.identity;
            }
        }
        
        private string GetSuitSymbol(string suit)
        {
            return suit?.ToLower() switch
            {
                "hearts" or "h" => "♥",
                "diamonds" or "d" => "♦",
                "clubs" or "c" => "♣",
                "spades" or "s" => "♠",
                _ => "?"
            };
        }
        
        private Color GetSuitColor(string suit)
        {
            return suit?.ToLower() switch
            {
                "hearts" or "h" or "diamonds" or "d" => Color.red,
                _ => Color.black
            };
        }
    }
}

