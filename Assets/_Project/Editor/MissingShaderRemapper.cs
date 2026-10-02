using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace CrazyLabs.EditorTools
{
    /// <summary>
    /// The Ladybug asset pack ships without its custom shaders, leaving ~275 materials pink.
    /// This tool finds every material whose shader is missing and moves it to a built-in
    /// equivalent, carrying over the textures, colours and cutoff stored in the .mat file.
    /// </summary>
    public static class MissingShaderRemapper
    {
        const string ErrorShaderName = "Hidden/InternalErrorShader";

        const string EnvironmentShader = "Mobile/Diffuse";
        const string CutoutShader = "Legacy Shaders/Transparent/Cutout/Diffuse";
        const string AlphaFxShader = "Legacy Shaders/Particles/Alpha Blended";
        const string AdditiveFxShader = "Legacy Shaders/Particles/Additive";

        static readonly string[] MainTexCandidates = { "_Diffuse", "_MainTex", "_Tex1", "_Texture1" };
        static readonly Regex AdditiveName = new Regex("glow|light|flare|spark|beam", RegexOptions.IgnoreCase);

        enum Kind { Environment, Cutout, FxAlpha, FxAdditive }

        [MenuItem("Tools/Ladybug/Remap Missing Shaders (Preview)")]
        static void Preview() => Run(apply: false);

        [MenuItem("Tools/Ladybug/Remap Missing Shaders")]
        static void Apply() => Run(apply: true);

        static void Run(bool apply)
        {
            var counts = new Dictionary<Kind, int>();
            var skipped = new List<string>();
            var log = new StringBuilder();
            int total = 0;

            string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
            try
            {
                AssetDatabase.StartAssetEditing();
                for (int i = 0; i < guids.Length; i++)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                    EditorUtility.DisplayProgressBar("Remapping materials", path, (float)i / guids.Length);

                    var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (material == null || !IsBroken(material)) continue;

                    total++;
                    var data = SavedProperties.Read(material);
                    Kind kind = Classify(material, data);
                    counts[kind] = counts.TryGetValue(kind, out int c) ? c + 1 : 1;
                    log.AppendLine($"  [{kind}] {path}");

                    if (!apply) continue;
                    if (!Convert(material, kind, data)) skipped.Add(path);
                    else EditorUtility.SetDirty(material);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
            }

            if (apply) AssetDatabase.SaveAssets();

            var summary = new StringBuilder();
            summary.AppendLine(apply ? $"Remapped {total} materials." : $"Preview: {total} materials have a missing shader.");
            foreach (var pair in counts) summary.AppendLine($"  {pair.Key}: {pair.Value}");
            if (skipped.Count > 0) summary.AppendLine($"Skipped {skipped.Count} (replacement shader not found): {string.Join(", ", skipped)}");
            Debug.Log(summary + "\n" + log);
        }

        static bool IsBroken(Material material) =>
            material.shader == null || material.shader.name == ErrorShaderName;

        static Kind Classify(Material material, SavedProperties data)
        {
            // Particle-style materials carry a tint / soft-particle property that environment materials never do.
            bool isFx = data.HasProperty("_TintColor") || data.HasProperty("_InvFade");
            bool blended = data.Keywords.Contains("_ALPHABLEND_ON") || data.Keywords.Contains("_ALPHAPREMULTIPLY_ON")
                           || data.GetFloat("_Mode", 0f) >= 2f || material.renderQueue >= 3000;

            if (isFx || blended)
                return AdditiveName.IsMatch(material.name) ? Kind.FxAdditive : Kind.FxAlpha;

            if (data.GetFloat("_Mode", 0f) == 1f) return Kind.Cutout;
            return Kind.Environment;
        }

        static bool Convert(Material material, Kind kind, SavedProperties data)
        {
            Shader shader = Shader.Find(kind switch
            {
                Kind.Cutout => CutoutShader,
                Kind.FxAlpha => AlphaFxShader,
                Kind.FxAdditive => AdditiveFxShader,
                _ => EnvironmentShader,
            });
            if (shader == null) return false;

            material.shader = shader;
            material.renderQueue = -1;
            material.shaderKeywords = null;

            if (data.TryGetMainTexture(MainTexCandidates, out var main, out var scale, out var offset))
            {
                material.SetTexture("_MainTex", main);
                material.SetTextureScale("_MainTex", scale);
                material.SetTextureOffset("_MainTex", offset);
            }

            bool isFx = kind == Kind.FxAlpha || kind == Kind.FxAdditive;
            if (isFx)
            {
                // Legacy particle shaders tint with _TintColor; fall back to _Color, then neutral grey.
                Color tint = data.TryGetColor("_TintColor", out var t) ? t
                    : data.TryGetColor("_Color", out var c) ? c
                    : new Color(0.5f, 0.5f, 0.5f, 0.5f);
                material.SetColor("_TintColor", tint);
            }
            else
            {
                if (data.TryGetColor("_Color", out var color)) material.SetColor("_Color", color);
                if (kind == Kind.Cutout && material.HasProperty("_Cutoff"))
                    material.SetFloat("_Cutoff", data.GetFloat("_Cutoff", 0.5f));
            }
            return true;
        }

        /// <summary>
        /// Reads a material's serialized property block directly. Needed because a material whose
        /// shader is missing refuses Get/Set calls (HasProperty is always false) yet keeps its data.
        /// </summary>
        sealed class SavedProperties
        {
            readonly Dictionary<string, (Texture tex, Vector2 scale, Vector2 offset)> textures = new();
            readonly Dictionary<string, float> floats = new();
            readonly Dictionary<string, Color> colors = new();
            public HashSet<string> Keywords { get; } = new();

            public static SavedProperties Read(Material material)
            {
                var result = new SavedProperties();
                var so = new SerializedObject(material);

                foreach (string keyword in ReadStringArray(so.FindProperty("m_ValidKeywords")))
                    result.Keywords.Add(keyword);
                // Legacy .mat files store keywords as a single space-separated string.
                var legacy = so.FindProperty("m_ShaderKeywords");
                if (legacy != null && legacy.propertyType == SerializedPropertyType.String)
                    foreach (string keyword in legacy.stringValue.Split(' ', System.StringSplitOptions.RemoveEmptyEntries))
                        result.Keywords.Add(keyword);

                var texEnvs = so.FindProperty("m_SavedProperties.m_TexEnvs");
                for (int i = 0; texEnvs != null && i < texEnvs.arraySize; i++)
                {
                    var element = texEnvs.GetArrayElementAtIndex(i);
                    string name = element.FindPropertyRelative("first").stringValue;
                    var second = element.FindPropertyRelative("second");
                    result.textures[name] = (
                        second.FindPropertyRelative("m_Texture").objectReferenceValue as Texture,
                        second.FindPropertyRelative("m_Scale").vector2Value,
                        second.FindPropertyRelative("m_Offset").vector2Value);
                }

                var floatProps = so.FindProperty("m_SavedProperties.m_Floats");
                for (int i = 0; floatProps != null && i < floatProps.arraySize; i++)
                {
                    var element = floatProps.GetArrayElementAtIndex(i);
                    result.floats[element.FindPropertyRelative("first").stringValue] =
                        element.FindPropertyRelative("second").floatValue;
                }

                var colorProps = so.FindProperty("m_SavedProperties.m_Colors");
                for (int i = 0; colorProps != null && i < colorProps.arraySize; i++)
                {
                    var element = colorProps.GetArrayElementAtIndex(i);
                    result.colors[element.FindPropertyRelative("first").stringValue] =
                        element.FindPropertyRelative("second").colorValue;
                }
                return result;
            }

            public bool HasProperty(string name) =>
                floats.ContainsKey(name) || colors.ContainsKey(name) || textures.ContainsKey(name);

            public float GetFloat(string name, float fallback) =>
                floats.TryGetValue(name, out float value) ? value : fallback;

            public bool TryGetColor(string name, out Color color) => colors.TryGetValue(name, out color);

            public bool TryGetMainTexture(string[] candidates, out Texture texture, out Vector2 scale, out Vector2 offset)
            {
                foreach (string name in candidates)
                {
                    if (textures.TryGetValue(name, out var entry) && entry.tex != null)
                    {
                        (texture, scale, offset) = entry;
                        return true;
                    }
                }
                texture = null;
                scale = Vector2.one;
                offset = Vector2.zero;
                return false;
            }
        }

        static string[] ReadStringArray(SerializedProperty property)
        {
            if (property == null || !property.isArray) return System.Array.Empty<string>();
            var values = new string[property.arraySize];
            for (int i = 0; i < values.Length; i++) values[i] = property.GetArrayElementAtIndex(i).stringValue;
            return values;
        }
    }
}
