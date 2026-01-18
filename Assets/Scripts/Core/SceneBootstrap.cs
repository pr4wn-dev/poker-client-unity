using UnityEngine;
using PokerClient.UI.Scenes;

namespace PokerClient.Core
{
    /// <summary>
    /// Bootstrap script that creates scenes programmatically.
    /// Add this to an empty GameObject in each scene to build the UI.
    /// </summary>
    public class SceneBootstrap : MonoBehaviour
    {
        public enum SceneType
        {
            MainMenu,
            Lobby,
            PokerTable,
            AdventureMap,
            BossBattle
        }
        
        [Header("Scene Configuration")]
        [SerializeField] private SceneType sceneType = SceneType.MainMenu;
        [SerializeField] private bool autoInitialize = true;
        
        private void Start()
        {
            if (autoInitialize)
            {
                InitializeScene();
            }
        }
        
        /// <summary>
        /// Initialize the scene based on type
        /// </summary>
        public void InitializeScene()
        {
            // Ensure GameManager exists
            if (GameManager.Instance == null)
            {
                var gmObj = new GameObject("GameManager");
                gmObj.AddComponent<GameManager>();
            }
            
            // Build the scene
            switch (sceneType)
            {
                case SceneType.MainMenu:
                    CreateMainMenuScene();
                    break;
                case SceneType.Lobby:
                    CreateLobbyScene();
                    break;
                case SceneType.PokerTable:
                    CreatePokerTableScene();
                    break;
                case SceneType.AdventureMap:
                    CreateAdventureMapScene();
                    break;
                case SceneType.BossBattle:
                    CreateBossBattleScene();
                    break;
            }
        }
        
        private void CreateMainMenuScene()
        {
            var sceneObj = new GameObject("MainMenuScene");
            sceneObj.AddComponent<MainMenuScene>();
        }
        
        private void CreateLobbyScene()
        {
            // TODO: Create lobby scene
            Debug.Log("Lobby scene not yet implemented");
        }
        
        private void CreatePokerTableScene()
        {
            var sceneObj = new GameObject("PokerTableScene");
            sceneObj.AddComponent<PokerTableScene>();
        }
        
        private void CreateAdventureMapScene()
        {
            var sceneObj = new GameObject("AdventureMapScene");
            sceneObj.AddComponent<AdventureMapScene>();
        }
        
        private void CreateBossBattleScene()
        {
            // TODO: Create boss battle scene (similar to poker table but vs AI)
            Debug.Log("Boss battle scene not yet implemented");
            
            // For now, use poker table scene as base
            var sceneObj = new GameObject("BossBattleScene");
            sceneObj.AddComponent<PokerTableScene>();
        }
    }
}






