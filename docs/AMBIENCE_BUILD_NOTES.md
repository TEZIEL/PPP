# Ambient Build Notes

본 문서는 앰비언트 사운드 제작/합성 과정을 기록하기 위한 제작 메모입니다.  
현재 Audacity 프로젝트 파일은 없고, 원본 파일만 보관되어 있습니다.  
향후 Audacity 또는 다른 편집 툴에서 최종 루프 파일을 제작할 경우, 아래 항목을 갱신합니다.

---

## General Production Notes

- Source files are stored separately as original files.
- Audacity project files are not currently available.
- Final loop files should be exported separately for game use.
- Recommended final format:
  - Working master: WAV, 44.1kHz, 16bit or 24bit
  - Game import: OGG for long ambient loops
  - Short one-shot SFX: WAV
- Recommended target length:
  - Ambient loops: 60–180 seconds
  - Short transition ambience: 15–45 seconds
- Recommended editing:
  - Volume balancing
  - EQ / high cut / low cut
  - Reverb if needed
  - Crossfade loop editing
  - Avoid overly clear speech if the sound plays under dialogue
  - Avoid sharp high-frequency sounds that may fatigue the player

---

## 01. wave

### Intended Final File

- `AMB_wave_loop.ogg`

### Source Files

| ID | Source File | Creator |
|---|---|---|
| 01-1 | `Perast, Montenegro - Small waves and boat passing in distance.wav` | signaturesounds |
| 01-2 | `441223__devy32__boat-horn.aiff` | devy32 |

### Planned Use

- 01-1: main sea/wave background layer
- 01-2: occasional distant boat horn point sound

### Editing Notes

- Keep wave layer low and stable.
- Use boat horn sparingly; avoid frequent repetition.
- Apply slight reverb or distance EQ to boat horn if it feels too close.
- Use crossfade at loop boundary.

---

## 02. firework

### Intended Final File

- `AMB_firework_loop.ogg`

### Source Files

| ID | Source File | Creator |
|---|---|---|
| 02-1 | `Ambiance_Sea_Loop_Stereo.wav` | nox sound |
| 02-2 | `Far Away Fireworks-01.wav` | signaturesounds |
| 02-3 | `Far Away Fireworks-02.wav` | signaturesounds |

### Planned Use

- 02-1: sea ambience base layer
- 02-2, 02-3: distant firework point sounds

### Editing Notes

- Fireworks should sound distant, not foreground.
- Lower high frequencies if fireworks are too sharp.
- Randomize placement to avoid rhythmic repetition.
- Consider long loop length, 90–180 seconds.

---

## 03. light rain

### Intended Final File

- `AMB_light_rain_loop.ogg`

### Source Files

| ID | Source File | Creator |
|---|---|---|
| 03-1 | `Light rain recordings mixed settings-01.wav` | signaturesounds |

### Planned Use

- Single-source light rain ambience.

### Editing Notes

- Check loop boundary carefully.
- Reduce harsh high frequencies if rain becomes tiring.
- If used under dialogue, keep volume low.
- Optionally add subtle room tone later if needed.

---

## 04. heavy rain

### Intended Final File

- `AMB_heavy_rain_loop.ogg`

### Source Files

| ID | Source File | Creator |
|---|---|---|
| 04-1 | `Ambiance_Rain_Calm_Loop_Stereo.wav` | nox sound |
| 04-2 | `581122__fission9__distant-thunder-1.wav` | Fission9 |
| 04-3 | `581123__fission9__distant-thunder-2.wav` | Fission9 |
| 04-4 | `581125__fission9__distant-thunder-4.wav` | Fission9 |

### Planned Use

- 04-1: rain base layer
- 04-2, 04-3, 04-4: distant thunder point sounds

### Editing Notes

- Thunder should be occasional and not too loud.
- Use low volume and distance EQ for thunder.
- Avoid predictable thunder timing.
- Keep final loop long enough to prevent repeated thunder pattern from becoming obvious.

---

## 05. morning

### Intended Final File

- `AMB_morning_loop.ogg`

### Source Files

| ID | Source File | Creator |
|---|---|---|
| 05-1 | `393699__vdr3__birds-loop.flac` | vdr3 |
| 05-2 | `697496__geoff-bremner-audio__gentle-relaxing-stream-2.wav` | Geoff-Bremner-Audio |
| 05-3 | `Risan, Montenegro- Church bells.WAV` | signaturesounds |

