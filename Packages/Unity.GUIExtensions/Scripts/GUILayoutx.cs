using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace UnityEngine.GUIExtensions
{

    public static class GUILayoutx
    {

        public static int PrefixLabelWidth = 160;
        public static Vector2 ScrollOffset;

        static GUIContent tooltipContent = new GUIContent();
        static Rect tooltipRect;
        static Rect tooltipSourceRect;
        static float tooltipAlpha;
        static float tooltipShowDelay = 0.1f;
        static float tooltipHideDelay;
        static int TooltipControlIdHint = new object().GetHashCode();
        static bool tooltipIsShow;
        static int tooltipLastControlId;
        static float cursorStayTime;
        static Vector2 lastCursorPos;
        private static GUISkin lightSkin;

        public static GUISkin LightSkin
        {
            get
            {
                if (!lightSkin)
                {
                    lightSkin = Resources.Load<GUISkin>("Unity.GUIExtensions/light");
                }
                return lightSkin;
            }
            set => lightSkin = value;
        }

        static void UpdateTooltipPosition()
        {
            Event evt = Event.current;
            GUIStyle tooltipStyle = "tooltip";
            Vector2 size = tooltipStyle.CalcSize(tooltipContent);
            tooltipRect.x = evt.mousePosition.x;
            tooltipRect.y = evt.mousePosition.y + 32;
            tooltipRect.width = size.x + 2;
            tooltipRect.height = size.y;
            tooltipRect = GUIUtility.GUIToScreenRect(tooltipRect);
        }

        public static bool Tooltip(Rect rect, GUIContent content)
        {
            Event evt = Event.current;
            bool isShow = false;
            if (!string.IsNullOrEmpty(content.tooltip))
            {
                int controlId = GUIUtility.GetControlID(TooltipControlIdHint, FocusType.Passive, rect);
                if (rect.Contains(evt.mousePosition))
                {
                    if (tooltipLastControlId != controlId)
                    {
                        GUIStyle tooltipStyle = "tooltip";
                        tooltipContent.text = content.tooltip;
                        tooltipSourceRect = GUIUtility.GUIToScreenRect(rect);
                        tooltipLastControlId = controlId;
                        tooltipAlpha = 0;
                        tooltipIsShow = false;
                    }
                    isShow = true;
                }

                if (tooltipLastControlId == controlId)
                {

                    if (evt.type == EventType.Repaint)
                    {
                        bool showing = tooltipAlpha > 0f && cursorStayTime > tooltipShowDelay;

                        if (evt.mousePosition == lastCursorPos)
                        {
                            cursorStayTime += Time.unscaledDeltaTime;
                        }
                        else
                        {
                            cursorStayTime = 0f;
                            lastCursorPos = evt.mousePosition;
                        }


                        Vector2 mousePos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
                        if (tooltipSourceRect.Contains(mousePos))
                        {
                            tooltipHideDelay = 1f;
                            if (!tooltipIsShow && cursorStayTime > tooltipShowDelay)
                            {
                                tooltipIsShow = true;
                                UpdateTooltipPosition();
                            }
                            if (tooltipIsShow && tooltipAlpha < 1f)
                            {
                                tooltipAlpha += Time.unscaledDeltaTime * 5f;
                                tooltipAlpha = Mathf.Clamp01(tooltipAlpha);
                            }
                        }
                        else
                        {
                            if (tooltipHideDelay > 0f)
                                tooltipHideDelay -= Time.unscaledDeltaTime;
                            if (tooltipHideDelay < 0f && tooltipAlpha > 0f)
                            {
                                tooltipAlpha -= Time.unscaledDeltaTime * 5f;
                                tooltipAlpha = Mathf.Clamp01(tooltipAlpha);
                                tooltipIsShow = false;
                            }
                        }
                    }


                    if (tooltipAlpha > 0f && tooltipContent != null && !string.IsNullOrEmpty(tooltipContent.text))
                    {
                        var oldMat = GUI.matrix;
                        GUI.matrix = Matrix4x4.identity;

                        int tooltipControlId = GUIUtility.GetControlID(tooltipContent.GetHashCode(), FocusType.Passive, tooltipRect);

                        GUI.Window(tooltipControlId, tooltipRect, (id) =>
                        {
                            GUIStyle tooltipStyle = "tooltip";
                            var oldColor = GUI.color;
                            var color = oldColor;
                            color.a = tooltipAlpha;
                            GUI.color = color;
                            GUI.Label(new Rect(0, 0, tooltipRect.width, tooltipRect.height), tooltipContent, tooltipStyle);
                            GUI.color = color;
                        }, GUIContent.none, GUIStyle.none);
                        GUI.matrix = oldMat;
                    }
                }

            }

            return isShow;
        }

        public static void Label(GUIContent content, params GUILayoutOption[] options)
        {
            GUIStyle style = "label";
            Rect rect = GUILayoutUtility.GetRect(content, style, options);
            //GUILayout.Label(content, options);
            GUI.Label(rect, content);
            Tooltip(rect, content);
        }


        public static void PrefixLabel(string text, params GUILayoutOption[] options)
        {
            PrefixLabel(new GUIContent(text), options);
        }
        public static void PrefixLabel(IGUIProperty property, params GUILayoutOption[] options)
        {
            PrefixLabel(new GUIContent(property.DisplayName, property.Tooltip), options);
        }
        public static void PrefixLabel(GUIContent content, params GUILayoutOption[] options)
        {
            float width = PrefixLabelWidth;
            //   if (Event.current.type != EventType.Layout)
            //{
            //float maxWidth = GUILayoutUtility.GetRect(width, Screen.width, 1, 1,GUILayout.ExpandWidth(true)).width;
            //Debug.Log(width + ", " + maxWidth + ", " + Event.current.type + "," + Event.current.rawType);
            //if (width < maxWidth * 0.4f)
            //    width = maxWidth * 0.4f;
            //    }

            using (new GUILayout.HorizontalScope(GUILayout.Width(width)/*, GUILayout.MaxWidth(width)*/))
            {
                GUILayout.Space(5 + (GUI.depth - 1) * 16);
                Label(content, options);
            }
        }



        //GC: 136B

        public static string DelayedTextField(string value, params GUILayoutOption[] options)
        {
            string current  ;
            return DelayedTextField(value, out current, null, options);
        }

        public static string DelayedTextField(string value, GUIStyle style, params GUILayoutOption[] options)
        {
            string current  ;
            return DelayedTextField(value, out current, style, options);
        }

        public static string DelayedTextField(string value, out string current, params GUILayoutOption[] options)
        {
            return DelayedTextField(value, out current, null, options);
        }

        public static string DelayedTextField(string value, out string current, GUIStyle style, params GUILayoutOption[] options)
        {
            if (style == null)
                style = "textfield";
            Rect rect = GUILayoutUtility.GetRect(new GUIContent(value), style, options);
            return GUIx.DelayedTextField(rect, value, out current, style);
        }


        public static int DelayedInt32Field(int value, params GUILayoutOption[] options)
        {
            string text;
            text = value.ToString();
            bool changed = GUI.changed;
            GUI.changed = false;
            text = DelayedTextField(text);
            if (GUI.changed)
            {
                int n;
                if (int.TryParse(text, out n) && n != value)
                {
                    value = n;
                    changed = true;
                }
            }

            if (changed)
                GUI.changed = true;
            return value;
        }
        public static long DelayedInt64Field(long value, params GUILayoutOption[] options)
        {
            string text;
            text = value.ToString();
            bool changed = GUI.changed;
            GUI.changed = false;
            text = DelayedTextField(text);
            if (GUI.changed)
            {
                long n;
                if (long.TryParse(text, out n) && n != value)
                {
                    value = n;
                    changed = true;
                }
            }
            if (changed)
                GUI.changed = true;
            return value;
        }
        //GC:164B
        public static float DelayedFloat32Field(float value, params GUILayoutOption[] options)
        {
            string text;
            bool changed = GUI.changed;

            text = value.ToString();
            //不使用 GUIChangeCheckScope 可减少 GC:18B
            GUI.changed = false;
            text = DelayedTextField(text);
            if (GUI.changed)
            {
                float f;
                if (float.TryParse(text, out f) && f != value)
                {
                    value = f;
                    changed = true;
                }
            }
            if (changed)
                GUI.changed = true;
            return value;
        }

        public static double DelayedFloat64Field(double value, params GUILayoutOption[] options)
        {
            string text;
            text = value.ToString();
            bool changed = GUI.changed;
            GUI.changed = false;
            text = DelayedTextField(text);
            if (GUI.changed)
            {
                double n;
                if (double.TryParse(text, out n) && n != value)
                {
                    value = n;
                    changed = true;
                }
            }

            if (changed)
                GUI.changed = true;
            return value;
        }

        public static Vector2 Vector2Field(Vector2 value, params GUILayoutOption[] options)
        {
            GUILayout.BeginHorizontal(options);
            {
                GUILayout.Label("X", GUILayout.ExpandWidth(false));
                value.x = DelayedFloat32Field(value.x);
                GUILayout.Label("Y", GUILayout.ExpandWidth(false));
                value.y = DelayedFloat32Field(value.y);
            }
            GUILayout.EndHorizontal();
            return value;
        }

        public static Vector2Int Vector2IntField(Vector2Int value, params GUILayoutOption[] options)
        {
            GUILayout.BeginHorizontal(options);
            {
                GUILayout.Label("X", GUILayout.ExpandWidth(false));
                value.x = DelayedInt32Field(value.x);
                GUILayout.Label("Y", GUILayout.ExpandWidth(false));
                value.y = DelayedInt32Field(value.y);
            }
            GUILayout.EndHorizontal();
            return value;
        }

        public static Vector3 Vector3Field(Vector3 value, params GUILayoutOption[] options)
        {
            GUILayout.BeginHorizontal(options);
            {
                GUILayout.Label("X", GUILayout.ExpandWidth(false));
                value.x = DelayedFloat32Field(value.x);
                GUILayout.Label("Y", GUILayout.ExpandWidth(false));
                value.y = DelayedFloat32Field(value.y);
                GUILayout.Label("Z", GUILayout.ExpandWidth(false));
                value.z = DelayedFloat32Field(value.z);
            }
            GUILayout.EndHorizontal();
            return value;
        }
        public static Vector3Int Vector3IntField(Vector3Int value, params GUILayoutOption[] options)
        {
            GUILayout.BeginHorizontal(options);
            {
                GUILayout.Label("X", GUILayout.ExpandWidth(false));
                value.x = DelayedInt32Field(value.x);
                GUILayout.Label("Y", GUILayout.ExpandWidth(false));
                value.y = DelayedInt32Field(value.y);
                GUILayout.Label("Z", GUILayout.ExpandWidth(false));
                value.z = DelayedInt32Field(value.z);
            }
            GUILayout.EndHorizontal();
            return value;
        }
        public static Vector4 Vector4Field(Vector4 value, params GUILayoutOption[] options)
        {
            GUILayout.BeginHorizontal(options);
            {
                GUILayout.Label("X", GUILayout.ExpandWidth(false));
                value.x = DelayedFloat32Field(value.x);
                GUILayout.Label("Y", GUILayout.ExpandWidth(false));
                value.y = DelayedFloat32Field(value.y);
                GUILayout.Label("Z", GUILayout.ExpandWidth(false));
                value.z = DelayedFloat32Field(value.z);
                GUILayout.Label("W", GUILayout.ExpandWidth(false));
                value.w = DelayedFloat32Field(value.w);
            }
            GUILayout.EndHorizontal();
            return value;
        }
        public static Rect RectField(Rect value, params GUILayoutOption[] options)
        {
            GUILayout.BeginHorizontal(options);
            {
                GUILayout.Label("X", GUILayout.ExpandWidth(false));
                value.x = DelayedFloat32Field(value.x);
                GUILayout.Label("Y", GUILayout.ExpandWidth(false));
                value.y = DelayedFloat32Field(value.y);
                GUILayout.Label("Width", GUILayout.ExpandWidth(false));
                value.width = DelayedFloat32Field(value.width);
                GUILayout.Label("Height", GUILayout.ExpandWidth(false));
                value.height = DelayedFloat32Field(value.height);
            }
            GUILayout.EndHorizontal();
            return value;
        }
        public static RectInt RectIntField(RectInt value, params GUILayoutOption[] options)
        {
            GUILayout.BeginHorizontal(options);
            {
                GUILayout.Label("X", GUILayout.ExpandWidth(false));
                value.x = DelayedInt32Field(value.x);
                GUILayout.Label("Y", GUILayout.ExpandWidth(false));
                value.y = DelayedInt32Field(value.y);
                GUILayout.Label("Width", GUILayout.ExpandWidth(false));
                value.width = DelayedInt32Field(value.width);
                GUILayout.Label("Height", GUILayout.ExpandWidth(false));
                value.height = DelayedInt32Field(value.height);
            }
            GUILayout.EndHorizontal();
            return value;
        }

        static object CreateDefaultValue(Type valueType)
        {

            object defaultValue;
            if (valueType.IsValueType)
            {
                defaultValue = Activator.CreateInstance(valueType);
            }
            else
            {
                defaultValue = null;
            }
            return defaultValue;
        }
        public static object ValueField(Type valueType, object value, params GUILayoutOption[] options)
        {
            TypeCode typeCode = Type.GetTypeCode(valueType);
            if ((value == null && valueType.IsValueType))
            {
                value = CreateDefaultValue(valueType);
                GUI.changed = true;
            }
            else if (!valueType.IsAssignableFrom(value.GetType()))
            {
                try
                {
                    value = Convert.ChangeType(value, valueType);
                }
                catch
                {
                    value = CreateDefaultValue(valueType);
                }
                GUI.changed = true;
            }

            switch (typeCode)
            {
                case TypeCode.String:
                    value = DelayedTextField((string)value, options);
                    break;
                case TypeCode.Int32:
                    value = DelayedInt32Field((int)value, options);
                    break;
                case TypeCode.Int64:
                    value = DelayedInt64Field((long)value, options);
                    break;
                case TypeCode.Single:
                    value = DelayedFloat32Field((float)value, options);
                    break;
                case TypeCode.Double:
                    value = DelayedFloat64Field((double)value, options);
                    break;
                case TypeCode.Boolean:
                    value = GUILayout.Toggle((bool)value, GUIContent.none, options);
                    break;

            }
            return value;
        }


        class PopupState
        {
            public int controlId;
            public bool showWindow;
            public Rect rect;
            public int selectedIndex;
            public bool hasValue;
            public Vector2 scrollPos;
            public static int activeId;

            public static GUIStyle itemStyle;
            public static GUIStyle itemStyle2;
            public int focused;
        }


        static void EnsureStyle()
        {
            if (PopupState.itemStyle == null)
            {
                GUIStyle itemStyle = new GUIStyle("label");
                var img = new Texture2D(2, 2);
                var clrs = img.GetPixels();
                for (int i = 0; i < clrs.Length; i++)
                    clrs[i] = new Color(0.4f, 0.4f, 0.4f, 1f);
                img.SetPixels(clrs);
                img.Apply();
                itemStyle.normal.background = img;
                itemStyle.margin = new RectOffset();
                itemStyle.padding = new RectOffset(5, 0, 3, 3);
                PopupState.itemStyle = itemStyle;

                itemStyle = new GUIStyle("label");
                img = new Texture2D(2, 2);
                clrs = img.GetPixels();
                for (int i = 0; i < clrs.Length; i++)
                    clrs[i] = new Color(0.3f, 0.3f, 0.3f, 1f);
                img.SetPixels(clrs);
                img.Apply();
                itemStyle.normal.background = img;
                itemStyle.margin = new RectOffset();
                itemStyle.padding = new RectOffset(5, 0, 3, 3);
                PopupState.itemStyle2 = itemStyle;
            }
        }

        public static int Popup(int selectedIndex, GUIContent[] displayedOptions, params GUILayoutOption[] options)
        {
            GUIContent current;
            bool[] selected = new bool[displayedOptions.Length];
            if (selectedIndex >= 0 && selectedIndex < displayedOptions.Length)
            {
                current = displayedOptions[selectedIndex];
                selected[selectedIndex] = true;
            }
            else
            {
                current = GUIContent.none;
            }

            int newIndex = Popup(current, selected, displayedOptions, options);
            if (newIndex != -1)
                selectedIndex = newIndex;
            return selectedIndex;
        }

        static int Popup(GUIContent content, bool[] selected, GUIContent[] displayedOptions, params GUILayoutOption[] options)
        {
            int controlId = GUIUtility.GetControlID(FocusType.Passive);
            var state = (PopupState)GUIUtility.GetStateObject(typeof(PopupState), controlId);
            state.controlId = controlId;

            var pos = GUILayoutUtility.GetRect(GUIContent.none, "button", options);

            int selectedIndex = -1;
            if (state.hasValue)
            {
                selectedIndex = state.selectedIndex;
                state.hasValue = false;
            }

            GUIStyle itemStyle = "menu_item";
            GUIStyle itemStyle2 = "menu_item_check";
            GUIStyle winStyle = "menu_box";

            if (itemStyle == null)
                itemStyle = PopupState.itemStyle;
            if (itemStyle2 == null)
                itemStyle2 = PopupState.itemStyle2;

            if (GUI.Button(pos, content))
            {

                state.showWindow = true;
                state.scrollPos = Vector2.zero;
                PopupState.activeId = controlId;

                EnsureStyle();

                float height = displayedOptions.Length * itemStyle.CalcHeight(GUIContent.none, 1);
                height += winStyle.padding.vertical;
                state.rect = new Rect(pos.xMin, pos.yMax, pos.width, height);


                var startPoint = GUIUtility.GUIToScreenPoint(GUI.matrix.inverse.MultiplyPoint(new Vector2(pos.xMin, pos.yMax)));

                state.rect.x = startPoint.x;
                state.rect.y = startPoint.y;

                if (state.rect.xMax > Screen.width)
                    state.rect.xMax = Screen.width;
                if (state.rect.yMax > Screen.height)
                    state.rect.yMax = Screen.height;

                if (state.rect.y / Screen.height > 0.5f && state.rect.height < height)
                {
                    startPoint = GUIUtility.GUIToScreenPoint(GUI.matrix.inverse.MultiplyPoint(new Vector2(pos.xMin, pos.yMin)));

                    state.rect.y = startPoint.y - height;
                    if (state.rect.y < 0)
                        state.rect.y = 0;
                    state.rect.height = height;
                    if (state.rect.yMax > startPoint.y)
                        state.rect.yMax = startPoint.y;
                }


                state.focused = 0;
                state.selectedIndex = -1;
                GUIUtility.hotControl = controlId;

            }

            if (PopupState.activeId == controlId)
            {
                if (Event.current.type == EventType.MouseDown)
                {
                    if (!state.rect.Contains(Event.current.mousePosition))
                    {
                        state.showWindow = false;
                        PopupState.activeId = 0;
                    }
                }

                GUILayout.Window(controlId, state.rect, (winId) =>
                {

                    using (var sv = new GUILayout.ScrollViewScope(state.scrollPos))
                    {
                        state.scrollPos = sv.scrollPosition;

                        //
                        //itemStyle.padding = new RectOffset();
                        for (int i = 0; i < displayedOptions.Length; i++)
                        {
                            var item = displayedOptions[i];
                            GUIStyle _itemStyle;
                            _itemStyle = itemStyle;

                            if (selected[i])
                            {
                                _itemStyle = itemStyle2;
                            }

                            if (GUILayout.Button(item, _itemStyle))
                            {
                                state.showWindow = false;
                                state.hasValue = true;
                                state.selectedIndex = i;
                                PopupState.activeId = 0;
                            }
                            GUI.color = Color.white;
                        }
                    }
                    if (Event.current.type == EventType.MouseDown)
                    {
                        if (!new Rect(0, 0, state.rect.width, state.rect.height).Contains(Event.current.mousePosition))
                        {
                            state.showWindow = false;
                            PopupState.activeId = 0;
                        }
                    }

                }, GUIContent.none, winStyle);
                if (state.focused < 5)
                {
                    state.focused++;
                    GUI.FocusWindow(state.controlId);
                }
            }

            return selectedIndex;
        }

        public static int Mask(int maskValue, int[] values, GUIContent[] displayedOptions, params GUILayoutOption[] options)
        {

            GUIContent content = null;
            int selectedOffset = 2;
            bool[] selected = new bool[values.Length + selectedOffset];
            bool isNothing = maskValue == 0, isEverything = maskValue != 0, isMixed = false;
            bool hasValue = false;
            int lastValue = 0;
            for (int i = 0; i < values.Length; i++)
            {

                if ((values[i] & maskValue) == values[i])
                {
                    selected[i + selectedOffset] = true;
                    if (content == null || lastValue == 0)
                    {
                        content = displayedOptions[i];
                        lastValue = values[i];
                    }
                    else
                    {
                        isMixed = true;
                    }

                }
                else
                {
                    if (values[i] != 0)
                        isEverything = false;
                }

                if (values[i] != 0)
                {
                    if (!hasValue)
                        hasValue = true;
                }
            }

            displayedOptions = new GUIContent[] { new GUIContent("Nothing"), new GUIContent("Everthing") }.Concat(displayedOptions).ToArray();
            if (!hasValue)
            {
                isNothing = true;
                isEverything = false;
                if (content == null)
                    content = new GUIContent("Nothing");
            }
            else
            {
                if (isEverything)
                {
                    content = new GUIContent("Everything");
                }
                else if (isMixed)
                {
                    content = new GUIContent("Mixed...");
                }
            }


            selected[0] = isNothing;
            selected[1] = isEverything;
            int newIndex = Popup(content, selected, displayedOptions, options);
            if (newIndex != -1)
            {
                if (newIndex < selectedOffset)
                {
                    if (newIndex == 0)
                    {
                        maskValue = 0;
                    }
                    else if (newIndex == 1)
                    {
                        maskValue = 0;
                        for (int i = 0; i < values.Length; i++)
                        {
                            maskValue |= values[i];
                        }
                    }
                }
                else
                {
                    newIndex -= selectedOffset;
                    if (selected[newIndex + selectedOffset])
                    {
                        maskValue &= ~values[newIndex];
                    }
                    else
                    {
                        maskValue |= values[newIndex];
                    }
                }

            }
            return maskValue;
        }



    }

    



}