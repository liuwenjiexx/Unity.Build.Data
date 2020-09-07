using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Localizations
{
    public static class Extensions
    {

        public static string Localization(this string key)
        {
            return Localizations.Localization.GetString(key);
        }

        public static T Localization<T>(this string key)
        {
            return (T)Localizations.Localization.GetItem(key).Value;
        }
    }

}