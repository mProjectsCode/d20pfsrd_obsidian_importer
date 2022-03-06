using System.Diagnostics;
using System.Reflection;
using System.Runtime.Serialization;

namespace d20pfsrd_web_scraper;

public class Config
{
    public const string ConfigFileName = "config.txt";
    public string ConfigPath;

    public ConfigOptions Options;

    public Config()
    {
        ConfigPath = PathHelper.Combine(Program.RunLocation, ConfigFileName);
        Options = ConfigOptions.GetDefaultOptions();
    }

    public void ReadConfig()
    {
        if (!File.Exists(ConfigPath))
        {
            Console.WriteLine("Config file does not exist");

            Console.WriteLine("Creating default config file");
            Options = ConfigOptions.GetDefaultOptions();
            SaveConfig();
        }

        Console.WriteLine("Opening config file...");
        Process process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            CreateNoWindow = true,
            UseShellExecute = true,
            FileName = ConfigPath,
        };
        process.Start();
        process.WaitForExit();

        string[] config = File.ReadAllLines(ConfigPath);

        Options.SetValuesFromString(config);
    }

    public void SaveConfig()
    {
        File.WriteAllText(ConfigPath, Options.ToString());
    }


    public class ConfigOptions
    {
        public ConfigOptions(string gameSystem, bool parseAsync, bool skipParsing)
        {
            GameSystem = gameSystem;
            ParseAsync = parseAsync;
            SkipParsing = skipParsing;
        }

        [Description("The game system (default: pathfinder_1e) [pathfinder_1E, dnd_5e]")]
        public string GameSystem { get; set; }

        [Description("Async parsing is recommended (default: true) [true, false]")]
        public bool ParseAsync { get; set; }

        [Description("Whether to skip parsing (default: false) [true, false]")]
        public bool SkipParsing { get; set; }

        public void SetValuesFromString(string[] config)
        {
            List<string> configList = config.ToList();

            configList = configList.Where(s => !s.StartsWith('#')).ToList();

            foreach (PropertyInfo propertyInfo in typeof(ConfigOptions).GetProperties())
            {
                try
                {
                    string line = configList.First(s => s.StartsWith(propertyInfo.Name));
                    string[] parts = line.Split(':');

                    if (parts.Length != 2)
                    {
                        throw new SerializationException("the config is invalid");
                    }

                    string value = parts[1].Trim();

                    Console.WriteLine(propertyInfo.Name + " := " + value);

                    propertyInfo.SetValue(this, Convert.ChangeType(value, propertyInfo.PropertyType));
                }
                catch (InvalidOperationException e)
                {
                    throw new SerializationException("the config is invalid");
                }
            }
        }

        public override string ToString()
        {
            List<string> configList = new List<string>();

            foreach (PropertyInfo propertyInfo in typeof(ConfigOptions).GetProperties())
            {
                if (propertyInfo.GetCustomAttribute(typeof(DescriptionAttribute)) is DescriptionAttribute descriptionAttribute)
                {
                    configList.Add("# " + descriptionAttribute.Description);
                }

                string line = propertyInfo.Name + ": " + propertyInfo.GetValue(this);
                configList.Add(line);
            }

            return string.Join("\n", configList);
        }

        public static ConfigOptions GetDefaultOptions()
        {
            return new ConfigOptions("pathfinder_1e", true, false);
        }
    }
}