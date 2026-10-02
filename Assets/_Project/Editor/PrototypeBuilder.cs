using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CrazyLabs.Core;
using CrazyLabs.Effects;
using CrazyLabs.Player;
using CrazyLabs.Progression;
using CrazyLabs.Selection;
using CrazyLabs.Track;
using CrazyLabs.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CrazyLabs.EditorTools
{
    /// <summary>
    /// One-click assembly of the playable prototype from the Miraculous Ladybug asset pack:
    /// prefabs, materials, characters, levels, animator, scene and UI.
    ///
    /// Phase 1 creates every asset on disk. Phase 2 builds the scene and wires it by loading those
    /// assets fresh from disk, because Unity unloads unreferenced assets while importing and a
    /// stale object reference would be saved as an empty one.
    /// </summary>
    public static class PrototypeBuilder
    {
        const string ProjectRoot = "Assets/_Project";
        const string Geometry = "Assets/Ladybug/Content/Geometry";
        const string CharacterRoot = Geometry + "/Characters/Animations";
        const string Sounds = "Assets/Ladybug/Sound Manager/Sounds";
        const string UiTextures = "Assets/Ladybug/Content/UI Textures";
        const string ScenePath = "Assets/Scenes/SledRun.unity";
        const string TuningPath = ProjectRoot + "/Data/SledTuning.asset";
        const string AnimatorPath = ProjectRoot + "/Animation/Sled.controller";
        const string RepairedFolder = ProjectRoot + "/Materials/Repaired";
        const string LadybugModelPath = Geometry + "/Characters/UI/Ladybug/LadybugUI@TPose.fbx";
        const string LadybugTextures = Geometry + "/Characters/UI/Ladybug";
        const string FxQuest = "Assets/AssetStore/FX Quest/Particles/Prefabs/3D";
        const string PackFx = Geometry + "/Levels/AllLevels/FX/ParticleEffects";

        // ---------------------------------------------------------------------
        // Content tables
        // ---------------------------------------------------------------------

        // Display name, model, animation folder (under Characters/Animations), voice type, transformation sound.
        // Ladybug uses the UI model (the only Ladybug mesh in the pack) with rebuilt materials.
        static readonly (string name, string fbx, string folder, CharacterVoice voice, string sound)[] Characters =
        {
            ("Ladybug", LadybugModelPath, "Ladybug", CharacterVoice.Female, "LadyBug"),
            ("Cat Noir", CharacterRoot + "/CatNoir/CatNoir@TPose.FBX", "CatNoir", CharacterVoice.Male, "CatNoir"),
            ("Adrien", CharacterRoot + "/Adrien/Adrian@TPose.FBX", "Adrien", CharacterVoice.Male, "Adrien"),
            ("Marinette", CharacterRoot + "/Marinette/Marinette@TPose.FBX", "Marinette", CharacterVoice.Female, "Marinette"),
            ("Alya", CharacterRoot + "/Alya_Animations/Alya@TPose.FBX", "Alya_Animations", CharacterVoice.Female, "Ayla"),
            ("Chloe", CharacterRoot + "/Chloe/Chloe@TPose.fbx", "Chloe", CharacterVoice.Female, "Chloe"),
            ("Nino", CharacterRoot + "/Nino/Nino@TPose.FBX", "Nino", CharacterVoice.Male, "Nino"),
            ("Kagami", CharacterRoot + "/Kagami/Kagami_TPose.FBX", "Kagami", CharacterVoice.Female, "Kagami"),
            ("Carapace", CharacterRoot + "/Carapace/Carapace@TPose.FBX", "Carapace", CharacterVoice.Male, "Carapace"),
            ("Rena Rouge", CharacterRoot + "/RenaRouge/RR@TPose.FBX", "RenaRouge", CharacterVoice.Female, "RenaRouge"),
            ("Queen Bee", CharacterRoot + "/QueenBee/QueenBee@TPose.fbx", "QueenBee", CharacterVoice.Female, "QueenBee"),
            ("Cosmo Bug", CharacterRoot + "/CosmoBug/CosmoBug@TPose.FBX", "CosmoBug", CharacterVoice.Female, "LadyBug"),
            ("Alix", CharacterRoot + "/Alix/Alix@TPose.FBX", "Alix", CharacterVoice.Female, "Alix"),
            ("Viperion", CharacterRoot + "/Viperion/Viperion@TPose.FBX", "Viperion", CharacterVoice.Male, "Luka"),
        };

        // Animation pools are found by clip file name; heroes missing a category fall back to Ladybug's clips.
        static readonly Regex CrashClipName = new Regex(@"death|dead|fallfromheight|@fall$", RegexOptions.IgnoreCase);
        static readonly Regex LaunchClipName = new Regex(@"jump", RegexOptions.IgnoreCase);
        static readonly Regex LaunchClipExclude = new Regex(@"fall|float|down|torun|loop|land|wall|car|test|ui|cut|over|turn|vault", RegexOptions.IgnoreCase);
        static readonly Regex VictoryClipName = new Regex(@"victory|dance|cheer", RegexOptions.IgnoreCase);
        static readonly Regex VictoryClipExclude = new Regex(@"idle|breath|loop|start|redo|screen", RegexOptions.IgnoreCase);

        sealed class LevelSpec
        {
            public string name, file;
            public string trackMaterial, surroundMaterial;
            public Color sky, ambient, sun;
            public TrackLayout.Section[] sections;
            public float width;
            public string[] crash, slow, scenery;
            public string ambientEffect;
        }

        static readonly LevelSpec[] Levels =
        {
            new LevelSpec
            {
                name = "Snowy Park", file = "Snow", trackMaterial = "TrackSnow", surroundMaterial = "SurroundSnow",
                sky = new Color(0.78f, 0.88f, 0.98f), ambient = new Color(0.62f, 0.68f, 0.78f), sun = new Color(1f, 0.96f, 0.88f),
                width = 14f,
                sections = new[]
                {
                    new TrackLayout.Section(90, 16), new TrackLayout.Section(70, 8), new TrackLayout.Section(45, 1.5f),
                    new TrackLayout.Section(110, 14), new TrackLayout.Section(70, 5), new TrackLayout.Section(45, 0.5f),
                    new TrackLayout.Section(130, 16), new TrackLayout.Section(90, 9), new TrackLayout.Section(100, 13),
                },
                crash = new[] { "Obstacle_StoneBall", "Obstacle_Bomb", "Obstacle_Obelisk", "Obstacle_Easel" },
                slow = new[] { "Obstacle_RoundBush", "Obstacle_SquareBush" },
                scenery = new[] { "Scenery_Bush", "Scenery_Crystal", "Scenery_Obelisk" },
                ambientEffect = FxQuest + "/Props/3D_Snow_01_Loop.prefab",
            },
            new LevelSpec
            {
                name = "Sunny Park", file = "Park", trackMaterial = "ParkPath", surroundMaterial = "ParkGrass",
                sky = new Color(0.62f, 0.82f, 0.98f), ambient = new Color(0.58f, 0.68f, 0.6f), sun = new Color(1f, 0.97f, 0.85f),
                width = 16f,
                sections = new[]
                {
                    new TrackLayout.Section(80, 13), new TrackLayout.Section(80, 9), new TrackLayout.Section(40, 3),
                    new TrackLayout.Section(100, 12), new TrackLayout.Section(60, 6), new TrackLayout.Section(40, 2),
                    new TrackLayout.Section(120, 13), new TrackLayout.Section(100, 8), new TrackLayout.Section(100, 12),
                },
                crash = new[] { "Obstacle_BalloonCart", "Obstacle_StoneBall", "Obstacle_Bomb" },
                slow = new[] { "Obstacle_BushFence", "Obstacle_RoundBush", "Obstacle_SquareBush" },
                scenery = new[] { "Scenery_Bush" },
            },
            new LevelSpec
            {
                name = "Louvre", file = "Louvre", trackMaterial = "LouvreMarble", surroundMaterial = "LouvreFloor",
                sky = new Color(0.95f, 0.82f, 0.62f), ambient = new Color(0.72f, 0.64f, 0.55f), sun = new Color(1f, 0.88f, 0.7f),
                width = 12f,
                sections = new[]
                {
                    new TrackLayout.Section(70, 18), new TrackLayout.Section(60, 10), new TrackLayout.Section(50, 1),
                    new TrackLayout.Section(100, 15), new TrackLayout.Section(60, 6), new TrackLayout.Section(50, 0.5f),
                    new TrackLayout.Section(120, 17), new TrackLayout.Section(80, 10), new TrackLayout.Section(110, 14),
                },
                crash = new[] { "Obstacle_Sarcophagus", "Obstacle_Tomb", "Obstacle_Sphinx", "Obstacle_LouvreCart" },
                slow = new[] { "Obstacle_Fence", "Obstacle_Banner", "Obstacle_CatStatue" },
                scenery = new[] { "Scenery_Sphinx", "Scenery_Tomb" },
            },
        };

        // ---------------------------------------------------------------------
        // Entry points
        // ---------------------------------------------------------------------

        [MenuItem("Tools/Sled Run/Build Prototype Scene")]
        static void BuildAll()
        {
            foreach (string folder in new[] { "Data", "Data/Characters", "Data/Levels", "Materials", "Materials/Repaired", "Prefabs", "Prefabs/Characters", "Animation" })
                EnsureFolder($"{ProjectRoot}/{folder}");

            // Phase 1: everything that lives on disk.
            EvaluateCharacters();
            LoadOrCreate<SledTuning>(TuningPath);
            CreateUpgrades();
            CreateMaterials();
            CreateProps();
            CreateCharacterPrefabs();
            BuildAnimator();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            CreateDefinitions();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Phase 2: scene, wired from freshly loaded assets.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BuildScene();

            EnsureFolder("Assets/Scenes");
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            ReportCharacters();
            Debug.Log($"Sled Run prototype built ({Characters.Length} characters, {Levels.Length} levels). Open {ScenePath} and press Play.");
        }

        [MenuItem("Tools/Sled Run/Report Characters")]
        static void ReportCharacters()
        {
            var lines = new List<string> { "Sled Run character report:" };
            foreach (var character in Characters)
            {
                var definition = AssetDatabase.LoadAssetAtPath<CharacterDefinition>(CharacterAssetPath(character.name));
                if (definition == null) { lines.Add($"  {character.name}: NOT selectable"); continue; }

                var so = new SerializedObject(definition);
                var prefab = definition.ModelPrefab;
                var materials = prefab == null
                    ? "no prefab"
                    : string.Join("; ", prefab.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials).Distinct().Select(m =>
                        m == null ? "null material" : $"{m.name} [{(MaterialRepair.IsBroken(m) ? "SHADER MISSING" : m.shader.name)}] tex={(m.HasProperty("_MainTex") && m.GetTexture("_MainTex") != null ? m.GetTexture("_MainTex").name : "NONE")}"));
                lines.Add($"  {character.name}: selectable, voice={definition.Voice}, lose={definition.CrashClips.Length}, win={definition.VictoryClips.Length}, launch={definition.LaunchClips.Length} | {materials}");
            }
            Debug.Log(string.Join("\n", lines));
        }

        [MenuItem("Tools/Sled Run/Reset Saved Progress")]
        static void ResetProgress()
        {
            PlayerPrefs.DeleteKey(PlayerPrefsProfileStore.Key);
            PlayerPrefs.Save();
            Debug.Log("Sled Run: saved progress cleared.");
        }

        // =====================================================================
        // Phase 1a: upgrades & materials
        // =====================================================================

        static void CreateUpgrades()
        {
            var specs = new[]
            {
                (file: "Upgrade_LaunchPower", type: UpgradeType.LaunchPower, name: "Launch Power", desc: "Stronger slingshot launch.", cost: 25, growth: 1.7f, bonus: 0.10f, max: 5),
                (file: "Upgrade_Speed", type: UpgradeType.Speed, name: "Slope Speed", desc: "Less friction and drag, higher top speed.", cost: 30, growth: 1.7f, bonus: 0.12f, max: 5),
                (file: "Upgrade_Steering", type: UpgradeType.Steering, name: "Steering", desc: "Faster sideways response.", cost: 20, growth: 1.6f, bonus: 0.15f, max: 5),
                (file: "Upgrade_CroissantValue", type: UpgradeType.CollectibleValue, name: "Croissant Value", desc: "Each croissant is worth more.", cost: 40, growth: 1.8f, bonus: 0.5f, max: 4),
            };

            foreach (var spec in specs)
            {
                string path = $"{ProjectRoot}/Data/{spec.file}.asset";
                bool existed = AssetDatabase.LoadAssetAtPath<UpgradeDefinition>(path) != null;
                var definition = LoadOrCreate<UpgradeDefinition>(path);
                if (existed) continue;

                var so = new SerializedObject(definition);
                so.FindProperty("type").enumValueIndex = (int)spec.type;
                so.FindProperty("displayName").stringValue = spec.name;
                so.FindProperty("description").stringValue = spec.desc;
                so.FindProperty("maxLevel").intValue = spec.max;
                so.FindProperty("baseCost").intValue = spec.cost;
                so.FindProperty("costGrowth").floatValue = spec.growth;
                so.FindProperty("bonusPerLevel").floatValue = spec.bonus;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        static void CreateMaterials()
        {
            Mat("TrackSnow", "Standard", new Color(0.80f, 0.90f, 1f), 0.45f);
            Mat("SurroundSnow", "Standard", new Color(0.95f, 0.97f, 1f), 0.15f);
            Mat("ParkPath", "Standard", new Color(0.80f, 0.72f, 0.55f), 0.1f);
            Mat("ParkGrass", "Standard", new Color(0.40f, 0.66f, 0.30f), 0.05f);
            Mat("Finish", "Standard", new Color(0.90f, 0.12f, 0.20f), 0.3f);
            Mat("SlingshotWood", "Standard", new Color(0.45f, 0.28f, 0.16f), 0.1f);
            Mat("SlingshotBand", "Sprites/Default", new Color(0.9f, 0.1f, 0.15f), 0f);

            var marble = Mat("LouvreMarble", "Standard", Color.white, 0.5f);
            AssignTexture(marble, "Assets/Ladybug/Content/Levels/Louvre/Marble01.png");
            var floor = Mat("LouvreFloor", "Standard", new Color(0.85f, 0.75f, 0.65f), 0.2f);
            AssignTexture(floor, "Assets/Ladybug/Content/Levels/Louvre/WoodFloor.png");
        }

        static Material Mat(string name, string shaderName, Color color, float smoothness)
        {
            string path = $"{ProjectRoot}/Materials/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;

            material = new Material(Shader.Find(shaderName)) { color = color };
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        static void AssignTexture(Material material, string texturePath)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null) { Debug.LogWarning($"Sled Run builder: texture not found at {texturePath}."); return; }
            material.SetTexture("_MainTex", texture);
            EditorUtility.SetDirty(material);
        }

        // =====================================================================
        // Phase 1b: prop prefabs (pack models)
        // =====================================================================

        static void CreateProps()
        {
            string villains = $"{Geometry}/Villans_obstacles";
            string villainMats = $"{villains}/Materials";
            string park = $"{Geometry}/Levels/Park/Park_obstacles";
            string collectibles = $"{Geometry}/Levels/AllLevels/Collectables";
            string louvre = "Assets/Ladybug/Content/Levels/Louvre";
            string projectiles = $"{villainMats}/Static_Projectiles.mat";
            string bucketMat = $"{villainMats}/BucketObeliskChevalet.mat";
            string bushMat = $"{park}/Materials/ParkBushObstacle.mat";

            MakeProp("Croissant", $"{collectibles}/Meshes/Croissant.fbx", $"{collectibles}/Materials/CroissantMaterial.mat", 0.9f, null, 1.5f, true,
                go => go.AddComponent<Collectible>());

            // Snow / shared
            ObstacleProp("Obstacle_StoneBall", $"{villains}/Stone_Ball.FBX", projectiles, 2.2f, null, ObstacleKind.Crash, ObstacleSurface.Stone);
            ObstacleProp("Obstacle_Bomb", $"{villains}/Bomb.FBX", projectiles, 1.8f, null, ObstacleKind.Crash, ObstacleSurface.Metal);
            ObstacleProp("Obstacle_Obelisk", $"{villains}/V_obs_obelisque.fbx", bucketMat, 3.2f, null, ObstacleKind.Crash, ObstacleSurface.Stone);
            ObstacleProp("Obstacle_Easel", $"{villains}/V_obs_chevalet.fbx", bucketMat, 2.6f, null, ObstacleKind.Crash, ObstacleSurface.Wood);
            ObstacleProp("Obstacle_RoundBush", $"{park}/Park_BushObstacles.fbx", bushMat, 2.2f, "RoundBush", ObstacleKind.Slow, ObstacleSurface.Leaves);
            ObstacleProp("Obstacle_SquareBush", $"{park}/Park_BushObstacles.fbx", bushMat, 2.0f, "SquareBush", ObstacleKind.Slow, ObstacleSurface.Leaves);
            SceneryProp("Scenery_Bush", $"{park}/Park_BushObstacles.fbx", bushMat, 3.5f, "RoundBush");
            SceneryProp("Scenery_Crystal", $"{villains}/Blue_Crystal.FBX", projectiles, 4.5f, null);
            SceneryProp("Scenery_Obelisk", $"{villains}/V_obs_obelisque.fbx", bucketMat, 6f, null);

            // Park (materials come from the models themselves, repaired where the shader is missing)
            ObstacleProp("Obstacle_BalloonCart", $"{park}/Baloon_Cart.FBX", null, 3f, null, ObstacleKind.Crash, ObstacleSurface.Wood);
            ObstacleProp("Obstacle_BushFence", $"{park}/Park_VineObstacle.fbx", null, 3.2f, null, ObstacleKind.Slow, ObstacleSurface.Leaves);

            // Louvre
            ObstacleProp("Obstacle_Sarcophagus", $"{louvre}/Obstacles_Sarcophagus.fbx", null, 3f, null, ObstacleKind.Crash, ObstacleSurface.Stone);
            ObstacleProp("Obstacle_Tomb", $"{louvre}/Obstacles_Tomb.fbx", null, 3f, null, ObstacleKind.Crash, ObstacleSurface.Stone);
            ObstacleProp("Obstacle_Sphinx", $"{louvre}/Louvre_Sphinx.fbx", null, 3.5f, null, ObstacleKind.Crash, ObstacleSurface.Stone);
            ObstacleProp("Obstacle_LouvreCart", $"{louvre}/Louvre_Cart.fbx", null, 3f, null, ObstacleKind.Crash, ObstacleSurface.Wood);
            ObstacleProp("Obstacle_Fence", $"{louvre}/Obstacles_Fence.fbx", null, 3f, null, ObstacleKind.Slow, ObstacleSurface.Metal);
            ObstacleProp("Obstacle_Banner", $"{louvre}/Obstacles_Banner.fbx", null, 2.6f, null, ObstacleKind.Slow, ObstacleSurface.Leaves);
            ObstacleProp("Obstacle_CatStatue", $"{louvre}/Obstacles_Cat.fbx", null, 2.2f, null, ObstacleKind.Slow, ObstacleSurface.Stone);
            SceneryProp("Scenery_Sphinx", $"{louvre}/Louvre_Sphinx.fbx", null, 6f, null);
            SceneryProp("Scenery_Tomb", $"{louvre}/Obstacles_Tomb.fbx", null, 4.5f, null);
        }

        static void ObstacleProp(string name, string fbx, string material, float size, string meshFilter, ObstacleKind kind, ObstacleSurface surface) =>
            MakeProp(name, fbx, material, size, meshFilter, 0.85f, true, go =>
            {
                var obstacle = go.AddComponent<Obstacle>();
                var so = new SerializedObject(obstacle);
                so.FindProperty("kind").enumValueIndex = (int)kind;
                so.FindProperty("surface").enumValueIndex = (int)surface;
                so.ApplyModifiedPropertiesWithoutUndo();
            });

        static void SceneryProp(string name, string fbx, string material, float size, string meshFilter) =>
            MakeProp(name, fbx, material, size, meshFilter, 1f, false, null);

        /// <summary>
        /// Instantiates a pack FBX, keeps only meshes whose name contains <paramref name="meshFilter"/>,
        /// applies a working material (the given one, or the model's own repaired), normalises size so the
        /// bottom sits at y=0, adds a trigger hitbox if asked, and saves a prefab.
        /// </summary>
        static void MakeProp(string name, string fbxPath, string materialPath, float size, string meshFilter,
            float hitboxPadding, bool hasHitbox, Action<GameObject> configure)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (model == null)
            {
                Debug.LogWarning($"Sled Run builder: model not found at {fbxPath}; skipping {name}.");
                return;
            }

            var root = new GameObject(name);
            var instance = (GameObject)Object.Instantiate(model, root.transform);
            instance.name = "Model";

            if (!string.IsNullOrEmpty(meshFilter))
            {
                foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
                    if (!renderer.name.Contains(meshFilter)) Object.DestroyImmediate(renderer.gameObject);
            }

            if (materialPath != null) ApplySingleMaterial(instance, materialPath, name);
            else RepairMaterials(instance);

            Bounds bounds = CalculateBounds(root);
            if (bounds.size.sqrMagnitude > 0f)
            {
                instance.transform.localScale *= size / Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                bounds = CalculateBounds(root);
                instance.transform.position += new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
                bounds = CalculateBounds(root);
            }

            if (hasHitbox)
            {
                var box = root.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.center = bounds.center;
                box.size = bounds.size * hitboxPadding;
            }

            configure?.Invoke(root);

            PrefabUtility.SaveAsPrefabAsset(root, $"{ProjectRoot}/Prefabs/{name}.prefab");
            Object.DestroyImmediate(root);
        }

        static void ApplySingleMaterial(GameObject instance, string materialPath, string label)
        {
            var material = MaterialRepair.Repair(AssetDatabase.LoadAssetAtPath<Material>(materialPath), RepairedFolder);
            if (material == null) Debug.LogWarning($"Sled Run builder: no usable material at {materialPath} for {label}.");
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
                renderer.sharedMaterials = Enumerable.Repeat(material, Mathf.Max(1, renderer.sharedMaterials.Length)).ToArray();
        }

        /// <summary>Keeps each slot's own material, replacing any whose shader is missing.</summary>
        static void RepairMaterials(GameObject instance, Material bodyMaterial = null)
        {
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
            {
                var slots = renderer.sharedMaterials;
                for (int i = 0; i < slots.Length; i++)
                {
                    // A slot that imported without a texture takes the hero's own body material (glasses keep theirs).
                    bool glasses = slots[i] != null && slots[i].name.IndexOf("glass", StringComparison.OrdinalIgnoreCase) >= 0;
                    if (bodyMaterial != null && !glasses && !MaterialRepair.HasTexture(slots[i])) slots[i] = bodyMaterial;
                    slots[i] = MaterialRepair.Repair(slots[i], RepairedFolder);
                }
                renderer.sharedMaterials = slots;
            }
        }

        static Bounds CalculateBounds(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(root.transform.position, Vector3.zero);
            Bounds bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            return bounds;
        }

        // =====================================================================
        // Phase 1c: characters
        // =====================================================================

        static void CreateCharacterPrefabs()
        {
            foreach (var (name, fbx, folder, _, _) in Characters)
            {
                if (!SelectablePools.ContainsKey(name)) continue;

                var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
                if (model == null)
                {
                    Debug.LogWarning($"Sled Run builder: character model not found at {fbx}; skipping {name}.");
                    continue;
                }

                var root = new GameObject(PrefabName(name));
                var instance = (GameObject)Object.Instantiate(model, root.transform);
                instance.name = "Model";

                if (fbx == LadybugModelPath) ApplyLadybugMaterials(instance);
                else RepairMaterials(instance, FindBodyMaterial(folder));

                // Normalise to ~1.75 m with the feet at the sled origin.
                Bounds bounds = CalculateBounds(instance);
                if (bounds.size.y > 0.01f)
                {
                    instance.transform.localScale *= 1.75f / bounds.size.y;
                    bounds = CalculateBounds(instance);
                    instance.transform.position += new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
                }

                var animator = instance.GetComponent<Animator>();
                if (animator == null) animator = instance.AddComponent<Animator>();
                if (animator.avatar == null)
                    animator.avatar = AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<Avatar>().FirstOrDefault();
                animator.applyRootMotion = false;

                PrefabUtility.SaveAsPrefabAsset(root, $"{ProjectRoot}/Prefabs/Characters/{PrefabName(name)}.prefab");
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>Own win/lose animation pools for every hero that qualifies as selectable; filled by <see cref="EvaluateCharacters"/>.</summary>
        static readonly Dictionary<string, ClipPools> SelectablePools = new Dictionary<string, ClipPools>();

        /// <summary>
        /// A hero is selectable only if it has a texture and its own lose and win animations (no borrowing
        /// Ladybug's). Voices are not a filter: every hero maps to the Ladybug or Adrien voice by gender.
        /// </summary>
        static void EvaluateCharacters()
        {
            SelectablePools.Clear();
            foreach (var character in Characters)
            {
                // After unused pack assets were removed, a hero's source model may be gone while its finished
                // prefab and definition remain. Leave those untouched rather than treating them as excluded.
                if (AssetDatabase.LoadAssetAtPath<GameObject>(character.fbx) == null)
                {
                    Debug.Log($"Sled Run builder: source model for '{character.name}' not in the project; keeping its existing assets.");
                    continue;
                }

                var pools = new ClipPools($"{CharacterRoot}/{character.folder}");
                var missing = new List<string>();
                if (!HasTexture(character.fbx, character.folder, out string materialReport)) missing.Add($"texture [{materialReport}]");
                if (pools.Crash.Length == 0) missing.Add("lose animation");
                if (pools.Victory.Length == 0) missing.Add("win animation");

                if (missing.Count == 0)
                {
                    SelectablePools[character.name] = pools;
                    continue;
                }

                Debug.LogWarning($"Sled Run builder: '{character.name}' excluded - no own {string.Join(" / ", missing)}.");
                AssetDatabase.DeleteAsset(CharacterAssetPath(character.name));
                AssetDatabase.DeleteAsset($"{ProjectRoot}/Prefabs/Characters/{PrefabName(character.name)}.prefab");
            }
        }

        /// <summary>True if at least one of the model's materials has a usable main texture. Ladybug's are rebuilt from the UI textures.</summary>
        static bool HasTexture(string fbxPath, string folder, out string report)
        {
            report = "n/a";
            if (fbxPath == LadybugModelPath) return true;

            var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (model == null) { report = "model not found"; return false; }

            var materials = model.GetComponentsInChildren<Renderer>(true).SelectMany(r => r.sharedMaterials).Distinct().ToArray();
            report = materials.Length == 0
                ? "none"
                : string.Join(", ", materials.Select(m => m == null ? "null" : $"{m.name} ({(MaterialRepair.IsBroken(m) ? "shader missing, " : "")}{(MaterialRepair.HasTexture(m) ? "textured" : "no texture")})"));
            if (materials.Any(MaterialRepair.HasTexture)) return true;

            // Many models import with generic default materials; the real textured material sits in the hero's folder.
            var body = FindBodyMaterial(folder);
            if (body != null) { report += $" -> using folder material {body.name}"; return true; }
            report += " (and no textured material in the hero's folder)";
            return false;
        }

        static readonly Regex NotBodyMaterial = new Regex(@"glass|mask|transp|test|UI|Ui", RegexOptions.None);

        /// <summary>The hero's own textured material (skipping UI variants, glasses and masks), or null.</summary>
        static Material FindBodyMaterial(string folder)
        {
            string path = $"{CharacterRoot}/{folder}";
            if (!AssetDatabase.IsValidFolder(path)) return null;

            return AssetDatabase.FindAssets("t:Material", new[] { path })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => !NotBodyMaterial.IsMatch(Path.GetFileNameWithoutExtension(p)))
                .Select(AssetDatabase.LoadAssetAtPath<Material>)
                .FirstOrDefault(MaterialRepair.HasTexture);
        }

        static string PrefabName(string displayName) => "Char_" + displayName.Replace(" ", "");

        /// <summary>
        /// The pack's own Ladybug materials use shaders that are not included, so rebuild them from the
        /// model's textures: a Standard body and a cutout material for the eyelashes.
        /// </summary>
        static void ApplyLadybugMaterials(GameObject instance)
        {
            var body = Mat("LadybugBody", "Standard", Color.white, 0.4f);
            body.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>($"{LadybugTextures}/Ladybug_UI_Diffuse.png"));
            string normalPath = $"{LadybugTextures}/Ladybug_UI_Normal.png";
            var importer = AssetImporter.GetAtPath(normalPath) as TextureImporter;
            if (importer != null && importer.textureType != TextureImporterType.NormalMap)
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.SaveAndReimport();
            }
            var normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            if (normal != null)
            {
                body.SetTexture("_BumpMap", normal);
                body.EnableKeyword("_NORMALMAP");
            }
            EditorUtility.SetDirty(body);

            var lashes = Mat("LadybugLashes", "Legacy Shaders/Transparent/Cutout/Diffuse", Color.white, 0f);
            lashes.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>($"{LadybugTextures}/Ladybug_UI_eyelashes.png"));
            EditorUtility.SetDirty(lashes);

            foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
            {
                var slots = renderer.sharedMaterials;
                for (int i = 0; i < slots.Length; i++)
                    slots[i] = slots[i] != null && slots[i].name.Contains("Default") ? lashes : body;
                renderer.sharedMaterials = slots;
            }
        }

        // =====================================================================
        // Phase 1d: animator (shared by every character; humanoid clips retarget)
        // =====================================================================

        const string PlaceholderFolder = CharacterRoot + "/Ladybug";
        const string IdleClipPath = PlaceholderFolder + "/LadyBug@Ilde_Breathing_FacingForward.FBX";
        const string LaunchClipPath = PlaceholderFolder + "/LadyBug@Jump.FBX";
        const string RideClipPath = PlaceholderFolder + "/LadyBug@Slide.FBX";
        const string CrashClipPath = PlaceholderFolder + "/LadyBug@Death2.FBX";
        const string VictoryClipPath = PlaceholderFolder + "/LadyBug@Victory.FBX";

        /// <summary>
        /// Shared controller. The launch, crash and victory clips are placeholders: at runtime an override
        /// controller replaces them with a random clip from the selected character's own pool.
        /// </summary>
        static void BuildAnimator()
        {
            AssetDatabase.DeleteAsset(AnimatorPath);

            var controller = AnimatorController.CreateAnimatorControllerAtPath(AnimatorPath);
            foreach (string parameter in new[] { "Idle", "Launch", "Ride", "Crash", "Victory" })
                controller.AddParameter(parameter, AnimatorControllerParameterType.Trigger);

            var machine = controller.layers[0].stateMachine;
            AnimatorState idle = AddState(machine, "Idle", IdleClipPath);
            AnimatorState launch = AddState(machine, "Launch", LaunchClipPath);
            AnimatorState ride = AddState(machine, "Ride", RideClipPath);
            AddState(machine, "Crash", CrashClipPath);
            AddState(machine, "Victory", VictoryClipPath);
            machine.defaultState = idle;

            // The launch animation plays once, then the sled settles into the ride pose.
            var toRide = launch.AddTransition(ride);
            toRide.hasExitTime = true;
            toRide.exitTime = 0.9f;
            toRide.duration = 0.15f;
        }

        static AnimatorState AddState(AnimatorStateMachine machine, string name, string clipSource)
        {
            var state = machine.AddState(name);
            state.motion = LoadClip(clipSource);

            var transition = machine.AddAnyStateTransition(state);
            transition.AddCondition(AnimatorConditionMode.If, 0f, name);
            transition.hasExitTime = false;
            transition.duration = 0.12f;
            transition.canTransitionToSelf = false;
            return state;
        }

        static AnimationClip LoadClip(string fbxPath)
        {
            var clip = AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
            if (clip == null) Debug.LogWarning($"Sled Run builder: no animation clip in {fbxPath}.");
            return clip;
        }

        // =====================================================================
        // Phase 1e: character & level definitions
        // =====================================================================

        static string CharacterAssetPath(string name) => $"{ProjectRoot}/Data/Characters/{PrefabName(name)}.asset";
        static string LevelAssetPath(LevelSpec spec) => $"{ProjectRoot}/Data/Levels/Level_{spec.file}.asset";
        static string LayoutAssetPath(LevelSpec spec) => $"{ProjectRoot}/Data/Levels/Layout_{spec.file}.asset";

        static void CreateDefinitions()
        {
            AssetDatabase.DeleteAsset($"{ProjectRoot}/Data/TrackLayout.asset"); // superseded by per-level layouts

            var fallback = new ClipPools(Path.Combine(CharacterRoot, "Ladybug").Replace('\\', '/'));
            foreach (var character in Characters)
            {
                if (!SelectablePools.TryGetValue(character.name, out var pools)) continue;
                var prefab = LoadPrefab($"Characters/{PrefabName(character.name)}");
                if (prefab == null) continue;

                var definition = LoadOrCreate<CharacterDefinition>(CharacterAssetPath(character.name));
                var so = new SerializedObject(definition);
                so.FindProperty("displayName").stringValue = character.name;
                so.FindProperty("modelPrefab").objectReferenceValue = prefab;
                so.FindProperty("voice").enumValueIndex = (int)character.voice;
                so.FindProperty("selectSound").objectReferenceValue =
                    LoadClipAsset($"{Sounds}/Character/Transformations/TransformationSounds/{character.sound}.wav");
                AssignClips(so.FindProperty("launchClips"), pools.Launch.Length > 0 ? pools.Launch : fallback.Launch);
                AssignClips(so.FindProperty("crashClips"), pools.Crash);
                AssignClips(so.FindProperty("victoryClips"), pools.Victory);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(definition);
            }

            foreach (var spec in Levels)
            {
                bool existed = AssetDatabase.LoadAssetAtPath<LevelDefinition>(LevelAssetPath(spec)) != null;

                var layout = LoadOrCreate<TrackLayout>(LayoutAssetPath(spec));
                if (!existed)
                {
                    layout.width = spec.width;
                    layout.sections = spec.sections;
                    EditorUtility.SetDirty(layout);
                }

                var level = LoadOrCreate<LevelDefinition>(LevelAssetPath(spec));
                var so = new SerializedObject(level);
                if (!existed)
                {
                    so.FindProperty("displayName").stringValue = spec.name;
                    so.FindProperty("skyColor").colorValue = spec.sky;
                    so.FindProperty("ambientColor").colorValue = spec.ambient;
                    so.FindProperty("sunColor").colorValue = spec.sun;
                }

                // Asset references are refreshed every run (prefab GUIDs are stable, so this is idempotent).
                Find(so, "layout").objectReferenceValue = layout;
                Find(so, "trackMaterial").objectReferenceValue = LoadMaterial(spec.trackMaterial);
                Find(so, "surroundMaterial").objectReferenceValue = LoadMaterial(spec.surroundMaterial);
                AssignPrefabs(Find(so, "crashObstacles"), spec.crash);
                AssignPrefabs(Find(so, "slowObstacles"), spec.slow);
                AssignPrefabs(Find(so, "scenery"), spec.scenery);
                Find(so, "ambientEffect").objectReferenceValue = spec.ambientEffect != null ? LoadEffect(spec.ambientEffect) : null;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(level);
            }
        }

        /// <summary>The launch / crash / victory clips found in one hero's animation folder.</summary>
        sealed class ClipPools
        {
            public readonly AnimationClip[] Launch, Crash, Victory;

            public ClipPools(string folder)
            {
                Launch = Find(folder, LaunchClipName, LaunchClipExclude, 2.0f);
                Crash = Find(folder, CrashClipName, null, 12f); // some heroes' death takes are 7-9 s long
                Victory = Find(folder, VictoryClipName, VictoryClipExclude, 8f);
            }

            static AnimationClip[] Find(string folder, Regex include, Regex exclude, float maxLength)
            {
                if (!AssetDatabase.IsValidFolder(folder)) return new AnimationClip[0];

                var clips = new List<AnimationClip>();
                foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { folder }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    string name = Path.GetFileNameWithoutExtension(path);
                    if (!include.IsMatch(name) || (exclude != null && exclude.IsMatch(name))) continue;

                    var clip = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                        .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
                    if (clip != null && clip.length <= maxLength) clips.Add(clip);
                }
                return clips.ToArray();
            }
        }

        static void AssignClips(SerializedProperty array, AnimationClip[] clips)
        {
            array.arraySize = clips.Length;
            for (int i = 0; i < clips.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = clips[i];
        }

        static void AssignPrefabs(SerializedProperty array, string[] names)
        {
            var prefabs = names.Select(n => LoadPrefab(n)).Where(p => p != null).ToArray();
            array.arraySize = prefabs.Length;
            for (int i = 0; i < prefabs.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = prefabs[i];
        }

        static GameObject LoadPrefab(string name) => AssetDatabase.LoadAssetAtPath<GameObject>($"{ProjectRoot}/Prefabs/{name}.prefab");
        static Material LoadMaterial(string name) => AssetDatabase.LoadAssetAtPath<Material>($"{ProjectRoot}/Materials/{name}.mat");

        // =====================================================================
        // Phase 2: scene
        // =====================================================================

        static void BuildScene()
        {
            var tuning = AssetDatabase.LoadAssetAtPath<SledTuning>(TuningPath);
            var upgrades = new[] { "LaunchPower", "Speed", "Steering", "CroissantValue" }
                .Select(n => AssetDatabase.LoadAssetAtPath<UpgradeDefinition>($"{ProjectRoot}/Data/Upgrade_{n}.asset")).ToArray();
            var characters = Characters.Select(c => AssetDatabase.LoadAssetAtPath<CharacterDefinition>(CharacterAssetPath(c.name))).Where(c => c != null).ToArray();
            var levels = Levels.Select(l => AssetDatabase.LoadAssetAtPath<LevelDefinition>(LevelAssetPath(l))).Where(l => l != null).ToArray();
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimatorPath);

            Light sun = ConfigureEnvironment();

            // --- player ---
            var player = new GameObject("Player");
            var lean = new GameObject("Lean").transform;
            lean.SetParent(player.transform, false);

            player.AddComponent<Rigidbody>();
            var sensorBox = player.AddComponent<BoxCollider>();
            sensorBox.center = new Vector3(0f, 0.7f, 0f);
            sensorBox.size = new Vector3(1.1f, 1.4f, 1.8f);
            var steering = player.AddComponent<SteeringInput>();
            var sled = player.AddComponent<SledMotor>();
            var sensor = player.AddComponent<PlayerSensor>();
            Set(sled, "steering", steering);

            // --- slingshot ---
            var wood = LoadMaterial("SlingshotWood");
            var slingshotRoot = new GameObject("Slingshot");
            var slingshot = slingshotRoot.AddComponent<SlingshotLauncher>();
            Transform leftTip = BuildSlingshotPost(slingshotRoot.transform, "Post L", -1.7f, wood);
            Transform rightTip = BuildSlingshotPost(slingshotRoot.transform, "Post R", 1.7f, wood);
            var band = new GameObject("Band").AddComponent<LineRenderer>();
            band.transform.SetParent(slingshotRoot.transform, false);
            band.sharedMaterial = LoadMaterial("SlingshotBand");
            band.widthMultiplier = 0.12f;
            band.useWorldSpace = true;
            band.numCapVertices = 4;
            Set(slingshot, "sled", sled);
            Set(slingshot, "leftTip", leftTip);
            Set(slingshot, "rightTip", rightTip);
            Set(slingshot, "band", band);

            // --- track ---
            var trackRoot = new GameObject("Track");
            var track = trackRoot.AddComponent<TrackBuilder>();
            Set(track, "finishMaterial", LoadMaterial("Finish"));
            Set(track, "collectiblePrefab", LoadPrefab("Croissant"));

            // --- camera & atmosphere ---
            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.farClipPlane = 400f;
            cameraObject.AddComponent<AudioListener>();
            var follow = cameraObject.AddComponent<FollowCamera>();
            Set(follow, "target", player.transform);
            Set(follow, "sled", sled);

            var atmosphere = new GameObject("Atmosphere").AddComponent<LevelAtmosphere>();
            Set(atmosphere, "sceneCamera", camera);
            Set(atmosphere, "sun", sun);
            Set(atmosphere, "followTarget", player.transform);

            // --- game flow & audio ---
            var systems = new GameObject("Game");
            var flow = systems.AddComponent<GameFlow>();
            Set(flow, "tuning", tuning);
            SetArray(flow, "upgrades", upgrades);
            SetArray(flow, "characters", characters);
            SetArray(flow, "levels", levels);
            Set(flow, "atmosphere", atmosphere);
            Set(flow, "sled", sled);
            Set(flow, "sensor", sensor);
            Set(flow, "slingshot", slingshot);
            Set(flow, "track", track);
            Set(flow, "followCamera", follow);

            var audio = BuildAudio(systems, flow, sled);
            BuildParticles(systems, flow, sled, player.transform);

            var shake = systems.AddComponent<CameraShakeFeedback>();
            Set(shake, "flow", flow);
            Set(shake, "followCamera", follow);

            var visuals = player.AddComponent<PlayerVisuals>();
            Set(visuals, "flow", flow);
            Set(visuals, "sled", sled);
            Set(visuals, "controller", controller);
            Set(visuals, "launchPlaceholder", LoadClip(LaunchClipPath));
            Set(visuals, "crashPlaceholder", LoadClip(CrashClipPath));
            Set(visuals, "victoryPlaceholder", LoadClip(VictoryClipPath));
            Set(visuals, "leanRoot", lean);

            BuildUi(flow, audio, upgrades);

            Validate(flow, "tuning", "upgrades", "characters", "levels", "atmosphere", "sled", "sensor", "slingshot", "track", "followCamera");
            Validate(track, "finishMaterial", "collectiblePrefab");
            Validate(visuals, "flow", "sled", "controller", "leanRoot", "launchPlaceholder", "crashPlaceholder", "victoryPlaceholder");
            Validate(shake, "flow", "followCamera");
            Validate(systems.GetComponent<ParticleFeedback>(), "flow", "sled", "pickupEffect", "softHitEffect", "crashEffect", "fireworkEffect", "confettiEffect", "slideTrail");
            foreach (var level in levels)
                Validate(level, "layout", "trackMaterial", "surroundMaterial", "crashObstacles", "slowObstacles", "scenery");
            foreach (var character in characters) Validate(character, "modelPrefab", "selectSound", "launchClips", "crashClips", "victoryClips");
        }

        static Light ConfigureEnvironment()
        {
            var lightObject = new GameObject("Sun");
            lightObject.transform.rotation = Quaternion.Euler(48f, -28f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.15f;
            light.color = new Color(1f, 0.96f, 0.88f);
            light.shadows = LightShadows.Soft;
            RenderSettings.skybox = null;
            return light;
        }

        static Transform BuildSlingshotPost(Transform parent, string name, float x, Material wood)
        {
            var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = name;
            Object.DestroyImmediate(post.GetComponent<Collider>());
            post.transform.SetParent(parent, false);
            post.transform.localPosition = new Vector3(x, 1.1f, 0f);
            post.transform.localScale = new Vector3(0.28f, 1.1f, 0.28f);
            post.GetComponent<Renderer>().sharedMaterial = wood;

            var tip = new GameObject(name + " Tip").transform;
            tip.SetParent(parent, false);
            tip.localPosition = new Vector3(x, 2.2f, 0f);
            return tip;
        }

        static void BuildParticles(GameObject host, GameFlow flow, SledMotor sled, Transform player)
        {
            var feedback = host.AddComponent<ParticleFeedback>();
            Set(feedback, "flow", flow);
            Set(feedback, "sled", sled);
            Set(feedback, "pickupEffect", LoadEffect($"{FxQuest}/Misc/3D_PickItem_03.prefab"));
            Set(feedback, "softHitEffect", LoadEffect($"{FxQuest}/Fight/3D_Hit_03.prefab"));
            Set(feedback, "crashEffect", LoadEffect($"{FxQuest}/Explosions/3D_GroundExplode_01.prefab"));
            Set(feedback, "fireworkEffect", LoadEffect($"{FxQuest}/Props/3D_Firework_01.prefab"));
            Set(feedback, "confettiEffect", LoadEffect($"{PackFx}/FallingConfeties.prefab"));

            // The dust trail lives on the player so it follows the sled; the feedback component drives it.
            var dust = LoadEffect($"{PackFx}/Slide Dust.prefab");
            if (dust != null)
            {
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(dust, player);
                instance.name = "Slide Dust";
                instance.transform.localPosition = new Vector3(0f, 0.15f, -0.6f);
                Set(feedback, "slideTrail", instance.GetComponent<ParticleSystem>());
            }
        }

        static GameObject LoadEffect(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) Debug.LogWarning($"Sled Run builder: effect not found at {path}.");
            return prefab;
        }

        static AudioFeedback BuildAudio(GameObject host, GameFlow flow, SledMotor sled)
        {
            var effects = host.AddComponent<AudioSource>();
            effects.playOnAwake = false;

            var loopObject = new GameObject("Slide Loop");
            loopObject.transform.SetParent(host.transform, false);
            var loop = loopObject.AddComponent<AudioSource>();
            loop.playOnAwake = false;
            loop.loop = true;
            loop.volume = 0f;
            loop.clip = LoadClipAsset($"{Sounds}/Effects/CartoonSlide.mp3");

            var feedback = host.AddComponent<AudioFeedback>();
            Set(feedback, "flow", flow);
            Set(feedback, "sled", sled);
            Set(feedback, "effects", effects);
            Set(feedback, "slideLoop", loop);
            Set(feedback, "launch", LoadClipAsset($"{Sounds}/Effects/woosh1.wav"));
            Set(feedback, "pickup", LoadClipAsset($"{Sounds}/Old Runner Engine/PowerupCoin.wav"));
            Set(feedback, "finish", LoadClipAsset($"{Sounds}/Effects/PowerupPickup3.wav"));
            Set(feedback, "uiClick", LoadClipAsset($"{Sounds}/Old Runner Engine/UITap.mp3"));
            Set(feedback, "upgradePurchased", LoadClipAsset($"{Sounds}/UI/AW_Pickup_Chest.ogg"));

            string breakables = $"{Sounds}/BreakableObstaclesSounds";
            var surfaces = new (ObstacleSurface surface, string[] clips)[]
            {
                (ObstacleSurface.Stone, new[] { "concrete_bricks_brake.wav" }),
                (ObstacleSurface.Metal, new[] { "metal_brake.wav" }),
                (ObstacleSurface.Wood, new[] { "wood_brake.wav" }),
                (ObstacleSurface.Leaves, new[] { "leaves_brake_1.wav", "leaves_brake_2.wav", "leaves_brake_3.wav" }),
            };
            var surfaceProperty = Find(new SerializedObject(feedback), "surfaceSounds");
            var surfaceObject = surfaceProperty.serializedObject;
            surfaceProperty.arraySize = surfaces.Length;
            for (int i = 0; i < surfaces.Length; i++)
            {
                var element = surfaceProperty.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("surface").enumValueIndex = (int)surfaces[i].surface;
                var clips = element.FindPropertyRelative("clips");
                clips.arraySize = surfaces[i].clips.Length;
                for (int c = 0; c < surfaces[i].clips.Length; c++)
                    clips.GetArrayElementAtIndex(c).objectReferenceValue = LoadClipAsset($"{breakables}/{surfaces[i].clips[c]}");
            }
            surfaceObject.ApplyModifiedPropertiesWithoutUndo();

            SetArray(feedback, "femaleVoices", Enumerable.Range(1, 4).Select(n => LoadClipAsset($"{Sounds}/Vocals/LadybugOuches/PlayerOuchLadybug{n}.wav")).ToArray());
            SetArray(feedback, "maleVoices", Enumerable.Range(1, 4).Select(n => LoadClipAsset($"{Sounds}/Vocals/AdrienOuches/PlayerOuchAdrien{n}.wav")).ToArray());
            return feedback;
        }

        static AudioClip LoadClipAsset(string path)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            if (clip == null) Debug.LogWarning($"Sled Run builder: audio clip not found at {path}.");
            return clip;
        }

        // =====================================================================
        // UI
        // =====================================================================

        static Font uiFont;
        static Sprite buttonGreen, buttonGreenPressed, buttonOrange, buttonOrangePressed, buttonDisabled;

        static void BuildUi(GameFlow flow, AudioFeedback audio, UpgradeDefinition[] upgrades)
        {
            uiFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Ladybug/Content/Fonts/Curse Casual.ttf");
            buttonGreen = LoadSprite($"{UiTextures}/_Buttons/button_classic_green_default.png");
            buttonGreenPressed = LoadSprite($"{UiTextures}/_Buttons/button_classic_green_pressed.png");
            buttonOrange = LoadSprite($"{UiTextures}/_Buttons/button_classic_orange_default.png");
            buttonOrangePressed = LoadSprite($"{UiTextures}/_Buttons/button_classic_orange_pressed.png");
            buttonDisabled = LoadSprite($"{UiTextures}/_Buttons/button_classic_disabled_default.png");

            var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            var eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystem.transform.SetAsLastSibling();

            var hud = BuildHud(canvas.transform, flow);
            var menu = BuildMenu(canvas.transform, upgrades);
            var result = BuildResult(canvas.transform, upgrades);

            var presenterObject = new GameObject("UI Presenter");
            var presenter = presenterObject.AddComponent<UiPresenter>();
            Set(presenter, "flow", flow);
            Set(presenter, "menu", menu);
            Set(presenter, "hud", hud);
            Set(presenter, "result", result);
            Set(presenter, "audioFeedback", audio);
        }

        static HudView BuildHud(Transform canvas, GameFlow flow)
        {
            var root = NewRect("HUD", canvas);
            Stretch(root);
            var hud = root.gameObject.AddComponent<HudView>();

            var coins = NewText(root, "Coins", "0", 64, TextAnchor.UpperLeft);
            Anchor(coins.rectTransform, new Vector2(0f, 1f), new Vector2(50f, -50f), new Vector2(360f, 100f));
            var distance = NewText(root, "Distance", "", 52, TextAnchor.UpperCenter);
            Anchor(distance.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(520f, 100f));
            var speed = NewText(root, "Speed", "", 52, TextAnchor.UpperRight);
            Anchor(speed.rectTransform, new Vector2(1f, 1f), new Vector2(-50f, -50f), new Vector2(360f, 100f));

            var hint = NewText(root, "Aim Hint", "Drag down and release to launch!\n(or hold SPACE)", 60, TextAnchor.MiddleCenter);
            Anchor(hint.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 330f), new Vector2(980f, 220f));

            Set(hud, "flow", flow);
            Set(hud, "coinsText", coins);
            Set(hud, "distanceText", distance);
            Set(hud, "speedText", speed);
            Set(hud, "aimHint", hint.gameObject);
            return hud;
        }

        static MenuView BuildMenu(Transform canvas, UpgradeDefinition[] upgrades)
        {
            var root = NewRect("Menu", canvas);
            Stretch(root);
            AddImage(root, new Color(0.05f, 0.08f, 0.2f, 0.45f));
            var menu = root.gameObject.AddComponent<MenuView>();

            var title = NewText(root, "Title", "LADYBUG SLED RUN", 100, TextAnchor.MiddleCenter);
            Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -70f), new Vector2(1000f, 200f));

            var characterSelector = BuildSelector(root, "Character Selector", "CHARACTER", -300f);
            var levelSelector = BuildSelector(root, "Level Selector", "LEVEL", -450f);

            var panel = BuildUpgradePanel(root, upgrades, new Vector2(0f, -640f));
            var launch = NewButton(root, "Launch Button", "LAUNCH", buttonGreen, buttonGreenPressed, new Vector2(620f, 190f));
            Anchor((RectTransform)launch.transform, new Vector2(0.5f, 0f), new Vector2(0f, 240f), new Vector2(620f, 190f));

            Set(menu, "launchButton", launch);
            Set(menu, "upgrades", panel);
            Set(menu, "characterSelector", characterSelector);
            Set(menu, "levelSelector", levelSelector);
            return menu;
        }

        static ResultView BuildResult(Transform canvas, UpgradeDefinition[] upgrades)
        {
            var root = NewRect("Result", canvas);
            Stretch(root);
            AddImage(root, new Color(0.05f, 0.08f, 0.2f, 0.55f));
            var view = root.gameObject.AddComponent<ResultView>();

            var title = NewText(root, "Title", "Result", 100, TextAnchor.MiddleCenter);
            Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(1000f, 200f));
            var detail = NewText(root, "Detail", "", 58, TextAnchor.MiddleCenter);
            Anchor(detail.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -310f), new Vector2(900f, 200f));

            var panel = BuildUpgradePanel(root, upgrades, new Vector2(0f, -560f));
            var retry = NewButton(root, "Retry Button", "RETRY", buttonOrange, buttonOrangePressed, new Vector2(620f, 180f));
            Anchor((RectTransform)retry.transform, new Vector2(0.5f, 0f), new Vector2(0f, 330f), new Vector2(620f, 180f));
            var menuButton = NewButton(root, "Menu Button", "MENU", buttonGreen, buttonGreenPressed, new Vector2(420f, 130f));
            Anchor((RectTransform)menuButton.transform, new Vector2(0.5f, 0f), new Vector2(0f, 130f), new Vector2(420f, 130f));
            menuButton.GetComponentInChildren<Text>().fontSize = 52;

            Set(view, "titleText", title);
            Set(view, "detailText", detail);
            Set(view, "retryButton", retry);
            Set(view, "menuButton", menuButton);
            Set(view, "upgrades", panel);
            return view;
        }

        static SelectorView BuildSelector(RectTransform parent, string name, string caption, float y)
        {
            var rect = NewRect(name, parent);
            Anchor(rect, new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(940f, 130f));
            AddImage(rect, new Color(0.08f, 0.1f, 0.28f, 0.9f));
            var selector = rect.gameObject.AddComponent<SelectorView>();

            var captionText = NewText(rect, "Caption", caption, 30, TextAnchor.UpperCenter);
            Anchor(captionText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(600f, 40f));
            var value = NewText(rect, "Value", "-", 58, TextAnchor.MiddleCenter);
            Anchor(value.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0f, -14f), new Vector2(620f, 90f));

            var previous = NewButton(rect, "Previous", "<", buttonOrange, buttonOrangePressed, new Vector2(110f, 100f));
            Anchor((RectTransform)previous.transform, new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(110f, 100f));
            var next = NewButton(rect, "Next", ">", buttonOrange, buttonOrangePressed, new Vector2(110f, 100f));
            Anchor((RectTransform)next.transform, new Vector2(1f, 0.5f), new Vector2(-14f, 0f), new Vector2(110f, 100f));

            Set(selector, "valueText", value);
            Set(selector, "previousButton", previous);
            Set(selector, "nextButton", next);
            return selector;
        }

        static UpgradePanelView BuildUpgradePanel(RectTransform parent, UpgradeDefinition[] upgrades, Vector2 position)
        {
            const float rowHeight = 120f, rowGap = 14f, header = 110f;
            float height = header + upgrades.Length * (rowHeight + rowGap) + 20f;

            var panelRect = NewRect("Upgrade Panel", parent);
            Anchor(panelRect, new Vector2(0.5f, 1f), position, new Vector2(940f, height));
            AddImage(panelRect, new Color(0.08f, 0.1f, 0.28f, 0.9f));
            var panel = panelRect.gameObject.AddComponent<UpgradePanelView>();

            var label = NewText(panelRect, "Wallet Label", "CROISSANTS", 48, TextAnchor.MiddleLeft);
            Anchor(label.rectTransform, new Vector2(0f, 1f), new Vector2(40f, -20f), new Vector2(520f, 80f));
            var coins = NewText(panelRect, "Wallet Coins", "0", 56, TextAnchor.MiddleRight);
            Anchor(coins.rectTransform, new Vector2(1f, 1f), new Vector2(-40f, -20f), new Vector2(300f, 80f));

            var rows = new List<UpgradeRowView>();
            for (int i = 0; i < upgrades.Length; i++)
            {
                float y = -header - i * (rowHeight + rowGap);
                rows.Add(BuildUpgradeRow(panelRect, i, new Vector2(0f, y), new Vector2(880f, rowHeight)));
            }

            Set(panel, "coinsText", coins);
            SetArray(panel, "rows", rows.ToArray());
            return panel;
        }

        static UpgradeRowView BuildUpgradeRow(RectTransform parent, int index, Vector2 position, Vector2 size)
        {
            var rowRect = NewRect($"Upgrade Row {index}", parent);
            Anchor(rowRect, new Vector2(0.5f, 1f), position, size);
            AddImage(rowRect, new Color(1f, 1f, 1f, 0.08f));
            var row = rowRect.gameObject.AddComponent<UpgradeRowView>();

            var nameText = NewText(rowRect, "Name", "Upgrade", 46, TextAnchor.MiddleLeft);
            Anchor(nameText.rectTransform, new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(380f, 100f));
            var levelText = NewText(rowRect, "Level", "Lv 0/5", 40, TextAnchor.MiddleCenter);
            Anchor(levelText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(40f, 0f), new Vector2(200f, 100f));

            var buy = NewButton(rowRect, "Buy Button", "", buttonOrange, buttonOrangePressed, new Vector2(230f, 96f));
            Anchor((RectTransform)buy.transform, new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(230f, 96f));
            var cost = buy.GetComponentInChildren<Text>();
            cost.text = "0";
            cost.fontSize = 48;

            Set(row, "nameText", nameText);
            Set(row, "levelText", levelText);
            Set(row, "costText", cost);
            Set(row, "buyButton", buy);
            return row;
        }

        // ---------- UI helpers ----------

        static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        /// <summary>Places a rect by its anchor point; the anchor doubles as the pivot so offsets read as margins.</summary>
        static void Anchor(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        static Image AddImage(RectTransform rect, Color color, Sprite sprite = null)
        {
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.sprite = sprite;
            return image;
        }

        static Text NewText(RectTransform parent, string name, string value, int size, TextAnchor anchor)
        {
            var rect = NewRect(name, parent);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = size;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            var outline = rect.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.05f, 0.05f, 0.15f, 0.9f);
            outline.effectDistance = new Vector2(3f, -3f);
            return text;
        }

        static Button NewButton(RectTransform parent, string name, string label, Sprite normal, Sprite pressed, Vector2 size)
        {
            var rect = NewRect(name, parent);
            rect.sizeDelta = size;
            var image = AddImage(rect, normal != null ? Color.white : new Color(0.2f, 0.7f, 0.3f), normal);

            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            if (normal != null)
            {
                button.transition = Selectable.Transition.SpriteSwap;
                button.spriteState = new SpriteState { pressedSprite = pressed, highlightedSprite = normal, disabledSprite = buttonDisabled };
            }
            else
            {
                button.transition = Selectable.Transition.ColorTint;
            }

            var text = NewText(rect, "Label", label, 64, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform);
            return button;
        }

        static Sprite LoadSprite(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return null;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        // =====================================================================
        // Asset / serialization helpers
        // =====================================================================

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
            }

            // Importing prefabs mid-build makes Unity unload unreferenced assets; a destroyed object
            // would be serialized into the scene as an empty reference.
            asset.hideFlags |= HideFlags.DontUnloadUnusedAsset;
            return asset;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        /// <summary>Fails loudly if any wiring ended up empty, instead of discovering it at runtime.</summary>
        static void Validate(Object target, params string[] fields)
        {
            var so = new SerializedObject(target);
            foreach (string field in fields)
            {
                var property = Find(so, field);
                bool missing = property.isArray
                    ? property.arraySize == 0 || Enumerable.Range(0, property.arraySize).Any(i => property.GetArrayElementAtIndex(i).objectReferenceValue == null)
                    : property.objectReferenceValue == null;
                if (missing) Debug.LogError($"Sled Run builder: {target.GetType().Name} '{target.name}'.{field} is empty after wiring.");
            }
        }

        static SerializedProperty Find(SerializedObject so, string field) =>
            so.FindProperty(field) ?? throw new InvalidOperationException($"{so.targetObject.GetType().Name} has no serialized field '{field}'.");

        static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            Find(so, field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetArray<T>(Object target, string field, T[] values) where T : Object
        {
            var so = new SerializedObject(target);
            var property = Find(so, field);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
