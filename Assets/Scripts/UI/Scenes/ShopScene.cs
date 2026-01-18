using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using PokerClient.Networking;
using System.Collections.Generic;

namespace PokerClient.UI.Scenes
{
    /// <summary>
    /// Shop scene for purchasing chips, items, and cosmetics.
    /// </summary>
    public class ShopScene : MonoBehaviour
    {
        private GameService _gameService;
        
        // UI References
        private TextMeshProUGUI chipsBalanceText;
        private TextMeshProUGUI gemsBalanceText;
        private Transform chipsContainer;
        private Transform itemsContainer;
        private Transform cosmeticsContainer;
        private Transform tabsContainer;
        
        private List<GameObject> _shopItems = new List<GameObject>();
        
        private enum ShopTab { Chips, Items, Cosmetics }
        private ShopTab _currentTab = ShopTab.Chips;
        
        private void Start()
        {
            _gameService = GameService.Instance;
            BuildScene();
            ShowTab(ShopTab.Chips);
        }
        
        private void BuildScene()
        {
            var theme = Theme.Current;
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            gameObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            gameObject.AddComponent<GraphicRaycaster>();
            
            // Background
            var bg = UIFactory.CreatePanel(transform, "Background", theme.backgroundColor);
            bg.GetComponent<RectTransform>().anchorMin = Vector2.zero;
            bg.GetComponent<RectTransform>().anchorMax = Vector2.one;
            bg.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
            
            BuildHeader();
            BuildTabs();
            BuildChipsSection();
            BuildItemsSection();
            BuildCosmeticsSection();
        }
        
        private void BuildHeader()
        {
            var theme = Theme.Current;
            
            var header = UIFactory.CreatePanel(transform, "Header", theme.cardPanelColor);
            var headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 0.88f);
            headerRect.anchorMax = Vector2.one;
            headerRect.sizeDelta = Vector2.zero;
            
            // Title
            var title = UIFactory.CreateTitle(header.transform, "Title", "SHOP", 42f);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.03f, 0);
            titleRect.anchorMax = new Vector2(0.3f, 1);
            titleRect.sizeDelta = Vector2.zero;
            title.color = theme.accentColor;
            
            // Chips balance
            var chipsPanel = UIFactory.CreatePanel(header.transform, "ChipsPanel", theme.successColor);
            var chipsRect = chipsPanel.GetComponent<RectTransform>();
            chipsRect.anchorMin = new Vector2(0.5f, 0.2f);
            chipsRect.anchorMax = new Vector2(0.68f, 0.8f);
            chipsRect.sizeDelta = Vector2.zero;
            
