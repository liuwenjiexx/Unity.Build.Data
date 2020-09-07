using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Build.Data
{

    [AttributeUsage(AttributeTargets.Field| AttributeTargets.Property)]
    public class DataMemberAttribute : Attribute
    {
        public DataMemberAttribute() { }
        public DataMemberAttribute(string name)
        {
            this.Name = name;
        }

        public string Name { get; set; }

    }
}
