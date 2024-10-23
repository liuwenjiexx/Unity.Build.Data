using Build.Data;
using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Configuration.Assemblies;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace Build.Data
{

    /// <summary>
    /// 数据源提供程序
    /// </summary>
    public abstract class DataReader : IDisposable
    {
        private bool isOpened;
        private Dictionary<string, DataTableInfo> dataTables;
        private static Dictionary<string, Regex> cachedRegex;
        private Dictionary<string, Type> typeNameToType;
        private static Dictionary<string, Type> globalTypeNameToType;
        private static Regex keywordRegex = new Regex("\\$?(?<keyword>[^\\(\\s]+)\\s*(\\((?<param>.*?)\\))?");

        private BuildDataConfig config;
        private IDictionary<string, object> parameters = new Dictionary<string, object>();

        private int arrayDepth;
        private int objectDepth;
        private string[] arraySeparators;
        private string[] objectSeparators;
        private bool isReadData;
        private Stack<DepthScope> scopes;
        private Dictionary<string, string> typeMappings;
        public Type[] allTypes;
        public Dictionary<Type, DataTableInfo> allTypeToDataTables;
        private static List<IDataConverter> converters;


        public DataReader()
        {

        }

        public BuildDataConfig Config { get => config; }

        public IDictionary<string, object> Parameters { get => parameters; }

        public bool IsOpened { get => isOpened; }



        public IEnumerable<DataTableInfo> DataTableInfos
        {
            get
            {
                if (!isOpened)
                    throw new Exception("not read");
                return dataTables.Values;
            }
        }


        public int Depth { get => arrayDepth; }
        public string[] ArraySeparators { get => arraySeparators; }
        public string[] ObjectSeparators { get => objectSeparators; }


        public virtual void LoadConfig(BuildDataConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            this.config = config;

            arraySeparators = null;
            objectSeparators = null;
            if (!string.IsNullOrEmpty(config.Input.ArraySeparator))
            {
                arraySeparators = config.Input.ArraySeparator.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            }
            if (arraySeparators == null || arraySeparators.Length == 0)
                arraySeparators = InputDataConfig.DefaultArraySeparator.Split(' ');

            if (!string.IsNullOrEmpty(config.Input.ObjectSeparator))
            {
                objectSeparators = config.Input.ObjectSeparator.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            }
            if (objectSeparators == null || objectSeparators.Length == 0)
                objectSeparators = InputDataConfig.DefaultObjectSeparator.Split(' ');

            typeMappings = new Dictionary<string, string>();
            if (config.TypeMappings != null)
            {
                foreach (var item in config.TypeMappings)
                    typeMappings[item.Mapping] = item.Name;
            }
        }

        public void LoadConfig(string configFile)
        {
            BuildDataUtility.LoadConfigFromFile(configFile);
        }


        #region Read

        public virtual void Open()
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (isOpened)
                return;
            isOpened = true;

            dataTables = new Dictionary<string, DataTableInfo>(StringComparer.InvariantCultureIgnoreCase);

            foreach (var tableInfo in ReadDataTableInfo())
            {
                dataTables.Add(tableInfo.Name, tableInfo);
            }
            scopes = new Stack<DepthScope>();
        }

        protected abstract IEnumerable<DataTableInfo> ReadDataTableInfo();

        public abstract IEnumerable<object[]> ReadRows(string tableName);

        public IEnumerable<object> LoadDataObjects(string tableName, Type type)
        {
            var tableInfo = GetTableInfo(tableName);
            if (tableInfo == null)
                throw new Exception("not table " + tableName);

            Console.WriteLine($"load data table <{tableInfo.Name}> type <{type.FullName}>");

            DataMemberCollection members = DataMember.GetMembers(type);
            var columns = tableInfo.Columns;
            int columnCount = columns.Length;
            DataMember[] column_member = new DataMember[columnCount];


            for (int i = 0; i < columnCount; i++)
            {
                var col = columns[i];
                if (!members.Contains(col.Name))
                    continue;
                var member = members[col.Name];
                column_member[i] = member;
            }
            int rowCount = 0;

            isReadData = true;

            foreach (var row in ReadRows(tableName))
            {
                object obj = Activator.CreateInstance(type);
                for (int i = 0; i < columnCount; i++)
                {
                    var column = columns[i];

                    DataMember member = column_member[i];
                    if (member == null)
                        continue;
                    object value = row[i];
                    //     if (column.DataType != member.ValueType)
                    {
                        try
                        {
                            value = ChangeType(column, value, member.ValueType, column);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"ChangeType Error. table [{tableName}] row <{rowCount}> column <{column.Name}> index <{column.OriginIndex}> value <{value}> type <{member.ValueType}>", ex);
                        }
                    }
                    member.SetValue(obj, value);
                }
                rowCount++;
                yield return obj;
            }

            isReadData = false;
        }

        #endregion

        #region Write


        #endregion

        public object GetDefaultValue(Type type)
        {
            object defaultValue;
            if (type == typeof(string))
                defaultValue = null;
            else
            if (type.IsValueType)
                defaultValue = Activator.CreateInstance(type);
            else
                defaultValue = null;
            return defaultValue;
        }


        public void ResetDepth()
        {
            arrayDepth = 0;
            objectDepth = 0;
        }



        /// <summary>
        /// 开始数组序列化
        /// </summary>
        /// <returns></returns>
        public IDisposable BeginArray()
        {
            var scope = new DepthScope(this, true);
            scopes.Push(scope);
            return scope;
        }
        /// <summary>
        /// 开始对象序列化
        /// </summary>
        /// <returns></returns>
        public IDisposable BeginObject()
        {
            var scope = new DepthScope(this, false);
            scopes.Push(scope);
            return scope;
        }

        class DepthScope : IDisposable
        {
            private DataReader owner;
            public bool isArray;

            public DepthScope(DataReader owner, bool isArray)
            {
                this.owner = owner;
                this.isArray = isArray;
                if (isArray)
                    owner.arrayDepth++;
                else
                    owner.objectDepth++;
            }

            public void Dispose()
            {
                if (owner.scopes.Peek() != this)
                    throw new Exception("scope dispose error");
                owner.scopes.Pop();
                if (isArray)
                    owner.arrayDepth--;
                else
                    owner.objectDepth--;
            }
        }

        public string GetSeparator(DataFieldInfo field)
        {
            int index;
            if (scopes.Count == 0)
                throw new Exception($"call only in <{nameof(BeginObject)}> or  <{nameof(BeginArray)}>");
            bool isArray = scopes.Peek().isArray;
            if (isArray)
            {
                index = arrayDepth - 1;
            }
            else
            {
                index = objectDepth - 1;
            }

            if (index < 0)
                index = 0;
            string[] separators;
            if (isArray)
            {
                separators = field.ArraySeparator;
                if (separators == null || separators.Length <= 0)
                {
                    separators = ArraySeparators;
                }
                if (index >= separators.Length)
                    throw new Exception($"separator depth overflow, array depth <{arrayDepth}>,  length <{separators.Length}>");
            }
            else
            {
                separators = field.ObjectSeparator;
                if (separators == null || separators.Length <= 0)
                {
                    separators = ObjectSeparators;
                }
                if (index >= separators.Length)
                    throw new Exception($"separator depth overflow, object depth <{objectDepth}>, length <{separators.Length}>");
            }


            return separators[index];
        }

        public virtual void Close()
        {
            if (isOpened)
            {
                isOpened = false;
            }
        }

        public void CheckFilePath(string path)
        {
            if (!File.Exists(path))
                throw new Exception("file not exists " + path);
        }


        public DataTableInfo GetTableInfo(string tableName)
        {
            //if (tableConfig == null)
            //    throw new ArgumentNullException(nameof(tableConfig));
            DataTableInfo tableInfo;
            if (dataTables.TryGetValue(tableName, out tableInfo))
                return tableInfo;
            return null;
        }



        protected void ParseKeyword(DataTableInfo tableInfo, DataFieldInfo fieldInfo, string strKeyword)
        {
            if (string.IsNullOrEmpty(strKeyword))
                return;
            foreach (Match m in keywordRegex.Matches(strKeyword))
            {
                string keyword = m.Groups["keyword"].Value.ToLower();
                string param = m.Groups["param"].Value;
                switch (keyword)
                {
                    case "enum":
                        tableInfo.Flags |= DataTableFlags.Enum;
                        break;
                    case "class":
                        tableInfo.Flags |= DataTableFlags.Class;
                        break;
                    case "struct":
                        tableInfo.Flags |= DataTableFlags.Struct;
                        break;
                    case "key":
                        fieldInfo.Flags |= DataFieldFlags.Key;
                        break;
                    case "nodata":
                        tableInfo.Flags |= DataTableFlags.NoData;
                        break;
                    case "arr_sep":
                        if (!string.IsNullOrEmpty(param))
                        {
                            fieldInfo.ArraySeparator = param.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        }
                        break;
                    case "obj_sep":
                        if (!string.IsNullOrEmpty(param))
                        {
                            fieldInfo.ObjectSeparator = param.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        }
                        break;
                    case "index":
                        int index;
                        if (!int.TryParse(param, out index))
                        {
                            throw new Exception("table " + tableInfo.Name + ", field: " + fieldInfo.Name + ", parse index error: " + param);
                        }
                        fieldInfo.DataIndex = index;
                        break;
                    case "exclude":
                        fieldInfo.Flags |= DataFieldFlags.Exclude;
                        break;
                    case "client":
                    case "server":
                        fieldInfo.Tags.Add(keyword);
                        break;
                    case "tag":
                        fieldInfo.Tags.Add(param.Trim().ToLowerInvariant());
                        break;
                    case "default":

                        if (fieldInfo.DataType != null && fieldInfo.DataType.IsPrimitive)
                        {
                            if (param.ToLower() == "null")
                                fieldInfo.DefaultValue = GetDefaultValue(fieldInfo.DataType);
                            else
                                fieldInfo.DefaultValue = Convert.ChangeType(param, fieldInfo.DataType);
                        }
                        else
                        {
                            if (param.ToLower() == "null")
                                fieldInfo.DefaultValue = null;
                            else
                                fieldInfo.DefaultValue = param;
                        }
                        fieldInfo.HasDefaultValue = true;
                        break;
                }
            }
        }

        public bool CheckFieldTagInclude(BuildDataConfig config, DataFieldInfo field)
        {
            if (config.Input == null)
                return true;

            if (!string.IsNullOrEmpty(config.Input.TagInclude))
            {
                bool contains = true;
                foreach (var tag in config.Input.TagInclude.Trim().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (!field.Tags.Contains(tag))
                    {
                        contains = false;
                        break;
                    }
                }
                if (!contains)
                    return false;
            }

            if (!string.IsNullOrEmpty(config.Input.TagExclude))
            {
                bool contains = false;
                foreach (var tag in config.Input.TagExclude.Trim().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    if (field.Tags.Contains(tag))
                    {
                        contains = true;
                        break;
                    }
                }
                if (contains)
                    return false;
            }
            return true;
        }

        public string GetStringPatternResult(string text, string pattern)
        {
            string result = text;
            if (!string.IsNullOrEmpty(pattern))
            {
                var regex = CachedRegex(pattern);
                var m = regex.Match(text);
                if (m.Success)
                {
                    var g = m.Groups["result"];
                    if (g.Success)
                        result = g.Value;
                    else
                        result = string.Empty;
                }
                else
                {
                    result = string.Empty;
                }
            }
            return result;
        }

        protected Regex CachedRegex(string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return null;
            Regex regex;
            if (cachedRegex == null)
                cachedRegex = new Dictionary<string, Regex>();
            if (!cachedRegex.TryGetValue(pattern, out regex))
            {
                regex = new Regex(pattern, RegexOptions.IgnoreCase);
                if (cachedRegex == null)
                    cachedRegex = new Dictionary<string, Regex>();
                cachedRegex[pattern] = regex;
            }
            return regex;
        }

        public object ChangeType(object value, Type type)
        {
            if (value == null)
            {
                return GetDefaultValue(type);
            }

            if (value is string)
            {
                if (string.IsNullOrEmpty(value as string))
                    return GetDefaultValue(type);
            }
            object result;
            DefaultConverter.instance.ConvertTo(this, null, value, type, out result);
            return result;
        }

        public object ChangeType(object value, Type type, object defaultValue)
        {
            if (value == null)
            {
                return defaultValue;
            }

            if (value is string)
            {
                if (string.IsNullOrEmpty(value as string))
                    return defaultValue;
            }
            return ChangeType(value, type);
        }


        public object ChangeType(DataFieldInfo fieldInfo, object value, Type type, object defaultValue)
        {
            if (value == null)
            {
                return defaultValue;
            }

            if (value is string)
            {
                if (string.IsNullOrEmpty(value as string))
                    return defaultValue;
            }

            object obj = ChangeType(fieldInfo, value, type);
            return obj;
        }
        public object ChangeType(DataFieldInfo fieldInfo, object value, Type type, DataFieldInfo columnInfo)
        {

            if (value == null)
            {
                return ChangeType(fieldInfo, columnInfo.DefaultValue, type);
            }

            if (value is string)
            {
                if (string.IsNullOrEmpty(value as string))
                    return columnInfo.DefaultValue;
            }

            object obj = ChangeType(fieldInfo, value, type);
            return obj;
        }

        public void AddType(Type type, string typeName)
        {
            if (typeNameToType == null)
                typeNameToType = new Dictionary<string, Type>();
            typeNameToType[typeName] = type;
        }

        public string GetTypeName(string typeName)
        {
            if (typeMappings.ContainsKey(typeName))
                typeName = typeMappings[typeName];

            return typeName;
        }

        public Type TypeNameToType(string typeName)
        {
            Type type;

            if (typeName == null)
                return null;
            typeName = typeName.Trim();
            if (typeName.EndsWith("[]"))
            {
                Type elementType = TypeNameToType(typeName.Substring(0, typeName.Length - 2));
                if (elementType == null)
                    return null;
                return elementType.MakeArrayType();
            }

            if (typeNameToType != null && typeNameToType.TryGetValue(typeName, out type))
                return type;
            if (GetGlobalTypeNameToType().TryGetValue(typeName, out type))
                return type;
            return null;
        }


        public DataTableInfo TryTypeToDataTableInfo(Type type)
        {
            if (allTypeToDataTables == null)
                return null;
            DataTableInfo dataTable;
            allTypeToDataTables.TryGetValue(type, out dataTable);
            return dataTable;
        }

        public string TableNameToTypeName(string tableName)
        {
            if (!string.IsNullOrEmpty(config.OutputCode.TypeName))
            {
                IDictionary<string, object> ps = new HierarchyDictionary<string, object>(parameters);
                ps["TableName"] = tableName;
                tableName = config.OutputCode.TypeName.FormatString(ps);
            }
            return tableName;
        }

        public static Dictionary<string, Type> GetGlobalTypeNameToType()
        {
            if (globalTypeNameToType == null)
            {
                globalTypeNameToType = new Dictionary<string, Type>(StringComparer.InvariantCultureIgnoreCase);
                globalTypeNameToType["int8"] = typeof(sbyte);
                globalTypeNameToType["int16"] = typeof(short);
                globalTypeNameToType["int32"] = typeof(int);
                globalTypeNameToType["int"] = typeof(int);
                globalTypeNameToType["int64"] = typeof(long);
                globalTypeNameToType["long"] = typeof(long);
                globalTypeNameToType["uint8"] = typeof(byte);
                globalTypeNameToType["byte"] = typeof(byte);
                globalTypeNameToType["uint16"] = typeof(ushort);
                globalTypeNameToType["uint32"] = typeof(uint);
                globalTypeNameToType["uint"] = typeof(uint);
                globalTypeNameToType["uint64"] = typeof(ulong);
                globalTypeNameToType["ulong"] = typeof(ulong);
                globalTypeNameToType["float32"] = typeof(float);
                globalTypeNameToType["float"] = typeof(float);
                globalTypeNameToType["float64"] = typeof(double);
                globalTypeNameToType["double"] = typeof(double);
                globalTypeNameToType["bool"] = typeof(bool);
                globalTypeNameToType["boolean"] = typeof(bool);
                globalTypeNameToType["string"] = typeof(string);

                //foreach(var item in globalTypeNameToType.ToArray())
                //{
                //    if (item.Value.IsArray)
                //        continue;
                //    Type type= item.Value;
                //    Type arrayType= type.MakeArrayType();
                //    globalTypeNameToType[arrayType.Name] = arrayType;

                //}

            }

            return globalTypeNameToType;
        }
        static List<IDataConverter> GetConverters()
        {
            if (converters == null)
            {
                converters = new List<IDataConverter>();
                foreach (Assembly ass in AppDomain.CurrentDomain
                    .GetAssemblies()
                    .Referenced(typeof(IDataConverter).Assembly))
                {
                    foreach (var type in ass.GetTypes())
                    {
                        if (!type.IsClass || type.IsAbstract)
                            continue;
                        if (typeof(IDataConverter).IsAssignableFrom(type))
                        {
                            IDataConverter conv = Activator.CreateInstance(type) as IDataConverter;
                            if (conv != null)
                                converters.Add(conv);
                        }
                    }
                }

                converters = converters.OrderBy(o => o.Order).ToList();

                for (int i = 0; i < converters.Count; i++)
                {
                    if (converters[i].GetType() == typeof(DefaultConverter))
                    {
                        var tmp = converters[i];
                        converters[i] = converters[converters.Count - 1];
                        converters[converters.Count - 1] = tmp;
                        break;
                    }
                }

            }

            return converters;
        }

        public object ChangeType(DataFieldInfo fieldInfo, object value, Type type)
        {
            //null
            if (value == null)
                return GetDefaultValue(type);
            //string.empty
            if (value is string && string.IsNullOrEmpty((string)value))
                return GetDefaultValue(type);

            object result = null;
            foreach (var converter in GetConverters())
            {
                if (converter.ConvertTo(this, fieldInfo, value, type, out result))
                {
                    break;
                }
            }
            return result;
        }



        public void InitalizeTypes()
        {

            Assembly assembly = null;

            if (string.IsNullOrEmpty(config.OutputCode.assemblyName))
            {
                throw new Exception("Assembly Name empty");
            }

            if (config.OutputCode.format == CodeFormat.Assembly)
            {
                if (string.IsNullOrEmpty(config.OutputCode.outputDir))
                {
                    throw new Exception("code outputDir empty");
                }
                string assemblyPath = Path.GetFullPath(Path.Combine(config.OutputCode.outputDir, $"{config.OutputCode.assemblyName}.dll"));
                CheckFilePath(assemblyPath);
                assembly = Assembly.LoadFile(assemblyPath);
            }
            else if (!string.IsNullOrEmpty(BuildOptions.instance.tmpAssemblyPath))
            {
                assembly = Assembly.LoadFile(BuildOptions.instance.tmpAssemblyPath);
            }
            else
            {
                string assemblyPath = Path.Combine("Library/ScriptAssemblies", $"{config.OutputCode.assemblyName}.dll");
                assemblyPath = Path.GetFullPath(assemblyPath);
                if (File.Exists(assemblyPath))
                {
                    assembly = Assembly.LoadFile(assemblyPath);
                }
            }

            if (assembly == null)
                throw new Exception($"Assembly [{config.OutputCode.assemblyName}] null");
            Console.WriteLine($"Use metadata assembly: {assembly.Location}");
            allTypes = assembly.GetTypes();

            allTypeToDataTables = new Dictionary<Type, DataTableInfo>();
            foreach (var tableInfo in DataTableInfos)
            {
                allTypeToDataTables[DataTableInfoToType(tableInfo)] = tableInfo;
            }
        }
        public Type DataTableInfoToType(DataTableInfo tableInfo)
        {
            string typeName = TableNameToTypeName(tableInfo.Name);

            Type objType;
            objType = allTypes.Where(o => string.Equals(o.FullName, typeName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            if (objType == null)
            {
                objType = allTypes.Where(o => string.Equals(o.Name, typeName, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            }
            if (objType == null)
                throw new Exception("not found mapping type. table:[ " + tableInfo.Name + "], try to build code");
            return objType;
        }


        //public void _InitTableFormatValues(DataConfig config, DataTableInfo tableInfo, IDictionary<string, object> parentValues, ref IDictionary<string, object> formatValues)
        //{
        //    if (formatValues == null)
        //    {
        //        formatValues = new HierarchyDictionary<string, object>(parentValues);
        //    }
        //    formatValues["TableName"] = tableInfo.Name;
        //}


        public virtual void Dispose()
        {
            Close();
        }
        ~DataReader()
        {
            Dispose();
        }
    }

    public interface IDataFormatProvider
    {

    }

}
