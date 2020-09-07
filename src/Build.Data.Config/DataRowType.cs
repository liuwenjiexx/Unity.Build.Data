using System;

namespace Build.Data
{

    public enum DataRowType
    {
        FieldName = 1,
        FieldType,
        [Obsolete]
        DefaultValue,
        FieldDescription,
        Keyword,
        Data,
        TableDescription,
        [Obsolete]
        Exclude,
    }
}
