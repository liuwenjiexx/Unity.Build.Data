using Build.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor.GUIExtensions;
using UnityEngine;
#if __LOCALIZATION
using UnityEngine.Localizations;
#endif
namespace UnityEditor.Build.Data
{
    public class BuildDataConfigEditorWindow : EditorWindow
    {
        Vector2 scrollPos;
        string relativePath;
        GUIContent[] OutputDataFormats;

        public BuildDataConfig Config
        {
            get { return EditorBuildData.Settings; }
        }

        [MenuItem(EditorBuildData.MenuPrefix + "Settings", priority = EditorBuildData.Settings_MenuPriority)]
        public static void ShowWindow()
        {
            var win = GetWindow<BuildDataConfigEditorWindow>();
            win.Show();
        }

        private void OnEnable()
        {
#if __LOCALIZATION
            using (EditorBuildData.EditorLocalizationValues.BeginScope())
#endif
            {
                titleContent = new GUIContent("Build Data Config".Localization());
            }
        }



        private void OnGUI()
        {
            BuildDataConfig config = Config;
            relativePath = Path.GetDirectoryName(config.FileName);

#if __LOCALIZATION
            using (EditorBuildData.EditorLocalizationValues.BeginScope())
#endif
            {
                //    config.Enabled = EditorGUILayout.Toggle(new GUIContent("Enabled", "开启"), config.Enabled);

                //  GUI.enabled = config.Enabled;


                using (var sv = new GUILayout.ScrollViewScope(scrollPos))
                {

                    using (new GUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("Build Code".Localization()))
                        {
                            EditorBuildData.BuildCode();
                        }
                        if (GUILayout.Button("Build Data".Localization()))
                        {
                            EditorBuildData.BuildData();
                        }
                        if (GUILayout.Button("Build".Localization()))
                        {
                            EditorBuildData.Build();
                        }
                    }

                    GUIUserSettings();

                    using (var checker = new EditorGUI.ChangeCheckScope())
                    {
                        scrollPos = sv.scrollPosition;

                        if (config.Input == null)
                            config.Input = new InputDataConfig();
                        DrawInputConfig(config.Input);


                        if (config.OutputCode == null)
                            config.OutputCode = new BuildCodeConfig();
                        DrawCodeConfig(config.OutputCode);

                        if (config.Output == null)
                            config.Output = new OutputDataConfig();
                        DrawOutputConfig(config.Output);

                        if (checker.changed)
                        {
                            Save();
                        }

                    }
                }
            }
        }



