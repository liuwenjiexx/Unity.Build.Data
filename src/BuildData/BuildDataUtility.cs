using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using System.Xml;
using Template.Xslt;

namespace Build.Data
{
    public static class BuildDataUtility
    {
        public static BuildDataConfig LoadConfigFromFile(string configFile)
        {
            Console.WriteLine("load config: " + configFile);
            configFile = Path.GetFullPath(configFile);
            if (!File.Exists(configFile))
                throw new Exception($"config file not exists: <{ configFile}>");

            string jsonString;
            jsonString = File.ReadAllText(configFile, Encoding.UTF8);
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            BuildDataConfig config = (BuildDataConfig)serializer.Deserialize(jsonString, typeof(BuildDataConfig));
            config.FileName = configFile;
            return config;
        }

        public static Type GetDataReaderType(BuildDataConfig config)
        {
            Type type = null;

            if (!string.IsNullOrEmpty(config.Input.Provider))
            {
                Console.WriteLine($"load {nameof(DataReader)} <{config.Input.Provider}>");
                type = Type.GetType(config.Input.Provider, true);
                if (!typeof(DataReader).IsAssignableFrom(type))
                    throw new Exception($"not <{nameof(DataReader)}> AssignableFrom <{type.FullName}>");
            }
            if (type == null)
                type = Type.GetType("Build.Data.MSExcel.ExcelDataReader, Build.Data.Provider.MSExcel", true);

            return type;
        }
        public static Type GetDataWriterType(BuildDataConfig config)
        {
            Type type = null;

            if (!string.IsNullOrEmpty(config.Output.Provider))
            {
                Console.WriteLine($"load {nameof(DataWriter)} <{config.Output.Provider}>");
                type = Type.GetType(config.Output.Provider, true);
                if (!typeof(DataWriter).IsAssignableFrom(type))
                    throw new Exception($"not <{nameof(DataWriter)}> AssignableFrom <{type.FullName}>");
            }

            if (type == null)
                type = typeof(JsonDataWriter);

            return type;
        }

        public static void Build(BuildOptions options)
        {
            Console.WriteLine();
            Console.WriteLine("*** Build Begin ***");
            DateTime startTime = DateTime.Now;
            Type readerType = GetDataReaderType(options.config);
            using (var reader = (DataReader)Activator.CreateInstance(readerType))
            {
                reader.LoadConfig(options.config);
                Console.WriteLine();
                Console.WriteLine("*** Reader Open Begin ***");
                DateTime startTime2 = DateTime.Now;
                Console.WriteLine("input directory: " + options.config.Input.Directory);
                reader.Open();
                Console.WriteLine("*** Reader Open End. time:{0:0.#}s ***", (DateTime.Now - startTime2).TotalSeconds);
                Console.WriteLine();

                if (options.buildCode)
                {
                    BuildCode(options, reader);
                }

                if (options.buildData)
                {
                    BuildData(options, reader);
                }
            }

            Console.WriteLine("*** Build End. time:{0:0.#}s ***", (DateTime.Now - startTime).TotalSeconds);
        }

        private static void BuildData(BuildOptions options, DataReader reader)
        {
            Console.WriteLine();
            Console.WriteLine("*** Build Data Begin ***");
            DateTime startTime = DateTime.Now;

            Console.WriteLine($"output directory: {BuildDataUtility.ToRelativePath(reader.Config.Output.Path, ".")}");
            Type writerType = GetDataWriterType(reader.Config);

            using (var writer = (DataWriter)Activator.CreateInstance(writerType))
            {
                writer.LoadConfig(reader.Config);
                writer.Open(reader);

                writer.WriteAll();
            }
            var usedTime = (DateTime.Now - startTime).TotalSeconds;
            Console.WriteLine("*** Build Data End. time:{0:0.#}s ***", usedTime);
        }


