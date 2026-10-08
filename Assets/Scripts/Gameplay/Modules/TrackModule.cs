using System;
using System.Collections.Generic;
using CrazyLabs.Levels.Data;
using CrazyLabs.Gameplay.Track;
using Cysharp.Threading.Tasks;
using gSDK.Patterns.Pooling;
using UnityEngine;

namespace CrazyLabs.Gameplay.Modules
{
    public class TrackModule : MonoBehaviour
    {
        [Serializable]
        private class FinishLineSettings
        {
            public float PostHeight = 6f;
            public float PostThickness = 0.6f;
            [Tooltip("How far the banner hangs below the top of the posts.")]
            public float BannerDrop = 0.5f;
            public float BannerHeight = 1.2f;
            public float BannerThickness = 0.4f;
            [Tooltip("Size of the trigger the sled crosses to finish.")]
            public float TriggerHeight = 5f;
            public float TriggerDepth = 1.5f;
        }
        
        public TrackProfile Profile { get; private set; }
        
        [Header("Shared props")]
        [SerializeField] private Material _finishMaterial;
        [SerializeField] private GameObject _collectiblePrefab;

        [Header("Ground")]
        [SerializeField] private float _trackStripStep = 2f;
        [SerializeField] private float _surroundStripStep = 6f;
        [SerializeField] private float _surroundHalfWidth = 90f;
        [Tooltip("The surround sits slightly below the track so the two never z-fight.")]
        [SerializeField] private float _surroundYOffset = -0.06f;
        [Tooltip("Meters of ground covered by one repeat of the ground texture.")]
        [SerializeField] private float _uvTileSize = 6f;

        [Header("Spawning")]
        [SerializeField] private float _firstSpawnZ = 55f;
        [Tooltip("No props are placed within this distance of the finish line.")]
        [SerializeField] private float _propsStopBeforeFinish = 30f;
        [Tooltip("Chance that an obstacle row has two obstacles instead of one.")]
        [SerializeField, Range(0f, 1f)] private float _pairChance = 0.35f;
        [Tooltip("Props stay this far from the track edge.")]
        [SerializeField] private float _laneEdgeMargin = 1.5f;
        [Tooltip("Min and max sideways gap between two obstacles in the same row, so there is always a way through.")]
        [SerializeField] private Vector2 _pairGap = new(5f, 8f);
        [Tooltip("Chance that a croissant row wiggles instead of running straight.")]
        [SerializeField, Range(0f, 1f)] private float _swayChance = 0.5f;
        [SerializeField] private Vector2 _swayAmount = new(0.6f, 1.4f);
        [SerializeField] private float _swayFrequency = 0.9f;
        [Tooltip("Props get a random yaw in [-range, +range] degrees.")]
        [SerializeField] private float _propYawRange = 25f;
        [SerializeField] private Vector2 _spawnSpacing = new(11f, 19f);
        [SerializeField, Range(0f, 1f)] private float _crashChance = 0.3f;
        [SerializeField, Range(0f, 1f)] private float _slowChance = 0.25f;
        [SerializeField] private int _collectiblesPerRow = 6;
        [SerializeField] private float _collectibleSpacing = 2.4f;
        [SerializeField] private float _collectibleHeight = 0.9f;
        [Header("Scenery")]
        [SerializeField] private float _sceneryDensity = 9f;
        [Tooltip("Multiplies the scenery spacing by a random value in this range.")]
        [SerializeField] private Vector2 _scenerySpacingJitter = new(0.6f, 1.4f);
        [Tooltip("Scenery starts this far from the track edge...")]
        [SerializeField] private float _sceneryMinDistance = 2f;
        [Tooltip("...and is spread up to this much further out.")]
        [SerializeField] private float _sceneryExtraDistance = 14f;
        [SerializeField] private Vector2 _sceneryScaleRange = new(0.8f, 1.5f);

        [Header("Finish line")]
        [SerializeField] private FinishLineSettings _finishLine = new();

        private const int kVerticesPerRow = 2;
        private const int kIndicesPerQuad = 6;
        private const float kCoinFlip = 0.5f;
        private static readonly int[] kQuadTriangles = { 0, 2, 1, 1, 2, 3 };

