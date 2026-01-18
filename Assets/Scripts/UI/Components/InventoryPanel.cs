using UnityEngine;
using UnityEngine.UI;
using TMPro;
using PokerClient.Networking;
using System.Collections.Generic;

namespace PokerClient.UI.Components
{
    /// <summary>
    /// Inventory panel for viewing and managing items.
    /// </summary>
    public class InventoryPanel : MonoBehaviour
    {
        private RectTransform _rect;
        private CanvasGroup _canvasGroup;
        private GameService _gameService;
        
        private Transform _itemsContainer;
        private TextMeshProUGUI _selectedItemName;
        private TextMeshProUGUI _selectedItemDesc;
        private Image _selectedItemIcon;
        private Button _useButton;
        private Button _closeButton;
        
        private List<GameObject> _itemSlots = new List<GameObject>();
        private Item _selectedItem;
        
        public System.Action OnClose;
        public System.Action<Item> OnItemUsed;
        
        public static InventoryPanel Create(Transform parent)
        {
            var go = new GameObject("InventoryPanel");
            go.transform.SetParent(parent, false);
            var panel = go.AddComponent<InventoryPanel>();
            panel.Initialize();
            return panel;
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
            bg.color = new Color(0, 0, 0, 0.8f);
            
            // Main panel
            var panel = UIFactory.CreatePanel(transform, "Panel", theme.cardPanelColor);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.15f, 0.1f);
            panelRect.anchorMax = new Vector2(0.85f, 0.9f);
            panelRect.sizeDelta = Vector2.zero;
            
            BuildHeader(panel.transform);
            BuildItemsGrid(panel.transform);
            BuildItemDetail(panel.transform);
            
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
            
            var title = UIFactory.CreateTitle(header.transform, "Title", "INVENTORY", 32f);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.03f, 0);
            titleRect.anchorMax = new Vector2(0.7f, 1);
            titleRect.sizeDelta = Vector2.zero;
            title.color = theme.accentColor;
            
