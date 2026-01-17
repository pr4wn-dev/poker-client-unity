using UnityEngine;
using System.Collections.Generic;

namespace PokerClient.UI.Components
{
    /// <summary>
    /// Manages card sprites and other game visuals.
    /// Falls back to procedural graphics when sprites aren't available.
    /// </summary>
    public class SpriteManager : MonoBehaviour
    {
        public static SpriteManager Instance { get; private set; }
        
        [Header("Card Sprites")]
        [Tooltip("Optional: Sprite for card backs")]
        public Sprite cardBackSprite;
        
        [Tooltip("Optional: Array of 52 card face sprites (ordered: clubs, diamonds, hearts, spades, each A-K)")]
        public Sprite[] cardFaceSprites;
        
        [Header("Chip Sprites")]
        public Sprite chipWhite;      // 1
        public Sprite chipRed;        // 5
        public Sprite chipBlue;       // 10
        public Sprite chipGreen;      // 25
        public Sprite chipBlack;      // 100
        public Sprite chipPurple;     // 500
        public Sprite chipYellow;     // 1000
        public Sprite chipPink;       // 5000
        
        [Header("UI Sprites")]
        public Sprite dealerButton;
        public Sprite tableFelt;
        public Sprite buttonNormal;
        public Sprite buttonHighlight;
        public Sprite buttonPressed;
        
        [Header("Avatar Sprites")]
        public Sprite[] avatarSprites;
        
        // Procedurally generated textures as fallback
        private Dictionary<string, Texture2D> _generatedTextures = new Dictionary<string, Texture2D>();
        private Dictionary<string, Sprite> _generatedSprites = new Dictionary<string, Sprite>();
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        /// <summary>
        /// Get sprite for a specific card
        /// </summary>
        public Sprite GetCardSprite(string rank, string suit)
        {
            // Try to use assigned sprites
            if (cardFaceSprites != null && cardFaceSprites.Length == 52)
            {
                int index = GetCardIndex(rank, suit);
                if (index >= 0 && index < cardFaceSprites.Length)
                {
                    return cardFaceSprites[index];
                }
            }
            
            // Generate procedural sprite
            string key = $"card_{rank}_{suit}";
            if (!_generatedSprites.TryGetValue(key, out Sprite sprite))
            {
                sprite = GenerateCardSprite(rank, suit);
                _generatedSprites[key] = sprite;
            }
            return sprite;
        }
        
        /// <summary>
        /// Get card back sprite
        /// </summary>
        public Sprite GetCardBack()
        {
            if (cardBackSprite != null) return cardBackSprite;
            
            if (!_generatedSprites.TryGetValue("card_back", out Sprite sprite))
            {
                sprite = GenerateCardBackSprite();
                _generatedSprites["card_back"] = sprite;
            }
            return sprite;
        }
        
        /// <summary>
        /// Get chip sprite by value
        /// </summary>
        public Sprite GetChipSprite(int value)
        {
            // Use assigned sprites if available
            if (value >= 5000 && chipPink != null) return chipPink;
            if (value >= 1000 && chipYellow != null) return chipYellow;
            if (value >= 500 && chipPurple != null) return chipPurple;
            if (value >= 100 && chipBlack != null) return chipBlack;
            if (value >= 25 && chipGreen != null) return chipGreen;
            if (value >= 10 && chipBlue != null) return chipBlue;
            if (value >= 5 && chipRed != null) return chipRed;
            if (chipWhite != null) return chipWhite;
            
            // Generate procedural chip
            Color chipColor = GetChipColor(value);
            string key = $"chip_{value}";
            if (!_generatedSprites.TryGetValue(key, out Sprite sprite))
            {
                sprite = GenerateChipSprite(chipColor);
                _generatedSprites[key] = sprite;
            }
            return sprite;
        }
        
        /// <summary>
        /// Get table felt sprite
        /// </summary>
        public Sprite GetTableFelt()
        {
            if (tableFelt != null) return tableFelt;
            
            if (!_generatedSprites.TryGetValue("table_felt", out Sprite sprite))
            {
                sprite = GenerateTableFeltSprite();
                _generatedSprites["table_felt"] = sprite;
            }
            return sprite;
        }
        
