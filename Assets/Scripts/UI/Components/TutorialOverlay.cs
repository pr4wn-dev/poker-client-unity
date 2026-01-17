using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace PokerClient.UI.Components
{
    /// <summary>
    /// Tutorial overlay for guiding new players through the game.
    /// </summary>
    public class TutorialOverlay : MonoBehaviour
    {
        public static TutorialOverlay Instance { get; private set; }
        
        private RectTransform _rect;
        private CanvasGroup _canvasGroup;
        
        private Image _highlightMask;
        private RectTransform _tooltipPanel;
        private TextMeshProUGUI _titleText;
        private TextMeshProUGUI _descriptionText;
        private TextMeshProUGUI _stepText;
        private Button _nextButton;
        private Button _skipButton;
        
        private List<TutorialStep> _steps = new List<TutorialStep>();
        private int _currentStepIndex = 0;
        
        private System.Action _onComplete;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            Initialize();
        }
        
        private void Initialize()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 996;
            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            gameObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            gameObject.AddComponent<GraphicRaycaster>();
            
            // Container
            var container = new GameObject("Container");
            container.transform.SetParent(transform, false);
            _rect = container.AddComponent<RectTransform>();
            _rect.anchorMin = Vector2.zero;
            _rect.anchorMax = Vector2.one;
            _rect.sizeDelta = Vector2.zero;
            
            _canvasGroup = container.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0;
            _canvasGroup.blocksRaycasts = false;
            
            // Semi-transparent overlay
            var overlay = container.AddComponent<Image>();
            overlay.color = new Color(0, 0, 0, 0.7f);
            
            var theme = Theme.Current;
            
            // Tooltip panel
            var tooltip = UIFactory.CreatePanel(container.transform, "Tooltip", theme.cardPanelColor);
            _tooltipPanel = tooltip.GetComponent<RectTransform>();
            _tooltipPanel.anchorMin = new Vector2(0.25f, 0.6f);
            _tooltipPanel.anchorMax = new Vector2(0.75f, 0.85f);
            _tooltipPanel.sizeDelta = Vector2.zero;
            
            var vlg = tooltip.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 15;
            vlg.padding = new RectOffset(25, 25, 20, 20);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            
            // Title
            _titleText = UIFactory.CreateTitle(tooltip.transform, "Title", "Welcome!", 32f);
            _titleText.GetOrAddComponent<LayoutElement>().preferredHeight = 45;
            _titleText.alignment = TextAlignmentOptions.Center;
            _titleText.color = theme.accentColor;
            
            // Description
            _descriptionText = UIFactory.CreateText(tooltip.transform, "Desc", "Description here", 18f, theme.textPrimary);
            _descriptionText.GetOrAddComponent<LayoutElement>().preferredHeight = 80;
            _descriptionText.alignment = TextAlignmentOptions.Center;
            _descriptionText.enableWordWrapping = true;
            
            // Buttons row
            var btnRow = UIFactory.CreatePanel(tooltip.transform, "Buttons", Color.clear);
            btnRow.GetOrAddComponent<LayoutElement>().preferredHeight = 55;
            var hlg = btnRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            
            _skipButton = UIFactory.CreateButton(btnRow.transform, "Skip", "SKIP TUTORIAL", OnSkipClick).GetComponent<Button>();
            _skipButton.GetComponent<RectTransform>().sizeDelta = new Vector2(180, 45);
            _skipButton.GetComponent<Image>().color = theme.dangerColor;
            
            _nextButton = UIFactory.CreateButton(btnRow.transform, "Next", "NEXT →", OnNextClick).GetComponent<Button>();
            _nextButton.GetComponent<RectTransform>().sizeDelta = new Vector2(140, 45);
            _nextButton.GetComponent<Image>().color = theme.primaryColor;
            
            // Step indicator
            _stepText = UIFactory.CreateText(tooltip.transform, "Step", "Step 1 of 5", 14f, theme.textSecondary);
            _stepText.GetOrAddComponent<LayoutElement>().preferredHeight = 20;
            _stepText.alignment = TextAlignmentOptions.Center;
        }
        
        /// <summary>
        /// Start a tutorial with the given steps.
        /// </summary>
        public static void StartTutorial(List<TutorialStep> steps, System.Action onComplete = null)
        {
            if (Instance == null)
            {
                var go = new GameObject("TutorialOverlay");
                go.AddComponent<TutorialOverlay>();
            }
            
            Instance.Start(steps, onComplete);
        }
        
        /// <summary>
        /// Quick tutorial for poker basics.
        /// </summary>
        public static void ShowPokerBasics(System.Action onComplete = null)
        {
            var steps = new List<TutorialStep>
            {
                new TutorialStep { title = "🃏 Welcome to Poker!", description = "Let's learn the basics of Texas Hold'em poker. It's easy and fun!" },
                new TutorialStep { title = "📋 The Goal", description = "Make the best 5-card hand using your 2 hole cards and 5 community cards. The best hand wins the pot!" },
                new TutorialStep { title = "🎯 Hand Rankings", description = "From highest to lowest: Royal Flush, Straight Flush, Four of a Kind, Full House, Flush, Straight, Three of a Kind, Two Pair, One Pair, High Card." },
                new TutorialStep { title = "💰 Betting Rounds", description = "There are 4 betting rounds: Pre-Flop, Flop (3 cards), Turn (4th card), and River (5th card)." },
                new TutorialStep { title = "🎮 Your Actions", description = "Fold (give up), Check (pass), Call (match bet), Bet/Raise (increase), All-In (bet everything)!" },
                new TutorialStep { title = "✨ Ready to Play!", description = "That's the basics! Join a table and start playing. Good luck!" }
            };
            
            StartTutorial(steps, onComplete);
        }
        
        /// <summary>
        /// Tutorial for adventure mode.
        /// </summary>
        public static void ShowAdventureBasics(System.Action onComplete = null)
        {
            var steps = new List<TutorialStep>
            {
                new TutorialStep { title = "👹 Adventure Mode", description = "Challenge AI bosses in heads-up poker battles! Defeat them to earn XP and items." },
                new TutorialStep { title = "🗺️ World Map", description = "Explore different areas, each with unique bosses of increasing difficulty." },
                new TutorialStep { title = "⬆️ Level Up", description = "Earn XP to level up and unlock new areas and bosses." },
                new TutorialStep { title = "💎 Rewards", description = "Defeating bosses grants chips, XP, and sometimes rare items!" },
                new TutorialStep { title = "🎯 Strategy", description = "Each boss has a unique play style. Learn their patterns to defeat them!" }
            };
            
            StartTutorial(steps, onComplete);
        }
        
        private void Start(List<TutorialStep> steps, System.Action onComplete)
        {
            _steps = steps;
            _currentStepIndex = 0;
            _onComplete = onComplete;
            
            if (_steps.Count > 0)
            {
                ShowStep(0);
                _canvasGroup.alpha = 1;
                _canvasGroup.blocksRaycasts = true;
            }
        }
        
        private void ShowStep(int index)
        {
            if (index < 0 || index >= _steps.Count)
            {
                Hide();
                return;
            }
            
            _currentStepIndex = index;
            var step = _steps[index];
            
            _titleText.text = step.title;
            _descriptionText.text = step.description;
            _stepText.text = $"Step {index + 1} of {_steps.Count}";
            
            bool isLast = index == _steps.Count - 1;
            _nextButton.GetComponentInChildren<TextMeshProUGUI>().text = isLast ? "FINISH" : "NEXT →";
        }
        
        private void Hide()
        {
            _canvasGroup.alpha = 0;
            _canvasGroup.blocksRaycasts = false;
            _onComplete?.Invoke();
            _onComplete = null;
        }
        
        private void OnNextClick()
        {
            if (_currentStepIndex >= _steps.Count - 1)
            {
                Hide();
            }
            else
            {
                ShowStep(_currentStepIndex + 1);
            }
        }
        
        private void OnSkipClick()
        {
            Hide();
        }
    }
    
    [System.Serializable]
    public class TutorialStep
    {
        public string title;
        public string description;
        public RectTransform highlightTarget;  // Optional: element to highlight
        public Vector2 tooltipOffset;
    }
}

