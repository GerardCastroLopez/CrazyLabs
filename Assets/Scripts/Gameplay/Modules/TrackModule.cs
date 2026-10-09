using System;
using System.Collections.Generic;
using CrazyLabs.Gameplay.Config;
using CrazyLabs.Levels.Data;
using CrazyLabs.Gameplay.Track;
using Cysharp.Threading.Tasks;
using gSDK.Patterns.Pooling;
using UnityEngine;

namespace CrazyLabs.Gameplay.Modules
{
    public class TrackModule : MonoBehaviour
    {
        public TrackProfile Profile { get; private set; }
        
        [SerializeField] private FinishLine _finish;
        [SerializeField] private SlingshotVisual _slingshot;

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
        private GroundTuningData _ground;
        private SpawnTuningData _spawn;
        private GameObject[] _crashObstacles, _slowObstacles, _scenery;

        
        public void BuildLevel(LevelData levelData, GroundTuningData ground, SpawnTuningData spawn, SlingshotTuningData slingshot)
        {
            _ground = ground;
            _spawn = spawn;
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
            PlaceFinishLine();
            PlaceSlingshot(slingshot);
        }

        public void TickSlingshot(bool aiming, Vector3 pouchPosition)
        {
            _slingshot.Tick(aiming, pouchPosition);
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
            if (!_spawn.CollectiblePrefab)
            {
                return;
            }

            if (!_spawn.CollectiblePrefab.TryGetComponent(out Collectible collectible))
            {
                Debug.LogError($"'{_spawn.CollectiblePrefab.name}' has no Collectible component, so it can't be used as a collectible", _spawn.CollectiblePrefab);
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
            CreateStrip("Track", Profile.HalfWidth, 0f, _ground.TrackStripStep, _level.TrackMaterial);
            CreateStrip("Surround", _ground.SurroundHalfWidth, _ground.SurroundYOffset, _ground.SurroundStripStep, _level.SurroundMaterial);
        }

        private void CreateStrip(string stripName, float halfWidth, float yOffset, float zStep, Material material)
        {
            float startZ = -_ground.ExtraBehind;
            float endZ = Profile.Length + _ground.ExtraAhead;
            int rows = Mathf.CeilToInt((endZ - startZ) / zStep) + 1;
            var vertices = new Vector3[rows * kVerticesPerRow];
            var uvs = new Vector2[rows * kVerticesPerRow];
            var triangles = new int[(rows - 1) * kIndicesPerQuad];

            for (int i = 0; i < rows; i++)
            {
                float z = Mathf.Min(startZ + i * zStep, endZ);
                float y = Profile.HeightAt(z) + yOffset;
                int left = i * kVerticesPerRow, right = left + 1;
                vertices[left] = new Vector3(-halfWidth, y, z);
                vertices[right] = new Vector3(halfWidth, y, z);
                uvs[left] = new Vector2(-halfWidth / _ground.UvTileSize, z / _ground.UvTileSize);
                uvs[right] = new Vector2(halfWidth / _ground.UvTileSize, z / _ground.UvTileSize);
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
            float lastZ = Profile.FinishZ - _spawn.PropsStopBeforeFinish;
            float z = _spawn.FirstSpawnZ;

            while (z < lastZ)
            {
                double roll = rng.NextDouble();
                if (roll < _level.CrashChance)
                {
                    await SpawnObstacleGroup(_crashObstacles, z, rng);
                }
                else if (roll < _level.CrashChance + _level.SlowChance)
                {
                    await SpawnObstacleGroup(_slowObstacles, z, rng);
                }
                else
                {
                    await SpawnCollectibleRow(z, rng);
                }
                
                z += Mathf.Lerp(_spawn.SpawnSpacing.x, _spawn.SpawnSpacing.y, (float)rng.NextDouble());
            }

            await SpawnScenery(rng);
        }

        private async UniTask SpawnObstacleGroup(GameObject[] prefabs, float z, System.Random rng)
        {
            if (prefabs == null || prefabs.Length == 0)
            {
                return;
            }

            int count = rng.NextDouble() < _level.PairChance ? 2 : 1;
            float usable = Profile.HalfWidth - _spawn.LaneEdgeMargin;
            float firstX = Mathf.Lerp(-usable, usable, (float)rng.NextDouble());
            await PlaceObstacle(prefabs[rng.Next(prefabs.Length)], firstX, z, rng);

            if (count == 2)
            {
                float gap = Between(rng, _spawn.PairGap);
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

            float usable = Profile.HalfWidth - _spawn.LaneEdgeMargin;
            float laneX = Mathf.Lerp(-usable, usable, (float)rng.NextDouble());
            float sway = rng.NextDouble() < _spawn.SwayChance ? Between(rng, _spawn.SwayAmount) : 0f;

            for (int i = 0; i < _spawn.CollectiblesPerRow; i++)
            {
                float rowZ = z + i * _spawn.CollectibleSpacing;
                float x = Mathf.Clamp(laneX + Mathf.Sin(i * _spawn.SwayFrequency) * sway, -usable, usable);

                var collectible = await _collectiblePool.GetAsync();
                collectible.transform.SetParent(_propsRoot, false);
                collectible.transform.SetPositionAndRotation(GroundPoint(x, rowZ, _spawn.CollectibleHeight), Quaternion.identity);
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

            for (float z = -_ground.ExtraBehind; z < Profile.Length + _ground.ExtraAhead; z += _spawn.SceneryDensity * Between(rng, _spawn.ScenerySpacingJitter))
            {
                float side = rng.NextDouble() < kCoinFlip ? -1f : 1f;
                float x = side * (Profile.HalfWidth + _spawn.SceneryMinDistance + Between(rng, 0f, _spawn.SceneryExtraDistance));
                var prefab = _scenery[rng.Next(_scenery.Length)];

                if (!_sceneryPools.TryGetValue(prefab, out var pool))
                {
                    continue;
                }

                var instance = await pool.GetAsync();
                Place(instance, x, z, rng);
                instance.localScale = _sceneryBaseScales[prefab] * Between(rng, _spawn.SceneryScaleRange);
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
            var rotation = Quaternion.Euler(slopeDegrees, 0f, 0f) * Quaternion.Euler(0f, Between(rng, -_spawn.PropYawRange, _spawn.PropYawRange), 0f);
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

        private void PlaceSlingshot(SlingshotTuningData tuning)
        {
            float z = tuning.StartZ + tuning.PostForwardOffset;
            _slingshot.Place(new Vector3(0f, Profile.HeightAt(z), z), Quaternion.Euler(Profile.PitchAt(z) * Mathf.Rad2Deg, 0f, 0f));
        }

        private void PlaceFinishLine()
        {
            float z = Profile.FinishZ;
            _finish.Place(new Vector3(0f, Profile.HeightAt(z), z), Profile.HalfWidth * 2f);
        }
    }
}
