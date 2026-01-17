using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using PokerClient.UI;
using PokerClient.Core;

namespace PokerClient.UI.Scenes
{
    /// <summary>
    /// Settings scene - volume, graphics, and account settings
    /// </summary>
    public class SettingsScene : MonoBehaviour
    {
        private Canvas _canvas;
        private string _previousScene = "MainMenuScene";
        
        // Audio sliders
        private Slider _masterVolumeSlider;
        private Slider _musicVolumeSlider;
        private Slider _sfxVolumeSlider;
        private Slider _uiVolumeSlider;
        
        // Volume labels
        private TextMeshProUGUI _masterVolumeLabel;
        private TextMeshProUGUI _musicVolumeLabel;
        private TextMeshProUGUI _sfxVolumeLabel;
        private TextMeshProUGUI _uiVolumeLabel;
        
        // Graphics toggles
        private Toggle _fullscreenToggle;
        private TMP_Dropdown _resolutionDropdown;
        private TMP_Dropdown _qualityDropdown;
        private Toggle _vsyncToggle;
        
        // Gameplay
        private Toggle _autoMuckToggle;
        private Toggle _showCardsOnFoldToggle;
        private Toggle _vibrationToggle;
        private TMP_Dropdown _cardStyleDropdown;
        private TMP_Dropdown _tableStyleDropdown;
        
        private void Start()
        {
            // Try to remember where we came from
            _previousScene = PlayerPrefs.GetString("settings_return_scene", "MainMenuScene");
            
            BuildScene();
            LoadCurrentSettings();
        }
        
        public static void OpenSettings(string returnScene = "MainMenuScene")
        {
            PlayerPrefs.SetString("settings_return_scene", returnScene);
            SceneManager.LoadScene("SettingsScene");
        }
        
        private void BuildScene()
        {
            _canvas = FindObjectOfType<Canvas>();
            if (_canvas == null)
            {
                var canvasObj = new GameObject("Canvas");
                _canvas = canvasObj.AddComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasObj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                canvasObj.AddComponent<GraphicRaycaster>();
            }
            
            var theme = Theme.Current;
            
            // Background
            var bg = UIFactory.CreatePanel(_canvas.transform, "Background", theme.backgroundColor);
            var bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.sizeDelta = Vector2.zero;
            
            BuildHeader();
            BuildAudioSection();
            BuildGraphicsSection();
            BuildGameplaySection();
            BuildFooter();
        }
        
        private void BuildHeader()
        {
            var theme = Theme.Current;
            
            var header = UIFactory.CreatePanel(_canvas.transform, "Header", theme.cardPanelColor);
            var headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0, 0.9f);
            headerRect.anchorMax = Vector2.one;
            headerRect.sizeDelta = Vector2.zero;
            
            var title = UIFactory.CreateTitle(header.transform, "Title", "SETTINGS", 42f);
            var titleRect = title.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.05f, 0);
            titleRect.anchorMax = new Vector2(0.5f, 1);
            titleRect.sizeDelta = Vector2.zero;
            title.alignment = TextAlignmentOptions.MidlineLeft;
            title.color = theme.accentColor;
            
