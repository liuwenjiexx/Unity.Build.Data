using System;
using System.Collections;
using System.Collections.Generic;
using System.Configure;
using UnityEngine;
namespace UnityEngine.GUIExtensions
{
    [CustomGUIPropertyDrawer(typeof(Vector3))]
    class Vector3Drawer : GUIPropertyDrawer
    {
        public override void OnGUILayout(IGUIProperty property, Attribute attribute)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayoutx.PrefixLabel(property.DisplayName);
                Vector3 value = (Vector3)property.Value;
                value = GUILayoutx.Vector3Field(value);
                property.SetValue(value);
            }
        }
    }

}