using MiniExcelLibs.OpenXml;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Build.Data.Provider.MiniExcel
{
    public class MiniExcelDataReader : DataReader
    {
        private InputDataConfig tableConfig { get => Config.Input; }
        private Dictionary<string, CacheSheet> tableNameToSheet;

        class CacheSheet
        {
            public string filePath;
            public SheetInfo sheetInfo;
        }

        protected override IEnumerable<DataTableInfo> ReadDataTableInfo()
        {
            var config = Config;

            tableNameToSheet = new Dictionary<string, CacheSheet>();
            Regex fileIncludeRegex = null, fileExcludeRegex = null;
            if (!string.IsNullOrEmpty(config.Input.FileInclude))
            {
                fileIncludeRegex = new Regex(config.Input.FileInclude, RegexOptions.IgnoreCase);
            }
            if (!string.IsNullOrEmpty(config.Input.FileExclude))
            {
                fileExcludeRegex = new Regex(config.Input.FileExclude, RegexOptions.IgnoreCase);
            }


            var tableConfig = config.Input;

            string inputDir = tableConfig.Directory;
            if (string.IsNullOrEmpty(inputDir))
            {
                inputDir = ".";
            }

            // inputDir = Path.GetFullPath(inputDir);

            List<DataTableInfo> tables = new List<DataTableInfo>();
            foreach (var file in Directory.GetFiles(inputDir, "*", SearchOption.AllDirectories))
            {
                string extensionName = Path.GetExtension(file).ToLower();
                string fileName = Path.GetFileName(file);

                string filePath = Path.GetFullPath(file);
                if (fileIncludeRegex != null && !fileIncludeRegex.IsMatch(file))
                    continue;
                if (fileExcludeRegex != null && fileExcludeRegex.IsMatch(file))
                    continue;

                Console.WriteLine($"load file {BuildDataUtility.ToRelativePath(filePath, Config.Input.Directory)}");

                var list = MiniExcelLibs.MiniExcel.GetSheetInformations(filePath);
                if (list != null)
                {
                    foreach (var si in list)
                    {
                        if (si.State != SheetState.Visible)
                            continue;
                        DataTableInfo tableInfo = ParseDataTableInfo(filePath, si);
                        if (tableInfo == null)
                            continue;
                        if (tables.Any(o => o.Name == tableInfo.Name))
                            throw new Exception(string.Format("<{0}> table exists, file:{1}", tableInfo.Name, filePath));
                        tableNameToSheet[tableInfo.Name] = new CacheSheet()
                        {
                            filePath = filePath,
                            sheetInfo = si
                        };
                        tables.Add(tableInfo);
                        yield return tableInfo;
                    }
                }
            }
        }

        bool IsEmptyRow(object[] values)
        {
            if (values == null || values.Length == 0) return true;
            for (int i = 0; i < values.Length; i++)
            {
                var val = values[i];
                if (!(val == null || object.Equals(val, string.Empty)))
                {
                    return false;
                }
            }
            return true;
        }

        DataTableInfo ParseDataTableInfo(string filePath, SheetInfo si)
        {
            Console.WriteLine($"load sheet <{si.Name}>");

            string tableName = si.Name;
            tableName = tableName.Trim();
            DataTableInfo tableInfo = new DataTableInfo(Parameters);

            Match m;
            if (!string.IsNullOrEmpty(tableConfig.TableName))
            {
                m = CachedRegex(tableConfig.TableName).Match(tableName);
                if (m.Success && m.Groups["result"].Success)
                {
                    tableName = m.Groups["result"].Value;
                    if (m.Groups["desc"] != null && m.Groups["desc"].Success)
                    {
                        tableInfo.Description = m.Groups["desc"].Value;
                    }
                }
                else
                {
                    Console.WriteLine("not match table name.  table name pattern: {0}", tableConfig.TableName);
                    return null;
                }
            }

            tableInfo.Name = BuildDataUtility.ReplaceCodeNameSafeChar(tableName);
            tableInfo.OriginIndex = (int)si.Index;

            var rows = MiniExcelLibs.MiniExcel.Query(filePath, useHeaderRow: true, sheetName: si.Name);

            bool isFirst = true;
            int columnCount = 0;
            int rowIndex = 0;
            DataRowConfig nameRow, ignoreColumnRow, fieldTypeRow, fieldDescRow, defaultValueRow, dataRow, fieldDefRow;
            nameRow = tableConfig.FindRow(DataRowType.FieldName);
            ignoreColumnRow = tableConfig.FindRow(DataRowType.Exclude);
            fieldTypeRow = tableConfig.FindRow(DataRowType.FieldType);
            fieldDescRow = tableConfig.FindRow(DataRowType.FieldSummary);
            defaultValueRow = tableConfig.FindRow(DataRowType.DefaultValue);
            fieldDefRow = tableConfig.FindRow(DataRowType.Keyword);


            var rowConfigs = tableConfig.Rows.Where(o => o.Index >= 0).ToList();
            Dictionary<int, string[]> rowDatas = new Dictionary<int, string[]>();
            string[] keys = null;
            string[] values;
            foreach (IDictionary<string, object> row in rows)
            {
                var rowConfig = rowConfigs.FirstOrDefault(o => o.Index == rowIndex);
                if (isFirst)
                {
                    isFirst = false;
                    keys = row.Keys.ToArray();
                    columnCount = keys.Length;
                    //Console.WriteLine("column count " + columnCount);
                    if (columnCount <= 0)
                    {
                        Console.WriteLine("ignore");
                        return null;
                    }
                    //keys 为第一行
                    if (rowConfig != null)
                    {
                        values = new string[columnCount];
                        Array.Copy(keys, values, values.Length);
                        rowDatas[rowConfig.Index] = values;
                        rowConfigs.Remove(rowConfig);
                    }

                    rowIndex++;
                    rowConfig = rowConfigs.FirstOrDefault(o => o.Index == rowIndex);
                }



                if (rowConfig != null)
                {
                    values = new string[columnCount];
                    for (int i = 0; i < keys.Length; i++)
                    {
                        values[i] = Convert.ToString(row[keys[i]]);
                    }
                    rowDatas[rowConfig.Index] = values;
                    rowConfigs.Remove(rowConfig);

                }
                if (rowConfigs.Count == 0)
                    break;
                rowIndex++;
            }


            tableInfo.UpdateParameters();

            List<DataFieldInfo> fields = new List<DataFieldInfo>();
            int fieldNameRowIndex, fieldTypeRowIndex, fieldDescRowIndex, defaultValueRowIndex, fieldDefRowIndex;


            fieldNameRowIndex = nameRow == null ? -1 : nameRow.Index;
            fieldTypeRowIndex = fieldTypeRow == null ? -1 : fieldTypeRow.Index;
            fieldDescRowIndex = fieldDescRow == null ? -1 : fieldDescRow.Index;
            defaultValueRowIndex = defaultValueRow == null ? -1 : defaultValueRow.Index;
            fieldDefRowIndex = fieldDefRow == null ? -1 : fieldDefRow.Index;

            if (fieldNameRowIndex < 0)
            {
                Console.WriteLine("not found [field name] row");
                return null;
            }


            string key = null;

            for (int i = tableConfig.OffsetColumn; i < columnCount; i++)
            {
                values = rowDatas[fieldNameRowIndex];

                string fieldName = GetStringPatternResult(values[i], nameRow.ValuePattern);
                if (string.IsNullOrEmpty(fieldName))
                {
                    Console.WriteLine("field name not match. field: {0}, pattern: {1}", values[i], nameRow.ValuePattern);
                    continue;
                }



                DataFieldInfo fieldInfo = new DataFieldInfo(tableInfo);
                fieldInfo.Name = BuildDataUtility.ReplaceCodeNameSafeChar(fieldName);
                fieldInfo.DataType = typeof(string);
                fieldInfo.DataTypeName = "string";
                fieldInfo.OriginIndex = i;
                fieldInfo.Index = fields.Count;
                fieldInfo.DataIndex = fields.Count;

                if (key != null && string.Equals(key, fieldName, StringComparison.InvariantCultureIgnoreCase))
                {
                    fieldInfo.Flags |= DataFieldFlags.Key;
                }


                if (fieldTypeRowIndex >= 0)
                {
                    string typeName = GetStringPatternResult(rowDatas[fieldTypeRowIndex][i], fieldTypeRow.ValuePattern);
                    if (!string.IsNullOrEmpty(typeName))
                    {

                        fieldInfo.DataTypeName = GetTypeName(typeName);
                        fieldInfo.DataType = TypeNameToType(typeName);
                        //if (fieldInfo.DataType == null)
                        //    Console.WriteLine("DataType null. " + typeName + " " + tableName + "." + fieldName + "\n" + item.filePath);
                        //    throw new Exception("unknown type name: " + typeName + " " + tableName + "." + fieldName + "\n" + this.excelFile);
                    }
                }

                if (fieldDefRowIndex >= 0)
                {
                    string strDef = GetStringPatternResult(rowDatas[fieldDefRowIndex][i], fieldDefRow.ValuePattern);
                    ParseKeyword(tableInfo, fieldInfo, strDef);
                }
                if ((fieldInfo.Flags & DataFieldFlags.Exclude) == DataFieldFlags.Exclude)
                    continue;

                if (!CheckFieldTagInclude(Config, fieldInfo))
                    continue;


                if (fieldDescRowIndex >= 0)
                {
                    fieldInfo.Description = GetStringPatternResult(rowDatas[fieldDescRowIndex][i], fieldDescRow.ValuePattern);
                }

                if (defaultValueRowIndex >= 0)
                {
                    object value = GetStringPatternResult(rowDatas[defaultValueRowIndex][i], defaultValueRow.ValuePattern);
                    if (fieldInfo.DataType != null)
                    {
                        fieldInfo.DefaultValue = ChangeType(value, fieldInfo.DataType, GetDefaultValue(fieldInfo.DataType));
                        fieldInfo.HasDefaultValue = true;
                    }
                }
                fields.Add(fieldInfo);
            }

            if (fields.Count == 0)
            {
                Console.WriteLine("field count is 0");
                return null;
            }

            tableInfo.Columns = fields.ToArray();
            Console.WriteLine($"table <{tableInfo.Name}> field <{tableInfo.Columns.Length}>");
            return tableInfo;
        }


        public override IEnumerable<object[]> ReadRows(string tableName)
        {
            var tableInfo = GetTableInfo(tableName);
            if (tableInfo == null)
                throw new Exception("not table " + tableName);
            int dataColumnStartIndex = tableConfig.OffsetColumn;
            int dataRowIndex = -1;
            var dataRow = tableConfig.FindRow(DataRowType.Data);
            if (dataRow == null || dataRow.Index < 0)
            {
                dataRowIndex = tableConfig.Rows.Max(o => o.Index) + 1;
            }
            var cacheSheet = tableNameToSheet[tableName];

            var rows = MiniExcelLibs.MiniExcel.Query(cacheSheet.filePath, useHeaderRow: true, sheetName: cacheSheet.sheetInfo.Name);

            int rowIndex = 0;
            string[] keys = null;
            object[] values;
            bool isFirst = true;
            var columns = tableInfo.Columns;
            foreach (IDictionary<string, object> row in rows)
            {
                if (isFirst)
                {
                    isFirst = false;
                    keys = row.Keys.ToArray();
                    rowIndex++;
                }

                if (rowIndex >= dataRowIndex)
                {
                    values = new object[columns.Length];


                    for (int i = 0; i < columns.Length; i++)
                    {
                        var col = columns[i];
                        values[i] = row[keys[col.OriginIndex]];
                    }

                    if (IsEmptyRow(values))
                    {
                        break;
                    }
                    yield return values;
                }
                rowIndex++;
            }
        }

    }
}
