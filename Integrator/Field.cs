using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integrator
{
    class Field
    {
        public int seq { get; set; }
        public string tag { get; set; }
        public string dbColumn { get; set; }
        public int? size { get; set; }
        public int? order { get; set; }
        public int? position { get; set; } //Apenas para HL7
        public bool key { get; set; }
        public bool hide { get; set; }
        public string mask { get; set; } //Apenas para campos do tipo date
    }
}
