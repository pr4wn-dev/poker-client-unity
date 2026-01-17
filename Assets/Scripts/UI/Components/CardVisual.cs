using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PokerClient.Networking;

namespace PokerClient.UI.Components
{
    /// <summary>
    /// Visual representation of a playing card.
    /// Currently uses text rendering - easily swappable to sprite-based later.
    /// 
    /// To swap to sprites later:
    /// 1. Create a CardSpriteAtlas with all 52 cards + back
    /// 2. Add a Sprite reference and use it instead of generated visuals
    /// 3. Keep the same public interface (SetCard, SetFaceDown, etc.)
    /// </summary>
    public class CardVisual : MonoBehaviour
    {
        [Header("=== SWAP THESE FOR CUSTOM CARDS ===")]
        [Tooltip("Assign a sprite to use image-based cards instead of generated")]
        public Sprite customCardSprite;
        [Tooltip("Assign a sprite for card back")]
        public Sprite customBackSprite;
        
        [Header("Components (Auto-generated if null)")]
        [SerializeField] private Image cardBackground;
        [SerializeField] private Image cardBorder;
        [SerializeField] private TextMeshProUGUI rankTextTopLeft;
        [SerializeField] private TextMeshProUGUI suitTextTopLeft;
        [SerializeField] private TextMeshProUGUI centerSuitText;
        [SerializeField] private TextMeshProUGUI rankTextBottomRight;
        [SerializeField] private TextMeshProUGUI suitTextBottomRight;
        [SerializeField] private GameObject faceContent;
        [SerializeField] private GameObject backContent;
        
        private string _rank;
        private string _suit;
        private bool _isFaceDown = true;
        
        public string Rank => _rank;
        public string Suit => _suit;
        public bool IsFaceDown => _isFaceDown;
        
        private void Awake()
        {
            if (cardBackground == null)
                BuildCardVisual();
        }
        
        /// <summary>
        /// Build the card visual from scratch
        /// </summary>
        private void BuildCardVisual()
        {
            var theme = Theme.Current;
            var rect = GetComponent<RectTransform>();
            if (rect == null) rect = gameObject.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(theme.cardWidth, theme.cardHeight);
            
            // Card border/shadow
            var borderObj = UIFactory.CreatePanel(transform, "Border", theme.tableBorderColor);
            cardBorder = borderObj.GetComponent<Image>();
            UIFactory.FillParent(borderObj.GetComponent<RectTransform>());
            
            // Card background (white face)
            var bgObj = UIFactory.CreatePanel(transform, "Background", theme.cardFaceColor);
            cardBackground = bgObj.GetComponent<Image>();
            var bgRect = bgObj.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = new Vector2(2, 2);
            bgRect.offsetMax = new Vector2(-2, -2);
            
            // Face content container
            faceContent = new GameObject("FaceContent", typeof(RectTransform));
            faceContent.transform.SetParent(bgObj.transform, false);
            UIFactory.FillParent(faceContent.GetComponent<RectTransform>());
            
            // Top-left rank
            rankTextTopLeft = UIFactory.CreateText(faceContent.transform, "RankTL", "A", 16f, 
                theme.suitBlack, TextAlignmentOptions.TopLeft);
            var rankTLRect = rankTextTopLeft.GetComponent<RectTransform>();
            rankTLRect.anchorMin = new Vector2(0, 1);
            rankTLRect.anchorMax = new Vector2(0, 1);
            rankTLRect.pivot = new Vector2(0, 1);
            rankTLRect.anchoredPosition = new Vector2(5, -3);
            rankTLRect.sizeDelta = new Vector2(20, 20);
            rankTextTopLeft.fontStyle = FontStyles.Bold;
            
            // Top-left suit
            suitTextTopLeft = UIFactory.CreateText(faceContent.transform, "SuitTL", "♠", 14f,
                theme.suitBlack, TextAlignmentOptions.TopLeft);
            var suitTLRect = suitTextTopLeft.GetComponent<RectTransform>();
            suitTLRect.anchorMin = new Vector2(0, 1);
            suitTLRect.anchorMax = new Vector2(0, 1);
            suitTLRect.pivot = new Vector2(0, 1);
            suitTLRect.anchoredPosition = new Vector2(5, -20);
            suitTLRect.sizeDelta = new Vector2(20, 20);
            
            // Center suit (big)
            centerSuitText = UIFactory.CreateText(faceContent.transform, "CenterSuit", "♠", 36f,
                theme.suitBlack, TextAlignmentOptions.Center);
            var centerRect = centerSuitText.GetComponent<RectTransform>();
            UIFactory.Center(centerRect, new Vector2(50, 50));
            
            // Bottom-right rank (upside down)
            rankTextBottomRight = UIFactory.CreateText(faceContent.transform, "RankBR", "A", 16f,
                theme.suitBlack, TextAlignmentOptions.BottomRight);
            var rankBRRect = rankTextBottomRight.GetComponent<RectTransform>();
            rankBRRect.anchorMin = new Vector2(1, 0);
            rankBRRect.anchorMax = new Vector2(1, 0);
            rankBRRect.pivot = new Vector2(1, 0);
            rankBRRect.anchoredPosition = new Vector2(-5, 20);
            rankBRRect.sizeDelta = new Vector2(20, 20);
            rankBRRect.localRotation = Quaternion.Euler(0, 0, 180);
            rankTextBottomRight.fontStyle = FontStyles.Bold;
            
            // Bottom-right suit (upside down)
            suitTextBottomRight = UIFactory.CreateText(faceContent.transform, "SuitBR", "♠", 14f,
                theme.suitBlack, TextAlignmentOptions.BottomRight);
            var suitBRRect = suitTextBottomRight.GetComponent<RectTransform>();
            suitBRRect.anchorMin = new Vector2(1, 0);
            suitBRRect.anchorMax = new Vector2(1, 0);
            suitBRRect.pivot = new Vector2(1, 0);
            suitBRRect.anchoredPosition = new Vector2(-5, 3);
            suitBRRect.sizeDelta = new Vector2(20, 20);
            suitBRRect.localRotation = Quaternion.Euler(0, 0, 180);
            
            // Back content
            backContent = UIFactory.CreatePanel(transform, "BackContent", theme.cardBackColor);
            var backRect = backContent.GetComponent<RectTransform>();
            backRect.anchorMin = Vector2.zero;
            backRect.anchorMax = Vector2.one;
            backRect.offsetMin = new Vector2(2, 2);
            backRect.offsetMax = new Vector2(-2, -2);
            
            // Back pattern (simple diamond pattern placeholder)
            var pattern = UIFactory.CreatePanel(backContent.transform, "Pattern", theme.cardBackPattern);
            var patternRect = pattern.GetComponent<RectTransform>();
            patternRect.anchorMin = new Vector2(0.15f, 0.1f);
            patternRect.anchorMax = new Vector2(0.85f, 0.9f);
            patternRect.offsetMin = Vector2.zero;
            patternRect.offsetMax = Vector2.zero;
            
            // Default to face down
            SetFaceDown(true);
        }
        
