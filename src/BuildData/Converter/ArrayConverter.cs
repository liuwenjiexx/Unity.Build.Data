using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Build.Data
{

    /// <summary>
    /// 数组格式转换器
    /// </summary>
    class ArrayConverter : IDataConverter
    {
        public int Order => -10;

        public bool ConvertTo(DataReader reader, DataFieldInfo field, object value, Type targetType, out object result)
        {
            if (targetType.IsArray)
            {
                var elemType = targetType.GetElementType();
                if (value is string)
                {
                    string str = value as string;
                    if (string.IsNullOrEmpty(str))
                    {
                        result = null;
                        return true;
                    }
                    using (var s = reader.BeginArray())
                    {
                        string arraySeparator = reader.GetSeparator(field);

                        string[] arrStr = str.Split(new string[] { arraySeparator }, StringSplitOptions.None);
                        Array arr = Array.CreateInstance(elemType, arrStr.Length);
                        object elemValue;
                        for (int i = 0; i < arrStr.Length; i++)
                        {
                            elemValue = reader.ChangeType(field, arrStr[i], elemType);
                            arr.SetValue(elemValue, i);
                        }
                        result = arr;
                        return true;
                    }
                }
                else
                {
                    //excel 数字 日期不一定是字符串类型
                    Array arr = Array.CreateInstance(elemType, 1);
                    if (!elemType.IsAssignableFrom(value.GetType()))
                        value = Convert.ChangeType(value, elemType);
                    arr.SetValue(value, 0);
                    result = arr;
                    return true;
                }
            }

            result = null;
            return false;
        }
    }
}
