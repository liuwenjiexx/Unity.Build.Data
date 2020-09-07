using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Build.Data
{

    [Serializable]
    public class DataRowConfig
    {
        public DataRowType Type;
        public int Index;
        public string Pattern;
        public string ValuePattern;

        public DataRowConfig()
        {
        }

        public DataRowConfig(DataRowType rowType, int rowIndex)
        {
            this.Type = rowType;
            this.Index = rowIndex;
        }
        public DataRowConfig(DataRowType rowType, string rowPattern)
        {
            this.Type = rowType;
            this.Pattern = rowPattern;
        }
    }

}
