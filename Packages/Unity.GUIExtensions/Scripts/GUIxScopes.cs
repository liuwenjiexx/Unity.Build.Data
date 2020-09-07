using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace UnityEngine.GUIExtensions
{
    public partial class GUIx
    {
        public class Scopes
        {


            public abstract class ValueScope<T> : GUI.Scope
            {
                private T originValue;
                public ValueScope(T value)
                {
                    originValue = Value;
                    Value = value;
                }
                protected abstract T Value { get; set; }

                protected override void CloseScope()
                {
                    Value = originValue;
                }
            }

            public class ColorScope : ValueScope<Color>
            {
                public ColorScope(Color value)
                    : base(value)
                { }

                protected override Color Value { get => GUI.color; set => GUI.color = value; }
            }
            public class ContentColorScope : ValueScope<Color>
            {
                public ContentColorScope(Color value)
                    : base(value)
                { }

                protected override Color Value { get => GUI.contentColor; set => GUI.contentColor = value; }
            }
            public class BackgroundColorScope : ValueScope<Color>
            {
                public BackgroundColorScope(Color value)
                    : base(value)
                { }

                protected override Color Value { get => GUI.backgroundColor; set => GUI.backgroundColor = value; }
            }

            /// <summary>
            /// 恢复之前的 changed
            /// </summary>
            public class ChangedScope : GUI.Scope
            {
                private bool oldChanged;
                private bool closed;
                public ChangedScope()
                {
                    oldChanged = GUI.changed;
                    GUI.changed = false;
                }

                /// <summary>
                /// CloseScope
                /// </summary>
                public bool changed
                {
                    get
                    {
                        CloseScope();
                        return GUI.changed;
                    }
                }

                protected override void CloseScope()
                {
                    if (closed)
                        return;
                    closed = true;
                    GUI.changed = oldChanged;
                }

            }


        }
    }
}
