using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace UnityEngine.Localizations
{
    class StringLocalizationValue : ILocalizationValueProvider
    {
        public string TypeName => "string";

        public object DefaultValue
        {
            get => null;
        }

        public string Serialize(object value)
        {
            return value as string;
        }

        public object Deserialize(string text)
        {
            return text;
        }
         
    }
}