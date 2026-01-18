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
        private static SpriteManager _instance;
        public static SpriteManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<SpriteManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("SpriteManager");
                        _instance = go.AddComponent<SpriteManager>();
                    }
                }
                return _instance;
            }
        }
        
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
        
        // Cache for individually loaded card sprites
        private Dictionary<string, Sprite> _cardSpriteCache = new Dictionary<string, Sprite>();
        private Dictionary<string, Sprite> _avatarCache = new Dictionary<string, Sprite>();
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            LoadSpritesFromResources();
        }
        
        /// <summary>
        /// Auto-load sprites from Resources folder if not assigned in Inspector
        /// </summary>
        private void LoadSpritesFromResources()
        {
            // Card back
            if (cardBackSprite == null)
                cardBackSprite = Resources.Load<Sprite>("Sprites/Cards/card_back");
            
            // Chip sprites
            if (chipWhite == null) chipWhite = Resources.Load<Sprite>("Sprites/Chips/chip_white");
            if (chipRed == null) chipRed = Resources.Load<Sprite>("Sprites/Chips/chip_red");
            if (chipBlue == null) chipBlue = Resources.Load<Sprite>("Sprites/Chips/chip_blue");
            if (chipGreen == null) chipGreen = Resources.Load<Sprite>("Sprites/Chips/chip_green");
            if (chipBlack == null) chipBlack = Resources.Load<Sprite>("Sprites/Chips/chip_black");
            if (chipPurple == null) chipPurple = Resources.Load<Sprite>("Sprites/Chips/chip_purple");
            if (chipYellow == null) chipYellow = Resources.Load<Sprite>("Sprites/Chips/chip_yellow");
            if (chipPink == null) chipPink = Resources.Load<Sprite>("Sprites/Chips/chip_pink");
            
            // Table felt
            if (tableFelt == null) tableFelt = Resources.Load<Sprite>("Sprites/UI/table_felt");
            if (dealerButton == null) dealerButton = Resources.Load<Sprite>("Sprites/UI/dealer_button");
        }
        
        /// <summary>
        /// Get sprite for a specific card
        /// </summary>
        public Sprite GetCardSprite(string rank, string suit)
        {
            string key = $"{rank}_{suit}";
            
            // Check cache first
            if (_cardSpriteCache.TryGetValue(key, out Sprite cachedSprite) && cachedSprite != null)
            {
                return cachedSprite;
            }
            
            // Try to use assigned sprites array
            if (cardFaceSprites != null && cardFaceSprites.Length == 52)
            {
                int index = GetCardIndex(rank, suit);
                if (index >= 0 && index < cardFaceSprites.Length && cardFaceSprites[index] != null)
                {
                    _cardSpriteCache[key] = cardFaceSprites[index];
                    return cardFaceSprites[index];
                }
            }
            
            // Try to load individual card sprite from Resources
            // Format: Resources/Sprites/Cards/{rank}_{suit}.png (e.g., A_hearts, K_spades, 10_diamonds)
            string resourcePath = $"Sprites/Cards/{rank}_{suit}";
            Sprite loadedSprite = Resources.Load<Sprite>(resourcePath);
            if (loadedSprite != null)
            {
                _cardSpriteCache[key] = loadedSprite;
                return loadedSprite;
            }
            
            // Generate procedural sprite as fallback
            string genKey = $"card_{rank}_{suit}";
            if (!_generatedSprites.TryGetValue(genKey, out Sprite sprite))
            {
                sprite = GenerateCardSprite(rank, suit);
                _generatedSprites[genKey] = sprite;
            }
            _cardSpriteCache[key] = sprite;
            return sprite;
        }
        
        /// <summary>
        /// Get card back sprite
        /// </summary>
        public Sprite GetCardBack()
        {
            // Always use procedural card back for reliability
            // The PNG file has import issues that cause weird patterns
            if (!_generatedSprites.TryGetValue("card_back", out Sprite sprite))
            {
                sprite = GenerateCardBackSprite();
                _generatedSprites["card_back"] = sprite;
            }
            return sprite;
        }
        
        /// <summary>
        /// Get avatar sprite by name or index
        /// </summary>
        public Sprite GetAvatar(string avatarName)
        {
            if (string.IsNullOrEmpty(avatarName)) avatarName = "default_1";
            
            // Check cache
            if (_avatarCache.TryGetValue(avatarName, out Sprite cached) && cached != null)
                return cached;
            
            // Try loading from Resources
            Sprite avatar = Resources.Load<Sprite>($"Sprites/Avatars/{avatarName}");
            if (avatar != null)
            {
                _avatarCache[avatarName] = avatar;
                return avatar;
            }
            
            // Check assigned array
            if (avatarSprites != null && avatarSprites.Length > 0)
            {
                // Try to parse index
                if (int.TryParse(avatarName.Replace("default_", ""), out int index))
                {
                    index = Mathf.Clamp(index - 1, 0, avatarSprites.Length - 1);
                    if (avatarSprites[index] != null)
                    {
                        _avatarCache[avatarName] = avatarSprites[index];
                        return avatarSprites[index];
                    }
                }
                
                // Return first available
                foreach (var s in avatarSprites)
                {
                    if (s != null)
                    {
                        _avatarCache[avatarName] = s;
                        return s;
                    }
                }
            }
            
            // Generate procedural avatar
            if (!_generatedSprites.TryGetValue($"avatar_{avatarName}", out Sprite generated))
            {
                generated = GenerateAvatarSprite(avatarName);
                _generatedSprites[$"avatar_{avatarName}"] = generated;
            }
            _avatarCache[avatarName] = generated;
            return generated;
        }
        
        /// <summary>
        /// Get bot avatar sprite
        /// </summary>
        public Sprite GetBotAvatar(string botName)
        {
            string avatarName = botName?.ToLower() switch
            {
                "tex" => "bot_tex",
                "lazy larry" => "bot_larry",
                "pickles" => "bot_pickles",
                _ => $"bot_{botName?.ToLower()}"
            };
            
            return GetAvatar(avatarName);
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
            int width = 55;
            int height = 75;
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
            int width = 55;
            int height = 75;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            
            // Simple navy blue card back - NO border (sprites have their own)
            Color backColor = new Color(0.15f, 0.2f, 0.5f);
            Color patternColor = new Color(0.2f, 0.28f, 0.6f);
            Color[] pixels = new Color[width * height];
            
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Simple diagonal stripe pattern
                    bool pattern = ((x + y) / 6) % 2 == 0;
                    pixels[y * width + x] = pattern ? patternColor : backColor;
                }
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
        
        private Sprite GenerateAvatarSprite(string name)
        {
            int size = 128;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            
            // Generate a unique color based on the name
            int hash = name?.GetHashCode() ?? 0;
            Color bgColor = new Color(
                0.3f + (Mathf.Abs(hash % 70) / 100f),
                0.3f + (Mathf.Abs((hash >> 8) % 70) / 100f),
                0.3f + (Mathf.Abs((hash >> 16) % 70) / 100f),
                1f
            );
            
            Color[] pixels = new Color[size * size];
            float center = size / 2f;
            float radius = size / 2f - 4;
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    
                    if (dist < radius)
                    {
                        // Simple face - head circle
                        if (dist < radius * 0.7f && dy < 0)
                        {
                            // Face area (top part)
                            pixels[y * size + x] = bgColor * 1.2f;
                        }
                        else if (dist < radius * 0.4f && dy > radius * 0.1f)
                        {
                            // Body area (bottom part)
                            pixels[y * size + x] = bgColor * 0.9f;
                        }
                        else
                        {
                            pixels[y * size + x] = bgColor;
                        }
                    }
                    else if (dist < radius + 3)
                    {
                        // Border
                        pixels[y * size + x] = bgColor * 0.5f;
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

