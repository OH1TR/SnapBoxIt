using System.Drawing;
using System.Drawing.Printing;
using ZXing;
using ZXing.QrCode;
using ZXing.Windows.Compatibility;
using Azure.Messaging.ServiceBus;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace LabelPrintingAgent
{
    internal class Program
    {
        private static ServiceBusClient? _client;
        private static ServiceBusProcessor? _processor;
        private static string _connectionString = "";
        private static readonly string _queueName = "PrintJob";

        static async Task Main(string[] args)
        {
            Console.WriteLine("LabelPrintingAgent käynnistyy...");
            Console.WriteLine("Kuunnellaan PrintJob-jonoa Azure Service Busista...");

            // Lataa konfiguraatio
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: true)
                .Build();

            _connectionString = configuration["ServiceBus:ConnectionString"] ?? "";

            if (string.IsNullOrEmpty(_connectionString))
            {
                Console.WriteLine("Virhe: Service Bus connection string puuttuu appsettings.json tiedostosta!");
                Console.WriteLine("Tarkista että ServiceBus:ConnectionString on asetettu appsettings.json tiedostossa.");
                return;
            }

            try
            {
                await StartListeningAsync();
                
                Console.WriteLine("Paina Ctrl+C lopettaaksesi...");
                
                // Odota lopetussignaalia
                var cts = new CancellationTokenSource();
                Console.CancelKeyPress += (sender, e) =>
                {
                    e.Cancel = true;
                    cts.Cancel();
                };

                await Task.Delay(Timeout.Infinite, cts.Token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Lopetetaan...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Virhe: {ex.Message}");
            }
            finally
            {
                await StopListeningAsync();
            }
        }

        static async Task StartListeningAsync()
        {
            _client = new ServiceBusClient(_connectionString);
            _processor = _client.CreateProcessor(_queueName, new ServiceBusProcessorOptions());

            _processor.ProcessMessageAsync += ProcessMessageAsync;
            _processor.ProcessErrorAsync += ProcessErrorAsync;

            await _processor.StartProcessingAsync();
            Console.WriteLine($"Kuunnellaan jonoa: {_queueName}");
        }

        static async Task StopListeningAsync()
        {
            if (_processor != null)
            {
                await _processor.StopProcessingAsync();
                await _processor.DisposeAsync();
            }

            if (_client != null)
            {
                await _client.DisposeAsync();
            }
        }

        static async Task ProcessMessageAsync(ProcessMessageEventArgs args)
        {
            try
            {
                string messageBody = Encoding.UTF8.GetString(args.Message.Body);
                Console.WriteLine($"Vastaanotettu viesti: {messageBody}");

                // Jaa viesti pipe-merkillä
                var parts = messageBody.Split('|');
                if (parts.Length != 2)
                {
                    Console.WriteLine($"Virheellinen viestin muoto: {messageBody}");
                    await args.CompleteMessageAsync(args.Message);
                    return;
                }

                string printType = parts[0].Trim().ToLower();
                string printText = parts[1].Trim();

                Console.WriteLine($"Tulosteen tyyppi: {printType}");
                Console.WriteLine($"Tulostettava teksti: {printText}");

                switch (printType)
                {
                    case "qrlabel":
                        await PrintQrLabelAsync(printText);
                        break;
                    case "label":
                        await PrintTextLabelAsync(printText);
                        break;
                    default:
                        Console.WriteLine($"Tuntematon tulosteen tyyppi: {printType}");
                        break;
                }

                await args.CompleteMessageAsync(args.Message);
                Console.WriteLine("Viesti käsitelty onnistuneesti.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Virhe viestin käsittelyssä: {ex.Message}");
                await args.DeadLetterMessageAsync(args.Message);
            }
        }

        static Task ProcessErrorAsync(ProcessErrorEventArgs args)
        {
            Console.WriteLine($"Virhe Service Bus käsittelyssä: {args.Exception.Message}");
            return Task.CompletedTask;
        }

        static async Task PrintQrLabelAsync(string text)
        {
            Console.WriteLine($"Tulostetaan QR-label: {text}");
            
            try
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
                printDocument.PrintPage += (sender, e) => PrintQrLabelPage(sender, e, text, qrCodeBitmap);
                
                await ConfigurePrinterAsync(printDocument);
                printDocument.Print();
                
                Console.WriteLine("QR-label tulostettu onnistuneesti!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Virhe QR-label tulostuksessa: {ex.Message}");
                throw;
            }
        }

        static async Task PrintTextLabelAsync(string text)
        {
            Console.WriteLine($"Tulostetaan tekstilabel: {text}");
            
            try
            {
                // Tulostus ilman QR-koodia
                using var printDocument = new PrintDocument();
                printDocument.PrintPage += (sender, e) => PrintTextLabelPage(sender, e, text);
                
                await ConfigurePrinterAsync(printDocument);
                printDocument.Print();
                
                Console.WriteLine("Tekstilabel tulostettu onnistuneesti!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Virhe tekstilabel tulostuksessa: {ex.Message}");
                throw;
            }
        }

        static async Task ConfigurePrinterAsync(PrintDocument printDocument)
        {
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
                throw new InvalidOperationException("Tulostimia ei löytynyt!");
            }
        }

        static void PrintQrLabelPage(object sender, PrintPageEventArgs e, string text, Bitmap qrCode)
        {
            var graphics = e.Graphics;
            var font = new Font("Arial", 30);
            var brush = new SolidBrush(Color.Black);
            
            // Tulosta teksti
            graphics.DrawString($"{text}", font, brush, 1, 20);
            
            // Tulosta QR-koodi
            graphics.DrawImage(qrCode, 140, 1);
        }

        static void PrintTextLabelPage(object sender, PrintPageEventArgs e, string text)
        {
            var graphics = e.Graphics;
            var font = new Font("Arial", 36, FontStyle.Bold);
            var brush = new SolidBrush(Color.Black);
            
            // Tulosta vain teksti, isommalla fontilla ja keskitettynä
            var textSize = graphics.MeasureString(text, font);
            var x = (e.PageBounds.Width - textSize.Width) / 2;
            var y = (e.PageBounds.Height - textSize.Height) / 2;
            
            graphics.DrawString(text, font, brush, x, y);
        }
    }
}