        private static void BuildCode(BuildOptions options, DataReader reader)
        {
            Console.WriteLine();
            Console.WriteLine("*** Build Code Begin ***");
            DateTime startTime = DateTime.Now;

            XmlDocument doc = new XmlDocument();
            XmlNode rootNode = doc.CreateElement("Root");
            XmlAttribute attr;

            attr = doc.CreateAttribute("Output");

            string output = options.config.OutputCode.Path;
            output = Path.GetFullPath(output);
            attr.Value = output;
            rootNode.Attributes.Append(attr);
            IDictionary<string, object> configFormatValues = new HierarchyDictionary<string, object>();



            foreach (var tableInfo in reader.DataTableInfos)
            {
                //  WorkbookItem item = reader.tableNameToWorkbook[tableInfo.Name];


                XmlNode typeNode = doc.CreateElement("Type");
                attr = doc.CreateAttribute("Name");
                attr.Value = reader.TableNameToTypeName(tableInfo.Name);
                typeNode.Attributes.Append(attr);

                attr = doc.CreateAttribute("Type");
                if ((tableInfo.Flags & DataTableFlags.Struct) == DataTableFlags.Struct)
                    attr.Value = "Struct";
                else if ((tableInfo.Flags & DataTableFlags.Enum) == DataTableFlags.Enum)
                    attr.Value = "Enum";
                else
                    attr.Value = "Class";

                typeNode.Attributes.Append(attr);

                attr = doc.CreateAttribute("Namespace");
                attr.Value = options.config.OutputCode.Namespace;
                typeNode.Attributes.Append(attr);

                if (!((tableInfo.Flags & DataTableFlags.Enum) == DataTableFlags.Enum))
                {

                    foreach (var col in tableInfo.Columns)
                    {
                        XmlNode fieldNode = doc.CreateElement("Field");

                        attr = doc.CreateAttribute("Name");
                        attr.Value = col.Name;
                        fieldNode.Attributes.Append(attr);

                        attr = doc.CreateAttribute("Type");
                        if (col.DataType != null)
                        {
                            attr.Value = col.DataType.FullName;
                        }
                        else
                        {
                            attr.Value = reader.TableNameToTypeName(col.DataTypeName);
                        }
                        fieldNode.Attributes.Append(attr);

                        attr = doc.CreateAttribute("Description");
                        attr.Value = ReplaceSafeChar(col.Description);
                        fieldNode.Attributes.Append(attr);

                        attr = doc.CreateAttribute("Key");
                        attr.Value = col.IsKey.ToString();
                        fieldNode.Attributes.Append(attr);

                        typeNode.AppendChild(fieldNode);
                    }
                }
                else
                {
                    var colName = tableInfo.GetColumn("Name");
                    var colValue = tableInfo.GetColumn("Value");

                    foreach (EnumValue row in reader.LoadDataObjects(tableInfo.Name, typeof(EnumValue)))
                    {
                        XmlNode fieldNode = doc.CreateElement("Enum");

                        attr = doc.CreateAttribute("Name");
                        attr.Value = row.Name;
                        fieldNode.Attributes.Append(attr);

                        attr = doc.CreateAttribute("Type");
                        attr.Value = colName.DataType.FullName;
                        fieldNode.Attributes.Append(attr);

                        if (!string.IsNullOrEmpty(row.Description))
                        {
                            attr = doc.CreateAttribute("Description");
                            attr.Value = ReplaceSafeChar(row.Description);
                            fieldNode.Attributes.Append(attr);
                        }

                        attr = doc.CreateAttribute("Value");
                        attr.Value = reader.ChangeType(row.Value, colValue.DataType).ToStringOrNulll();
                        fieldNode.Attributes.Append(attr);

                        typeNode.AppendChild(fieldNode);
                    }
                }

                rootNode.AppendChild(typeNode);
            }


            doc.AppendChild(rootNode);

            //string tplPath = config.Base.ExportCodeTpl;
            //if (!Path.IsPathRooted(tplPath))
            //    tplPath = Path.Combine(baseDir, tplPath);

            bool isOutputDll = false;
            string outputCodeFile;

            if (Path.GetExtension(output).ToLower() == ".dll")
            {
                isOutputDll = true;
                outputCodeFile = Path.Combine("Temp/gen/Data", Path.GetFileNameWithoutExtension(output) + ".cs");
                outputCodeFile = Path.GetFullPath(outputCodeFile);
            }
            else
            {
                outputCodeFile = output;
            }


            XsltTemplate tpl = new XsltTemplate();
            tpl.Variables["OutputPath"] = outputCodeFile;
            var ass = typeof(DataReader).Assembly;
            var tplSteam = ass.GetManifestResourceStream("Build.Data.Template.code_tpl.xslt");
            if (tplSteam == null)
                throw new Exception("not exists " + "BuildData.Template.code_tpl.xslt");
            using (StreamReader sr = new StreamReader(tplSteam))
            {
                string xslXml = sr.ReadToEnd();
                tpl.LoadXslXml(xslXml);

                string[] codeFiles = tpl.Transform(doc);

                if (isOutputDll)
                {
                    CompilerCode(output, codeFiles);
                }
            }

            var usedTime = (DateTime.Now - startTime).TotalSeconds;
            Console.WriteLine("*** Build Code End. time:{0:0.#}s ***", usedTime);

        }

        class EnumValue
        {
            public string Name;
            public object Value;
            public string Description;
        }



        public static bool CompilerCode(string outputPath, string[] codeFiles, string[] assemblies = null)
        {
            string tmpDir = Path.GetFullPath("Temp\\gen");
            if (!Directory.Exists(tmpDir))
                Directory.CreateDirectory(tmpDir);

            outputPath = outputPath.Replace('/', '\\');
            if (!Directory.Exists(Path.GetDirectoryName(outputPath)))
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath));


            CodeDomProvider cSharpCodePrivoder = new CSharpCodeProvider();

            CompilerParameters cp = new CompilerParameters();
            cp.ReferencedAssemblies.Add("System.dll");
            cp.ReferencedAssemblies.Add("System.Core.dll");
            cp.GenerateExecutable = false;
            cp.GenerateInMemory = false;
            cp.OutputAssembly = outputPath;
            cp.CompilerOptions = "/optimize";
            cp.WarningLevel = 3;

