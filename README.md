# CrazyLabs – Sled Run

A slingshot-launched sled run built on the Miraculous Ladybug asset pack, made for the Unity gameplay test.
Pull the slingshot, launch, steer down the slope, dodge obstacles, collect croissants, reach the finish line.
Coins are kept between runs and spent on permanent upgrades.

- Unity **2023.1.22f1**, Built-in render pipeline
- Packages: Addressables, UniTask, DOTween, TextMeshPro, and the in-house **gSDK** (`Packages/gSDK`)

## How to run

1. Open the project in Unity 2023.1.22f1.
2. Open `Assets/Scenes/Main.unity` and press Play. It is the only scene and is the one in Build Settings.
3. Addressables must be built for a player build (`Window > Asset Management > Addressables > Groups > Build`); the editor plays from the asset database.

`Main` registers the configs and services; everything else is created by them at runtime.

## How to play

| Action | Touch | Keyboard / mouse |
|---|---|---|
| Aim and pull the slingshot | Press and drag **down** from anywhere | Hold **Space** to charge |
| Aim the launch sideways | Drag left or right while pulling (launch goes the opposite way, like a real slingshot) | Hold **Left / Right** while charging |
| Launch | Release | Release Space / mouse |
| Steer | Drag left or right | **Left / Right** or **A / D** |
| Pause | Pause button (top of the HUD) | Pause button |

- Input is a force, not a position: steering turns the sled and it keeps its heading until you steer back.
- Presses that start over UI are ignored by the gameplay input.
- Hitting a **crash** obstacle ends the run at once. **Slow** obstacles cut your speed. Running out of momentum or reaching the finish also ends the run.
- The result popup shows distance, coins earned, and your wallet, with the upgrade list, **Retry** and **Home**.

### Upgrades (persistent)

| Upgrade | Effect | Levels |
|---|---|---|
| Launch Power | Stronger slingshot launch | 5 |
| Slope Speed | Less friction and drag, higher top speed | 5 |
| Steering | Faster sideways response | 5 |
| Currency Value | Each croissant is worth more | 4 |

Costs grow geometrically per level. All values live in `Assets/Configs/Upgrades/`. Wallet and upgrade levels are saved with `PlayerPrefs` through `ISaveData`.

### Content

- **3 levels**: Sunny Park, Snowy Park, Louvre. Each has its own layout, materials, obstacles, scenery, fog and sky.
- **9 playable heroes**: Ladybug, Marinette, Alix, Alya, Chloe, Kagami, Queen Bee, Viperion, Cosmo Bug. The transformation sound plays when you select one, and the voice (Ladybug or Adrien) follows the hero's gender when a run is lost.
- Feedback: camera shake (small on slow hits, large on crash), slide dust, pick-up, hit, crash and finish particles, and sounds for launch, pick-up, hits per surface, sliding and finishing.

## Architecture

MVC on top of gSDK. Unity is used as little as possible: there is one `Update`, in `GameplayView`, and everything else is ticked from it.

```
Main (MonoBehaviour)          registers configs + services in a Locator
 ├─ UIService / PopupService  (gSDK) layers, loading, popup stack
 ├─ PlayerService             wallet, selected hero, upgrade levels, persistence
 ├─ LevelService              selected level
 ├─ MainMenuService           MainMenuController + MainMenuView
 └─ GameplayService           owns the run flow
     ├─ GameplayController + GameplayView        the world
     │   ├─ PlayerController + PlayerView        input, state machine, animations
     │   └─ modules: Track, Atmosphere, Camera, Feedback (Audio + Particles)
     ├─ GameplayUIController + GameplayUIView    HUD
     └─ PausePopup / ResultPopup controllers + views
```

