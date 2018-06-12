using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZeroTrain
{
    class Program
    {
        static void Main(string[] args)
        {
            var t = new TrainPipeline(12, 5);
            t.Run();
        }
    }
}
