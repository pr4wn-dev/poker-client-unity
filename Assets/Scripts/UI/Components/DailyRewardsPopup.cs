using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PokerClient.Networking;
using System.Collections.Generic;

namespace PokerClient.UI.Components
{
    /// <summary>
    /// Daily rewards popup showing 7-day login streak rewards.
    /// </summary>
    public class DailyRewardsPopup : MonoBehaviour
    {
        private RectTransform _rect;
        private CanvasGroup _canvasGroup;
        private GameService _gameService;
        
        private Transform rewardsContainer;
        private Button claimButton;
        private TextMeshProUGUI claimButtonText;
        private TextMeshProUGUI streakText;
        private TextMeshProUGUI timerText;
        
        private int _currentDay = 1;
        private bool _canClaim = true;
        
        public System.Action<DailyReward> OnRewardClaimed;
        public System.Action OnClose;
        
        // Reward data
        private static readonly DailyReward[] DAILY_REWARDS = new DailyReward[]
        {
            new DailyReward { day = 1, chips = 5000, xp = 50 },
            new DailyReward { day = 2, chips = 7500, xp = 75 },
            new DailyReward { day = 3, chips = 10000, xp = 100, bonus = "Mystery Box" },
            new DailyReward { day = 4, chips = 15000, xp = 125 },
            new DailyReward { day = 5, chips = 20000, xp = 150, gems = 5 },
            new DailyReward { day = 6, chips = 30000, xp = 200 },
            new DailyReward { day = 7, chips = 50000, xp = 300, gems = 20, bonus = "Epic Item" }
        };
        
        public static DailyRewardsPopup Create(Transform parent)
        {
            var go = new GameObject("DailyRewardsPopup");
            go.transform.SetParent(parent, false);
            var popup = go.AddComponent<DailyRewardsPopup>();
            popup.Initialize();
            return popup;
        }
        
        private void Initialize()
        {
            var theme = Theme.Current;
            
            _rect = gameObject.AddComponent<RectTransform>();
            _rect.anchorMin = Vector2.zero;
            _rect.anchorMax = Vector2.one;
            _rect.sizeDelta = Vector2.zero;
            
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
            // Dimmed background
            var bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.85f);
            
            // Main panel
            var panel = UIFactory.CreatePanel(transform, "Panel", theme.cardPanelColor);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.1f, 0.15f);
            panelRect.anchorMax = new Vector2(0.9f, 0.85f);
            panelRect.sizeDelta = Vector2.zero;
            
            BuildHeader(panel.transform);
            BuildRewardsGrid(panel.transform);
            BuildFooter(panel.transform);
            