            _closeButton = UIFactory.CreateButton(header.transform, "Close", "✕", Hide).GetComponent<Button>();
            var closeRect = _closeButton.GetComponent<RectTransform>();
            closeRect.anchorMin = new Vector2(0.92f, 0.1f);
            closeRect.anchorMax = new Vector2(0.98f, 0.9f);
            closeRect.sizeDelta = Vector2.zero;
            _closeButton.GetComponent<Image>().color = theme.dangerColor;
        }
        
        private void BuildItemsGrid(Transform parent)
        {
            var theme = Theme.Current;
            
            var gridPanel = UIFactory.CreatePanel(parent, "ItemsGrid", theme.backgroundColor);
            var gridRect = gridPanel.GetComponent<RectTransform>();
            gridRect.anchorMin = new Vector2(0.02f, 0.02f);
            gridRect.anchorMax = new Vector2(0.6f, 0.9f);
            gridRect.sizeDelta = Vector2.zero;
            
            // Scroll view
            var scrollView = new GameObject("ScrollView");
            scrollView.transform.SetParent(gridPanel.transform, false);
            var scrollViewRect = scrollView.AddComponent<RectTransform>();
            scrollViewRect.anchorMin = Vector2.zero;
            scrollViewRect.anchorMax = Vector2.one;
            scrollViewRect.sizeDelta = Vector2.zero;
            
            var scroll = scrollView.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            
            var viewport = UIFactory.CreatePanel(scrollView.transform, "Viewport", Color.clear);
            viewport.AddComponent<Mask>().showMaskGraphic = false;
            var viewportRect = viewport.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.sizeDelta = Vector2.zero;
            scroll.viewport = viewportRect;
            
            var content = UIFactory.CreatePanel(viewport.transform, "Content", Color.clear);
            _itemsContainer = content.transform;
            var contentRect = content.GetComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0, 1);
            contentRect.anchorMax = new Vector2(1, 1);
            contentRect.pivot = new Vector2(0.5f, 1);
            
            var glg = content.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(80, 100);
            glg.spacing = new Vector2(10, 10);
            glg.padding = new RectOffset(10, 10, 10, 10);
            glg.childAlignment = TextAnchor.UpperLeft;
            
            var csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            scroll.content = contentRect;
        }
        
        private void BuildItemDetail(Transform parent)
        {
            var theme = Theme.Current;
            
            var detailPanel = UIFactory.CreatePanel(parent, "ItemDetail", theme.backgroundColor);
            var detailRect = detailPanel.GetComponent<RectTransform>();
            detailRect.anchorMin = new Vector2(0.62f, 0.02f);
            detailRect.anchorMax = new Vector2(0.98f, 0.9f);
            detailRect.sizeDelta = Vector2.zero;
            
            var vlg = detailPanel.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 15;
            vlg.padding = new RectOffset(20, 20, 20, 20);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            
            // Icon
            var iconHolder = UIFactory.CreatePanel(detailPanel.transform, "Icon", theme.cardPanelColor);
            iconHolder.GetOrAddComponent<LayoutElement>().preferredHeight = 120;
            iconHolder.GetOrAddComponent<LayoutElement>().preferredWidth = 120;
            _selectedItemIcon = iconHolder.GetComponent<Image>();
            
            // Name
            _selectedItemName = UIFactory.CreateTitle(detailPanel.transform, "Name", "Select an item", 24f);
            _selectedItemName.GetOrAddComponent<LayoutElement>().preferredHeight = 35;
            _selectedItemName.alignment = TextAlignmentOptions.Center;
            _selectedItemName.color = theme.textPrimary;
            
            // Description
            _selectedItemDesc = UIFactory.CreateText(detailPanel.transform, "Desc", "", 16f, theme.textSecondary);
            _selectedItemDesc.GetOrAddComponent<LayoutElement>().preferredHeight = 80;
            _selectedItemDesc.alignment = TextAlignmentOptions.Center;
            _selectedItemDesc.enableWordWrapping = true;
            
            // Use button
            _useButton = UIFactory.CreateButton(detailPanel.transform, "Use", "USE ITEM", OnUseClick).GetComponent<Button>();
            _useButton.GetOrAddComponent<LayoutElement>().preferredHeight = 50;
            _useButton.GetComponent<Image>().color = theme.primaryColor;
            _useButton.interactable = false;
        }
        
        public void Show()
        {
            _gameService = GameService.Instance;
            gameObject.SetActive(true);
            RefreshInventory();
        }
        
        public void Hide()
        {
            gameObject.SetActive(false);
            OnClose?.Invoke();
        }
        
        private void RefreshInventory()
        {
            // Clear existing
            foreach (var slot in _itemSlots)
            {
                Destroy(slot);
            }
            _itemSlots.Clear();
            
            // Get items from user profile
            var items = _gameService?.CurrentUser?.inventory;
            if (items == null) return;
            
            foreach (var item in items)
            {
                CreateItemSlot(item);
            }
        }
        
        private void CreateItemSlot(Item item)
        {
            var theme = Theme.Current;
            
            var slot = UIFactory.CreatePanel(_itemsContainer, $"Item_{item.id}", GetRarityColor(item.rarity));
            _itemSlots.Add(slot);
            
            var btn = slot.AddComponent<Button>();
            btn.targetGraphic = slot.GetComponent<Image>();
            btn.onClick.AddListener(() => SelectItem(item));
            
            // Item name
            var nameText = UIFactory.CreateText(slot.transform, "Name", item.name, 12f, Color.white);
            nameText.alignment = TextAlignmentOptions.Center;
            var nameRect = nameText.GetComponent<RectTransform>();
            nameRect.anchorMin = new Vector2(0, 0);
            nameRect.anchorMax = new Vector2(1, 0.25f);
            nameRect.sizeDelta = Vector2.zero;
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
        
        private void SelectItem(Item item)
        {
            _selectedItem = item;
            _selectedItemName.text = item.name;
            _selectedItemDesc.text = item.description ?? "No description";
            _useButton.interactable = item.type == "consumable" || item.type == "xp_boost";
        }
        
        private void OnUseClick()
        {
            if (_selectedItem == null) return;
            
            OnItemUsed?.Invoke(_selectedItem);
            // TODO: Call GameService to use item
        }
    }
}


