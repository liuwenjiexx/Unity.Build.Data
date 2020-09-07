using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using System.Linq;
using System.Collections.ObjectModel;

namespace UnityEngine.Localizations
{
    public abstract class LocalizationValues
    {
        public string Lang { get; private set; }

        private IDictionary<string, LocalizationValue> langDic;

        private Dictionary<string, IDictionary<string, LocalizationValue>> cached;
        private List<string> langParts;

        public bool IsInitialized { get; private set; }
        private List<string> langNames;
        private ReadOnlyCollection<string> readonlyLangNames;
        public ReadOnlyCollection<string> LangNames { get => readonlyLangNames; }

        public string NotFoundKeyFormat { get; set; }

        public LocalizationValues()
        {
            langNames = new List<string>();
            readonlyLangNames = langNames.AsReadOnly();
        }

        protected abstract IEnumerable<string> LoadNames();

        protected abstract IDictionary<string, LocalizationValue> LoadValues(string lang);


        public virtual void Initialize()
        {
            if (IsInitialized)
                return;
            IsInitialized = true;
            langNames.Clear();
            foreach (var lang in LoadNames())
            {
                if (!langNames.Contains(lang))
                {
                    langNames.Add(lang);
                }
            }
            langNames.Sort(StringComparer.Ordinal);
            Localization.All.Add(this);
        }



        public virtual void LoadLang(string lang)
        {
            lang = lang ?? string.Empty;
            if (cached == null)
                cached = new Dictionary<string, IDictionary<string, LocalizationValue>>();
            langDic = null;
            if (langParts == null)
                langParts = new List<string>();
            langParts.Clear();

            foreach (var part in EnumerateLang(lang))
            {
                IDictionary<string, LocalizationValue> content = null;

                if (!cached.TryGetValue(part, out content))
                {
                    content = LoadValues(part);
                    if (content != null)
                    {
                        cached[part] = content;
                    }
                }
                if (content != null)
                {
                    var dic = new HierarchyDictionary<string, LocalizationValue>(langDic);
                    dic.AddRangeLocal(content);
                    langDic = dic;
                    langParts.Add(part);
                }
            }

            if (langDic == null)
                langDic = new Dictionary<string, LocalizationValue>();

            Lang = lang;
        }




        IEnumerable<string> EnumerateLang(string lang)
        {
            HashSet<string> set = new HashSet<string>();

            if (!string.IsNullOrEmpty(Localization.DefaultLang))
            {
                set.Add(Localization.DefaultLang);
                yield return Localization.DefaultLang;
            }

            string[] parts = lang.Split('-');
            if (parts.Length > 1)
            {
                if (!set.Contains(parts[0]))
                {
                    set.Add(parts[0]);
                    yield return parts[0];
                }
            }

            if (!set.Contains(lang))
            {
                set.Add(lang);
                yield return lang;
            }

        }


        public bool HasItem(string key)
        {
            return langDic.ContainsKey(key);
        }

        #region Get Value

        public LocalizationValue GetValue(string key)
        {
            LocalizationValue value;

            if (key != null && !langDic.TryGetValue(key, out value))
                return value;
            return default(LocalizationValue);
        }

        public bool TryGetValue(string key, out LocalizationValue value)
        {
            if (key != null)
                return langDic.TryGetValue(key, out value);
            value = default(LocalizationValue);
            return false;
        }


        public string GetString(string key)
        {
            LocalizationValue value;
            if (!TryGetValue(key, out value))
            {
                if (string.IsNullOrEmpty(NotFoundKeyFormat))
                    return key;
                return string.Format(NotFoundKeyFormat, key);
            }
            return value.Value as string;
        }

        public Texture2D GetTexture2D(string key)
        {
            return GetValue(key).Value as Texture2D;
        }

        public Color GetColor(string key)
        {
            LocalizationValue value;
            if (!TryGetValue(key, out value))
            {
                return Color.clear;
            }
            return (Color)value.Value;
        }

        #endregion


        public IDisposable BeginScope(string lang = null)
        {
            if (string.IsNullOrEmpty(lang))
                lang = Localization.Lang;
            return new Localization.LocalizationScope(this, lang);
        }

    }

    public abstract class DefaultLocalizationValues : LocalizationValues
    {

    }

}
