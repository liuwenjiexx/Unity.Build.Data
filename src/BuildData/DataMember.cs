using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace Build.Data
{
    public class DataMember
    {
        private string name;
        private MemberInfo member;
        static Dictionary<Type, DataMemberCollection> cachedMembers;
        static List<IDataMemberProvider> providers = new List<IDataMemberProvider>();


        public DataMember(string name, MemberInfo member)
        {
            this.name = name;
            this.member = member;

            if (member is FieldInfo)
            {
                var f = (FieldInfo)member;
                ValueType = f.FieldType;
            }
            else {

                var p = (PropertyInfo)member;
                ValueType = p.PropertyType;
            }
        }

        public MemberInfo Member { get => member; }

        public Type ValueType { get; private set; }
        public string Name { get => name; }

        public void SetValue(object obj, object value)
        {
            if (member.MemberType == MemberTypes.Field)
            {
                FieldInfo fInfo = (FieldInfo)member;
                fInfo.SetValue(obj, value);
            }
            else if (member.MemberType == MemberTypes.Property)
            {
                PropertyInfo pInfo = (PropertyInfo)member;
                pInfo.SetValue(obj, value, null);
            }
        }

        public object GetValue(object obj)
        {
            object value = null;
            if (member.MemberType == MemberTypes.Field)
            {
                FieldInfo fInfo = (FieldInfo)member;
                value = fInfo.GetValue(obj);
            }
            else if (member.MemberType == MemberTypes.Property)
            {
                PropertyInfo pInfo = (PropertyInfo)member;
                value = pInfo.GetValue(obj, null);
            }
            return value;
        }

        public static DataMemberCollection GetMembers(Type type)
        {
            if (cachedMembers == null)
                cachedMembers = new Dictionary<Type, DataMemberCollection>();
            DataMemberCollection members;

            if (!cachedMembers.TryGetValue(type, out members))
            {
                List<DataMember> list = new List<DataMember>();
                foreach (var mInfo in type.GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetField | BindingFlags.SetProperty))
                {
                    if (!(mInfo.MemberType == MemberTypes.Field || mInfo.MemberType == MemberTypes.Property))
                        continue;
                    if (mInfo.IsDefined(typeof(CompilerGeneratedAttribute), true))
                        continue;

                    DataMemberAttribute memberAttr = (DataMemberAttribute)mInfo.GetCustomAttributes(typeof(DataMemberAttribute), true).FirstOrDefault();
          

                    if (mInfo.MemberType == MemberTypes.Field)
                    {
                        if (mInfo.IsDefined(typeof(NonSerializedAttribute), true))
                            continue;
                        FieldInfo fInfo = (FieldInfo)mInfo;
                        if (!fInfo.IsPublic && memberAttr == null)
                            continue;
                    }
                    else
                    {
                        PropertyInfo pInfo = (PropertyInfo)mInfo;
                        if (!(pInfo.CanRead && pInfo.CanWrite))
                            continue;
                        if (!pInfo.GetSetMethod().IsPublic && memberAttr == null)
                            continue;

                    }
                    string name = mInfo.Name;
                    if (memberAttr != null)
                    {
                        if (!string.IsNullOrEmpty(memberAttr.Name))
                            name = memberAttr.Name;
                    }

                    DataMember dataMember = new DataMember(name, mInfo);

                    list.Add(dataMember);
                }

                if (providers != null)
                {
                    foreach (var p in providers)
                    {
                        p.ResolveDataMembers(type, list);
                    }
                }
                members= new DataMemberCollection(type, list);
                cachedMembers[type] = members;
            }
            return members;
        }

        public static void AddDataMemberProvider(IDataMemberProvider memberProvider)
        {
            providers.Add(memberProvider);
        }



    }





}
