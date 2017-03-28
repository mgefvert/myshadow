using System;
using System.IO;
using System.Linq;
using System.Reflection;
using DotNetCommons;

namespace myshadow
{
    class Program
    {
        static int Main()
        {
            Console.Error.WriteLine("myshadow " + Assembly.GetExecutingAssembly().GetName().Version);

            try
            {
                var options = CommandLine.Parse<Options>();

                if (string.IsNullOrEmpty(options.Definition))
                    throw new Exception("No shadow file definition specified.");

                var filename = options.Definition;
                if (!File.Exists(filename))
                {
                    filename = filename + ".shadow";
                    if (!File.Exists(filename))
                        throw new Exception($"File definition '{options.Definition}' does not exist.");
                }

                var definition = new ShadowDefinition();
                definition.Load(filename);
                definition.Validate();

                var shadower = new Shadower(definition, options.Verbose, options.RemoveAuto);
                var commands = options.Commands.Select(shadower.TextToCommand).ToList();
                if (!commands.Any())
                {
                    commands.Add(ShadowCommand.Dump);
                    commands.Add(ShadowCommand.Reload);
                    commands.Add(ShadowCommand.Transform);
                }

                foreach (var cmd in commands)
                    shadower.Run(cmd);

                return 0;
            }
            catch (CommandLineDisplayHelpException ex)
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine("Format: myshadow [-v] <definition-file> [<commands>...]");
                Console.Error.WriteLine();
                Console.Error.WriteLine("Available commands:");
                Console.Error.WriteLine("   dump             Dump remote database to a local file");
                Console.Error.WriteLine("   reload           Recreate local database from file");
                Console.Error.WriteLine("   local-schema     Dump the local database schema");
                Console.Error.WriteLine("   remote-schema    Dump the remote database schema");
                Console.Error.WriteLine("   transform        Apply transformations to local file");
                Console.Error.WriteLine();
                Console.Error.WriteLine("If no commands are given, 'dump reload transform' will be assumed.");
                Console.Error.WriteLine();
                Console.Error.WriteLine("Available options:");
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
            catch (Exception ex)
            {
                using (new SetConsoleColor(ConsoleColor.Red))
                {
                    Console.Error.WriteLine(ex.GetType().Name + ": " + ex.Message);
                    return 1;
                }
            }
        }
    }
}
