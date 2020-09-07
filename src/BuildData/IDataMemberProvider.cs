using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Build.Data
{
    public interface IDataMemberProvider
    {
        void ResolveDataMembers(Type type, List<DataMember> members);

    }


}
