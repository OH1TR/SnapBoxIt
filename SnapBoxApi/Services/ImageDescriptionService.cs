using Azure;
using OpenAI;
using OpenAI.Chat;
using SnapBoxApi.Model;
using System.Text.Json;

namespace SnapBoxApi.Services
{
    public class ImageDescriptionService
    {
        private readonly OpenAI.Embeddings.EmbeddingClient _embeddingClient;
        private readonly ChatClient _chatClient;
        private readonly Uri _endpoint;
        private readonly string _apiKey;

        public ImageDescriptionService(IConfiguration config)
        {
            _endpoint = new Uri(config["OpenAI:Endpoint"]!);
            _apiKey = config["OpenAI:ApiKey"]!;
            var chatDeployment = config["OpenAI:ChatDeployment"]!;
            var embeddingDeployment = config["OpenAI:EmbeddingDeployment"]!;

            _chatClient = GetGPTClient().GetChatClient(chatDeployment);
            _embeddingClient = GetEmbeddingsClient().GetEmbeddingClient(embeddingDeployment);
        }

        Azure.AI.OpenAI.AzureOpenAIClient GetGPTClient()
        {
            var model = "gpt-4o";

            return new(
                _endpoint,
                new AzureKeyCredential(_apiKey));
        }

        Azure.AI.OpenAI.AzureOpenAIClient GetEmbeddingsClient()
        {
            var model = "text-embedding-ada-002";

            return new(
                _endpoint,
                new AzureKeyCredential(_apiKey));
        }


        public async Task<ItemDto> GetImageDescriptionAsync(MemoryStream file, string contentType = "image/jpeg")
        {
            // Reset position to beginning to ensure we read the entire stream
            file.Position = 0;
            
            // Validate that we have data
            if (file.Length == 0)
            {
                throw new ArgumentException("Image stream is empty", nameof(file));
            }
            
            // Copy to a new stream to avoid position issues
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            
            // Validate the copy was successful
            if (memoryStream.Length == 0)
            {
                throw new InvalidOperationException("Failed to copy image data to memory stream");
            }
            
            // Create BinaryData from the array (stream will be disposed after using block)
            BinaryData imageBinary = new BinaryData(memoryStream.ToArray());

            var textPart = ChatMessageContentPart.CreateTextPart("Describe this image in one sentence.");
            var imgPart = ChatMessageContentPart.CreateImagePart(imageBinary, contentType);

            var msg = new UserChatMessage(textPart, imgPart);

            ChatCompletion completion = _chatClient.CompleteChat(
                [
                    new SystemChatMessage(
                        @"Sinä olet avustaja, joka TARKASTELEE KÄYTTÄJÄN ANTAMAA KUVAA (yksi kuva kerrallaan) ja palauttaa AINOASTAAN validin JSON-olion yhdestä kuvassa näkyvästä PÄÄKOHTEESTA.

Säännöt:
1) Palauta aina täsmälleen tämä rakenne ja avainjärjestys:
   {
     ""Title"": <lyhyt yhdellä lauseella mitä kuvassa on>,
     ""Category"": <yksi luokka, esim. ""elektroniikan komponentti"" | ""tietokoneen osa"" | ""auton osa"" | ""työkalu"" | ""kodinkone"" | ""huonekalu"" | ""vaate"" | ""muu"">,
     ""DetailedDescription"": <täsmällinen, 1–3 lausetta. Jos kohde on elektroniikan komponentti ja tyyppikoodi näkyy, lisää tähän koodi ja verkkohausta varmistettu luokitus (esim. 'voltage regulator')>,
     ""Colors"": <taulukko 1–3 vallitsevasta väristä suomeksi, esim. [""musta""] tai [""musta"",""hopea""]>
   }

2) Valitse vain kuvan pääkohde. Jos kuvassa on monta esinettä, päättele mikä on visuaalisesti hallitsevin / kuvauksen kannalta olennaisin.

3) colors:
   - Palauta vain VALTAVÄRIT (1–3 kpl). Älä luettele kaikkia sävyjä.
   - Käytä suomenkielisiä perusvärien nimiä (esim. ""musta"", ""valkoinen"", ""harmaa"", ""hopea"", ""sininen"", ""punainen"", ""vihreä"", ""keltainen"", ""ruskea"").
   - Jos on vain yksi selkeä pääväri, palauta taulukko, jossa on yksi arvo.

4) Elektroniikan komponentit:
   - Jos komponentin tyyppikoodi (esim. ""LM7805"", ""NE555"", ""ATmega328P"") on LUETTAVISSA, lisää koodi ja verkkohausta löydetty komponenttiluokka tarkempaan kuvaukseen. Esimerkki: ""... Tyyppikoodi 'LM7805'; verkkohaku: 'voltage regulator' (5 V).""
   - Jos koodi ei ole luettavissa, älä arvaile koodia. Kuvaile komponenttia ulkoisten tuntomerkkien perusteella (kotelotyyppi, liitinmäärä, ym.).
   - Jos tyyppikoodi ei ole luettavissa, älä kerro sitä erikseen vaan jätä huomiotta.

5) Kieli: kirjoita suomeksi.

6) Älä palauta mitään muuta kuin JSON-olion. Ei selitystekstiä, ei lisäsanoja, ei koodilohkon merkkejä.

