using System;

namespace Build.Data
{

    /// <summary>
    /// 生成 Class
    /// </summary>
    [Serializable]
    public class BuildCodeConfig
    {
        public string outputDir = "Assets/Plugins/gen/Data";
        public string Namespace;
        public string TypeName;
        public string assemblyName = "Data";
        public CodeFormat format = CodeFormat.Asmdef;
        public string template = "Template/gen_code_tpl.xslt";
        public bool genIndexerClass = true;
    }

    public enum CodeFormat
    {
        Assembly,
        Code,
        Asmdef,
    }
}
