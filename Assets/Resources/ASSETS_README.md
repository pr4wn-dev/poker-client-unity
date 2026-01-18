# Poker Game Assets

This folder contains all game assets loaded at runtime. Follow these instructions to add real assets.

## Folder Structure

```
Resources/
├── Audio/
│   ├── SFX/           # Sound effects
│   └── Music/         # Background music tracks
├── Sprites/
│   ├── Cards/         # Playing card sprites
│   ├── Avatars/       # Player avatar images
│   └── Chips/         # Poker chip sprites
```

---

## 🎴 CARD SPRITES

### Option 1: Free Card Deck (Recommended)
Download from OpenGameArt: https://opengameart.org/content/playing-cards-vector-png

1. Download the PNG pack
2. Place individual card images in `Sprites/Cards/`
3. Name format: `{rank}_{suit}.png`
   - Example: `A_hearts.png`, `K_spades.png`, `10_diamonds.png`, `2_clubs.png`
4. Add `card_back.png` for the card back

### Option 2: Kenney Assets (Public Domain)
https://kenney.nl/assets/playing-cards-pack

### Required Files:
```
Sprites/Cards/
├── A_hearts.png, A_diamonds.png, A_clubs.png, A_spades.png
├── 2_hearts.png, 2_diamonds.png, 2_clubs.png, 2_spades.png
├── 3_hearts.png ... (continue for all ranks 2-10, J, Q, K, A)
├── card_back.png
└── (52 cards + 1 back = 53 files)
```

---

## 🔊 SOUND EFFECTS

### Free SFX Sources:
- **Freesound.org**: https://freesound.org (free with attribution)
- **Kenney**: https://kenney.nl/assets/casino-audio (public domain)
- **OpenGameArt**: https://opengameart.org/content/54-casino-sound-effects

### Required SFX Files (place in `Audio/SFX/`):

```
Audio/SFX/
├── card_deal.wav       # Single card dealt
├── card_flip.wav       # Card revealed
├── card_shuffle.wav    # Deck shuffle
├── chip_bet.wav        # Chips placed
├── chip_win.wav        # Chips collected
├── chip_stack.wav      # Chip stacking sound
├── all_in.wav          # All-in announcement
├── fold.wav            # Fold action
├── check.wav           # Check/tap sound
├── call.wav            # Call action
├── raise.wav           # Raise action
├── timer_tick.wav      # Turn timer tick
├── timer_warning.wav   # Low time warning
├── button_click.wav    # UI button click
├── button_hover.wav    # UI hover sound
├── notification.wav    # Alert/notification
├── error.wav           # Error sound
├── success.wav         # Success chime
├── game_start.wav      # Game starting
├── hand_win.wav        # You won the hand
├── hand_lose.wav       # You lost
├── royal_flush.wav     # Special hand sound
├── player_join.wav     # Player joined table
├── player_leave.wav    # Player left
├── level_up.wav        # Level up fanfare
└── item_drop.wav       # Item received
```

---

## 🎵 MUSIC

### Free Music Sources:
- **Incompetech**: https://incompetech.com/music/ (Kevin MacLeod, CC-BY)
- **OpenGameArt**: https://opengameart.org/content/5-chiptunes-action
- **FreePD**: https://freepd.com/

### Suggested Style:
- Jazz/lounge for menus
- Smooth jazz for lobby
- Low-key casino ambience for tables
- Tension music for boss battles

### Required Music Files (place in `Audio/Music/`):

```
Audio/Music/
├── menu_music.ogg      # Main menu (looping)
├── lobby_music.ogg     # Lobby screen
├── table_music.ogg     # Poker table gameplay
├── adventure_music.ogg # Adventure mode
├── boss_music.ogg      # Boss battle tension
└── victory_music.ogg   # Victory fanfare
```

**Format**: Use `.ogg` for music (smaller file size, good quality)

---

## 👤 AVATARS

### Free Avatar Sources:
- **Kenney**: https://kenney.nl/assets/boardgame-icons
- **Game-icons.net**: https://game-icons.net/
- **OpenGameArt avatars**: https://opengameart.org/art-search?keys=avatar

### Required Avatar Files (place in `Sprites/Avatars/`):

```
Sprites/Avatars/
├── default_1.png       # Default male
├── default_2.png       # Default female  
├── default_3.png       # Neutral/other
├── bot_tex.png         # Tex bot avatar
├── bot_larry.png       # Lazy Larry avatar
├── bot_pickles.png     # Pickles avatar
└── (add more for unlockables)
```

**Size**: 128x128 or 256x256 pixels recommended

---

## 🎰 CHIP SPRITES

### Free Chip Sources:
- **Kenney Casino**: https://kenney.nl/assets/casino-pack
- **OpenGameArt**: https://opengameart.org/content/poker-chips

### Required Chip Files (place in `Sprites/Chips/`):

```
Sprites/Chips/
├── chip_white.png      # $1
├── chip_red.png        # $5
├── chip_blue.png       # $10
├── chip_green.png      # $25
├── chip_black.png      # $100
├── chip_purple.png     # $500
└── chip_yellow.png     # $1000
```

---

## Unity Import Settings

After adding files, select them in Unity and set:

### For Sprites:
- Texture Type: **Sprite (2D and UI)**
- Sprite Mode: **Single**
- Filter Mode: **Bilinear**
- Compression: **Normal Quality**

### For Audio:
- Load Type: **Decompress On Load** (for SFX)
- Load Type: **Streaming** (for Music)
- Compression Format: **Vorbis** (quality 70-80%)

---

## Quick Test

After adding assets, the game will automatically load them from Resources. Test by:
1. Open Unity
2. Play the MainMenuScene
3. You should hear menu music
4. Create a table and verify card sprites appear

---

## Attribution

If using Creative Commons assets, add credits to your game's credits screen:
- Card art by [Artist Name]
- Sound effects from [Source]
- Music by [Composer] (CC-BY)



