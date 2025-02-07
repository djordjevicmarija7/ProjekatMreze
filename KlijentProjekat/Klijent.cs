using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace KlijentProjekat
{
    public class Klijent
    {
        private const int Port = 5000;
        private string ServerIp;
        private Socket klijentSocket;
        private string ime; // Ime lokalnog igrača

        public Klijent(string serverIp, string ime, int port = Port)
        {
            ServerIp = serverIp;
            this.ime = ime;
        }

        public void Pokreni()
        {
            klijentSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint serverEP = new IPEndPoint(IPAddress.Parse(ServerIp), Port);

            try
            {
                klijentSocket.Connect(serverEP);
                Console.WriteLine("Povezan sa serverom. Čekate azuriranje igre...");
                // Postavljamo neblokirajući režim
                klijentSocket.Blocking = false;

                // Beskonačna petlja – klijent stalno prima izveštaje sa servera
                while (true)
                {
                    string update = PrimiPoruku();
                    if (!string.IsNullOrEmpty(update))
                    {
                        Console.WriteLine("\n--- Azuriranje igre ---");
                        Console.WriteLine(update);

                        // Provera: ako je u izveštaju naznačeno da je na potezu
                        // naš igrač (preko linije "Trenutni igrač: <ime>"), onda pokrećemo unos poteza.
                        if (update.Contains($"Trenutni igrač: {ime}"))
                        {
                            Console.WriteLine("\n*** Vaš je potez! ***");
                            ProcessirajPotez();
                        }
                    }
                    // Ako nema poruke, sačekaj malo pre ponovnog pokušaja
                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Greška: {ex.Message}");
            }
            finally
            {
                Console.WriteLine("Zatvaranje veze sa serverom.");
                klijentSocket.Close();
            }
        }

        // Metoda koja omogućava unos poteza od strane igrača
        private void ProcessirajPotez()
        {
            bool turnOngoing = true;
            while (turnOngoing)
            {
                Console.WriteLine("Unesite rezultat bacanja kockice:");
                string diceInput = Console.ReadLine();
                if (!int.TryParse(diceInput, out int diceResult))
                {
                    Console.WriteLine("Neispravan rezultat kockice. Pokušajte ponovo.");
                    continue;
                }

                Console.WriteLine("Unesite akciju (aktivacija, pomicanje):");
                string akcija = Console.ReadLine()?.Trim().ToLower();
                if (string.IsNullOrEmpty(akcija))
                {
                    Console.WriteLine("Akcija ne može biti prazna.");
                    continue;
                }

                Console.WriteLine("Unesite ID figure:");
                string idInput = Console.ReadLine();
                if (!int.TryParse(idInput, out int idFigure))
                {
                    Console.WriteLine("Neispravan unos ID figure.");
                    continue;
                }

                // Sastavljamo poruku (tri reda): akcija, ID figure, rezultat bacanja kockice
                string poruka = $"{akcija}\n{idFigure}\n{diceResult}";
                PosaljiPoruku(poruka);
                string odgovor = PrimiPoruku();
                Console.WriteLine("Odgovor servera: " + odgovor);

                // Ako odgovor sadrži "dodatni potez", igrač dobija još jedan potez,
                // pa se ponovo prikazuje prompt za unos rezultata kockice.
                if (!odgovor.ToLower().Contains("dodatni potez"))
                {
                    turnOngoing = false;
                }
                else
                {
                    Console.WriteLine("Imate dodatni potez. Unesite rezultat bacanja kockice za dodatni potez.");
                    Thread.Sleep(500); // mala pauza radi jasnijeg prikaza
                }
            }
        }


        private void PosaljiPoruku(string poruka)
        {
            byte[] podaci = Encoding.UTF8.GetBytes(poruka);
            try
            {
                klijentSocket.Send(podaci);
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Greška pri slanju poruke: {ex.Message}");
            }
        }

        private string PrimiPoruku()
        {
            byte[] buffer = new byte[2048];
            int brojPokusaja = 0;
            while (true)
            {
                try
                {
                    if (klijentSocket.Poll(1500000, SelectMode.SelectRead))
                    {
                        int primljeno = klijentSocket.Receive(buffer);
                        if (primljeno > 0)
                        {
                            string poruka= Encoding.UTF8.GetString(buffer, 0, primljeno);
                            
                            return poruka;
                        }
                    }
                    else
                    {
                        brojPokusaja++;
                        if (brojPokusaja >= 5)
                        {
                            return "";
                        }
                    }
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.WouldBlock)
                        continue;
                    Console.WriteLine($"Greška pri prijemu poruke: {ex.Message}");
                    break;
                }
            }
            return "";
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Unesite IP adresu servera:");
            string serverIp = Console.ReadLine();

            Console.WriteLine("Unesite vaše ime:");
            string ime = Console.ReadLine();

            Klijent klijent = new Klijent(serverIp, ime);
            klijent.Pokreni();
        }
    }
}
