using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace UnityEditor.Localizations
{

    public class StringLocalizationValueDrawer : ILocalizationValueDrawer
    {
        public string TypeName { get => "string"; }
        public object OnGUI(object value)
        {
            string text = value as string;
            text = EditorGUILayout.DelayedTextField(text ?? string.Empty);
            return text;
        }
    }

}