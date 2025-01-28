using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeLjutiSeCovece
{
    public class Figura
    {
        public int Pozicija { get; set; } = -1;
        public bool jeAktivna => Pozicija >= 0;
    }
}
