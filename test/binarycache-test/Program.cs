using System;
using System.Diagnostics;
using System.Collections.Generic;

using Dependencies.ClrPh;
using NDesk.Options;

namespace Dependencies
{
    namespace Test
    {
        class Program
        {
            static void ShowHelp(OptionSet p)
            {
                Console.WriteLine("Usage: binarycache [options] <FILES_TO_LOAD>");
                Console.WriteLine();
                Console.WriteLine("Options:");
                p.WriteOptionDescriptions(Console.Out);
            }

            static void Main(string[] args)
            {
                bool is_verbose = false;
                bool show_help = false;

                OptionSet opts = new OptionSet() {
                            { "v|verbose", "redirect debug traces to console", v => is_verbose = v != null },
                            { "h|help",  "show this message and exit", v => show_help = v != null },
                        };

                List<string> eps = opts.Parse(args);

                if (show_help)
                {
                    ShowHelp(opts);
                    return;
                }

                if (is_verbose)
                {
                    // Redirect debug log to the console
                    Trace.Listeners.Add(new TextWriterTraceListener(Console.Out));
                    Debug.AutoFlush = true;
                }

                // always the first call to make
                Phlib.InitializePhLib();

                BinaryCache.Instance.Load();

                foreach ( var peFilePath in eps)
                {
                    PE Pe = BinaryCache.LoadPe(peFilePath);
                    Console.WriteLine("Loaded PE file : {0:s}", Pe.Filepath);
                }


                BinaryCache.Instance.Unload();
            }
        }
    }
}
