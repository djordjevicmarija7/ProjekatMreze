using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KlijentProjekat;

namespace NeLjutiSeCovece
{
    public class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Izaberite ulogu: 1. Server, 2.Klijent");
            string izbor = Console.ReadLine();

            if(izbor=="1")
            {
                Igra igra = new Igra();
                Server server= new Server(igra);
                server.Pokreni();
            }
            else if(izbor=="2")
            {
                Console.WriteLine("Unesite Ip adresu servera:");
                string serverIp= Console.ReadLine();
                Klijent klijent = new Klijent(serverIp, 5000);
                klijent.Pokreni(serverIp);
            }
        }
    }
}
