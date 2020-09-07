using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Build.Data
{

    public class DataTableInfo
    {
        public string Name;
        public string MappingName;

        public DataFieldInfo[] Columns;
        public string Description;
        public int OriginIndex;

        public DataTableFlags Flags;

        public DataTableInfo(IDictionary<string, object> parent)
        {
            Parameters = new HierarchyDictionary<string, object>(parent);
        }

        public IDictionary<string, object> Parameters { get; private set; }


        public bool TryGetColumn(string name, out DataFieldInfo c)
        {
            if (Columns == null)
            {
                c = null;
                return false;
            }
            c = Columns.Where(o => string.Equals(o.Name, name, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            return c != null;
        }
        public DataFieldInfo GetColumn(string name)
        {
            DataFieldInfo c;
            if (!TryGetColumn(name, out c))
                throw new Exception("not found column " + name);
            return c;
        }

        public void UpdateParameters()
        {
            Parameters["TableName"] = Name;
        }


    }

    public class DataFieldInfo
    {
        public DataFieldInfo(DataTableInfo table)
        {
            if (table == null)
                throw new ArgumentNullException(nameof(table));
            this.Table = table;
            Parameters = new HierarchyDictionary<string, object>(table.Parameters);
        }

        public DataTableInfo Table { get; private set; }

        public IDictionary<string, object> Parameters { get; private set; }

        public string Name;
        public string MappingName;
        public Type DataType;
        public string DataTypeName;
        public string Description;
        /// <summary>
        /// 数据表相对索引
        /// </summary>
        public int Index;
        /// <summary>
        /// 数据表原始索引
        /// </summary>
        public int OriginIndex;
        /// <summary>
        /// 序列化索引
        /// </summary>
        public int DataIndex;
        public bool HasDefaultValue;
        public object DefaultValue;
        public bool IsKey { get => (Flags & DataFieldFlags.Key) == DataFieldFlags.Key; }
        public string[] ArraySeparator;
        public string[] ObjectSeparator;
        public HashSet<string> Tags = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
        public DataFieldFlags Flags;

        public void UpdateParameters()
        {

        }
   
    }


    [Flags]
    public enum DataTableFlags
    {
        Class = 1 << 0,
        Struct = 1 << 1,
        Enum = 1 << 2,
        NoData = 1 << 3,
    }

    [Flags]
    public enum DataFieldFlags
    {
        None = 0,
        Key = 1 << 0,
        Exclude = 1 << 1,
    }


}
