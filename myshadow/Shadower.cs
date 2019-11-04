using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DotNetCommons.Sys;

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

    [Flags]
    public enum ShadowOptions
    {
        None = 0,
        Verbose = 1,
        RemoteAutoIncrement = 2
    }

    public class Shadower
    {
        private readonly ShadowDefinition _def;
        private readonly bool _removeAuto;
        private readonly string _verbose;
        private readonly List<string> _tables;

        public Shadower(ShadowDefinition def, ShadowOptions options, List<string> tables)
        {
            _def = def;
            _removeAuto = options.HasFlag(ShadowOptions.RemoteAutoIncrement);
            _verbose = options.HasFlag(ShadowOptions.Verbose) ? "-v" : null;
            _tables = tables.ToList();
        }

        private static string AddToFileName(string filename, string postfix)
        {
            return Path.ChangeExtension(filename, null) + postfix + Path.GetExtension(filename);
        }

        public void DoDump()
        {
            Msg("Dumping remote server data");
            var datafile = _def.DataFile + ".gz";

            // tablelist = searchable list of all tables specified on the command line
            var tablelist = new HashSet<string>(_tables, StringComparer.CurrentCultureIgnoreCase);

            // firstlist = which specified tables to process in the "global" section
            var firstlist = new HashSet<string>(_tables, StringComparer.CurrentCultureIgnoreCase);
            foreach (var t in _def.ExtraParams.Keys)
                firstlist.Remove(t);

            foreach (var item in _def.ExtraParams.OrderBy(x => x.Key))
            {
                var extra = string.Join(" ", item.Value);

                // If we did specify tables and the current table is not part of that list, skip
                if (_tables.Any() && !tablelist.Contains(item.Key))
                    continue;

                ShellCmd($"mysqldump.exe {_def.RemoteServer} -R -E -C --single_transaction {_verbose} {extra} {_def.RemoteDatabase}",
                    (string.IsNullOrEmpty(item.Key) ? string.Join(" ", firstlist) : item.Key),
                    "| gzip",
                    (string.IsNullOrEmpty(item.Key) ? "> " : ">> ") + datafile);
            }
        }

        public void DoReload()
        {
            Msg($"Recreating local {_def.LocalDatabase} database");
            ShellCmd($"mysql.exe {_def.LocalServer} --default-character-set=utf8mb4",
                $"-e \"drop database if exists `{_def.LocalDatabase}`; create database `{_def.LocalDatabase}` collate '{_def.LocalCollation}';\"");

            Msg($"Loading database {_def.LocalDatabase} with data");

            var datafile = _def.DataFile + ".gz";
            ShellCmd($"gzip -d -c {datafile}",
                "| sed 's/NO_AUTO_CREATE_USER//'",
                $"| mysql.exe {_def.LocalServer} --default-character-set=utf8mb4 {_def.LocalDatabase}");
        }

        public void DoSchemaLocal()
        {
            Msg("Dumping local schema");
            ShellCmd($"mysqldump.exe {_def.LocalServer} -R -E -C --comments --no-data {_verbose} {_def.LocalDatabase}", 
                _removeAuto ? "| sed \"s/ AUTO_INCREMENT=[0-9]*//g\"" : "",
                $"> {AddToFileName(_def.DataFile, "_local")}");
        }

        public void DoSchemaRemote()
        {
            Msg("Dumping remote schema");
            ShellCmd($"mysqldump.exe {_def.RemoteServer} -R -E --comments --no-data {_verbose} {_def.RemoteDatabase}", 
                _removeAuto ? "| sed \"s/ AUTO_INCREMENT=[0-9]*//g\"" : "",
                $" > {AddToFileName(_def.DataFile, "_remote")}");
        }

        public void DoTransform()
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

        public void Run(ShadowCommand cmd)
        {
            switch (cmd)
            {
                case ShadowCommand.Dump:
                    DoDump();
                    break;
                case ShadowCommand.LocalSchema:
                    DoSchemaLocal();
                    break;
                case ShadowCommand.Reload:
                    DoReload();
                    break;
                case ShadowCommand.RemoteSchema:
                    DoSchemaRemote();
                    break;
                case ShadowCommand.Transform:
                    DoTransform();
                    break;
            }
        }

        private void Msg(string message)
        {
            using (new SetConsoleColor(ConsoleColor.Green))
            {
                Console.Error.WriteLine("");
                Console.Error.WriteLine(message);
            }
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

        public static ShadowCommand TextToCommand(string command)
        {
            if (string.IsNullOrEmpty(command))
                throw new ArgumentNullException(nameof(command));

            ShadowCommand result;
            if (!Enum.TryParse(command.Replace("-", "").Trim(), true, out result))
                throw new Exception("Invalid command: " + command);

            return result;
        }
    }
}
