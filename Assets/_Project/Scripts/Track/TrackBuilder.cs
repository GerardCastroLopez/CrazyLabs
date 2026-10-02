using System.Collections.Generic;
using CrazyLabs.Selection;
using UnityEngine;
using Random = System.Random;

namespace CrazyLabs.Track
{
    /// <summary>
    /// Builds the playable slope at runtime: ground meshes from the <see cref="TrackProfile"/>,
    /// procedurally placed obstacles and collectibles (seeded per run), scenery, and the finish line.
    /// </summary>
    public sealed class TrackBuilder : MonoBehaviour
    {
        [Header("Shared props")]
        [SerializeField] Material finishMaterial;
        [SerializeField] GameObject collectiblePrefab;

        [Header("Spawning")]
        [SerializeField] float firstSpawnZ = 55f;
        [SerializeField] Vector2 spawnSpacing = new Vector2(11f, 19f);
        [SerializeField, Range(0f, 1f)] float crashChance = 0.3f;
        [SerializeField, Range(0f, 1f)] float slowChance = 0.25f;
        [SerializeField] int collectiblesPerRow = 6;
        [SerializeField] float collectibleSpacing = 2.4f;
        [SerializeField] float collectibleHeight = 0.9f;
        [SerializeField] float sceneryDensity = 9f;

        Transform generated;
        LevelDefinition level;
        GameObject[] crashObstacles;
        GameObject[] slowObstacles;
        GameObject[] scenery;

        public TrackProfile Profile { get; private set; }

        /// <summary>(Re)generates the whole track. Pass a different seed for a different prop layout.</summary>
        public void Build(int seed, LevelDefinition levelDefinition)
        {
            level = levelDefinition;
            if (generated != null) Destroy(generated.gameObject);
            generated = new GameObject("Generated Track").transform;
            generated.SetParent(transform, false);

            Profile = new TrackProfile(level.Layout);
            ValidatePrefabs();
            BuildGround();
            SpawnProps(new Random(seed));
            BuildFinishLine();
        }

        void ValidatePrefabs()
        {
            crashObstacles = RemoveMissing(level.CrashObstacles, nameof(crashObstacles));
            slowObstacles = RemoveMissing(level.SlowObstacles, nameof(slowObstacles));
            scenery = RemoveMissing(level.Scenery, nameof(scenery));
            if (collectiblePrefab == null) Debug.LogWarning("TrackBuilder: no collectible prefab assigned.", this);
        }

        GameObject[] RemoveMissing(GameObject[] prefabs, string label)
        {
            if (prefabs == null) return new GameObject[0];
            var valid = new List<GameObject>(prefabs.Length);
            foreach (var prefab in prefabs) if (prefab != null) valid.Add(prefab);
            if (valid.Count != prefabs.Length)
                Debug.LogWarning($"TrackBuilder: {prefabs.Length - valid.Count} unassigned entries in '{label}' were ignored.", this);
            return valid.ToArray();
        }

        // ---------- ground ----------

        void BuildGround()
        {
            CreateStrip("Track", Profile.HalfWidth, 0f, 2f, level.TrackMaterial);
            CreateStrip("Surround", 90f, -0.06f, 6f, level.SurroundMaterial);
        }

