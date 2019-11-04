using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotNetCommons.Sys;

namespace myshadow
{
    public class ShadowDefinition
    {
        public string RemoteDatabase { get; set; }
        public string RemoteServer { get; set; }
        public string LocalDatabase { get; set; }
        public string LocalServer { get; set; }
        public string LocalCollation { get; set; }
        public string DataFile { get; set; }
        public string TransformFile { get; set; }
        public Dictionary<string, List<string>> ExtraParams { get; }

        private string _currentTable;

        public ShadowDefinition()
        {
            ExtraParams = new Dictionary<string, List<string>> { { "", new List<string>() } };
            _currentTable = "";
        }

        public void Load(string filename)
        {
            var lines = File.ReadAllLines(filename)
                .Select(x => x.Trim())
                .ToList();

            foreach (var line in lines)
            {
                var text = line.Split(new[] { '#' }, 2).First().Trim();
                if (string.IsNullOrEmpty(text))
                    continue;

                if (text.StartsWith("-"))
                {
                    ExtraParams[_currentTable].Add(text);
                    continue;
                }

                var items = text.Split(new[] { '=' }, 2);
                if (items.Length != 2)
                    throw new Exception($"Unrecognized directive \"{line}\"");

                ParseDirective(items[0].Trim(), items[1].Trim());
            }
        }

        public void ParseDirective(string key, string value)
        {
            switch (key.ToLower())
            {
                case "remote-database":
                    RemoteDatabase = value;
                    return;
                case "remote-server":
                    RemoteServer = value;
                    return;
                case "local-database":
                    LocalDatabase = value;
                    return;
                case "local-server":
                    LocalServer = value;
                    return;
                case "local-collation":
                    LocalCollation = value;
                    return;
                case "data-file":
                    DataFile = value;
                    return;
                case "transform-file":
                    TransformFile = value;
                    return;
                case "table":
                    _currentTable = value;
                    if (!ExtraParams.ContainsKey(_currentTable))
                        ExtraParams.Add(_currentTable, new List<string>());
                    return;
                default:
                    throw new Exception("Unrecognized directive: " + key);
            }
        }

        public void Validate()
        {
            var errors = new List<string>();

            using (new SetConsoleColor(ConsoleColor.Yellow))
            {
                if (string.IsNullOrEmpty(RemoteDatabase))
                    errors.Add("Remote database name is missing (remote-database).");

                if (string.IsNullOrEmpty(RemoteServer))
                    errors.Add("Remote server connection is missing (remote-server).");

                if (string.IsNullOrEmpty(LocalDatabase))
                    errors.Add("Local database name is missing (local-database).");

                if (string.IsNullOrEmpty(LocalServer))
                    errors.Add("Local server connection is missing (local-server).");

                if (string.IsNullOrEmpty(DataFile))
                    errors.Add("No data file specified (data-file).");
            }

            if (errors.Any())
            {
                using (new SetConsoleColor(ConsoleColor.Yellow))
                    Console.Error.WriteLine(string.Join("\r\n", errors));

                throw new Exception("One or more missing parameters in definition file.");
            }

            if (string.IsNullOrEmpty(LocalCollation))
            {
                using (new SetConsoleColor(ConsoleColor.Yellow))
                    Console.Error.WriteLine("Warning: No local collation specified, using 'utf8mb4_general_ci'.");

                LocalCollation = "utf8mb4_general_ci";
            }
        }
    }
}
