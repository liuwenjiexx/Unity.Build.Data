using Build.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor.Callbacks;
using UnityEngine;
using UnityEngine.Localizations;
using Debug = UnityEngine.Debug;

namespace UnityEditor.Build.Data
{

    public static class EditorBuildData
    {
        public const string PackageName = "unity.build.data";
        public const string MenuPrefix = "Build/Data/";
        public const int Build_MenuPriority = 21;
        public const int Settings_MenuPriority = Build_MenuPriority + 20;
        private static string packageDir;
        private static LocalizationValues editorLocalizationValues;

        private static SettingsProvider<BuildDataConfig> settingsProvider;

        public static string PackageDir
        {
            get
            {
                if (string.IsNullOrEmpty(packageDir))
                {
                    packageDir = GetPackageDirectory(PackageName);
                }
                return packageDir;
            }
        }

        internal static SettingsProvider<BuildDataConfig> SettingsProvider
        {
            get
            {
                if (settingsProvider == null)
                {
                    settingsProvider = new SettingsProvider<BuildDataConfig>(PackageName, false, true);
                    settingsProvider.OnFirstCreateInstance = CreateDefaultConfig;
                    settingsProvider.OnLoadAfter = (o) => o.FileName = settingsProvider.FilePath;
                }
                return settingsProvider;
            }
        }

        public static BuildDataConfig Settings
        {
            get => SettingsProvider.Settings;
        }
        public static string SettingsPath
        {
            get => SettingsProvider.FilePath;
        }

        public static LocalizationValues EditorLocalizationValues
        {
            get
            {
                if (editorLocalizationValues == null)
                    editorLocalizationValues = new DirectoryLocalizationValues(Path.Combine(PackageDir, "Editor/Localization"));
                return editorLocalizationValues;
            }
        }


        static string ProgressBarTitle;

