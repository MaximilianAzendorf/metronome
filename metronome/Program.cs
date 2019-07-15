using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace metronome
{
    class Program
    {
        private static DateTime _start;

        private static void PrintHeader()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Console.WriteLine("{0} [Version {1}]",
                ((AssemblyTitleAttribute) assembly.GetCustomAttributes(
                    typeof(AssemblyTitleAttribute)).SingleOrDefault())?.Title,
                Assembly.GetExecutingAssembly().GetName().Version);
            Console.WriteLine("{0}\n",
                ((AssemblyCopyrightAttribute) assembly.GetCustomAttributes(
                    typeof(AssemblyCopyrightAttribute)).SingleOrDefault())?.Copyright);
        }
        private static void PrintVersion()
        {
            Console.WriteLine(Assembly.GetExecutingAssembly().GetName().Version);
        }

        private static int Main(string[] args)
        {
            try
            {
                CultureInfo ci = CultureInfo.InvariantCulture;
                Thread.CurrentThread.CurrentCulture = ci;
                Thread.CurrentThread.CurrentUICulture = ci;
                
#if DEBUG
                Console.WriteLine($"PID: [{Process.GetCurrentProcess().Id}]");
#endif
                if (args.Length == 1 && args[0] == "--version")
                {
                    PrintVersion();
                    return 0;
                }

                PrintHeader();

                if (!Options.ParseFromArgs(args))
                {
                    return 0;
                }

                RunMetronome();

                return 0;
            }
            finally
            {
                Console.ReadKey();
            }
        }

        private static int GetTotalNumberOfBeatsInCycle()
        {
            int beats = 0;
            foreach (var div in Options.Divisions)
            {
                beats += div.Sum();
            }

            return beats;
        }

        private static (int division, int segment, int beat, double segmentFrac) GetCurrentCyclePosition(DateTime start, DateTime time)
        {
            double minutes = (time - start).TotalMinutes;
            double beats = minutes * Options.Speed;
            double cycles = beats / GetTotalNumberOfBeatsInCycle();
            double fracCycle = cycles - Math.Floor(cycles);
            
            double cycleBeat = fracCycle * GetTotalNumberOfBeatsInCycle();
            int division = 0;
            int segment = 0;
            int beat = 0;
            double segmentFrac = 0;
            
            while (cycleBeat > Options.Divisions[division].Sum())
            {
                cycleBeat -= Options.Divisions[division].Sum();
                division++;
            }

            while (cycleBeat > Options.Divisions[division][segment])
            {
                cycleBeat -= Options.Divisions[division][segment];
                segment++;
            }

            segmentFrac = cycleBeat / Options.Divisions[division][segment];

            beat = (int) Math.Floor(cycleBeat);

            return (division, segment, beat, segmentFrac);
        }

        private static string GenBlock(char c, int width, int height)
        {
            StringBuilder b = new StringBuilder();
            string l = new string(c, width);
            while (height-- > 0) b.AppendLine(l);

            return b.ToString();
        }

        private static void SoundThread()
        {
            int lastSeg = -1;
            while (true)
            {
                var pos = GetCurrentCyclePosition(_start, DateTime.Now);

                if (lastSeg != pos.segment)
                {
                    lastSeg = pos.segment;
                    Console.Write("\u0007");
                }

                System.Threading.Thread.Sleep(3);
            }
        }

        private static void RunMetronome()
        {
            _start = DateTime.Now;
            
            Thread t = new Thread(SoundThread);
            t.Start();
            
            int lastLevel = 0;

            int top = 0;
            int left = 0;
            
            while (true)
            {
                var pos = GetCurrentCyclePosition(_start, DateTime.Now);

                if(pos.segmentFrac < 0 || pos.segmentFrac > 1)
                    Console.WriteLine("xxx " + pos);
                
                int level = Console.WindowHeight - (int)Math.Round(pos.segmentFrac * (Console.WindowHeight - 1));

                Console.BackgroundColor = pos.segment != 0 ? ConsoleColor.White : ConsoleColor.Red;
                if (level < lastLevel)
                {
                    string block = GenBlock(' ', Console.WindowWidth, lastLevel - level);
                    Console.CursorTop = level + top;
                    Console.CursorLeft = left;
                    Console.Write(block);
                }
                
                Console.BackgroundColor = ConsoleColor.Black;
                if (level > lastLevel)
                {
                    string block = GenBlock(' ', Console.WindowWidth, level - lastLevel);
                    Console.CursorTop = lastLevel + top;
                    Console.CursorLeft = left;
                    Console.Write(block);
                }

                Console.ForegroundColor = ConsoleColor.Red;
                Console.CursorTop = Console.CursorLeft = 0;
                Console.Write($"   {Options.Divisions[pos.division].Sum()} ({string.Join("+", Options.Divisions[pos.division])}) beats @ {Options.Speed} bpm  ");

                lastLevel = level;
            }
        }
    }
}