using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Build.Data
{

    public class EditorPrefsSettingsProvider<T> : SettingsProvider
        where T : EditorPrefsSettingsProvider<T>
    {
        [NonSerialized]
        private SettingsScope prefsScope;

        private static T instanceUser;
        private static T instanceProject;

        public EditorPrefsSettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null)
            : base(path, scopes, keywords)
        {
        }


        public static T UserSettings
        {
            get
            {
                if (instanceUser == null)
                {
                    instanceUser = Load(GetPrefsKey(), SettingsScope.User);
                }
                return instanceUser;
            }
        }


        public static T ProjectSettings
        {
            get
            {
                if (instanceProject == null)
                {
                    instanceProject = Load(GetPrefsKey(), SettingsScope.Project);
                }
                return instanceProject;
            }
        }

        static string GetPrefsKey()
        {
            string prefsKey = "Settings:" + typeof(T).FullName;
            return prefsKey;
        }

        static T Load(string prefsKey, SettingsScope prefsScope)
        {

            T instance = default(T);

            try
            {
                string str;
                if (prefsScope == SettingsScope.User)
                {
                    str = EditorPrefs.GetString(prefsKey, null);
                }
                else
                {
                    str = PlayerPrefs.GetString(prefsKey, null);
                }
                if (!string.IsNullOrEmpty(str))
                {
                    instance = JsonUtility.FromJson<T>(str);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }

            if (instance == null)
            {
                instance = (T)Activator.CreateInstance(typeof(T));
                instance.prefsScope = prefsScope;
            }

            return instance;
        }

        public void Save()
        {
            string json = JsonUtility.ToJson(this);
            string prefsKey = GetPrefsKey();
            if (prefsScope == SettingsScope.User)
            {
                EditorPrefs.SetString(prefsKey, json);
            }
            else
            {
                PlayerPrefs.SetString(prefsKey, json);
                PlayerPrefs.Save();
            }
        }
    }
}
