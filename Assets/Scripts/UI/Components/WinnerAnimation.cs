using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

namespace PokerClient.UI.Components
{
    /// <summary>
    /// Animated winner announcement display.
    /// Shows when a player wins a pot with optional hand information.
    /// </summary>
    public class WinnerAnimation : MonoBehaviour
    {
        private RectTransform _rect;
        private CanvasGroup _canvasGroup;
        private Image _background;
        
        private TextMeshProUGUI _winnerNameText;
        private TextMeshProUGUI _amountText;
        private TextMeshProUGUI _handNameText;
        private GameObject _cardDisplay;
        private Image[] _winningCards;
        
        private ParticleSystem _confetti;
        
        public static WinnerAnimation Create(Transform parent)
        {
            var go = new GameObject("WinnerAnimation");
            go.transform.SetParent(parent, false);
            var anim = go.AddComponent<WinnerAnimation>();
            anim.Initialize();
            return anim;
        }
        
        private void Initialize()
        {
            var theme = Theme.Current;
            
            _rect = gameObject.AddComponent<RectTransform>();
            _rect.anchorMin = new Vector2(0.2f, 0.3f);
            _rect.anchorMax = new Vector2(0.8f, 0.7f);
            _rect.sizeDelta = Vector2.zero;
            
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0;
            
            // Background with gradient effect
            _background = gameObject.AddComponent<Image>();
            _background.color = new Color(0, 0, 0, 0.85f);
            
            // Content layout
            var content = UIFactory.CreatePanel(transform, "Content", Color.clear);
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.sizeDelta = Vector2.zero;
            
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 15;
            vlg.padding = new RectOffset(30, 30, 30, 30);
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            
            // "WINNER" header
            var header = UIFactory.CreateTitle(content.transform, "Header", "WINNER!", 48f);
            header.GetOrAddComponent<LayoutElement>().preferredHeight = 60;
            header.alignment = TextAlignmentOptions.Center;
            header.color = new Color(1f, 0.85f, 0.2f); // Gold
            header.fontStyle = FontStyles.Bold;
            
            // Winner name
            _winnerNameText = UIFactory.CreateTitle(content.transform, "WinnerName", "Player", 36f);
            _winnerNameText.GetOrAddComponent<LayoutElement>().preferredHeight = 50;
            _winnerNameText.alignment = TextAlignmentOptions.Center;
            _winnerNameText.color = theme.textPrimary;
            
            // Amount won
            _amountText = UIFactory.CreateTitle(content.transform, "Amount", "+1,000", 32f);
            _amountText.GetOrAddComponent<LayoutElement>().preferredHeight = 45;
            _amountText.alignment = TextAlignmentOptions.Center;
            _amountText.color = theme.successColor;
            
            // Winning cards display
            _cardDisplay = UIFactory.CreatePanel(content.transform, "CardDisplay", Color.clear);
            _cardDisplay.GetOrAddComponent<LayoutElement>().preferredHeight = 80;
            var cardsHlg = _cardDisplay.AddComponent<HorizontalLayoutGroup>();
            cardsHlg.spacing = 8;
            cardsHlg.childAlignment = TextAnchor.MiddleCenter;
            cardsHlg.childControlWidth = false;
            
            _winningCards = new Image[5];
            for (int i = 0; i < 5; i++)
            {
                var card = UIFactory.CreatePanel(_cardDisplay.transform, $"Card{i}", Color.white);
                card.GetOrAddComponent<LayoutElement>().preferredWidth = 50;
                card.GetOrAddComponent<LayoutElement>().preferredHeight = 70;
                _winningCards[i] = card.GetComponent<Image>();
                card.SetActive(false);
            }
            
            // Hand name
            _handNameText = UIFactory.CreateText(content.transform, "HandName", "Royal Flush", 24f, theme.accentColor);
            _handNameText.GetOrAddComponent<LayoutElement>().preferredHeight = 35;
            _handNameText.alignment = TextAlignmentOptions.Center;
            _handNameText.fontStyle = FontStyles.Bold;
            
            // Start hidden
            gameObject.SetActive(false);
        }
        
