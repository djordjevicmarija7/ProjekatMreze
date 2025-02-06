using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace NeLjutiSeCovece
{
    public class Server
    {
        private const int Port = 5000;
        private Igra Igra;
        private Socket serverSocket;
        private List<Socket> Klijenti;
        private const int MaxIgraca = 4;
        private const int MinIgraca = 2;

        public Server(Igra igra)
        {
            this.Igra = igra;
            this.Klijenti = new List<Socket>();
        }

        public void Pokreni()
        {
            try
            {
              
                serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint serverEp = new IPEndPoint(IPAddress.Any, Port);
                serverSocket.Bind(serverEp);
                serverSocket.Listen(10);

               
                serverSocket.Blocking = false;

                Console.WriteLine($"Server je pokrenut i čeka klijente na: {serverEp}");

               
                while (Klijenti.Count < MaxIgraca)
                {
                    try
                    {
                        if (serverSocket.Poll(1000, SelectMode.SelectRead))
                        {
                            Socket klijentSocket = serverSocket.Accept();
                            klijentSocket.Blocking = false;
                            Console.WriteLine($"Klijent povezan sa {klijentSocket.RemoteEndPoint}");
                            Klijenti.Add(klijentSocket);

                         
                            int igracId = Klijenti.IndexOf(klijentSocket);
                            string imeIgraca = GetImeIgraca(igracId);

                            Korisnik igrac = new Korisnik(igracId, imeIgraca, igracId * 10, igracId * 10 + 39);
                            igrac.Figure = new List<Figura>
                            {
                                new Figura{Id=0, Aktivna=false, Pozicija=-1},
                                new Figura{Id=1, Aktivna=false, Pozicija=-1},
                                new Figura{Id=2, Aktivna=false, Pozicija=-1},
                                new Figura{Id=3, Aktivna=false, Pozicija=-1}
                            };

                            Igra.Igraci.Add(igrac);
                            Console.WriteLine($"Dodat igrac: {igrac.Ime}");
                            PosaljiPoruku(klijentSocket, $"Dobrodošli, vaš ID je {igracId}");
                        }
                    }
                    catch (SocketException e)
                    {
                        if (e.SocketErrorCode == SocketError.WouldBlock)
                        {
                          
                            continue;
                        }
                        Console.WriteLine($"Greška pri prihvatanju novog klijenta: {e.Message}");
                    }
                }

                
                if (Klijenti.Count >= MinIgraca)
                {
                    ObavestiSve("Igra počinje!");
                    Console.WriteLine("Svi igrači su povezani. Igra može da počne!");
                }

               
                while (!Igra.Zavrsena)
                {
                    Korisnik trenutniIgrac = Igra.TrenutniIgrac();
                    Console.WriteLine($"Na redu je: {trenutniIgrac.Ime}");
                    PosaljiPoruku(Klijenti[trenutniIgrac.Id], "Vaš red! Bacite kockicu.");

                    List<Socket> spremniZaCitanje = new List<Socket>(Klijenti);
                    Socket.Select(spremniZaCitanje, null, null, 1000);

                    foreach (Socket klijent in spremniZaCitanje)
                    {
                        byte[] prijemniBafer = new byte[6000];
                        int brojPrimljenihBajtova = klijent.Receive(prijemniBafer);

                        if (brojPrimljenihBajtova == 0)
                        {
                            Console.WriteLine("Greška: Prazna poruka primljena.");
                            klijent.Close();
                            Klijenti.Remove(klijent);
                            continue;
                        }

                        string poruka = Encoding.UTF8.GetString(prijemniBafer, 0, brojPrimljenihBajtova).Trim();
                        Console.WriteLine($"Primljeno: {poruka}");

                        
                        if (poruka.Equals("kraj", StringComparison.OrdinalIgnoreCase))
                        {
                            Igra.SledeciPotez(false);
                            ObavestiSveOStanjuIgre();
                        }
                        else if (poruka == "izvestaj")
                        {
                            ObavestiSveOStanjuIgre();
                        }
                        else
                        {
                            
                            try
                            {
                                string[] dijelovi = poruka.Split('\n');
                                if (dijelovi.Length != 3)
                                    throw new FormatException("Poruka mora sadržavati tačno 3 linije: Akcija, IdFigure i BrojPolja.");

                                Potez potez = new Potez
                                {
                                    Akcija = dijelovi[0].Trim(),
                                    Id = int.Parse(dijelovi[1].Trim()),
                                    BrojPolja = int.Parse(dijelovi[2].Trim())
                                };

                                string rezultat1 = Igra.ValidirajPotez(potez);
                                byte[] odgovorBafer = Encoding.UTF8.GetBytes(rezultat1);
                                klijent.Send(odgovorBafer);
                                Console.WriteLine($"Rezultat poslat klijentu: {rezultat1}");
                                ObavestiSveOStanjuIgre();
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Greška pri parsiranju poruke: {ex.Message}");
                                byte[] greskaBafer = Encoding.UTF8.GetBytes("Neispravan format poruke.");
                                klijent.Send(greskaBafer);
                            }
                        }
                    }
                }

                ObavestiSve("Igra je završena!");
                Console.WriteLine("Igra je završena. Svi igrači su obavešteni!");
                ZatvoriSve();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška u mrežnoj komunikaciji: {ex.Message}");
                ZatvoriSve();
            }
        }

        private void ZatvoriSve()
        {
            foreach (Socket klijent in Klijenti)
            {
                klijent.Send(Encoding.UTF8.GetBytes("Server je završio sa radom."));
                klijent.Close();
            }
            serverSocket.Close();
        }

        private string GetImeIgraca(int igracId)
        {

            if (igracId == 0)
            {
                return "Crvena";
            }
            else if (igracId == 1)
            {
                return "Plava";
            }
            else if (igracId == 2)
            {
                return "Zelena";
            }
            else if (igracId == 3)
            {
                return "Žuta";
            }
            else
            {
                return "Nepoznato";
            }
        }

        private void ObavestiSveOStanjuIgre()
        {
            string izvestaj = Igra.GenerisiIzvestaj();
            Console.WriteLine($"Stanje igre: {izvestaj}");
            ObavestiSve(izvestaj);
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
