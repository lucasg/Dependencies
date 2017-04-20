using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ClrPh;

namespace ClrPhTester
{
    class Program
    {
        static void Main(string[] args)
        {
            Phlib.InitializePhLib();

            PE Pe = new PE("F:\\Dev\\processhacker2\\TestBt\\ClangDll\\Release\\ClangDll.dll");
        }
    }
}
