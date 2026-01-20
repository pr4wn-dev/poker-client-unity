using UnityEngine;
using System.Collections.Generic;

namespace PokerClient.Core
{
    /// <summary>
    /// Manages all game audio - sound effects and music.
    /// Assign audio clips in the Inspector or load from Resources.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager _instance;
        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<AudioManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("AudioManager");
                        _instance = go.AddComponent<AudioManager>();
                    }
                }
                return _instance;
            }
        }
        
        [Header("Audio Sources")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource uiSource;
        
        [Header("Volume Settings")]
        [Range(0f, 1f)] public float masterVolume = 1f;
        [Range(0f, 1f)] public float sfxVolume = 1f;
        [Range(0f, 1f)] public float musicVolume = 0f;  // Muted by default
        [Range(0f, 1f)] public float uiVolume = 1f;
        
        [Header("=== SOUND EFFECTS - Assign or Load from Resources ===")]
        [Header("Card Sounds")]
        public AudioClip cardDeal;
        public AudioClip cardFlip;
        public AudioClip cardShuffle;
        
        [Header("Chip Sounds")]
        public AudioClip chipBet;
        public AudioClip chipWin;
        public AudioClip chipStack;
        public AudioClip allIn;
        
        [Header("Action Sounds")]
        public AudioClip fold;
        public AudioClip check;
        public AudioClip call;
        public AudioClip raise;
        public AudioClip turnTimer;
        public AudioClip turnTimerWarning;
        
        [Header("Countdown Sounds")]
        public AudioClip countdownBeep;
        public AudioClip readyToRumble; // "Let's get ready to rumble!" sound
        
        [Header("UI Sounds")]
        public AudioClip buttonClick;
        public AudioClip buttonHover;
        public AudioClip notification;
        public AudioClip error;
        public AudioClip success;
        public AudioClip menuOpen;
        public AudioClip menuClose;
        
        [Header("Game Event Sounds")]
        public AudioClip gameStart;
        public AudioClip handWin;
        public AudioClip handLose;
        public AudioClip royalFlush;
        public AudioClip playerJoin;
        public AudioClip playerLeave;
        
        [Header("Adventure Sounds")]
        public AudioClip bossAppear;
        public AudioClip bossDefeat;
        public AudioClip bossWin;
        public AudioClip levelUp;
        public AudioClip itemDrop;
        public AudioClip rareItemDrop;
        
        [Header("Music Tracks")]
        public AudioClip menuMusic;
        public AudioClip lobbyMusic;
        public AudioClip tableMusic;
        public AudioClip adventureMusic;
        public AudioClip bossMusic;
        public AudioClip victoryMusic;
        
        // PlayerPrefs keys
        private const string KEY_MASTER_VOL = "audio_master";
        private const string KEY_SFX_VOL = "audio_sfx";
        private const string KEY_MUSIC_VOL = "audio_music";
        private const string KEY_UI_VOL = "audio_ui";
        
        // Currently playing music
        private AudioClip _currentMusic;
        
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            SetupAudioSources();
            LoadVolumeSettings();
            LoadAudioFromResources();
        }
        
        /// <summary>
        /// Auto-load audio clips from Resources folder if not assigned in Inspector
        /// </summary>
        private void LoadAudioFromResources()
        {
            // SFX - Card sounds
            if (cardDeal == null) cardDeal = Resources.Load<AudioClip>("Audio/SFX/card_deal");
            if (cardFlip == null) cardFlip = Resources.Load<AudioClip>("Audio/SFX/card_flip");
            if (cardShuffle == null) cardShuffle = Resources.Load<AudioClip>("Audio/SFX/card_shuffle");
            
            // SFX - Chip sounds
            if (chipBet == null) chipBet = Resources.Load<AudioClip>("Audio/SFX/chip_bet");
            if (chipWin == null) chipWin = Resources.Load<AudioClip>("Audio/SFX/chip_win");
            if (chipStack == null) chipStack = Resources.Load<AudioClip>("Audio/SFX/chip_stack");
            if (allIn == null) allIn = Resources.Load<AudioClip>("Audio/SFX/all_in");
            
            // SFX - Action sounds
            if (fold == null) fold = Resources.Load<AudioClip>("Audio/SFX/fold");
            if (check == null) check = Resources.Load<AudioClip>("Audio/SFX/check");
            if (call == null) call = Resources.Load<AudioClip>("Audio/SFX/call");
            if (raise == null) raise = Resources.Load<AudioClip>("Audio/SFX/raise");
            if (turnTimer == null) turnTimer = Resources.Load<AudioClip>("Audio/SFX/timer_tick");
            if (turnTimerWarning == null) turnTimerWarning = Resources.Load<AudioClip>("Audio/SFX/timer_warning");
            
            // SFX - Countdown sounds
            if (countdownBeep == null) countdownBeep = Resources.Load<AudioClip>("Audio/SFX/countdown_beep");
            if (readyToRumble == null) readyToRumble = Resources.Load<AudioClip>("Audio/SFX/ready_to_rumble");
            
            // SFX - UI sounds
            if (buttonClick == null) buttonClick = Resources.Load<AudioClip>("Audio/SFX/button_click");
            if (buttonHover == null) buttonHover = Resources.Load<AudioClip>("Audio/SFX/button_hover");
            if (notification == null) notification = Resources.Load<AudioClip>("Audio/SFX/notification");
            if (error == null) error = Resources.Load<AudioClip>("Audio/SFX/error");
            if (success == null) success = Resources.Load<AudioClip>("Audio/SFX/success");
            
            // SFX - Game events
            if (gameStart == null) gameStart = Resources.Load<AudioClip>("Audio/SFX/game_start");
            if (handWin == null) handWin = Resources.Load<AudioClip>("Audio/SFX/hand_win");
            if (handLose == null) handLose = Resources.Load<AudioClip>("Audio/SFX/hand_lose");
            if (royalFlush == null) royalFlush = Resources.Load<AudioClip>("Audio/SFX/royal_flush");
            if (playerJoin == null) playerJoin = Resources.Load<AudioClip>("Audio/SFX/player_join");
            if (playerLeave == null) playerLeave = Resources.Load<AudioClip>("Audio/SFX/player_leave");
            
            // SFX - Adventure
            if (levelUp == null) levelUp = Resources.Load<AudioClip>("Audio/SFX/level_up");
            if (itemDrop == null) itemDrop = Resources.Load<AudioClip>("Audio/SFX/item_drop");
            
            // Music
            if (menuMusic == null) menuMusic = Resources.Load<AudioClip>("Audio/Music/menu_music");
            if (lobbyMusic == null) lobbyMusic = Resources.Load<AudioClip>("Audio/Music/lobby_music");
            if (tableMusic == null) tableMusic = Resources.Load<AudioClip>("Audio/Music/table_music");
            if (adventureMusic == null) adventureMusic = Resources.Load<AudioClip>("Audio/Music/adventure_music");
            if (bossMusic == null) bossMusic = Resources.Load<AudioClip>("Audio/Music/boss_music");
            if (victoryMusic == null) victoryMusic = Resources.Load<AudioClip>("Audio/Music/victory_music");
        }
        
        private void SetupAudioSources()
        {
            if (sfxSource == null)
            {
                var sfxObj = new GameObject("SFX Source");
                sfxObj.transform.SetParent(transform);
                sfxSource = sfxObj.AddComponent<AudioSource>();
                sfxSource.playOnAwake = false;
            }
            
            if (musicSource == null)
            {
                var musicObj = new GameObject("Music Source");
                musicObj.transform.SetParent(transform);
                musicSource = musicObj.AddComponent<AudioSource>();
                musicSource.playOnAwake = false;
                musicSource.loop = true;
            }
            
            if (uiSource == null)
            {
                var uiObj = new GameObject("UI Source");
                uiObj.transform.SetParent(transform);
                uiSource = uiObj.AddComponent<AudioSource>();
                uiSource.playOnAwake = false;
            }
        }
        
        private void LoadVolumeSettings()
        {
            masterVolume = PlayerPrefs.GetFloat(KEY_MASTER_VOL, 1f);
            sfxVolume = PlayerPrefs.GetFloat(KEY_SFX_VOL, 1f);
            musicVolume = PlayerPrefs.GetFloat(KEY_MUSIC_VOL, 0f);  // Muted by default
            uiVolume = PlayerPrefs.GetFloat(KEY_UI_VOL, 1f);
            
            UpdateAllVolumes();
        }
        
        public void SaveVolumeSettings()
        {
            PlayerPrefs.SetFloat(KEY_MASTER_VOL, masterVolume);
            PlayerPrefs.SetFloat(KEY_SFX_VOL, sfxVolume);
            PlayerPrefs.SetFloat(KEY_MUSIC_VOL, musicVolume);
            PlayerPrefs.SetFloat(KEY_UI_VOL, uiVolume);
            PlayerPrefs.Save();
        }
        
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            UpdateAllVolumes();
            SaveVolumeSettings();
        }
        
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            UpdateAllVolumes();
            SaveVolumeSettings();
        }
        
        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            UpdateAllVolumes();
            SaveVolumeSettings();
        }
        
        public void SetUIVolume(float volume)
        {
            uiVolume = Mathf.Clamp01(volume);
            UpdateAllVolumes();
            SaveVolumeSettings();
        }
        
        private void UpdateAllVolumes()
        {
            if (sfxSource) sfxSource.volume = masterVolume * sfxVolume;
            if (musicSource) musicSource.volume = masterVolume * musicVolume;
            if (uiSource) uiSource.volume = masterVolume * uiVolume;
        }
        
        #region Play Methods
        
        /// <summary>
        /// Play a sound effect
        /// </summary>
        public void PlaySFX(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null)
            {
                Debug.LogWarning($"[AudioManager] PlaySFX called with null clip - sound will not play");
                return;
            }
            if (sfxSource == null)
            {
                Debug.LogWarning("[AudioManager] PlaySFX called but sfxSource is null");
                return;
            }
            
            // Comprehensive audio logging for simulation debugging
            Debug.Log($"[AUDIO] PlaySFX: clip={clip.name}, length={clip.length:F2}s, volume={volumeScale * sfxVolume * masterVolume:F2}, time={Time.time:F3}");
            
            sfxSource.PlayOneShot(clip, volumeScale);
        }
        
        /// <summary>
        /// Play a UI sound
        /// </summary>
        public void PlayUI(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null || uiSource == null) return;
            uiSource.PlayOneShot(clip, volumeScale);
        }
        
        /// <summary>
        /// Play music track (cross-fades if music already playing)
        /// </summary>
        public void PlayMusic(AudioClip clip, bool fadeIn = true)
        {
            if (clip == null || clip == _currentMusic) return;
            
            _currentMusic = clip;
            
            if (fadeIn && musicSource.isPlaying)
            {
                StartCoroutine(CrossfadeMusic(clip));
            }
            else
            {
                musicSource.clip = clip;
                musicSource.Play();
            }
        }
        
        private System.Collections.IEnumerator CrossfadeMusic(AudioClip newClip)
        {
            float fadeTime = 1f;
            float startVolume = musicSource.volume;
            
            // Fade out
            for (float t = 0; t < fadeTime; t += Time.deltaTime)
            {
                musicSource.volume = Mathf.Lerp(startVolume, 0, t / fadeTime);
                yield return null;
            }
            
            // Switch and fade in
            musicSource.clip = newClip;
            musicSource.Play();
            
            for (float t = 0; t < fadeTime; t += Time.deltaTime)
            {
                musicSource.volume = Mathf.Lerp(0, masterVolume * musicVolume, t / fadeTime);
                yield return null;
            }
            
            musicSource.volume = masterVolume * musicVolume;
        }
        
        public void StopMusic(bool fadeOut = true)
        {
            if (!musicSource.isPlaying) return;
            
            if (fadeOut)
            {
                StartCoroutine(FadeOutMusic());
            }
            else
            {
                musicSource.Stop();
            }
            _currentMusic = null;
        }
        
        private System.Collections.IEnumerator FadeOutMusic()
        {
            float fadeTime = 1f;
            float startVolume = musicSource.volume;
            
            for (float t = 0; t < fadeTime; t += Time.deltaTime)
            {
                musicSource.volume = Mathf.Lerp(startVolume, 0, t / fadeTime);
                yield return null;
            }
            
            musicSource.Stop();
            musicSource.volume = masterVolume * musicVolume;
        }
        
        #endregion
        
        #region Convenience Methods
        
        // Card sounds
        public void PlayCardDeal() => PlaySFX(cardDeal);
        public void PlayCardFlip() => PlaySFX(cardFlip);
        public void PlayCardShuffle() => PlaySFX(cardShuffle);
        
        // Chip sounds
        public void PlayChipBet() => PlaySFX(chipBet);
        public void PlayChipWin() => PlaySFX(chipWin);
        public void PlayAllIn() => PlaySFX(allIn);
        
        // Action sounds
        public void PlayFold() => PlaySFX(fold);
        public void PlayCheck() => PlaySFX(check);
        public void PlayCall() => PlaySFX(call);
        public void PlayRaise() => PlaySFX(raise);
        
        // UI sounds
        public void PlayButtonClick() => PlayUI(buttonClick);
        public void PlayButtonHover() => PlayUI(buttonHover);
        public void PlayNotification() => PlayUI(notification);
        public void PlayError() => PlayUI(error);
        public void PlaySuccess() => PlayUI(success);
        
        // Game events
        public void PlayHandWin() => PlaySFX(handWin);
        public void PlayHandLose() => PlaySFX(handLose);
        public void PlayRoyalFlush() => PlaySFX(royalFlush, 1.2f);
        
        // Adventure
        public void PlayBossAppear() => PlaySFX(bossAppear);
        public void PlayBossDefeat() => PlaySFX(bossDefeat);
        public void PlayLevelUp() => PlaySFX(levelUp);
        public void PlayItemDrop() => PlaySFX(itemDrop);
        public void PlayRareItemDrop() => PlaySFX(rareItemDrop, 1.2f);
        
        // Countdown sounds
        public void PlayCountdownBeep()
        {
            if (countdownBeep == null)
            {
                Debug.LogWarning("[AudioManager] countdownBeep is null - cannot play sound. Add Audio/SFX/countdown_beep to Resources folder.");
                return;
            }
            PlaySFX(countdownBeep);
        }
        
        public void PlayReadyToRumble()
        {
            if (readyToRumble == null)
            {
                Debug.LogWarning("[AudioManager] readyToRumble is null - cannot play sound. Add Audio/SFX/ready_to_rumble to Resources folder.");
                return;
            }
            PlaySFX(readyToRumble, 1.0f);
        }
        
        // Scene music
        public void PlayMenuMusic() => PlayMusic(menuMusic);
        public void PlayLobbyMusic() => PlayMusic(lobbyMusic);
        public void PlayTableMusic() => PlayMusic(tableMusic);
        public void PlayAdventureMusic() => PlayMusic(adventureMusic);
        public void PlayBossMusic() => PlayMusic(bossMusic);
        public void PlayVictoryMusic() => PlayMusic(victoryMusic);
        
        #endregion
        
        #region Poker Action Helper
        
        /// <summary>
        /// Play appropriate sound for a poker action
        /// </summary>
        public void PlayPokerAction(string action, int? amount = null)
        {
            switch (action?.ToLower())
            {
                case "fold":
                    PlayFold();
                    break;
                case "check":
                    PlayCheck();
                    break;
                case "call":
                    PlayCall();
                    PlayChipBet();
                    break;
                case "bet":
                case "raise":
                    PlayRaise();
                    PlayChipBet();
                    break;
                case "allin":
                case "all-in":
                    PlayAllIn();
                    break;
            }
        }
        
        #endregion
    }
}

