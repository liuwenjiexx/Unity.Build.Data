using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Build.Data
{
    /// <summary>
    /// 基础默认转换器
    /// </summary>
    class DefaultConverter : IDataConverter
    {
        public int Order => 10;

        public static DefaultConverter instance = new DefaultConverter();


        public bool ConvertTo(DataReader reader, DataFieldInfo field, object value, Type targetType, out object result)
        {
            if (value == null)
            {
                if (targetType == typeof(string))
                {
                    result = null;
                    return true;
                }
                result = reader.GetDefaultValue(targetType);
                return true;
            }
            else if (targetType == typeof(string))
            {
                
                if (value is string && string.IsNullOrEmpty((string)value))
                {
                    result = null;
                    return true;
                }
            }


            Type valueType = value.GetType();
            if (valueType == targetType)
            {
                result = value;
                return true;
            }

            if (targetType.IsEnum)
            {
                result = Enum.Parse(targetType, value.ToString());
                return true;
            }

            result = Convert.ChangeType(value, targetType);
            return true;
        }

    }
}
