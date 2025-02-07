﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace NeLjutiSeCovece
{
    public class Server
    {
        private const int Port = 5000;
        private Socket serverSocket;
        private List<Socket> klijenti = new List<Socket>();
        private int maxIgraca; // dinamički broj igrača (2, 3 ili 4)
        private Igra igra;

        public Server(Igra igra, int maxIgraca)
        {
            if (maxIgraca < 2 || maxIgraca > 4)
                throw new ArgumentException("Broj igrača mora biti 2, 3 ili 4.");

            this.igra = igra;
            this.igra.BrojIgraca = maxIgraca; // prosleđujemo broj igrača modelu igre
            this.maxIgraca = maxIgraca;
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

            // Nakon što se svi igrači povežu, šaljemo inicijalnu poruku sa informacijom ko je prvi na potezu.
            string initialMsg = $"Igra počinje! Trenutni igrač: {igra.TrenutniIgrac().Ime}";
            ObavestiSve(initialMsg);
            Console.WriteLine("Poslat je početni update svim klijentima.");

            // Pokrećemo glavnu petlju igre
            PokreniIgru();
        }
        private void PokreniIgru()
        {
            Console.WriteLine("Igra počinje!");
            // Glavna petlja igre – dok igra nije završena
            while (!igra.Zavrsena)
            {
                foreach (var klijent in klijenti)
                {
                    try
                    {
                        // Koristimo Poll da proverimo da li je klijent spreman za čitanje (1,5 sekundi)
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
                        {
                            // Nema novih podataka – nastavljamo petlju
                            continue;
                        }
                        Console.WriteLine($"Greška pri prijemu podataka od {klijent.RemoteEndPoint}: {ex.Message}");
                    }
                }
                // Pauza radi smanjenja potrošnje CPU resursa
                Thread.Sleep(100);
            }
            // Kada se igra završi, obavesti sve klijente
            ObavestiSve("Igra je završena!");
        }

        private void ObradiPoruku(Socket klijent, string poruka)
        {
          
            // Očekivani format poruke:  
            // Prvi red: akcija ("aktivacija" ili "pomicanje")  
            // Drugi red: ID figure  
            // Treći red: broj polja  
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

            // Validacija poteza (metoda u klasi Igra proverava pravila igre)
            string rezultat = igra.ValidirajPotez(potez);
            PosaljiPoruku(klijent, rezultat);
            if(potez.BrojPolja!=6)
                ObavestiSveOStanjuIgre();
        }

        private void ObavestiSveOStanjuIgre()
        {
            string izvestaj = igra.GenerisiIzvestaj();
            ObavestiSve(izvestaj);
        }

        private void ObavestiSve(string poruka)
        {
            byte[] podaci = Encoding.UTF8.GetBytes(poruka);
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
        static void Main(string[] args)
        {
            // Unos broja igrača
            Console.WriteLine("Unesite broj igrača za igru (2, 3 ili 4):");
            int brojIgraca;
            while (!int.TryParse(Console.ReadLine(), out brojIgraca) || brojIgraca < 2 || brojIgraca > 4)
            {
                Console.WriteLine("Neispravan unos! Molimo unesite 2, 3 ili 4.");
            }

            // Unos dimenzija table – broj ćelija na obimu
            Console.WriteLine("Unesite broj ćelija na obimu table (mora biti veći od 16 i deljiv sa 4):");
            int boardSize;
            while (!int.TryParse(Console.ReadLine(), out boardSize) || boardSize <= 16 || boardSize % 4 != 0)
            {
                Console.WriteLine("Neispravan unos! Broj ćelija mora biti veći od 16 i deljiv sa 4. Pokušajte ponovo:");
            }

            // Računamo dužinu jednog segmenta (kvadranta)
            int segment = boardSize / 4;

            // Kreiramo instancu igre
            Igra igra = new Igra();

            // Određivanje kvadranta (pozicija) na osnovu broja igrača:
            // Ako su 4 igrača, koriste se svi kvadranti [0,1,2,3]
            // Ako su 3 igrača, koristićemo kvadrante [0,1,3] (preskačemo kvadrant 2)
            // Ako su 2 igrača, koristićemo kvadrante [0,2] (suprotne strane)
            int[] quadrantIndices;
            if (brojIgraca == 4)
                quadrantIndices = new int[] { 0, 1, 2, 3 };
            else if (brojIgraca == 3)
                quadrantIndices = new int[] { 0, 1, 3 };
            else // brojIgraca == 2
                quadrantIndices = new int[] { 0, 2 };

            // Dodavanje igrača u igru sa proračunatim početnim pozicijama i krajnjim ciljevima
            for (int i = 0; i < brojIgraca; i++)
            {
                int startPoz = quadrantIndices[i] * segment;
                int ciljPoz = startPoz +boardSize+4;  // Krajnji cilj je za segment unapred

                igra.Igraci.Add(new Korisnik
                {
                    Id = i,
                    Ime = $"Igrac{i + 1}",
                    StratPozicija = startPoz,
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

            // Ispis informacija o inicijalizovanim igračima
            Console.WriteLine("\nInicijalizovani igrači sa početnim pozicijama i krajnjim ciljevima:");
            foreach (var igrac in igra.Igraci)
            {
                Console.WriteLine($"Igrač: {igrac.Ime} | Početak: {igrac.StratPozicija} | Cilj: {igrac.CiljPozicija}");
            }
            Console.WriteLine();

            // Kreiramo server i pokrećemo igru
            Server server = new Server(igra, brojIgraca);
            server.Pokreni();
        }
    }
}

