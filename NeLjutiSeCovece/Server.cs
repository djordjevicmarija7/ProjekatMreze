using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace NeLjutiSeCovece
{
    public class Server
    {
        private const int Port = 5000;
        private Igra Igra;
        private Socket serverSocket;
        private List<Socket> Klijenti;
        private const int MaxIgraca = 4;

        public Server(Igra igra)
        {
            this.Igra = igra;
            this.Klijenti = new List<Socket>();
        }

        public void Pokreni()
        {
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint serverEp = new IPEndPoint(IPAddress.Any, Port);
            serverSocket.Bind(serverEp);
            serverSocket.Listen(10);

            Console.WriteLine($"Server je pokrenut i čeka klijente na: {serverEp}");

            while (Klijenti.Count < MaxIgraca)
            {
                Socket klijentSocket = serverSocket.Accept();
                Console.WriteLine("Klijent je povezan.");
                Klijenti.Add(klijentSocket);

                Thread klijentThread = new Thread(() => ObradiKlijenta(klijentSocket));
                klijentThread.Start();
            }
            PokreniIgru();
        }

        private void ObradiKlijenta(Socket klijentSocket)
        {
            try
            {
                int igracId = Klijenti.IndexOf(klijentSocket);
                int startPozicija = igracId * 10;
                int ciljPozicija = startPozicija + 39;
                Korisnik igrac = new Korisnik(igracId, $"Igrac{igracId + 1}", startPozicija, ciljPozicija);

                igrac.Figure = new List<Figura>
                {
                    new Figura{Id=0,Aktivna=false,Pozicija=-1},
                    new Figura{Id=1,Aktivna=false,Pozicija=-1},
                    new Figura{Id=2,Aktivna=false,Pozicija=-1},
                    new Figura{Id=3,Aktivna=false,Pozicija=-1}
                };

                Igra.Igraci.Add(igrac);
                Console.WriteLine($"Dodat igrac: {igrac.Ime} sa {igrac.Figure.Count} figura.");

                while (true)
                {
                    byte[] prijemniBafer = new byte[6000];
                    int brojPrimljenihBajtova = klijentSocket.Receive(prijemniBafer);

                    if (brojPrimljenihBajtova == 0)
                    {
                        Console.WriteLine("Greška: Prazna poruka primljena.");
                        klijentSocket.Close();
                        return;
                    }

                    string poruka = Encoding.UTF8.GetString(prijemniBafer, 0, brojPrimljenihBajtova).Trim();
                    Console.WriteLine($"Primljeno: {poruka}");

                    if(poruka.Equals("kraj",StringComparison.OrdinalIgnoreCase))
                    {
                        Console.WriteLine("Korisnik je zavrsio potez.");
                        Igra.SledeciPotez(false);
                        ObavestiSveOStanjuIgre();
                        continue;
                    }

                    Potez potez;
                    try
                    {
                        string[] dijelovi = poruka.Split('\n');
                       if(dijelovi.Length==1 && dijelovi[0].Trim().Equals("kraj"))
                        {
                            Console.WriteLine("Korisnik je zavrsio potez");
                            Igra.SledeciPotez(false);
                            ObavestiSveOStanjuIgre();
                            continue;
                        }
                        if (dijelovi.Length != 3)
                            throw new FormatException("Poruka mora sadržavati tačno 3 linije: Akcija, IdFigure i BrojPolja.");
                        potez = new Potez
                        {
                            Akcija = dijelovi[0].Trim(),
                            Id = int.Parse(dijelovi[1].Trim()),
                            BrojPolja = int.Parse(dijelovi[2].Trim())
                        };
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Greška pri parsiranju poruke: {ex.Message}");
                        byte[] greskaBafer = Encoding.UTF8.GetBytes("Neispravan format poruke.");
                        klijentSocket.Send(greskaBafer);
                        continue;
                    }

                    string rezultat1 = Igra.ValidirajPotez(potez);
                    byte[] odgovorBafer = Encoding.UTF8.GetBytes(rezultat1);
                    klijentSocket.Send(odgovorBafer);

                    Console.WriteLine($"Rezultat poslat klijentu: {rezultat1}");
                    ObavestiSveOStanjuIgre();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška pri obradi klijenta: {ex.Message}");
            }
            finally
            {
                Console.WriteLine("Zatvaranje konekcije sa klijentom.");
                klijentSocket.Close();
            }
        }

        private void ObavestiSveOStanjuIgre()
        {
            string izvestaj = Igra.GenerisiIzvestaj();
            foreach(var klijent in Klijenti)
            {
                PosaljiPoruku(klijent, izvestaj);
            }
        }
        private void PokreniIgru()
        {
            Console.WriteLine("Igra zapocinje...");

            while (!Igra.Zavrsena)
            {
                Korisnik trenutniIgrac = Igra.TrenutniIgrac();

                if (trenutniIgrac.Id >= Klijenti.Count || trenutniIgrac.Id < 0)
                {
                    Console.WriteLine("Greska: Nepoznat Id trenutnog igraca.");
                    break;
                }

                Socket klijent = Klijenti[trenutniIgrac.Id];
                PosaljiPoruku(klijent, "Vas red! Bacite kockicu.");
                ObradiKlijenta(klijent);
               
            }
            ObavestiSve("Igra je zavrsena!");
        }
        private void PosaljiPoruku(Socket klijent, string poruka)
        {
            byte[] podaci = Encoding.UTF8.GetBytes(poruka);
            klijent.Send(podaci);
        }
        private void ObavestiSve(string poruka)
        {
            foreach (var klijent in Klijenti)
            {
                PosaljiPoruku(klijent, poruka);
            }
        }
        static void Main(string[] args)
        {
            Igra igra = new Igra();
            Server server = new Server(igra);
            server.Pokreni();
        }
    }
}
