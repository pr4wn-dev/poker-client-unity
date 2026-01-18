using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PokerClient.UI;

namespace PokerClient.UI.Components
{
    /// <summary>
    /// Visual representation of a chip stack.
    /// Shows stacked chips with a value label.
    /// 
    /// To swap to 3D chips later:
    /// 1. Replace this with a 3D prefab
    /// 2. Keep the same public interface (SetValue, etc.)
    /// </summary>
    public class ChipStack : MonoBehaviour
    {
        [Header("=== SWAP THESE FOR CUSTOM CHIPS ===")]
        [Tooltip("Assign chip sprites for different denominations")]
        public Sprite[] customChipSprites;
        
        [Header("Components")]
        [SerializeField] private TextMeshProUGUI valueText;
        [SerializeField] private Image[] chipImages;
        
        private int _value;
        public int Value => _value;
        
        private const int MAX_VISIBLE_CHIPS = 5;
        
        private void Awake()
        {
            if (valueText == null)
                BuildChipStack();
        }
        
        private void BuildChipStack()
        {
            var theme = Theme.Current;
            var rect = GetComponent<RectTransform>();
            if (rect == null) rect = gameObject.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(theme.chipSize + 20, theme.chipSize + 40);
            
            // Create stacked chip visuals
            var chipContainer = new GameObject("Chips", typeof(RectTransform));
            chipContainer.transform.SetParent(transform, false);
            var chipContainerRect = chipContainer.GetComponent<RectTransform>();
            chipContainerRect.anchorMin = new Vector2(0.5f, 0.5f);
            chipContainerRect.anchorMax = new Vector2(0.5f, 0.5f);
            chipContainerRect.pivot = new Vector2(0.5f, 0.5f);
            chipContainerRect.anchoredPosition = new Vector2(0, 10);
            chipContainerRect.sizeDelta = new Vector2(theme.chipSize, theme.chipSize + 20);
            
            chipImages = new Image[MAX_VISIBLE_CHIPS];
            
            for (int i = 0; i < MAX_VISIBLE_CHIPS; i++)
            {
                var chipObj = new GameObject($"Chip{i}", typeof(RectTransform), typeof(Image));
                chipObj.transform.SetParent(chipContainer.transform, false);
                chipImages[i] = chipObj.GetComponent<Image>();
                
                var chipRect = chipObj.GetComponent<RectTransform>();
                // Make chips circular, not thin rectangles
                chipRect.sizeDelta = new Vector2(theme.chipSize * 0.8f, theme.chipSize * 0.8f);
                chipRect.anchorMin = new Vector2(0.5f, 0);
                chipRect.anchorMax = new Vector2(0.5f, 0);
                chipRect.pivot = new Vector2(0.5f, 0);
                chipRect.anchoredPosition = new Vector2(0, i * 4); // Stack with slight overlap
                
                // Set color - will be updated by SetValue
                chipImages[i].color = theme.chipGreen;
            }
            
            // Value text
            valueText = UIFactory.CreateText(transform, "Value", "0", 11f, theme.textPrimary);
            var textRect = valueText.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0);
            textRect.anchorMax = new Vector2(0.5f, 0);
            textRect.pivot = new Vector2(0.5f, 0);
            textRect.anchoredPosition = new Vector2(0, -3);
            textRect.sizeDelta = new Vector2(60, 16);
            valueText.fontStyle = FontStyles.Bold;
            
            SetValue(0);
        }
        
        /// <summary>
        /// Set the chip stack value
        /// </summary>
        public void SetValue(int value)
        {
            _value = value;
            
            if (valueText != null)
            {
                valueText.text = value > 0 ? FormatChipValue(value) : "";
            }
            
            // Determine how many chips to show based on value
            int chipsToShow = value switch
            {
                0 => 0,
                < 100 => 1,
                < 500 => 2,
                < 1000 => 3,
                < 5000 => 4,
                _ => 5
            };
            
            // Try to get chip sprite from SpriteManager
            Sprite chipSprite = null;
            if (SpriteManager.Instance != null)
            {
                chipSprite = SpriteManager.Instance.GetChipSprite(value);
            }
            
            // Get the appropriate color for the value
            var chipColor = Theme.Current.GetChipColor(value);
            
            // Update chip visuals
            for (int i = 0; i < chipImages.Length; i++)
            {
                if (chipImages[i] != null)
                {
                    chipImages[i].gameObject.SetActive(i < chipsToShow);
                    
                    if (chipSprite != null)
                    {
                        chipImages[i].sprite = chipSprite;
                        chipImages[i].color = Color.white;
                    }
                    else
                    {
                        chipImages[i].sprite = null;
                        chipImages[i].color = chipColor;
                    }
                }
            }
        }
        
        /// <summary>
        /// Format chip value for display
        /// </summary>
        public static string FormatChipValue(int value)
        {
            if (value >= 1000000)
                return $"{value / 1000000f:0.#}M";
            if (value >= 1000)
                return $"{value / 1000f:0.#}K";
            return value.ToString();
        }
        
        /// <summary>
        /// Create a new chip stack
        /// </summary>
        public static ChipStack Create(Transform parent, int value = 0)
        {
            var stackObj = new GameObject("ChipStack", typeof(RectTransform), typeof(ChipStack));
            stackObj.transform.SetParent(parent, false);
            
            var stack = stackObj.GetComponent<ChipStack>();
            stack.SetValue(value);
            
            return stack;
        }
    }
}



