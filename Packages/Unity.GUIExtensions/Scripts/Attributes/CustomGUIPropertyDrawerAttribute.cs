using System.Collections;
using System.Collections.Generic;
using System.Configure;
using UnityEngine;
using System.Linq;
using System;

namespace UnityEngine.GUIExtensions
{


    public class CustomGUIPropertyDrawerAttribute : Attribute
    {

        public CustomGUIPropertyDrawerAttribute(Type targetType)
        {
            TargetType = targetType;
        }

        public Type TargetType { get; set; }
        public int Priority { get; set; }
        public bool UseForChildren { get; set; }
    }





}