using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace NeLjutiSeCovece
{
    public class Igra
    {
        public List<Korisnik> Igraci { get; set; }
        public int TrenutniIgracIndeks { get; set; }
        public bool Zavrsena { get; set; }

        public Igra()
        {
            Igraci = new List<Korisnik>();
            TrenutniIgracIndeks = 0;
            Zavrsena = false;
        }

        public Korisnik TrenutniIgrac()
        {
            return Igraci[TrenutniIgracIndeks];
        }

        public void SledeciPotez(bool dodatniPotez)
        {
            if (!dodatniPotez)
            {
                TrenutniIgracIndeks = (TrenutniIgracIndeks + 1) % Igraci.Count;
            }
        }
        public bool DaLiJePotezValidan(Figura figura, int rezultatKocke, int CiljPozicija)
        {
            if (!figura.Aktivna && rezultatKocke == 6)
            {
                return true; //aktivacija
            }
            if (figura.Aktivna && figura.Pozicija + rezultatKocke <= CiljPozicija)
            {
                return true; //pomeranje figure unutar granica cilja 
            }
            return false;
        }

        public void AzurirajFiguru(Figura figura, int rezultatKocke)
        {
            if (!figura.Aktivna && rezultatKocke == 6)
            {
                figura.Aktivna = true;
                figura.Pozicija = 0;
                figura.UdaljenostDoCilja = figura.Pozicija;
            }
            else if (figura.Aktivna)
            {
                figura.Pozicija += rezultatKocke;
                figura.UdaljenostDoCilja -= rezultatKocke;
                Console.WriteLine($"Trenutna pozicija figure: {figura.Pozicija}");
            }
        }

        public bool ProveriPrelapanje(Figura figura, List<Korisnik> igraci, Korisnik trnutniIgrac)
        {
            foreach (var igrac in igraci)
            {
                if (igrac.Id != trnutniIgrac.Id)
                {
                    foreach (var protivnickaFigura in igrac.Figure)
                    {
                        if (protivnickaFigura.Aktivna && protivnickaFigura.Pozicija == figura.Pozicija)
                        {
                            protivnickaFigura.Aktivna = false;
                            protivnickaFigura.Pozicija = -1;
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        public string ValidirajPotez(Potez potez)
        {
            Console.WriteLine($"Validacija poteza: Akcija={potez.Akcija}");
            potez.Akcija = potez.Akcija.Trim().ToLower();

            Korisnik trenutniIgrac = TrenutniIgrac();

            if (potez.Id < 0 || potez.Id >= trenutniIgrac.Figure.Count)
            {
                return "Neispravan ID figure";
            }
                Figura figura = trenutniIgrac.Figure[potez.Id];

                if (potez.Akcija == "aktivacija")
                {
                    if (figura.Aktivna)
                        return "Figura je vec aktivna";

                    if (potez.BrojPolja != 6)
                        return "Figura se moze aktivirati samo bacanjem broja 6.";

                    figura.Pozicija = 0;
                    figura.Aktivna = true;
                    return "Figura uspjesno aktivirana.";
                }
                else if (potez.Akcija == "pomicanje")
                {
                    if (!figura.Aktivna)
                        return "Figura nije aktivna";

                    if (!DaLiJePotezValidan(figura, potez.BrojPolja, trenutniIgrac.CiljPozicija))
                        return "Potez nije validan.";

                AzurirajFiguru(figura, potez.BrojPolja);
                    return "Potez uspjesno izvrsen.";
                }
                else if (potez.Akcija == "kraj")
                {
                    SledeciPotez(false);
                    return "Potez zavrsen. Sada je na potezu sljedeci igrac.";
                }
                else
                {
                    return "nepoznata akcija.";
                }
            }
        }
    }
