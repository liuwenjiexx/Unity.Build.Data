using Build.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Build.Data
{
    public class Program
    {

        public static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("BuildData");
            Console.WriteLine();
            Console.WriteLine("*** Command Line Arguments ***");
            foreach(var arg in args)
            {
                Console.WriteLine(arg);
            }
            Console.WriteLine("*** Command Line Arguments ***");

            Console.WriteLine("CurrentDirectory: " + Environment.CurrentDirectory);
            
            string configFile = null;

            Dictionary<string, string> argDic = CommandLineArgsToDictionary(args);

            if (argDic.ContainsKey("-config"))
            {
                configFile = argDic["-config"];
                if (configFile != null)
                    configFile = configFile.Trim();
            }

            BuildDataConfig config = BuildDataUtility.LoadConfigFromFile(configFile);

            try
            {
                BuildOptions options = new BuildOptions()
                {
                    config = config
                };

                if (argDic.ContainsKey("-code"))
                {
                    options.buildCode = true;
                }
                if (argDic.ContainsKey("-data"))
                {
                    options.buildData = true;
                }
                BuildDataUtility.Build(options);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ex.StackTrace);
                throw ex;
            }
        }


        public static Dictionary<string, string> CommandLineArgsToDictionary(string[] args)
        {
            var dic = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            for (int i = 0; i < args.Length; i++)
            {
                string key = args[i];
                string value = null;
                int index = key.IndexOf('=');
                if (index >= 0)
                {
                    value = key.Substring(index + 1);
                    key = key.Substring(0, index);
                }
                dic[key] = value;
            }
            return dic;
        }

    }
}
