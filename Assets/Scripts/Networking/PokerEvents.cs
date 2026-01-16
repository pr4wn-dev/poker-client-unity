namespace PokerClient.Networking
{
    /// <summary>
    /// Socket event names - keep in sync with poker-server/src/sockets/Events.js
    /// </summary>
    public static class PokerEvents
    {
        // ============ Authentication ============
        public const string Login = "login";
        public const string Register = "register";
        public const string Logout = "logout";
        
        // ============ Lobby ============
        public const string GetTables = "get_tables";
        public const string CreateTable = "create_table";
        public const string GetHouseRulesPresets = "get_house_rules_presets";
        
        // ============ Table Actions ============
        public const string JoinTable = "join_table";
        public const string LeaveTable = "leave_table";
        public const string Action = "action";
        public const string Chat = "chat";
        public const string Spectate = "spectate";
        
        // ============ Social ============
        public const string GetFriends = "get_friends";
        public const string SendFriendRequest = "send_friend_request";
        public const string AcceptFriendRequest = "accept_friend_request";
        public const string DeclineFriendRequest = "decline_friend_request";
        public const string RemoveFriend = "remove_friend";
        public const string InviteToTable = "invite_to_table";
        public const string GetTableInvites = "get_table_invites";
        public const string SearchUsers = "search_users";
        
        // ============ Adventure Mode ============
        public const string GetLevels = "get_levels";
        public const string StartAdventure = "start_adventure";
        public const string AdventureAction = "adventure_action";
        public const string ForfeitAdventure = "forfeit_adventure";
        
        // ============ Inventory ============
        public const string GetInventory = "get_inventory";
        public const string EquipItem = "equip_item";
        public const string UnequipItem = "unequip_item";
        
        // ============ Server -> Client Events ============
        public const string TableCreated = "table_created";
        public const string PlayerJoined = "player_joined";
        public const string PlayerLeft = "player_left";
        public const string PlayerDisconnected = "player_disconnected";
        public const string PlayerAction = "player_action";
        public const string TableState = "table_state";
        public const string ChatMessage = "chat";
        public const string SpectatorJoined = "spectator_joined";
        public const string SpectatorLeft = "spectator_left";
        
        // Social Events
        public const string FriendRequestReceived = "friend_request_received";
        public const string FriendRequestAccepted = "friend_request_accepted";
        public const string FriendOnline = "friend_online";
        public const string FriendOffline = "friend_offline";
        public const string TableInviteReceived = "table_invite_received";
        
        // Adventure Events
        public const string AdventureState = "adventure_state";
        public const string AdventureResult = "adventure_result";
        public const string BossTaunt = "boss_taunt";
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
    
    /// <summary>
    /// House rules preset IDs
    /// </summary>
    public static class HouseRulesPresets
    {
        public const string Standard = "standard";
        public const string NoLimit = "no_limit";
        public const string PotLimit = "pot_limit";
        public const string FixedLimit = "fixed_limit";
        public const string ShortDeck = "short_deck";
        public const string Straddle = "straddle";
        public const string BombPot = "bomb_pot";
    }
}
