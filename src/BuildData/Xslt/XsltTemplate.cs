using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Xml.Xsl;


namespace Template.Xslt
{
    public class XsltTemplate
    {
        private const string NS = "urn:templates";
        private XslCompiledTransform transform;
        private string xsltPath;
        string inputValue;
        private Dictionary<string, object> variables = new Dictionary<string, object>();
        public string BaseDirectory { get; set; }
        public Dictionary<string, object> Variables { get { return variables; } }

        public void LoadXslXml(string xslXml)
        {
            XmlDocument xsltDoc = new XmlDocument();
            xsltDoc.LoadXml(xslXml);

            transform = new XslCompiledTransform();
            transform.Load(XmlReader.Create(new StringReader(xslXml)));

            Load(xsltDoc);
        }

        public void Load(string xsltPath)
        {
            this.xsltPath = xsltPath;
            BaseDirectory = Path.GetDirectoryName(xsltPath);
            transform = new XslCompiledTransform();
            transform.Load(xsltPath);
            XmlDocument xsltDoc = new XmlDocument();
            xsltDoc.Load(xsltPath);
            Load(xsltDoc);
        }

        private void Load(XmlDocument xsltDoc)
        {

            var ns = new XmlNamespaceManager(xsltDoc.NameTable);
            ns.AddNamespace("tpl", NS);
            var settingsNode = xsltDoc.DocumentElement.SelectSingleNode("//tpl:Settings", ns);
            if (settingsNode != null)
            {

                var inputNode = settingsNode.SelectSingleNode("tpl:Input", ns);
                if (settingsNode == null)
                    throw new System.Exception("Input node null");
                inputValue = inputNode.InnerText;
            }

        }


        public string[] Transform()
        {
            List<string> outputs = new List<string>();

            foreach (var doc in GetXmlDocuments(xsltPath, inputValue))
            {
                outputs.AddRange(Transform(doc));
            }
            return outputs.ToArray();
        }

        public string[] Transform(XmlDocument doc)
        {

            XsltContext context = new XsltContext();

            foreach (var item in variables)
            {
                context.Set(item.Key, item.Value as string);
            }
            context.xsltArgList.AddExtensionObject(NS, context);

            StringWriter stream = new StringWriter();
            transform.Transform(doc.CreateNavigator(), context.xsltArgList, stream);

            string result;
            result = stream.ToString();
            string outputPath = context.OutputPath;
            if (string.IsNullOrEmpty(outputPath))
                throw new Exception("output path null");

            outputPath = ResolveEditorFilePath(BaseDirectory, outputPath);
            bool write = true;
            if (File.Exists(outputPath))
            {
                string old = File.ReadAllText(outputPath, Encoding.UTF8);
                if (old == result)
                {
                    write = false;
                }
            }

            if (write)
            {
                string dir = Path.GetDirectoryName(outputPath);
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                ClearFileAttributes(outputPath);
                File.WriteAllText(outputPath, result, Encoding.UTF8);
            }

            return new string[] { outputPath };
        }
        static void ClearFileAttributes(string path)
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
                File.SetAttributes(path, FileAttributes.Normal);
        }

        string ResolveEditorFilePath(string baseDir, string path)
        {
            string filePath = null;
            Uri uri = null;
            string scheme = null;
            try
            {
                uri = new Uri(path);
            }
            catch { }

            if (uri == null)
            {
                if (Path.IsPathRooted(path))
                    filePath = path;
                else
                    filePath = Path.Combine(baseDir, path);
            }
            else
            {
                scheme = uri.Scheme.ToLower();
                string localFilePath = uri.Host + uri.AbsolutePath;
                if (scheme == "file")
                {
                    if (Path.IsPathRooted(localFilePath))
                        filePath = localFilePath;
                    else
                        filePath = Path.Combine(baseDir, localFilePath);
                }
                else if (scheme == "assets")
                {
                    filePath = Path.Combine("Assets", localFilePath);
                }
                else if (scheme == "resources")
                {
                    filePath = Path.Combine("Assets/Resources", localFilePath);
                }
                else if (scheme == "project")
                {
                    filePath = localFilePath;
                }
            }

            return filePath;
        }

        IEnumerable<XmlDocument> GetXmlDocuments(string xsltPath, string input)
        {
            string inputFilePath = null;
            Uri uri = null;
            string scheme = null;
            try
            {
                uri = new Uri(input);
                scheme = uri.Scheme;
            }
            catch { }

            string baseDir = Path.GetDirectoryName(xsltPath);

            inputFilePath = ResolveEditorFilePath(baseDir, input);


            if (!string.IsNullOrEmpty(inputFilePath))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(inputFilePath);
                yield return doc;
            }
            else
            {

                if (scheme == "type")
                {
                    string typePath = uri.AbsolutePath;
                    int index = typePath.LastIndexOf('.');
                    string memberName = uri.Segments[1];
                    string typeName = uri.Host;

                    Type type = Type.GetType(typeName, false, true);
                    if (type == null)
                    {
                        type = AppDomain.CurrentDomain
                            .GetAssemblies()
                            .SelectMany(o => o.GetTypes())
                            .Where(o => o.Name.ToLower() == typeName || o.FullName.ToLower() == typeName)
                            .FirstOrDefault();
                    }

                    if (type == null)
                        throw new Exception("not found type:" + typeName);

                    var pInfo = type.GetProperty(memberName, BindingFlags.Public | BindingFlags.Static);
                    if (pInfo == null)
                        throw new Exception("not found property:" + memberName);
                    var value = pInfo.GetGetMethod().Invoke(null, null);
                    if (value == null)
                        yield break;
                    if (pInfo.PropertyType == typeof(XmlDocument))
                    {
                        XmlDocument doc = value as XmlDocument;
                        if (doc == null)
                            yield break;
                        yield return doc;
                    }
                    else if (pInfo.PropertyType == typeof(IEnumerable<XmlDocument>))
                    {
                        IEnumerable<XmlDocument> docs = value as IEnumerable<XmlDocument>;
                        if (docs == null)
                            yield break;
                        foreach (var doc in docs)
                            yield return doc;
                    }
                    else
                    {
                        throw new Exception("type error");
                    }
                }
            }

        }
    }
}