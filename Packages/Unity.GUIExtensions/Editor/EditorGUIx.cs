using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.GUIExtensions;

namespace UnityEditor.GUIExtensions
{
    public partial class EditorGUIx
    {

        public static GUIContent MenuLabel = new GUIContent("◥");
        public static GUIContent PositiveLabel = new GUIContent("✚");
        public static GUIContent NegativeLabel = new GUIContent("▬");
        /// <summary>
        /// 省略号符号
        /// </summary>
        public static GUIContent EllipsisLabel = new GUIContent("…");

        internal static Version UnityVersion_201903 = new Version(2019, 3);

        private static Version unityVersion;
        static FieldInfo getLastControlIdField;

        internal static Version UnityVersion
        {
            get
            {
                if (unityVersion == null)
                {
                    string[] parts = Application.unityVersion.Split('.');
                    if (parts.Length == 3)
                        parts = parts.Take(2).ToArray();
                    unityVersion = new Version(string.Join(".", parts));
                }
                return unityVersion;
            }
        }
        public static int GetLastControlId()
        {
            if (getLastControlIdField == null)
                getLastControlIdField = typeof(EditorGUIUtility).GetField("s_LastControlID", BindingFlags.Static | BindingFlags.NonPublic);
            if (getLastControlIdField != null)
                return (int)getLastControlIdField.GetValue(null);
            return 0;
        }
        //public static FieldInfo LastControlIdField = typeof(EditorGUI).GetField("lastControlID", BindingFlags.Static | BindingFlags.NonPublic);
        //public static int GetLastControlId()
        //{
        //    if (LastControlIdField == null)
        //    {
        //        Debug.LogError("Compatibility with Unity broke: can't find lastControlId field in EditorGUI");
        //        return 0;
        //    }
        //    return (int)LastControlIdField.GetValue(null);
        //}

        public static string DelayedPlaceholderField(Rect rect, string text, GUIContent placeholder, GUIStyle textStyle = null, GUIStyle placeholderStyle = null)
        {
            string current;
            return DelayedPlaceholderField(rect, text, out current, placeholder, textStyle: textStyle, placeholderStyle: placeholderStyle);
        }

        public static string DelayedPlaceholderField(Rect rect, string text, out string current, GUIContent placeholder, GUIStyle textStyle = null, GUIStyle placeholderStyle = null)
        {
            return GUIx.DelayedPlaceholderField(rect, text, out current, placeholder, textStyle: textStyle, placeholderStyle: placeholderStyle,
                   textField: o => EditorGUI.TextField(rect, o)) ;;
        }




        public static bool ToggleLabel(Rect rect, GUIContent label, bool value)
        {
            GUIStyle style = Styles.ToggleLabel;

            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
            {
                value = !value;
            }
            if (Event.current.type == EventType.Repaint)
            {
                var oldColor = GUI.backgroundColor;
                if (value)
                    GUI.backgroundColor *= new Color(0.9f, 0.6f, 1f, 1f);
                style.Draw(rect, label, false, false, value, false);
                GUI.backgroundColor = oldColor;
            }

            return value;
        }

        public static string DelayedEditableLabel(Rect rect, string text, GUIStyle labelStyle = null, GUIStyle textStyle = null, int clickCount = GUIx.EditableLabelClickCount)
        {
            if (labelStyle == null)
                labelStyle = "label";
            if (textStyle == null)
                textStyle = "textfield";
            //DelayedTextField: 切换焦点时值错误
            return GUIx.DelayedEditableLabel(rect, text, clickCount: clickCount, labelStyle: labelStyle, textStyle: textStyle,
                textField: (o) => EditorGUI.TextField(rect, o, textStyle), onStart: () =>
                {
                    //EditorGUIUtility.editingTextField = false; 
                    EditorGUIUtility.editingTextField = true;
                }, onEnd: (b) => EditorGUIUtility.editingTextField = false);
            ;
        }



        public static string FileField(Rect rect, string file, string extension, string title, GUIStyle style = null, string relativePath = null)
        {
            if (style == null)
                style = "textfield";
            float ellipsisLabelWidth = Styles.Ellipsis.CalcSize(EllipsisLabel).x;
            file = EditorGUI.DelayedTextField(new Rect(rect.x, rect.y, rect.width - ellipsisLabelWidth, rect.height), file ?? string.Empty, style);
            if (GUI.Button(new Rect(rect.xMax - ellipsisLabelWidth, rect.y, ellipsisLabelWidth, rect.height), EllipsisLabel, Styles.Ellipsis))
            {
                string newPath = EditorUtility.OpenFilePanel(title, file, extension);
                if (!string.IsNullOrEmpty(newPath))
                {
                    string result;
                    if (!string.IsNullOrEmpty(relativePath) && newPath.ToRelativePath(relativePath, out result))
                    {
                        newPath = result;
                    }

                    if (!string.Equals(file, newPath, StringComparison.InvariantCultureIgnoreCase))
                    {
                        file = newPath;
                        GUIUtility.keyboardControl = -1;
                        GUI.changed = true;
                    }
                }
            }

            return file;
        }

