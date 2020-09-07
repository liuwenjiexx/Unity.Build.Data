using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;
using UnityEditor.GUIExtensions;
using UnityEngine;
using UnityEngine.GUIExtensions;
using UnityEngine.Localizations;

namespace UnityEditor.Localizations
{
    using Localization = UnityEngine.Localizations.Localization;

    public class LocalizationEditorWindow : EditorWindow
    {
        private Vector2 scrollPos;
        private string newKey;
        private string newValueTypeName;
        private bool isDataDirted;

        private ItemData baseData;
        private ItemData[] itemDatas;

        private string[] allLangNames;
        private string[] allLangPaths;

        string searchKey;
        bool isBaseDataDirted;
        IEnumerable<string> keys;
        int itemWidth = 180;
        int itemHeight;

        static bool isLoaded;


        private void OnEnable()
        {
            if (!isLoaded)
            {
                isLoaded = true;

            }

            using (EditorLocalization.EditorLocalizationValues.BeginScope())
            {
                titleContent = new GUIContent("Localization".Localization());

                if (itemDatas == null)
                    itemDatas = new ItemData[0];
                if (baseData == null)
                    baseData = new ItemData();

                baseData.Load();

                foreach (var item in itemDatas)
                {
                    item.Load();
                }
                if (allLangNames == null)
                {
                    allLangNames = new string[0];
                    allLangPaths = new string[0];
                }
                EditorLocalization.GetValueDrawer("string");
            }
        }


        [MenuItem(EditorLocalization.MenuPrefix + "Localization", priority = EditorLocalization.MenuPriority)]
        public static void Show_Menu()
        {
            GetWindow<LocalizationEditorWindow>().Show();
        }

        public bool HasItem(string path)
        {
            return itemDatas.Where(o => o.path == path).Count() > 0;
        }

        ItemData AddItem(string path)
        {
            if (HasItem(path))
                return null;
            ItemData item = new ItemData() { path = path };
            item.Load();
            ArrayUtility.Add(ref itemDatas, item);
            return item;
        }

        ItemData FindByPath(string path)
        {
            foreach (var item in itemDatas)
            {
                if (item.path == path)
                    return item;
            }
            return null;
        }

        public void RemoveItem(int itemIndex)
        {
            if (itemIndex >= itemDatas.Length)
                return;

            ArrayUtility.RemoveAt(ref itemDatas, itemIndex);
        }


        public void SelectBase(string dir)
        {
            var allLangs = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            if (!string.IsNullOrEmpty(dir))
            {
                foreach (var file in Localization.GetLocalizationFiles(dir))
                {
                    string lang = Localization.ParseLangNameByFileName(file);
                    allLangs.Add(lang, file.ReplacePathSeparator());
                }
            }

            var tmp = allLangs.OrderBy(o => o.Key).ToArray();
            allLangNames = tmp.Select(o => o.Key).ToArray();
            allLangPaths = tmp.Select(o => o.Value).ToArray();

            if (string.IsNullOrEmpty(baseData.path))
            {
                if (allLangs.ContainsKey("en"))
                {
                    baseData.path = allLangs["en"];
                }
                else if (allLangs.ContainsKey("zh"))
                {
                    baseData.path = allLangs["zh"];
                }
                else
                {
                    if (allLangs.Count > 0)
                        baseData.path = allLangs.First().Value;
                }
            }

            LoadBase(baseData.path);

            itemDatas = new ItemData[0];
            foreach (var path in allLangPaths)
            {
                var item = AddItem(path);
                if (baseData.path == path)
                {
                    ShowIndex(item, 0);
                }
            }
        }

        void ShowIndex(ItemData item, int index)
        {
            if (item.displayIndex == index)
                return;

            if (item.displayIndex >= 0)
            {
                foreach (var it in itemDatas)
                {
                    if (it.displayIndex > index)
                    {
                        it.displayIndex--;
                    }
                }
                item.displayIndex = -1;
            }

            if (index >= 0)
            {
                foreach (var it in itemDatas)
                {
                    if (it.displayIndex >= index)
                    {
                        it.displayIndex++;
                    }
                }
                item.displayIndex = index;
            }
        }

