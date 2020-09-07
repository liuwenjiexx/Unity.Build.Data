using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace UnityEngine.Localizations
{
    public class ResourcesLocalizationValues : LocalizationValues
    {
        public ResourcesLocalizationValues(string resourcesPath)
        {
            this.ResourcesPath = resourcesPath;
        }

        public string ResourcesPath { get; private set; }



        protected override IEnumerable<string> LoadNames()
        {

            foreach (var item in Resources.LoadAll<TextAsset>(ResourcesPath))
            {
                string name = item.name;
                if (name.EndsWith(".lang"))
                {
                    name = name.Substring(0, name.Length - 5);
                    yield return name;
                }
            }

        }

        protected override IDictionary<string, LocalizationValue> LoadValues(string lang)
        {
            IDictionary<string, LocalizationValue> content = null;
            TextAsset txt = Resources.Load<TextAsset>(ResourcesPath + "/" + lang + ".lang");
            if (txt == null)
                return null;
            content = new Dictionary<string, LocalizationValue>();
            Localization.LoadFromXml(txt.text, content);
            return content;
        }
    }
}
