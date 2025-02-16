using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Biblioteka
{
    public static class Izvestaj
    {
        public static void PrikaziIzvestaj(byte[] podaci)
        {
            string header = "IZVESTAJ:";

            string podaciString = Encoding.UTF8.GetString(podaci);
            if (!podaciString.StartsWith(header))
            {
                Console.WriteLine("Podaci ne sadrze validan header za izvestaj.");
                return;
            }

            byte[] izvestajPodaci = new byte[podaci.Length - header.Length];
            Array.Copy(podaci, header.Length, izvestajPodaci, 0, izvestajPodaci.Length);
            try
            {
                Thread.Sleep(1000);
                using (MemoryStream ms = new MemoryStream(izvestajPodaci))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    var listaIgraca = (List<Korisnik>)bf.Deserialize(ms);
                    Console.WriteLine("\n--- Ažuriranje igre (Izveštaj) ---");
                    foreach (var igrac in listaIgraca)
                    {
                        Console.WriteLine($"Igrač: {igrac.Ime}");
                        Console.WriteLine($"Pocetna pozicija: {igrac.StartPozicija}, ciljna pozicija: {igrac.CiljPozicija}");
                        foreach (var figura in igrac.Figure)
                        {
                            string status = figura.Aktivna ? "Aktivna" : "Nije aktivna";
                            if (figura.Pozicija == -1)
                            {
                                Console.WriteLine($"Figura {figura.Id}: {status} | Pozicija: {figura.Pozicija} | Nije na tabli");
                            }
                            else if (figura.Pozicija < igrac.CiljPozicija - 3)
                            {
                                int udaljenostDoKucice = igrac.CiljPozicija - 3 - figura.Pozicija;
                                Console.WriteLine($"Figura {figura.Id}: {status} | Pozicija: {figura.Pozicija} | Udaljenost do kucice: {udaljenostDoKucice}");
                            }
                            else
                            {
                                Console.WriteLine($"Figura {figura.Id}: {status} | Pozicija: {figura.Pozicija} | Udaljenost do kucice: 0 | Figura je u kucici");
                            }
                        }
                        Console.WriteLine();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Greška pri deserijalizaciji izveštaja: " + ex.Message);
            }
        }
    }
}
