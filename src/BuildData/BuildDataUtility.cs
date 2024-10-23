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
using System.Runtime.Remoting.Messaging;
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
                throw new Exception($"config file not exists: <{configFile}>");

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
            bool isOutputDll = false;
            string outputCodeFile;
            string outputCodeDir;
            string tmpCodeDir = Path.GetFullPath("Temp/gen/Data");
            string tmpAssemblyPath = Path.Combine(tmpCodeDir, $"{options.config.OutputCode.assemblyName}.dll");
            if (options.config.OutputCode.format == CodeFormat.Assembly)
            {
                isOutputDll = true;

            }

            outputCodeDir = Path.GetFullPath(options.config.OutputCode.outputDir);
            string assemblyPath = Path.Combine(outputCodeDir, $"{options.config.OutputCode.assemblyName}.dll");
            string asmdefPath = Path.Combine(outputCodeDir, $"{options.config.OutputCode.assemblyName}.asmdef");

            XsltTemplate tpl = new XsltTemplate();
            tpl.Variables["IndexerClass"] = options.config.OutputCode.genIndexerClass.ToString().ToLower();

            var ass = typeof(DataReader).Assembly;
            string codeTpl = null;
            string codeTplPath = null;
            if (!string.IsNullOrEmpty(options.codeTpl))
            {
                codeTpl = options.codeTpl;
            }
            else
            {
                codeTpl = options.config.OutputCode.template;
            }
            if (string.IsNullOrEmpty(codeTpl))
                throw new Exception("Code template empty");
            if (Path.IsPathRooted(codeTpl))
            {
                codeTplPath = codeTpl;
            }
            if (string.IsNullOrEmpty(codeTplPath))
            {
                codeTplPath = Path.Combine(Path.GetDirectoryName(typeof(BuildDataUtility).Assembly.Location), codeTpl);
                if (!File.Exists(codeTplPath))
                {
                    codeTplPath = null;
                }
            }
            if (string.IsNullOrEmpty(codeTplPath))
            {
                if (File.Exists(codeTpl))
                {
                    codeTplPath = codeTpl;
                }
            }
            if (string.IsNullOrEmpty(codeTplPath))
                throw new Exception("not exists code template file");

            codeTplPath = Path.GetFullPath(codeTplPath);
            List<string> codeFiles = new List<string>();
            using (StreamReader sr = new StreamReader(codeTplPath))
            {
                string xslXml = sr.ReadToEnd();
                tpl.LoadXslXml(xslXml);


                XmlDocument doc = new XmlDocument();
                XmlNode rootNode = doc.CreateElement("Assembly");

                doc.AppendChild(rootNode);
                XmlAttribute attr;


                attr = doc.CreateAttribute("Name");
                attr.Value = options.config.OutputCode.assemblyName;
                rootNode.Attributes.Append(attr);


                attr = doc.CreateAttribute("First");
                attr.Value = "true";
                rootNode.Attributes.Append(attr);

                IDictionary<string, object> configFormatValues = new HierarchyDictionary<string, object>();

                int index = 0;

                if (isOutputDll || options.buildData)
                {
                    codeFiles.Clear();
                    foreach (var tableInfo in reader.DataTableInfos)
                    {
                        var typeNode = TableToTypeNode(options, reader, tableInfo, doc);

                        rootNode.AppendChild(typeNode);
                    }

                    outputCodeFile = Path.Combine(tmpCodeDir, $"{options.config.OutputCode.assemblyName}.cs");
                    outputCodeFile = Path.GetFullPath(outputCodeFile);
                    tpl.Variables["OutputPath"] = outputCodeFile;
                    tpl.Variables["first"] = "true";
                    string[] files = tpl.Transform(doc);
                    foreach (var codeFile in files)
                    {
                        string file = codeFile.Replace("\\", "/");
                        Console.WriteLine($"Gen code file: {file}");
                        codeFiles.Add(file);
                    }
                    //清理代码文件
                    if (Directory.Exists(tmpCodeDir))
                    {
                        foreach (var file in Directory.GetFiles(tmpCodeDir, "*.cs", SearchOption.AllDirectories).Select(o => o.Replace("\\", "/")))
                        {
                            if (file.EndsWith(".cs"))
                            {
                                if (!codeFiles.Contains(file))
                                {
                                    File.Delete(file);
                                    Console.WriteLine($"delete file: {file}");
                                }
                            }
                        }
                    }

                    if (File.Exists(tmpAssemblyPath))
                        File.Delete(tmpAssemblyPath);

                    CompilerCode(tmpAssemblyPath, codeFiles.ToArray());

                    if (File.Exists(tmpAssemblyPath))
                    {
                        options.tmpAssemblyPath = tmpAssemblyPath;
                    }

                    if (options.config.OutputCode.format == CodeFormat.Assembly)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(assemblyPath));
                        File.Copy(tmpAssemblyPath, assemblyPath, true);

                        string path1 = Path.Combine(Path.GetDirectoryName(tmpAssemblyPath), Path.GetFileNameWithoutExtension(tmpAssemblyPath) + ".xml");
                        if (File.Exists(path1))
                        {
                            File.Copy(path1, Path.Combine(outputCodeDir, options.config.OutputCode.assemblyName + ".xml"));
                        }
                    }
                }

                if (options.config.OutputCode.format == CodeFormat.Code || options.config.OutputCode.format == CodeFormat.Asmdef)
                {
                    codeFiles.Clear();
                    index = 0;
                    foreach (var tableInfo in reader.DataTableInfos)
                    {

                        while (rootNode.ChildNodes.Count > 0)
                        {
                            rootNode.RemoveChild(rootNode.ChildNodes[0]);
                        }

                        if (index != 0)
                        {
                            tpl.Variables["first"] = "false";
                        }
                        else
                        {
                            tpl.Variables["first"] = "true";
                        }

                        var typeNode = TableToTypeNode(options, reader, tableInfo, doc);

                        rootNode.AppendChild(typeNode);
                        string typeName = typeNode.Attributes["Name"].Value;

                        string codeFilePath = Path.Combine(outputCodeDir, $"{typeName}.cs");
                        tpl.Variables["OutputPath"] = codeFilePath;
                        string[] files = tpl.Transform(doc);
                        foreach (var codeFile in files)
                        {
                            string file = codeFile.Replace("\\", "/");
                            Console.WriteLine($"Gen code file: {file}");
                            codeFiles.Add(file);
                        }

                        index++;

                    }

                    //清理代码文件
                    if (Directory.Exists(outputCodeDir))
                    {
                        foreach (var file in Directory.GetFiles(outputCodeDir, "*.cs", SearchOption.TopDirectoryOnly).Select(o => o.Replace("\\", "/")))
                        {
                            if (file.EndsWith(".cs"))
                            {
                                if (!codeFiles.Contains(file))
                                {
                                    File.Delete(file);
                                    Console.WriteLine($"delete file: {file}");
                                }
                            }
                        }
                    }

                }

                if (options.config.OutputCode.format == CodeFormat.Asmdef)
                {
                    if (!File.Exists(asmdefPath))
                    {
                        Asmdef asmdef = new Asmdef()
                        {
                            name = options.config.OutputCode.assemblyName
                        };
                        var serializer = new JavaScriptSerializer();
                        var json = serializer.Serialize(asmdef);
                        File.WriteAllText(asmdefPath, json, Encoding.UTF8);
                    }
                }

                if (!(options.config.OutputCode.format == CodeFormat.Code || options.config.OutputCode.format == CodeFormat.Asmdef))
                {
                    foreach (var file in Directory.GetFiles(outputCodeDir, "*.cs", SearchOption.TopDirectoryOnly).Select(o => o.Replace("\\", "/")))
                    {
                        if (file.EndsWith(".cs"))
                        {
                            File.Delete(file);
                            Console.WriteLine($"delete file: {file}");
                        }
                    }
                }

                if (options.config.OutputCode.format != CodeFormat.Assembly)
                {
                    //清理程序集
                    if (File.Exists(assemblyPath))
                    {
                        File.Delete(assemblyPath);
                        Console.WriteLine($"delete file: {assemblyPath}");
                        string path1 = Path.Combine(Path.GetDirectoryName(assemblyPath), Path.GetFileNameWithoutExtension(assemblyPath) + ".xml");
                        if (File.Exists(path1))
                        {
                            File.Delete(path1);
                            Console.WriteLine($"delete file: {path1}");
                        }
                    }
                }

                if (options.config.OutputCode.format != CodeFormat.Asmdef)
                {
                    //清理 asmdef
                    if (File.Exists(asmdefPath))
                    {
                        File.Delete(asmdefPath);
                        Console.WriteLine($"delete file: {asmdefPath}");
                    }
                }
                //string tplPath = config.Base.ExportCodeTpl;
                //if (!Path.IsPathRooted(tplPath))
                //    tplPath = Path.Combine(baseDir, tplPath);


            }

            var usedTime = (DateTime.Now - startTime).TotalSeconds;
            Console.WriteLine("*** Build Code End. time:{0:0.#}s ***", usedTime);

        }

        static XmlNode TableToTypeNode(BuildOptions options, DataReader reader, DataTableInfo tableInfo, XmlDocument doc)
        {
            XmlAttribute attr;

            string typeName;
            XmlNode typeNode = doc.CreateElement("Type");
            attr = doc.CreateAttribute("Name");
            typeName = reader.TableNameToTypeName(tableInfo.Name);
            attr.Value = typeName;
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
            return typeNode;
        }


        class EnumValue
        {
            public string Name;
            public object Value;
            public string Description;
        }

        class Asmdef
        {
            public string name;
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
            string[] files = (string[])codeFiles.Clone();
            for (int i = 0; i < files.Length; i++)
            {
                files[i] = files[i].Replace('/', '\\');
            }
            CompilerResults cr = cSharpCodePrivoder.CompileAssemblyFromFile(cp, files);

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
        public string codeTpl;
        public string tmpAssemblyPath;
        public static BuildOptions instance;
    }
}

