using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.GUIExtensions
{
    public interface IGUIProperty
    {
        string DisplayName { get; set; }

        string Tooltip { get; set; }

        Type ValueType { get; }

        object Value { get; }


        bool SetValue(object value);

    }
}