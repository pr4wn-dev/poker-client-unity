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
        public static AudioManager Instance { get; private set; }
        
        [Header("Audio Sources")]
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource musicSource;
        [SerializeField] private AudioSource uiSource;
        
        [Header("Volume Settings")]
        [Range(0f, 1f)] public float masterVolume = 1f;
        [Range(0f, 1f)] public float sfxVolume = 1f;
        [Range(0f, 1f)] public float musicVolume = 0.5f;
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
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            SetupAudioSources();
            LoadVolumeSettings();
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
            musicVolume = PlayerPrefs.GetFloat(KEY_MUSIC_VOL, 0.5f);
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
            if (clip == null || sfxSource == null) return;
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

