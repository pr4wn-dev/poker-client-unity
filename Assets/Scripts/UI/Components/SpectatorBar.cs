using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PokerClient.Networking;

namespace PokerClient.UI.Components
{
    /// <summary>
    /// Top bar showing when in spectator mode.
    /// Shows table info and provides option to join.
    /// </summary>
    public class SpectatorBar : MonoBehaviour
    {
        private RectTransform _rect;
        private CanvasGroup _canvasGroup;
        private GameService _gameService;
        
        private TextMeshProUGUI _statusText;
        private TextMeshProUGUI _tableInfoText;
        private TextMeshProUGUI _spectatorCountText;
        private Button _joinButton;
        private Button _leaveButton;
        
        public System.Action OnJoinRequested;
        public System.Action OnLeaveRequested;
        
        public static SpectatorBar Create(Transform parent)
        {
            var go = new GameObject("SpectatorBar");
            go.transform.SetParent(parent, false);
            var bar = go.AddComponent<SpectatorBar>();
            bar.Initialize();
            return bar;
        }
        
        private void Initialize()
        {
            var theme = Theme.Current;
            
            _rect = gameObject.AddComponent<RectTransform>();
            _rect.anchorMin = new Vector2(0, 1);
            _rect.anchorMax = new Vector2(1, 1);
            _rect.pivot = new Vector2(0.5f, 1);
            _rect.anchoredPosition = Vector2.zero;
            _rect.sizeDelta = new Vector2(0, 45);
            
            var bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.1f, 0.2f, 0.95f);
            
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
            // Status icon/text
            var statusIcon = UIFactory.CreateText(transform, "StatusIcon", "👁️", 24f, Color.white);
            statusIcon.alignment = TextAlignmentOptions.Center;
            var iconRect = statusIcon.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.01f, 0.1f);
            iconRect.anchorMax = new Vector2(0.04f, 0.9f);
            iconRect.sizeDelta = Vector2.zero;
            
            _statusText = UIFactory.CreateText(transform, "Status", "SPECTATING", 18f, theme.warningColor);
            _statusText.fontStyle = FontStyles.Bold;
            var statusRect = _statusText.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.05f, 0.1f);
            statusRect.anchorMax = new Vector2(0.2f, 0.9f);
            statusRect.sizeDelta = Vector2.zero;
            
            // Table info
            _tableInfoText = UIFactory.CreateText(transform, "TableInfo", "Table Name • $10/$20", 16f, theme.textPrimary);
            var infoRect = _tableInfoText.GetComponent<RectTransform>();
            infoRect.anchorMin = new Vector2(0.22f, 0.1f);
            infoRect.anchorMax = new Vector2(0.55f, 0.9f);
            infoRect.sizeDelta = Vector2.zero;
            
            // Spectator count
            _spectatorCountText = UIFactory.CreateText(transform, "SpectatorCount", "2 spectators", 14f, theme.textSecondary);
            _spectatorCountText.alignment = TextAlignmentOptions.Center;
            var countRect = _spectatorCountText.GetComponent<RectTransform>();
            countRect.anchorMin = new Vector2(0.56f, 0.1f);
            countRect.anchorMax = new Vector2(0.7f, 0.9f);
            countRect.sizeDelta = Vector2.zero;
            
            // Join button
            _joinButton = UIFactory.CreateButton(transform, "Join", "JOIN TABLE", OnJoinClick).GetComponent<Button>();
            var joinRect = _joinButton.GetComponent<RectTransform>();
            joinRect.anchorMin = new Vector2(0.72f, 0.15f);
            joinRect.anchorMax = new Vector2(0.87f, 0.85f);
            joinRect.sizeDelta = Vector2.zero;
            _joinButton.GetComponent<Image>().color = theme.successColor;
            
            // Leave button
            _leaveButton = UIFactory.CreateButton(transform, "Leave", "LEAVE", OnLeaveClick).GetComponent<Button>();
            var leaveRect = _leaveButton.GetComponent<RectTransform>();
            leaveRect.anchorMin = new Vector2(0.88f, 0.15f);
            leaveRect.anchorMax = new Vector2(0.98f, 0.85f);
            leaveRect.sizeDelta = Vector2.zero;
            _leaveButton.GetComponent<Image>().color = theme.dangerColor;
            
            gameObject.SetActive(false);
        }
        
        public void Show(TableState state)
        {
            if (state == null) return;
            
            _gameService = GameService.Instance;
            
            _tableInfoText.text = $"{state.name ?? "Table"} • ${state.smallBlind}/{state.bigBlind}";
            UpdateSpectatorCount(state.spectatorCount);
            
            // Check if table has open seats
            bool canJoin = false;
            if (state.seats != null)
            {
                foreach (var seat in state.seats)
                {
                    if (seat == null || string.IsNullOrEmpty(seat.playerId))
                    {
                        canJoin = true;
                        break;
                    }
                }
            }
            _joinButton.interactable = canJoin;
            
            gameObject.SetActive(true);
        }
        
        public void Hide()
        {
            gameObject.SetActive(false);
        }
        
        public void UpdateSpectatorCount(int count)
        {
            _spectatorCountText.text = count == 1 ? "1 spectator" : $"{count} spectators";
        }
        
        private void OnJoinClick()
        {
            OnJoinRequested?.Invoke();
        }
        
        private void OnLeaveClick()
        {
            OnLeaveRequested?.Invoke();
        }
    }
}

