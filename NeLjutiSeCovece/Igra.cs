using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeLjutiSeCovece
{
    public class Igra
    {
        public int velicinaTable { get; set; }
        public List<Korisnik> Igraci { get; set; } = new List<Korisnik>();
        public int trenutniIgracIndeks { get; set; } = 0;
        public void sledeciPotez()
        {
            trenutniIgracIndeks = (trenutniIgracIndeks + 1) % Igraci.Count;
        }

        public Korisnik DohvatiTrenutnogIgraca()
        {
            return Igraci[trenutniIgracIndeks];
        }

        public string ValidirajPotez(Potez potez)
        {
            Console.WriteLine($"Validacija poteza: Akcija={potez.Akcija}");
            potez.Akcija = potez.Akcija.Trim().ToLower();

            if (potez.Akcija == "aktivacija")
            {
                if (potez.brojPolja != 6)
                    return "Figura se moze aktivirati samo bacanjem broja 6.";

                Korisnik trenutniIgrac = DohvatiTrenutnogIgraca();
                Figura figura = trenutniIgrac.Figure[potez.IdFigure];

                if (figura.jeAktivna)
                    return "Figura je vec aktivna.";

                figura.Pozicija = 0;
                return "Figura je uspjesno aktivirana.";
            }


            if (potez.Akcija == "pomicanje")
            {
                Korisnik trenutniIgrac = DohvatiTrenutnogIgraca();
                Figura figura = trenutniIgrac.Figure[potez.IdFigure];

                if (!figura.jeAktivna)
                    return "Figura nije aktivna.";

                figura.Pozicija += potez.brojPolja;
                return "Potez uspesno izvrsen.";
            }
            if (potez.Akcija == "kraj poteza")
            {
                sledeciPotez();
                return "Potez zavrsen. Sada je na potezu sljedeci igrac.";
            }

            return "Nepoznata akcija.";

        }
    }
}
