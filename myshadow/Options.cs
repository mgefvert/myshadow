using System;
using System.Collections.Generic;
using DotNetCommons;

namespace myshadow
{
    public class Options
    {
        [CommandLineRemaining]
        public List<string> Commands { get; } = new List<string>();

        [CommandLinePosition(1)]
        public string Definition { get; set; }

        [CommandLineOption('a', "remove-auto", "Remove auto_increment counter from schema dump (requires sed)")]
        public bool RemoveAuto { get; set; }

        [CommandLineOption('v', "verbose", "Verbose output from mysql tools")]
        public bool Verbose { get; set; }
    }
}
