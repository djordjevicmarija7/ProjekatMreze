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

            while (true)
            {
                Socket klijentSocket = serverSocket.Accept();
                Console.WriteLine("Klijent je povezan.");
                Klijenti.Add(klijentSocket);

                Thread klijentThread = new Thread(() => ObradiKlijenta(klijentSocket));
                klijentThread.Start();

                if (Klijenti.Count == 2)
                {
                    PokreniIgru();
                }
            }
        }

        private void ObradiKlijenta(Socket klijentSocket)
        {
            try
            {
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

                    string poruka = Encoding.UTF8.GetString(prijemniBafer, 0, brojPrimljenihBajtova);
                    Console.WriteLine($"Primljeno: {poruka}");

                    Potez potez;
                    try
                    {
                        string[] dijelovi = poruka.Split('\n');
                        if (dijelovi.Length != 3)
                            throw new FormatException("Poruka mora sadržavati tačno 3 linije: Akcija, IdFigure i BrojPolja.");
                        potez = new Potez
                        {
                            Akcija = dijelovi[0].Trim(),
                            Id = int.Parse(dijelovi[1].Trim()),
                            BrojPolja = int.Parse(dijelovi[2].Trim())
                        };
                        Console.WriteLine($"Primljena akcija: {potez.Akcija}, IdFigure: {potez.Id}, BrojPolja: {potez.BrojPolja}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Greška pri parsiranju poruke: {ex.Message}");
                        byte[] greskaBafer = Encoding.UTF8.GetBytes("Neispravan format poruke.");
                        klijentSocket.Send(greskaBafer);
                        continue;
                    }

                    string rezultat = "Neispravan potez.";

                    Korisnik trenutniIgrac = null;

                    foreach (var igrac in Igra.Igraci)
                    {
                        if (igrac.Id == potez.Id)
                        {
                            trenutniIgrac = igrac;
                            break;
                        }
                    }
                    if (trenutniIgrac != null)
                    {
                        Figura figura = null;
                        foreach (var f in trenutniIgrac.Figure)
                        {
                            if (f.Id == potez.Id)
                            {
                                figura = f;
                                break;
                            }
                        }
                        if (figura != null)
                        {
                            bool validanPotez = Igra.DaLiJePotezValidan(figura, potez.BrojPolja, trenutniIgrac.CiljPozicija);

                            if (validanPotez)
                            {
                                Igra.AzurirajFiguru(figura, potez.BrojPolja);
                                rezultat = $"Potez validan. Nova pozicija figure: {figura.Pozicija}";
                            }
                        }
                    }
                    byte[] odgovorBafer = Encoding.UTF8.GetBytes(rezultat);
                    klijentSocket.Send(odgovorBafer);


                    Console.WriteLine($"Rezultat poslat klijentu: {rezultat}");
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
        private void PokreniIgru()
        {
            Console.WriteLine("Igra zapocinje...");

            for (int i = 0; i < Klijenti.Count; i++)
            {
                int startPozicija = i * 10;
                int ciljPozicija = startPozicija + 39;
                Igra.Igraci.Add(new Korisnik(i, $"Igrac{i + 1}", startPozicija, ciljPozicija));
            }
            while (!Igra.Zavrsena)
            {
                Korisnik trenutniIgrac = Igra.TrenutniIgrac();
                Socket klijent = Klijenti[trenutniIgrac.Id];
                PosaljiPoruku(klijent, "Vas red!Bacite kockicu.");
                ObradiKlijenta(klijent);
                Igra.SledeciPotez(false);

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
    }
}
