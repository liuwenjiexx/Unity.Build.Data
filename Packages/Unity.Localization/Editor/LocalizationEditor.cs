using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System.Reflection;
using System;
using UnityEngine.Localizations;

namespace UnityEditor.Localizations
{
    using Localization = UnityEngine.Localizations.Localization;

    [CanEditMultipleObjects]
    [CustomEditor(typeof(Localization))]
    class LocalizationEditor : Editor
    {

        SerializedProperty keyProperty;
        SerializedProperty formatPropery;

        private void OnEnable()
        {
            keyProperty = serializedObject.FindProperty("key");
            formatPropery = serializedObject.FindProperty("format");
            Localization.LoadLang(Localization.Lang);
        }

        [InitializeOnLoadMethod]
        static void InitializeOnLoadMethod()
        {
            UnityEngine.Localizations.Localization.LoadLang(Localization.Lang);
        }


        public override void OnInspectorGUI()
        {



            int selectedIndex = -1;
            for (int i = 0; i < Localization.LangNames.Count; i++)
            {
                if (Localization.LangNames[i] == Localization.SelectedLang)
                {
                    selectedIndex = i;
                    break;
                }
            }

            using (new GUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(new GUIContent(Localization.Lang ?? string.Empty, $"({ Localization.Current.GetType().Name})"));
                int newIndex = selectedIndex;
                if (GUILayout.Button("<", GUILayout.ExpandWidth(false)))
                {
                    newIndex = (newIndex - 1 + Localization.LangNames.Count) % Localization.LangNames.Count;
                }

                newIndex = EditorGUILayout.Popup(newIndex + 1, new GUIContent[] { new GUIContent("None".Localization()) }.Concat(Localization.LangNames.Select(o => new GUIContent(o))).ToArray());

                newIndex--;


                if (GUILayout.Button(">", GUILayout.ExpandWidth(false)))
                {
                    newIndex = (newIndex + 1) % Localization.LangNames.Count;
                }

                if (selectedIndex != newIndex)
                {
                    if (newIndex < 0)
                        Localization.SelectedLang = null;
                    else
                        Localization.SelectedLang = Localization.LangNames[newIndex];
                    Localization.LoadLang(Localization.Lang);
                }

            }

            string key = keyProperty.stringValue;
            bool hasKey = false;
            if (!string.IsNullOrEmpty(key) && Localization.HasItem(key))
            {
                hasKey = true;
            }
            var editorLocalization = EditorLocalization.EditorLocalizationValues;
            using (editorLocalization.BeginScope())
            {
                using (var checker = new EditorGUI.ChangeCheckScope())
                {

                    using (new GUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PrefixLabel(editorLocalization.GetString("Key"));
                        keyProperty.stringValue = EditorGUILayout.DelayedTextField(keyProperty.stringValue);
                    }

                    if (!hasKey)
                    {
                        EditorGUILayout.HelpBox(string.Format(editorLocalization.GetString("MissingKeyError"), key), MessageType.Error);
                    }

                    using (new GUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PrefixLabel(editorLocalization.GetString( "Format"));
                        formatPropery.stringValue = EditorGUILayout.TextArea(formatPropery.stringValue, GUILayout.Height(30));

                    }
                    if (checker.changed)
                    {
                        serializedObject.ApplyModifiedProperties();
                        foreach (var target in targets.Select(o => (Localization)o))
                        {
                            //Type type = target.GetType();
                            //var method = type.GetMethod("OnLanguageChanged", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                            //if (method != null && method.GetParameters().Length == 1)
                            //    method.Invoke(target, new string[] { Localization.Lang });
                            target.OnChanged();
                        }
                    }
                }

                serializedObject.ApplyModifiedProperties();
            }
        }


    }

}