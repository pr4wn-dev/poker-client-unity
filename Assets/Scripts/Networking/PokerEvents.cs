namespace PokerClient.Networking
{
    /// <summary>
    /// Socket event names - keep in sync with poker-server/src/sockets/Events.js
    /// </summary>
    public static class PokerEvents
    {
        // Client -> Server
        public const string Register = "register";
        public const string GetTables = "get_tables";
        public const string CreateTable = "create_table";
        public const string JoinTable = "join_table";
        public const string LeaveTable = "leave_table";
        public const string Action = "action";
        public const string Chat = "chat";
        
        // Server -> Client
        public const string TableCreated = "table_created";
        public const string PlayerJoined = "player_joined";
        public const string PlayerLeft = "player_left";
        public const string PlayerDisconnected = "player_disconnected";
        public const string PlayerAction = "player_action";
        public const string TableState = "table_state";
        public const string ChatMessage = "chat";
    }
    
    /// <summary>
    /// Action string values for game actions
    /// </summary>
    public static class ActionStrings
    {
        public const string Fold = "fold";
        public const string Check = "check";
        public const string Call = "call";
        public const string Bet = "bet";
        public const string Raise = "raise";
        public const string AllIn = "allin";
        
        public static string FromAction(PokerAction action)
        {
            return action switch
            {
                PokerAction.Fold => Fold,
                PokerAction.Check => Check,
                PokerAction.Call => Call,
                PokerAction.Bet => Bet,
                PokerAction.Raise => Raise,
                PokerAction.AllIn => AllIn,
                _ => Fold
            };
        }
    }
}

