using System;
using System.Collections.Generic;

namespace PokerClient.Networking
{
    /// <summary>
    /// Data models matching the server's socket events
    /// Keep these in sync with poker-server/src/sockets/Events.js
    /// </summary>
    
    #region Enums
    
    public enum GameMode
    {
        Adventure,
        Multiplayer
    }
    
    public enum GamePhase
    {
        Waiting,
        PreFlop,
        Flop,
        Turn,
        River,
        Showdown
    }
    
    public enum PokerAction
    {
        Fold,
        Check,
        Call,
        Bet,
        Raise,
        AllIn
    }
    
    public enum CardSuit
    {
        Hearts,
        Diamonds,
        Clubs,
        Spades
    }
    
    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
    
    public enum ItemType
    {
        CardBack,
        TableSkin,
        Avatar,
        Emote,
        ChipStyle,
        Trophy,
        Consumable,
        Special
    }
    
    public enum BossDifficulty
    {
        Easy,
        Medium,
        Hard,
        Expert,
        Legendary
    }
    
    #endregion

    #region Card & Basic Data Models
    
    [Serializable]
    public class Card
    {
        public string rank;
        public string suit;
        
        public bool IsHidden => string.IsNullOrEmpty(rank);
        
        public CardSuit GetSuit()
        {
            return suit?.ToLower() switch
            {
                "hearts" => CardSuit.Hearts,
                "diamonds" => CardSuit.Diamonds,
                "clubs" => CardSuit.Clubs,
                "spades" => CardSuit.Spades,
                _ => CardSuit.Hearts
            };
        }
        
        public int GetRankValue()
        {
            return rank switch
            {
                "A" => 14,
                "K" => 13,
                "Q" => 12,
                "J" => 11,
                _ => int.TryParse(rank, out int val) ? val : 0
            };
        }
        
        public override string ToString()
        {
            return IsHidden ? "??" : $"{rank}{GetSuitSymbol()}";
        }
        
        private string GetSuitSymbol()
        {
            return GetSuit() switch
            {
                CardSuit.Hearts => "♥",
                CardSuit.Diamonds => "♦",
                CardSuit.Clubs => "♣",
                CardSuit.Spades => "♠",
                _ => "?"
            };
        }
    }
    
    #endregion
    
    #region User & Social Models
    
    [Serializable]
    public class UserProfile
    {
        public string id;
        public string username;
        public int chips;
        public int adventureCoins;
        public bool isOnline;
        public UserStats stats;
        public AdventureProgress adventureProgress;
        public List<Item> inventory;
        public List<string> friends;
        public List<FriendRequest> friendRequests;
    }
    
    [Serializable]
    public class PublicProfile
    {
        public string id;
        public string username;
        public int chips;
        public bool isOnline;
        public UserStats stats;
        public int currentLevel;
        public int highestLevel;
    }
    
    [Serializable]
    public class UserStats
    {
        public int handsPlayed;
        public int handsWon;
        public int biggestPot;
        public int royalFlushes;
        public int tournamentsWon;
    }
    
    [Serializable]
    public class FriendRequest
    {
        public string fromUserId;
        public string fromUsername;
        public long sentAt;
    }
    
    [Serializable]
    public class TableInvite
    {
        public string fromUserId;
        public string fromUsername;
        public string tableId;
        public string tableName;
        public long sentAt;
    }
    
    #endregion
    
    #region Item & Inventory Models
    
    [Serializable]
    public class Item
    {
        public string id;
        public string templateId;
        public string name;
        public string description;
        public string type;
        public string rarity;
        public string icon;
        public int uses;
        public int maxUses;
        public int baseValue;
        public long obtainedAt;
        public string obtainedFrom;
        public bool isTradeable;
        public bool isGambleable;
        public string color;
        
        public ItemRarity GetRarity()
        {
            return rarity?.ToLower() switch
            {
                "common" => ItemRarity.Common,
                "uncommon" => ItemRarity.Uncommon,
                "rare" => ItemRarity.Rare,
                "epic" => ItemRarity.Epic,
                "legendary" => ItemRarity.Legendary,
                _ => ItemRarity.Common
            };
        }
        