            // Back button
            var backBtn = UIFactory.CreateButton(header.transform, "Back", "← BACK", OnBackClick);
            var backRect = backBtn.GetComponent<RectTransform>();
            backRect.anchorMin = new Vector2(0.85f, 0.2f);
            backRect.anchorMax = new Vector2(0.98f, 0.8f);
            backRect.sizeDelta = Vector2.zero;
            backBtn.GetComponent<Image>().color = theme.dangerColor;
        }
        
        private void BuildAudioSection()
        {
            var theme = Theme.Current;
            
            var section = CreateSection("Audio", new Vector2(0.02f, 0.52f), new Vector2(0.48f, 0.88f));
            
            // Master volume
            var masterRow = CreateSliderRow(section.transform, "Master Volume", 1f);
            _masterVolumeSlider = masterRow.slider;
            _masterVolumeLabel = masterRow.label;
            _masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            
            // Music volume
            var musicRow = CreateSliderRow(section.transform, "Music Volume", 0.5f);
            _musicVolumeSlider = musicRow.slider;
            _musicVolumeLabel = musicRow.label;
            _musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            
            // SFX volume
            var sfxRow = CreateSliderRow(section.transform, "SFX Volume", 1f);
            _sfxVolumeSlider = sfxRow.slider;
            _sfxVolumeLabel = sfxRow.label;
            _sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            
            // UI volume
            var uiRow = CreateSliderRow(section.transform, "UI Volume", 1f);
            _uiVolumeSlider = uiRow.slider;
            _uiVolumeLabel = uiRow.label;
            _uiVolumeSlider.onValueChanged.AddListener(OnUIVolumeChanged);
        }
        
        private void BuildGraphicsSection()
        {
            var theme = Theme.Current;
            
            var section = CreateSection("Graphics", new Vector2(0.52f, 0.52f), new Vector2(0.98f, 0.88f));
            
            // Fullscreen toggle
            _fullscreenToggle = CreateToggleRow(section.transform, "Fullscreen", Screen.fullScreen);
            _fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
            
            // Resolution dropdown
            _resolutionDropdown = CreateDropdownRow(section.transform, "Resolution", 
                new string[] { "1920x1080", "1600x900", "1280x720", "1024x768" });
            _resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
            
            // Quality dropdown
            _qualityDropdown = CreateDropdownRow(section.transform, "Quality", 
                QualitySettings.names);
            _qualityDropdown.value = QualitySettings.GetQualityLevel();
            _qualityDropdown.onValueChanged.AddListener(OnQualityChanged);
            
            // VSync toggle
            _vsyncToggle = CreateToggleRow(section.transform, "VSync", QualitySettings.vSyncCount > 0);
            _vsyncToggle.onValueChanged.AddListener(OnVSyncChanged);
        }
        
        private void BuildGameplaySection()
        {
            var theme = Theme.Current;
            
            var section = CreateSection("Gameplay", new Vector2(0.02f, 0.12f), new Vector2(0.48f, 0.5f));
            
            // Auto muck losing hands
            _autoMuckToggle = CreateToggleRow(section.transform, "Auto-Muck Losing Hands", 
                PlayerPrefs.GetInt("auto_muck", 1) == 1);
            _autoMuckToggle.onValueChanged.AddListener(OnAutoMuckChanged);
            
            // Show cards on fold
            _showCardsOnFoldToggle = CreateToggleRow(section.transform, "Show Cards When Folding", 
                PlayerPrefs.GetInt("show_cards_fold", 0) == 1);
            _showCardsOnFoldToggle.onValueChanged.AddListener(OnShowCardsChanged);
            
            // Vibration (mobile)
            _vibrationToggle = CreateToggleRow(section.transform, "Vibration", 
                PlayerPrefs.GetInt("vibration", 1) == 1);
            _vibrationToggle.onValueChanged.AddListener(OnVibrationChanged);
            
            // Card style dropdown
            _cardStyleDropdown = CreateDropdownRow(section.transform, "Card Style", 
                new string[] { "Classic", "Modern", "4-Color" });
            _cardStyleDropdown.value = PlayerPrefs.GetInt("card_style", 0);
            _cardStyleDropdown.onValueChanged.AddListener(OnCardStyleChanged);
        }
        
        private void BuildFooter()
        {
            var theme = Theme.Current;
            
            var footer = UIFactory.CreatePanel(_canvas.transform, "Footer", theme.cardPanelColor);
            var footerRect = footer.GetComponent<RectTransform>();
            footerRect.anchorMin = new Vector2(0.52f, 0.12f);
            footerRect.anchorMax = new Vector2(0.98f, 0.5f);
            footerRect.sizeDelta = Vector2.zero;
            
            var vlg = footer.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 15;
            vlg.padding = new RectOffset(30, 30, 20, 20);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            
            // Section title
            var title = UIFactory.CreateTitle(footer.transform, "Title", "Account", 24f);
            title.GetOrAddComponent<LayoutElement>().preferredHeight = 35;
            title.color = theme.textPrimary;
            
            // Reset progress button (dangerous!)
            var resetBtn = UIFactory.CreateButton(footer.transform, "Reset", "RESET ALL PROGRESS", OnResetProgress);
            resetBtn.GetOrAddComponent<LayoutElement>().preferredHeight = 50;
            resetBtn.GetComponent<Image>().color = theme.dangerColor;
            
            // Logout button
            var logoutBtn = UIFactory.CreateButton(footer.transform, "Logout", "LOGOUT", OnLogout);
            logoutBtn.GetOrAddComponent<LayoutElement>().preferredHeight = 50;
            logoutBtn.GetComponent<Image>().color = theme.warningColor;
            
            // Save button
            var saveBtn = UIFactory.CreateButton(footer.transform, "Save", "SAVE SETTINGS", OnSaveClick);
            saveBtn.GetOrAddComponent<LayoutElement>().preferredHeight = 50;
            saveBtn.GetComponent<Image>().color = theme.primaryColor;
            
            // Version info
            var version = UIFactory.CreateText(footer.transform, "Version", 
                $"Version {Application.version}", 14f, theme.textSecondary);
            version.GetOrAddComponent<LayoutElement>().preferredHeight = 25;
            version.alignment = TextAlignmentOptions.Center;
        }
        
        #region Helpers
        
        private GameObject CreateSection(string title, Vector2 anchorMin, Vector2 anchorMax)
        {
            var theme = Theme.Current;
            
            var section = UIFactory.CreatePanel(_canvas.transform, $"Section_{title}", theme.cardPanelColor);
            var rect = section.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.sizeDelta = Vector2.zero;
            
            var vlg = section.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10;
            vlg.padding = new RectOffset(20, 20, 15, 15);
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlHeight = false;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandWidth = true;
            
            var titleText = UIFactory.CreateTitle(section.transform, "Title", title.ToUpper(), 24f);
            titleText.GetOrAddComponent<LayoutElement>().preferredHeight = 35;
            titleText.color = theme.textPrimary;
            
            return section;
        }
        
        private (Slider slider, TextMeshProUGUI label) CreateSliderRow(Transform parent, string labelText, float defaultValue)
        {
            var theme = Theme.Current;
            
            var row = UIFactory.CreatePanel(parent, $"Row_{labelText}", Color.clear);
            row.GetOrAddComponent<LayoutElement>().preferredHeight = 40;
            
            var label = UIFactory.CreateText(row.transform, "Label", labelText, 16f, theme.textPrimary);
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(0.4f, 1);
            labelRect.sizeDelta = Vector2.zero;
            
            var sliderObj = new GameObject("Slider");
            sliderObj.transform.SetParent(row.transform, false);
            var sliderRect = sliderObj.AddComponent<RectTransform>();
            sliderRect.anchorMin = new Vector2(0.42f, 0.2f);
            sliderRect.anchorMax = new Vector2(0.85f, 0.8f);
            sliderRect.sizeDelta = Vector2.zero;
            
            var slider = sliderObj.AddComponent<Slider>();
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.value = defaultValue;
            
            // Background
            var bgImg = UIFactory.CreatePanel(sliderObj.transform, "Background", theme.backgroundColor);
            var bgImgRect = bgImg.GetComponent<RectTransform>();
            bgImgRect.anchorMin = Vector2.zero;
            bgImgRect.anchorMax = Vector2.one;
            bgImgRect.sizeDelta = Vector2.zero;
            
            // Fill
            var fill = UIFactory.CreatePanel(sliderObj.transform, "Fill", theme.primaryColor);
            var fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(0.5f, 1);
            fillRect.sizeDelta = Vector2.zero;
            slider.fillRect = fillRect;
            
            // Handle
            var handle = UIFactory.CreatePanel(sliderObj.transform, "Handle", Color.white);
            var handleRect = handle.GetComponent<RectTransform>();
            handleRect.sizeDelta = new Vector2(20, 30);
            slider.handleRect = handleRect;
            slider.targetGraphic = handle.GetComponent<Image>();
            
            // Value label
            var valueLabel = UIFactory.CreateText(row.transform, "Value", $"{(int)(defaultValue * 100)}%", 16f, theme.accentColor);
            var valueLabelRect = valueLabel.GetComponent<RectTransform>();
            valueLabelRect.anchorMin = new Vector2(0.87f, 0);
            valueLabelRect.anchorMax = new Vector2(1, 1);
            valueLabelRect.sizeDelta = Vector2.zero;
            valueLabel.alignment = TextAlignmentOptions.Right;
            
            return (slider, valueLabel);
        }
        
        private Toggle CreateToggleRow(Transform parent, string labelText, bool defaultValue)
        {
            var theme = Theme.Current;
            
            var row = UIFactory.CreatePanel(parent, $"Row_{labelText}", Color.clear);
            row.GetOrAddComponent<LayoutElement>().preferredHeight = 35;
            
            var label = UIFactory.CreateText(row.transform, "Label", labelText, 16f, theme.textPrimary);
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(0.7f, 1);
            labelRect.sizeDelta = Vector2.zero;
            
            var toggleObj = new GameObject("Toggle");
            toggleObj.transform.SetParent(row.transform, false);
            var toggleRect = toggleObj.AddComponent<RectTransform>();
            toggleRect.anchorMin = new Vector2(0.8f, 0.1f);
            toggleRect.anchorMax = new Vector2(0.95f, 0.9f);
            toggleRect.sizeDelta = Vector2.zero;
            
            var toggle = toggleObj.AddComponent<Toggle>();
            toggle.isOn = defaultValue;
            
            var bgImg = toggleObj.AddComponent<Image>();
            bgImg.color = defaultValue ? theme.primaryColor : theme.backgroundColor;
            toggle.targetGraphic = bgImg;
            toggle.onValueChanged.AddListener(isOn => bgImg.color = isOn ? theme.primaryColor : theme.backgroundColor);
            
            // Checkmark
            var check = UIFactory.CreateText(toggleObj.transform, "Check", "✓", 20f, Color.white);
            var checkRect = check.GetComponent<RectTransform>();
            checkRect.anchorMin = Vector2.zero;
            checkRect.anchorMax = Vector2.one;
            checkRect.sizeDelta = Vector2.zero;
            check.alignment = TextAlignmentOptions.Center;
            toggle.graphic = check;
            
            return toggle;
        }
        
        private TMP_Dropdown CreateDropdownRow(Transform parent, string labelText, string[] options)
        {
            var theme = Theme.Current;
            
            var row = UIFactory.CreatePanel(parent, $"Row_{labelText}", Color.clear);
            row.GetOrAddComponent<LayoutElement>().preferredHeight = 40;
            
            var label = UIFactory.CreateText(row.transform, "Label", labelText, 16f, theme.textPrimary);
            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0, 0);
            labelRect.anchorMax = new Vector2(0.4f, 1);
            labelRect.sizeDelta = Vector2.zero;
            
            var dropdownObj = new GameObject("Dropdown");
            dropdownObj.transform.SetParent(row.transform, false);
            var dropdownRect = dropdownObj.AddComponent<RectTransform>();
            dropdownRect.anchorMin = new Vector2(0.42f, 0.1f);
            dropdownRect.anchorMax = new Vector2(0.98f, 0.9f);
            dropdownRect.sizeDelta = Vector2.zero;
            
            var dropdownImg = dropdownObj.AddComponent<Image>();
            dropdownImg.color = theme.backgroundColor;
            
            var dropdown = dropdownObj.AddComponent<TMP_Dropdown>();
            dropdown.ClearOptions();
            dropdown.AddOptions(new System.Collections.Generic.List<string>(options));
            
            // Label text
            var ddLabel = UIFactory.CreateText(dropdownObj.transform, "Label", options.Length > 0 ? options[0] : "", 16f, theme.textPrimary);
            var ddLabelRect = ddLabel.GetComponent<RectTransform>();
            ddLabelRect.anchorMin = new Vector2(0.05f, 0);
            ddLabelRect.anchorMax = new Vector2(0.9f, 1);
            ddLabelRect.sizeDelta = Vector2.zero;
            dropdown.captionText = ddLabel;
            
            // Template
            var template = UIFactory.CreatePanel(dropdownObj.transform, "Template", theme.cardPanelColor);
            var templateRect = template.GetComponent<RectTransform>();
            templateRect.anchorMin = new Vector2(0, 0);
            templateRect.anchorMax = new Vector2(1, 0);
            templateRect.pivot = new Vector2(0.5f, 1);
            templateRect.sizeDelta = new Vector2(0, 150);
            template.SetActive(false);
            dropdown.template = templateRect;
            
            return dropdown;
        }
        
        #endregion
        
        #region Event Handlers
        
        private void LoadCurrentSettings()
        {
            var audio = AudioManager.Instance;
            if (audio != null)
            {
                _masterVolumeSlider.value = audio.masterVolume;
                _musicVolumeSlider.value = audio.musicVolume;
                _sfxVolumeSlider.value = audio.sfxVolume;
                _uiVolumeSlider.value = audio.uiVolume;
            }
        }
        
        private void OnMasterVolumeChanged(float value)
        {
            _masterVolumeLabel.text = $"{(int)(value * 100)}%";
            AudioManager.Instance?.SetMasterVolume(value);
        }
        
        private void OnMusicVolumeChanged(float value)
        {
            _musicVolumeLabel.text = $"{(int)(value * 100)}%";
            AudioManager.Instance?.SetMusicVolume(value);
        }
        
        private void OnSFXVolumeChanged(float value)
        {
            _sfxVolumeLabel.text = $"{(int)(value * 100)}%";
            AudioManager.Instance?.SetSFXVolume(value);
        }
        
        private void OnUIVolumeChanged(float value)
        {
            _uiVolumeLabel.text = $"{(int)(value * 100)}%";
            AudioManager.Instance?.SetUIVolume(value);
        }
        
        private void OnFullscreenChanged(bool isOn)
        {
            Screen.fullScreen = isOn;
        }
        
        private void OnResolutionChanged(int index)
        {
            var resolutions = new (int w, int h)[] 
            { 
                (1920, 1080), (1600, 900), (1280, 720), (1024, 768) 
            };
            if (index >= 0 && index < resolutions.Length)
            {
                Screen.SetResolution(resolutions[index].w, resolutions[index].h, Screen.fullScreen);
            }
        }
        
        private void OnQualityChanged(int index)
        {
            QualitySettings.SetQualityLevel(index, true);
        }
        
        private void OnVSyncChanged(bool isOn)
        {
            QualitySettings.vSyncCount = isOn ? 1 : 0;
        }
        
        private void OnAutoMuckChanged(bool isOn)
        {
            PlayerPrefs.SetInt("auto_muck", isOn ? 1 : 0);
        }
        
        private void OnShowCardsChanged(bool isOn)
        {
            PlayerPrefs.SetInt("show_cards_fold", isOn ? 1 : 0);
        }
        
        private void OnVibrationChanged(bool isOn)
        {
            PlayerPrefs.SetInt("vibration", isOn ? 1 : 0);
        }
        
        private void OnCardStyleChanged(int index)
        {
            PlayerPrefs.SetInt("card_style", index);
        }
        
        private void OnResetProgress()
        {
            // TODO: Show confirmation dialog
            Debug.LogWarning("Reset progress requested - implement confirmation!");
        }
        
        private void OnLogout()
        {
            // TODO: Call GameService.Logout()
            Debug.Log("Logout requested");
            SceneManager.LoadScene("MainMenuScene");
        }
        
        private void OnSaveClick()
        {
            PlayerPrefs.Save();
            AudioManager.Instance?.SaveVolumeSettings();
            Debug.Log("Settings saved");
        }
        
        private void OnBackClick()
        {
            PlayerPrefs.Save();
            SceneManager.LoadScene(_previousScene);
        }
        
        #endregion
    }
}

