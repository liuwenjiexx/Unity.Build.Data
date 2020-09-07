using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Localizations
{
    public struct LocalizationValue
    {
        public LocalizationValue(string typeName, object value)
        {
            this.Value = value;
            this.TypeName = typeName;
        }

        public LocalizationValue(string value)
        {
            this.TypeName = "string";
            this.Value = value;
        }

        public object Value { get; set; }

        public string TypeName { get; set; }

  

        public static Dictionary<string, LocalizationValue> StringDictionary(IDictionary<string, string> dic)
        {
            Dictionary<string, LocalizationValue> result = new Dictionary<string, LocalizationValue>();
            foreach (var item in dic)
            {
                result[item.Key] = new LocalizationValue(item.Value);
            }
            return result;
        }

        public LocalizationValue Clone()
        {
            return new LocalizationValue() { TypeName = TypeName, Value = Value };
        }

        public override string ToString()
        {
            if (Value == null)
                return string.Empty;
            return Value.ToString();
        }
    }




}