        public bool IsShow(string lang)
        {
            foreach (var it in itemDatas)
            {
                if (it.lang == lang)
                {
                    return it.displayIndex >= 0;
                }
            }
            return false;
        }

        public bool IsShowWithPath(string path)
        {
            foreach (var it in itemDatas)
            {
                if (it.path == path)
                {
                    return it.displayIndex >= 0;
                }
            }
            return false;
        }

        int GetShowCount()
        {
            int count = 0;
            foreach (var it in itemDatas)
            {
                if (it.displayIndex >= 0)
                {
                    count++;
                }
            }
            return count;
        }


        int FindIndexWithLangName(string lang)
        {
            int index = -1;
            for (int i = 0; i < allLangNames.Length; i++)
            {
                if (string.Equals(allLangNames[i], lang, StringComparison.InvariantCultureIgnoreCase))
                {
                    index = i;
                    break;
                }
            }
            return index;
        }
        int FindIndexWithPath(string path)
        {
            int index = -1;

            for (int i = 0; i < allLangPaths.Length; i++)
            {
                if (string.Equals(allLangPaths[i], path, StringComparison.InvariantCultureIgnoreCase))
                {
                    index = i;
                    break;
                }
            }
            return index;
        }


        void LoadBase(string path)
        {
            baseData.path = path;
            baseData.Load();
        }

        void LoadAll()
        {
            foreach (var itemData in itemDatas)
            {
                itemData.Load();
            }
        }


        void Load(string path)
        {
            foreach (var item in itemDatas)
            {
                if (item.path == path)
                {
                    item.Load();
                    break;
                }
            }
        }



        void DirtyData(ItemData item)
        {
            //Debug.Log("dirty");
            isDataDirted = true;
            GUIUtility.keyboardControl = -1;

            item.Save();
            if (item.path == baseData.path)
            {
                baseData.Load();
            }
        }

        void DiryBaseData()
        {
            isBaseDataDirted = true;
            EditorApplication.delayCall += () =>
            {
                if (isBaseDataDirted)
                {
                    baseData.Save();
                    Load(baseData.path);
                }
            };

        }

        public static string CreateNewFile(string filePath)
        {
            if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            if (File.Exists(filePath))
            {
                EditorUtility.DisplayDialog("error", $"file exists <{filePath}>", "ok");
                return null;
            }

            XmlDocument doc = new XmlDocument();
            doc.AppendChild(doc.CreateXmlDeclaration("1.0", "utf-8", null));
            XmlElement elemRoot = doc.CreateElement(EditorLocalization.RootNodeName);
            elemRoot.SetOrAddAttributeValue("xmlns", EditorLocalization.XMLNS);

            doc.AppendChild(elemRoot);
            doc.Save(filePath);

            AssetDatabase.ImportAsset(filePath, ImportAssetOptions.ForceUpdate);
            return filePath;
        }

        public static bool PopupLang(string[] langNames, bool expandWidth = true, params GUILayoutOption[] options)
        {
            if (langNames == null)
                return false;
            GUIContent[] items;
            items = new GUIContent[] { new GUIContent("None".Localization()) }.Concat(langNames.Select(o => new GUIContent(o))).ToArray();
            bool changed = false;
            int selectedIndex = -1;
            for (int i = 0; i < langNames.Length; i++)
            {
                if (langNames[i] == Localization.SelectedLang)
                {
                    selectedIndex = i;
                    break;
                }
            }

            if (!expandWidth)
            {
                float maxLength;
                if (selectedIndex == -1)
                {
                    maxLength = EditorStyles.popup.CalcSize(items[0]).x;
                }
                else
                {
                    maxLength = EditorStyles.popup.CalcSize(items[selectedIndex + 1]).x;
                }

                options = options.Concat(new GUILayoutOption[] { GUILayout.Width(maxLength) }).ToArray();
            }


            int newIndex = EditorGUILayout.Popup(selectedIndex + 1, items, options);
            if (selectedIndex + 1 != newIndex)
            {
                newIndex--;
                if (newIndex < 0)
                    Localization.SelectedLang = null;
                else
                    Localization.SelectedLang = langNames[newIndex];
                changed = true;
                GUI.changed = true;
            }
            return changed;
        }

