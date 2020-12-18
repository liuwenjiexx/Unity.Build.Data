//2020.4.24
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace UnityEditor.Build.Data
{

    /// <summary>
    /// Editor, Project: ProjectSettings/Packages/PackageName/TypeName.json
    /// Editor, User: UserSettings/Packages/PackageName/TypeName.json
    /// Runtime, Project: Resources/ProjectSettings/Packages/PackageName/TypeName.json
    /// Runtime, User: Resources/UserSettings/Packages/PackageName/TypeName.json
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal class SettingsProvider<T>
        where T : new()
    {
        private bool isRuntime;
        private bool isProject;
        private string packageName;
        private T settings;
        private Encoding encoding;
        private string filePath;
        private string baseDir;

        public Func<T> OnFirstCreateInstance;
        public Action<T> OnLoadAfter;

        public event PropertyChangedDelegate PropertyChanged;

        public delegate void PropertyChangedDelegate(object instance, string propertyName);

        public SettingsProvider(string packageName, bool isRuntime, bool isProject, string baseDir = null)
        {
            this.packageName = packageName;
            this.isRuntime = isRuntime;
            this.isProject = isProject;
            this.baseDir = baseDir;

        }

        public T Settings
        {
            get
            {
                if (settings == null)
                {
                    Load();
                }
                return settings;
            }
        }

        public string PackageName
        {
            get => packageName;
        }

        public bool IsRuntime
        {
            get => isRuntime;
        }

        public bool IsProject
        {
            get => isProject;
        }

        public Encoding Encoding
        {
            get => encoding ?? new UTF8Encoding(false);
            set => encoding = value;
        }

        public string FileName
        {
            get; set;
        }

        public string FilePath
        {
            get
            {
                if (string.IsNullOrEmpty(filePath))
                {

                    filePath = string.Empty;
                    if (string.IsNullOrEmpty(baseDir))
                    {
                        if (isRuntime)
                        {
                            filePath = "Assets/Resources";
                        }
                    }
                    else
                    {
                        filePath = baseDir;
                    }
                    if (isProject)
                    {
                        filePath = Path.Combine(filePath, "ProjectSettings");
                    }
                    else
                    {
                        filePath = Path.Combine(filePath, "UserSettings");
                    }
                    filePath = Path.Combine(filePath, "Packages", packageName);

                    if (!string.IsNullOrEmpty(FileName))
                        filePath = Path.Combine(filePath, FileName);
                    else
                        filePath = Path.Combine(filePath, "Settings.json");
                }
                return filePath;
            }
            set => filePath = value;
        }




        public void SetProperty<TValue>(string propertyName, ref TValue value, TValue newValue)
        {
            if (!object.Equals(value, newValue))
            {
                value = newValue;
                Save();
                PropertyChanged?.Invoke(Settings, propertyName);
            }
        }

        public void Load(T instance = default(T))
        {
            try
            {
                if (File.Exists(FilePath))
                {
                    try
                    {
                        string json = File.ReadAllText(FilePath, Encoding);

                        if (!string.IsNullOrEmpty(json))
                        {
                            this.settings = JsonUtility.FromJson<T>(json);
                        }
                        else
                        {
                            this.settings = new T();
                        }
                        lastWriteTime = File.GetLastWriteTimeUtc(FilePath);
                        EnableFileSystemWatcher();
                        OnLoadAfter?.Invoke(this.settings);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"load file <{FilePath}>", ex);
                    }
                }
                else
                {
                    if (OnFirstCreateInstance != null)
                    {
                        this.settings = OnFirstCreateInstance();
                        if (this.settings == null)
                            throw new Exception("OnFirstCreateInstance return null");
                    }
                    else
                    {
                        this.settings = new T();
                    }
                    OnLoadAfter?.Invoke(this.settings);
                    Save();
                }
            }
            catch { }
        }

        public void Save()
        {
            string filePath = FilePath;
            string json = JsonUtility.ToJson(Settings, true);
            if (!Directory.Exists(Path.GetDirectoryName(filePath)))
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            File.WriteAllText(filePath, json, Encoding);
            lastWriteTime = File.GetLastWriteTimeUtc(filePath);
            EnableFileSystemWatcher();
        }


        static FileSystemWatcher fsw;
        static DateTime lastWriteTime;

        public void EnableFileSystemWatcher()
        {
            if (fsw != null)
                return;
            try
            {
                string filePath = FilePath;
                string dir = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(dir))
                    return;
                string fileName = Path.GetFileName(filePath);
                fsw = new FileSystemWatcher();
                fsw.BeginInit();
                fsw.Path = dir;
                fsw.Filter = fileName;
                fsw.NotifyFilter = NotifyFilters.LastWrite;
                fsw.Changed += OnFileSystemWatcher;
                fsw.Deleted += OnFileSystemWatcher;
                fsw.Renamed += OnFileSystemWatcher;
                fsw.Created += OnFileSystemWatcher;
                fsw.IncludeSubdirectories = true;
                fsw.EnableRaisingEvents = true;
                fsw.EndInit();
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }

        public void OnFileSystemWatcher(object sender, FileSystemEventArgs e)
        {
            EditorApplication.delayCall += () =>
            {
                bool changed = true;
                string filePath = FilePath;
                if (File.Exists(filePath) && lastWriteTime == File.GetLastWriteTimeUtc(filePath))
                {
                    changed = false;
                }

                if (changed)
                {
                    settings = default(T);
                }
            };
        }

        public override string ToString()
        {
            return JsonUtility.ToJson(Settings, true);
        }
    }
}
