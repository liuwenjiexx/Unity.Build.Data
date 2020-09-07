using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace UnityEngine.Data
{

    public class DataUtility
    {
        public delegate byte[] LoadDataDelegate(string className);



        public static void InitializeJsonData(Assembly dataAssembly, LoadDataDelegate loadData)
        {

            foreach (var type in dataAssembly.GetTypes())
            {
                string className = type.Name;
                //跳过非索引器类型，需要表配置Key
                if (!className.EndsWith("Indexer"))
                    continue;

                className = className.Substring(0, className.Length - 7);

                var insField = type.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                if (insField == null)
                    continue;
                var indexer = insField.GetGetMethod().Invoke(null, null);
                Debug.Assert(indexer != null, "indexer null");
                byte[] bytes = loadData(className);
                if (bytes == null || bytes.Length == 0)
                    continue;
                string json = Encoding.UTF8.GetString(bytes);

                if (string.IsNullOrEmpty(json))
                    continue;
                json = "{\"array\":" + json + "}";
                Type dataType;

                dataType = type.BaseType.GetGenericArguments()[1];

                var array = ArrayFromJson(json, dataType);
                if (array == null)
                {
                    continue;
                }

                var InitializeMethod = indexer.GetType().GetMethod("Initialize", BindingFlags.Public | BindingFlags.Instance)
                    .Invoke(indexer, new object[] { array });
            }

        }

        public static T[] ArrayFromArrayJson<T>(string arrayJson)
        {
            arrayJson = "{\"array\":" + arrayJson + "}";
            return ArrayFromJson<T>(arrayJson);
        }
        public static object ArrayFromArrayJson(string arrayJson, Type elmentType)
        {

            arrayJson = "{\"array\":" + arrayJson + "}";
            return ArrayFromArrayJson(arrayJson, elmentType);
        }

        public static T[] ArrayFromJson<T>(string json)
        {
            var tmp = JsonUtility.FromJson<JsonSerializableArray<T>>(json);
            if (tmp == null)
                return null;
            return tmp.Array;
        }
        public static object ArrayFromJson(string json, Type elmentType)
        {
            Type t;
            t = typeof(JsonSerializableArray<>).MakeGenericType(elmentType);
            var tmp = JsonUtility.FromJson(json, t);
            if (tmp == null)
                return null;
            FieldInfo arrayField = tmp.GetType().GetField("array", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            return arrayField.GetValue(tmp);
        }
        public static string ToJsonArray<T>(T[] array)
        {
            string json = JsonUtility.ToJson(new JsonSerializableArray<T>(array));
            return json;
        }

        //public static T[] FromJsonArray<T>(string json)
        //{
        //    JsonSerializableDictionary dic = new JsonSerializableDictionary();

        //    var tmp = JsonUtility.FromJson<JsonSerializableArray<T>>(json);
        //    return tmp;
        //}

        class JsonSerializableArray<T>
        {
            [SerializeField]
            private T[] array;

            public JsonSerializableArray(T[] array)
            {
                this.array = array;
            }

            public T[] Array { get { return array; } }

        }
    }

}