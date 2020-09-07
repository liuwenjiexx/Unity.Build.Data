using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Script.Serialization;

namespace Build.Data
{

    /// <summary>
    /// JSON数据格式提供程序
    /// </summary>
    public class JsonDataWriter : DataWriter
    {
        private StringBuilder sb;
        private JavaScriptSerializer serializer;

        public JsonDataWriter()
        {

        }

        public override void Open(DataReader reader)
        {
            base.Open(reader);
            serializer = new JavaScriptSerializer();
            sb = new StringBuilder();

            foreach (var file in Directory.GetFiles(OutputDir, "*", SearchOption.AllDirectories))
            {
                file.ClearFileAttributes();
                File.Delete(file);
            }
        }

        public override void Write(DataTableInfo tableInfo)
        {
            Type objType;
            objType = Reader.DataTableInfoToType(tableInfo);

            sb.Clear();
            var objs = Reader.LoadDataObjects(tableInfo.Name, objType).ToArray();
            serializer.Serialize(objs, sb);
            string outputFile = Path.Combine(OutputDir, tableInfo.Name + ".json");
            File.WriteAllText(outputFile, sb.ToString(), new UTF8Encoding(false));
            Console.WriteLine($"output {BuildDataUtility.ToRelativePath(outputFile, OutputDir)}. table <{tableInfo.Name}> type <{objType.FullName}> data length <{objs.Length}>");

        }

    }
}
