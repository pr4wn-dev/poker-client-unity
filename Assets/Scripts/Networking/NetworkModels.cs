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
        Special,
        LocationKey,  // Unlocks special map areas
        Vehicle,      // Yacht, jet, etc.
        XpBoost       // XP multiplier items
    }
    
    public enum AreaType
    {
        Starter,
        City,
        Casino,
        Underground,
        Vip,
        Yacht,
        Island,
        Penthouse,
        Secret
    }
    
    public enum AreaUnlockType
    {
        XpLevel,
        BossDefeat,
        Item,
        Chips,
        Achievement
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
    
    /// <summary>
    /// Simplified item info for rewards and side pots (less fields than full Item)
    /// </summary>
    [Serializable]
    public class ItemInfo
    {
        public string id;
        public string name;
        public string type;
        public string rarity;
        public string icon;
        public int baseValue;
    }
    
    #endregion
    
    #region Adventure Mode Models
    
    // ============ XP & Progression ============
    
    [Serializable]
    public class PlayerXPInfo
    {
        public int xp;
        public int level;
        public int? xpForNextLevel;
        public int xpProgress; // Percentage to next level
    }
    
    /// <summary>
    /// Level/stage info for adventure mode UI
    /// </summary>
    [Serializable]
    public class LevelInfo
    {
        public int level;
        public string bossId;
        public string bossName;
        public string difficulty;
        public bool isUnlocked;
        public bool isDefeated;
        public int entryFee;
        public RewardPreview rewards;
    }
    
    [Serializable]
    public class AdventureProgress
    {
        public string currentArea;
        public int xp;
        public int level;
        public int xpToNextLevel;
        public List<string> bossesDefeated;
        public Dictionary<string, int> bossDefeatCounts;
        public int totalWins;
        public int totalLosses;
    }
    
    // ============ World Map ============
    
    [Serializable]
    public class WorldMapState
    {
        public int playerLevel;
        public int playerXP;
        public int xpProgress;
        public int? xpForNextLevel;
        public int maxLevel;
        public List<AreaInfo> areas;
    }
    
    [Serializable]
    public class AreaInfo
    {
        public string id;
        public string name;
        public string type;
        public string description;
        public string icon;
        public Position position;
        public bool isUnlocked;
        public string unlockReason;
        public List<AreaRequirement> requirements;
        public int bossCount;
        public int completedBosses;
    }
    
    [Serializable]
    public class Position
    {
        public int x;
        public int y;
    }
    
    [Serializable]
    public class AreaRequirement
    {
        public string type; // "xp_level", "boss_defeat", "item", "chips"
        public string value;
    }
    
    // ============ Boss Models ============
    
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
    public class BossListItem
    {
        public string id;
        public string name;
        public string avatar;
        public string description;
        public string difficulty;
        public int minLevel;
        public int entryFee;
        public bool canChallenge;
        public string challengeBlockedReason;
        public int defeatCount;
        public RewardPreview rewards;
    }
    
    [Serializable]
    public class RewardPreview
    {
        public int xp;
        public int coins;
        public int chips;
        public int entryFee;
        public int minLevel;
        public List<DropChance> possibleDrops;
    }
    
    [Serializable]
    public class DropChance
    {
        public string itemId;
        public float chance;
        public int minDefeats; // Must defeat boss X times before this can drop
    }
    
    // ============ Session & Results ============
    
    [Serializable]
    public class AdventureSession
    {
        public string oderId;
        public BossInfo boss;
        public int userChips;
        public int handsPlayed;
        public int entryFee;
        public int level;
    }
    
    [Serializable]
    public class AdventureResult
    {
        public string status;  // "victory", "defeat", "ongoing"
        public BossResultInfo boss;
        public int handsPlayed;
        public AdventureRewards rewards;
        public int playerXP;
        public int playerLevel;
        public int xpProgress;
        public int defeatCount;
        public bool isFirstDefeat;
        public int consolationXP;  // XP given even on defeat
        public int entryFeeLost;
        public string message;
    }
    
    [Serializable]
    public class BossResultInfo
    {
        public string id;
        public string name;
        public string winQuote;
        public string loseQuote;
    }
    
    [Serializable]
    public class AdventureRewards
    {
        public int xp;
        public int coins;
        public int chips;
        public List<ItemInfo> items;
    }
    
    #endregion
    
    #region Tournament Models
    
    public enum TournamentStatus
    {
        Registering,
        Starting,
        InProgress,
        FinalTable,
        Completed,
        Cancelled
    }
    
    public enum TournamentType
    {
        SitNGo,
        Scheduled,
        Freeroll,
        Satellite
    }
    
    [Serializable]
    public class TournamentInfo
    {
        public string id;
        public string name;
        public string areaId;
        public string type;
        public string status;
        public int registeredCount;
        public int minPlayers;
        public int maxPlayers;
        public int startingChips;
        public int entryFee;
        public int minLevel;
        public int minChips;
        public List<string> requiredItems;
        public bool sidePotRequired;
        public string sidePotMinRarity;
        public int prizePool;
        public int xpPrizePool;
        public long? scheduledStart;
        public int currentBlindLevel;
        public BlindLevel blinds;
        
        // Added by client when checking eligibility
        public TournamentEligibility canEnter;
    }
    
    [Serializable]
    public class TournamentEligibility
    {
        public bool canEnter;
        public string reason;
        public List<string> reasons;
    }
    
    [Serializable]
    public class BlindLevel
    {
        public int small;
        public int big;
        public int ante;
    }
    
    [Serializable]
    public class TournamentState
    {
        public string id;
        public string name;
        public string areaId;
        public string status;
        public int registeredCount;
        public int maxPlayers;
        public int prizePool;
        public int xpPrizePool;
        public List<TournamentPlayer> players;
        public List<TournamentSidePotEntry> sidePotItems;
        public int eliminatedCount;
        public Dictionary<int, int> payoutStructure;
        public int currentBlindLevel;
        public BlindLevel blinds;
    }
    
    [Serializable]
    public class TournamentPlayer
    {
        public string oderId;
        public string username;
        public bool isEliminated;
        public int? seatAssignment;
    }
    
    [Serializable]
    public class TournamentSidePotEntry
    {
        public string oderId;
        public ItemInfo item;
    }
    
    [Serializable]
    public class TournamentResult
    {
        public string status;
        public string oderId;
        public List<TournamentPayout> payouts;
    }
    
    [Serializable]
    public class TournamentPayout
    {
        public string oderId;
        public int position;
        public int chips;
        public int xp;
        public ItemInfo itemPrize;
        public List<ItemInfo> sidePotItems;
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
    
    #region Side Pot Models (Item Gambling)
    
    public enum SidePotStatus
    {
        Inactive,
        Collecting,
        Locked,
        Awarded
    }
    
    public enum SubmissionStatus
    {
        Pending,
        Approved,
        Declined,
        OptedOut
    }
    
    [Serializable]
    public class SidePotState
    {
        public string id;
        public string status;
        public string creatorId;
        public SidePotItem creatorItem;
        public long collectionEndTime;
        public int approvedCount;
        public int totalValue;
        public List<SidePotEntry> approvedItems;
        public List<SidePotSubmission> pendingSubmissions;  // Only for creator
        public MySubmission mySubmission;  // User's own submission status
        public string winnerId;  // Set when awarded
        
        public SidePotStatus GetStatus()
        {
            return status?.ToLower() switch
            {
                "inactive" => SidePotStatus.Inactive,
                "collecting" => SidePotStatus.Collecting,
                "locked" => SidePotStatus.Locked,
                "awarded" => SidePotStatus.Awarded,
                _ => SidePotStatus.Inactive
            };
        }
        
        public bool IsActive => GetStatus() != SidePotStatus.Inactive;
        public bool IsCollecting => GetStatus() == SidePotStatus.Collecting;
    }
    
    [Serializable]
    public class SidePotItem
    {
        public string id;
        public string name;
        public string rarity;
        public string type;
        public string icon;
        public int baseValue;
    }
    
    [Serializable]
    public class SidePotEntry
    {
        public string oderId;
        public SidePotItem item;
    }
    
    [Serializable]
    public class SidePotSubmission
    {
        public string oderId;
        public SidePotItem item;
        public long submittedAt;
    }
    
    [Serializable]
    public class MySubmission
    {
        public string status;
        public SidePotItem item;
        
        public SubmissionStatus GetStatus()
        {
            return status?.ToLower() switch
            {
                "pending" => SubmissionStatus.Pending,
                "approved" => SubmissionStatus.Approved,
                "declined" => SubmissionStatus.Declined,
                "opted_out" => SubmissionStatus.OptedOut,
                _ => SubmissionStatus.Pending
            };
        }
    }
    
    // Events
    [Serializable]
    public class SidePotStartedEvent
    {
        public string creatorId;
        public SidePotItem creatorItem;
        public long collectionEndTime;
    }
    
    [Serializable]
    public class SidePotSubmissionEvent
    {
        public string oderId;
        public string username;
        public SidePotItem item;
    }
    
    [Serializable]
    public class SidePotAwardedEvent
    {
        public string winnerId;
        public List<SidePotItem> items;
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
        public bool hasSidePot;
        public int sidePotItemCount;
        public long createdAt;
    }
    
    [Serializable]
    public class SeatInfo
    {
        public int index;
        public string playerId;
        public string playerName;  // Display name
        public string name;        // Alias for playerName
        public int chips;
        public int currentBet;
        public bool isFolded;
        public bool isAllIn;
        public bool isConnected;
        public bool inSidePot;  // Whether player is participating in item side pot
        public List<Card> cards;
        
        public bool IsEmpty => string.IsNullOrEmpty(playerId);
        public bool IsActive => !IsEmpty && !isFolded && isConnected;
        
        // Get display name (prefer playerName, fallback to name)
        public string GetDisplayName() => !string.IsNullOrEmpty(playerName) ? playerName : name ?? "Player";
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
        public int minBet;          // Minimum bet amount
        public int dealerIndex;
        public int currentPlayerIndex;
        public string currentPlayerId;  // ID of player whose turn it is
        public float? turnTimeRemaining; // Seconds left in turn
        public int handsPlayed;
        public int spectatorCount;
        public bool isSpectating;
        public string creatorId;
        public HouseRules houseRules;
        public SidePotState sidePot;  // Item side pot state
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
    
    // ============ Generic Responses ============
    
    [Serializable]
    public class SimpleResponse
    {
        public bool success;
        public string error;
    }
    
    // ============ Auth Requests/Responses ============
    
    [Serializable]
    public class RegisterRequest
    {
        public string playerName;
    }
    
    [Serializable]
    public class RegisterResponse
    {
        public bool success;
        public string error;
        public string playerId;
        public UserProfile profile;
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
        public string error;
        public string userId;
        public UserProfile profile;
    }
    
    // ============ Table Requests/Responses ============
    
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
        public string error;
        public string tableId;
        public TableInfo table;
    }
    
    [Serializable]
    public class GetTablesResponse
    {
        public bool success;
        public List<TableInfo> tables;
    }
    
    /// <summary>
    /// Alias for GetTablesResponse (backwards compatibility)
    /// </summary>
    [Serializable]
    public class TablesResponse
    {
        public bool success;
        public List<TableInfo> tables;
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
        public string error;
        public int seatIndex;
        public bool isSpectating;
        public TableState state;
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
        public string error;
        public string action;
        public int amount;
    }
    
    // ============ Social Requests/Responses ============
    
    [Serializable]
    public class SearchUsersResponse
    {
        public bool success;
        public List<UserSearchResult> users;
    }
    
    [Serializable]
    public class UserSearchResult
    {
        public string id;
        public string username;
        public bool isOnline;
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
        public string userId;
        public string tableId;
    }
    
    // ============ Adventure Requests/Responses ============
    
    [Serializable]
    public class GetWorldMapResponse
    {
        public bool success;
        public WorldMapState mapState;
    }
    
    [Serializable]
    public class GetBossesResponse
    {
        public bool success;
        public string areaId;
        public List<BossListItem> bosses;
    }
    
    [Serializable]
    public class StartAdventureResponse
    {
        public bool success;
        public string error;
        public AdventureSession session;
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
