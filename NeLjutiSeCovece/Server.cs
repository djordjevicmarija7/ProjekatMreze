using Biblioteka;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;

namespace NeLjutiSeCovece
{
    public class Server
    {
        private const int Port = 5000;
        private Socket serverSocket;
        private List<Socket> klijenti = new List<Socket>();
        private int maxIgraca;
        private Igra igra;
        private Dictionary<Socket, int> klijentIgracIndeks = new Dictionary<Socket, int>();
        private List<string> dostupneBoje = new List<string>();

        public Server(Igra igra, int brIgraca)
        {
            if (brIgraca < 2 || brIgraca > 4)
                throw new ArgumentException("Broj igrača mora biti 2, 3 ili 4.");

            this.igra = igra;
            this.igra.BrojIgraca = brIgraca;
            this.maxIgraca = brIgraca;

            if (brIgraca == 4)
                dostupneBoje.AddRange(new[] { "crvena", "plava", "zuta", "zelena" });
            else if (brIgraca == 3)
                dostupneBoje.AddRange(new[] { "crvena", "plava", "zelena" });
            else
                dostupneBoje.AddRange(new[] { "crvena", "zelena" });
        }

        public void Pokreni()
        {
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Bind(new IPEndPoint(IPAddress.Any, Port));
            serverSocket.Listen(maxIgraca);
            serverSocket.Blocking = false;
            Console.WriteLine($"Server pokrenut na portu {Port}. Čekam {maxIgraca} igrača da se povežu...");

            while (klijenti.Count < maxIgraca)
            {
                if (serverSocket.Poll(1000000, SelectMode.SelectRead))
                {
                    try
                    {
                        Socket clientSocket = serverSocket.Accept();
                        clientSocket.Blocking = false;
                        klijenti.Add(clientSocket);

                        int igracIndeks = klijenti.Count - 1;
                        klijentIgracIndeks[clientSocket] = igracIndeks;

                        if (dostupneBoje.Count > 0)
                        {
                            string dodeljenaBoja = dostupneBoje[0];
                            dostupneBoje.RemoveAt(0);
                            igra.Igraci[igracIndeks].Ime = dodeljenaBoja;
                            PosaljiPoruku(clientSocket, "TEXT:Boja uspseno odabrana: " + dodeljenaBoja);
                            Console.WriteLine($"Klijent {clientSocket.RemoteEndPoint} je dodeljen boji: {dodeljenaBoja}");
                        }
                        else
                        {

                            PosaljiPoruku(clientSocket, "TEXT:Nema dostupnih boja!");
                        }

                        Console.WriteLine($"Klijent povezan: {clientSocket.RemoteEndPoint} (ukupno: {klijenti.Count}/{maxIgraca})");
                    }
                    catch (SocketException ex)
                    {
                        Console.WriteLine($"Greška pri prihvatanju konekcije: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("Čekam nove klijente...");
                }
            }

            string pocetnaPoruka = $"Igra počinje! Trenutni igrač: {igra.TrenutniIgrac().Ime}";
            ObavestiSve(pocetnaPoruka);
            Console.WriteLine("Poslat je početni update svim klijentima.");

            PokreniIgru();
        }

        private void PokreniIgru()
        {
            Console.WriteLine("Igra počinje!");

            while (!igra.Zavrsena)
            {
                foreach (var klijent in klijenti)
                {
                    try
                    {
                        if (klijent.Poll(1500000, SelectMode.SelectRead))
                        {
                            byte[] buffer = new byte[1024];
                            int primljeno = klijent.Receive(buffer);
                            if (primljeno > 0)
                            {
                                string poruka = Encoding.UTF8.GetString(buffer, 0, primljeno).Trim();
                                Console.WriteLine($"Primljena poruka od {klijent.RemoteEndPoint}: {poruka}");
                                ObradiPoruku(klijent, poruka);
                            }
                        }
                    }
                    catch (SocketException ex)
                    {
                        if (ex.SocketErrorCode == SocketError.WouldBlock)
                            continue;
                        Console.WriteLine($"Greška pri prijemu podataka od {klijent.RemoteEndPoint}: {ex.Message}");
                    }
                }
                Thread.Sleep(100);
            }

            GameOver();
        }

        private void ObradiPoruku(Socket klijent, string poruka)
        {

            if (igra.Zavrsena)
            {
                PosaljiPoruku(klijent, "TEXT:Igra je završena, čekajte novu partiju.");
                return;
            }

            Potez potez;
            try
            {
                string[] delovi = poruka.Split('\n');
                if (delovi.Length != 3)
                    throw new FormatException("Neispravan format poruke.");

                potez = new Potez
                {
                    Akcija = delovi[0].Trim(),
                    Id = int.Parse(delovi[1].Trim()),
                    BrojPolja = int.Parse(delovi[2].Trim())
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška pri parsiranju poruke: {ex.Message}");
                PosaljiPoruku(klijent, "Neispravan format poruke.");
                return;
            }

            string rezultat = igra.ValidirajPotez(potez);
            PosaljiPoruku(klijent, rezultat);
            if (potez.BrojPolja != 6)
                ObavestiSveOStanjuIgre();
        }

        private void ObavestiSveOStanjuIgre()
        {
            try
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(ms, igra.Igraci);
                    byte[] serijalizovaniPodaci = ms.ToArray();
                    ObavestiSveSerijalizovano(serijalizovaniPodaci);
                }

                Thread.Sleep(1500);
                if (!igra.Zavrsena)
                {
                    string naRedu = $"Trenutni igrač: {igra.TrenutniIgrac().Ime}";
                    ObavestiSve(naRedu);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greska pri serijalizaciji izvestaja: {ex.Message}");
            }
        }

        private void ObavestiSve(string poruka)
        {
            string porukaSaHeaderom = "TEXT:" + poruka;
            byte[] podaci = Encoding.UTF8.GetBytes(porukaSaHeaderom);
            foreach (var klijent in klijenti)
            {
                try
                {
                    klijent.Send(podaci);
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"Greška pri slanju poruke klijentu {klijent.RemoteEndPoint}: {ex.Message}");
                }
            }
        }

        private void ObavestiSveSerijalizovano(byte[] serijalizovaniPodaci)
        {
            byte[] header = Encoding.UTF8.GetBytes("IZVESTAJ:");
            byte[] porukaSaHeaderom = new byte[header.Length + serijalizovaniPodaci.Length];
            Buffer.BlockCopy(header, 0, porukaSaHeaderom, 0, header.Length);
            Buffer.BlockCopy(serijalizovaniPodaci, 0, porukaSaHeaderom, header.Length, serijalizovaniPodaci.Length);

            foreach (var klijent in klijenti)
            {
                try
                {
                    klijent.Send(porukaSaHeaderom);
                }
                catch (SocketException ex)
                {
                    Console.WriteLine($"Greška pri slanju izveštaja klijentu {klijent.RemoteEndPoint}: {ex.Message}");
                }
            }
        }

        private void PosaljiPoruku(Socket klijent, string poruka)
        {
            byte[] podaci = Encoding.UTF8.GetBytes(poruka);
            try
            {
                klijent.Send(podaci);
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Greška pri slanju poruke klijentu {klijent.RemoteEndPoint}: {ex.Message}");
            }
        }

        private void GameOver()
        {
            int prethodniBrojIgraca = klijenti.Count;

            ObavestiSve("Igra je završena!\n");
            ObavestiSve("Ukoliko želite da odigrate još jednu partiju unesite 'DA'.");
            Console.WriteLine("Čekam prijave za novu igru (15 sekundi)...");

            List<Socket> ponovoPovezaniKlijenti = new List<Socket>();
            DateTime pocetak = DateTime.Now;
            TimeSpan cekanje = TimeSpan.FromSeconds(15);

            while (DateTime.Now - pocetak < cekanje)
            {
                foreach (var klijent in klijenti.ToArray())
                {
                    try
                    {
                        if (klijent.Poll(500000, SelectMode.SelectRead))
                        {
                            byte[] buffer = new byte[1024];
                            int primljeno = klijent.Receive(buffer);
                            if (primljeno > 0)
                            {
                                string odgovor = Encoding.UTF8.GetString(buffer, 0, primljeno).Trim();
                                if (odgovor.Equals("DA", StringComparison.OrdinalIgnoreCase))
                                {
                                    if (!ponovoPovezaniKlijenti.Contains(klijent))
                                    {
                                        ponovoPovezaniKlijenti.Add(klijent);
                                        Console.WriteLine($"Klijent {klijent.RemoteEndPoint} se ponovo prijavio.");
                                    }
                                }
                            }
                        }
                    }
                    catch (SocketException ex)
                    {
                        if (ex.SocketErrorCode != SocketError.WouldBlock)
                        {
                            Console.WriteLine($"Greška kod klijenta {klijent.RemoteEndPoint}: {ex.Message}");
                            klijenti.Remove(klijent);
                        }
                    }
                }
                Thread.Sleep(100);
            }

            while (ponovoPovezaniKlijenti.Count < prethodniBrojIgraca)
            {
                Console.WriteLine($"Trenutno ima {ponovoPovezaniKlijenti.Count} prijavljenih, čekam nove klijente...");
                if (serverSocket.Poll(1000000, SelectMode.SelectRead))
                {
                    try
                    {
                        Socket noviKlijent = serverSocket.Accept();
                        noviKlijent.Blocking = false;
                        ponovoPovezaniKlijenti.Add(noviKlijent);
                        Console.WriteLine($"Novi klijent se povezao: {noviKlijent.RemoteEndPoint}");
                    }
                    catch (SocketException ex)
                    {
                        Console.WriteLine($"Greška pri prihvatanju nove konekcije: {ex.Message}");
                    }
                }
                Thread.Sleep(100);
            }

            if (ponovoPovezaniKlijenti.Count == 0)
            {
                Console.WriteLine("Svi klijenti su se diskonektovali. Server završava sa radom.");
                Environment.Exit(0);
            }

            klijenti = ponovoPovezaniKlijenti;
            Console.WriteLine("Svi igrači su se prijavili za novu igru. Igra se resetuje.");

            igra.Resetuj();
            Thread.Sleep(500);
            string pocetnaPoruka = $"Igra počinje! Trenutni igrač: {igra.TrenutniIgrac().Ime}";
            ObavestiSve(pocetnaPoruka);
            PokreniIgru();
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Unesite broj igrača za igru (2, 3 ili 4):");
            int brojIgraca;
            while (!int.TryParse(Console.ReadLine(), out brojIgraca) || brojIgraca < 2 || brojIgraca > 4)
            {
                Console.WriteLine("Neispravan unos! Molimo unesite 2, 3 ili 4.");
            }

            Console.WriteLine("Unesite broj ćelija na obimu table (mora biti veći od 16 i deljiv sa 4):");
            int velicinaTabele;
            while (!int.TryParse(Console.ReadLine(), out velicinaTabele) || velicinaTabele <= 16 || velicinaTabele % 4 != 0)
            {
                Console.WriteLine("Neispravan unos! Broj ćelija mora biti veći od 16 i deljiv sa 4. Pokušajte ponovo:");
            }

            int segment = velicinaTabele / 4;
            Igra igra = new Igra();

            int[] kvadrant;
            if (brojIgraca == 4)
                kvadrant = new int[] { 0, 1, 2, 3 };
            else if (brojIgraca == 3)
                kvadrant = new int[] { 0, 1, 3 };
            else
                kvadrant = new int[] { 0, 2 };

            for (int i = 0; i < brojIgraca; i++)
            {
                int startPoz = kvadrant[i] * segment;
                int ciljPoz = startPoz + velicinaTabele + 4;
                igra.Igraci.Add(new Korisnik
                {
                    Id = i,
                    Ime = "",
                    StartPozicija = startPoz,
                    CiljPozicija = ciljPoz,
                    Figure = new List<Figura>
                    {
                        new Figura { Id = 0, Aktivna = false, Pozicija = -1, UdaljenostDoCilja = ciljPoz },
                        new Figura { Id = 1, Aktivna = false, Pozicija = -1, UdaljenostDoCilja = ciljPoz },
                        new Figura { Id = 2, Aktivna = false, Pozicija = -1, UdaljenostDoCilja = ciljPoz },
                        new Figura { Id = 3, Aktivna = false, Pozicija = -1, UdaljenostDoCilja = ciljPoz }
                    }
                });
            }

            Console.WriteLine("\nInicijalizovani igrači sa početnim pozicijama i krajnjim ciljevima:");
            foreach (var igrac in igra.Igraci)
            {
                Console.WriteLine($"Igrač: {igrac.Ime} | Početak: {igrac.StartPozicija} | Cilj: {igrac.CiljPozicija}");
            }
            Console.WriteLine();

            Server server = new Server(igra, brojIgraca);
            server.Pokreni();
        }
    }
}

