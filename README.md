# Dragon Hunt: Changelog for Version 0.1.3

Group 12 · Game Design and Development (26CSSE03I) · Unity 2021.3.10f1

This file lists every change from the first **"Version 0.1.3"** push up to the latest push, and then the
**known bugs** and **warnings** that still need fixing.

---

## 1. Pushes covered

| # | Commit | Date (Cairo) | Author | Message |
|---|--------|--------------|--------|---------|
| 1 | `685c005` | 28 Nov 2026, 13:58 | mohamed2064 | Version 0.1.3 |
| 2 | `124baea` | 28 Nov 2026, 19:47 | otg2005 | Version 0.1.3 Audio for warlord and crossbow |
| 3 | `2c1c2d1` | 28 Nov 2026, 21:11 | otg2005 | Version 0.1.3 Animations for Freya, Script to be added |
| 4 | `19bb9bc` | 28 Nov 2026, 21:25 | otg2005 | Version 0.1.3 Animations for Freya, warlord, crossbow, Scripts to be added |
| 5 | `58f1299` | 28 Nov 2026, 21:59 | otg2005 | Version 0.1.3 Animations for Freya, warlord, crossbow, with armies added |

---

## 2. Changes by push

### 2.1 `685c005`: Version 0.1.3 (Dagger Raider, Level 3, shared prefabs)
- **`DaggerRaider.cs`**
  - Uses Animator parameters instead of always playing "Run": `Speed`, `Height`, plus the `Attack` and `Damage` triggers.
  - When he dies he stops, his collider turns off, and he is removed after 0.4 s, so the hit animation has time to play.
- **Dagger Raider animator** (`dagger_raider_spritesheet_0.controller`): new states `idle`, `jump`, `fall`, `hit` and
  `Damage`, with new clips `Damage.anim`, `fall.anim`, `hit.anim` and `jump.anim`.
