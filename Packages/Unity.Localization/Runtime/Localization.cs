using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine.UI;
using System.Linq;
using System.Xml;
using System.Text;
using System.Collections.ObjectModel;

namespace UnityEngine.Localizations
{

    [ExecuteInEditMode]
    public class Localization : MonoBehaviour
    {
        public string key;
        [TextArea]
        public string format;
        private bool isDiried;
        public string Format
        {
            get => format;
            set
            {
                if (format != value)
                {
                    format = value;
                    OnChanged();
                }
            }
        }

        private const string LocalizationLanguageKey = "Localization.Language";
        private static LocalizationValues current;
        private static string selectedLang;


        public static ReadOnlyCollection<string> LangNames { get => Current.LangNames; }


        internal static List<LocalizationValues> All { get; private set; } = new List<LocalizationValues>();

        public static string DefaultLang { get; set; }

        public static string ExtensionName { get; private set; } = "lang.xml";

        public static bool IsInitialized { get; private set; }

        public static string NotFoundKeyFormat { get; private set; }

        private static Dictionary<string, ILocalizationValueProvider> valueProviders;

        const string RootNodeName = "localization";
        public const string XMLNS = "urn:schema-localization";
        private static LinkedList<Localization> nodes = new LinkedList<Localization>();
        private LinkedListNode<Localization> node;
        public static Action LanguageChanged;
        //   static ConfigProperty langConfig = new ConfigProperty(localization_current_key, typeof(string), null);

        /// <summary>
        /// 优先级 <see cref="SelectedLang"/>, <see cref="DefaultLang"/> <see cref="Thread.CurrentUICulture"/> <see cref="Thread.CurrentCulture"/>, <see cref="Application.systemLanguage"/>, <see cref="DefaultLang"/>
        /// </summary>
        public static string Lang
        {
            get
            {
                string lang;
                lang = SelectedLang;

                if (string.IsNullOrEmpty(lang))
                {
                    if (!string.IsNullOrEmpty(DefaultLang))
                    {
                        lang = DefaultLang;
                    }
                    else
                    {
                        Thread thread = Thread.CurrentThread;
                        if (thread.CurrentUICulture != null && !string.IsNullOrEmpty(thread.CurrentUICulture.Name))
                        {
                            lang = thread.CurrentUICulture.Name;
                        }
                        else if (thread.CurrentCulture != null && !string.IsNullOrEmpty(thread.CurrentCulture.Name))
                        {
                            lang = thread.CurrentCulture.Name;
                        }
                        else
                        {
                            lang = SystemLanguageToLangName(Application.systemLanguage);
                        }
                    }
                }


                if (string.IsNullOrEmpty(lang))
                    lang = "en";
                return lang;
            }
        }


        public static string SelectedLang
        {
            get
            {
                if (selectedLang == null)
                {
                    selectedLang = PlayerPrefs.GetString(LocalizationLanguageKey, null);
                    if (selectedLang == null)
                        selectedLang = string.Empty;
                }
                return selectedLang;
            }
            set
            {
                if (SelectedLang != value)
                {
                    string lang = Lang;
                    selectedLang = value;
                    PlayerPrefs.SetString(LocalizationLanguageKey, value);
                    PlayerPrefs.Save();
                    if (lang != Lang)
                    {
                        LoadLang(Lang);
                    }
                }
            }
        }

        private static LocalizationValues defaultLocal;
        public static LocalizationValues Default
        {
            get
            {
                if (defaultLocal == null)
                {
                    foreach (var type in System.AppDomain.CurrentDomain.GetAssemblies()
                        .Referenced(typeof(LocalizationValues).Assembly)
                        .SelectMany(o => o.GetTypes())
                        .Where(o => !o.IsAbstract && o.IsSubclassOf(typeof(DefaultLocalizationValues))))
                    {
                        defaultLocal = Activator.CreateInstance(type) as LocalizationValues;
                        break;
                    }

                    if (defaultLocal == null)
                        defaultLocal = new ResourcesLocalizationValues("Localization");
                }
                return defaultLocal;
            }

            set
            {
                defaultLocal = value;
            }
        }

        public static LocalizationValues Current
        {
            get
            {
                if (current == null)
                    return Default;
                return current;
            }
            set => current = value;
        }

        private void Awake()
        {
            node = nodes.AddFirst(this);
            isDiried = true;

        }
        // Use this for initialization
        void Start()
        {
            if (isDiried)
                OnChanged();
        }