            var chipsIcon = UIFactory.CreateText(chipsPanel.transform, "Icon", "🪙", 24f, Color.white);
            var iconRect = chipsIcon.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.02f, 0);
            iconRect.anchorMax = new Vector2(0.18f, 1);
            iconRect.sizeDelta = Vector2.zero;
            
            chipsBalanceText = UIFactory.CreateText(chipsPanel.transform, "Balance", "0", 20f, Color.white);
            chipsBalanceText.fontStyle = FontStyles.Bold;
            var balanceRect = chipsBalanceText.GetComponent<RectTransform>();
            balanceRect.anchorMin = new Vector2(0.2f, 0);
            balanceRect.anchorMax = new Vector2(0.98f, 1);
            balanceRect.sizeDelta = Vector2.zero;
            
            // Gems balance (premium currency)
            var gemsPanel = UIFactory.CreatePanel(header.transform, "GemsPanel", new Color(0.8f, 0.2f, 0.9f));
            var gemsRect = gemsPanel.GetComponent<RectTransform>();
            gemsRect.anchorMin = new Vector2(0.7f, 0.2f);
            gemsRect.anchorMax = new Vector2(0.85f, 0.8f);
            gemsRect.sizeDelta = Vector2.zero;
            
            var gemsIcon = UIFactory.CreateText(gemsPanel.transform, "Icon", "G", 24f, Color.white);
            var gIconRect = gemsIcon.GetComponent<RectTransform>();
            gIconRect.anchorMin = new Vector2(0.02f, 0);
            gIconRect.anchorMax = new Vector2(0.22f, 1);
            gIconRect.sizeDelta = Vector2.zero;
            
            gemsBalanceText = UIFactory.CreateText(gemsPanel.transform, "Balance", "0", 20f, Color.white);
            gemsBalanceText.fontStyle = FontStyles.Bold;
            var gBalanceRect = gemsBalanceText.GetComponent<RectTransform>();
            gBalanceRect.anchorMin = new Vector2(0.24f, 0);
            gBalanceRect.anchorMax = new Vector2(0.98f, 1);
            gBalanceRect.sizeDelta = Vector2.zero;
            
            // Back button
            var backBtn = UIFactory.CreateButton(header.transform, "Back", "← BACK", () => SceneManager.LoadScene("MainMenuScene"));
            var backRect = backBtn.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.88f, 0.2f);
            backRect.anchorMax = new Vector2(0.98f, 0.8f);
            backRect.sizeDelta = Vector2.zero;
            
            UpdateBalances();
        }
        
        private void BuildTabs()
        {
            var theme = Theme.Current;
            
            var tabs = UIFactory.CreatePanel(transform, "Tabs", Color.clear);
            tabsContainer = tabs.transform;
            var tabsRect = tabs.GetComponent<RectTransform>();
            tabsRect.anchorMin = new Vector2(0.02f, 0.8f);
            tabsRect.anchorMax = new Vector2(0.98f, 0.87f);
            tabsRect.sizeDelta = Vector2.zero;
            
            var hlg = tabs.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.childControlWidth = true;
            hlg.childForceExpandWidth = true;
            hlg.padding = new RectOffset(0, 0, 5, 5);
            
            CreateTabButton("CHIPS", ShopTab.Chips);
            CreateTabButton("ITEMS", ShopTab.Items);
            CreateTabButton("COSMETICS", ShopTab.Cosmetics);
        }
        
        private void CreateTabButton(string label, ShopTab tab)
        {
            var theme = Theme.Current;
            var btn = UIFactory.CreateButton(tabsContainer, $"Tab_{tab}", label, () => ShowTab(tab));
            btn.GetComponent<Image>().color = theme.cardPanelColor;
        }
        
        private void BuildChipsSection()
        {
            var theme = Theme.Current;
            
            var section = UIFactory.CreatePanel(transform, "ChipsSection", Color.clear);
            chipsContainer = section.transform;
            var sectionRect = section.GetComponent<RectTransform>();
            sectionRect.anchorMin = new Vector2(0.02f, 0.02f);
            sectionRect.anchorMax = new Vector2(0.98f, 0.78f);
            sectionRect.sizeDelta = Vector2.zero;
            
            var glg = section.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(280, 200);
            glg.spacing = new Vector2(25, 25);
            glg.padding = new RectOffset(30, 30, 30, 30);
            glg.childAlignment = TextAnchor.UpperCenter;
            
            // Chip packages
            CreateChipPackage("Starter Stack", 10000, 0.99f);
            CreateChipPackage("Small Stack", 50000, 4.99f);
            CreateChipPackage("Medium Stack", 150000, 9.99f);
            CreateChipPackage("Big Stack", 500000, 24.99f);
            CreateChipPackage("High Roller", 1500000, 49.99f);
            CreateChipPackage("Whale Pack", 5000000, 99.99f);
        }
        
        private void CreateChipPackage(string name, int chips, float price)
        {
            var theme = Theme.Current;
            
            var package = UIFactory.CreatePanel(chipsContainer, $"Package_{name}", theme.cardPanelColor);
            
            var vlg = package.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10;
            vlg.padding = new RectOffset(15, 15, 20, 15);
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            
            // Icon
            var icon = UIFactory.CreateText(package.transform, "Icon", "🪙", 48f, theme.successColor);
            icon.GetOrAddComponent<LayoutElement>().preferredHeight = 60;
            icon.alignment = TextAlignmentOptions.Center;
            
            // Name
            var nameText = UIFactory.CreateText(package.transform, "Name", name, 20f, theme.textPrimary);
            nameText.GetOrAddComponent<LayoutElement>().preferredHeight = 30;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.fontStyle = FontStyles.Bold;
            
            // Amount
            var amountText = UIFactory.CreateText(package.transform, "Amount", chips.ToString("N0"), 26f, theme.successColor);
            amountText.GetOrAddComponent<LayoutElement>().preferredHeight = 35;
            amountText.alignment = TextAlignmentOptions.Center;
            
            // Buy button
            var buyBtn = UIFactory.CreateButton(package.transform, "Buy", $"${price:F2}", () => OnBuyChips(name, chips, price));
            buyBtn.GetOrAddComponent<LayoutElement>().preferredHeight = 45;
            buyBtn.GetComponent<Image>().color = theme.primaryColor;
        }
        
        private void BuildItemsSection()
        {
            var theme = Theme.Current;
            
            var section = UIFactory.CreatePanel(transform, "ItemsSection", Color.clear);
            itemsContainer = section.transform;
            var sectionRect = section.GetComponent<RectTransform>();
            sectionRect.anchorMin = new Vector2(0.02f, 0.02f);
            sectionRect.anchorMax = new Vector2(0.98f, 0.78f);
            sectionRect.sizeDelta = Vector2.zero;
            
            var glg = section.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(220, 260);
            glg.spacing = new Vector2(20, 20);
            glg.padding = new RectOffset(30, 30, 30, 30);
            glg.childAlignment = TextAnchor.UpperCenter;
            
            // Items
            CreateItemForSale("XP Boost (1hr)", "Double XP for 1 hour", "⚡", 500, "uncommon");
            CreateItemForSale("Lucky Chip", "5% bonus on wins", "🍀", 1000, "rare");
            CreateItemForSale("Card Protector", "Protect your hand", "🛡️", 2500, "epic");
            CreateItemForSale("Golden Ticket", "Free tournament entry", "🎫", 5000, "legendary");
            
            section.SetActive(false);
        }
        
        private void CreateItemForSale(string name, string desc, string icon, int gemsPrice, string rarity)
        {
            var theme = Theme.Current;
            
            var item = UIFactory.CreatePanel(itemsContainer, $"Item_{name}", GetRarityColor(rarity));
            
            var vlg = item.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8;
            vlg.padding = new RectOffset(12, 12, 15, 12);
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            
            // Icon
            var iconText = UIFactory.CreateText(item.transform, "Icon", icon, 52f, Color.white);
            iconText.GetOrAddComponent<LayoutElement>().preferredHeight = 65;
            iconText.alignment = TextAlignmentOptions.Center;
            
            // Name
            var nameText = UIFactory.CreateText(item.transform, "Name", name, 16f, Color.white);
            nameText.GetOrAddComponent<LayoutElement>().preferredHeight = 25;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.fontStyle = FontStyles.Bold;
            
            // Description
            var descText = UIFactory.CreateText(item.transform, "Desc", desc, 12f, new Color(1, 1, 1, 0.8f));
            descText.GetOrAddComponent<LayoutElement>().preferredHeight = 40;
            descText.alignment = TextAlignmentOptions.Center;
            descText.enableWordWrapping = true;
            
            // Rarity
            var rarityText = UIFactory.CreateText(item.transform, "Rarity", rarity.ToUpper(), 11f, new Color(1, 1, 1, 0.6f));
            rarityText.GetOrAddComponent<LayoutElement>().preferredHeight = 18;
            rarityText.alignment = TextAlignmentOptions.Center;
            
            // Buy button
            var buyBtn = UIFactory.CreateButton(item.transform, "Buy", $"{gemsPrice}G", () => OnBuyItem(name, gemsPrice));
            buyBtn.GetOrAddComponent<LayoutElement>().preferredHeight = 40;
            buyBtn.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.15f, 0.9f);
        }
        
        private void BuildCosmeticsSection()
        {
            var theme = Theme.Current;
            
            var section = UIFactory.CreatePanel(transform, "CosmeticsSection", Color.clear);
            cosmeticsContainer = section.transform;
            var sectionRect = section.GetComponent<RectTransform>();
            sectionRect.anchorMin = new Vector2(0.02f, 0.02f);
            sectionRect.anchorMax = new Vector2(0.98f, 0.78f);
            sectionRect.sizeDelta = Vector2.zero;
            
            var glg = section.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(200, 220);
            glg.spacing = new Vector2(20, 20);
            glg.padding = new RectOffset(30, 30, 30, 30);
            glg.childAlignment = TextAnchor.UpperCenter;
            
            // Cosmetics
            CreateCosmeticForSale("Card Back: Fire", "*", 200, "card_back");
            CreateCosmeticForSale("Card Back: Ice", "❄️", 200, "card_back");
            CreateCosmeticForSale("Avatar: Crown", "👑", 500, "avatar");
            CreateCosmeticForSale("Avatar: Sunglasses", "😎", 300, "avatar");
            CreateCosmeticForSale("Table: Velvet", "🎰", 1000, "table");
            CreateCosmeticForSale("Table: Neon", "✨", 1500, "table");
            
            section.SetActive(false);
        }
        
        private void CreateCosmeticForSale(string name, string icon, int gemsPrice, string type)
        {
            var theme = Theme.Current;
            
            var item = UIFactory.CreatePanel(cosmeticsContainer, $"Cosmetic_{name}", theme.cardPanelColor);
            
            var vlg = item.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10;
            vlg.padding = new RectOffset(12, 12, 18, 12);
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            
            // Icon
            var iconText = UIFactory.CreateText(item.transform, "Icon", icon, 56f, Color.white);
            iconText.GetOrAddComponent<LayoutElement>().preferredHeight = 70;
            iconText.alignment = TextAlignmentOptions.Center;
            
            // Name
            var nameText = UIFactory.CreateText(item.transform, "Name", name, 15f, theme.textPrimary);
            nameText.GetOrAddComponent<LayoutElement>().preferredHeight = 35;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.fontStyle = FontStyles.Bold;
            nameText.enableWordWrapping = true;
            
            // Type
            var typeText = UIFactory.CreateText(item.transform, "Type", type.Replace("_", " ").ToUpper(), 11f, theme.textSecondary);
            typeText.GetOrAddComponent<LayoutElement>().preferredHeight = 18;
            typeText.alignment = TextAlignmentOptions.Center;
            
            // Buy button
            var buyBtn = UIFactory.CreateButton(item.transform, "Buy", $"{gemsPrice}G", () => OnBuyCosmetic(name, gemsPrice));
            buyBtn.GetOrAddComponent<LayoutElement>().preferredHeight = 40;
            buyBtn.GetComponent<Image>().color = theme.primaryColor;
        }
        
        private void ShowTab(ShopTab tab)
        {
            _currentTab = tab;
            
            chipsContainer.gameObject.SetActive(tab == ShopTab.Chips);
            itemsContainer.gameObject.SetActive(tab == ShopTab.Items);
            cosmeticsContainer.gameObject.SetActive(tab == ShopTab.Cosmetics);
            
            // Update tab button styles
            foreach (Transform child in tabsContainer)
            {
                var img = child.GetComponent<Image>();
                var isActive = child.name.Contains(tab.ToString());
                img.color = isActive ? Theme.Current.primaryColor : Theme.Current.cardPanelColor;
            }
        }
        
        private void UpdateBalances()
        {
            var user = GameService.Instance?.CurrentUser;
            if (user != null)
            {
                chipsBalanceText.text = user.chips.ToString("N0");
                gemsBalanceText.text = (user.gems).ToString("N0");
            }
        }
        
        private Color GetRarityColor(string rarity)
        {
            return rarity?.ToLower() switch
            {
                "legendary" => new Color(1f, 0.5f, 0f),
                "epic" => new Color(0.6f, 0.2f, 0.8f),
                "rare" => new Color(0.2f, 0.5f, 1f),
                "uncommon" => new Color(0.2f, 0.8f, 0.3f),
                _ => new Color(0.5f, 0.5f, 0.5f)
            };
        }
        
        private void OnBuyChips(string name, int chips, float price)
        {
            Debug.Log($"[Shop] Buying {name} ({chips} chips) for ${price}");
            // TODO: Integrate with payment system
            Components.ToastNotification.Show($"Purchase: {name} - Coming Soon!", Components.ToastType.Info);
        }
        
        private void OnBuyItem(string name, int gemsPrice)
        {
            Debug.Log($"[Shop] Buying item {name} for {gemsPrice} gems");
            // TODO: Call GameService to purchase
            Components.ToastNotification.Show($"Purchasing {name}...", Components.ToastType.Info);
        }
        
        private void OnBuyCosmetic(string name, int gemsPrice)
        {
            Debug.Log($"[Shop] Buying cosmetic {name} for {gemsPrice} gems");
            // TODO: Call GameService to purchase
            Components.ToastNotification.Show($"Purchasing {name}...", Components.ToastType.Info);
        }
    }
}