        public static string FolderField(Rect rect, string folder, string title, string relativePath = null, GUIStyle style = null, params GUILayoutOption[] options)
        {
            if (style == null)
                style = "textfield";
            float ellipsisLabelWidth = Styles.Ellipsis.CalcSize(EllipsisLabel).x;
            folder = EditorGUI.TextField(new Rect(rect.x, rect.y, rect.width - ellipsisLabelWidth, rect.height), folder ?? string.Empty, style);
            if (GUI.Button(new Rect(rect.xMax - ellipsisLabelWidth, rect.y, ellipsisLabelWidth, rect.height), EllipsisLabel, Styles.Ellipsis))
            {
                string newPath = EditorUtility.OpenFolderPanel(title, folder, "");
                if (!string.IsNullOrEmpty(newPath))
                {
                    string result;
                    if (!string.IsNullOrEmpty(relativePath) && newPath.ToRelativePath(relativePath, out result))
                    {
                        newPath = result;
                    }

                    if (!string.Equals(folder, newPath, StringComparison.InvariantCultureIgnoreCase))
                    {
                        folder = newPath;
                        GUIUtility.keyboardControl = -1;
                        GUI.changed = true;
                    }
                }
            }

            return folder;
        }



        public static string SearchTextField(Rect rect, string text, GUIContent placeholder)
        {
            GUIStyle searchTextFieldStyle;
            GUIStyle searchCancelButtonStyle;
            GUIStyle searchCancelButtonEmptyStyle;

            searchTextFieldStyle = "SearchTextField";
            searchCancelButtonStyle = "SearchCancelButton";
            searchCancelButtonEmptyStyle = "SearchCancelButtonEmpty";



            bool isEmpty = string.IsNullOrEmpty(text);
            GUIStyle cancelButtonStyle = !isEmpty ? searchCancelButtonStyle : searchCancelButtonEmptyStyle;

            float cancelButtonWidth = 0f;
            if (UnityVersion >= UnityVersion_201903)
            {
            }
            else
            {
                cancelButtonWidth = cancelButtonStyle.fixedWidth;
            }

            Rect cancelButtonRect = new Rect(rect.xMax - cancelButtonStyle.fixedWidth, rect.y, cancelButtonStyle.fixedWidth, cancelButtonStyle.fixedHeight);
            if (GUI.Button(cancelButtonRect, GUIContent.none, GUIStyle.none) && !isEmpty)
            {
                text = string.Empty;
                GUI.changed = true;
                GUIUtility.keyboardControl = 0;
            }
            if (Event.current.type == EventType.MouseMove)
            {
                if (cancelButtonRect.Contains(Event.current.mousePosition))
                {
                }
            }

            text = EditorGUI.TextField(new Rect(rect.x, rect.y, rect.width - cancelButtonWidth, rect.height), text, searchTextFieldStyle);

            if (Event.current.type == EventType.Repaint)
            {
                cancelButtonStyle.Draw(cancelButtonRect, GUIContent.none, true, false, false, false);
                if (isEmpty)
                {
                    GUIStyle placeholderStyle = new GUIStyle("label");
                    placeholderStyle.normal.textColor = Color.grey;
                    placeholderStyle.fontSize = (int)searchTextFieldStyle.lineHeight - 2;
                    placeholderStyle.clipping = TextClipping.Overflow;
                    placeholderStyle.padding = new RectOffset();
                    placeholderStyle.margin = new RectOffset();
                    if (UnityVersion >= UnityVersion_201903)
                    {
                        placeholderStyle.padding.top = searchTextFieldStyle.margin.top + searchTextFieldStyle.padding.top;
                    }
                    else
                    {
                        placeholderStyle.padding.top = searchTextFieldStyle.padding.top;
                    }
                    placeholderStyle.Draw(new Rect(rect.x + searchTextFieldStyle.padding.left, rect.y, rect.width - searchTextFieldStyle.padding.horizontal, rect.height), placeholder, false, false, false, false);
                }
            }



            return text;
        }


    }

}