        void DrawInputConfig(InputDataConfig inputConfig)
        {
            int selectedindex;

            BuildDataConfig config = Config;
            if (OutputDataFormats == null)
                OutputDataFormats = new GUIContent[] { new GUIContent("json") };

            GUILayout.Label("Input".Localization());
            using (new EditorGUILayoutx.Scopes.IndentLevelVerticalScope())
            {
                //using (new GUILayout.HorizontalScope())
                //{
                //    EditorGUILayout.PrefixLabel("Provider".Localization());
                //    inputConfig.Provider = EditorGUILayoutx.ProviderTypeName(inputConfig.Provider, EditorBuildData.PackageDir, "^Build\\.Data\\.Provider\\.(.*)\\.dll$|BuildData\\.exe", "Build.Data.DataReader");
                //}
                inputConfig.Provider = EditorGUILayout.TextField(new GUIContent("Provider".Localization()), inputConfig.Provider ?? string.Empty);

                inputConfig.Directory = new GUIContent("Directory".Localization(), "").FolderField(inputConfig.Directory, "Data Source Folder", relativePath: relativePath);
                inputConfig.FileInclude = EditorGUILayout.TextField(new GUIContent("File Include".Localization(), "Regex, excel(\\.xlsx?$)"), inputConfig.FileInclude ?? string.Empty);
                inputConfig.FileExclude = EditorGUILayout.TextField(new GUIContent("File Exclude".Localization(), "Regex, excel(~\\$)"), inputConfig.FileExclude ?? string.Empty);


                EditorGUILayout.LabelField("Table".Localization());
                using (new EditorGUILayoutx.Scopes.IndentLevelVerticalScope())
                {

                    inputConfig.TableName = EditorGUILayout.TextField(new GUIContent("Table Name".Localization()), inputConfig.TableName ?? string.Empty);
                    using (new GUILayout.HorizontalScope())
                    {
                        EditorGUILayout.PrefixLabel("Offset".Localization().Localization());
                        GUILayout.Label(new GUIContent("Row".Localization().Localization()), GUILayout.ExpandWidth(false));
                        inputConfig.OffsetRow = EditorGUILayout.IntField(inputConfig.OffsetRow);
                        GUILayout.Label(new GUIContent("Column".Localization().Localization()), GUILayout.ExpandWidth(false));
                        inputConfig.OffsetColumn = EditorGUILayout.IntField(inputConfig.OffsetColumn);
                    }
                }


                EditorGUILayout.LabelField("Field".Localization());
                using (new EditorGUILayoutx.Scopes.IndentLevelVerticalScope())
                {
                    inputConfig.TagInclude = EditorGUILayout.DelayedTextField(new GUIContent("Tag Include".Localization()), inputConfig.TagInclude ?? string.Empty);
                    inputConfig.TagExclude = EditorGUILayout.DelayedTextField(new GUIContent("Tag Exclude".Localization()), inputConfig.TagExclude ?? string.Empty);
                }

                EditorGUILayout.LabelField("Data".Localization());
                using (new EditorGUILayoutx.Scopes.IndentLevelVerticalScope())
                {
                    inputConfig.ArraySeparator = new GUIContent("Array Separator".Localization()).DelayedPlaceholderField(inputConfig.ArraySeparator ?? string.Empty, new GUIContent(InputDataConfig.DefaultArraySeparator));
                    inputConfig.ObjectSeparator = new GUIContent("Object Separator".Localization()).DelayedPlaceholderField(inputConfig.ObjectSeparator ?? string.Empty, new GUIContent(InputDataConfig.DefaultObjectSeparator));
                }



                if (inputConfig.Rows == null)
                    inputConfig.Rows = new DataRowConfig[0];

                Rect rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none);


                if (GUI.Button(new Rect(rect.xMax - 16, rect.y, 16, rect.height), "+", GUIStyle.none))
                {
                    GenericMenu menu = new GenericMenu();

                    foreach (DataRowType rowType in Enum.GetValues(typeof(DataRowType)))
                    {
                        if (inputConfig.Rows.FirstOrDefault(o => o.Type == rowType) == null)
                        {
                            menu.AddItem(new GUIContent(rowType.ToString()), false, (userData) =>
                            {
                                addRowType = (DataRowType)userData;
                                rowsFoldout = true;
                            }, rowType);
                        }
                    }

                    menu.ShowAsContext();
                }

                if (addRowType != 0)
                {

                    //Undo.undoRedoPerformed += () =>
                    //{
                    //    Debug.Log("   Undo.undoRedoPerformed " + addRowType);
                    //};
                    //Undo.willFlushUndoRecord += () =>
                    //{
                    //    Debug.Log(" Undo.willFlushUndoRecord " + addRowType);
                    //};
                    var dataRow = new DataRowConfig()
                    {
                        Type = addRowType
                    };
                    var rows = inputConfig.Rows;
                    ArrayUtility.Insert(ref inputConfig.Rows, inputConfig.Rows.Length, dataRow);
                    GUI.changed = true;
                    //Undo.IncrementCurrentGroup();
                    //Undo.SetCurrentGroupName("addRowType");

                    addRowType = 0;
                }

                rowsFoldout = EditorGUI.BeginFoldoutHeaderGroup(new Rect(rect.x, rect.y, rect.width - 20, rect.height), rowsFoldout, new GUIContent("Row Declaration".Localization()));
                //rowsFoldout=EditorGUILayout.BeginToggleGroup( new GUIContent("Rows"),rowsFoldout);


                EditorGUI.indentLevel++;
                if (rowsFoldout)
                {

                    foreach (var row in inputConfig.Rows.OrderBy(o => o.Index).ToArray())
                    {
                        int index = Array.FindIndex(inputConfig.Rows, o => o == row);
                        DrawDataRowConfig(inputConfig, row, index);
                    }

                }
                EditorGUI.indentLevel--;
                EditorGUI.EndFoldoutHeaderGroup();
                //EditorGUILayout.EndToggleGroup();

            }
        }

        bool rowsFoldout;
        DataRowType addRowType;

        int deleteRowIndex = -1;
        void DrawDataRowConfig(InputDataConfig inputConfig, DataRowConfig row, int index)
        {

            using (new GUILayout.HorizontalScope())
            {
                GUILayout.Space(16 * EditorGUI.indentLevel);
                var oldIndentLevel = EditorGUI.indentLevel;
                EditorGUI.indentLevel = 0;
                using (new GUILayout.VerticalScope("box"))
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(row.Type.ToString().Localization());
                        GUIStyle style = new GUIStyle("label");
                        style.fontSize = (int)(style.fontSize * 1.2f);
                        style.padding.top = 0;
                        style.padding.right = 0;
                        style.margin.right = 0;
                        if (GUILayout.Button("◥", style, GUILayout.ExpandWidth(false)))
                        {
                            GenericMenu menu = new GenericMenu();
                            menu.AddItem(new GUIContent("Delete".Localization()), false, () =>
                            {
                                deleteRowIndex = index;
                            });
                            menu.ShowAsContext();
                        }
                        GUILayout.Space(10);
                    }
                    EditorGUI.indentLevel++;
                    row.Index = EditorGUILayout.DelayedIntField("Index".Localization(), row.Index);
                    row.Pattern = EditorGUILayout.DelayedTextField("Pattern".Localization(), row.Pattern);
                    row.ValuePattern = EditorGUILayout.DelayedTextField("Value Pattern".Localization(), row.ValuePattern);

                    EditorGUI.indentLevel--;
                }
                EditorGUI.indentLevel = oldIndentLevel;
            }


            if (deleteRowIndex >= 0 && deleteRowIndex < inputConfig.Rows.Length)
            {
                ArrayUtility.RemoveAt(ref inputConfig.Rows, deleteRowIndex);
                deleteRowIndex = -1;
                GUI.changed = true;
            }
        }

        void DrawCodeConfig(BuildCodeConfig codeConfig)
        {
            EditorGUILayout.LabelField("Generate Code".Localization());
            EditorGUI.indentLevel++;
            codeConfig.outputDir = EditorGUILayout.TextField(new GUIContent("OutputDir".Localization()), codeConfig.outputDir ?? string.Empty);
            //codeConfig.outputDir = new GUIContent("OutputDir".Localization(), "").FileField(codeConfig.outputDir ?? string.Empty, "dll", "Build Code File", relativePath: relativePath);
            codeConfig.assemblyName = EditorGUILayout.TextField(new GUIContent("Assembly Name".Localization()), codeConfig.assemblyName ?? string.Empty);
            codeConfig.Namespace = EditorGUILayout.TextField(new GUIContent("Namespace".Localization()), codeConfig.Namespace ?? string.Empty);
            codeConfig.TypeName = EditorGUILayout.TextField(new GUIContent("TypeName".Localization()), codeConfig.TypeName ?? string.Empty);
            codeConfig.template = EditorGUILayout.TextField(new GUIContent("Template".Localization()), codeConfig.template ?? string.Empty);
            codeConfig.format = (CodeFormat)EditorGUILayout.EnumPopup(new GUIContent("Format".Localization()), codeConfig.format);
            codeConfig.genIndexerClass = EditorGUILayout.Toggle("Indexer Class".Localization(), codeConfig.genIndexerClass);
            EditorGUI.indentLevel--;
        }

        public void DrawOutputConfig(OutputDataConfig outputConfig)
        {

            EditorGUILayout.LabelField("Output".Localization());
            using (new EditorGUILayoutx.Scopes.IndentLevelVerticalScope())
            {
                //using (new GUILayout.HorizontalScope())
                //{
                //    EditorGUILayout.PrefixLabel("Provider".Localization());
                //    outputConfig.Provider = EditorGUILayoutx.ProviderTypeName(outputConfig.Provider, EditorBuildData.PackageDir, "^Build\\.Data\\.Provider\\.(.*)\\.dll$|BuildData\\.exe", "Build.Data.DataWriter");
                //}
                outputConfig.Provider = EditorGUILayout.TextField(new GUIContent("Provider".Localization()), outputConfig.Provider ?? string.Empty);

                outputConfig.Path = new GUIContent("Path".Localization(), "").FolderField(outputConfig.Path ?? string.Empty, "Output Data Folder", relativePath: relativePath);
                int oldIndentLevel;
                //using (new GUILayout.HorizontalScope())
                //{
                //    EditorGUILayout.PrefixLabel(new GUIContent("Format", ""));
                //    oldIndentLevel = EditorGUI.indentLevel;
                //    EditorGUI.indentLevel = 0;
                //    selectedindex = Array.FindIndex(OutputDataFormats, o => string.Equals(o.text, outputConfig.Foramt, StringComparison.InvariantCultureIgnoreCase));
                //    selectedindex = EditorGUILayout.Popup(selectedindex, OutputDataFormats);
                //    if (selectedindex != -1)
                //        outputConfig.Foramt = OutputDataFormats[selectedindex].text;
                //    EditorGUI.indentLevel = oldIndentLevel;
                //}
            }
        }

        public void GUIUserSettings()
        {
            using (var checker = new EditorGUI.ChangeCheckScope())
            {
                UserBuildDataSettings.AutoBuildCode = EditorGUILayout.Toggle("Auto Build Code".Localization(), UserBuildDataSettings.AutoBuildCode);
                UserBuildDataSettings.AutoBuildData = EditorGUILayout.Toggle("Auto Build Data".Localization(), UserBuildDataSettings.AutoBuildData);
                if (checker.changed)
                {
                    if (UserBuildDataSettings.AutoBuildCode || UserBuildDataSettings.AutoBuildData)
                    {
                        UserBuildDataSettings.EnableInputListener();
                    }
                    else
                    {
                        UserBuildDataSettings.DisableInputListener();
                    }
                }
            }
        }



        void Save()
        {
            EditorBuildData.SaveSettings();
        }

    }





}