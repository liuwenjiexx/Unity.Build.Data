using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Text;

namespace UnityEngine.Localizations
{

    public class DirectoryLocalizationValues : LocalizationValues
    {
        public DirectoryLocalizationValues(string directoryPath)
        {
            DirectoryPath = directoryPath;
        }

        public string DirectoryPath { get; private set; }

        protected override IEnumerable<string> LoadNames()
        {
            foreach (var file in Localization.GetLocalizationFiles(DirectoryPath))
            {
                string lang = Localization.ParseLangNameByFileName(file);
                yield return lang;
            }
        }

        protected override IDictionary<string, LocalizationValue> LoadValues(string lang)
        {
            if (DirectoryPath == null)
                return null;
            IDictionary<string, LocalizationValue> content = null;
            string path = Path.Combine(DirectoryPath, lang + "." + Localization.ExtensionName);
            if (File.Exists(path))
            {
                string text = File.ReadAllText(path, Encoding.UTF8);
                content = new Dictionary<string, LocalizationValue>();
                Localization.LoadFromXml(text, content);
            }

            return content;
        }
    }

}