### Planned Use

- 05-1: bird ambience
- 05-2: stream ambience
- 05-3: distant church bell point sound

### Editing Notes

- Birds and stream should be balanced so neither dominates.
- Church bells should be rare and distant.
- Reduce sharp bird frequencies if they distract from dialogue.
- Good candidate for calm morning scene.

---

## 06. night

### Intended Final File

- `AMB_night_loop.ogg`

### Source Files

| ID | Source File | Creator |
|---|---|---|
| 06-1 | `522298__defelozedd94__crickets-at-night-clean-sound.wav` | Defelozedd94 |
| 06-2 | `Ambiance_Wind_Calm_Loop_Stereo.wav` | nox sound |

### Planned Use

- 06-1: cricket night ambience
- 06-2: calm wind layer

### Editing Notes

- Crickets can become repetitive; keep volume low.
- Wind should be subtle and not mask the crickets entirely.
- Apply EQ if high-frequency insect sound becomes fatiguing.
- Useful for night outdoor scenes.

---

## 07. campfire

### Intended Final File

- `AMB_campfire_loop.ogg`

### Source Files

| ID | Source File | Creator |
|---|---|---|
| 07-1 | `Ambiance_Firecamp_Small_Loop_Mono.wav` | nox sound |
| 07-2 | `Ambiance_Wind_Forest_Loop_Stereo.wav` | nox sound |

### Planned Use

- 07-1: main campfire layer
- 07-2: forest wind layer

### Editing Notes

- Campfire should be warm but not too loud.
- Wind should sit behind the fire layer.
- Consider slight stereo widening if final fire layer feels too narrow.
- Avoid overly sharp crackles.

---

## 08. cafe

### Intended Final File

- `AMB_cafe_loop.ogg`

### Source Files

| ID | Source File | Creator |
|---|---|---|
| 08-1 | `Perast, Montenegro - Cafe Walla By The Waterfront.WAV` | signaturesounds |

### Planned Use

- Cafe walla / waterfront ambience.

### Editing Notes

- If speech is too clear, reduce 2kHz–4kHz range.
- Keep cafe walla low under dialogue scenes.
- Consider high cut to make it less distracting.
- If loop point is noticeable, make a longer final loop.

---

## 09. subway

### Intended Final File

- `AMB_subway_loop.ogg`

### Source Files

| ID | Source File | Creator |
|---|---|---|
| 09-1 | `Boarding tube and interior sounds.wav` | signaturesounds |

### Planned Use

- Subway boarding / train interior ambience.

### Rights Note

- Individual product page should be archived.
- Signature Sounds About page CC0 statement should also be archived because the product page may not visibly show CC0 in the body text.

### Editing Notes

- If announcements are too clear, reduce mid/high frequencies or lower volume.
- Train movement can be used as a transition or loop base.
- Avoid using speech-heavy sections under VN dialogue.
- Consider making both a loop version and a short transition version.

---

## 10. grocery

### Intended Final File

- `AMB_grocery_loop.ogg`

### Source Files

| ID | Source File | Creator |
|---|---|---|
| 10-1 | `Grocery store recordings-01.wav` | signaturesounds |
| 10-2 | `166178__jacobzeier__vhs-hum.wav` | jacobzeier |

### Planned Use

- 10-1: supermarket ambience
- 10-2: low VHS hum / retro noise layer

### Editing Notes

- VHS hum should be very low; it is a texture, not a main sound.
- Supermarket ambience should not contain overly clear speech if used under dialogue.
- Good candidate for mallsoft / retro store atmosphere.
- Consider using hum to tie the ambience to the retro OS aesthetic.

---

## Final Export Checklist

Before exporting final game audio, check:

- [ ] Source files are recorded in `SOUND_RIGHTS_AMBIENCE.md`
- [ ] Evidence screenshots are saved
- [ ] Final loop does not click at the boundary
- [ ] Final loop does not contain distracting speech
- [ ] High-frequency noise is not fatiguing
- [ ] Volume is low enough to sit under BGM or dialogue
- [ ] Exported file name matches the intended final file name
- [ ] Final file is stored in `/03_Final_Game_Audio`
