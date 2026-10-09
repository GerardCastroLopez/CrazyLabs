using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Utils
{
    public interface IScriptableObjectIds
    {
        string[] GetIds();
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class StringOptionsAttribute : PropertyAttribute
    {
        public string[] StringOptions;
        public bool SortAlphabetically;
        

        public StringOptionsAttribute(bool sortAlphabetically, params string[] options)
        {
            SortAlphabetically = sortAlphabetically;
            StringOptions = options;
        }
        
        /// <summary>
        /// The asset at the given path must be a ScriptableObject implementing the interface IScriptableObjectIds.
        /// </summary>
        /// <param name="scriptableObjectPath">Must start with "Assets" and finish with ".asset"</param>
        public StringOptionsAttribute(string scriptableObjectPath, bool sortAlphabetically)
        {
#if UNITY_EDITOR
            var asset = (IScriptableObjectIds)AssetDatabase.LoadAssetAtPath(scriptableObjectPath, typeof(IScriptableObjectIds)); 
            if(asset != null)
            {
                StringOptions = asset.GetIds();
                SortAlphabetically = sortAlphabetically;
            }
            else
            {
                Debug.LogError($"There's no addressable scriptableObject with path {scriptableObjectPath}!");
            }
#else
            SortAlphabetically = sortAlphabetically;
            StringOptions = Array.Empty<string>();
#endif
        }

        public StringOptionsAttribute(bool sortAlphabetically, params Type[] staticClassTypes)
        {
            var opts = new HashSet<string>();

            foreach (var type in staticClassTypes)
            {
                if (type.IsEnum)
                {
                    opts.UnionWith(Enum.GetNames(type));
                }
                else
                {
                    opts.UnionWith(type
                        .GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.FlattenHierarchy)
                        .Where(fi => fi.IsLiteral && !fi.IsInitOnly && fi.FieldType == typeof(string))
                        .Select(x => (string)x.GetRawConstantValue()));
                }
            }
            StringOptions = opts.ToArray(); 
            SortAlphabetically = sortAlphabetically;
        }
    }
}