- **Services** are `BaseService`s found through the `Locator`. Controllers talk to services, services to each other (for example `GameplayService` → `MainMenuService` to go home). There is no event for it.
- **Controllers / views**: `ViewController`, `UIViewController` and `PopupViewController` own an Addressable view instance; views derive from `BaseView<TController>`. Views are cached hidden and shown on demand.
- **Modules** are plain C# classes (or one MonoBehaviour for the track) and are ticked by `GameplayView`:
  - `InputModule` – wraps touch, mouse and keyboard into tapped / dragged / held; ignores presses that start over UI.
  - `SledModule` – the physics-free sled: speed × `Time.deltaTime`, gravity from the slope, friction, drag, steering as a turn rate.
  - `SlingshotModule` – pull strength and aim; `SlingshotVisual` draws the band.
  - `TrackModule` – builds the ground meshes from the level layout, places the finish line and slingshot, and spawns obstacles, croissants and scenery from `ComponentPool`s.
  - `CameraModule` – smoothed follow that tilts with the slope, FOV boost with speed, trauma-based shake.
  - `AtmosphereModule` – fog, sky, sun and the level's ambient effect.
  - `FeedbackModule` – `AudioModule` and `ParticleModule`.
- **Run state** is a gSDK `StateMachine` in `PlayerController`: `AimingState` → `RunningState` → `ResultState`.
- **Events** (`EventDispatcher`) are used only where several unrelated listeners care: `RunLaunchedEvent`, `CollectibleCollectedEvent`, `ObstacleHitEvent`, `RunEndedEvent`, `UpgradePurchasedEvent`.
- **Pause** sets `Time.timeScale` and `AudioListener.pause`; popup tweens run on unscaled time.
- The gameplay view is reused across levels. Only the atmosphere and track are rebuilt when the level changes.

### Data

All tuning is data, not code:

```
Assets/Configs/
  GameplayConfigSO        sled, controls, slingshot, camera, audio, effects, flow, ground, spawn
  AllLevelsSO / Levels/   track layout (sections of length + pitch), materials, fog, obstacles,
                          scenery, spawn chances per level
  AllCharactersSO / Characters/   prefab, voice, animation pools, transformation sound
  AllUpgradeDefinitionsSO / Upgrades/
```

`Main._configs` must contain `GameplayConfigSO` and the three `All*SO` assets.

### Addressable keys

`Prefabs/GameplayView`, `Prefabs/PlayerView`, `Prefabs/Characters/Char_<Hero>` (one per hero), `UI/Prefabs/GameplayUIView`, `UI/Prefabs/MainMenuView`, `UI/Prefabs/PausePopup`, `UI/Prefabs/ResultView`, `UI/Prefabs/PopupBackground`. Obstacle prefabs are Addressables too, addressed by asset path.

### Folders

```
Assets/Scripts    code, by feature (Gameplay, Player, Levels, Characters, MainMenu, Upgrades, UI, SaveData)
Assets/Configs    ScriptableObject data
Assets/Assets     prefabs, models, materials, audio, particles
Assets/Editor     small editor tools (legacy Text → TextMeshPro converter)
Packages/gSDK     the in-house SDK
```

## Design decisions

- **Slopes are data.** A level is a list of sections (length, pitch in degrees). The ground mesh, the sled height and the camera tilt all come from one smoothed `TrackProfile`, so they always agree.
- **Props are seeded.** Each run respawns obstacles, croissants and scenery from a random seed, from pools; there is always a gap between paired obstacles so there is a way through.
- **One hero, one prefab.** Each hero is an Addressable prefab holding the model and its Avatar. `PlayerView` loads it as the visuals, and plays a shared `AnimatorController` through an `AnimatorOverrideController` that swaps in a random launch, crash or victory clip from the hero's pools.
- **Cheap shaders.** The asset pack shipped without its custom shaders, so materials were remapped to built-in ones (Mobile/Diffuse, legacy cutout and particle shaders).

## Known limitations

- Cosmo Bug's mask material still has the missing shader and renders without it.
- Cat Noir, Adrien, Carapace, Rena Rouge and Nino were part of the pack but are not included: they were removed because their models, textures or avatars were incomplete.
- Balancing (speeds, slopes, costs) was tuned by hand, not against telemetry.
- Persistence uses `PlayerPrefs`.
