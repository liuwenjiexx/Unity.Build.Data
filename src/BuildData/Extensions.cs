using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Build.Data
{
    public static class Extensions
    {
        public static void ClearFileAttributes(this string path)
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
                File.SetAttributes(path, FileAttributes.Normal);
        }


        private static Regex formatStringRegex = new Regex("(?<!\\{)\\{\\$([^}:]*)(:([^}]*))?\\}(?!\\})");

        public static string FormatString(this string input, IDictionary<string, object> values)
        {
            return FormatString(input, null, values);
        }
        /// <summary>
        /// format:{$name:format} 
        /// </summary>
        /// <param name="input"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public static string FormatString(this string input, IFormatProvider formatProvider, IDictionary<string, object> values)
        {
            string result;

            result = formatStringRegex.Replace(input, (m) =>
            {
                string paramName = m.Groups[1].Value;
                string format = m.Groups[3].Value;
                object value;
                string ret = null;

                if (string.IsNullOrEmpty(paramName))
                    throw new FormatException("format error:" + m.Value);

                if (!values.TryGetValue(paramName, out value))
                    throw new ArgumentException("not found param name:" + paramName);

                if (value != null)
                {
                    ret = string.Format(formatProvider, "{0:" + format + "}", value);
                    //if (format.Length > 0)
                    //{
                    //    IFormattable formattable = value as IFormattable;
                    //    if (formattable != null)
                    //    {
                    //        ret = formattable.ToString(format, null);
                    //    }
                    //}
                    //if (ret == null)
                    //    ret = value.ToString();
                }
                else
                {
                    ret = string.Empty;
                }

                return ret;
            });
            return result;
        }
    }

    class HierarchyDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        IDictionary<TKey, TValue> parent;
        Dictionary<TKey, TValue> values;

        public HierarchyDictionary()
        {
            values = new Dictionary<TKey, TValue>();
        }

        public HierarchyDictionary(IDictionary<TKey, TValue> parent)
            : this()
        {
            this.parent = parent;
        }

        public TValue this[TKey key]
        {
            get
            {
                TValue value;
                if (!TryGetValue(key, out value))
                    throw new Exception("Not key " + key);
                return value;
            }
            set => values[key] = value;
        }

        public ICollection<TKey> Keys
        {
            get
            {
                return AllItems().Select(o => o.Key).ToList();
            }
        }
        private IEnumerable<KeyValuePair<TKey, TValue>> AllItems()
        {
            foreach (var item in values)
            {
                yield return item;
            }
            if (parent != null)
            {
                foreach (var item in parent)
                {
                    yield return item;
                }
            }
        }


        public ICollection<TValue> Values
        {
            get
            {
                return AllItems().Select(o => o.Value).ToList();
            }
        }

        public int Count
        {
            get
            {
                return AllItems().Count();
            }
        }

        public bool IsReadOnly => false;

        public void Add(TKey key, TValue value)
        {
            values.Add(key, value);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            Add(item.Key, item.Value);
        }

        public void Clear()
        {
            values.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return AllItems().Contains(item);
        }

        public bool ContainsKey(TKey key)
        {
            return AllItems().Where(o => object.Equals(o.Key, key)).Count() > 0;
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return AllItems().GetEnumerator();
        }

        public bool Remove(TKey key)
        {
            return values.Remove(key);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return values.Remove(item.Key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (values.TryGetValue(key, out value))
                return true;
            if (parent != null)
            {
                if (parent.TryGetValue(key, out value))
                    return true;
            }
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return AllItems().GetEnumerator();
        }
    }

}
