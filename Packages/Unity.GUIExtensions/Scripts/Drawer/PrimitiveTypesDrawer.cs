using System;
using System.Collections;
using System.Collections.Generic;
using System.Configure;
using UnityEngine;


namespace UnityEngine.GUIExtensions
{
    [CustomGUIPropertyDrawer(typeof(float))]
    class Float32Drawer : GUIPropertyDrawer
    {
        public override void OnGUILayout(IGUIProperty property, Attribute attribute)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayoutx.PrefixLabel(property);
                float value = (float)property.Value;
                value = GUILayoutx.DelayedFloat32Field(value);
                property.SetValue(value);
            }
        }
    }

    [CustomGUIPropertyDrawer(typeof(double))]
    class Float64Drawer : GUIPropertyDrawer
    {
        public override void OnGUILayout(IGUIProperty property, Attribute attribute)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayoutx.PrefixLabel(property);
                double value = (double)property.Value;
                value = GUILayoutx.DelayedFloat64Field(value);
                property.SetValue(value);
            }
        }
    }

    [CustomGUIPropertyDrawer(typeof(string))]
    class StringDrawer : GUIPropertyDrawer
    {
        public override void OnGUILayout(IGUIProperty property, Attribute attribute)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayoutx.PrefixLabel(property);
                string value = (string)property.Value;
                value = GUILayoutx.DelayedTextField(value);
                property.SetValue(value);
            }
        }
    }


    [CustomGUIPropertyDrawer(typeof(int))]
    class Int32Drawer : GUIPropertyDrawer
    {
        public override void OnGUILayout(IGUIProperty property, Attribute attribute)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayoutx.PrefixLabel(property);
                int value = (int)property.Value;
                value = GUILayoutx.DelayedInt32Field(value);
                property.SetValue(value);
            }
        }
    }

    [CustomGUIPropertyDrawer(typeof(bool))]
    class BoolDrawer : GUIPropertyDrawer
    {
        public override void OnGUILayout(IGUIProperty property, Attribute attribute)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayoutx.PrefixLabel(property);
                bool value = (bool)property.Value;
                value = GUILayout.Toggle(value, GUIContent.none);
                property.SetValue(value);
            }
        }
    }

    [CustomGUIPropertyDrawer(typeof(Enum))]
    class EnumDrawer : GUIPropertyDrawer
    {
        public override void OnGUILayout(IGUIProperty property, Attribute attribute)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayoutx.PrefixLabel(property);
                int value = (int)property.Value;

                Array arr = Enum.GetValues(property.ValueType);

                if (property.ValueType.IsDefined(typeof(FlagsAttribute), false))
                {
                    int[] intValues = new int[arr.Length];
                    GUIContent[] conents = new GUIContent[arr.Length];
                    int mask = 0;
                    for (int i = 0; i < arr.Length; i++)
                    {
                        int val = (int)arr.GetValue(i);
                        conents[i] = new GUIContent(arr.GetValue(i).ToString());
                        intValues[i] = val;
                        if ((value & val) == val)
                        {
                            mask |= val;
                        }
                    }
                    int newMask = GUILayoutx.Mask(mask, intValues, conents);
                    if (mask != newMask)
                    {
                        property.SetValue(newMask);
                    }
                }
                else
                {

                    GUIContent[] conents = new GUIContent[arr.Length];
                    int selectedIndex = -1;
                    for (int i = 0; i < arr.Length; i++)
                    {
                        conents[i] = new GUIContent(arr.GetValue(i).ToString());
                        if (object.Equals(arr.GetValue(i), property.Value))
                        {
                            selectedIndex = i;
                        }
                    }
                    int newIndex = GUILayoutx.Popup(selectedIndex, conents);
                    if (newIndex != selectedIndex && newIndex >= 0)
                    {
                        property.SetValue(arr.GetValue(newIndex));
                    }
                }


            }
        }
    }

}