        private readonly List<Collectible> _collectibles = new();
        private readonly Dictionary<GameObject, ComponentPool<Obstacle>> _obstaclePools = new();
        private readonly Dictionary<GameObject, ComponentPool<Transform>> _sceneryPools = new();
        private readonly Dictionary<GameObject, Vector3> _sceneryBaseScales = new();
        private ComponentPool<Collectible> _collectiblePool;
        private Transform _templatesRoot, _propsRoot;
        private LevelData _poolsLevel;
        private Transform _generated;
        private LevelData _level;
        private GameObject[] _crashObstacles, _slowObstacles, _scenery;

        
        
        public void BuildLevel(LevelData levelData)
        {
            if (_generated)
            {
                Destroy(_generated.gameObject);
            }

            _level = levelData;
            _generated = new GameObject("Generated Track").transform;
            _generated.SetParent(transform, false);

            Profile = new TrackProfile(_level.Layout);
            ValidatePrefabs();
            EnsurePools();
            BuildGround();
            BuildFinishLine();
        }

        public async UniTask RespawnProps(int seed)
        {
            ReturnPropsToPools();
            await SpawnProps(new System.Random(seed));
        }

        public void Tick(float deltaTime, float time)
        {
            foreach (var collectible in _collectibles)
            {
                collectible.Tick(deltaTime, time);
            }
        }

        private void OnDestroy()
        {
            DisposePools();
        }

        private void EnsurePools()
        {
            if (_poolsLevel == _level)
            {
                return;
            }

            DisposePools();
            _poolsLevel = _level;

            _templatesRoot = CreateRoot("Pool Templates");
            _propsRoot ??= CreateRoot("Props");

            _crashObstacles.Foreach(CreateObstaclePool);
            _slowObstacles.Foreach(CreateObstaclePool);
            _scenery.Foreach(CreateSceneryPool);
            CreateCollectiblePool();
        }

        private Transform CreateRoot(string rootName)
        {
            var root = new GameObject(rootName).transform;
            root.SetParent(transform, false);
            return root;
        }

        private void CreateObstaclePool(GameObject prefab)
        {
            if (_obstaclePools.ContainsKey(prefab))
            {
                return;
            }

            if (!prefab.TryGetComponent(out Obstacle obstacle))
            {
                Debug.LogError($"'{prefab.name}' has no Obstacle component, so it can't be used as an obstacle", prefab);
                return;
            }

            var pool = new ComponentPool<Obstacle>();
            pool.Init(obstacle, 0);
            _obstaclePools.Add(prefab, pool);
        }

        private void CreateSceneryPool(GameObject prefab)
        {
            if (_sceneryPools.ContainsKey(prefab))
            {
                return;
            }

            _sceneryBaseScales.Add(prefab, prefab.transform.localScale);

            var pool = new ComponentPool<Transform>();
            pool.Init(prefab.transform, 0);
            _sceneryPools.Add(prefab, pool);
        }

        private void CreateCollectiblePool()
        {
            if (!_collectiblePrefab)
            {
                return;
            }

            if (!_collectiblePrefab.TryGetComponent(out Collectible collectible))
            {
                Debug.LogError($"'{_collectiblePrefab.name}' has no Collectible component, so it can't be used as a collectible", _collectiblePrefab);
                return;
            }

            _collectiblePool = new();
            _collectiblePool.Init(collectible, 0);
        }

        private void ReturnPropsToPools()
        {
            _collectibles.Clear();
            _collectiblePool?.ReturnAllInstances();
            _obstaclePools.Values.Foreach(pool => pool.ReturnAllInstances());
            _sceneryPools.Values.Foreach(pool => pool.ReturnAllInstances());
        }

        private void DisposePools()
        {
            _collectibles.Clear();
            _collectiblePool?.Dispose();
            _collectiblePool = null;
            _obstaclePools.Values.Foreach(pool => pool.Dispose());
            _obstaclePools.Clear();
            _sceneryPools.Values.Foreach(pool => pool.Dispose());
            _sceneryPools.Clear();
            _sceneryBaseScales.Clear();

            if (_templatesRoot)
            {
                Destroy(_templatesRoot.gameObject);
            }

            _poolsLevel = null;
        }

        private void ValidatePrefabs()
        {
            _crashObstacles = RemoveMissing(_level.CrashObstacles, nameof(_crashObstacles));
            _slowObstacles = RemoveMissing(_level.SlowObstacles, nameof(_slowObstacles));
            _scenery = RemoveMissing(_level.Scenery, nameof(_scenery));
        }

