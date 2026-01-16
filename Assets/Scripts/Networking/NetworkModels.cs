using System;
using System.Collections.Generic;

namespace PokerClient.Networking
{
    /// <summary>
    /// Data models matching the server's socket events
    /// Keep these in sync with poker-server/src/sockets/Events.js
    /// </summary>
    
    #region Enums
    
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
    
    #endregion

    #region Data Models
    
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
    
    [Serializable]
    public class TableInfo
    {
        public string id;
        public string name;
        public int playerCount;
        public int maxPlayers;
        public int smallBlind;
        public int bigBlind;
        public bool isPrivate;
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
        
        public SeatInfo FindPlayer(string playerId)
        {
            return seats.Find(s => s?.playerId == playerId);
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
    public class CreateTableRequest
    {
        public string name;
        public int maxPlayers;
        public int smallBlind;
        public int bigBlind;
        public bool isPrivate;
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
    }
    
    [Serializable]
    public class JoinTableResponse
    {
        public bool success;
        public int seatIndex;
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
    
    #endregion
}