        /// <summary>
        /// 生成表结构和数据
        /// </summary>
        [MenuItem(MenuPrefix + "Build", priority = Build_MenuPriority)]
        public static void Build()
        {
            string args = "-code -data -config=\"" + Path.GetFullPath(SettingsPath) + "\"";
            ClearLog();
            ProgressBarTitle = "Build Code & Data";
            StartProcess(args);
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 生成数据
        /// </summary>
        [MenuItem(MenuPrefix + "Build Data", priority = Build_MenuPriority)]
        public static void BuildData()
        {
            string args = "-data -config=\"" + Path.GetFullPath(SettingsPath) + "\"";
            ClearLog();
            ProgressBarTitle = "Build Data";
            StartProcess(args);
            AssetDatabase.Refresh();
        }

        /// <summary>
        /// 生成表结构类
        /// </summary>
        [MenuItem(MenuPrefix + "Build Code", priority = Build_MenuPriority)]
        public static void BuildCode()
        {
            string args = "-code -config=\"" + Path.GetFullPath(SettingsPath) + "\"";
            ClearLog();
            ProgressBarTitle = "Build Code";
            StartProcess(args);
            AssetDatabase.Refresh();
        }

        [MenuItem(MenuPrefix + "Open Data Directory", priority = Settings_MenuPriority)]
        static void OpenDataDirectory_Menu()
        {
            string dir = Path.GetDirectoryName(SettingsPath);

            string path = Settings.Input.Directory;
            if (!string.IsNullOrEmpty(dir))
                path = Path.Combine(dir, path);
            path = Path.GetFullPath(path);
            EditorUtility.RevealInFinder(path);
        }

        [MenuItem(MenuPrefix + "Help", priority = Settings_MenuPriority + 20)]
        static void OpenREADME_Menu()
        {
            string assetPath = Path.Combine(PackageDir, "README.md");
            //AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath));
            Application.OpenURL(Path.GetFullPath(assetPath));
        }

        //[MenuItem(MenuPrefix + "Open Log File", priority = Config_MenuPriority)]
        //static void OpenLogFile_Menu()
        //{ 
        //    EditorUtility.RevealInFinder(logPath);
        //}
        static void StartProcess(string args)
        {
            string filePath = Path.Combine(PackageDir, "Editor/BuildData.exe");
            filePath = Path.GetFullPath(filePath);
            StartProcess(filePath, args, null);
        }


        private static string GetPackageDirectory(string packageName)
        {
            foreach (var dir in Directory.GetDirectories("Assets", "*", SearchOption.AllDirectories))
            {
                if (string.Equals(Path.GetFileName(dir), packageName, StringComparison.InvariantCultureIgnoreCase))
                {
                    return dir;
                }
            }

            string path = Path.Combine("Packages", packageName);
            if (Directory.Exists(path))
            {
                return path;
            }

            return null;
        }



        static void StartProcess(string filePath, string args, string workingDirectory)
        {
            EditorUtility.DisplayProgressBar(ProgressBarTitle, "", 0f);

            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.FileName = filePath;
            startInfo.Arguments = args;
            if (!string.IsNullOrEmpty(workingDirectory))
            {
                workingDirectory = Path.GetFullPath(workingDirectory);
            }
            else
            {
                workingDirectory = Path.GetFullPath(".");
            }
            startInfo.WorkingDirectory = workingDirectory;
            startInfo.CreateNoWindow = true;
            startInfo.UseShellExecute = false;
            startInfo.RedirectStandardOutput = true;
            //startInfo.StandardOutputEncoding = Encoding.UTF8;
            startInfo.RedirectStandardError = true;
            //startInfo.StandardErrorEncoding = Encoding.UTF8;
            DateTime startTime = DateTime.Now;
            using (var p = Process.Start(startInfo))
            {

                StringBuilder sb = new StringBuilder();
                var output = p.StandardOutput;

                string line;
                while ((line = output.ReadLine()) != null)
                {
                    sb.AppendLine(line);
                    EditorUtility.DisplayProgressBar(ProgressBarTitle, line, 0f);
                }

                string text = sb.ToString();

                if (!string.IsNullOrEmpty(text))
                {
                    int firstLine = text.IndexOf('\n');
                    bool success = true;
                    if (p.ExitCode != 0)
                    {
                        success = false;
                    }
                    if (firstLine >= 0)
                    {
                        string text2 = string.Format(" {0} ({1:0.#}s)", success ? "success" : "failure", (DateTime.Now - startTime).TotalSeconds);
                        text = text.Substring(0, firstLine) + text2 + text.Substring(firstLine);
                    }
                    if (success)
                        Log(text);
                    else
                        LogError(text);
                }
            }

            EditorUtility.ClearProgressBar();
        }
        static string logPath = "Logs/build.data.log";
        static void ClearLog()
        {
            if (File.Exists(logPath))
                File.Delete(logPath);
        }

        static void Log(string log)
        {
            Log(log, false);
        }
        static void LogError(string log)
        {
            Log(log, true);
        }

        static void Log(string log, bool error)
        {
            //string path = logPath;

            //string dir = Path.GetDirectoryName(path);
            //if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            //    Directory.CreateDirectory(dir);

            //File.AppendAllText(path, log, Encoding.UTF8);
            if (error)
            {
                Debug.LogError(log);
            }
            else
            {
                Debug.Log(log);
            }
        }

        public static BuildDataConfig CreateDefaultConfig()
        {
            BuildDataConfig config = new BuildDataConfig()
            {
                FileName = SettingsPath,
                Input = new InputDataConfig
                {
                    Directory = "Data",
                    TableName = "[^\\|]*\\|(?<result>.*)",
                    FileInclude = "\\.xlsx?$",
                    FileExclude = "~\\$",
                    Rows = new DataRowConfig[]
                    {
                        new DataRowConfig(){ Index=1,  Type= DataRowType.FieldSummary},
                        new DataRowConfig(){ Index=2,  Type= DataRowType.FieldName},
                        new DataRowConfig(){ Index=3,  Type= DataRowType.FieldType},
                        new DataRowConfig(){ Index=4,  Type= DataRowType.Keyword}
                    },
                    Provider = "Build.Data.MSExcel.ExcelDataReader, Build.Data.Provider.MSExcel"
                },
                OutputCode = new BuildCodeConfig
                {
                    Path = "Assets/Plugins/gen/Data.dll",
                    TypeName = "DATA_{$TableName}"
                },
                Output = new OutputDataConfig()
                {
                    Path = "Assets/Resources/Data",
                    Provider = "Build.Data.JsonDataWriter, BuildData"
                },
            };
            return config;
        }


        public static void SaveSettings()
        {
            SettingsProvider.Save();
        }


    }
}

