using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace Zoom_AutoJoiner
{
    class Program
    {
        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        private delegate bool EventHandler(CtrlType sig);
        static EventHandler _handler;

        enum CtrlType
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT = 1,
            CTRL_CLOSE_EVENT = 2,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT = 6
        }

        static void Main(string[] args)
        {
            Console.WriteLine("It is currently {0}.", DateTime.Now);
            SystemEvents.PowerModeChanged += OnPowerModeChanged;
            _handler += (args) => 
            {
                if (args == (CtrlType.CTRL_CLOSE_EVENT | CtrlType.CTRL_BREAK_EVENT | CtrlType.CTRL_SHUTDOWN_EVENT | CtrlType.CTRL_C_EVENT))
                SystemEvents.PowerModeChanged -= OnPowerModeChanged;
                return false; 
            };
            SetConsoleCtrlHandler(_handler, true);
            Run().GetAwaiter().GetResult();
        }

        static async void OnPowerModeChanged(object sender, PowerModeChangedEventArgs args)
        {
            switch (args.Mode)
            {
                case PowerModes.Resume:
                    Console.WriteLine("Restarting in...");
                    for (int i = 0; i < 10; i++)
                    {
                        Console.WriteLine("{0}...", 10 - i);
                        await Task.Delay(1000);
                    }
                    ProcessStartInfo info = new ProcessStartInfo()
                    {
                        WorkingDirectory = Directory.GetCurrentDirectory()
                    };
                    var p = Process.Start("Zoom AutoJoiner.exe");
                    Environment.Exit(0);
                    break;
                case PowerModes.StatusChange:
                    break;
                case PowerModes.Suspend:
                    break;
            };
        }

        static async Task Run()
        {
            while (true)
            {
                try
                {
                    switch (DateTime.Now.DayOfWeek)
                    {
                        case DayOfWeek.Monday:
                            await ExecuteList(ReadFile("mon.txt"));
                            break;
                        case DayOfWeek.Tuesday:
                            await ExecuteList(ReadFile("tue.txt"));
                            break;
                        case DayOfWeek.Wednesday:
                            await ExecuteList(ReadFile("wed.txt"));
                            break;
                        case DayOfWeek.Thursday:
                            await ExecuteList(ReadFile("thu.txt"));
                            break;
                        case DayOfWeek.Friday:
                            await ExecuteList(ReadFile("fri.txt"));
                            break;
                        case DayOfWeek.Saturday:
                            await ExecuteList(ReadFile("sat.txt"));
                            break;
                        case DayOfWeek.Sunday:
                            await ExecuteList(ReadFile("sun.txt"));
                            break;
                    }
                }
                catch { }
                Console.WriteLine("Waiting until next valid day...");
                await WaitUntil(DateTime.Now.AddDays(1) - DateTime.Now.TimeOfDay + new TimeSpan(0, 0, 1));
            }
        }

        static async Task<bool> WaitUntil(DateTime date)
        {
            if (date < DateTime.Now)
                return false;

            var wait = date - DateTime.Now;
            Console.WriteLine("Waiting for {1} until {0}...", date, wait);

            await Task.Delay(wait);
            return true;
        }

        static async Task ExecuteList(SortedList<DateTime, string> list)
        {
            foreach (var pair in list)
            {
                if (await WaitUntil(pair.Key))
                {
                    Console.WriteLine("Opening {0}...", pair.Value);
                    OpenBrowser(pair.Value);
                }
            }
        }

        static SortedList<DateTime, string> ReadFile(string name)
        {
            string[] text;
            SortedList<DateTime, string> pairs = new SortedList<DateTime, string>();

            if (File.Exists(name))
            {
                text = File.ReadAllLines(name);

                foreach (string s in text)
                {
                    string[] linkPair = s.Split(", ");
                    DateTime date = DateTime.Parse(linkPair[0]);
                    string link = linkPair[1];

                    pairs.Add(date, link);
                }
            }

            return pairs;
        }

        // Taken from https://brockallen.com/2016/09/24/process-start-for-urls-on-net-core/
        public static void OpenBrowser(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {
                // hack because of this: https://github.com/dotnet/corefx/issues/10361
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    throw;
                }
            }
        }
    }
}
