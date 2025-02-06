using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace KlijentProjekat
{
    public class Klijent
    {
        private const int DefaultPort = 5000;
        private string ServerIp;
        private int Port;


        public Klijent(string serverIp, int port = DefaultPort)
        {
            ServerIp = serverIp;
            Port = port;
        }

        public void Pokreni(string ipServera)
        {
            Socket klijentSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint serverEp = new IPEndPoint(IPAddress.Parse(ipServera), Port);
            try
            {
                
                klijentSocket.Connect(serverEp);
                Console.WriteLine("Povezan sa serverom. Mozete unositi poteze: ");

                while (true)
                {
                    List<Socket> cekanje = new List<Socket> { klijentSocket };
                    Socket.Select(cekanje, null, null, 1000);

                    if(cekanje.Count > 0 ) 
                    {
                        string serverPoruka = PrimiPoruku(klijentSocket);
                        Console.WriteLine(serverPoruka);
                    }

                    Console.WriteLine("Unesite akciju(aktivacija, pomicanje, izvestaj, kraj): ");
                    string akcija = Console.ReadLine()?.Trim();

                    if (string.IsNullOrEmpty(akcija))
                    {
                        Console.WriteLine("Akcija ne moze biti prazna. Pokusajte ponovo.");
                        continue;
                    }
                    if (akcija == "kraj")
                    {
                        PosaljiPoruku(klijentSocket, $"{akcija}\n");
                        string odgovor = PrimiPoruku(klijentSocket);
                        Console.WriteLine($"Odgovor servera: {odgovor}");

                        if (odgovor.Contains("igra je zavrsena"))
                        {
                            break;
                        }

                        continue;
                    }
                    else if (akcija == "aktivacija" || akcija == "pomicanje")
                    {
                        Console.WriteLine("Unesite ID figure(npr. 0, 1, 2...):");
                        string idFigure = Console.ReadLine()?.Trim();

                        Console.WriteLine("Unesite broj polja (za aktivaciju =6 ili broj za pomicanje):");
                        string brojPolja = Console.ReadLine()?.Trim();

                        if (string.IsNullOrEmpty(idFigure) || string.IsNullOrEmpty(brojPolja))
                        {
                            Console.WriteLine("Greška: Svi parametri moraju biti uneseni.");
                            continue;
                        }

                        string poruka = $"{akcija}\n{idFigure}\n{brojPolja}";
                        Console.WriteLine($"Poruka koja se šalje serveru: '{poruka}'");
                        PosaljiPoruku(klijentSocket, poruka);

                        string odgovor = PrimiPoruku(klijentSocket);
                        Console.WriteLine($"Odgovor servera: {odgovor}");
                    }
                    else if (akcija == "izvestaj")
                    {
                        PosaljiPoruku(klijentSocket, "izvestaj");
                        PrimiIzvestajOStanju(klijentSocket);
                    }
                    else
                    {
                        Console.WriteLine("Nepoznata akcija. Pokušajte ponovo.");
                    }
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

        private void PosaljiPoruku(Socket klijentSocket, string poruka)
        {
            byte[] podaci = Encoding.UTF8.GetBytes(poruka);
            klijentSocket.Send(podaci);
        }

        private string PrimiPoruku(Socket klijentSocket)
        {
            byte[] prijemniBafer = new byte[6000];
            int brojPrimljenihBajtova = klijentSocket.Receive(prijemniBafer);
            return Encoding.UTF8.GetString(prijemniBafer, 0, brojPrimljenihBajtova).Trim();
        }
        private void PrimiIzvestajOStanju(Socket klijentSocket)
        {
            string izvestaj = PrimiPoruku(klijentSocket);
            Console.WriteLine($"Izvestaj o stanju igre: {izvestaj}");
        }
        static void Main(string[] args)
        {
            Console.WriteLine("Unesite Ip adresu servera:");
            string serverIp = Console.ReadLine();
            Klijent klijent = new Klijent(serverIp, 5000);
            klijent.Pokreni(serverIp);
        }
    }
}