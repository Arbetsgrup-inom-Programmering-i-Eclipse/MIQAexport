using EvilDICOM.Core.Modules;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MIQAexport
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Run run = new Run();
            run.Execute();
        }
    }
}
