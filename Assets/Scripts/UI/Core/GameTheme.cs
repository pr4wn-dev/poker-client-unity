using UnityEngine;

namespace PokerClient.UI
{
    /// <summary>
    /// Central theme configuration for the entire game.
    /// Change these values to reskin the whole game instantly.
    /// </summary>
    [CreateAssetMenu(fileName = "GameTheme", menuName = "Poker/Game Theme")]
    public class GameTheme : ScriptableObject
    {
        [Header("=== PRIMARY COLORS ===")]
        public Color primaryColor = new Color(0.15f, 0.65f, 0.45f);      // Casino green
        public Color secondaryColor = new Color(0.85f, 0.65f, 0.13f);    // Gold accent
        public Color accentColor = new Color(0.8f, 0.2f, 0.25f);         // Action red
        
        [Header("=== BACKGROUNDS ===")]
        public Color backgroundColor = new Color(0.08f, 0.1f, 0.12f);    // Dark charcoal
        public Color panelColor = new Color(0.12f, 0.14f, 0.18f);        // Slightly lighter
        public Color cardPanelColor = new Color(0.18f, 0.2f, 0.24f);     // Card area bg
        
        [Header("=== TABLE ===")]
        public Color tableColor = new Color(0.1f, 0.45f, 0.3f);          // Felt green
        public Color tableBorderColor = new Color(0.4f, 0.25f, 0.1f);    // Wood brown
        public Color potAreaColor = new Color(0.08f, 0.35f, 0.25f);      // Darker felt
        
        [Header("=== TEXT ===")]
        public Color textPrimary = Color.white;
        public Color textSecondary = new Color(0.7f, 0.7f, 0.7f);
        public Color textMuted = new Color(0.5f, 0.5f, 0.5f);
        public Color textSuccess = new Color(0.3f, 0.85f, 0.4f);
        public Color textWarning = new Color(0.95f, 0.75f, 0.2f);
        public Color textDanger = new Color(0.9f, 0.3f, 0.3f);
        
        [Header("=== BUTTONS ===")]
        public Color buttonPrimary = new Color(0.2f, 0.7f, 0.5f);
        public Color buttonPrimaryHover = new Color(0.25f, 0.8f, 0.55f);
        public Color buttonSecondary = new Color(0.3f, 0.32f, 0.38f);
        public Color buttonDanger = new Color(0.75f, 0.22f, 0.22f);
        public Color buttonDisabled = new Color(0.25f, 0.25f, 0.28f);
        
        [Header("=== CARDS ===")]
        public Color cardFaceColor = new Color(0.98f, 0.96f, 0.92f);     // Off-white
        public Color cardBackColor = new Color(0.15f, 0.25f, 0.55f);     // Blue back
        public Color cardBackPattern = new Color(0.2f, 0.3f, 0.6f);      // Pattern accent
        public Color suitRed = new Color(0.85f, 0.15f, 0.15f);           // Hearts/Diamonds
        public Color suitBlack = new Color(0.1f, 0.1f, 0.1f);            // Clubs/Spades
        
        [Header("=== CHIPS ===")]
        public Color chipWhite = new Color(0.95f, 0.95f, 0.95f);         // $1
        public Color chipRed = new Color(0.85f, 0.2f, 0.2f);             // $5
        public Color chipBlue = new Color(0.2f, 0.4f, 0.85f);            // $10
        public Color chipGreen = new Color(0.2f, 0.7f, 0.3f);            // $25
        public Color chipBlack = new Color(0.15f, 0.15f, 0.15f);         // $100
        public Color chipPurple = new Color(0.6f, 0.2f, 0.7f);           // $500
        public Color chipYellow = new Color(0.9f, 0.8f, 0.2f);           // $1000
        
        [Header("=== PLAYER STATES ===")]
        public Color playerActive = new Color(0.3f, 0.85f, 0.5f);        // Current turn
        public Color playerWaiting = new Color(0.5f, 0.5f, 0.5f);        // Waiting
        public Color playerFolded = new Color(0.3f, 0.3f, 0.3f);         // Folded
        public Color playerAllIn = new Color(0.9f, 0.6f, 0.1f);          // All-in
        public Color playerWinner = new Color(1f, 0.85f, 0.2f);          // Won hand
        
        [Header("=== RARITY COLORS ===")]
        public Color rarityCommon = new Color(0.6f, 0.6f, 0.6f);
        public Color rarityUncommon = new Color(0.3f, 0.8f, 0.3f);
        public Color rarityRare = new Color(0.3f, 0.5f, 0.9f);
        public Color rarityEpic = new Color(0.7f, 0.3f, 0.9f);
        public Color rarityLegendary = new Color(1f, 0.7f, 0.2f);
        
        [Header("=== STATUS COLORS ===")]
        public Color dangerColor = new Color(0.9f, 0.3f, 0.3f);
        public Color successColor = new Color(0.3f, 0.85f, 0.4f);
        public Color warningColor = new Color(0.95f, 0.75f, 0.2f);
        
        [Header("=== SIZING ===")]
        public float cardWidth = 80f;
        public float cardHeight = 112f;
        public float chipSize = 40f;
        public float buttonHeight = 50f;
        public float avatarSize = 60f;
        public float cornerRadius = 8f;
        
        /// <summary>
        /// Get rarity color by name
        /// </summary>
        public Color GetRarityColor(string rarity)
        {
            return rarity?.ToLower() switch
            {
                "common" => rarityCommon,
                "uncommon" => rarityUncommon,
                "rare" => rarityRare,
                "epic" => rarityEpic,
                "legendary" => rarityLegendary,
                _ => rarityCommon
            };
        }
        
        /// <summary>
        /// Get chip color by value
        /// </summary>
        public Color GetChipColor(int value)
        {
            return value switch
            {
                < 5 => chipWhite,
                < 10 => chipRed,
                < 25 => chipBlue,
                < 100 => chipGreen,
                < 500 => chipBlack,
                < 1000 => chipPurple,
                _ => chipYellow
            };
        }
    }
    
    /// <summary>
    /// Static access to the current theme
    /// </summary>
    public static class Theme
    {
        private static GameTheme _current;
        
        public static GameTheme Current
        {
            get
            {
                if (_current == null)
                {
                    _current = Resources.Load<GameTheme>("GameTheme");
                    if (_current == null)
                    {
                        // Create default theme if none exists
                        _current = ScriptableObject.CreateInstance<GameTheme>();
                    }
                }
                return _current;
            }
            set => _current = value;
        }
    }
}



