using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PokerClient.Networking;
using System.Collections.Generic;

namespace PokerClient.UI.Components
{
    /// <summary>
    /// Tournament bracket visualization showing elimination rounds.
    /// </summary>
    public class TournamentBracket : MonoBehaviour
    {
        private RectTransform _rect;
        private CanvasGroup _canvasGroup;
        
        private Transform bracketContainer;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI statusText;
        private Button closeButton;
        private ScrollRect scrollRect;
        
        private List<GameObject> _roundColumns = new List<GameObject>();
        private List<GameObject> _matchCards = new List<GameObject>();
        
        public System.Action OnClose;
        
        public static TournamentBracket Create(Transform parent)
        {
            var go = new GameObject("TournamentBracket");
            go.transform.SetParent(parent, false);
            var bracket = go.AddComponent<TournamentBracket>();
            bracket.Initialize();
            return bracket;
        }
        
        private void Initialize()
        {
            var theme = Theme.Current;
            
            _rect = gameObject.AddComponent<RectTransform>();
            _rect.anchorMin = Vector2.zero;
            _rect.anchorMax = Vector2.one;
            _rect.sizeDelta = Vector2.zero;
            
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            
            // Dimmed background
            var bg = gameObject.AddComponent<Image>();
            bg.color = new Color(0, 0, 0, 0.9f);
            
            // Main panel
            var panel = UIFactory.CreatePanel(transform, "Panel", theme.cardPanelColor);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.05f, 0.05f);
            panelRect.anchorMax = new Vector2(0.95f, 0.95f);
            panelRect.sizeDelta = Vector2.zero;
            
            BuildHeader(panel.transform);
            BuildBracketArea(panel.transform);
            
            gameObject.SetActive(false);
        }
        
        private void BuildHeader(Transform parent)
        {
            var theme = Theme.Current;
            
            var header = UIFactory.CreatePanel(parent, "Header", Color.clear);
            var headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 0.92f);
            headerRect.anchorMax = Vector2.one;
            headerRect.sizeDelta = Vector2.zero;
            