        public void OnChanged()
        {
            if (!isActiveAndEnabled)
            {
                isDiried = true;
                return;
            }
            isDiried = false;
            if (!string.IsNullOrEmpty(key))
            {
                Text txt = GetComponent<Text>();
                string value = GetString(key);
                if (!string.IsNullOrEmpty(format))
                    value = string.Format(format, value);
                txt.text = value;
#if UNITY_EDITOR
                if (txt.enabled)
                {
                    txt.enabled = false;
                    txt.enabled = true;
                }
#endif

            }
            else
            {
                Debug.LogError("Localization key null, gameobject:" + name);
            }
        }

        public static void Initialize()
        {
            if (IsInitialized)
                return;
            IsInitialized = true;

            //Default.NotFoundKeyFormat = NotFoundKeyFormat;
            Default.Initialize();

            if (!Application.isEditor)
                Debug.Log($"Initalized Lang <{Lang}> LangNames <{LangNames.Count}> systemLanguage <{ Application.systemLanguage}> Thread CurrentCulture <{Thread.CurrentThread.CurrentCulture.Name}>");
        }

        public static void LoadLang(string lang)
        {
            
            Initialize();
            string oldLang = Current.Lang;
            if (oldLang == lang)
                return;

            bool prevLoaded = Current.Lang != null;
            Current.LoadLang(lang);

            // if (prevLoaded)
            {
                foreach (Localization item in nodes)
                {
                    item.OnChanged();
                }
            }
            if (!string.IsNullOrEmpty(oldLang))
            {
                LanguageChanged?.Invoke();
            }
        }

        private void OnEnable()
        {
            if (isDiried)
            {
                OnChanged();
            }
        }

        private void OnDestroy()
        {

            if (node != null)
            {
                nodes.Remove(node);
                node = null;
            }
        }

        //public static void EnsureLoad()
        //{
        //    if (Default.Lang != null)
        //        return;

        //    string lang;

        //    if (PlayerPrefs.HasKey(LocalizationLanguageKey))
        //    {
        //        lang = PlayerPrefs.GetString(LocalizationLanguageKey);
        //    }
        //    else
        //    {
        //        lang = SystemLanguageToLangName(Application.systemLanguage);
        //    }
        //    LoadLang(lang);

        //}
        /// <summary>
        /// 格式：<语言>-[<国家>]
        /// </summary>
        /// <param name="systemLanguage"></param>
        /// <returns></returns>
        public static string SystemLanguageToLangName(SystemLanguage systemLanguage)
        {
            string lang = "en";
            switch (systemLanguage)
            {
                case SystemLanguage.Chinese:
                    lang = "zh";
                    break;
                case SystemLanguage.ChineseSimplified:
                    lang = "zh-CN";
                    break;
                case SystemLanguage.ChineseTraditional:
                    lang = "zh-TW";
                    break;
                case SystemLanguage.French:
                    lang = "fr";
                    break;
                case SystemLanguage.Hungarian:
                    lang = "hu";
                    break;
                case SystemLanguage.German:
                    lang = "de";
                    break;
                case SystemLanguage.Japanese:
                    lang = "ja";
                    break;
                case SystemLanguage.Korean:
                    lang = "ko";
                    break;
                case SystemLanguage.Portuguese:
                    lang = "pt";
                    break;
                case SystemLanguage.Russian:
                    lang = "ru";
                    break;
                case SystemLanguage.Italian:
                    lang = "it";
                    break;
                case SystemLanguage.Spanish:
                    lang = "es";
                    break;
                case SystemLanguage.English:
                default:
                    lang = "en";
                    break;
            }
            return lang;
        }

        public static ILocalizationValueProvider GetValueProvider(string typeName)
        {
            if (valueProviders == null)
            {
                valueProviders = new Dictionary<string, ILocalizationValueProvider>();
                foreach (var type in AppDomain.CurrentDomain.GetAssemblies()
                    .Referenced(typeof(ILocalizationValueProvider).Assembly)
                    .SelectMany(o => o.GetTypes()))
                {
                    if (type.IsAbstract)
                        continue;
                    if (typeof(ILocalizationValueProvider).IsAssignableFrom(type))
                    {
                        var provider = Activator.CreateInstance(type) as ILocalizationValueProvider;
                        valueProviders.Add(provider.TypeName, provider);
                    }
                }
            }
            ILocalizationValueProvider valueProvider;
            if (!valueProviders.TryGetValue(typeName, out valueProvider))
                throw new Exception("not value type: " + typeName);
            return valueProvider;
        }




