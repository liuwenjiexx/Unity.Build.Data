using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Localizations
{
    public interface ILocalizationValueDrawer
    {
        string TypeName { get; }
        object OnGUI( object value);
    }
}