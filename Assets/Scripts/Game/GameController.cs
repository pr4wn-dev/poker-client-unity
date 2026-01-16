using UnityEngine;
using PokerClient.Networking;

namespace PokerClient.Game
{
    /// <summary>
    /// Main game controller - orchestrates game flow
    /// </summary>
    public class GameController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TableController tableController;
        
        private PokerNetworkManager _network;
        
        private void Start()
        {
            _network = PokerNetworkManager.Instance;
            
            // Subscribe to network events
            _network.OnTableStateUpdated += HandleTableStateUpdated;
            _network.OnPlayerAction += HandlePlayerAction;
            _network.OnPlayerJoined += HandlePlayerJoined;
            _network.OnPlayerLeft += HandlePlayerLeft;
        }
        
        private void OnDestroy()
        {
            if (_network != null)
            {
                _network.OnTableStateUpdated -= HandleTableStateUpdated;
                _network.OnPlayerAction -= HandlePlayerAction;
                _network.OnPlayerJoined -= HandlePlayerJoined;
                _network.OnPlayerLeft -= HandlePlayerLeft;
            }
        }
        
        #region Event Handlers
        
        private void HandleTableStateUpdated(TableState state)
        {
            Debug.Log($"[Game] Table state updated - Phase: {state.phase}, Pot: {state.pot}");
            
            // Update table UI
            tableController?.UpdateTable(state);
            
            // Check if it's our turn
            if (_network.IsMyTurn())
            {
                ShowActionButtons(state);
            }
            else
            {
                HideActionButtons();
            }
        }
        
        private void HandlePlayerAction(PlayerActionEvent evt)
        {
            Debug.Log($"[Game] Player {evt.playerId} performed {evt.action}");
            
            // Play action animation/sound
            // tableController?.ShowPlayerAction(evt.playerId, evt.action, evt.amount);
        }
        
        private void HandlePlayerJoined(PlayerJoinedEvent evt)
        {
            Debug.Log($"[Game] {evt.name} joined at seat {evt.seatIndex}");
        }
        
        private void HandlePlayerLeft(PlayerLeftEvent evt)
        {
            Debug.Log($"[Game] Player {evt.playerId} left");
        }
        
        #endregion
        
        #region Actions
        
        public async void OnFoldClicked()
        {
            var result = await _network.SendActionAsync(PokerAction.Fold);
            if (!result.success)
            {
                Debug.LogWarning($"[Game] Fold failed: {result.error}");
            }
        }
        
        public async void OnCheckClicked()
        {
            var result = await _network.SendActionAsync(PokerAction.Check);
            if (!result.success)
            {
                Debug.LogWarning($"[Game] Check failed: {result.error}");
            }
        }
        
        public async void OnCallClicked()
        {
            var result = await _network.SendActionAsync(PokerAction.Call);
            if (!result.success)
            {
                Debug.LogWarning($"[Game] Call failed: {result.error}");
            }
        }
        
        public async void OnBetClicked(int amount)
        {
            var result = await _network.SendActionAsync(PokerAction.Bet, amount);
            if (!result.success)
            {
                Debug.LogWarning($"[Game] Bet failed: {result.error}");
            }
        }
        
        public async void OnRaiseClicked(int amount)
        {
            var result = await _network.SendActionAsync(PokerAction.Raise, amount);
            if (!result.success)
            {
                Debug.LogWarning($"[Game] Raise failed: {result.error}");
            }
        }
        
        public async void OnAllInClicked()
        {
            var result = await _network.SendActionAsync(PokerAction.AllIn);
            if (!result.success)
            {
                Debug.LogWarning($"[Game] All-in failed: {result.error}");
            }
        }
        
        #endregion
        
        #region UI Helpers
        
        private void ShowActionButtons(TableState state)
        {
            // TODO: Enable action buttons based on valid actions
            // - Fold is always available
            // - Check if currentBet == mySeat.currentBet
            // - Call if currentBet > mySeat.currentBet
            // - Bet/Raise if you have chips
            // - All-in always available
        }
        
        private void HideActionButtons()
        {
            // TODO: Disable all action buttons
        }
        
        #endregion
    }
}

