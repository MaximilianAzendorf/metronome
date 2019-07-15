using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NDesk.Options;

namespace metronome
{
    public static class Options
    {
        public static List<List<int>> Divisions { get; } = new List<List<int>>();
        public static float Speed { get; private set; } = 60;

        public static bool ShowHelp { get; private set; }

        private static IEnumerable<List<int>> ParseDivison(string input)
        {
            int mul = 1;
            string[] mulParts = input.Split('*');
            string list = input;

            if (mulParts.Length == 2)
            {
                mul = int.Parse(mulParts[0]);
                list = mulParts[1];
            }

            while (mul-- > 0)
            {
                yield return list.Split(',').Select(int.Parse).ToList();
            }
        }
        
        private static OptionSet OptionSet { get; } = new OptionSet()
        {
            {
                "d|division=", "A comma separated list of beat segments. Can have a multiplicator prepended ('n*[...]'), indicating that the segment should be repeated n times.",
                (string x) => Divisions.AddRange(ParseDivison(x))
            },
            {
                "s|speed=", "The speed in beats per minute.",
                (float s) => Speed = s
            },
            {
                "h|help", "Show this help screen.",
                x => ShowHelp = x != null
            },
            {
                "version", "Show version.",
                x => { }
            },
        };

        public static bool ParseFromArgs(string[] args)
        {
            if (!args.Any()) ShowHelp = true;
            
            try
            {
                var rem = OptionSet.Parse(args);

                if (!Divisions.Any())
                {
                    Divisions.Add(new List<int>{4});
                }
                
                if (rem.Any())
                {
                    throw new OptionException();
                }
            }
            catch (Exception ex) when (ex is OptionException || ex is InvalidOperationException)
            {
                Console.WriteLine("Invalid Arguments.");
                PrintHelp();
                Environment.Exit(1);
            }

            if (ShowHelp)
            {
                PrintHelp();
                return false;
            }

            return true;
        }

        public static void PrintHelp()
        {
            Console.WriteLine("USAGE: {0} [Options]\n",
                Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().Location));
            Console.WriteLine("OPTIONS:");
            OptionSet.WriteOptionDescriptions(Console.Out);
        }
    }
}