            string docFile = Path.Combine(Path.GetDirectoryName(outputPath), Path.GetFileNameWithoutExtension(outputPath)) + ".xml";
            cp.CompilerOptions += " /doc:\"" + docFile + "\"";
            cp.TempFiles = new TempFileCollection(tmpDir, true);
            if (assemblies != null)
            {
                foreach (var ass in assemblies)
                    cp.ReferencedAssemblies.Add(ass);
            }

            string srcFile;
            srcFile = codeFiles[0];
            srcFile = srcFile.Replace('/', '\\');

            CompilerResults cr = cSharpCodePrivoder.CompileAssemblyFromFile(cp, srcFile);

            if (cr.Errors.HasErrors)
            {
                StringBuilder sb = new StringBuilder();
                foreach (CompilerError err in cr.Errors)
                {
                    sb.AppendFormat("file: {0}", err.FileName).AppendLine()
                        .AppendFormat("error {0}, line: {1}, warning: {2}", err.ErrorNumber, err.Line, err.IsWarning).AppendLine()
                        .AppendLine(err.ErrorText);
                }
                throw new Exception(sb.ToString());
            }

            return true;
        }


        public static string ReplaceSafeChar(string input)
        {
            if (input == null)
                return null;
            foreach (var ch in new char[] { '\n', '\r' })
            {
                input = input.Replace(ch.ToString(), "");
            }
            return input;
        }
        public static string ReplaceCodeNameSafeChar(string input)
        {
            if (input == null)
                return null;

            for (int i = 0; i < input.Length; i++)
            {
                if (!IsCodeNameSafeChar(input[i]))
                {
                    if (i < input.Length - 1)
                        input = input.Substring(0, i) + '_' + input.Substring(i + 1);
                    else
                        input = input.Substring(0, i) + '_';
                }
            }

            if (input.Length > 0)
            {
                if ('0' <= input[0] && input[0] <= '9')
                    input = '_' + input;
            }

            return input;
        }
        public static bool IsCodeNameSafeChar(char ch)
        {
            if (ch == '_')
                return true;
            if ('a' <= ch && ch <= 'z')
                return true;
            if ('A' <= ch && ch <= 'Z')
                return true;
            if ('0' <= ch && ch <= '9')
                return true;
            return false;
        }




        static string FileNameToSafeChar(string str)
        {
            if (string.IsNullOrEmpty(str))
                return str;
            foreach (var ch in new char[] { '#', '-', ' ', '.' })
                str = str.Replace(ch, '_');

            if (str.Length > 0)
            {
                char[] chs;
                int start = 0;
                if (str[0] >= '0' && str[0] <= '9')
                {
                    chs = new char[str.Length + 1];
                    start = 1;
                    chs[0] = '_';
                }
                else
                {
                    chs = new char[str.Length];
                }
                for (int i = 0; i < str.Length; i++)
                {
                    var ch = str[i];
                    if (!(('a' <= ch && ch <= 'z') || ('A' <= ch && ch <= 'Z') || ('0' <= ch && ch <= '9')))
                    {
                        ch = '_';
                    }
                    chs[start++] = ch;
                }
                str = new string(chs);
            }

            return str;
        }

        public static string ToRelativePath(string path, string relativeTo)
        {
            string result;
            ToRelativePath(path, relativeTo, out result);
            return result;
        }

        public static bool ToRelativePath(string path, string relativeTo, out string result)
        {
            string fullRelativeTo = Path.GetFullPath(relativeTo).Trim();
            string fullPath = Path.GetFullPath(path).Trim();

            if (fullPath.EndsWith("/") || fullPath.EndsWith("\\"))
                fullPath = fullPath.Substring(0, fullPath.Length - 1);
            if (fullRelativeTo.EndsWith("/") || fullRelativeTo.EndsWith("\\"))
                fullRelativeTo = fullRelativeTo.Substring(0, fullRelativeTo.Length - 1);

            string[] relativeToParts = fullRelativeTo.Split('/', '\\');
            string[] fullPathParts = fullPath.Split('/', '\\');
            int index = -1;

            if (fullPathParts.Length <= 1)
            {
                result = path;
                return false;
            }

            if (!string.Equals(fullPathParts[0], relativeToParts[0], StringComparison.InvariantCultureIgnoreCase))
            {
                result = path;
                return false;
            }


            for (int i = 0; i < fullPathParts.Length && i < relativeToParts.Length; i++)
            {
                if (!string.Equals(fullPathParts[i], relativeToParts[i], StringComparison.InvariantCultureIgnoreCase))
                    break;
                index = i;
            }

            result = "";
            for (int i = index + 1; i < relativeToParts.Length; i++)
            {
                if (result.Length > 0)
                    result += Path.DirectorySeparatorChar;
                result += "..";
            }
            for (int i = index + 1; i < fullPathParts.Length; i++)
            {
                if (result.Length > 0)
                    result += Path.DirectorySeparatorChar;
                result += fullPathParts[i];
            }
            return true;
        }

    }
    public class BuildOptions
    {
        public BuildDataConfig config;
        public bool buildData;
        public bool buildCode;
    }
}

