using System;
using System.IO;

namespace Build.Data
{
    [Serializable]
    public partial class BuildDataConfig
    {
        [NonSerialized]
        private string fileName;
        public string FileName
        {
            get => fileName;
            set
            {
                fileName = value;
            }
        }


        public InputDataConfig Input;

        public OutputDataConfig Output;

        public BuildCodeConfig OutputCode = new BuildCodeConfig();

        public TypeMappingConfig[] TypeMappings;



    }


    [Serializable]
    public class TypeMappingConfig
    {
        public string Name;
        public string Mapping;
    }






}
