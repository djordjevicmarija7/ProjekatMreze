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
        private List<Socket> Klijenti;

        public Server(Igra igra)
        {
            Igra = igra;
            Klijenti = new List<Socket>();
        }

        public void Pokreni()
        {
            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
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
            }
        }

        private void ObradiKlijenta(Socket klijentSocket)
        {
            try
            {
                byte[] prijemniBafer = new byte[6000];
                int brojPrimljenihBajtova = klijentSocket.Receive(prijemniBafer);

                if (brojPrimljenihBajtova == 0)
                {
                    Console.WriteLine("Greška: Prazna poruka primljena.");
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
                    return;
                }



                string rezultat = Igra.DaLiJePotezValidan(potez.Id,potez.BrojPolja,);
                byte[] odgovorBafer = Encoding.UTF8.GetBytes(rezultat);
                klijentSocket.Send(odgovorBafer);

                Console.WriteLine($"Rezultat poslat klijentu: {rezultat}");
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
    }
}
