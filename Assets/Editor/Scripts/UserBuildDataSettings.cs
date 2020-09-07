using Build.Data;
using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace UnityEditor.Build.Data
{
    public class UserBuildDataSettings
    {
        [SerializeField]
        private bool autoBuildCode;
        [SerializeField]
        private bool autoBuildData;
        private static bool? changed;
        private static SettingsProvider<UserBuildDataSettings> settingsProvider;

        public UserBuildDataSettings()
        {
        }


        static bool Changed
        {
            get
            {
                if (changed == null)
                    changed = PlayerPrefs.GetInt(EditorBuildData.PackageName + ".changed", 0) != 0;
                return changed.Value;
            }
            set
            {
                if (Changed != value)
                {
                    changed = value;
                    PlayerPrefs.SetInt(EditorBuildData.PackageName + ".changed", value ? 1 : 0);
                    PlayerPrefs.Save();
                }
            }
        }
        static BuildDataConfig BuildConfig
        {
            get => EditorBuildData.Settings;
        }

        public static bool AutoBuildCode
        {
            get => Settings.autoBuildCode;
            set => Provider.SetProperty(nameof(AutoBuildCode), ref Settings.autoBuildCode, value);
        }
        public static bool AutoBuildData
        {
            get => Settings.autoBuildData;
            set => Provider.SetProperty(nameof(AutoBuildData), ref Settings.autoBuildData, value);
        }

        private static SettingsProvider<UserBuildDataSettings> Provider
        {
            get
            {
                if (settingsProvider == null)
                {
                    settingsProvider = new SettingsProvider<UserBuildDataSettings>(EditorBuildData.PackageName, false, false);
                }
                return settingsProvider;
            }
        }

        private static UserBuildDataSettings Settings
        {
            get
            {
                return Provider.Settings;
            }
        }

        static FileSystemWatcher fswInput;

        [InitializeOnLoadMethod]
        static void InitializeOnLoadMethod()
        {
            if (AutoBuildCode || AutoBuildData)
            {
                EnableInputListener();
            }
        }

        public static void EnableInputListener()
        {

            if (fswInput == null && BuildConfig.Input != null && !string.IsNullOrEmpty(BuildConfig.Input.Directory))
            {
                string fullDir = Path.GetFullPath(BuildConfig.Input.Directory);
                if (Directory.Exists(fullDir))
                {
                    fswInput = new FileSystemWatcher();
                    fswInput.BeginInit();
                    fswInput.Path = fullDir;
                    fswInput.Filter = "*";
                    fswInput.NotifyFilter = NotifyFilters.LastWrite;
                    fswInput.Changed += OnFileSystemWatcher;
                    fswInput.Deleted += OnFileSystemWatcher;
                    fswInput.Renamed += OnFileSystemWatcher;
                    fswInput.Created += OnFileSystemWatcher;
                    fswInput.IncludeSubdirectories = true;
                    fswInput.EnableRaisingEvents = true;
                    fswInput.EndInit();
                }
                else
                {
                    Debug.LogError("dir not exits. " + fullDir);
                }
            }
        }

        public static void DisableInputListener()
        {
            if (fswInput != null)
            {
                fswInput.Dispose();
                fswInput = null;
            }
        }

        static void OnFileSystemWatcher(object sender, FileSystemEventArgs e)
        {
            string filePath = e.FullPath;
            EditorApplication.delayCall += () =>
            {
                if (BuildConfig.Input != null)
                {
                    bool changed = true;
                    if (!string.IsNullOrEmpty(BuildConfig.Input.FileInclude) && !new Regex(BuildConfig.Input.FileInclude).IsMatch(filePath))
                    {
                        changed = false;
                    }
                    if (!string.IsNullOrEmpty(BuildConfig.Input.FileExclude) && new Regex(BuildConfig.Input.FileExclude).IsMatch(filePath))
                    {
                        changed = false;
                    }

                    if (changed)
                    {
                        Changed = true;
                        EditorApplication.delayCall += Update;
                    }
                }
            };
        }



        static void Update()
        {
            if (!(EditorApplication.isPlaying || EditorApplication.isUpdating || EditorApplication.isCompiling))
            {
                if (Changed)
                {
                    Changed = false;
                    bool buildCode = AutoBuildCode, buildData = AutoBuildData;

                    if (buildCode && buildData)
                    {
                        EditorBuildData.Build();
                    }
                    else if (buildCode)
                    {
                        EditorBuildData.BuildCode();
                    }
                    else if (buildData)
                    {
                        EditorBuildData.BuildData();
                    }
                }
            }

            if (Changed)
                EditorApplication.delayCall += Update;
        }

    }

}
