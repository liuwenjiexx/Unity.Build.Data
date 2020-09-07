using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Build.Data
{

    public class DataMemberCollection: System.Collections.ObjectModel.ReadOnlyCollection<DataMember>
    {
        private Dictionary<string, DataMember> members;
        private IList<DataMember> list;
        private Type type;

        public DataMemberCollection(Type type, IList<DataMember> members)
            :base(members)
        {
            this.type = type;
            this.list = members;
            this.members = members.ToDictionary(o => o.Name);
        }

        public DataMember this[string name]
        {
            get { return members[name]; }
        }

 

        public Type Type
        {
            get { return type; }
        }

        public bool TryGet(string name, out DataMember member)
        {
            return members.TryGetValue(name, out member);
        }

        public bool Contains(string name)
        {
            return members.ContainsKey(name);
        }

    }
}
