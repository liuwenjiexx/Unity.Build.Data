using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Build.Data
{
    public interface IDataConverter
    {
        int Order { get; }
        bool ConvertTo(DataReader writer, DataFieldInfo field, object value, Type targetType,  out object result);
        
    }
}
