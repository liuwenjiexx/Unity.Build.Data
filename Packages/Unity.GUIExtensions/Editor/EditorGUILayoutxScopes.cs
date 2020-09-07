using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.GUIExtensions
{
    public partial class EditorGUILayoutx
    {
        public class Scopes
        {

            public class FoldoutHeaderGroupScope : GUI.Scope
            {
                public FoldoutHeaderGroupScope(bool initValue, GUIContent label, Action onShow = null, Action onHide = null)
                {
                    var state = (FoldoutHeaderGroupState)GUIUtility.GetStateObject(typeof(FoldoutHeaderGroupState), GUIUtility.GetControlID(FocusType.Passive));
                    if (!state.initialized)
                    {
                        state.initialized = true;
                        state.value = initValue;
                        if (state.value)
                        {
                            onShow?.Invoke();
                        }
                    }
                    var oldValue = state.value;
                    state.value = EditorGUILayout.BeginFoldoutHeaderGroup(state.value, label);
                    Visiable = state.value;
                    if (oldValue != state.value)
                    {
                        if (state.value)
                            onShow?.Invoke();
                        else
                            onHide?.Invoke();
                    }
                    EditorGUILayout.EndFoldoutHeaderGroup();
                }

                public bool Visiable { get; private set; }
                protected override void CloseScope()
                {
                    //EditorGUILayout.EndFoldoutHeaderGroup();
                }
            }


            [Serializable]
            class FoldoutHeaderGroupState
            {
                public bool initialized;
                public bool value;
                public DateTime dateTime;
                public float time;
            }
            public class FadeGroupScope : GUI.Scope
            {
                public FadeGroupScope(float initValue)
                {
                    FadeGroupState state = (FadeGroupState)GUIUtility.GetStateObject(typeof(FadeGroupState), GUIUtility.GetControlID(FocusType.Passive));
                    if (!state.initialized)
                    {
                        state.initialized = true;
                        state.fade = initValue;

                    }
                    bool newVisiable = EditorGUILayout.BeginFadeGroup(state.fade);

                    if (Visiable != newVisiable)
                    {
                        Visiable = newVisiable;
                        state.dateTime = DateTime.Now;
                        state.time = 0f;
                    }
                    if (Event.current.type == EventType.Repaint)
                    {
                        float end = Visiable ? 1 : 0f;

                        if (state.aaa)
                        {
                            end = 1f;
                            if (state.fade >= 1f)
                                state.aaa = false;
                        }
                        else
                        {
                            end = 0f;
                            if (state.fade <= 0f)
                                state.aaa = true;
                        }
                        state.time += (float)(DateTime.Now - state.dateTime).TotalSeconds * 0.5f;
                        state.fade = Mathf.Lerp(state.fade, end, state.time);


                    }
                    GUILayout.Label(state.fade.ToString());
                }

                public bool Visiable
                {
                    get;
                    private set;
                }

                protected override void CloseScope()
                {
                    EditorGUILayout.EndFadeGroup();
                }

                [Serializable]
                class FadeGroupState
                {
                    public bool initialized;
                    public float fade;
                    public DateTime dateTime;
                    public float time;
                    public bool aaa;
                }
            }

            /// <summary>
            /// 继承<see cref="GUI.Scope"/>会报错：UnityEngine.Scope:Finalize()
            /// </summary>
            public class IndentLevelVerticalScope : IDisposable
            {
                private int originIndentLevel;
                private float originLabelWidth;
                private bool disposed;

                public IndentLevelVerticalScope(GUIStyle style = null, params GUILayoutOption[] options)
                    : this(EditorGUI.indentLevel + 1, style, options)
                {

                }

                public IndentLevelVerticalScope(int indentLevel, GUIStyle style = null, GUILayoutOption[] options = null)
                {
                    originIndentLevel = EditorGUI.indentLevel;
                    originLabelWidth = EditorGUIUtility.labelWidth;
                    EditorGUI.indentLevel = indentLevel;


                    EditorGUILayout.BeginHorizontal();
                    GUILayout.Space(EditorGUI.indentLevel * 16);
                    if (style == null)
                        EditorGUILayout.BeginVertical(options);
                    else
                        EditorGUILayout.BeginVertical(style, options);

                    EditorGUIUtility.labelWidth -= EditorGUI.indentLevel * 16;
                    EditorGUI.indentLevel = 0;
                }

                public void Dispose()
                {
                    CloseScope();
                }

                protected void CloseScope()
                {
                    if (disposed)
                    {
                        Debug.LogError("CloseScope");
                        return;
                    }
                    disposed = true;
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndHorizontal();
                    EditorGUI.indentLevel = originIndentLevel;
                    EditorGUIUtility.labelWidth = originLabelWidth;
                }

            }
        }
    }
}
