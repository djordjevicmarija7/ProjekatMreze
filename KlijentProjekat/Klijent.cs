using Biblioteka; 
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;

namespace KlijentProjekat
{
    public class Klijent
    {
        private const int Port = 5000;
        private string ServerIp;
        private Socket klijentSocket;
       
        private string ime = "";

        public Klijent(string serverIp, string ime = "", int port = Port)
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
                Console.WriteLine("Povezan sa serverom. Čekate ažuriranje igre...");

                klijentSocket.Blocking = false;

                while (true)
                {
                    byte[] updateData = PrimiPodatke();
                    if (updateData != null && updateData.Length > 0)
                    {
                        ProcessReceivedData(updateData);
                    }
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

      
        private void ProcessReceivedData(byte[] updateData)
        {
            string headerReport = "REPORT:";
            string headerText = "TEXT:";

            string dataString = Encoding.UTF8.GetString(updateData);

           
            if (dataString.StartsWith(headerReport))
            {
                byte[] reportData = new byte[updateData.Length - headerReport.Length];
                Array.Copy(updateData, headerReport.Length, reportData, 0, reportData.Length);
                try
                {
                    using (MemoryStream ms = new MemoryStream(reportData))
                    {
                        BinaryFormatter bf = new BinaryFormatter();
                        var listaIgraca = (List<Korisnik>)bf.Deserialize(ms);
                        Console.WriteLine("\n--- Ažuriranje igre (Izveštaj) ---");
                        foreach (var igrac in listaIgraca)
                        {
                            Console.WriteLine($"Igrač: {igrac.Ime}");
                            Console.WriteLine($"Početna pozicija: {igrac.StratPozicija}, Ciljna pozicija: {igrac.CiljPozicija}");
                            foreach (var figura in igrac.Figure)
                            {
                                string status = figura.Aktivna ? "Aktivna" : "Neaktivna";
                                Console.WriteLine($"  Figura {figura.Id}: {status}, Pozicija: {figura.Pozicija}, Udaljenost do cilja: {figura.UdaljenostDoCilja}");
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
           
            else if (dataString.StartsWith(headerText))
            {
                string poruka = dataString.Substring(headerText.Length);
                Console.WriteLine("\n--- Ažuriranje igre ---");
                Console.WriteLine(poruka);

                if (poruka.StartsWith("Boja uspseno odabrana:"))
                {
                    string odabranaBoja = poruka.Substring("Boja uspseno odabrana:".Length).Trim();
                    ime = odabranaBoja;
                    Console.WriteLine("Vaša boja je dodeljena: " + ime);
                    return;
                }

                if (poruka.Contains("Trenutni igrač:") && poruka.Contains(ime))
                {
                    Console.WriteLine("\n*** Vaš je potez! ***");
                    ProcessirajPotez();
                }
            }
            else
            {
                
                Console.WriteLine("\n--- Ažuriranje igre ---");
                Console.WriteLine(dataString);
                if (dataString.Contains("Trenutni igrač:") && dataString.Contains(ime))
                {
                    Console.WriteLine("\n*** Vaš je potez! ***");
                    ProcessirajPotez();
                }
            }
        }

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

                string poruka = $"{akcija}\n{idFigure}\n{diceResult}";
                PosaljiPoruku(poruka);
                string odgovor = PrimiPoruku();
                Console.WriteLine("Odgovor servera: " + odgovor);

                if (!odgovor.ToLower().Contains("dodatni potez"))
                {
                    turnOngoing = false;
                }
                else
                {
                    Console.WriteLine("Imate dodatni potez. Unesite rezultat bacanja kockice za dodatni potez.");
                    Thread.Sleep(500);
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

        private byte[] PrimiPodatke()
        {
            byte[] buffer = new byte[4096];
            try
            {
                if (klijentSocket.Poll(1500000, SelectMode.SelectRead))
                {
                    int primljeno = klijentSocket.Receive(buffer);
                    if (primljeno > 0)
                    {
                        byte[] data = new byte[primljeno];
                        Array.Copy(buffer, data, primljeno);
                        return data;
                    }
                }
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode != SocketError.WouldBlock)
                {
                    Console.WriteLine($"Greška pri prijemu podataka: {ex.Message}");
                }
            }
            return null;
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
                            string poruka = Encoding.UTF8.GetString(buffer, 0, primljeno);
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

            
            Klijent klijent = new Klijent(serverIp);
            klijent.Pokreni();
        }
    }
}
