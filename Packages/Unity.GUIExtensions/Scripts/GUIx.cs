using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;

namespace UnityEngine.GUIExtensions
{
    public partial class GUIx
    {
        public const int EditableLabelClickCount = 1;

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
#if UNITY_EDITOR
            if (Application.isEditor)
            {
                if (getLastControlIdField == null)
                    getLastControlIdField = typeof(UnityEditor.EditorGUIUtility).GetField("s_LastControlID", BindingFlags.Static | BindingFlags.NonPublic);
                if (getLastControlIdField != null)
                    return (int)getLastControlIdField.GetValue(null);
            }
#endif
            return 0;
        }

        public static string DelayedTextField(Rect rect, string value, GUIStyle style = null, Func<string, string> textField = null)
        {
            string current;
            return DelayedTextField(rect, value, out current, style: style, textField: textField);
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="value"></param>
        /// <param name="current">PlaceholderField 使用到</param>
        /// <param name="style"></param>
        /// <param name="textField">EditorGUI.TextField 输入没有延迟</param>
        /// <returns></returns>
        public static string DelayedTextField(Rect rect, string value, out string current, GUIStyle style = null, Func<string, string> textField = null, Action<bool> onEnd = null)
        {
            var evt = Event.current;
            int ctrlId = GUIUtility.GetControlID(FocusType.Keyboard, rect);
            var state = (DelayedTextFieldState)GUIUtility.GetStateObject(typeof(DelayedTextFieldState), ctrlId);

            if (style == null)
                style = "textfield";

            Action<bool> submit = (b) =>
            {
                state.isEditing = false;
                if (GUIUtility.keyboardControl == state.inputControlId)
                    GUIUtility.keyboardControl = 0;
                if (b)
                {
                    if (!string.Equals(value, state.value))
                    {
                        value = state.value;
                        GUI.changed = true;
                    }
                }
                onEnd?.Invoke(b);
            };
            if (state.isEditing)
            {

                if (evt.type == EventType.KeyDown)
                {

                    if (evt.keyCode == KeyCode.Escape)
                    {
                        evt.Use();
                        submit(false);
                    }
                    else if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                    {
                        evt.Use();
                        submit(true);
                    }
                }
                else if (evt.type == EventType.MouseDown)
                {
                    if (!rect.Contains(evt.mousePosition))
                    {
                        evt.Use();
                        submit(true);
                    }
                }

                if (state.isEditing && GUIUtility.keyboardControl != state.inputControlId)
                {
                    submit(true);
                }
            }


            if (!state.isEditing)
            {
                state.value = value;
            }
            bool changed = GUI.changed;
            if (textField != null)
                state.value = textField(state.value);
            else
                state.value = GUI.TextField(rect, state.value, style);
            //GUILayout.Label(ctrlId + ", " + GetLastControlId() + "");
            if (!state.isEditing && GUIUtility.keyboardControl > 0)
            {
                int textCtrlId = GetLastControlId();
                if (( textCtrlId != 0 && GUIUtility.keyboardControl == textCtrlId) || (GUIUtility.keyboardControl == ctrlId + 1 || GUIUtility.keyboardControl == ctrlId + 2))
                {
                    state.isEditing = true;
                    state.value = value;
                    state.inputControlId = GUIUtility.keyboardControl;
                }
            }

            GUI.changed = changed;
            current = state.value;
            return value;
        }



        class DelayedTextFieldState
        {
            public int inputControlId;
            public string value;
            public bool isEditing;
        }

        public static string DelayedPlaceholderField(Rect rect, string text, GUIContent placeholder, GUIStyle textStyle = null, GUIStyle placeholderStyle = null, Func<string, string> textField = null)
        {
            string current;
            return DelayedPlaceholderField(rect, text, out current, placeholder, textStyle: textStyle, placeholderStyle: placeholderStyle, textField: textField);
        }

        public static string DelayedPlaceholderField(Rect rect, string text, out string current, GUIContent placeholder, GUIStyle textStyle = null, GUIStyle placeholderStyle = null, Func<string, string> textField = null)
        {
            if (textStyle == null)
                textStyle = "textfield";

            bool isEmpty = false;

            text = DelayedTextField(rect, text, out current, textStyle, textField: textField);
            isEmpty = string.IsNullOrEmpty(current);

            if (isEmpty)
            {
                if (Event.current.type == EventType.Repaint)
                {
                    if (placeholderStyle == null)
                        placeholderStyle = Styles.Placeholder;
                    placeholderStyle.Draw(rect, placeholder, false, false, false, false);
                }
            }
            return text;
        }

        public static string DelayedEditableLabel(Rect rect, string text, GUIStyle labelStyle = null, GUIStyle textStyle = null, int clickCount = EditableLabelClickCount, Func<string, string> textField = null, Action onStart = null, Action<bool> onEnd = null)
        {
            int ctrlId = GUIUtility.GetControlID(FocusType.Passive, rect);
            var state = (EditableLabelState)GUIUtility.GetStateObject(typeof(EditableLabelState), ctrlId);
            var evt = Event.current;

            if (state.isEditing)
            {
                if (textStyle == null)
                    textStyle = "textfield";
                string current;

                text = DelayedTextField(rect, text, out current, textStyle, textField: textField, onEnd: (b) =>
                        {
                            state.isEditing = false;
                            onEnd?.Invoke(b);
                        });
                if (state.first)
                {
                    state.first = false;
                    GUIUtility.keyboardControl = GetLastControlId();
                    onStart?.Invoke();
                }
            }
            else
            {
                if (labelStyle == null)
                    labelStyle = "label";
                GUI.Label(rect, text, labelStyle);


                if (rect.Contains(evt.mousePosition))
                {
                    if (evt.clickCount == clickCount)
                    {
                        state.value = text;
                        state.isEditing = true;
                        state.first = true;
                        evt.Use();
                    }
                }
            }

            //GUILayout.Label("" + ctrlId);
            return text;
        }

        class EditableLabelState
        {
            public bool isEditing;
            public string value;
            public bool first;
        }

    }
}
