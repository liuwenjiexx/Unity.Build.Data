using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.GUIExtensions;

namespace UnityEditor.GUIExtensions
{
    public static partial class EditorGUILayoutx
    {
        public static bool PingButton(UnityEngine.Object target)
        {
            using (new EditorGUI.DisabledGroupScope(!target))
            {
                GUIStyle style = new GUIStyle("label");
                style.padding = new RectOffset(2, 2, 0, 0);
                style.margin = new RectOffset(0, 2, 0, 0);
                style.fontSize = 14;
                if (GUILayout.Button("⊙", style, GUILayout.ExpandWidth(false)))
                {
                    if (target)
                    {
                        EditorGUIUtility.PingObject(target);
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool PingButton(string assetPath)
        {
            using (new EditorGUI.DisabledGroupScope(string.IsNullOrEmpty(assetPath)))
            {
                GUIStyle style = new GUIStyle("label");
                style.padding = new RectOffset(2, 2, 0, 0);
                style.margin = new RectOffset(0, 2, 0, 0);
                style.fontSize = 14;
                if (GUILayout.Button("⊙", style, GUILayout.ExpandWidth(false)))
                {
                    var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                    if (obj)
                    {
                        EditorGUIUtility.PingObject(obj);
                        return true;
                    }
                }
            }
            return false;
        }


        #region PlaceholderField

        public static string DelayedPlaceholderField(string text, GUIContent placeholder, params GUILayoutOption[] options)
        {
            return DelayedPlaceholderField(text, placeholder, textStyle: null, placeholderStyle: null, options);
        }

        public static string DelayedPlaceholderField(string text, GUIContent placeholder, GUIStyle textStyle = null, GUIStyle placeholderStyle = null, GUILayoutOption[] options = null)
        {
            string current;
            return DelayedPlaceholderField(text, out current, placeholder, textStyle: textStyle, placeholderStyle: placeholderStyle, options);
        }
        public static string DelayedPlaceholderField(this GUIContent label, string text, GUIContent placeholder, params GUILayoutOption[] options)
        {
            string current;
            return DelayedPlaceholderField(label, text, out current, placeholder, options);
        }


        public static string DelayedPlaceholderField(string text, out string current, GUIContent placeholder, params GUILayoutOption[] options)
        {
            return DelayedPlaceholderField(text, out current, placeholder, textStyle: null, placeholderStyle: null, options);
        }
        public static string DelayedPlaceholderField(string text, out string current, GUIContent placeholder, GUIStyle textStyle = null, GUIStyle placeholderStyle = null, GUILayoutOption[] options = null)
        {
            if (textStyle == null)
                textStyle = "textfield";
            Rect rect = GUILayoutUtility.GetRect(GUIContent.none, textStyle, options);
            return EditorGUIx.DelayedPlaceholderField(rect, text, out current, placeholder, textStyle: textStyle, placeholderStyle: placeholderStyle);
        }

        public static string DelayedPlaceholderField(this GUIContent label, string text, out string current, GUIContent placeholder, params GUILayoutOption[] options)
        {
            using (new GUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(label);
                text = DelayedPlaceholderField(text, out current, placeholder, options);
            }
            return text;
        }


        #endregion

        public static void RightMenuButton<T>(Func<T, GenericMenu> createMenu, T userData)
        {
            using (new GUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                GUIStyle style = new GUIStyle();
                style.padding.left = 5;
                style.padding.right = 5;
                style.fixedHeight = EditorGUIUtility.singleLineHeight - 6;

                MenuButton(EditorGUIx.MenuLabel, style, createMenu, userData);
            }
        }

        public static void MenuButton<T>(GUIContent label, GUIStyle style, Func<T, GenericMenu> createMenu, T userData, params GUILayoutOption[] options)
        {
            var old = GUI.changed;

            if (GUILayout.Button(label, style, options))
            {
                createMenu(userData).ShowAsContext();
            }
            GUI.changed = old;
        }
        public static void MenuButton(GUIContent label, GUIStyle style, Func<GenericMenu> createMenu, params GUILayoutOption[] options)
        {
            var old = GUI.changed;

            if (GUILayout.Button(label, style, options))
            {
                createMenu().ShowAsContext();
            }
            GUI.changed = old;
        }

        public static bool ToggleLabel(string label, bool value, params GUILayoutOption[] options)
        {
            return ToggleLabel(new GUIContent(label), value, EditorGUIx.Styles.ToggleLabel, options);
        }
        public static bool ToggleLabel(this GUIContent label, bool value, GUIStyle style = null, GUILayoutOption[] options = null)
        {
            if (style == null)
                style = EditorGUIx.Styles.ToggleLabel;
            var rect = GUILayoutUtility.GetRect(label, style, options);
            return EditorGUIx.ToggleLabel(rect, label, value);
        }


        public static string DelayedEditableLabel(string text, params GUILayoutOption[] options)
        {
            return DelayedEditableLabel(text, "label", "textfield", clickCount: 2, options);
        }

        public static string DelayedEditableLabel(string text, GUIStyle labelStyle = null, GUIStyle textStyle = null, int clickCount = GUIx.EditableLabelClickCount, GUILayoutOption[] options = null)
        {
            if (labelStyle == null)
                labelStyle = "label";
            if (options == null)
                options = new GUILayoutOption[0];
            Rect rect = GUILayoutUtility.GetRect(new GUIContent(text), labelStyle, options);
            return EditorGUIx.DelayedEditableLabel(rect, text, labelStyle: labelStyle, textStyle: textStyle, clickCount: clickCount);
        }
        public static string DelayedEditableLabel(this GUIContent label, string text, params GUILayoutOption[] options)
        {
            return DelayedEditableLabel(label, text, options: options);
        }
        public static string DelayedEditableLabel(this GUIContent label, string text, GUIStyle labelStyle = null, GUIStyle textStyle = null, int clickCount = GUIx.EditableLabelClickCount, GUILayoutOption[] options = null)
        {
            using (new GUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(label);
                return DelayedEditableLabel(text, labelStyle: labelStyle, textStyle: textStyle, clickCount: clickCount, options: options);
            }
        }

        public static string SearchTextField(string text, GUIContent placeholder, params GUILayoutOption[] options)
        {
            Rect rect;//= EditorGUILayout.GetControlRect(true, options);
            GUIStyle searchTextFieldStyle;
            searchTextFieldStyle = "SearchTextField";

            rect = GUILayoutUtility.GetRect(GUIContent.none, searchTextFieldStyle, options);
            return EditorGUIx.SearchTextField(rect, text, placeholder);
        }

        public static string SearchTextField(this GUIContent label, string text, GUIContent placeholder, params GUILayoutOption[] options)
        {
            using (new GUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(label);
                var oldIndentLevel = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                text = SearchTextField(text, placeholder);
                EditorGUI.indentLevel = oldIndentLevel;
            }
            return text;
        }

        public static string FileField(this GUIContent label, string file, string extension, string title, string relativePath = null, params GUILayoutOption[] options)
        {
            using (new GUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(label);
                var oldIndentLevel = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                file = FileField(file, extension, title, relativePath, options);
                EditorGUI.indentLevel = oldIndentLevel;
            }
            return file;
        }

        public static string FileField(string file, string extension, string title, string relativePath = null, params GUILayoutOption[] options)
        {
            return FileField(file, extension, title, relativePath: relativePath, style: "textfield", options);
        }

        public static string FileField(string file, string extension, string title, string relativePath = null, GUIStyle style = null, params GUILayoutOption[] options)
        {
            if (style == null) style = "textfield";
            Rect rect = GUILayoutUtility.GetRect(GUIContent.none, style, options);
            return EditorGUIx.FileField(rect, file ?? string.Empty, extension, title, style, relativePath);
        }

        public static string FolderField(this GUIContent label, string folder, string title, string relativePath = null, params GUILayoutOption[] options)
        {
            using (new GUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(label);
                var oldIndentLevel = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                folder = FolderField(folder, title, relativePath: relativePath, style: "textfield", options);
                EditorGUI.indentLevel = oldIndentLevel;
            }
            return folder;
        }

        public static string FolderField(string folder, string title, string relativePath = null, GUIStyle style = null, params GUILayoutOption[] options)
        {
            if (style == null) style = "textfield";
            GUIContent content = new GUIContent(folder ?? string.Empty);
            Rect rect = GUILayoutUtility.GetRect(content, style, options);
            return EditorGUIx.FolderField(rect, folder, title, relativePath: relativePath, style: style, options);
        }



        public static string Base64TextField(GUIContent label, string value, int byteLength, params GUILayoutOption[] options)
        {
            using (new GUILayout.HorizontalScope())
            {
                value = EditorGUILayout.TextField(label, value);
                var oldEnabled = GUI.enabled;
                GUI.enabled &= string.IsNullOrEmpty(value);

                if (GUILayout.Button("Gennerate", GUILayout.ExpandWidth(false)))
                {
                    byte[] data = new byte[byteLength];
                    new System.Random().NextBytes(data);
                    value = Convert.ToBase64String(data);
                    GUI.changed = true;
                    GUIUtility.keyboardControl = -1;
                }
                GUI.enabled = oldEnabled;
            }
            return value;
        }

        #region CryptoKeyField


        public static string CryptoKeyField(GUIContent label, string path, out string publicKey, string algorithm = null, params GUILayoutOption[] options)
        {
            var state = (CryptoKeyFieldState)GUIUtility.GetStateObject(typeof(CryptoKeyFieldState), GUIUtility.GetControlID(FocusType.Passive));
            if (state.path != path || state.algorithm != algorithm)
            {
                state.path = path;
                state.algorithm = algorithm;
                state.publicKey = string.Empty;

                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    var rsa = new RSACryptoServiceProvider();
                    rsa.FromXmlString(File.ReadAllText(path, Encoding.UTF8));
                    state.publicKey = Convert.ToBase64String(rsa.ExportCspBlob(false));
                }
            }
            Rect rect, newRect;

            using (new GUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(label);
                var oldIndentLevel = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                using (new GUILayout.HorizontalScope())
                {
                    path = EditorGUILayout.TextField(path);
                }
                rect = GUILayoutUtility.GetLastRect();
                EditorGUI.indentLevel = oldIndentLevel;
                using (new GUIx.Scopes.ChangedScope())
                {
                    if (GUILayout.Button("Create", GUILayout.ExpandWidth(false)))
                    {
                        GenericMenu menu = new GenericMenu();
                        menu.AddItem(new GUIContent("Open"), false, () =>
                        {
                            state.mode = 1;
                        });
                        menu.AddItem(new GUIContent("Create New"), false, () =>
                          {
                              state.mode = 2;
                          });
                        menu.ShowAsContext();

                    }
                }

                if (state.mode != 0)
                {
                    if (state.mode == 1)
                    {
                        state.mode = 0;
                        string newPath = EditorUtility.OpenFilePanel("Crypto RSA Key", "Assets", "");
                        if (!string.IsNullOrEmpty(newPath))
                        {
                            string result;
                            if (newPath.ToRelativePath(".", out result))
                            {
                                newPath = result;
                            }
                            path = newPath;
                            state.path = null;
                            state.publicKey = null;
                            GUI.changed = true;
                            GUIUtility.keyboardControl = -1;
                        }
                    }
                    else if (state.mode == 2)
                    {
                        state.mode = 0;
                        string newPath = EditorUtility.SaveFilePanel("Crypto RSA Key", "Assets", "rsa.key", "");
                        if (!string.IsNullOrEmpty(newPath))
                        {
                            string result;
                            if (newPath.ToRelativePath(".", out result))
                            {
                                newPath = result;
                            }
                            if (!File.Exists(newPath))
                            {
                                var rsa = new RSACryptoServiceProvider();
                                File.WriteAllText(newPath, rsa.ToXmlString(true), new UTF8Encoding(false));
                                path = newPath;
                                state.path = null;
                                state.publicKey = null;
                                GUI.changed = true;
                                GUIUtility.keyboardControl = -1;
                            }
                            else
                            {
                                Debug.LogError(newPath + " exists");
                            }
                        }
                    }
                }
            }

            using (new GUILayout.HorizontalScope())
            {
                GUIContent pubKeyContent = new GUIContent(state.publicKey ?? string.Empty);

                newRect = GUILayoutUtility.GetRect(GUIContent.none, "textfield");
                int labelWidth = 50;
                newRect.xMin = rect.xMin;
                newRect.xMin -= 16;


                EditorGUI.PrefixLabel(new Rect(newRect.x, newRect.y, labelWidth, newRect.height), new GUIContent("PubKey", "公匙"));

                EditorGUI.TextField(new Rect(newRect.x + labelWidth, newRect.y, newRect.width - labelWidth, newRect.height), pubKeyContent.text);

            }
            publicKey = state.publicKey;
            return path;
        }

        class CryptoKeyFieldState
        {
            public string path;
            public string algorithm;
            public string publicKey;
            public int mode;
        }
        #endregion



        //public static void ArrayField<T>(this GUIContent label, ref T[] array, Func<T, int, T> onGUI, Func<T> onCreate = null, Func<GenericMenu> onCreateMenu = null)
        //{
        //    var state = (ArrayFieldState)GUIUtility.GetStateObject(typeof(ArrayFieldState), GUIUtility.GetControlID(FocusType.Passive));

        //    Rect rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none);

        //    state.expand = EditorGUI.BeginFoldoutHeaderGroup(new Rect(rect.x, rect.y, rect.width - 18, rect.height), state.expand, label);

        //    if (state.expand)
        //    {
        //        EditorGUI.indentLevel++;
        //        for (int i = 0; i < array.Length; i++)
        //        {
        //            var item = array[i];
        //            array[i] = onGUI(item, i);
        //        }
        //        EditorGUI.indentLevel--;
        //    }

        //    EditorGUI.EndFoldoutHeaderGroup();

        //    if (GUI.Button(new Rect(rect.xMax - 16, rect.y, 16, rect.height), EditorGUIx.PositiveLabel, GUIStyle.none))
        //    {
        //        GenericMenu menu = onCreateMenu?.Invoke();
        //        state.expand = true;
        //        if (menu != null)
        //        {
        //            menu.ShowAsContext();
        //        }
        //        else
        //        {
        //            T instance;
        //            if (onCreate != null)
        //                instance = onCreate();
        //            else
        //                instance = Activator.CreateInstance<T>();
        //            ArrayUtility.Insert(ref array, array.Length, instance);
        //            GUI.changed = true;
        //        }

        //    }

        //}
        //不能嵌套使用 BeginFoldoutHeaderGroup
        static int FoldoutHeaderGroupCounter = 0;


        public static IList<T> ArrayField<T>(this GUIContent label, IList<T> list, Func<T, int, T> onGUI, bool initExpand = false, GUIStyle itemStyle = null, Func<T> createInstance = null, Func<GenericMenu> createMenu = null, Func<GenericMenu, GenericMenu> itemMenu = null, bool handleDelete = false, bool useFoldoutHeader = true)
        {
            var state = (ArrayFieldState)GUIUtility.GetStateObject(typeof(ArrayFieldState), GUIUtility.GetControlID(FocusType.Passive));
            if (!state.initialized)
            {
                state.initialized = true;
                state.expand = initExpand;
            }
            if (state.changed)
            {
                state.changed = false;
                list = (IList<T>)state.value;
                GUI.changed = true;
                EditorGUIUtility.editingTextField = false;
            }

            FoldoutHeaderGroupCounter++;
            state.expand = EditorGUILayout.BeginFoldoutHeaderGroup(state.expand, label, menuAction: (r) =>
            {
                GenericMenu menu = createMenu?.Invoke();
                state.expand = true;
                if (menu != null)
                {
                    menu.ShowAsContext();
                }
                else
                {
                    menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Add"), false, () =>
                    {

                        T instance;
                        if (createInstance != null)
                            instance = createInstance();
                        else
                        {
                            if (typeof(T) == typeof(string))
                                instance = (T)(object)string.Empty;
                            else
                                instance = Activator.CreateInstance<T>();
                        }
                        T[] array = list as T[];

                        if (array != null)
                        {
                            ArrayUtility.Insert(ref array, array.Length, instance);
                            list = array;
                        }
                        else
                        {
                            list.Add(instance);
                        }
                        state.value = list;
                        state.changed = true;

                    });
                    menu.ShowAsContext();
                }
            });
            EditorGUI.EndFoldoutHeaderGroup();

            if (state.expand)
            {
                //EditorGUI.indentLevel++;
                for (int i = 0; i < list.Count; i++)
                {
                    var item = list[i];
                    using (new Scopes.IndentLevelVerticalScope(itemStyle))
                    {
                        if (!handleDelete)
                        {
                            RightMenuButton((itemIndex) =>
                            {
                                GenericMenu menu = new GenericMenu();

                                if (itemIndex > 0)
                                {
                                    menu.AddItem(new GUIContent("Move Up"), false, () =>
                                    {
                                        var tmp = list[itemIndex];
                                        list[itemIndex] = list[itemIndex - 1];
                                        list[itemIndex - 1] = tmp;
                                        state.value = list;
                                        state.changed = true;
                                    });
                                }
                                else
                                {
                                    menu.AddDisabledItem(new GUIContent("Move Up"));
                                }
                                if (itemIndex < list.Count - 1)
                                {
                                    menu.AddItem(new GUIContent("Move Down"), false, () =>
                                    {
                                        var tmp = list[itemIndex];
                                        list[itemIndex] = list[itemIndex + 1];
                                        list[itemIndex + 1] = tmp;
                                        state.value = list;
                                        state.changed = true;
                                    });
                                }
                                else
                                {
                                    menu.AddDisabledItem(new GUIContent("Move Down"));
                                }

                                menu.AddItem(new GUIContent("Delete"), false, () =>
                                {
                                    T[] array = list as T[];
                                    if (array != null)
                                    {
                                        ArrayUtility.RemoveAt(ref array, itemIndex);
                                        list = array;
                                    }
                                    else
                                    {
                                        list.RemoveAt(itemIndex);
                                    }
                                    state.value = list;
                                    state.changed = true;
                                });

                                if (itemMenu != null)
                                    menu = itemMenu(menu);

                                return menu;
                            }, i);
                        }
                        item = onGUI(item, i);
                        list[i] = item;
                    }
                }
                //EditorGUI.indentLevel--;
            }

            FoldoutHeaderGroupCounter--;

            return list;
        }

        [Serializable]
        class ArrayFieldState
        {
            public bool initialized;
            public bool expand;
            public int addIndex = -1;
            public int removeIndex = -1;
            public bool changed = false;
            public object value;
        }



        public static string ProviderTypeName(string providerTypeName, string assemblyDir, string assemblyFilterPattern, string baseTypeName)
        {
            ProviderTypeState state;
            int ctrlId = GUIUtility.GetControlID(FocusType.Passive);
            state = (ProviderTypeState)GUIUtility.GetStateObject(typeof(ProviderTypeState), ctrlId);
            if (!state.loaded)
            {
                state.loaded = true;
                state.LoadAssemblies(assemblyDir, assemblyFilterPattern, baseTypeName);
            }


            string typeName = null, assemblyName = null;
            int selectedIndex = -1;

            if (!string.IsNullOrEmpty(providerTypeName))
            {
                string[] parts = providerTypeName.Split(',');
                typeName = parts[0].Trim();
                if (parts.Length > 1)
                    assemblyName = parts[1].Trim();
                for (int i = 0; i < state.list.Count; i++)
                {
                    var item = state.list[i];
                    if (item == null || string.IsNullOrEmpty(item.TypeFullName))
                    {
                        continue;
                    }
                    if (item.TypeFullName.Equals(typeName, StringComparison.InvariantCultureIgnoreCase) &&
                        item.AssemblyName.Equals(assemblyName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        selectedIndex = i;
                        break;
                    }
                }
            }
            else
            {
                if (state.list.Count > 0 && string.IsNullOrEmpty(state.list[0].TypeFullName))
                {
                    selectedIndex = 0;
                }
            }



            int newIndex = EditorGUILayout.Popup(selectedIndex, state.list.Select(o => new GUIContent(o.displayName)).ToArray());
            if (newIndex != selectedIndex)
            {
                if (newIndex != -1)
                {
                    var item = state.list[newIndex];
                    providerTypeName = item.AssemblyFullName;
                }
            }
            return providerTypeName;
        }
        [Serializable]
        public class ProviderTypeState
        {
            public bool loaded;
            public List<TypeNameInfo> list;
            [Serializable]
            public class TypeNameInfo
            {
                public string displayName;
                public string AssemblyName;
                public string TypeFullName;
                public string AssemblyFullName;
            }

            public void LoadAssemblies(string assemblyDir, string filePattern, string baseTypeName)
            {
                Regex regex = new Regex(filePattern, RegexOptions.IgnoreCase);


                var types = new List<Type>();
                types.Add(null);

                List<Assembly> loadedAssemblies = new List<Assembly>();
                AppDomain domain = null;
                foreach (var file in Directory.GetFiles(Path.GetFullPath(assemblyDir), "*", SearchOption.AllDirectories))
                {
                    string fileName = Path.GetFileName(file);
                    Match m = regex.Match(fileName);
                    if (!m.Success)
                        continue;
                    string assemblyName = Path.GetFileNameWithoutExtension(fileName);
                    Assembly assembly = null;
                    try
                    {
                        Assembly[] allAssemblies = AppDomain.CurrentDomain.GetAssemblies();
                        foreach (var ass in allAssemblies)
                        {
                            if (ass.IsDynamic)
                                continue;
                            if (string.Equals(ass.Location, file, StringComparison.InvariantCultureIgnoreCase))
                            {
                                assembly = ass;
                                break;
                            }
                        }
                        if (assembly == null)
                        {
                            foreach (var ass in allAssemblies)
                            {
                                if (ass.GetName().Name.Equals(assemblyName, StringComparison.InvariantCultureIgnoreCase))
                                {
                                    Debug.Log("name eq: " + assemblyName);
                                    assembly = ass;
                                    break;
                                }
                            }
                        }

                        if (assembly == null)
                        {

                            assembly = LoadAssembly(assemblyDir, assemblyName);
                            if (assembly == null)
                                continue;
                            loadedAssemblies.Add(assembly);

                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                        continue;
                    }

                    foreach (var type in GetTypes(assembly, baseTypeName))
                    {
                        types.Add(type);
                    }
                }

                types = types.OrderBy(o => o != null ? o.FullName : null)
                    .OrderBy(o => o != null ? o.Assembly.GetName().Name : null)
                    .ToList();

                list = new List<TypeNameInfo>();
                foreach (var type in types)
                {
                    if (type != null)
                    {
                        list.Add(new TypeNameInfo()
                        {
                            displayName = type.FullName + ", " + type.Assembly.GetName().Name,
                            AssemblyName = type.Assembly.GetName().Name,
                            TypeFullName = type.FullName,
                            AssemblyFullName = type.FullName + ", " + type.Assembly.GetName().Name,
                        });
                    }
                    else
                    {
                        list.Add(new TypeNameInfo() { displayName = "None" });
                    }
                }


            }
            public IEnumerable<Type> GetTypes(Assembly assembly, string baseTypeName)
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type.IsInterface || type.IsAbstract)
                        continue;
                    if (EnumerateParentTypes(type).FirstOrDefault(o => o.FullName.Equals(baseTypeName, StringComparison.InvariantCultureIgnoreCase)) == null)
                        continue;
                    yield return type;
                }
            }

            IEnumerable<Type> EnumerateParentTypes(Type type, bool includeSelf = false)
            {
                Type p = type.BaseType;
                if (includeSelf)
                    yield return type;
                while (p != null)
                {
                    yield return p;
                    p = p.BaseType;
                }
            }

            private Assembly LoadAssembly(string assemblyDir, string assemblyName)
            {
                var domain = AppDomain.CurrentDomain;

                Func<string, string> FindAssemblyPath = (assName) =>
                 {
                     foreach (var file in Directory.GetFiles(assemblyDir, "*", SearchOption.AllDirectories))
                     {
                         string fileName = Path.GetFileName(file);
                         if (!(fileName.Equals(assemblyName + ".dll", StringComparison.InvariantCultureIgnoreCase) ||
                         fileName.Equals(assemblyName, StringComparison.InvariantCultureIgnoreCase)))
                             continue;

                         return file;
                     }
                     return null;
                 };

                ResolveEventHandler Domain_AssemblyResolve = (sender, args) =>
                {
                    string path = FindAssemblyPath(args.Name);
                    if (!string.IsNullOrEmpty(path))
                    {
                        return Assembly.LoadFile(path);
                    }

                    return null;
                };


                Assembly assembly = null;

                foreach (var ass in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (ass.GetName().Name.Equals(assemblyName, StringComparison.InvariantCultureIgnoreCase))
                    {
                        assembly = ass;
                        break;
                    }
                }
                if (assembly == null)
                {
                    //string path = FindAssemblyPath(assemblyName);
                    //if (string.IsNullOrEmpty(path))
                    //    throw new Exception("not found assembly file: " + assemblyName+", baseDir:"+assemblyDir);
                    //assembly = Assembly.ReflectionOnlyLoadFrom(path);

                    try
                    {
                        domain.AssemblyResolve += Domain_AssemblyResolve;
                        assembly = domain.Load(assemblyName);
                    }
                    catch (Exception ex)
                    {
                        //Debug.LogException(ex);
                    }
                    finally
                    {
                        domain.AssemblyResolve -= Domain_AssemblyResolve;
                    }
                }
                return assembly;
            }

        }


        #region Scope


        #endregion

    }
}