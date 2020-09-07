using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Build.Data
{

    /// <summary>
    /// 数组值转对象
    /// </summary>
    class ArrayToObjectConverter : IDataConverter
    {
        public int Order => -5;

        static Dictionary<Type, CachedDataMember[]> cachedMembers;
        public bool ConvertTo(DataReader reader, DataFieldInfo field, object value, Type targetType, out object result)
        {

            DataTableInfo dataTable;
            dataTable = reader.TryTypeToDataTableInfo(targetType);
            if (dataTable == null)
            {
                result = null;
                return false;
            }

            TypeCode typeCode = Type.GetTypeCode(targetType);
            if (typeCode == TypeCode.Object && !targetType.IsArray)
            {
                if (value == null)
                {
                    result = reader.GetDefaultValue(targetType);
                    return true;
                }

                if (value is string)
                {
                    string str = value as string;
                    if (string.IsNullOrEmpty(str))
                    {
                        result = reader.GetDefaultValue(targetType);
                        return true;
                    }

                    using (reader.BeginObject())
                    {
                        string arraySeparator = reader.GetSeparator(field);
                        string[] parts = str.Split(new string[] { arraySeparator }, StringSplitOptions.None);
                        result = Activator.CreateInstance(targetType);
                        object memberValue;
                        var members = GetMembers(reader, targetType);
                        for (int i = 0; i < members.Length; i++)
                        {
                            var m = members[i];
                            var col = dataTable.GetColumn(m.member.Name);
                            if (col != null)
                            {
                                if (col.HasDefaultValue)
                                {
                                    m.setValue(result, col.DefaultValue);
                                }
                            }
                        }
                        for (int i = 0; i < parts.Length; i++)
                        {
                            if (i >= members.Length)
                                throw new Exception("data member index over index: " + i + " type: " + targetType.Name);
                            var member = members[i];
                            if (string.IsNullOrEmpty(parts[i]))
                            {
                                continue;
                            }
                            memberValue = reader.ChangeType(field, parts[i], member.valueType);
                            member.setValue(result, memberValue);
                        }

                        return true;
                    }
                }
            }

            result = null;
            return false;
        }
        class CachedDataMember
        {
            public Type valueType;
            public MemberInfo member;
            public Action<object, object> setValue;

        }

        CachedDataMember[] GetMembers(DataReader reader, Type type)
        {
            if (cachedMembers == null)
                cachedMembers = new Dictionary<Type, CachedDataMember[]>();

            CachedDataMember[] members;
            if (!cachedMembers.TryGetValue(type, out members))
            {
                DataTableInfo dataTable;
                dataTable = reader.TryTypeToDataTableInfo(type);
                if (dataTable != null)
                {
                    members = new CachedDataMember[dataTable.Columns.Length];
                    foreach (var col in dataTable.Columns)
                    {
                        CachedDataMember dataMember = new CachedDataMember();
                        FieldInfo fInfo = type.GetField(col.Name);
                        if (fInfo != null)
                        {
                            dataMember.member = fInfo;
                            dataMember.valueType = fInfo.FieldType;
                            dataMember.setValue = fInfo.SetValue;
                        }
                        else
                        {
                            PropertyInfo pInfo = type.GetProperty(col.Name);
                            dataMember.member = pInfo;
                            dataMember.valueType = pInfo.PropertyType;
                            dataMember.setValue = (obj, value) => pInfo.SetValue(obj, value, null);
                        }
                        members[col.DataIndex] = dataMember;
                    }
                }
                else
                {
                    members = new CachedDataMember[0];
                }
                cachedMembers[type] = members;
            }
            return members;
        }

    }
}