        public static bool HasItem(string key)
        {
            return Current.HasItem(key);
        }

        public static LocalizationValue GetItem(string key)
        {
            return Current.GetValue(key);
        }

        public static string GetString(string key)
        {
            return Current.GetString(key);
        }



        public static bool IsLocalizationFile(string file)
        {
            return Path.GetFileName(file).EndsWith("." + ExtensionName, StringComparison.InvariantCultureIgnoreCase);
        }

        public static IEnumerable<string> GetLocalizationFiles(string dir)
        {
            foreach (var file in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
            {
                if (IsLocalizationFile(file))
                {
                    yield return file;
                }
            }
        }

        public static string ParseLangNameByFileName(string file)
        {
            string lang = Path.GetFileName(file);
            lang = lang.Substring(0, lang.Length - ExtensionName.Length - 1);
            return lang;
        }

        public static void LoadFromFile(string file, IDictionary<string, LocalizationValue> dic)
        {
            if (File.Exists(file))
            {
                string xml = File.ReadAllText(file, Encoding.UTF8);
                LoadFromXml(xml, dic);
            }
        }

        public static void LoadFromXml(string xml, IDictionary<string, LocalizationValue> dic)
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);
            string key, text, typeName;
            ILocalizationValueProvider valueProvider;
            var nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("ns", XMLNS);
            XmlNode root = doc.SelectSingleNode("ns:" + RootNodeName, nsmgr);

            if (root != null)
            {
                foreach (XmlNode item in root.SelectNodes("*"))
                {
                    typeName = item.LocalName;
                    key = item.SelectSingleNode("@key").Value;
                    text = item.InnerText;

                    object value;
                    try
                    {
                        valueProvider = GetValueProvider(typeName);
                        value = valueProvider.Deserialize(text);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                        Debug.LogError($"error key: <{key}>, typeName: <{typeName}>");
                        continue;
                    }
                    LocalizationValue itemValue = new LocalizationValue(typeName, value);
                    dic[key] = itemValue;
                }
            }
        }

        public static void SaveToXml(string filePath, IDictionary<string, LocalizationValue> dic)
        {
            XmlDocument doc = new XmlDocument();
            doc.AppendChild(doc.CreateXmlDeclaration("1.0", "utf-8", null));
            XmlElement root = doc.CreateElement(RootNodeName);
            root.SetOrAddAttributeValue("xmlns", XMLNS);

            string key, typeName;
            LocalizationValue value;
            ILocalizationValueProvider valueProvider;
            XmlNode itemNode;
            foreach (var item in dic)
            {
                key = item.Key;
                value = item.Value;
                typeName = item.Value.TypeName;
                try
                {
                    valueProvider = GetValueProvider(typeName);

                    itemNode = doc.CreateElement(valueProvider.TypeName);
                    itemNode.SetOrAddAttributeValue("key", key);
                    itemNode.InnerText = valueProvider.Serialize(value.Value);
                    root.AppendChild(itemNode);
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    Debug.LogError($"<{filePath}> key <{key}> typeName:<{typeName}>");
                    throw ex;
                }
            }

            doc.AppendChild(root);
            doc.Save(filePath);
        }

        [Obsolete]
        public static IDisposable BeginScope(LocalizationValues values, string lang = null)
        {
            return values.BeginScope(lang);
        }


        internal class LocalizationScope : IDisposable
        {
            public LocalizationValues origin;
            private LocalizationValues newItem;
            private string newItemLang;

            public LocalizationScope(LocalizationValues newItem, string lang)
            {
                this.newItem = newItem;
                origin = Localization.Current;
                if (newItem != null)
                {
                    if (!string.IsNullOrEmpty(lang) && newItem.Lang != lang)
                    {
                        newItemLang = newItem.Lang;
                        newItem.LoadLang(lang);
                    }
                }
                Localization.Current = newItem;
            }

            public void Dispose()
            {
                if (newItem.Lang != newItemLang)
                {
                    newItem.LoadLang(newItemLang);
                }
                Localization.Current = origin;
            }
        }
    }


    public interface ILanguageChanged
    {
        void OnLanguageChanged(string lang);
    }




}