        public ItemType GetItemType()
        {
            return type?.ToLower() switch
            {
                "card_back" => ItemType.CardBack,
                "table_skin" => ItemType.TableSkin,
                "avatar" => ItemType.Avatar,
                "emote" => ItemType.Emote,
                "chip_style" => ItemType.ChipStyle,
                "trophy" => ItemType.Trophy,
                "consumable" => ItemType.Consumable,
                _ => ItemType.Special
            };
        }
    }
    
    #endregion
    
    #region Adventure Mode Models
    
    [Serializable]
    public class AdventureProgress
    {
        public int currentLevel;
        public int highestLevel;
        public List<string> bossesDefeated;
        public int totalWins;
        public int totalLosses;
    }
    
    [Serializable]
    public class BossInfo
    {
        public string id;
        public string name;
        public string avatar;
        public int chips;
        public string difficulty;
        public string description;
        public string taunt;
        
        public BossDifficulty GetDifficulty()
        {
            return difficulty?.ToLower() switch
            {
                "easy" => BossDifficulty.Easy,
                "medium" => BossDifficulty.Medium,
                "hard" => BossDifficulty.Hard,
                "expert" => BossDifficulty.Expert,
                "legendary" => BossDifficulty.Legendary,
                _ => BossDifficulty.Easy
            };
        }
    }
    
    [Serializable]
    public class LevelInfo
    {
        public int level;
        public string bossId;
        public string bossName;
        public string bossAvatar;
        public string difficulty;
        public bool isUnlocked;
        public bool isDefeated;
        public RewardPreview rewards;
    }
    
    [Serializable]
    public class RewardPreview
    {
        public int coins;
        public List<DropChance> possibleDrops;
    }
    
    [Serializable]
    public class DropChance
    {
        public string itemId;
        public float chance;
    }
    
    [Serializable]
    public class AdventureSession
    {
        public string oderId;
        public int level;
        public BossInfo boss;
        public int userChips;
        public int handsPlayed;
    }
    
    [Serializable]
    public class AdventureResult
    {
        public string status;  // "victory", "defeat", "ongoing"
        public int level;
        public string boss;
        public int handsPlayed;
        public AdventureRewards rewards;
        public string message;
    }
    
    [Serializable]
    public class AdventureRewards
    {
        public int coins;
        public List<Item> items;
    }
    
    #endregion
    
    #region House Rules Models
    
    [Serializable]
    public class HouseRules
    {
        public string bettingType;  // no_limit, pot_limit, fixed_limit
        public int smallBlind;
        public int bigBlind;
        public int ante;
        public bool allowStraddle;
        public int minBet;
        public int? maxBet;
        public int? maxRaises;
        public int minBuyIn;
        public int maxBuyIn;
        public bool allowRebuy;
        public int turnTimeSeconds;
        public bool runItTwice;
        public string deckType;
        public List<string> wildCards;
        public int bombPotFrequency;
        public string gameType;
        public int maxPlayers;
        public bool allowSpectators;
    }
    
    [Serializable]
    public class HouseRulesPreset
    {
        public string id;
        public string name;
        public string description;
    }
    
    #endregion
    
    #region Table Models
    
    [Serializable]
    public class TableInfo
    {
        public string id;
        public string name;
        public int playerCount;
        public int maxPlayers;
        public int spectatorCount;
        public int smallBlind;
        public int bigBlind;
        public bool isPrivate;
        public bool hasPassword;
        public bool gameStarted;
        public bool allowSpectators;
        public string houseRulesPreset;
        public long createdAt;
    }
    
    [Serializable]
    public class SeatInfo
    {
        public int index;
        public string playerId;
        public string name;
        public int chips;
        public int currentBet;
        public bool isFolded;
        public bool isAllIn;
        public bool isConnected;
        public List<Card> cards;
        