        /// <summary>
        /// Show the winner animation
        /// </summary>
        public void Show(string winnerName, int amount, string handName = null, bool isRoyalFlush = false)
        {
            gameObject.SetActive(true);
            
            _winnerNameText.text = winnerName;
            _amountText.text = $"+{amount:N0}";
            
            if (!string.IsNullOrEmpty(handName))
            {
                _handNameText.text = handName;
                _handNameText.gameObject.SetActive(true);
            }
            else
            {
                _handNameText.gameObject.SetActive(false);
            }
            
            // Hide card display for now (would need actual card data)
            _cardDisplay.SetActive(false);
            
            StartCoroutine(AnimateIn(isRoyalFlush));
        }
        
        /// <summary>
        /// Show with detailed pot award info
        /// </summary>
        public void Show(PotAward award)
        {
            Show(award.name, award.amount, award.handName, award.handName?.ToLower().Contains("royal") ?? false);
        }
        
        /// <summary>
        /// Hide the animation
        /// </summary>
        public void Hide()
        {
            StartCoroutine(AnimateOut());
        }
        
        private IEnumerator AnimateIn(bool special)
        {
            // Scale and fade in
            _rect.localScale = Vector3.one * 0.5f;
            _canvasGroup.alpha = 0;
            
            float duration = 0.4f;
            float elapsed = 0;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float easeT = EaseOutBack(t);
                
                _rect.localScale = Vector3.Lerp(Vector3.one * 0.5f, Vector3.one, easeT);
                _canvasGroup.alpha = Mathf.Lerp(0, 1, t);
                
                yield return null;
            }
            
            _rect.localScale = Vector3.one;
            _canvasGroup.alpha = 1;
            
            // Special effect for royal flush
            if (special)
            {
                StartCoroutine(RoyalFlushEffect());
            }
            
            // Pulse effect
            StartCoroutine(PulseEffect());
            
            // Auto-hide after delay
            yield return new WaitForSeconds(3f);
            Hide();
        }
        
        private IEnumerator AnimateOut()
        {
            float duration = 0.3f;
            float elapsed = 0;
            Vector3 startScale = _rect.localScale;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                _rect.localScale = Vector3.Lerp(startScale, Vector3.one * 1.2f, t);
                _canvasGroup.alpha = Mathf.Lerp(1, 0, t);
                
                yield return null;
            }
            
            gameObject.SetActive(false);
        }
        
        private IEnumerator PulseEffect()
        {
            float pulseSpeed = 2f;
            float pulseAmount = 0.05f;
            
            while (gameObject.activeInHierarchy && _canvasGroup.alpha > 0.5f)
            {
                float scale = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
                _rect.localScale = Vector3.one * scale;
                yield return null;
            }
        }
        
        private IEnumerator RoyalFlushEffect()
        {
            // Flash the background gold
            var originalColor = _background.color;
            var flashColor = new Color(0.6f, 0.5f, 0.1f, 0.9f);
            
            for (int i = 0; i < 3; i++)
            {
                _background.color = flashColor;
                yield return new WaitForSeconds(0.1f);
                _background.color = originalColor;
                yield return new WaitForSeconds(0.1f);
            }
            
            // Gold tint remains
            _background.color = new Color(0.2f, 0.15f, 0.05f, 0.9f);
        }
        
        private float EaseOutBack(float t)
        {
            float c1 = 1.70158f;
            float c3 = c1 + 1;
            return 1 + c3 * Mathf.Pow(t - 1, 3) + c1 * Mathf.Pow(t - 1, 2);
        }
    }
    
    /// <summary>
    /// Simple pot award data for animation
    /// </summary>
    [System.Serializable]
    public class PotAward
    {
        public string playerId;
        public string name;
        public int amount;
        public string handName;
        public string potType;
    }
}


