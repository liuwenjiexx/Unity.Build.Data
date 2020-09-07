using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace UnityEngine.Localizations
{
    //2020.3.31
    class HierarchyDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        private IDictionary<TKey, TValue> parent;
        private Dictionary<TKey, TValue> local;

        public HierarchyDictionary()
        {
            local = new Dictionary<TKey, TValue>();
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
            set
            {
                Set(key, value);
            }
        }

        public ICollection<TKey> Keys
        {
            get
            {
                return AllItems().Select(o => o.Key).Distinct().ToList();
            }
        }
        private IEnumerable<KeyValuePair<TKey, TValue>> AllItems()
        {
            foreach (var item in local)
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
                int count = local.Count;
                if (parent != null)
                    count += parent.Count;
                return count;
            }
        }

        public bool IsReadOnly => false;

        public void AddLocal(TKey key, TValue value)
        {
            local.Add(key, value);
        }

        public void Add(TKey key, TValue value)
        {
            AddLocal(key, value);
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            AddLocal(item.Key, item.Value);
        }

        public void AddRangeLocal(IDictionary<TKey, TValue> items)
        {
            foreach (var item in items)
                local.Add(item.Key, item.Value);
        }

        public void SetLocal(TKey key,TValue value)
        {
            local[key] = value;
        }

        public void Set(TKey key, TValue value)
        {
            if (ContainsKeyLocal(key))
            {
                local[key] = value;
            }
            else
            {
                if (parent != null && parent.ContainsKey(key))
                    parent[key] = value;
                else
                    local[key] = value;
            }
        }

        public void ClearLocal()
        {
            local.Clear();
        }

        public void Clear()
        {
            ClearLocal();
            if (parent != null)
                parent.Clear();
        }
        public bool ContainsLocal(KeyValuePair<TKey, TValue> item)
        {
            if (local.Contains(item))
                return true;
            return false;
        }
        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            if (ContainsLocal(item))
                return true;
            if (parent != null)
                return parent.Contains(item);
            return false;
        }
        public bool ContainsKeyLocal(TKey key)
        {
            if (local.ContainsKey(key))
                return true;
            return false;
        }

        public bool ContainsKey(TKey key)
        {
            if (ContainsKeyLocal(key))
                return true;
            if (parent != null)
                return parent.ContainsKey(key);
            return false;
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return AllItems().GetEnumerator();
        }

        public bool RemoveLocal(TKey key)
        {
            if (local.Remove(key))
                return true;
            return false;
        }

        public bool Remove(TKey key)
        {
            if (RemoveLocal(key))
                return true;
            if (parent != null)
                return parent.Remove(key);
            return false;
        }

        public bool RemoveLocal(KeyValuePair<TKey, TValue> item)
        {
            return RemoveLocal(item.Key);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            return Remove(item.Key);
        }

        public bool TryGetValueLocal(TKey key, out TValue value)
        {
            if (local.TryGetValue(key, out value))
                return true;
            return false;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (TryGetValueLocal(key, out value))
                return true;
            if (parent != null)
                return parent.TryGetValue(key, out value);
            return false;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return AllItems().GetEnumerator();
        }
    }

}