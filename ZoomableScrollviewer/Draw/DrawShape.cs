using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZoomableScrollviewer.Draw
{
    public abstract class DrawShape
    {
        public abstract void OnButtonDown();
        public abstract void OnButtonMove();
        public abstract void OnButtonUp();
    }
}