        public bool IsEmpty => string.IsNullOrEmpty(playerId);
        public bool IsActive => !IsEmpty && !isFolded && isConnected;
    }
    
    [Serializable]
    public class TableState
    {
        public string id;
        public string name;
        public string phase;
        public int pot;
        public List<Card> communityCards;
        public int currentBet;
        public int dealerIndex;
        public int currentPlayerIndex;
        public int handsPlayed;
        public int spectatorCount;
        public bool isSpectating;
        public HouseRules houseRules;
        public List<SeatInfo> seats;
        
        public GamePhase GetPhase()
        {
            return phase?.ToLower() switch
            {
                "waiting" => GamePhase.Waiting,
                "preflop" => GamePhase.PreFlop,
                "flop" => GamePhase.Flop,
                "turn" => GamePhase.Turn,
                "river" => GamePhase.River,
                "showdown" => GamePhase.Showdown,
                _ => GamePhase.Waiting
            };
        }
        
        public SeatInfo GetCurrentPlayer()
        {
            if (currentPlayerIndex < 0 || currentPlayerIndex >= seats.Count)
                return null;
            return seats[currentPlayerIndex];
        }
        
        public SeatInfo FindPlayer(string oderId)
        {
            return seats.Find(s => s?.playerId == oderId);
        }
    }
    
    #endregion
    
    #region Request/Response Models
    
    [Serializable]
    public class RegisterRequest
    {
        public string playerName;
    }
    
    [Serializable]
    public class RegisterResponse
    {
        public bool success;
        public string playerId;
        public string error;
    }
    
    [Serializable]
    public class LoginRequest
    {
        public string username;
        public string password;
    }
    
    [Serializable]
    public class LoginResponse
    {
        public bool success;
        public string oderId;
        public UserProfile profile;
        public string error;
    }
    
    [Serializable]
    public class CreateTableRequest
    {
        public string name;
        public int maxPlayers;
        public int smallBlind;
        public int bigBlind;
        public bool isPrivate;
        public string password;
        public string houseRulesPreset;
        public HouseRules customRules;
    }
    
    [Serializable]
    public class CreateTableResponse
    {
        public bool success;
        public string tableId;
        public string error;
    }
    
    [Serializable]
    public class JoinTableRequest
    {
        public string tableId;
        public int? seatIndex;
        public string password;
        public bool asSpectator;
    }
    
    [Serializable]
    public class JoinTableResponse
    {
        public bool success;
        public int seatIndex;
        public bool isSpectating;
        public TableState state;
        public string error;
    }
    
    [Serializable]
    public class ActionRequest
    {
        public string action;
        public int amount;
    }
    
    [Serializable]
    public class ActionResponse
    {
        public bool success;
        public string action;
        public int amount;
        public string error;
    }
    
    [Serializable]
    public class TablesResponse
    {
        public bool success;
        public List<TableInfo> tables;
    }
    
    [Serializable]
    public class FriendsListResponse
    {
        public bool success;
        public List<PublicProfile> friends;
    }
    
    [Serializable]
    public class InviteToTableRequest
    {
        public string oderId;
        public string tableId;
    }
    
    #endregion
    
    #region Event Models
    
    [Serializable]
    public class PlayerJoinedEvent
    {
        public string playerId;
        public string name;
        public int seatIndex;
    }
    
    [Serializable]
    public class PlayerLeftEvent
    {
        public string playerId;
    }
    
    [Serializable]
    public class PlayerActionEvent
    {
        public string playerId;
        public string action;
        public int amount;
    }
    
    [Serializable]
    public class ChatMessage
    {
        public string playerId;
        public string name;
        public string message;
    }
    
    [Serializable]
    public class TableInviteEvent
    {
        public string fromUserId;
        public string fromUsername;
        public string tableId;
        public string tableName;
    }
    
    [Serializable]
    public class FriendRequestEvent
    {
        public string fromUserId;
        public string fromUsername;
    }
    
    #endregion
}