        /// <summary>
        /// Set the card from a Card data object
        /// </summary>
        public void SetCard(Card card)
        {
            if (card == null || card.IsHidden)
            {
                SetFaceDown(true);
                return;
            }
            
            SetCard(card.rank, card.suit);
        }
        
        /// <summary>
        /// Set the card rank and suit
        /// </summary>
        public void SetCard(string rank, string suit)
        {
            _rank = rank;
            _suit = suit?.ToLower();
            
            var suitSymbol = GetSuitSymbol(_suit);
            var isRed = _suit == "hearts" || _suit == "diamonds";
            var color = isRed ? Theme.Current.suitRed : Theme.Current.suitBlack;
            
            // Update all text elements
            rankTextTopLeft.text = rank;
            rankTextTopLeft.color = color;
            
            suitTextTopLeft.text = suitSymbol;
            suitTextTopLeft.color = color;
            
            centerSuitText.text = suitSymbol;
            centerSuitText.color = color;
            
            rankTextBottomRight.text = rank;
            rankTextBottomRight.color = color;
            
            suitTextBottomRight.text = suitSymbol;
            suitTextBottomRight.color = color;
            
            SetFaceDown(false);
        }
        
        /// <summary>
        /// Show or hide the card face
        /// </summary>
        public void SetFaceDown(bool faceDown)
        {
            _isFaceDown = faceDown;
            
            if (faceContent != null)
                faceContent.SetActive(!faceDown);
            if (backContent != null)
                backContent.SetActive(faceDown);
            if (cardBackground != null)
                cardBackground.gameObject.SetActive(!faceDown);
        }
        
        /// <summary>
        /// Get the Unicode symbol for a suit
        /// </summary>
        public static string GetSuitSymbol(string suit)
        {
            return suit?.ToLower() switch
            {
                "hearts" => "♥",
                "diamonds" => "♦",
                "clubs" => "♣",
                "spades" => "♠",
                _ => "?"
            };
        }
        
        /// <summary>
        /// Create a new card visual
        /// </summary>
        public static CardVisual Create(Transform parent, string rank = null, string suit = null)
        {
            var cardObj = new GameObject("Card", typeof(RectTransform), typeof(CardVisual));
            cardObj.transform.SetParent(parent, false);
            
            var card = cardObj.GetComponent<CardVisual>();
            
            if (!string.IsNullOrEmpty(rank) && !string.IsNullOrEmpty(suit))
            {
                card.SetCard(rank, suit);
            }
            
            return card;
        }
    }
}


