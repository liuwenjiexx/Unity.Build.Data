using System;
using System.Collections;
using System.Collections.Generic;
using System.Configure;
using System.Linq;
using UnityEngine;


namespace UnityEngine.GUIExtensions
{
    [CustomGUIPropertyDrawer(typeof(EnumValuesAttribute))]
    class EnumValuesDrawer : GUIPropertyDrawer
    {
        public override void OnGUILayout(IGUIProperty property, Attribute attribute)
        {
            EnumValuesAttribute attr = attribute as EnumValuesAttribute;

            object value = property.Value;

            object[] values = attr.Values;
            string[] displayTexts = attr.DisplayTexts;

            using (new GUILayout.HorizontalScope())
            {
                GUILayoutx.PrefixLabel(new GUIContent(property.DisplayName, property.Tooltip));

                int selectedIndex2 = -1;
                GUIContent[] displays = new GUIContent[values.Length];
                for (int i = 0; i < values.Length; i++)
                {
                    displays[i] = new GUIContent(displayTexts != null && displayTexts.Length > 0 ? displayTexts[i] : values[i].ToString());

                    if (object.Equals(values[i], property.Value))
                    {
                        selectedIndex2 = i;
                    }
                }

                if (attr.IsMask)
                {
                    int maskValue = 0;
                    string[] maskValues = ((string)value).Split(new string[] { attr.Separator }, StringSplitOptions.RemoveEmptyEntries);
                    int[] intValues = new int[values.Length];
                    for (int i = 0; i < values.Length; i++)
                    {
                        intValues[i] = 1 << i;
                        if (maskValues.Contains(values[i]))
                        {
                            maskValue |= intValues[i];
                        }
                    }
                    int newMask = GUILayoutx.Mask(maskValue, intValues, displays);
                    if (newMask != maskValue)
                    {
                        List<string> list = new List<string>();
                        for (int i = 0; i < values.Length; i++)
                        {
                            if ((intValues[i] & newMask) == intValues[i])
                            {
                                list.Add((string)values[i]);
                            }
                        }
                        value = string.Join(attr.Separator, list);
                    }
                }
                else
                {
                    int newIndex = GUILayoutx.Popup(selectedIndex2, displays);
                    if (newIndex != selectedIndex2)
                    {
                        value = values[newIndex];
                    }
                }
            }

            property.SetValue(value);

        }
    }

}