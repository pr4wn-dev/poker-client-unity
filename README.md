# Poker Client - Unity

Android poker game client built with Unity, connecting to the poker-server via WebSockets.

## Setup

### Prerequisites
- Unity 2022.3 LTS or newer
- Visual Studio or VS Code

### Installation

1. Open this folder as a Unity project (or create new Unity project and copy scripts)
2. Install WebSocket package via Package Manager or NuGet:
   - Recommended: [socket.io-client-csharp](https://github.com/doghappy/socket.io-client-csharp)
   - Install via: `dotnet add package SocketIOClient`

3. Configure server address in `PokerNetworkManager.cs`

### Project Structure

```
poker-client-unity/
├── Assets/
│   ├── Scripts/
│   │   ├── Networking/
│   │   │   ├── PokerNetworkManager.cs   # Socket.IO connection
│   │   │   ├── PokerEvents.cs           # Event definitions
│   │   │   └── NetworkModels.cs         # Data models
│   │   ├── Game/
│   │   │   ├── GameController.cs        # Game logic
│   │   │   ├── TableController.cs       # Table management
│   │   │   └── PlayerController.cs      # Player management
│   │   └── UI/
│   │       ├── LobbyUI.cs
│   │       ├── TableUI.cs
│   │       └── ActionButtonsUI.cs
│   ├── Prefabs/
│   ├── Scenes/
│   └── Resources/
└── README.md
```

## Connecting to Server

```csharp
// Initialize connection
await PokerNetworkManager.Instance.ConnectAsync("http://192.168.1.100:3000");

// Register player
await PokerNetworkManager.Instance.RegisterAsync("PlayerName");

// Join a table
await PokerNetworkManager.Instance.JoinTableAsync(tableId);

// Perform actions
await PokerNetworkManager.Instance.SendActionAsync(PokerAction.Call);
await PokerNetworkManager.Instance.SendActionAsync(PokerAction.Raise, 200);
```

## Build for Android

1. File > Build Settings
2. Switch platform to Android
3. Set up Android SDK in Preferences
4. Build & Run

## License

MIT