        bool IsFilterKey(string key)
        {
            if (string.IsNullOrEmpty(searchKey))
                return true;

            return key.IndexOf(searchKey, StringComparison.InvariantCultureIgnoreCase) >= 0;
        }

        private void OnGUI()
        {
            using (EditorLocalization.EditorLocalizationValues.BeginScope())
            {
                GUILangStatus();

                GUINewItem();

                itemHeight = 20;


                bool inheritValue;
                using (var sv = new GUILayout.ScrollViewScope(scrollPos))
                {
                    scrollPos = sv.scrollPosition;
                    keys = baseData.values.Keys;
                    keys = keys.Concat(itemDatas.SelectMany(o => o.values.Keys));
                    keys = keys.Distinct().Where(o => IsFilterKey(o)).OrderBy(o => o).ToArray();

                    using (new GUILayout.HorizontalScope())
                    {
                        GUILangKeys();
                        foreach (var item in itemDatas.OrderBy(o => o.displayIndex))
                        //for (int itemIndex = 0; itemIndex < itemDatas.Length; itemIndex++)
                        {
                            //var item = itemDatas[itemIndex];
                            if (item.displayIndex < 0)
                                continue;
                            int itemIndex = -1;
                            for (int i = 0; i < itemDatas.Length; i++)
                            {
                                if (itemDatas[i] == item)
                                {
                                    itemIndex = i;
                                    break;
                                }
                            }
                            bool isBaseEditing = baseData.path == item.path;

                            using (new GUILayout.VerticalScope(GUILayout.Width(itemWidth)))
                            {

                                using (new GUILayout.HorizontalScope())
                                {
                                    int selectedIndex = FindIndexWithPath(item.path);
                                    /*
                                    //float width = (Screen.width - EditorGUIUtility.labelWidth) * 0.3f;
                                    //width = Mathf.Min(width, 150);
                                    GUILayoutOption[] options = new GUILayoutOption[] { GUILayout.ExpandWidth(true) };
                                    //if (width < EditorStyles.popup.fixedWidth)
                                    //    options = options.Concat(new GUILayoutOption[] { GUILayout.MaxWidth(width) }).ToArray();
                                    newIndex = EditorGUILayout.Popup(GUIContent.none, selectedIndex, allLangNames, options);
                                    if (newIndex != selectedIndex)
                                    {
                                        selectedIndex = newIndex;
                                        if (newIndex != -1)
                                        {
                                            item.path = allLangPaths[newIndex];
                                            item.Load();
                                        }
                                    }

                                    if (EditorGUILayoutx.PingButton(item.path))
                                    {
                                    }*/

                                    if (GUILayout.Button("×", "label", GUILayout.ExpandWidth(false)))
                                    {
                                        //RemoveItem(i);

                                        //i--;
                                        ShowIndex(item, -1);
                                        continue;
                                    }

                                    GUIStyle style = new GUIStyle("label");
                                    style.alignment = TextAnchor.MiddleCenter;
                                    if (GUILayout.Button(allLangNames[selectedIndex], style))
                                    {
                                        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(allLangPaths[selectedIndex]));
                                    }


                                    if (item.lang != baseData.lang)
                                    {

                                        if (GUILayout.Button("T", TranslateButtonStyle, GUILayout.ExpandWidth(false)))
                                        {
                                            List<TranslateItem> list = new List<TranslateItem>();
                                            foreach (var key in item.values.Keys)
                                            {
                                                list.Add(new TranslateItem() { itemData = item, key = key });
                                            }
                                            Translate(baseData, list);
                                        }

                                        bool allSet = true;
                                        foreach (var key in baseData.values.Keys)
                                        {
                                            if (!item.values.ContainsKey(key))
                                            {
                                                allSet = false;
                                                break;
                                            }
                                        }
                                        if (GUILayout.Toggle(allSet, GUIContent.none, GUILayout.ExpandWidth(false)) != allSet)
                                        {
                                            allSet = !allSet;
                                            if (allSet)
                                            {
                                                foreach (var key in baseData.values.Keys)
                                                {
                                                    if (!item.values.ContainsKey(key))
                                                    {
                                                        item.values.Add(key, baseData.values[key].Clone());
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }

                                if (!string.IsNullOrEmpty(item.loadError))
                                {
                                    EditorGUILayout.HelpBox(item.loadError, MessageType.Error);
                                    continue;
                                }

                                using (var itemChecker = new EditorGUI.ChangeCheckScope())
                                {

                                    foreach (var key in keys)
                                    {

                                        inheritValue = !item.values.ContainsKey(key);
                                        using (new GUILayout.HorizontalScope(GUILayout.MaxHeight(itemHeight), GUILayout.Height(itemHeight)))
                                        {
                                            if (inheritValue && !baseData.values.ContainsKey(key))
                                            {
                                                using (new GUIx.Scopes.ColorScope(Color.red))
                                                {
                                                    GUILayout.Label("(missing)");
                                                }
                                                continue;
                                            }

                                            LocalizationValue value;
                                            if (inheritValue)
                                                value = baseData.values[key];
                                            else
                                                value = item.values[key];

                                            GUIItemNode(item, key, value, itemIndex);

                                            // using (new EditorGUI.DisabledGroupScope(inheritValue))
                                            {
                                                bool isEditValue = GUILayout.Toggle(!inheritValue, GUIContent.none, GUILayout.ExpandWidth(false));
                                                if (!inheritValue != isEditValue)
                                                {
                                                    if (isEditValue)
                                                    {
                                                        item.values.Add(key, baseData.values[key].Clone());
                                                    }
                                                    else
                                                    {
                                                        item.values.Remove(key);
                                                    }
                                                }
                                            }


                                            //using (new GUIx.Scopes.ChangedScope())
                                            //{
                                            //    if (GUILayout.Button("◥", "label", GUILayout.ExpandWidth(false)))
                                            //    {
                                            //        GenericMenu menu = new GenericMenu();

                                            //        if (item.values.ContainsKey(key))
                                            //        {
                                            //            menu.AddItem(new GUIContent("Delete".Localization() + $" [{key}]"), false, (o) =>
                                            //            {
                                            //                object[] arr = (object[])o;
                                            //                ItemData item1 = (ItemData)arr[0];
                                            //                string key1 = (string)arr[1];
                                            //                item1.values.Remove(key1);
                                            //                DirtyData(item1);
                                            //            }, new object[] { item, key });
                                            //        }
                                            //        else
                                            //        {
                                            //            menu.AddDisabledItem(new GUIContent("Delete".Localization() + $" [{key}]"), false);
                                            //        }

                                            //        /*    if (isBaseEditing)
                                            //            {
                                            //                menu.AddItem(new GUIContent("Delete".Localization() + $" [{key}]"), false, (o) =>
                                            //                  {
                                            //                      string key1 = (string)o;
                                            //                      values.Remove(key1);
                                            //                      ReloadBaseData();
                                            //                      DirtyData();
                                            //                  }, key);
                                            //            }
                                            //            else
                                            //            {
                                            //               // if (missingKey)
                                            //              //  {
                                            //                    menu.AddItem(new GUIContent("Delete".Localization() + $" [{key}]"), false, (o) =>
                                            //                    {
                                            //                        string key1 = (string)o;
                                            //                        values.Remove(key1);
                                            //                        DirtyData();
                                            //                    }, key);
                                            //           //     }
                                            //            //    else
                                            //              //  {
                                            //             //       menu.AddDisabledItem(new GUIContent("Delete".Localization() + $" [{key}]"), false);
                                            //             //   }
                                            //            }*/
                                            //        menu.ShowAsContext();
                                            //    }

                                            //}

                                        }
                                    }
                                    if (itemChecker.changed)
                                    {
                                        DirtyData(item);

                                        GUIUtility.keyboardControl = -1;
                                        GUI.changed = false;
                                    }
                                }
                            }
                        }


                        using (new GUILayout.HorizontalScope(GUILayout.Width(itemWidth)))
                        {
                            int[] indexs = new int[allLangPaths.Length];
                            for (int i = 0; i < allLangPaths.Length; i++)
                            {
                                indexs[i] = i;
                            }
                            indexs = indexs.Where((o) => !IsShowWithPath(allLangPaths[o])).ToArray();
                            int selectedIndex = -1;
                            selectedIndex = EditorGUILayout.Popup(GUIContent.none, selectedIndex,
                                indexs.Select(o => allLangNames[o]).ToArray(),
                                GUILayout.Width(80));

                            if (selectedIndex != -1)
                            {
                                //AddItem();
                                ShowIndex(itemDatas[indexs[selectedIndex]], GetShowCount());
                            }

                            if (GUILayout.Button("Create New".Localization(), GUILayout.ExpandWidth(false)))
                            {
                                string dir = "Assets";
                                if (!string.IsNullOrEmpty(baseData.path))
                                {
                                    dir = Path.GetDirectoryName(baseData.path);
                                }

                                string path = EditorUtility.SaveFilePanel("Create New Localization".Localization(), dir, "", Localization.ExtensionName);
                                if (!string.IsNullOrEmpty(path))
                                {
                                    string tmp;
                                    if (path.ToRelativePath(".", out tmp))
                                    {
                                        path = tmp;
                                    }
                                    path = CreateNewFile(path);
                                    dir = Path.GetDirectoryName(path);
                                    if (allLangNames.Length == 0)
                                    {
                                        SelectBase(dir);
                                    }
                                    ShowIndex(AddItem(path), GetShowCount());

                                }
                            }
                        }
                    }

                }
            }
        }
        class TranslateItem
        {
            public ItemData itemData;
            public string key;
        }
        void Translate(ItemData baseData, List<TranslateItem> items)
        {
            int total = items.Count;
            int current = -1;
            if (total == 0)
            {
                return;
            }

            int changed = 0;
            Action next = null;
            HashSet<ItemData> changeds = new HashSet<ItemData>();
            next = () =>
            {
                current++;
                if (current >= total)
                {
                    foreach (var it in changeds)
                    {
                        DirtyData(it);
                    }
                    EditorUtility.ClearProgressBar();
                    return;
                }

                var item = items[current];

                if (!baseData.values.ContainsKey(item.key))
                {
                    Debug.LogError("base data not contains key: " + item.key);
                    current = total;
                    next();
                    return;
                }

                if (!item.itemData.values.ContainsKey(item.key))
                {
                    next();
                    return;
                }
                string srcText = (string)baseData.values[item.key].Value;
                var value = item.itemData.values[item.key];
                EditorUtility.DisplayProgressBar("Translate", $"{baseData.lang} > {item.itemData.lang} [{current}/{total}]", current / (float)total);
                GoogleTranslator.Process(baseData.lang, item.itemData.lang, srcText, (b, result) =>
                {
                    if (!b)
                    {
                        Debug.LogError("key: " + item.key + ", lang: " + item.itemData.lang);
                        current = total;
                        next();
                        return;
                    }

                    if (!object.Equals(value.Value, result))
                    {
                        value.Value = result;
                        item.itemData.values[item.key] = value;
                        changed++;
                        if (!changeds.Contains(item.itemData))
                            changeds.Add(item.itemData);
                    }

                    next();
                });
            };
            next();
        }


        void GUILangStatus()
        {

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Label($"{"Current".Localization()}[{Localization.Lang}] {"Selected".Localization()} ", GUILayout.ExpandWidth(false));

                PopupLang(allLangNames, expandWidth: false);

                GUILayout.Label($"{"Default".Localization()}[{Localization.DefaultLang}] {"CurrentUICulture".Localization()}[{Thread.CurrentThread.CurrentUICulture.Name}] {"CurrentCulture".Localization()}[{ Thread.CurrentThread.CurrentCulture.Name}] {"systemLanguage".Localization()}[{Application.systemLanguage}]");
            }
        }

        void GUIBase()
        {
            int selectedBaseIndex;

            selectedBaseIndex = FindIndexWithPath(baseData.path);
            int newIndex = EditorGUILayout.Popup(GUIContent.none, selectedBaseIndex, allLangNames, GUILayout.MaxWidth(EditorGUIUtility.labelWidth));
            if (newIndex != selectedBaseIndex)
            {
                selectedBaseIndex = newIndex;
                if (selectedBaseIndex != -1)
                {
                    LoadBase(allLangPaths[selectedBaseIndex]);
                }
            }
        }

        void GUINewItem()
        {
            if (string.IsNullOrEmpty(baseData.path))
                return;

            string error = null;
            using (new GUILayout.HorizontalScope())
            {

                searchKey = EditorGUILayoutx.SearchTextField(searchKey, GUIContent.none, GUILayout.Width(EditorGUIUtility.labelWidth));

                int typeNameIndex = 0;
                string[] typeNames = EditorLocalization.GetValueTypeNames().ToArray();
                for (int i = 0; i < typeNames.Length; i++)
                {
                    if (typeNames[i] == newValueTypeName)
                    {
                        typeNameIndex = i;
                        break;
                    }
                }
                float width = (Screen.width - EditorGUIUtility.labelWidth) * 0.3f;
                width = Mathf.Min(width, 150);
                typeNameIndex = EditorGUILayout.Popup(typeNameIndex, typeNames.Select(o => ("type_" + o).Localization()).ToArray(), GUILayout.Width(width));
                newValueTypeName = typeNames[typeNameIndex];

                string newKeyCurrent;
                newKey = EditorGUILayoutx.DelayedPlaceholderField(newKey ?? string.Empty, out newKeyCurrent, new GUIContent("New Key".Localization())/*, GUILayout.Width(EditorGUIUtility.labelWidth)*/);



                if (!string.IsNullOrEmpty(newKeyCurrent))
                {
                    if (baseData.values.ContainsKey(newKeyCurrent))
                    {
                        error = string.Format("key <{0}> base exists", newKeyCurrent);
                    }
                }

                if (!string.IsNullOrEmpty(newKey) && error == null)
                {

                    var value = new LocalizationValue(newValueTypeName, Localization.GetValueProvider(newValueTypeName).DefaultValue);

                    if (newValueTypeName == "string")
                        value.Value = newKey;

                    baseData.values[newKey] = value;
                    newKey = string.Empty;
                    GUIUtility.keyboardControl = -1;
                    DiryBaseData();
                }
            }

            if (error != null)
            {
                EditorGUILayout.HelpBox(error, MessageType.Error);
            }

        }

        void GUILangKeys()
        {
            using (new GUILayout.VerticalScope(GUILayout.Width(EditorGUIUtility.labelWidth)))
            {
                GUIBase();
                bool oldChanged;
                foreach (var key in keys)
                {
                    bool baseMissingKey = !baseData.values.ContainsKey(key);
                    using (new GUILayout.HorizontalScope(GUILayout.Width(EditorGUIUtility.labelWidth), GUILayout.Height(itemHeight + 2)))
                    {
                        oldChanged = GUI.changed;
                        if (GUILayout.Button("◤", "label", GUILayout.ExpandWidth(false)))
                        {
                            GenericMenu menu = new GenericMenu();

                            if (baseData.values.ContainsKey(key))
                            {
                                menu.AddItem(new GUIContent("Delete".Localization()), false, (o) =>
                               {
                                   string key1 = (string)o;
                                   baseData.values.Remove(key1);
                                   DiryBaseData();
                               }, key);
                            }
                            else
                            {
                                menu.AddDisabledItem(new GUIContent("Delete".Localization()), false);
                            }

                            menu.ShowAsContext();
                            GUIUtility.keyboardControl = 0;
                            GUI.changed = oldChanged;
                        }




                        GUIStyle style;
                        /*
                                                            if (inheritValue)
                                                            {
                                                                style = new GUIStyle("label");
                                                                style.normal.textColor = Color.grey;
                                                                style.hover.textColor = style.normal.textColor;
                                                                style.active.textColor = style.normal.textColor;
                                                            }
                                                            else*/
                        if (baseMissingKey)
                        {
                            style = new GUIStyle("label");
                            style.normal.textColor = Color.red;
                            style.hover.textColor = style.normal.textColor;
                            style.active.textColor = style.normal.textColor;
                        }
                        else
                        {
                            style = "label";
                        }
                        oldChanged = GUI.changed;
                        //  using (new GUILayout.HorizontalScope())
                        {
                            // if (isBaseEditing)
                            //{
                            string newKey = EditorGUILayoutx.DelayedEditableLabel(key, labelStyle: style);
                            if (newKey != key && !string.IsNullOrEmpty(newKey))
                            {

                                /*   if (!values.ContainsKey(newKey))
                                   {
                                       if (values.ContainsKey(key))
                                       {
                                           values[newKey] = values[key];
                                           values.Remove(key);
                                       }
                                       else
                                       {
                                           if (baseData.values.ContainsKey(key))
                                           {
                                               values[newKey] = baseData.values[key];
                                           }
                                       }
                                   }*/
                                if (baseData.values.ContainsKey(key) && !baseData.values.ContainsKey(newKey))
                                {
                                    baseData.values[newKey] = baseData.values[key];
                                    baseData.values.Remove(key);
                                    DiryBaseData();

                                    GUIUtility.keyboardControl = 0;
                                }

                                //if (isBaseEditing)
                                //{
                                //    ReloadBaseData();
                                //}

                                GUI.changed = true;
                                continue;
                            }
                            //}
                            //else
                            //{
                            //    GUILayout.Label(key, style);
                            //}
                        }

                        if (GUILayout.Button("T", TranslateButtonStyle, GUILayout.ExpandWidth(false)))
                        {
                            List<TranslateItem> list = new List<TranslateItem>();
                            foreach (var itemData in itemDatas)
                            {
                                if (itemData.lang != baseData.lang)
                                {
                                    list.Add(new TranslateItem() { itemData = itemData, key = key });
                                }
                            }
                            Translate(baseData, list);
                        }


                        Rect labelRect = GUILayoutUtility.GetLastRect();
                        if (labelRect.Contains(Event.current.mousePosition))
                        {
                            if (Event.current.type == EventType.MouseDown)
                            {
                                EditorGUIUtility.systemCopyBuffer = key;
                            }
                        }
                        GUI.changed = oldChanged;

                    }
                }
            }
        }

        static GUIStyle translateButtonStyle;
        static GUIStyle TranslateButtonStyle
        {
            get
            {
                if (translateButtonStyle == null)
                {
                    translateButtonStyle = new GUIStyle("button");
                    //Debug.Log(translateButtonStyle.padding);
                    translateButtonStyle.padding = new RectOffset(4, 5, 3, 2);
                    //translateButtonStyle.margin = new RectOffset();
                    translateButtonStyle.fontSize -= 2;
                }
                return translateButtonStyle;
            }
        }


        void GUIItemNode(ItemData item, string key, LocalizationValue value, int itemIndex)
        {
            ILocalizationValueDrawer drawer;
            drawer = EditorLocalization.GetValueDrawer(value.TypeName);
            if (drawer == null)
            {
                drawer = EditorLocalization.GetValueDrawer("string");
            }

            bool ineritValue = !item.values.ContainsKey(key);

            if (!ineritValue)
            {
                value.Value = drawer.OnGUI(value.Value);
                item.values[key] = value;

                if (baseData.lang != item.lang)
                {
                    if (GUILayout.Button("T", TranslateButtonStyle, GUILayout.ExpandWidth(false)))
                    {
                        Translate(baseData, new List<TranslateItem>() { new TranslateItem() { itemData = item, key = key } });
                        //EditorUtility.DisplayProgressBar("Translate", "", 0f);

                        //GoogleTranslator.Process(itemDatas[0].lang, item.lang, (string)baseData.values[key].Value, (b, result) =>
                        //{
                        //    EditorUtility.ClearProgressBar();
                        //    if (b)
                        //    {
                        //        value.Value = result;
                        //        item.values[key] = value;
                        //        DirtyData(item);
                        //    }
                        //});
                    }
                }
            }
            else
            {
                var baseValue = baseData.values[key];
                using (new GUIx.Scopes.ColorScope(GUI.color * new Color(1, 1, 1, 0.5f)))
                using (var checker = new EditorGUI.ChangeCheckScope())
                {
                    object newValue = drawer.OnGUI(baseValue.Value);
                    if (checker.changed)
                    {
                        var clone = baseValue.Clone();
                        clone.Value = newValue;
                        item.values[key] = clone;
                        GUI.changed = true;
                    }
                }
            }
        }


        [UnityEditor.Callbacks.OnOpenAsset(-1)]
        static bool OnOpenAsset(int instanceID, int line)
        {
            string assetPath;
            assetPath = AssetDatabase.GetAssetPath(instanceID);
            if (!string.IsNullOrEmpty(assetPath))
            {
                if (Localization.IsLocalizationFile(assetPath))
                {
                    Show_Menu();
                    assetPath = assetPath.ReplacePathSeparator();
                    var win = GetWindow<LocalizationEditorWindow>();
                    //if (string.IsNullOrEmpty(win.baseData.path))

                    var item = win.FindByPath(assetPath);
                    if (item == null)
                    {
                        win.SelectBase(Path.GetDirectoryName(assetPath));
                        item = win.FindByPath(assetPath);
                    }
                    if (!win.IsShowWithPath(assetPath))
                        win.ShowIndex(item, win.GetShowCount());
                    return true;
                }
            }
            return false;
        }

        [Serializable]
        class ItemData
        {
            public string path;
            public string lang;
            [NonSerialized]
            public Dictionary<string, LocalizationValue> values;
            public string loadError;
            public int displayIndex = -1;
            public ItemData()
            {
                values = new Dictionary<string, LocalizationValue>();
            }

            public void Load()
            {
                values.Clear();
                loadError = null;
                lang = null;
                if (string.IsNullOrEmpty(path))
                    return;

                if (!File.Exists(path))
                    return;

                try
                {
                    Localization.LoadFromFile(path, values);
                }
                catch (Exception ex)
                {
                    loadError = ex.Message;
                }

                string filename = Path.GetFileName(path);
                if (path.EndsWith("." + Localization.ExtensionName, StringComparison.InvariantCultureIgnoreCase))
                {
                    lang = filename.Substring(0, filename.Length - Localization.ExtensionName.Length - 1);
                }
                else
                {
                    lang = Path.GetFileNameWithoutExtension(filename);
                }

            }
            public void Save()
            {
                Localization.SaveToXml(path, values);
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            }
        }

    }


}