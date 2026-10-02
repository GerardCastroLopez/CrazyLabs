using UnityEditor;
using UnityEngine;

namespace gSDK.Editor
{
    public class ReactiveDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            SerializedProperty valueProp = property.FindPropertyRelative("_value");
            EditorGUI.PropertyField(position, valueProp, label);
        }
    }

    [CustomPropertyDrawer(typeof(rBool))]
    public class rBoolDrawer : ReactiveDrawer { }

    [CustomPropertyDrawer(typeof(rInt))]
    public class rIntDrawer : ReactiveDrawer { }

    [CustomPropertyDrawer(typeof(rUint))]
    public class rUintDrawer : ReactiveDrawer { }
    
    [CustomPropertyDrawer(typeof(rLong))]
    public class rLongDrawer : ReactiveDrawer { }

    [CustomPropertyDrawer(typeof(rFloat))]
    public class rFloatDrawer : ReactiveDrawer { }

    [CustomPropertyDrawer(typeof(rString))]
    public class rStringDrawer : PropertyDrawer
    {
        private Vector2 _scrollPosition;


        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var valueProp = property.FindPropertyRelative("_value");

            var textAreaAttr = fieldInfo.GetCustomAttributes(typeof(rTextAreaAttribute), true);
            if (textAreaAttr.Length > 0 && textAreaAttr[0] is rTextAreaAttribute area)
            {
                EditorGUILayout.LabelField(label);
                position.y += EditorGUIUtility.singleLineHeight;

                GUIStyle textAreaStyle = new(EditorStyles.textArea) {

                    wordWrap = true,
                };

                float textAreaHeightRequired = GUI.skin.textArea.CalcHeight(new GUIContent(valueProp.stringValue), position.width);
                float textAreaMaxHeight = area.maxLines * EditorGUIUtility.singleLineHeight;
                if (textAreaHeightRequired > textAreaMaxHeight)
                {
                    Rect scrollRect = new(position.x, position.y, position.width, textAreaMaxHeight);
                    _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(scrollRect.height));
                    valueProp.stringValue = EditorGUILayout.TextArea(valueProp.stringValue, textAreaStyle);
                    EditorGUILayout.EndScrollView();
                }
                else
                {
                    textAreaStyle.fixedHeight = textAreaMaxHeight;
                    valueProp.stringValue = EditorGUILayout.TextArea(valueProp.stringValue, textAreaStyle);
                }
                position.y += textAreaMaxHeight;
            }
            else
            {
                EditorGUILayout.PropertyField(valueProp, label);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 0f;
        }
    }

    [CustomPropertyDrawer(typeof(rVector2))]
    public class rVector2Drawer : ReactiveDrawer { }
    
    [CustomPropertyDrawer(typeof(rSprite))]
    public class rSpriteDrawer : ReactiveDrawer { }

    [CustomPropertyDrawer(typeof(rRangeAttribute))]
    public class rRangeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType != SerializedPropertyType.Generic)
            {
                EditorGUI.LabelField(position, label.text, "Use [rRange] with compatible Reactive variables.");
                return;
            }

            var valueProp = property.FindPropertyRelative("_value");
            var attr = (rRangeAttribute)attribute;

            valueProp.floatValue = EditorGUI.Slider(position, label, valueProp.floatValue, attr.Min, attr.Max);
        }
    }
}