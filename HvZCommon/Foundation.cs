using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace HvZ.Common {
    public interface IVisual : ITakeSpace {
        string Texture { get; }
    }
}
