using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Localizations;
using UnityEngine.UI;

namespace UnityEngine.Localizations.Example
{

    public class LocalizationExample : MonoBehaviour
    {
        public Dropdown listLanguage;
        public Text text;

        /// <summary>
        /// 下拉列表语言选项
        /// </summary>
        private static Dictionary<string, string> langs = new Dictionary<string, string>()
        {
            {"en", "English" },
            {"zh","中文" },
              {"zh-TW","正體字" }
        };


        class DataLocalizationValues : DefaultLocalizationValues
        {

            protected override IEnumerable<string> LoadNames()
            {
                return langs.Keys;
            }

            protected override IDictionary<string, LocalizationValue> LoadValues(string lang)
            {
                IDictionary<string, LocalizationValue> result = null;
                switch (lang)
                {
                    case "zh":
                        result = LocalizationValue.StringDictionary(new Dictionary<string, string>()
                        {
                            {"Hello World","你好世界"},
                            {"Language","语言"  }
                        });
                        break;
                    case "en":
                        result = LocalizationValue.StringDictionary(new Dictionary<string, string>()
                        {
                            {"Hello World","Hello World" },
                            {"Language","Language" }
                        });
                        break;
                    case "zh-TW":
                        result = LocalizationValue.StringDictionary(new Dictionary<string, string>() {
                        { "Language", "語言" }
                    });
                        break;
                }
                return result;
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            //初始化
            Localization.Initialize();

            InitializeLanguageDropdown();


            UpdateUI();
        }

        /// <summary>
        /// 初始化语言选项下拉列表
        /// </summary>
        void InitializeLanguageDropdown()
        {

            listLanguage.onValueChanged.AddListener((v) =>
            {
                string lang = null;
                if (v == 0)
                {
                    Localization.SelectedLang = null;
                    return;
                }
                int i = 1;
                foreach (var item in langs)
                {
                    if (i == v)
                    {
                        lang = item.Key;
                        break;
                    }
                    i++;
                }

                //设置选择的语言
                Localization.SelectedLang = lang;
            });
            listLanguage.ClearOptions();

            listLanguage.AddOptions(new string[] { "Follow  System" }.Concat(langs.Values).ToList());

            UpdateLanguageDropdown();
        }


        void UpdateUI()
        {
            text.text = "Language".Localization();
        }

        void UpdateLanguageDropdown()
        {

            int j = 0;
            foreach (var item in langs)
            {
                if (item.Key == Localization.Lang)
                {
                    listLanguage.value = j + 1;
                    break;
                }
                j++;
            }
        }
        private void OnEnable()
        {
            Localization.LanguageChanged += OnLanguageChanged;
        }

        private void OnDisable()
        {
            Localization.LanguageChanged -= OnLanguageChanged;
        }


        void OnLanguageChanged()
        {
            Debug.Log("OnLanguageChanged");
            UpdateLanguageDropdown();
            UpdateUI();
        }
    }
}