        private int GetCardIndex(string rank, string suit)
        {
            // Index cards as: suit * 13 + rank
            // Suits: clubs=0, diamonds=1, hearts=2, spades=3
            // Ranks: A=0, 2-10=1-9, J=10, Q=11, K=12
            
            int suitIndex = suit?.ToLower() switch
            {
                "clubs" or "c" => 0,
                "diamonds" or "d" => 1,
                "hearts" or "h" => 2,
                "spades" or "s" => 3,
                _ => -1
            };
            
            int rankIndex = rank?.ToUpper() switch
            {
                "A" => 0,
                "2" => 1,
                "3" => 2,
                "4" => 3,
                "5" => 4,
                "6" => 5,
                "7" => 6,
                "8" => 7,
                "9" => 8,
                "10" => 9,
                "J" => 10,
                "Q" => 11,
                "K" => 12,
                _ => -1
            };
            
            if (suitIndex < 0 || rankIndex < 0) return -1;
            return suitIndex * 13 + rankIndex;
        }
        
        private Color GetChipColor(int value)
        {
            return value switch
            {
                >= 5000 => new Color(1f, 0.4f, 0.7f),    // Pink
                >= 1000 => new Color(1f, 0.85f, 0.2f),   // Yellow
                >= 500 => new Color(0.6f, 0.2f, 0.8f),   // Purple
                >= 100 => new Color(0.1f, 0.1f, 0.1f),   // Black
                >= 25 => new Color(0.2f, 0.7f, 0.3f),    // Green
                >= 10 => new Color(0.2f, 0.4f, 0.8f),    // Blue
                >= 5 => new Color(0.9f, 0.2f, 0.2f),     // Red
                _ => Color.white                          // White
            };
        }
        
        #region Procedural Sprite Generation
        
        private Sprite GenerateCardSprite(string rank, string suit)
        {
            int width = 60;
            int height = 84;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            
            // White background
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.white;
            }
            
            // Add border
            for (int x = 0; x < width; x++)
            {
                pixels[x] = Color.gray; // Bottom
                pixels[(height - 1) * width + x] = Color.gray; // Top
            }
            for (int y = 0; y < height; y++)
            {
                pixels[y * width] = Color.gray; // Left
                pixels[y * width + width - 1] = Color.gray; // Right
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100);
        }
        
        private Sprite GenerateCardBackSprite()
        {
            int width = 60;
            int height = 84;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            
            Color backColor = new Color(0.15f, 0.25f, 0.5f);
            Color patternColor = new Color(0.2f, 0.35f, 0.6f);
            Color[] pixels = new Color[width * height];
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Diamond pattern
                    bool isDiamond = ((x + y) % 8 < 4) ^ ((x - y + 100) % 8 < 4);
                    pixels[y * width + x] = isDiamond ? patternColor : backColor;
                }
            }
            
            // Border
            for (int x = 0; x < width; x++)
            {
                pixels[x] = Color.white;
                pixels[(height - 1) * width + x] = Color.white;
            }
            for (int y = 0; y < height; y++)
            {
                pixels[y * width] = Color.white;
                pixels[y * width + width - 1] = Color.white;
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100);
        }
        
        private Sprite GenerateChipSprite(Color chipColor)
        {
            int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            
            Color[] pixels = new Color[size * size];
            float center = size / 2f;
            float radius = size / 2f - 2;
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    
                    if (dist < radius - 4)
                    {
                        pixels[y * size + x] = chipColor;
                    }
                    else if (dist < radius)
                    {
                        // Edge stripe pattern
                        float angle = Mathf.Atan2(dy, dx);
                        bool stripe = Mathf.Sin(angle * 16) > 0;
                        pixels[y * size + x] = stripe ? Color.white : chipColor * 0.8f;
                    }
                    else
                    {
                        pixels[y * size + x] = Color.clear;
                    }
                }
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
        }
        
        private Sprite GenerateTableFeltSprite()
        {
            int size = 256;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Repeat;
            
            Color feltColor = new Color(0.1f, 0.35f, 0.2f);
            Color[] pixels = new Color[size * size];
            
            // Add subtle noise for felt texture
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float noise = Random.Range(-0.02f, 0.02f);
                    pixels[y * size + x] = new Color(
                        feltColor.r + noise,
                        feltColor.g + noise,
                        feltColor.b + noise,
                        1f
                    );
                }
            }
            
            texture.SetPixels(pixels);
            texture.Apply();
            
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
        }
        
        #endregion
        
        private void OnDestroy()
        {
            // Clean up generated textures
            foreach (var tex in _generatedTextures.Values)
            {
                if (tex != null) Destroy(tex);
            }
            foreach (var sprite in _generatedSprites.Values)
            {
                if (sprite != null && sprite.texture != null)
                {
                    Destroy(sprite.texture);
                }
            }
        }
    }
}

