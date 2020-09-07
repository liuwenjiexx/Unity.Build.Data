using System.Collections;
using System.Collections.Generic;

namespace UnityEngine.Localizations
{
    public interface ILocalizationValueProvider
    {
        string TypeName { get; }

        object DefaultValue { get; }

        string Serialize(object value);

        object Deserialize(string text);
         
    }

}