- **New shared prefabs:** `Canvas.prefab` (Erik's portrait, health bar and lives) and `Levelmanger.prefab`
  (`levelmanger` + `RespawnManager`).
- **`Level_3.unity`:** Canvas, Levelmanger, mushroom props and a Beast Rider added.

### 2.2 `124baea`: Audio for Warlord and Crossbow Hunter
- Crossbow Hunter: `CrossBow_Hunter_Intro`, `Crossbow_Attack`, `Crossbow_Damage`, `Crossbow_Defeat`, `Crossbow_taunt` (.wav).
- Warlord: `Warlord_Intro`, `Warlord_Initiate_Attck`, `Warlord_Mid_Attack`, `Warlord_Damage`, `Warlord_Defeat` (.wav).

### 2.3 `2c1c2d1`: Freya animations, Erik controls, Level 5 art
- **Freya:**
  - 7 clips: `Freya_Standing`, `Freya_Running`, `Freya_Stop`, `Freya_Jump`, `Freya_Fall`, `Freya_Attack`, `Freya_Damage`.
  - A new animator, `freya_spritesheet_0 1.controller`.
  - A new `freya_spritesheet_0.prefab` (Rigidbody2D, BoxCollider2D, Animator, empty `Freya_Controller` script).
- **`PlayerController.cs`:** two key sets per action (`jump/jump2`, `left/left2`, `right/right2`).
  Erik's prefab uses Space/W to jump, ←/A to go left and →/D to go right. His jump height went from 10 to 7.
- **Erik's prefab:** `PlayerAttack` attack key changed from **X** to **K**.
- **New art:**
  - `crossbow_hunter_army_spritesheet.png` and `warlord_army_spritesheet.png`.
  - `crossbow_arrow.png` (in Resources).
  - A new `Level_5_Background.jpg`.
- **New music:** `bgm_boss_warlord.mp3` (in Resources/Audio).
- **`Level_5.unity`:** Erik, Freya, a Crossbow Hunter and the Warlord placed. The Canvas and Levelmanger prefabs were added.

### 2.4 `19bb9bc`: Crossbow Hunter and Warlord animations
- Crossbow Hunter clips: `Cross_Standing`, `Cross_Running`, `Cross_Stopping`, `Cross_Jumping`, `Cross_Fall`,
  `Cross_Attack`, `Cross_Damage`.
- Warlord clips: `Warlord_Standing`, `Warlord_Running`, `Warlord_Stopping`, `Warlord_Jumping`, `Warlord_Falling`,
  `Warlord_Attacking`, `Warlord_Damage`.
- Both controllers were created, with the states only and no transitions yet.

### 2.5 `58f1299`: Animators, scripts, armies and the Level 5 setup
**Animators** (each one has a clear flow with parameters and transitions):

| Controller | Parameters | Flow |
|---|---|---|
| `crossbow_hunter_spritesheet_0` | Speed, Height, Grounded, Aiming, Shoot, Reloading, Hit, Dead | Standing ↔ Running → Stopping · Jumping / Falling · **Aim → Shoot → Reload** → Standing · Any → Hurt / Defeat |
| `warlord_spritesheet_0` | Speed, Height, Grounded, Attack (int), Release, Vulnerable, Hit, Dead | Locomotion as above · **Strike_Windup → Strike**, **Lunge_Windup → Lunge**, **Sweep_Windup → Sweep → Recover** · Any → Hurt / Defeat |
| `freya_spritesheet_0 1` (rebuilt) | Speed, Height, Grounded, Attack, Hit | Locomotion as above · Any → Freya_Attack / Freya_Damage → back to Standing |

- Freya's old controller had states with no way out (Attack, Damage and Fall), so she would freeze. It was rebuilt
  with the same clips.
- **Army animators** (`Army/` folders) are override controllers. They reuse the controllers above and only swap the
  pictures:
  - `crossbow_army.overrideController` with 10 `CrossArmy_*` clips.
  - `warlord_army.overrideController` with 14 `WarlordArmy_*` clips.
- **New clips for the main characters:**
  - Crossbow Hunter: `Cross_Aim`, `Cross_Shoot`, `Cross_Reload`, `Cross_Defeat`.
  - Warlord: `Warlord_StrikeWindup`, `Warlord_Strike`, `Warlord_LungeWindup`, `Warlord_Lunge`,
    `Warlord_SweepWindup`, `Warlord_Sweep`, `Warlord_Recover`, `Warlord_Defeat`.
- **`warlord_army_spritesheet` re-sliced:**
  - 28 sprites, one row at a time, because the rows are not evenly spaced.
  - A custom pivot keeps the feet in the same place in every animation.

**New scripts** (all commented):

| Script | What it does |
|---|---|
| `CrossbowHunter.cs` | Notices Erik and aims for 0.8 s while the crossbow flashes and the "click" plays (the warning). Then he shoots a bolt at Erik's chest and reloads for 1.6 s, which is the window to hit him. If Erik gets close while he isn't reloading, he jumps back. Optional patrol. Also used by the crossbow army. |
| `CrossbowBolt.cs` | Flies straight, hurts Erik once, breaks on the Ground layer, and can be knocked out of the air by the axe. |
| `Warlord.cs` | The final boss: Strike / Lunge / Sweep, each with a wind-up pose. The Sweep is low, so Erik can jump over it. After the Sweep he goes into **Recover**, the opening. He blocks hits outside his openings. 250 HP (10 axe hits), faster wind-ups below half health, a boss bar, Freya callouts and an `onDefeated` event. With **Is Boss** off it is the army spear soldier. |
| `Freya_Controller.cs` | Follows a little behind Erik. Jumps for ledges, walls and gaps. Fights enemies that are near her and near Erik. Can't be defeated (only knocked back). Appears next to Erik if she falls too far behind or Erik respawns. |
| `ClipPlayer.cs` | Plays sound files dragged into a script in the Inspector. Long voice lines don't overlap. |
| `Editor/Level5Setup.cs` | The **Dragon Hunt > Setup Level 5** menu. It wires everything in Level 5 and can be run again safely. |

**New prefabs:**
- `CrossbowBolt`.
- `CrossbowArmy`: scale 1.4, 25 HP.
- `WarlordArmy`: scale 1.9, 75 HP, no blocking.

**What the setup tool changed in `Level_5.unity`:**
- **Crossbow Hunter:** Enemy layer and tag, Rigidbody2D, script, bolt prefab, FirePoint, Glint, and all 5 sounds.
- **Warlord:** Enemy layer and tag, heavy Rigidbody2D, boss script, all 5 sounds, and a **BossBar** ("THE WARLORD")
  on the Canvas.
- **Freya:** `Freya_Controller` and `FreyaCallout`, with a TextMeshPro **Bubble** above her head.
- **Erik:** health bar and lives UI linked.
- **Army:** 2 × CrossbowArmy (x = 33, 47) and 2 × WarlordArmy (x = 57, 70) placed.
- **Music:** a SoundManager playing `bgm_boss_warlord` with wind ambience.
- **Build:** `Level_5` added to **Build Settings**.

**Tested in Play mode:**
- No compile errors.
- Freya follows Erik with her run and stop animations.
- The Crossbow Hunter aims, shoots and hits Erik, and his intro and shot sounds play.

The Warlord fight has **not** been play-tested yet.

---

## 3. Bugs to fix

| # | Where | Problem | Suggested fix |
|---|-------|---------|---------------|
| B1 | Erik prefab: `PlayerAttack` vs `PlayerAnimatorParams` | **The attack is split across two keys.** `PlayerAttack` deals damage on **K**, but `PlayerAnimatorParams` plays the attack animation on **X**. The report says Attack = **R** and K = Call Shadow. | Set both `attackKey` fields to **R**. |
| B2 | `PlayerController.cs` (lines 59-61) | Sets Animator parameters `speed`, `Height` and `grounded`, which don't exist in Erik's controller (`Speed`, `height`, `ground`). This floods the Console with warnings (see W1). `PlayerAnimatorParams` already sets the right ones. | Delete the three `anim.Set…` lines in `PlayerController`. |
| B3 | Every scene | **No Game Over screen and no Pause menu.** `GameOverUI` and `PauseMenu` aren't in any scene, so losing the last life only logs *"no lives left but Game Over is not linked"*, and ESC does nothing. | Add a Game Over panel and a Pause panel to `Canvas.prefab` and link `gameOver` on Erik. |
| B4 | Build Settings / `LevelExit` | Only `SampleScene` and `Level_5` are in Build Settings. `LevelExit` isn't used in any scene, and its default `nextSceneName` is `"Level2"` while the scene is named `Level_2`. | Add all levels in order and place a `LevelExit` at the end of each level with the right scene name. |
| B5 | Level 4 | There is no Level 4 (Vorgrim) scene. | Create `Level_4`. |
| B6 | Level 5 | No checkpoints (the report wants one at the start and one near the end) and no fall-death zone (`DeadlyDrop`). | Add 2 × `TorchCheckpoint` and a `DeadlyDrop` under the level. |
| B7 | Level 5 ending | No final cage with the rare dragon. The Warlord's `onDefeated` event is empty, and there is no fly-home ending or final cutscene. | Add a `DragonCage` (rare dragon) that unlocks in `onDefeated`, then load the ending. |
| B8 | Level 5 layout | The ground is flat. There are no fortress walls or ledges, so the Crossbow Hunters aren't up on walls as the report describes. | Add wall platforms and put the hunters on them. |
| B9 | Mechanics in the report that aren't built | Call Shadow (K, 10 s cooldown, burns barriers, carries over gaps), Climb (W), Courage Spell (double damage for 10 s), max health +10 after each level. W is currently a second jump key. | Implement, or remove them from the report. |
| B10 | `levelmanger.cs` | `Respawnplayer()` uses `currentcheckpoint`, which is always `null` (crash if called). It duplicates `RespawnManager`. | Remove `levelmanger` or make it call `RespawnManager`. |
| B11 | `EnemyWave.cs` | Only accepts `BeastRider`, so waves of Crossbow, Warlord-army or Dagger enemies can't lock a gate. | Make it accept any enemy (for example through a shared `onDied` interface). |
| B12 | Level 5 Erik | The scene overrides Erik's `jumpHeight` to **5** (the prefab is 7), so he may not clear obstacles that work in other levels. | Remove the override or confirm 5 is intended. |

---

## 4. Warnings / things to watch

| # | Warning | Notes |
|---|---------|-------|
| W1 | **Console: 999+ warnings in Play mode:** *Parameter 'speed' / 'Height' / 'grounded' does not exist* | Caused by B2. They can hide real errors. |
| W2 | `crossbow_hunter_army_spritesheet` uses **trimmed (Automatic) slicing** with a center pivot | Frames are different sizes, so army crossbowmen may bob up and down between animations. Re-slice with a grid (224 × 171.4) or custom pivots like the warlord army sheet. |
| W3 | Army sprites are smaller than the main characters | The prefabs use scale 1.4 (crossbow) and 1.9 (spear). Keep this in mind when you place them or adjust colliders. |
| W4 | Very long / large audio files | `Warlord_Defeat.wav` is about 41 s (7.8 MB). `Warlord_Intro`, `CrossBow_Hunter_Intro` and `Crossbow_Defeat` are 23-25 s each. Trim them, or set them to *Compressed / Streaming* in the import settings. Army units don't play the long lines. |
| W5 | Unused assets | `Cross_Attack.anim` and `Warlord_Attacking.anim` were replaced by Aim/Shoot/Reload and the three attacks. `freya/anim.anim` and the one-state `freya_spritesheet_0.controller` are also unused. |
| W6 | Freya targets any enemy near Erik | This includes the Warlord before his fight starts. Hitting him starts the fight early. |
| W7 | Untested | The Warlord fight and the army soldiers haven't been played through yet. Tune `strikeBox`, `sweepBox`, wind-up times and `recoverTime` in the Inspector after a test. |
| W8 | Repo clean-up | 5 × `.DS_Store` files and `UpgradeLog.htm` are committed. Add `.DS_Store` to `.gitignore`. |
| W9 | Re-running the setup tool | **Dragon Hunt > Setup Level 5** re-applies its default values: stats, sounds and layers on the Crossbow Hunter, Warlord and Freya, and it rebuilds the army prefabs. Inspector changes to those are overwritten, so re-run it only to repair missing links. |
