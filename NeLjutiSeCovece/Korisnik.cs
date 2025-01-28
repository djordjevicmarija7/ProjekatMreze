﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeLjutiSeCovece
{
    public class Korisnik
    {
        public Korisnik()
        {
        }

        public int Id { get; set; }
        public string Ime { get; set; }
        public List<Figura> Figure { get; set; } = new List<Figura>();

        public int StratPozicija { get; set; }
        public int CiljPozicija { get; set; }

        public Korisnik(int id, string ime, int stratPozicija, int ciljPozicija)
        {
            Id = id;
            Ime = ime;
            StratPozicija = stratPozicija;
            CiljPozicija = ciljPozicija;
            Figure = new List<Figura>();
            for(int i=0; i<4; i++)
            {
                Figura.Add(new Figura { Id = i, Pozicija = -1, Aktivna = false, UdaljenostDoCilja = ciljPozicija });
            }
        }
    }
}
