using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.GUIExtensions
{
    public partial class GUIx 
    {

        public class Styles
        {

            static GUIStyle placeholder;
            public static GUIStyle Placeholder
            {
                get
                {
                    if (placeholder == null)
                    {
                        placeholder = new GUIStyle("label");
                        placeholder.normal.textColor = Color.grey;
                        placeholder.fontSize -= 1;
                        placeholder.padding.left++;
                        placeholder.padding.top++;
                    }
                    return placeholder;
                }
            }

            static GUIStyle ellipsis;
            public static GUIStyle Ellipsis
            {
                get
                {
                    if (ellipsis == null)
                    {
                        ellipsis = new GUIStyle("label");
                    }
                    return ellipsis;
                }
            }

            static GUIStyle toggleLabel;
            public static GUIStyle ToggleLabel
            {
                get
                {
                    if (toggleLabel == null)
                    {
                        toggleLabel = new GUIStyle("button");
                        toggleLabel.onNormal.background = toggleLabel.normal.background;
                        toggleLabel.stretchWidth = false;
                        toggleLabel.wordWrap = false;

                    }
                    return toggleLabel;
                }
            }
        }

    }
}
