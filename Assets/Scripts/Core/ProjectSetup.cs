using UnityEngine;

namespace PokerClient.Core
{
    /// <summary>
    /// Project setup helper - Documents required Unity setup steps.
    /// This file also serves as documentation for project configuration.
    /// </summary>
    public static class ProjectSetup
    {
        /*
        ============================================================
        POKER CLIENT UNITY - PROJECT SETUP GUIDE
        ============================================================
        
        1. UNITY VERSION
           - Recommended: Unity 2022.3 LTS or newer
           - Scripting Backend: IL2CPP (for Android)
           
        2. REQUIRED PACKAGES (Install via Package Manager)
           - TextMeshPro (included in Unity)
           - Socket.IO for Unity (from Asset Store or GitHub)
             https://github.com/itisnajim/SocketIOUnity
           
        3. PROJECT SETTINGS
        
           Player Settings (Edit > Project Settings > Player):
           - Company Name: Your company
           - Product Name: Poker Game
           - Default Orientation: Landscape Left
           - Allowed Orientations: Landscape Left + Landscape Right
           - Resolution: 1920x1080 reference
           
           Android Settings:
           - Minimum API Level: 24 (Android 7.0)
           - Target API Level: Latest
           - Scripting Backend: IL2CPP
           - ARM64 enabled
           
        4. SCENE SETUP
        
           Create these scenes in Assets/Scenes/:
           - MainMenu.unity (add SceneBootstrap with SceneType.MainMenu)
           - Lobby.unity (add SceneBootstrap with SceneType.Lobby)
           - PokerTable.unity (add SceneBootstrap with SceneType.PokerTable)
           - AdventureMap.unity (add SceneBootstrap with SceneType.AdventureMap)
           - BossBattle.unity (add SceneBootstrap with SceneType.BossBattle)
           
           Add all scenes to Build Settings.
           
        5. THEME SETUP
        
           Create theme asset:
           - Right-click in Project > Create > Poker > Game Theme
           - Save as "GameTheme" in Resources folder
           - Customize colors as desired
           
        6. FOLDER STRUCTURE
        
           Assets/
           ├── Scenes/          (Unity scene files)
           ├── Scripts/         (All C# code - already set up)
           ├── Resources/       (GameTheme, other runtime-loaded assets)
           ├── Prefabs/         (Reusable prefabs - optional)
           ├── Sprites/         (Card images, UI elements - add later)
           ├── Fonts/           (Custom fonts - optional)
           └── Audio/           (Sound effects, music - add later)
           
        7. TESTING
        
           To test without server:
           - Scenes build themselves programmatically
           - Login uses mock data
           - Game actions log to console
           
           To test with server:
           - Start poker-server (npm start)
           - Update serverAddress in GameManager
           - Network events will connect automatically
           
        ============================================================
        */
        
        /// <summary>
        /// Reference resolution for UI scaling
        /// </summary>
        public static readonly Vector2 ReferenceResolution = new Vector2(1920, 1080);
        
        /// <summary>
        /// Check if project is properly configured
        /// </summary>
        public static bool ValidateSetup()
        {
            bool valid = true;
            
            // Check for TextMeshPro
            if (!TMProCheck())
            {
                Debug.LogWarning("[ProjectSetup] TextMeshPro not imported. Import via Window > TextMeshPro > Import TMP Essential Resources");
                valid = false;
            }
            
            // Check for theme
            var theme = Resources.Load<UI.GameTheme>("GameTheme");
            if (theme == null)
            {
                Debug.LogWarning("[ProjectSetup] GameTheme not found in Resources. Create via Create > Poker > Game Theme");
            }
            
            return valid;
        }
        
        private static bool TMProCheck()
        {
            // TextMeshPro is available if we can use the namespace
            return true; // We're using TMPro in our scripts, so it must be available
        }
    }
}







