﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeLjutiSeCovece
{
    public class Potez
    {
        public Potez()
        {
        }

        public int Id { get; set; }
        public string Akcija { get; set; }
        public int BrojPolja { get; set; }

        public Potez(int id, string akcija, int brojPolja)
        {
            Id = id;
            Akcija = akcija;
            BrojPolja = brojPolja;
        }
    }
}
