using System;

namespace Build.Data
{

    public enum DataRowType
    {
        FieldName = 1,
        FieldType,
        [Obsolete]
        DefaultValue,
        FieldSummary,
        FieldDescription= FieldSummary,
        Keyword,
        Data,
        TableDescription,
        [Obsolete]
        Exclude,
    }
}
