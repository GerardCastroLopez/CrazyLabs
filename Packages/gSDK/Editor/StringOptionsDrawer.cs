using System;
using UnityEditor;
using UnityEngine;
using Utils;

namespace gSDK.Editor
{
    [CustomPropertyDrawer(typeof(StringOptionsAttribute), true)]
    public class StringOptionsDrawer : PropertyDrawer
    {
        private bool _init = false;
        private int _index;
        private string[] _choicesArray;


        private void Init(SerializedProperty property)
        {
            if(_init)
            {
                return;
            }

            _init = true;

            if(property.propertyType != SerializedPropertyType.String)
            {
                Debug.LogError($"Can't use a StaticOptions attribute on non-string properties!");
            }

            var attr = attribute as StringOptionsAttribute;
            _choicesArray = attr!.StringOptions;

            if(_choicesArray == null || _choicesArray.Length == 0)
            {
                Debug.LogError($"StringOptionsAttribute with no options!");
            }
            else if(attr.SortAlphabetically)
            {
                Array.Sort(_choicesArray);
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Init(property);

            if(_choicesArray != null && _choicesArray.Length > 0)
            {
                _index = Array.IndexOf(_choicesArray, property.stringValue);

                if(_index < 0)
                {
                    _index = 0;

                    if(!string.IsNullOrEmpty(property.stringValue))
                    {
                        Debug.LogWarning($"Previously set value {property.stringValue} is no longer present among the options in {property.displayName}!", property.serializedObject.targetObject);
                    }

                    UpdateValue(property);
                }

                int newIndex = EditorGUI.Popup(position, property.displayName, _index, _choicesArray);
                
                if(_index != newIndex)
                {
                    _index = newIndex;

                    UpdateValue(property);
                }
            }
        }

        private void UpdateValue(SerializedProperty property)
        {
            property.stringValue = _choicesArray[_index];
            property.serializedObject.ApplyModifiedProperties();
        }
    }
}