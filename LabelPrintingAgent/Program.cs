using System.Drawing;
using System.Drawing.Printing;
using ZXing;
using ZXing.QrCode;
using ZXing.Windows.Compatibility;
using System.Linq;

namespace LabelPrintingAgent
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Käyttö: LabelPrintingAgent <teksti>");
                Console.WriteLine("Esimerkki: LabelPrintingAgent \"Tämä on testi\"");
                return;
            }

            string textToPrint = args[0];
            Console.WriteLine($"Tulostetaan: {textToPrint}");

            try
            {
                PrintLabel(textToPrint);
                Console.WriteLine("Tulostus onnistui!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Virhe tulostuksessa: {ex.Message}");
            }
        }

        static void PrintLabel(string text)
        {
            // Luo QR-koodi
            var qrCodeWriter = new BarcodeWriter
            {
                Format = BarcodeFormat.QR_CODE,
                Options = new QrCodeEncodingOptions
                {
                    Height = 90,
                    Width = 90,
                    Margin = 0
                }
            };

            using var qrCodeBitmap = qrCodeWriter.Write(text);
            
            // Tulostus
            using var printDocument = new PrintDocument();
            printDocument.PrintPage += (sender, e) => PrintPage(sender, e, text, qrCodeBitmap);
            
            // Yritä löytää oletustulostin
            if (PrinterSettings.InstalledPrinters.Count > 0)
            {
                for (int i = 0; i < PrinterSettings.InstalledPrinters.Count; i++)
                {
                    Console.WriteLine($"Löydetty tulostin: {PrinterSettings.InstalledPrinters[i]}");
                    if (PrinterSettings.InstalledPrinters[i].ToLower().Contains("brother"))
                    {
                        printDocument.PrinterSettings.PrinterName = PrinterSettings.InstalledPrinters[i];
                        break;
                    }
                }        

                Console.WriteLine($"Tulostetaan tulostimella: {printDocument.PrinterSettings.PrinterName}");
            }
            else
            {
                Console.WriteLine("Tulostimia ei löytynyt!");
                return;
            }

            printDocument.Print();
        }

        static void PrintPage(object sender, PrintPageEventArgs e, string text, Bitmap qrCode)
        {
            var graphics = e.Graphics;
            var font = new Font("Arial", 30);
            var brush = new SolidBrush(Color.Black);
            
            // Tulosta teksti
            graphics.DrawString($"{text}", font, brush, 1, 20);
            
            // Tulosta QR-koodi
            graphics.DrawImage(qrCode, 140, 1);
            
        }
    }
}