        void CreateStrip(string stripName, float halfWidth, float yOffset, float zStep, Material material)
        {
            int rows = Mathf.CeilToInt(Profile.Length / zStep) + 1;
            var vertices = new Vector3[rows * 2];
            var uvs = new Vector2[rows * 2];
            var triangles = new int[(rows - 1) * 6];

            for (int i = 0; i < rows; i++)
            {
                float z = Mathf.Min(i * zStep, Profile.Length);
                float y = Profile.HeightAt(z) + yOffset;
                vertices[i * 2] = new Vector3(-halfWidth, y, z);
                vertices[i * 2 + 1] = new Vector3(halfWidth, y, z);
                uvs[i * 2] = new Vector2(-halfWidth / 6f, z / 6f);
                uvs[i * 2 + 1] = new Vector2(halfWidth / 6f, z / 6f);
            }

            for (int i = 0; i < rows - 1; i++)
            {
                int v = i * 2, t = i * 6;
                triangles[t] = v; triangles[t + 1] = v + 2; triangles[t + 2] = v + 1;
                triangles[t + 3] = v + 1; triangles[t + 4] = v + 2; triangles[t + 5] = v + 3;
            }

            var mesh = new Mesh { name = stripName };
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            var go = new GameObject(stripName, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(generated, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        // ---------- props ----------

        void SpawnProps(Random rng)
        {
            float lastZ = Profile.FinishZ - 30f;
            float z = firstSpawnZ;

            while (z < lastZ)
            {
                double roll = rng.NextDouble();
                if (roll < crashChance) SpawnObstacleGroup(crashObstacles, z, rng);
                else if (roll < crashChance + slowChance) SpawnObstacleGroup(slowObstacles, z, rng);
                else SpawnCollectibleRow(z, rng);

                z += Mathf.Lerp(spawnSpacing.x, spawnSpacing.y, (float)rng.NextDouble());
            }

            SpawnScenery(rng);
        }

        void SpawnObstacleGroup(GameObject[] pool, float z, Random rng)
        {
            if (pool == null || pool.Length == 0) return;

            // One or two obstacles per row; with two, keep them apart so there is always a gap.
            int count = rng.NextDouble() < 0.35 ? 2 : 1;
            float usable = Profile.HalfWidth - 1.5f;
            float firstX = Mathf.Lerp(-usable, usable, (float)rng.NextDouble());
            Place(pool[rng.Next(pool.Length)], firstX, z, rng);

            if (count == 2)
            {
                float secondX = firstX > 0f ? firstX - Between(rng, 5f, 8f) : firstX + Between(rng, 5f, 8f);
                if (Mathf.Abs(secondX) <= usable)
                    Place(pool[rng.Next(pool.Length)], secondX, z, rng);
            }
        }

        void SpawnCollectibleRow(float z, Random rng)
        {
            if (collectiblePrefab == null) return;

            float usable = Profile.HalfWidth - 1.5f;
            float laneX = Mathf.Lerp(-usable, usable, (float)rng.NextDouble());
            float sway = (float)rng.NextDouble() < 0.5f ? 0f : Between(rng, 0.6f, 1.4f);

            for (int i = 0; i < collectiblesPerRow; i++)
            {
                float rowZ = z + i * collectibleSpacing;
                float x = Mathf.Clamp(laneX + Mathf.Sin(i * 0.9f) * sway, -usable, usable);
                var item = Instantiate(collectiblePrefab, GroundPoint(x, rowZ, collectibleHeight),
                    Quaternion.identity, generated);
                item.name = collectiblePrefab.name;
            }
        }

        void SpawnScenery(Random rng)
        {
            if (scenery == null || scenery.Length == 0) return;

            for (float z = 0f; z < Profile.Length; z += sceneryDensity * Between(rng, 0.6f, 1.4f))
            {
                float side = rng.NextDouble() < 0.5 ? -1f : 1f;
                float x = side * (Profile.HalfWidth + 2f + Between(rng, 0f, 14f));
                var prefab = scenery[rng.Next(scenery.Length)];
                var instance = Place(prefab, x, z, rng);
                instance.transform.localScale *= Between(rng, 0.8f, 1.5f);
            }
        }

        GameObject Place(GameObject prefab, float x, float z, Random rng)
        {
            float slopeDegrees = Profile.PitchAt(z) * Mathf.Rad2Deg;
            var rotation = Quaternion.Euler(slopeDegrees, 0f, 0f) * Quaternion.Euler(0f, Between(rng, -25f, 25f), 0f);
            var instance = Instantiate(prefab, GroundPoint(x, z, 0f), rotation, generated);
            instance.name = prefab.name;
            return instance;
        }

        Vector3 GroundPoint(float x, float z, float height) => new Vector3(x, Profile.HeightAt(z) + height, z);

        static float Between(Random rng, float min, float max) => min + (float)rng.NextDouble() * (max - min);

        // ---------- finish ----------

        void BuildFinishLine()
        {
            float z = Profile.FinishZ;
            float width = Profile.HalfWidth * 2f;
            float groundY = Profile.HeightAt(z);
            const float postHeight = 6f;

            var root = new GameObject("Finish Line");
            root.transform.SetParent(generated, false);
            root.transform.position = new Vector3(0f, groundY, z);

            AddPart(root.transform, PrimitiveType.Cube, "Post L", new Vector3(-Profile.HalfWidth, postHeight * 0.5f, 0f), new Vector3(0.6f, postHeight, 0.6f));
            AddPart(root.transform, PrimitiveType.Cube, "Post R", new Vector3(Profile.HalfWidth, postHeight * 0.5f, 0f), new Vector3(0.6f, postHeight, 0.6f));
            AddPart(root.transform, PrimitiveType.Cube, "Banner", new Vector3(0f, postHeight - 0.5f, 0f), new Vector3(width, 1.2f, 0.4f));

            var trigger = root.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = new Vector3(0f, 2.5f, 0f);
            trigger.size = new Vector3(width, 5f, 1.5f);
            root.AddComponent<FinishLine>();
        }

        void AddPart(Transform parent, PrimitiveType type, string partName, Vector3 localPosition, Vector3 scale)
        {
            var part = GameObject.CreatePrimitive(type);
            part.name = partName;
            Destroy(part.GetComponent<Collider>());
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = scale;
            if (finishMaterial != null) part.GetComponent<Renderer>().sharedMaterial = finishMaterial;
        }
    }
}
