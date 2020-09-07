using System;
using System.Collections;
using System.Collections.Generic;
using System.Configure;
using UnityEngine;


namespace UnityEngine.GUIExtensions
{
     

    [CustomGUIPropertyDrawer(typeof(UnityEngine.RangeAttribute))]
    class UnityRangeDrawer : GUIPropertyDrawer
    {
        public override void OnGUILayout(IGUIProperty property, Attribute attribute)
        {
            UnityEngine.RangeAttribute attr = attribute as UnityEngine.RangeAttribute;
            using (new GUILayout.HorizontalScope())
            {
                GUILayoutx.PrefixLabel(property.DisplayName);
                float value = (float)property.Value;
                value = GUILayout.HorizontalSlider(value, attr.min, attr.max);
                property.SetValue(value);
            }
        }
    }
}