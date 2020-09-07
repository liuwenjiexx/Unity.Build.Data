using System;
using System.Xml.Xsl;

namespace Template.Xslt
{

    public class XsltContext
    {
        private const string NS = "";
        public XsltArgumentList xsltArgList = new XsltArgumentList();

        public string OutputPath
        {
            get
            {
                return Get("OutputPath") as string;
            }
            set
            {
                Set("OutputPath", value);
            }
        }


        public object Set(string name, string value)
        {
            xsltArgList.RemoveParam(name, NS);
            xsltArgList.AddParam(name, NS, value);
            return string.Empty;
        }
        public object Get(string name)
        {
            return xsltArgList.GetParam(name, NS);
        }
        public string[] ToStringArray(string str, string separator)
        {
            return str.Split(new string[] { separator },StringSplitOptions.None);
        }
    }

}