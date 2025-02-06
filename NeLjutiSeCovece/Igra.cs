using KlijentProjekat;
using System;
using System.Collections.Generic;
using System.Text;

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

        public bool ProveriPreklapanje(Figura figura, List<Korisnik> igraci, Korisnik trnutniIgrac)
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
                

                bool preklapanje = ProveriPreklapanje(figura, Igraci, trenutniIgrac);
                if (preklapanje)
                {
                    return "Figura je presla na poziciju protivnicke figure i izbacija je ig igre.";
                }
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
        public void Resetuj()
        {
            foreach(var igrac in Igraci)
            {
                
                igrac.StratPozicija = igrac.Id * 10;
                igrac.CiljPozicija = igrac.StratPozicija + 39;
                

                igrac.Figure = new List<Figura>
                {
                    new Figura{Id=0,Aktivna=false,Pozicija=-1},
                    new Figura{Id=1,Aktivna=false,Pozicija=-1},
                    new Figura{Id=2,Aktivna=false,Pozicija=-1},
                    new Figura{Id=3,Aktivna=false,Pozicija=-1}
                };
            }
            Zavrsena = false;
            Console.WriteLine("Igra je resetovana i spremna za novu sesiju.");
        }
        public string GenerisiIzvestaj()
        {
            StringBuilder izvestaj = new StringBuilder();

            izvestaj.AppendLine("Izvestaj o igri: ");
            foreach (var igrac in Igraci)
            {
                izvestaj.AppendLine($"Igrac: {igrac.Ime}");
                izvestaj.AppendLine($"Pocetna pozicija: {igrac.StratPozicija}, ");

                foreach (var figura in igrac.Figure)
                {
                    string status = figura.Aktivna ? "Aktivna" : "Nije aktivna";
                    izvestaj.AppendLine($"Figura {figura.Id}: {status} | Pozicija: {figura.Pozicija}, Udaljenost do cilja: {figura.UdaljenostDoCilja}");

                }
                izvestaj.AppendLine();
            }
            Korisnik trenutniIgrac = TrenutniIgrac();
            izvestaj.AppendLine($"Trenutni igrac: {trenutniIgrac.Ime}");

            if (Zavrsena)
            {
                izvestaj.AppendLine("Igra je zavrsena!");
            }
            else
            {
                izvestaj.AppendLine("Igra nije zavrsena.");
            }
            return izvestaj.ToString();
        }
    }
}
