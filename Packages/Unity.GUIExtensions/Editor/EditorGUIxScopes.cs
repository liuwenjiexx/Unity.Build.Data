using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.GUIExtensions;

namespace UnityEditor.GUIExtensions
{
    public partial class EditorGUIx
    {

        public class Scopes : GUIx.Scopes
        {
            public class IndentLevelScope : ValueScope<int>
            {
                public IndentLevelScope(int indentLevel)
                    : base(indentLevel)
                {
                }

                protected override int Value { get => EditorGUI.indentLevel; set => EditorGUI.indentLevel = value; }
            }
        }

    }
}
