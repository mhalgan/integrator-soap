using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integrator
{
    interface IFile
    {
        List<File> GetFiles(List<Segment> segments);
    }
}
