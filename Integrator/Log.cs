using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integrator
{
    class Log
    {
        public int seq { get; set; }
        public int seqIntegration { get; set; }
        public DateTime logDate { get; set; }
        public string key { get; set; }
        public string integratedFile { get; set; }
        public string returnFile { get; set; }
        public DateTime initDate { get; set; }
        public DateTime endDate { get; set; }
        public string logStatus { get; set; }
        public string log { get; set; }
        public int tries { get; set; }
        public DateTime lastTryDate { get; set; }
    }
}
