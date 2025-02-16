
using System;
using System.Collections.Generic;

namespace Biblioteka
{
    public class Igra
    {
        public List<Korisnik> Igraci { get; set; }
        public int TrenutniIgracIndeks { get; set; }
        public bool Zavrsena { get; set; }
        public int BrojIgraca { get; set; }


        public Igra()
        {
            Igraci = new List<Korisnik>();
            TrenutniIgracIndeks = 0;
            Zavrsena = false;
        }

        public Korisnik TrenutniIgrac()
        {

            if (Igraci == null || Igraci.Count == 0)
                throw new InvalidOperationException("Lista igrača je prazna. Dodajte igrače pre početka igre.");

            int index = TrenutniIgracIndeks % Igraci.Count;
            return Igraci[index];
        }

        public void SledeciPotez(bool dodatniPotez)
        {
            if (!dodatniPotez)
            {
                TrenutniIgracIndeks = (TrenutniIgracIndeks + 1) % Igraci.Count;
            }
        }

        public bool IgraJeZavrsena(Korisnik igrac)
        {
            int brojFiguraUKucici = 0;
            int kucicaPocetak = igrac.CiljPozicija - 3;
            foreach (var figura in igrac.Figure)
            {
                if (figura.Aktivna && figura.Pozicija >= kucicaPocetak)
                    brojFiguraUKucici++;
            }
            return brojFiguraUKucici == igrac.Figure.Count;
        }

        public bool DaLiJePotezValidan(Figura figura, int rezultatKocke, int ciljPozicija, Korisnik trenutniIgrac)
        {
            int pocetakKucice = ciljPozicija - 3;
            int ciljPoz = trenutniIgrac.CiljPozicija;
            int novaPozicija = figura.Pozicija + rezultatKocke;

            if (!figura.Aktivna && rezultatKocke == 6)
            {
                return true;
            }

            if (figura.Pozicija < pocetakKucice)
            {
                if (novaPozicija < pocetakKucice)
                {
                    return true;
                }
                else
                {
                    if (novaPozicija > ciljPozicija)
                        return false;

                    foreach (var drugaFigura in trenutniIgrac.Figure)
                    {
                        if (drugaFigura.Id != figura.Id && drugaFigura.Aktivna && drugaFigura.Pozicija == novaPozicija && novaPozicija >= pocetakKucice)
                        {
                            return false;
                        }
                    }
                    return true;
                }


            }
            else
            {
                if (novaPozicija > ciljPozicija)
                    return false;

                foreach (var drugaFigura in trenutniIgrac.Figure)
                {
                    if (drugaFigura.Id != figura.Id && drugaFigura.Aktivna && drugaFigura.Pozicija == novaPozicija && novaPozicija >= pocetakKucice)
                    {
                        return false;
                    }
                }
                return true;
            }
        }


        public void AzurirajFiguru(Figura figura, int rezultatKocke, Korisnik trenutniIgrac)
        {
            if (!figura.Aktivna && rezultatKocke == 6)
            {
                figura.Aktivna = true;
                figura.Pozicija = trenutniIgrac.StartPozicija;
                figura.UdaljenostDoCilja = figura.Pozicija;
            }
            else if (figura.Aktivna)
            {
                figura.Pozicija += rezultatKocke;
                figura.UdaljenostDoCilja -= rezultatKocke;
                if (figura.Pozicija >= (figura.UdaljenostDoCilja - 3))
                {
                    Console.WriteLine($"Cestitamo, figura je u kucici! Trenutna pozicija figure: {figura.Pozicija}");
                }
                Console.WriteLine($"Trenutna pozicija figure: {figura.Pozicija}");
            }
        }

        public bool ProveriPreklapanje(Figura figura, List<Korisnik> igraci, Korisnik trenutniIgrac)
        {
            int pocetakKucice = trenutniIgrac.CiljPozicija - 3;

            if (figura.Pozicija >= pocetakKucice)
                return false;


            foreach (var igrac in igraci)
            {
                if (igrac.Id != trenutniIgrac.Id)
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
            Console.WriteLine($"Validacija poteza: Akcija={potez.Akcija}, Rezultat kockice={potez.BrojPolja}\n");
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

                figura.Pozicija = trenutniIgrac.StartPozicija;
                figura.Aktivna = true;
                if (IgraJeZavrsena(trenutniIgrac))
                {
                    Zavrsena = true;
                    return $" Čestitamo, {trenutniIgrac.Ime} je pobedio!";
                }


                if (potez.BrojPolja != 6)
                {
                    SledeciPotez(false);
                    return "Figura uspešno aktivirana. Vaš potez je završen.";
                }
                else
                {
                    SledeciPotez(true);
                    return "Figura uspešno aktivirana. Imate dodatni potez.";
                }
            }
            else if (potez.Akcija == "pomicanje")
            {
                if (!figura.Aktivna)
                    return "Figura nije aktivna";

                int novaPozicija = figura.Pozicija + potez.BrojPolja;


                if (novaPozicija > trenutniIgrac.CiljPozicija)
                {
                    SledeciPotez(false);
                    return "Prekoračili ste ciljnu poziciju. Vaš potez je završen.";
                }

                if (!DaLiJePotezValidan(figura, potez.BrojPolja, trenutniIgrac.CiljPozicija, trenutniIgrac))
                    return "Potez nije validan.";

                AzurirajFiguru(figura, potez.BrojPolja, trenutniIgrac);


                bool preklapanje = ProveriPreklapanje(figura, Igraci, trenutniIgrac);
                string poruka = preklapanje ?
                    "Figura je presla na poziciju protivničke figure i izbacila je iz igre." :
                    "Potez uspešno izvršen.";
                if (IgraJeZavrsena(trenutniIgrac))
                {
                    Zavrsena = true;
                    return $"Potez uspešno izvršen. Čestitamo, {trenutniIgrac.Ime} je pobedio!";
                }

                if (potez.BrojPolja != 6)
                {
                    SledeciPotez(false);
                    poruka += " Vaš potez je završen.";
                }
                else
                {
                    SledeciPotez(true);
                    poruka += " Imate dodatni potez.";
                }
                return poruka;
            }

            else
            {
                return "nepoznata akcija.";
            }

        }
        public void Resetuj()
        {
            foreach (var igrac in Igraci)
            {

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

    }
}
