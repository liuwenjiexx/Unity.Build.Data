using System;

namespace Build.Data
{

    /// <summary>
    /// 生成 Class
    /// </summary>
    [Serializable]
    public class BuildCodeConfig
    {
        public string Path;
        public string Namespace;
        public string TypeName;
    }
}
