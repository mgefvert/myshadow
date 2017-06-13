﻿using System;
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

        [CommandLineOption('z', "gzip", "Compress local data file (requires gzip)")]
        public bool GZip { get; set; }

        [CommandLineOption('a', "remove-auto", "Remove auto_increment counter from schema dump (requires sed)")]
        public bool RemoveAuto { get; set; }

        [CommandLineOption('t', "table", "Only dump specific table (or table sets)")]
        public List<string> Tables { get; } = new List<string>();
            
        [CommandLineOption('v', "verbose", "Verbose output from mysql tools")]
        public bool Verbose { get; set; }

        public ShadowOptions ShadowOptions =>
            (GZip ? ShadowOptions.GZip : ShadowOptions.None) |
            (RemoveAuto ? ShadowOptions.RemoteAutoIncrement : ShadowOptions.None) |
            (Verbose ? ShadowOptions.Verbose : ShadowOptions.None);
    }
}
