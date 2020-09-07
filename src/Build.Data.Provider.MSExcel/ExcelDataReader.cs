using Microsoft.Office.Interop.Excel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace Build.Data.MSExcel
{
    public class ExcelDataReader : DataReader
    {
        private ExcelApplication excelApp;


        private InputDataConfig tableConfig { get => Config.Input; }


        private List<WorkbookItem> workbooks;
        private Dictionary<string, WorkbookItem> tableNameToWorkbook;

        public ExcelDataReader()
        {
            workbooks = new List<WorkbookItem>();
            tableNameToWorkbook = new Dictionary<string, WorkbookItem>();

        }



        class WorkbookItem
        {
            public string filePath;
            public Workbook wb;
        }



        /// <summary>
        /// 只创建一个Excel实例
        /// </summary>
        public class ExcelApplication : IDisposable
        {
            private bool disposed;

            private static Application excel;
            private static Workbooks workbooks;
            private static int excelRef;

            public ExcelApplication()
            {
                if (excel == null)
                {
                    excel = new Application()
                    {
                        Visible = false,
                        UserControl = true,
                        DisplayAlerts = false,
                    };
                    if (excel == null)
                        throw new Exception("Can't access excel");
                    workbooks = excel.Workbooks;
                    excelRef = 0;
                }
                excelRef++;
            }

            public Application Excel
            {
                get => excel;
            }
            public Workbooks Workbooks
            {
                get => workbooks;
            }

            public void Dispose()
            {
                if (disposed)
                    return;
                disposed = true;
                excelRef--;
                if (excelRef <= 0)
                {
                    if (excel != null)
                    {
                        var tmp = excel;
                        excel = null;
                        DateTime now = DateTime.Now;
                        if (workbooks != null)
                        {
                            workbooks.Close();
                            Marshal.FinalReleaseComObject(workbooks);
                            workbooks = null;
                        }

                        tmp.Quit();
                        Marshal.FinalReleaseComObject(tmp);
                        tmp = null;
                        Console.WriteLine("Excel Quit: {0:0.###}s", (DateTime.Now - now).TotalSeconds);

                        now = DateTime.Now;
                        GC.Collect();
                        Console.WriteLine("Excel GC Collect: {0:0.###}s", (DateTime.Now - now).TotalSeconds);
                        //now = DateTime.Now;
                        //GC.WaitForPendingFinalizers();
                        //GC.Collect();
                        //GC.WaitForPendingFinalizers();
                        //Console.WriteLine("Excel GC WaitForPendingFinalizers: {0:0.###}s", (DateTime.Now - now).TotalSeconds);

                    }
                }
            }



            ~ExcelApplication()
            {
                Dispose();
            }
        }


        public override void Open()
        {
            if (IsOpened)
                return;
            excelApp = new ExcelApplication();
            base.Open();
        }

        protected override IEnumerable<DataTableInfo> ReadDataTableInfo()
        {
            var config = Config;

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

            foreach (var file in Directory.GetFiles(inputDir, "*", SearchOption.AllDirectories))
            {
                string extensionName = Path.GetExtension(file).ToLower();
                string fileName = Path.GetFileName(file);

                string filePath = Path.GetFullPath(file);
                if (fileIncludeRegex != null && !fileIncludeRegex.IsMatch(file))
                    continue;
                if (fileExcludeRegex != null && fileExcludeRegex.IsMatch(file))
                    continue;

                object missing = Type.Missing;
                WorkbookItem workbookItem = new WorkbookItem();
                workbookItem.filePath = filePath;
                workbooks.Add(workbookItem);

                workbookItem.wb = excelApp.Workbooks.Open(workbookItem.filePath, missing, true, missing, missing, missing,
               missing, missing, missing, true, missing, missing, missing, missing, missing);

                foreach (var dt in ReadDataTableInfo(workbookItem))
                {
                    yield return dt;
                }

            }



        }


        IEnumerable<DataTableInfo> ReadDataTableInfo(WorkbookItem item)
        {
            Console.WriteLine($"load file {BuildDataUtility.ToRelativePath(item.filePath, Config.Input.Directory)}");

            List<DataTableInfo> tables = new List<DataTableInfo>();
            for (int i = 1; i <= item.wb.Worksheets.Count; i++)
            {
                Worksheet ws = (Worksheet)item.wb.Worksheets.get_Item(i);
                DataTableInfo tableInfo = ParseDataTableInfo(item, ws);
                if (tableInfo == null)
                    continue;
                if (tableNameToWorkbook.ContainsKey(tableInfo.Name))
                    throw new Exception(string.Format("<{0}> table exists, file:{1}", tableInfo.Name, item.filePath));

                tableNameToWorkbook[tableInfo.Name] = item;
                tables.Add(tableInfo);
            }
            return tables.ToArray();
        }




        public override void Close()
        {

            foreach (var item in workbooks)
            {
                if (item.wb != null)
                {
                    item.wb.Close(false);
                    Marshal.FinalReleaseComObject(item.wb);
                    item.wb = null;
                }
            }
            workbooks.Clear();

            if (excelApp != null)
            {
                excelApp.Dispose();
                excelApp = null;
            }

            base.Close();
        }





        int FindNotEmptyRow(object[,] vals, int columnSize, int row)
        {
            while (row > 0)
            {
                if (!IsEmptyRow(vals, columnSize, row))
                    break;
                row--;
            }
            return row;
        }

        bool IsEmptyRow(object[,] vals, int columnSize, int row)
        {
            bool isEmpty = true;
            for (int i = 1; i <= columnSize; i++)
            {
                var v = vals[row, i];
                if (v != null)
                {
                    if (v is string)
                    {
                        if (!string.IsNullOrEmpty(((string)v).Trim()))
                        {
                            isEmpty = false;
                            break;
                        }
                    }
                    else
                    {
                        if (!string.IsNullOrEmpty(v.ToString().Trim()))
                        {
                            isEmpty = false;
                            break;
                        }
                    }
                }
            }
            return isEmpty;
        }


        int FindRowIndex(Worksheet ws, DataRowType rowType)
        {
            int index = FindRowIndex(ws, tableConfig.FindRow(rowType));
            if (index < 0)
            {
                if (rowType == DataRowType.Data)
                    index = tableConfig.Rows.Max(o => o.Index) + 1;
            }
            return index;
        }

        int FindRowIndex(Worksheet ws, DataRowConfig rowConfig)
        {
            if (rowConfig == null)
                return -1;
            if (rowConfig.Index > 0)
                return rowConfig.Index;

            if (!string.IsNullOrEmpty(rowConfig.Pattern))
            {
                Regex regex = CachedRegex(rowConfig.Pattern);

                int rowCount = ws.UsedRange.Cells.Rows.Count;
                for (int i = 1 + tableConfig.OffsetRow; i <= rowCount; i++)
                {
                    string text = GetCellString(ws, i, 1 + tableConfig.OffsetRow);

                    if (regex.IsMatch(text))
                    {
                        return i;
                    }
                }
            }
            return -1;
        }
        object GetCellValue(Worksheet ws, int row, int colum, string pattern)
        {
            object value = ws.Cells.get_Range(ToCell(row, colum), Type.Missing).Value;
            if (value != null && value is string)
            {
                if (!string.IsNullOrEmpty(pattern))
                {
                    string text = value as string;
                    var regex = CachedRegex(pattern);
                    var m = regex.Match(text);
                    if (m.Success && m.Groups.Count > 1)
                        text = m.Groups[1].Value;
                    else
                        text = string.Empty;
                    value = text;
                }
            }
            return value;
        }
        string GetCellString(Worksheet ws, int row, int colum)
        {
            object value = ws.Cells.get_Range(ToCell(row, colum), Type.Missing).Value;
            string text = Convert.ChangeType(value, typeof(string)) as string;
            if (text != null)
                text = text.Trim();
            else
                text = string.Empty;
            return text;
        }

        string GetCellString(Worksheet ws, int row, int colum, string pattern)
        {
            string text = GetCellString(ws, row, colum);

            if (!string.IsNullOrEmpty(pattern))
            {
                //var regex = GetRegex(pattern);
                //var m = regex.Match(text);
                //if (m.Success)
                //{
                //    var g = m.Groups["result"];
                //    if (g.Success)
                //        text = g.Value;
                //    else
                //        text = string.Empty;
                //}
                //else
                //{
                //    text = string.Empty;
                //}
                text = GetStringPatternResult(text, pattern);
            }
            return text;
        }


        DataTableInfo ParseDataTableInfo(WorkbookItem item, Worksheet ws)
        {
            Console.WriteLine($"load worksheet <{ws.Name}>");

            int columnCount = ws.UsedRange.Cells.Columns.Count;
            //Console.WriteLine("column count " + columnCount);
            if (columnCount <= 0)
            {
                Console.WriteLine("ignore");
                return null;
            }

            //Range rng = null;
            //rng = ws.Cells.get_Range(ToCell(1, 1), ToCell(1, columnCount));

            //object[,] vals = (object[,])rng.Value;
            //if (vals == null)
            //    return null;


            DataTableInfo tableInfo = new DataTableInfo(Parameters);

            string tableName = ws.Name;
            tableName = tableName.Trim();

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
            string key = null;


            tableInfo.Name = BuildDataUtility.ReplaceCodeNameSafeChar(tableName);
            tableInfo.OriginIndex = ws.Index;

            int index = FindRowIndex(ws, DataRowType.TableDescription);
            if (index > 0)
            {
                tableInfo.Description = GetCellString(ws, index, tableConfig.OffsetColumn);
            }

            tableInfo.UpdateParameters();

            List<DataFieldInfo> fields = new List<DataFieldInfo>();
            int fieldNameRowIndex, fieldTypeRowIndex, fieldDescRowIndex, defaultValueRowIndex, fieldDefRowIndex;


            fieldNameRowIndex = FindRowIndex(ws, DataRowType.FieldName);
            fieldTypeRowIndex = FindRowIndex(ws, DataRowType.FieldType);
            fieldDescRowIndex = FindRowIndex(ws, DataRowType.FieldDescription);
            defaultValueRowIndex = FindRowIndex(ws, DataRowType.DefaultValue);
            fieldDefRowIndex = FindRowIndex(ws, DataRowType.Keyword);

            int excludeColumnRow = FindRowIndex(ws, DataRowType.Exclude);

            if (fieldNameRowIndex <= 0)
            {
                Console.WriteLine("not found field name row");
                return null;
            }
            DataRowConfig nameRow, ignoreColumnRow, fieldTypeRow, fieldDescRow, defaultValueRow, dataRow, fieldDefRow;
            nameRow = tableConfig.FindRow(DataRowType.FieldName);
            ignoreColumnRow = tableConfig.FindRow(DataRowType.Exclude);
            fieldTypeRow = tableConfig.FindRow(DataRowType.FieldType);
            fieldDescRow = tableConfig.FindRow(DataRowType.FieldDescription);
            defaultValueRow = tableConfig.FindRow(DataRowType.DefaultValue);
            fieldDefRow = tableConfig.FindRow(DataRowType.Keyword);

            dataRow = tableConfig.FindRow(DataRowType.Data);
            if (dataRow == null)
            {
                dataRow = new DataRowConfig()
                {
                    Type = DataRowType.Data,
                    Index = tableConfig.Rows.Max(o => o.Index) + 1
                };
            }

            for (int i = 1 + tableConfig.OffsetColumn; i <= columnCount; i++)
            {
                string str = GetCellString(ws, fieldNameRowIndex, i);
                if (string.IsNullOrEmpty(str))
                    continue;
                string fieldName = GetStringPatternResult(str, nameRow.ValuePattern);
                if (string.IsNullOrEmpty(fieldName))
                {
                    Console.WriteLine("field name not match. field: {0}, pattern: {1}", str, nameRow.ValuePattern);
                    continue;
                }

                if (excludeColumnRow > 0)
                {
                    str = GetCellString(ws, excludeColumnRow, i, ignoreColumnRow.ValuePattern);
                    if (string.IsNullOrEmpty(str))
                    {
                        Console.WriteLine("ignore field, field: {0}, ignore:{1}, pattern: {2}, row:{3}", fieldName, str, ignoreColumnRow.ValuePattern, excludeColumnRow);
                        continue;
                    }
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


                if (fieldTypeRowIndex > 0)
                {
                    string typeName = GetCellString(ws, fieldTypeRowIndex, i, fieldTypeRow.ValuePattern);
                    if (!string.IsNullOrEmpty(typeName))
                    {

                        fieldInfo.DataTypeName = GetTypeName(typeName);
                        fieldInfo.DataType = TypeNameToType(typeName);
                        //if (fieldInfo.DataType == null)
                        //    Console.WriteLine("DataType null. " + typeName + " " + tableName + "." + fieldName + "\n" + item.filePath);
                        //    throw new Exception("unknown type name: " + typeName + " " + tableName + "." + fieldName + "\n" + this.excelFile);
                    }
                }

                if (fieldDefRowIndex > 0)
                {
                    string strDef = GetCellString(ws, fieldDefRowIndex, i, fieldDefRow.ValuePattern);
                    ParseKeyword(tableInfo, fieldInfo, strDef);
                }
                if ((fieldInfo.Flags & DataFieldFlags.Exclude) == DataFieldFlags.Exclude)
                    continue;

                if (!CheckFieldTagInclude(Config, fieldInfo))
                    continue;


                if (fieldDescRowIndex > 0)
                {
                    fieldInfo.Description = GetCellString(ws, fieldDescRowIndex, i, fieldDescRow.ValuePattern);
                }

                if (defaultValueRowIndex > 0)
                {
                    object value = GetCellValue(ws, defaultValueRowIndex, i, defaultValueRow.ValuePattern);
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
            WorkbookItem item = tableNameToWorkbook[tableName];
            Worksheet ws = (Worksheet)item.wb.Worksheets.get_Item(tableInfo.OriginIndex);
            int dataRowStartIndex = FindRowIndex(ws, DataRowType.Data);


            int dataColumnStartIndex = 1 + tableConfig.OffsetColumn;
            int dataColumnEndIndex = tableInfo.Columns.Max(o => o.OriginIndex);
            var usedRange = ws.UsedRange;
            int dataRowEndIndex = usedRange.Cells.Rows.Count;
            if (dataRowStartIndex <= 0)
                yield break;

            Range rng = ws.Cells.get_Range(ToCell(dataRowStartIndex, 1), ToCell(dataRowEndIndex, dataColumnEndIndex));

            object[,] vals = (object[,])rng.Value;

            var columns = tableInfo.Columns;
            int rowCount = dataRowEndIndex - dataRowStartIndex + 1;
            rowCount = FindNotEmptyRow(vals, dataColumnEndIndex, rowCount);
            object[] rowData;
            for (int i = 1; i <= rowCount; i++)
            {
                rowData = new object[columns.Length];
                for (int j = 0; j < columns.Length; j++)
                {

                    var col = columns[j];
                    object val = vals[i, col.OriginIndex];
                    rowData[j] = val;
                }
                yield return rowData;
            }

        }



        public System.Data.DataTable LoadDataTable(string tableName)
        {
            var tableInfo = GetTableInfo(tableName);
            if (tableInfo == null)
                throw new Exception("not table " + tableName);
            var dataTable = new System.Data.DataTable();
            foreach (var field in tableInfo.Columns)
            {
                var column = dataTable.Columns.Add(field.Name, field.DataType);
            }

            foreach (var row in ReadRows(tableName))
            {
                dataTable.Rows.Add(row);
            }

            return dataTable;
        }

        public IEnumerable<T> LoadData<T>(string tableName)
        {
            Type type = typeof(T);
            return LoadDataObjects(tableName, type).Cast<T>();
        }






       public static string ToCell(int row, int col)
        {
            string a = "";
            while (col > 26)
            {
                if (a.Length == 0)
                {
                    a = "A";
                }
                else
                {
                    a = ((char)((int)a[0] + 1)).ToString();
                }
                col -= 26;
            }

            string cell = a + (char)(((int)'A') + (col - 1)) + row.ToString();
            return cell;
        }



        public void Dispose()
        {
            Close();
        }


    }
}
