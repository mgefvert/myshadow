using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DotNetCommons;

namespace myshadow
{
    public enum ShadowCommand
    {
        Dump,
        Reload,
        LocalSchema,
        RemoteSchema,
        Transform
    }

    public class Shadower
    {
        private readonly ShadowDefinition _def;
        private readonly bool _removeAuto;
        private readonly string _verbose;

        public Shadower(ShadowDefinition def, bool verbose, bool removeAutoIncrement)
        {
            _def = def;
            _removeAuto = removeAutoIncrement;
            _verbose = verbose ? "-v" : null;
        }

        public ShadowCommand TextToCommand(string command)
        {
            if (string.IsNullOrEmpty(command))
                throw new ArgumentNullException(nameof(command));

            ShadowCommand result;
            if (!Enum.TryParse(command.Replace("-", "").Trim(), true, out result))
                throw new Exception("Invalid command: " + command);

            return result;
        }

        public void Run(ShadowCommand cmd)
        {
            switch (cmd)
            {
                case ShadowCommand.Dump:
                    Dump();
                    break;
                case ShadowCommand.LocalSchema:
                    LocalSchema();
                    break;
                case ShadowCommand.Reload:
                    Reload();
                    break;
                case ShadowCommand.RemoteSchema:
                    RemoteSchema();
                    break;
                case ShadowCommand.Transform:
                    Transform();
                    break;
            }
        }

        public void Dump()
        {
            Msg("Dumping remote server data");

            foreach (var item in _def.ExtraParams.OrderBy(x => x.Key))
            {
                var extra = string.Join(" ", item.Value);
                var pipe = string.IsNullOrEmpty(item.Key) ? "> " + _def.DataFile : ">> " + _def.DataFile;

                ShellCmd($"mysqldump.exe {_def.RemoteServer} -R -E -C --single_transaction {_verbose} {extra} {_def.RemoteDatabase} {item.Key} {pipe}");
            }
        }

        public void DumpTable(string table, List<string> extraParams)
        {
            if (!string.IsNullOrEmpty(table))
                Msg("Dumping additional table " + table);

            ShellCmd($"mysqldump.exe {_def.RemoteServer} -R -E -C --single_transaction {_verbose} -r{_def.DataFile} {_def.RemoteDatabase}");
        }

        public void LocalSchema()
        {
            Msg("Dumping local schema");
            ShellCmd($"mysqldump.exe {_def.LocalServer} -R -E -C --comments --no-data {_verbose} {_def.RemoteDatabase}", 
                _removeAuto ? "| sed \"s/ AUTO_INCREMENT=[0-9]*//g\"" : "",
                $"> {AddToFileName(_def.DataFile, "_local")}");
        }

        public void Reload()
        {
            Msg($"Recreating local {_def.LocalDatabase} database");
            ShellCmd($"mysql.exe {_def.LocalServer} --default-character-set=utf8mb4",
                $"-e \"drop database if exists {_def.LocalDatabase}; create database {_def.LocalDatabase} collate '{_def.LocalCollation}';\"");

            Msg($"Loading database {_def.LocalDatabase} with data");
            ShellCmd($"mysql.exe {_def.LocalServer} --default-character-set=utf8mb4 {_def.LocalDatabase} < {_def.DataFile}");
        }

        public void Transform()
        {
            if (string.IsNullOrEmpty(_def.TransformFile))
            {
                Console.Error.WriteLine("No transformation file specified.");
                return;
            }

            if (!File.Exists(_def.TransformFile))
            {
                using (new SetConsoleColor(ConsoleColor.Yellow))
                    Console.Error.WriteLine($"Warning: Transformation file '{_def.TransformFile}' does not exist.");

                return;
            }

            Msg("Applying transformations");
            ShellCmd($"mysql.exe {_def.LocalServer} --default-character-set=utf8mb4 {_verbose} {_def.LocalDatabase} < {_def.TransformFile}");
        }

        private void Msg(string message)
        {
            using (new SetConsoleColor(ConsoleColor.Green))
            {
                Console.Error.WriteLine("");
                Console.Error.WriteLine(message);
            }
        }

        public void RemoteSchema()
        {
            Msg("Dumping remote schema");
            ShellCmd($"mysqldump.exe {_def.RemoteServer} -R -E --comments --no-data {_verbose} {_def.RemoteDatabase}", 
                _removeAuto ? "| sed \"s/ AUTO_INCREMENT=[0-9]*//g\"" : "",
                $" > {AddToFileName(_def.DataFile, "_remote")}");
        }

        private void ShellCmd(params string[] arguments)
        {
            var args = string.Join(" ", arguments.Where(x => !string.IsNullOrWhiteSpace(x)));
            while (args.Contains("  "))
                args = args.Replace("  ", " ");
            if (_verbose != null)
                Console.Error.WriteLine(args);

            var startinfo = new ProcessStartInfo("cmd.exe", "/c " + args) { UseShellExecute = false };
            var process = Process.Start(startinfo);
            if (process == null)
                throw new Exception("Unable to start process.");

            process.WaitForExit();
            if (process.ExitCode > 0)
                throw new Exception("Subprocess returned exit code " + process.ExitCode);
        }

        private static string AddToFileName(string filename, string postfix)
        {
            return Path.ChangeExtension(filename, null) + postfix + Path.GetExtension(filename);
        }
    }
}
