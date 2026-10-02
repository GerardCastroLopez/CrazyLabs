using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace CrazyLabs.EditorTools
{
    /// <summary>
    /// Many pack materials point at custom shaders that are not included. This rebuilds such a material
    /// with a built-in shader, reusing the texture and colour stored in the original .mat file.
    /// </summary>
    static class MaterialRepair
    {
        const string ErrorShader = "Hidden/InternalErrorShader";
        static readonly string[] TextureSlots = { "_MainTex", "_Diffuse", "_Base", "_BaseMap", "_Albedo" };
        static readonly Regex NeedsCutout = new Regex("glass|lash|hair|transp|alpha|leaf|leaves", RegexOptions.IgnoreCase);

        public static bool IsBroken(Material material) =>
            material == null || material.shader == null || material.shader.name == ErrorShader;

        /// <summary>True if the material has a main texture, including one stored in a material whose shader is missing.</summary>
        public static bool HasTexture(Material material)
        {
            if (material == null) return false;
            if (!IsBroken(material)) return material.HasProperty("_MainTex") && material.GetTexture("_MainTex") != null;

            ReadSavedProperties(material, out Texture texture, out _);
            return texture != null;
        }

        /// <summary>Returns <paramref name="source"/> when it works, otherwise a repaired copy saved in <paramref name="folder"/>.</summary>
        public static Material Repair(Material source, string folder)
        {
            if (!IsBroken(source)) return source;

            string label = source != null ? source.name : "Missing";
            string path = $"{folder}/Repaired_{Regex.Replace(label, @"[^\w\-]+", "_")}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            Texture texture = null;
            Color color = Color.white;
            if (source != null) ReadSavedProperties(source, out texture, out color);

            bool cutout = NeedsCutout.IsMatch(label);
            var repaired = new Material(Shader.Find(cutout ? "Legacy Shaders/Transparent/Cutout/Diffuse" : "Standard"))
            {
                name = $"Repaired_{label}",
                color = texture != null ? Color.white : (color.maxColorComponent > 0.01f ? color : new Color(0.7f, 0.7f, 0.75f)),
            };
            if (repaired.HasProperty("_Glossiness")) repaired.SetFloat("_Glossiness", 0.25f);
            if (texture != null) repaired.SetTexture("_MainTex", texture);

            AssetDatabase.CreateAsset(repaired, path);
            return repaired;
        }

        /// <summary>Reads textures/colours straight from the serialized data, which survives a missing shader.</summary>
        static void ReadSavedProperties(Material source, out Texture texture, out Color color)
        {
            texture = null;
            color = Color.white;
            var so = new SerializedObject(source);

            var texEnvs = so.FindProperty("m_SavedProperties.m_TexEnvs");
            foreach (string slot in TextureSlots)
            {
                for (int i = 0; texEnvs != null && i < texEnvs.arraySize; i++)
                {
                    var element = texEnvs.GetArrayElementAtIndex(i);
                    if (element.FindPropertyRelative("first").stringValue != slot) continue;
                    var tex = element.FindPropertyRelative("second.m_Texture").objectReferenceValue as Texture;
                    if (tex != null) { texture = tex; goto colors; }
                }
            }

            colors:
            var colorProps = so.FindProperty("m_SavedProperties.m_Colors");
            for (int i = 0; colorProps != null && i < colorProps.arraySize; i++)
            {
                var element = colorProps.GetArrayElementAtIndex(i);
                if (element.FindPropertyRelative("first").stringValue == "_Color")
                    color = element.FindPropertyRelative("second").colorValue;
            }
        }
    }
}
