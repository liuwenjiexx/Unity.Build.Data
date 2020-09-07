using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Build.Data
{
    public abstract class DataWriter : IDisposable
    {

        private BuildDataConfig config;
        private IDictionary<string, object> parameters = new Dictionary<string, object>();
        private bool isOpened;

        private DataReader reader;

        public BuildDataConfig Config { get => config; }

        public IDictionary<string, object> ConfigFormatValues { get => parameters; }

        public string OutputDir { get; private set; }

        public bool IsOpened { get => isOpened; }

        public DataReader Reader { get => reader; }

        public IDictionary<string, object> Parameters { get => parameters; }


        public virtual void LoadConfig(BuildDataConfig config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            this.config = config;


        }

        public virtual void Open(DataReader reader)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            if (isOpened)
                return;
            isOpened = true;
            this.reader = reader;
            if (string.IsNullOrEmpty(config.Output.Path))
                OutputDir = Path.GetFullPath(".");
            else
                OutputDir = Path.GetFullPath(config.Output.Path);


            if (!Directory.Exists(OutputDir))
                Directory.CreateDirectory(OutputDir);

            reader.InitalizeTypes();
        }


        public abstract void Write(DataTableInfo tableInfo);


        public void WriteAll()
        {

            foreach (var tableInfo in reader.DataTableInfos)
            {
                if ((tableInfo.Flags & DataTableFlags.NoData) == DataTableFlags.NoData)
                    continue;
                if (((tableInfo.Flags & DataTableFlags.Enum) == DataTableFlags.Enum))
                    continue;
                //IDictionary<string, object> tableFormatValues = null;
                //_InitTableFormatValues(config, tableInfo, ConfigFormatValues, ref tableFormatValues);

                Write(tableInfo);
            }
        }

        public void CheckFilePath(string path)
        {
            if (!File.Exists(path))
                throw new Exception("file not exists " + path);
        }

        public virtual void Dispose()
        {
        }

        ~DataWriter()
        {
            Dispose();
        }
    }


}