            gameObject.SetActive(false);
        }
        
        private void BuildHeader(Transform parent)
        {
            var theme = Theme.Current;
            
            var header = UIFactory.CreatePanel(parent, "Header", Color.clear);
            var headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 0.85f);
            headerRect.anchorMax = Vector2.one;
            headerRect.sizeDelta = Vector2.zero;
            
            var title = UIFactory.CreateTitle(header.transform, "Title", "🎁 DAILY REWARDS", 36f);
            title.color = theme.accentColor;
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.02f, 0);
            titleRect.anchorMax = new Vector2(0.6f, 1);
            titleRect.sizeDelta = Vector2.zero;
            
            streakText = UIFactory.CreateText(header.transform, "Streak", "🔥 Day 1", 22f, theme.warningColor);
            streakText.fontStyle = FontStyles.Bold;
            var streakRect = streakText.GetComponent<RectTransform>();
            streakRect.anchorMin = new Vector2(0.6f, 0.2f);
            streakRect.anchorMax = new Vector2(0.85f, 0.8f);
            streakRect.sizeDelta = Vector2.zero;
            streakText.alignment = TextAlignmentOptions.Center;
            
            var closeBtn = UIFactory.CreateButton(header.transform, "Close", "✕", Hide);
            var closeRect = closeBtn.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.92f, 0.2f);
            closeRect.anchorMax = new Vector2(0.98f, 0.8f);
            closeRect.sizeDelta = Vector2.zero;
            closeBtn.GetComponent<Image>().color = theme.dangerColor;
        }
        
        private void BuildRewardsGrid(Transform parent)
        {
            var theme = Theme.Current;
            
            var grid = UIFactory.CreatePanel(parent, "Grid", Color.clear);
            rewardsContainer = grid.transform;
            var gridRect = grid.GetComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0.02f, 0.18f);
            gridRect.anchorMax = new Vector2(0.98f, 0.83f);
            gridRect.sizeDelta = Vector2.zero;
            
            var hlg = grid.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 12;
            hlg.padding = new RectOffset(15, 15, 15, 15);
            hlg.childControlWidth = true;
            hlg.childForceExpandWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandHeight = true;
            
            for (int i = 0; i < DAILY_REWARDS.Length; i++)
            {
                CreateRewardCard(DAILY_REWARDS[i]);
            }
        }
        
        private void CreateRewardCard(DailyReward reward)
        {
            var theme = Theme.Current;
            
            bool isClaimed = reward.day < _currentDay;
            bool isToday = reward.day == _currentDay;
            bool isLocked = reward.day > _currentDay;
            
            Color bgColor = isClaimed ? theme.successColor * 0.3f :
                           isToday ? theme.primaryColor :
                           theme.backgroundColor;
            
            var card = UIFactory.CreatePanel(rewardsContainer, $"Day_{reward.day}", bgColor);
            
            if (isToday)
            {
                var outline = card.AddComponent<Outline>();
                outline.effectColor = theme.accentColor;
                outline.effectDistance = new Vector2(3, 3);
            }
            
            var vlg = card.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 5;
            vlg.padding = new RectOffset(8, 8, 10, 10);
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            
            // Day label
            var dayText = UIFactory.CreateText(card.transform, "Day", $"DAY {reward.day}", 14f, 
                isToday ? Color.white : theme.textSecondary);
            dayText.fontStyle = FontStyles.Bold;
            dayText.GetOrAddComponent<LayoutElement>().preferredHeight = 25;
            dayText.alignment = TextAlignmentOptions.Center;
            
            // Icon
            string icon = reward.day == 7 ? "🎁" : "🪙";
            if (isClaimed) icon = "✓";
            var iconText = UIFactory.CreateText(card.transform, "Icon", icon, isClaimed ? 28f : 36f, 
                isClaimed ? theme.successColor : Color.white);
            iconText.GetOrAddComponent<LayoutElement>().preferredHeight = 50;
            iconText.alignment = TextAlignmentOptions.Center;
            
            // Chips
            var chipsText = UIFactory.CreateText(card.transform, "Chips", $"+{reward.chips:N0}", 
                isToday ? 16f : 14f, isLocked ? theme.textSecondary : theme.successColor);
            chipsText.GetOrAddComponent<LayoutElement>().preferredHeight = 22;
            chipsText.alignment = TextAlignmentOptions.Center;
            
            // XP
            var xpText = UIFactory.CreateText(card.transform, "XP", $"+{reward.xp} XP", 12f, 
                isLocked ? theme.textSecondary : theme.textPrimary);
            xpText.GetOrAddComponent<LayoutElement>().preferredHeight = 18;
            xpText.alignment = TextAlignmentOptions.Center;
            
            // Bonus if any
            if (!string.IsNullOrEmpty(reward.bonus))
            {
                var bonusText = UIFactory.CreateText(card.transform, "Bonus", reward.bonus, 11f, 
                    isLocked ? theme.textSecondary : theme.warningColor);
                bonusText.GetOrAddComponent<LayoutElement>().preferredHeight = 16;
                bonusText.alignment = TextAlignmentOptions.Center;
            }
            
            // Gems if any
            if (reward.gems > 0)
            {
                var gemsText = UIFactory.CreateText(card.transform, "Gems", $"💎 +{reward.gems}", 13f, 
                    isLocked ? theme.textSecondary : new Color(0.8f, 0.2f, 0.9f));
                gemsText.GetOrAddComponent<LayoutElement>().preferredHeight = 18;
                gemsText.alignment = TextAlignmentOptions.Center;
            }
            
            // Lock overlay
            if (isLocked)
            {
                var cg = card.AddComponent<CanvasGroup>();
                cg.alpha = 0.5f;
            }
        }
        
        private void BuildFooter(Transform parent)
        {
            var theme = Theme.Current;
            
            var footer = UIFactory.CreatePanel(parent, "Footer", Color.clear);
            var footerRect = footer.GetComponent<RectTransform>();
            footerRect.anchorMin = new Vector2(0.2f, 0.03f);
            footerRect.anchorMax = new Vector2(0.8f, 0.15f);
            footerRect.sizeDelta = Vector2.zero;
            
            var vlg = footer.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            
            // Claim button
            var claimGo = UIFactory.CreateButton(footer.transform, "Claim", "CLAIM REWARD!", OnClaimClick);
            claimGo.GetOrAddComponent<LayoutElement>().preferredHeight = 55;
            claimButton = claimGo.GetComponent<Button>();
            claimButton.GetComponent<Image>().color = theme.successColor;
            claimButtonText = claimButton.GetComponentInChildren<TextMeshProUGUI>();
            claimButtonText.fontSize = 22;
            
            // Timer text
            timerText = UIFactory.CreateText(footer.transform, "Timer", "", 14f, theme.textSecondary);
            timerText.GetOrAddComponent<LayoutElement>().preferredHeight = 20;
            timerText.alignment = TextAlignmentOptions.Center;
        }
        
        public void Show(int currentDay = 1, bool canClaim = true)
        {
            _currentDay = Mathf.Clamp(currentDay, 1, 7);
            _canClaim = canClaim;
            _gameService = GameService.Instance;
            
            streakText.text = $"🔥 Day {_currentDay}";
            
            claimButton.interactable = canClaim;
            claimButtonText.text = canClaim ? "CLAIM REWARD!" : "ALREADY CLAIMED";
            timerText.text = canClaim ? "" : "Next reward available in 23:45:30";
            
            // Rebuild grid with current day
            foreach (Transform child in rewardsContainer)
            {
                Destroy(child.gameObject);
            }
            for (int i = 0; i < DAILY_REWARDS.Length; i++)
            {
                CreateRewardCard(DAILY_REWARDS[i]);
            }
            
            gameObject.SetActive(true);
        }
        
        public void Hide()
        {
            gameObject.SetActive(false);
            OnClose?.Invoke();
        }
        
        private void OnClaimClick()
        {
            if (!_canClaim) return;
            
            var reward = DAILY_REWARDS[_currentDay - 1];
            
            claimButton.interactable = false;
            claimButtonText.text = "CLAIMING...";
            
            // TODO: Call server to claim reward
            // For now, simulate success
            OnRewardClaimed?.Invoke(reward);
            
            ToastNotification.Show($"Claimed Day {_currentDay} reward: +{reward.chips:N0} chips, +{reward.xp} XP!", ToastType.Success);
            
            _canClaim = false;
            claimButtonText.text = "CLAIMED!";
            timerText.text = "Come back tomorrow for more rewards!";
        }
    }
    
    [System.Serializable]
    public class DailyReward
    {
        public int day;
        public int chips;
        public int xp;
        public int gems;
        public string bonus;
    }
}