        private GameObject[] RemoveMissing(GameObject[] prefabs, string label)
        {
            if (prefabs == null)
            {
                return Array.Empty<GameObject>();
            }

            var valid = new List<GameObject>(prefabs.Length);
            prefabs.Foreach(p => {
                
                if (p)
                {
                    valid.Add(p);
                }
            });
            
            if (valid.Count != prefabs.Length)
                Debug.LogWarning($"TrackBuilder: {prefabs.Length - valid.Count} unassigned entries in '{label}' were ignored.", this);
            
            return valid.ToArray();
        }

        private void BuildGround()
        {
            CreateStrip("Track", Profile.HalfWidth, 0f, _trackStripStep, _level.TrackMaterial);
            CreateStrip("Surround", _surroundHalfWidth, _surroundYOffset, _surroundStripStep, _level.SurroundMaterial);
        }

        private void CreateStrip(string stripName, float halfWidth, float yOffset, float zStep, Material material)
        {
            int rows = Mathf.CeilToInt(Profile.Length / zStep) + 1;
            var vertices = new Vector3[rows * kVerticesPerRow];
            var uvs = new Vector2[rows * kVerticesPerRow];
            var triangles = new int[(rows - 1) * kIndicesPerQuad];

            for (int i = 0; i < rows; i++)
            {
                float z = Mathf.Min(i * zStep, Profile.Length);
                float y = Profile.HeightAt(z) + yOffset;
                int left = i * kVerticesPerRow, right = left + 1;
                vertices[left] = new Vector3(-halfWidth, y, z);
                vertices[right] = new Vector3(halfWidth, y, z);
                uvs[left] = new Vector2(-halfWidth / _uvTileSize, z / _uvTileSize);
                uvs[right] = new Vector2(halfWidth / _uvTileSize, z / _uvTileSize);
            }

            for (int i = 0; i < rows - 1; i++)
            {
                int firstVertex = i * kVerticesPerRow, firstIndex = i * kIndicesPerQuad;
                for (int k = 0; k < kIndicesPerQuad; k++)
                {
                    triangles[firstIndex + k] = firstVertex + kQuadTriangles[k];
                }
            }

            var mesh = new Mesh {
                name = stripName,
                vertices = vertices,
                uv = uvs,
                triangles = triangles,
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var go = new GameObject(stripName);
            go.transform.SetParent(_generated, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        private async UniTask SpawnProps(System.Random rng)
        {
            float lastZ = Profile.FinishZ - _propsStopBeforeFinish;
            float z = _firstSpawnZ;

            while (z < lastZ)
            {
                double roll = rng.NextDouble();
                if (roll < _crashChance)
                {
                    await SpawnObstacleGroup(_crashObstacles, z, rng);
                }
                else if (roll < _crashChance + _slowChance)
                {
                    await SpawnObstacleGroup(_slowObstacles, z, rng);
                }
                else
                {
                    await SpawnCollectibleRow(z, rng);
                }
                
                z += Mathf.Lerp(_spawnSpacing.x, _spawnSpacing.y, (float)rng.NextDouble());
            }

            await SpawnScenery(rng);
        }

        private async UniTask SpawnObstacleGroup(GameObject[] prefabs, float z, System.Random rng)
        {
            if (prefabs == null || prefabs.Length == 0)
            {
                return;
            }

            int count = rng.NextDouble() < _pairChance ? 2 : 1;
            float usable = Profile.HalfWidth - _laneEdgeMargin;
            float firstX = Mathf.Lerp(-usable, usable, (float)rng.NextDouble());
            await PlaceObstacle(prefabs[rng.Next(prefabs.Length)], firstX, z, rng);

            if (count == 2)
            {
                float gap = Between(rng, _pairGap);
                float secondX = firstX > 0f ? firstX - gap : firstX + gap;
                if (Mathf.Abs(secondX) <= usable)
                {
                    await PlaceObstacle(prefabs[rng.Next(prefabs.Length)], secondX, z, rng);
                }
            }
        }

        private async UniTask SpawnCollectibleRow(float z, System.Random rng)
        {
            if (_collectiblePool == null)
            {
                return;
            }

            float usable = Profile.HalfWidth - _laneEdgeMargin;
            float laneX = Mathf.Lerp(-usable, usable, (float)rng.NextDouble());
            float sway = rng.NextDouble() < _swayChance ? Between(rng, _swayAmount) : 0f;

            for (int i = 0; i < _collectiblesPerRow; i++)
            {
                float rowZ = z + i * _collectibleSpacing;
                float x = Mathf.Clamp(laneX + Mathf.Sin(i * _swayFrequency) * sway, -usable, usable);

                var collectible = await _collectiblePool.GetAsync();
                collectible.transform.SetParent(_propsRoot, false);
                collectible.transform.SetPositionAndRotation(GroundPoint(x, rowZ, _collectibleHeight), Quaternion.identity);
                collectible.Init();
                _collectibles.Add(collectible);
            }
        }

        private async UniTask SpawnScenery(System.Random rng)
        {
            if (_scenery == null || _scenery.Length == 0)
            {
                return;
            }

            for (float z = 0f; z < Profile.Length; z += _sceneryDensity * Between(rng, _scenerySpacingJitter))
            {
                float side = rng.NextDouble() < kCoinFlip ? -1f : 1f;
                float x = side * (Profile.HalfWidth + _sceneryMinDistance + Between(rng, 0f, _sceneryExtraDistance));
                var prefab = _scenery[rng.Next(_scenery.Length)];

                if (!_sceneryPools.TryGetValue(prefab, out var pool))
                {
                    continue;
                }

                var instance = await pool.GetAsync();
                Place(instance, x, z, rng);
                instance.localScale = _sceneryBaseScales[prefab] * Between(rng, _sceneryScaleRange);
            }
        }

        private async UniTask PlaceObstacle(GameObject prefab, float x, float z, System.Random rng)
        {
            if (!_obstaclePools.TryGetValue(prefab, out var pool))
            {
                return;
            }

            var obstacle = await pool.GetAsync();
            obstacle.ResetState();
            Place(obstacle.transform, x, z, rng);
        }

        private void Place(Transform instance, float x, float z, System.Random rng)
        {
            float slopeDegrees = Profile.PitchAt(z) * Mathf.Rad2Deg;
            var rotation = Quaternion.Euler(slopeDegrees, 0f, 0f) * Quaternion.Euler(0f, Between(rng, -_propYawRange, _propYawRange), 0f);
            instance.SetParent(_propsRoot, false);
            instance.SetPositionAndRotation(GroundPoint(x, z, 0f), rotation);
        }

        private Vector3 GroundPoint(float x, float z, float height)
        {
            return new(x, Profile.HeightAt(z) + height, z);
        }

        private static float Between(System.Random rng, float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }

        private static float Between(System.Random rng, Vector2 range)
        {
            return Between(rng, range.x, range.y);
        }

        private void BuildFinishLine()
        {
            var settings = _finishLine;
            float z = Profile.FinishZ;
            float width = Profile.HalfWidth * 2f;

            var root = new GameObject("Finish Line");
            root.transform.SetParent(_generated, false);
            root.transform.position = new(0f, Profile.HeightAt(z), z);

            var postSize = new Vector3(settings.PostThickness, settings.PostHeight, settings.PostThickness);
            float postCenterY = settings.PostHeight * 0.5f;
            AddPart(root.transform, PrimitiveType.Cube, "Post L", new Vector3(-Profile.HalfWidth, postCenterY, 0f), postSize);
            AddPart(root.transform, PrimitiveType.Cube, "Post R", new Vector3(Profile.HalfWidth, postCenterY, 0f), postSize);
            AddPart(root.transform, PrimitiveType.Cube, "Banner", new Vector3(0f, settings.PostHeight - settings.BannerDrop, 0f),
                new Vector3(width, settings.BannerHeight, settings.BannerThickness));

            var trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, settings.TriggerHeight * 0.5f, 0f);
            trigger.size = new Vector3(width, settings.TriggerHeight, settings.TriggerDepth);
            root.AddComponent<FinishLine>();
        }

        private void AddPart(Transform parent, PrimitiveType type, string partName, Vector3 localPosition, Vector3 scale)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = partName;
            Destroy(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = scale;
            if (_finishMaterial != null)
            {
                part.GetComponent<Renderer>().sharedMaterial = _finishMaterial;
            }
        }
    }
}
