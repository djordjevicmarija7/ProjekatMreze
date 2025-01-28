using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NeLjutiSeCovece
{
    public class Server
    {
        private const int Port = 5000;
        private Igra igra;

        public Server(Igra igra)
        {
            this.igra = igra;
        }

        public void Pokreni()
        {
            Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint serverEp = new IPEndPoint(IPAddress.Any, Port);
            serverSocket.Bind(serverEp);
            serverSocket.Listen(10);

            Console.WriteLine($"Server je pokrenut i ceka klijente na : {serverEp}");

            while (true)
            {
                Socket klijentSocket = serverSocket.Accept();
                Console.WriteLine("Klijnet je povezan.");

                byte[] prijemniBafer = new byte[6000];
                int brojPrimljenihBajtova = klijentSocket.Receive(prijemniBafer);

                if (brojPrimljenihBajtova == 0)
                {
                    Console.WriteLine("Greška: Prazna poruka primljena.");
                    klijentSocket.Close();
                    continue;
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
                        IdFigure = int.Parse(dijelovi[1].Trim()),
                        brojPolja = int.Parse(dijelovi[2].Trim())
                    };
                    Console.WriteLine($"Primljena akcija: {potez.Akcija},IdFigure: {potez.IdFigure}, Broj Polja: {potez.brojPolja}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Greska pri parsiranju poruke: {ex.Message}");
                    byte[] greskaBafer = Encoding.UTF8.GetBytes("Nespravan format poruke.");
                    klijentSocket.Send(greskaBafer);
                    klijentSocket.Close();
                    continue;
                }
                string rezultat = igra.ValidirajPotez(potez);

                byte[] odgovorBafer = Encoding.UTF8.GetBytes(rezultat);
                klijentSocket.Send(odgovorBafer);

                klijentSocket.Close();
            }
        }
    }
}
