using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Klijent
{
    public class Klijent
    {
        private const int Port = 5000;

        public void Pokreni(string ipServera)
        {
            Socket klijentSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint serverEp = new IPEndPoint(IPAddress.Parse(ipServera), Port);

            klijentSocket.Connect(serverEp);
            Console.WriteLine("Povezan sa serverom.Unesite vase poteze:");

            while (true)
            {
                Console.WriteLine("Unesite akciju (aktivacija, pomicanje, kraj poteza):");
                string akcija = Console.ReadLine()?.Trim();

                if (akcija == "kraj poteza")
                {
                    Console.WriteLine("Zavrsavate potez.");
                    byte[] podaci = Encoding.UTF8.GetBytes($"{akcija}\n");
                    klijentSocket.Send(podaci);

                    byte[] prijemniBafer = new byte[6000];
                    int brojPrimljenihBajtova = klijentSocket.Receive(prijemniBafer);
                    string odgovor = Encoding.UTF8.GetString(prijemniBafer, 0, brojPrimljenihBajtova);

                    Console.WriteLine($"Odgovor servera:{odgovor}");

                    if (odgovor.Contains("sljedeci igrac"))
                        break;

                    continue;
                }
                if (akcija == "aktivacija" || akcija == "pomicanje")
                {
                    Console.WriteLine("Unesite ID figure (npr. 0,1,2..):");
                    string idFigure = Console.ReadLine()?.Trim();

                    Console.WriteLine("Unesite broj polja (npr za Aktivaciju = 6 ili broj za pomjeranje):");
                    string brojPolja = Console.ReadLine()?.Trim();

                    if (string.IsNullOrEmpty(akcija) || string.IsNullOrEmpty(idFigure) || string.IsNullOrEmpty(brojPolja))
                    {
                        Console.WriteLine("Greska: svi parametri moraju biti uneseni.");
                        continue;

                    }

                    string poruka = $"{akcija}\n{idFigure}\n{brojPolja}";
                    Console.WriteLine($"Poruka koja se salje serveru: '{poruka}'");
                    byte[] podaci = Encoding.UTF8.GetBytes(poruka);
                    klijentSocket.Send(podaci);

                    byte[] prijemniBafer = new byte[6000];
                    int brojPrimljenihBajtova = klijentSocket.Receive(prijemniBafer);
                    string odgovor = Encoding.UTF8.GetString(prijemniBafer, 0, brojPrimljenihBajtova);

                    Console.WriteLine($"Odgovor servera: {odgovor}");
                }
                else
                {
                    Console.WriteLine("Nepoznata akcija.Pokusajte ponovo.");
                }
            }
            klijentSocket.Close();
        }
    }
}
