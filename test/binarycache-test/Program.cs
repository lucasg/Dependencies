using System;
using System.Diagnostics;
using Dependencies.ClrPh;

namespace Dependencies
{
    namespace Test
    {
        class Program
        {
            static void Main(string[] args)
            {
                // always the first call to make
                Phlib.InitializePhLib();

                // Redirect debug log to the console
                Debug.Listeners.Add(new TextWriterTraceListener(Console.Out));
                Debug.AutoFlush = true;

                BinaryCache.Instance.Load();

                foreach ( var peFilePath in args )
                {
                    PE Pe = BinaryCache.LoadPe(peFilePath);
                }


                BinaryCache.Instance.Unload();
            }
        }
    }
}
