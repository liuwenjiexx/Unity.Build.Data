using System;
using System.Linq;

namespace Build.Data
{
    [Serializable]
    public class InputDataConfig
    {

        public string Provider;
        //public string FileInclude;
        //public string FileExclude;
        public string Directory;
        public string FileInclude;
        public string FileExclude;
        public string TableName;
        public int OffsetRow;
        public int OffsetColumn;
        //public string MappingType;

        public string TagInclude;
        public string TagExclude;

        public string ArraySeparator;
        public string ObjectSeparator;
        public DataRowConfig[] Rows;



        public static string DefaultArraySeparator = "| !";
        public static string DefaultObjectSeparator = ": ,";

        public DataRowConfig FindRow(DataRowType rowType)
        {
            if (Rows == null)
                return null;
            return Rows.Where(o => o.Type == rowType).FirstOrDefault();
        }

    }






}