            titleText = UIFactory.CreateTitle(header.transform, "Title", "🏆 TOURNAMENT BRACKET", 32f);
            titleText.color = theme.accentColor;
            var titleRect = titleText.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.02f, 0);
            titleRect.anchorMax = new Vector2(0.5f, 1);
            titleRect.sizeDelta = Vector2.zero;
            
            statusText = UIFactory.CreateText(header.transform, "Status", "Round 1 of 4", 18f, theme.textSecondary);
            var statusRect = statusText.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.5f, 0.1f);
            statusRect.anchorMax = new Vector2(0.85f, 0.9f);
            statusRect.sizeDelta = Vector2.zero;
            statusText.alignment = TextAlignmentOptions.Center;
            
            closeButton = UIFactory.CreateButton(header.transform, "Close", "✕", Hide).GetComponent<Button>();
            var closeRect = closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.93f, 0.1f);
            closeRect.anchorMax = new Vector2(0.98f, 0.9f);
            closeRect.sizeDelta = Vector2.zero;
            closeButton.GetComponent<Image>().color = theme.dangerColor;
        }
        
        private void BuildBracketArea(Transform parent)
        {
            var theme = Theme.Current;
            
            // Scroll view for horizontal scrolling
            var scrollPanel = UIFactory.CreatePanel(parent, "ScrollPanel", Color.clear);
            var scrollPanelRect = scrollPanel.GetComponent<RectTransform>();
            scrollPanelRect.anchorMin = new Vector2(0.01f, 0.01f);
            scrollPanelRect.anchorMax = new Vector2(0.99f, 0.9f);
            scrollPanelRect.sizeDelta = Vector2.zero;
            
            var scrollView = new GameObject("ScrollView");
            scrollView.transform.SetParent(scrollPanel.transform, false);
            var scrollViewRect = scrollView.AddComponent<RectTransform>();
            scrollViewRect.anchorMin = Vector2.zero;
            scrollViewRect.anchorMax = Vector2.one;
            scrollViewRect.sizeDelta = Vector2.zero;
            
            scrollRect = scrollView.AddComponent<ScrollRect>();
            scrollRect.horizontal = true;
            scrollRect.vertical = false;
            
            var viewport = UIFactory.CreatePanel(scrollView.transform, "Viewport", Color.clear);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            scrollRect.viewport = viewportRect;
            
            var content = UIFactory.CreatePanel(viewport.transform, "Content", Color.clear);
            bracketContainer = content.transform;
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 0);
            contentRect.anchorMax = new Vector2(0, 1);
            contentRect.pivot = new Vector2(0, 0.5f);
            contentRect.sizeDelta = new Vector2(1500, 0);  // Width grows with rounds
            
            var hlg = content.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 50;
            hlg.padding = new RectOffset(30, 30, 30, 30);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlHeight = true;
            hlg.childControlWidth = false;
            
            scrollRect.content = contentRect;
        }
        
        public void Show(TournamentState tournament)
        {
            if (tournament == null) return;
            
            titleText.text = $"🏆 {tournament.name}";
            
            int currentRound = tournament.currentRound;
            int totalRounds = CalculateTotalRounds(tournament.maxPlayers);
            statusText.text = tournament.status == "completed" 
                ? "TOURNAMENT COMPLETE" 
                : $"Round {currentRound} of {totalRounds}";
            
            BuildBracket(tournament);
            
            gameObject.SetActive(true);
        }
        
        public void Hide()
        {
            gameObject.SetActive(false);
            OnClose?.Invoke();
        }
        
        private int CalculateTotalRounds(int players)
        {
            // For single elimination: log2(players) rounds
            int rounds = 0;
            while ((1 << rounds) < players) rounds++;
            return rounds;
        }
        
        private void BuildBracket(TournamentState tournament)
        {
            var theme = Theme.Current;
            
            // Clear existing
            foreach (var col in _roundColumns) Destroy(col);
            foreach (var card in _matchCards) Destroy(card);
            _roundColumns.Clear();
            _matchCards.Clear();
            
            int totalRounds = CalculateTotalRounds(tournament.maxPlayers);
            int matchesInRound = tournament.maxPlayers / 2;
            
            // Create mock bracket structure
            for (int round = 0; round < totalRounds; round++)
            {
                CreateRoundColumn(round, totalRounds, matchesInRound, tournament.currentRound, tournament.players);
                matchesInRound = matchesInRound / 2;
                if (matchesInRound < 1) matchesInRound = 1;
            }
            
            // Add Finals column
            CreateFinalsColumn(tournament);
            
            // Update content width
            var contentRect = bracketContainer.GetComponent<RectTransform>();
            contentRect.sizeDelta = new Vector2((totalRounds + 1) * 280, 0);
        }
        
        private void CreateRoundColumn(int roundIndex, int totalRounds, int matchCount, int currentRound, List<TournamentPlayer> players)
        {
            var theme = Theme.Current;
            
            string roundName = roundIndex switch
            {
                0 => "Round 1",
                _ when roundIndex == totalRounds - 2 => "Semi-Finals",
                _ when roundIndex == totalRounds - 1 => "Finals",
                _ => $"Round {roundIndex + 1}"
            };
            
            var column = UIFactory.CreatePanel(bracketContainer, $"Round_{roundIndex}", Color.clear);
            column.GetOrAddComponent<LayoutElement>().preferredWidth = 220;
            _roundColumns.Add(column);
            
            var vlg = column.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 15;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            
            // Round header
            var header = UIFactory.CreateText(column.transform, "Header", roundName, 18f, theme.textSecondary);
            header.GetOrAddComponent<LayoutElement>().preferredHeight = 30;
            header.alignment = TextAlignmentOptions.Center;
            header.fontStyle = FontStyles.Bold;
            
            // Matches
            for (int i = 0; i < matchCount; i++)
            {
                bool isPast = roundIndex < currentRound - 1;
                bool isCurrent = roundIndex == currentRound - 1;
                CreateMatchCard(column.transform, i, roundIndex, isPast, isCurrent, players);
            }
        }
        
        private void CreateMatchCard(Transform parent, int matchIndex, int roundIndex, bool isPast, bool isCurrent, List<TournamentPlayerInfo> players)
        {
            var theme = Theme.Current;
            
            Color bgColor = isCurrent ? theme.primaryColor * 0.6f : 
                           isPast ? theme.successColor * 0.3f : 
                           theme.backgroundColor;
            
            var card = UIFactory.CreatePanel(parent, $"Match_{matchIndex}", bgColor);
            card.GetOrAddComponent<LayoutElement>().preferredHeight = 80;
            _matchCards.Add(card);
            
            if (isCurrent)
            {
                var outline = card.AddComponent<Outline>();
                outline.effectColor = theme.accentColor;
                outline.effectDistance = new Vector2(2, 2);
            }
            
            var vlg = card.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 5;
            vlg.padding = new RectOffset(10, 10, 8, 8);
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            
            // Get player names from list
            int p1Index = matchIndex * 2;
            int p2Index = matchIndex * 2 + 1;
            
            string player1 = (players != null && p1Index < players.Count) 
                ? players[p1Index].username 
                : "TBD";
            string player2 = (players != null && p2Index < players.Count) 
                ? players[p2Index].username 
                : "TBD";
            
            // Player 1
            CreatePlayerRow(card.transform, player1, isPast && matchIndex % 2 == 0);
            
            // VS
            var vs = UIFactory.CreateText(card.transform, "VS", "vs", 12f, theme.textSecondary);
            vs.GetOrAddComponent<LayoutElement>().preferredHeight = 15;
            vs.alignment = TextAlignmentOptions.Center;
            
            // Player 2
            CreatePlayerRow(card.transform, player2, isPast && matchIndex % 2 == 1);
        }
        
        private void CreatePlayerRow(Transform parent, string playerName, bool isWinner)
        {
            var theme = Theme.Current;
            
            Color textColor = isWinner ? theme.successColor : theme.textPrimary;
            string prefix = isWinner ? "✓ " : "";
            
            var text = UIFactory.CreateText(parent, "Player", prefix + playerName, 14f, textColor);
            text.GetOrAddComponent<LayoutElement>().preferredHeight = 22;
            text.alignment = TextAlignmentOptions.Center;
            if (isWinner) text.fontStyle = FontStyles.Bold;
        }
        
        private void CreateFinalsColumn(TournamentState tournament)
        {
            var theme = Theme.Current;
            
            var column = UIFactory.CreatePanel(bracketContainer, "Winner", Color.clear);
            column.GetOrAddComponent<LayoutElement>().preferredWidth = 200;
            _roundColumns.Add(column);
            
            var vlg = column.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 15;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            
            // Winner header
            var header = UIFactory.CreateText(column.transform, "Header", "🏆 WINNER", 20f, theme.accentColor);
            header.GetOrAddComponent<LayoutElement>().preferredHeight = 35;
            header.alignment = TextAlignmentOptions.Center;
            header.fontStyle = FontStyles.Bold;
            
            // Winner card
            var winnerCard = UIFactory.CreatePanel(column.transform, "WinnerCard", 
                tournament.status == "completed" ? new Color(1f, 0.85f, 0f, 0.4f) : theme.cardPanelColor);
            winnerCard.GetOrAddComponent<LayoutElement>().preferredHeight = 120;
            
            var cardVlg = winnerCard.AddComponent<VerticalLayoutGroup>();
            cardVlg.spacing = 10;
            cardVlg.padding = new RectOffset(15, 15, 15, 15);
            cardVlg.childAlignment = TextAnchor.MiddleCenter;
            cardVlg.childControlHeight = false;
            cardVlg.childControlWidth = true;
            
            // Trophy
            var trophy = UIFactory.CreateText(winnerCard.transform, "Trophy", "🏆", 48f, Color.white);
            trophy.GetOrAddComponent<LayoutElement>().preferredHeight = 60;
            trophy.alignment = TextAlignmentOptions.Center;
            
            // Winner name - look up from players list
            string winnerName = "???";
            if (tournament.status == "completed" && !string.IsNullOrEmpty(tournament.winner))
            {
                var winnerPlayer = tournament.players?.Find(p => p.oderId == tournament.winner);
                winnerName = winnerPlayer?.username ?? tournament.winner;
            }
            var winnerText = UIFactory.CreateText(winnerCard.transform, "Name", winnerName, 22f, 
                tournament.status == "completed" ? theme.successColor : theme.textSecondary);
            winnerText.GetOrAddComponent<LayoutElement>().preferredHeight = 30;
            winnerText.alignment = TextAlignmentOptions.Center;
            winnerText.fontStyle = FontStyles.Bold;
        }
    }
}