7) Epävarmuus:
   - Jos category on epäselvä, käytä ""muu"" ja selitä lyhyesti epävarmuus tarkemmassa kuvauksessa.
   - Älä lisää kenttiä, joita skeemassa ei ole.

8) Muotoilu ja validius:
   - Varmista, että JSON on rakenteellisesti validi (lainausmerkit, pilkut, hakasulkeet).
   - Älä käytä rivinvaihtoja tai erikoismerkkejä, jotka rikkoisivat JSONin.
   - Älä kirjoita kuvaukseen ""kuvassa näkyy"" tai ""kuva sisältää"", vaan kirjoita suoraan kuvaus.
   - Jos tunnistat kuvasta jotain yksilöivää, kuten tuotemerkin tai mallin, mainitse se myös title-kentässä.

9) Tietoturva:
   - Älä tee perusteettomia päätelmiä tuotemerkeistä tai malleista, jos logo tai malli ei ole selvästi luettavissa.

Tehtäväsi: analysoi kuva ohjeiden mukaan ja palauta vain yllä määritelty JSON-olio.
"),
                    msg
                ]);


            var descriptionJson = completion.Content[0].Text;

            var itemDto = JsonSerializer.Deserialize<ItemDto>(descriptionJson);

            itemDto.id = Guid.NewGuid().ToString();
            itemDto.PartitionKey = "item";

            await UpdateEmbeddingsAsync(itemDto);

            return itemDto;
        }

        public async Task UpdateEmbeddingsAsync(ItemDto itemDto)
        {
            var fullText = string.IsNullOrWhiteSpace(itemDto.UserDescription) 
                ? $"{itemDto.Title} {itemDto.DetailedDescription} {string.Join(" ", itemDto.Colors)}" 
                : $"{itemDto.Title} {itemDto.UserDescription} {string.Join(" ", itemDto.Colors)}";

            var result = await _embeddingClient.GenerateEmbeddingsAsync(new[] { 
                itemDto.Title, 
                itemDto.Category, 
                itemDto.DetailedDescription, 
                fullText, 
                itemDto.UserDescription ?? "" 
            });
            itemDto.TitleEmbedding = result.Value[0].ToFloats().ToArray();
            itemDto.CategoryEmbedding = result.Value[1].ToFloats().ToArray();
            itemDto.DetailedDescriptionEmbedding = result.Value[2].ToFloats().ToArray();
            itemDto.FullTextEmbedding = result.Value[3].ToFloats().ToArray();
            itemDto.UserDescriptionEmbedding= result.Value[4].ToFloats().ToArray();
        }

        public async Task<float[]> GetEmbeddingAsync(string input)
        {
            var r = await _embeddingClient.GenerateEmbeddingAsync(input);
            return r.Value.ToFloats().ToArray();
        }
    }
}