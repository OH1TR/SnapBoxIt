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


        public async Task<ItemDto> GetImageDescriptionAsync(MemoryStream file)
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            BinaryData imageBinary = new BinaryData(memoryStream.ToArray());



            var textPart = ChatMessageContentPart.CreateTextPart("Describe this image in one sentence.");
            var imgPart = ChatMessageContentPart.CreateImagePart(imageBinary, "image/png");

            var msg = new UserChatMessage(textPart, imgPart);

            ChatCompletion completion = _chatClient.CompleteChat(
                [
                    new SystemChatMessage(
                        @"Sin� olet avustaja, joka TARKASTELEE K�YTT�J�N ANTAMAA KUVAA (yksi kuva kerrallaan) ja palauttaa AINOASTAAN validin JSON-olion yhdest� kuvassa n�kyv�st� P��KOHTEESTA.

S��nn�t:
1) Palauta aina t�sm�lleen t�m� rakenne ja avainj�rjestys:
   {
     ""Title"": <lyhyt yhdell� lauseella mit� kuvassa on>,
     ""Category"": <yksi luokka, esim. ""elektroniikan komponentti"" | ""tietokoneen osa"" | ""auton osa"" | ""ty�kalu"" | ""kodinkone"" | ""huonekalu"" | ""vaate"" | ""muu"">,
     ""DetailedDescription"": <t�sm�llinen, 1�3 lausetta. Jos kohde on elektroniikan komponentti ja tyyppikoodi n�kyy, lis�� t�h�n koodi ja verkkohausta varmistettu luokitus (esim. 'voltage regulator')>,
     ""Colors"": <taulukko 1�3 vallitsevasta v�rist� suomeksi, esim. [""musta""] tai [""musta"",""hopea""]>
   }

2) Valitse vain kuvan p��kohde. Jos kuvassa on monta esinett�, p��ttele mik� on visuaalisesti hallitsevin / kuvauksen kannalta olennaisin.

3) colors:
   - Palauta vain VALTAV�RIT (1�3 kpl). �l� luettele kaikkia s�vyj�.
   - K�yt� suomenkielisi� perusv�rien nimi� (esim. ""musta"", ""valkoinen"", ""harmaa"", ""hopea"", ""sininen"", ""punainen"", ""vihre�"", ""keltainen"", ""ruskea"").
   - Jos on vain yksi selke� p��v�ri, palauta taulukko, jossa on yksi arvo.

4) Elektroniikan komponentit:
   - Jos komponentin tyyppikoodi (esim. �LM7805�, �NE555�, �ATmega328P�) on LUETTAVISSA, lis�� koodi ja verkkohausta l�ydetty komponenttiluokka tarkempaan kuvaukseen. Esimerkki: �� Tyyppikoodi �LM7805�; verkkohaku: �voltage regulator� (5 V).�
   - Jos koodi ei ole luettavissa, �l� arvaile koodia. Kuvaile komponenttia ulkoisten tuntomerkkien perusteella (kotelotyyppi, liitinm��r�, ym.).
   - Jos tyyppikoodi ei ole luettavissa, �l� kerro sit� erikseen vaan j�t� huomiotta.

5) Kieli: kirjoita suomeksi.

6) �l� palauta mit��n muuta kuin JSON-olion. Ei selitysteksti�, ei lis�sanoja, ei koodilohkon merkkej�.

7) Ep�varmuus:
   - Jos category on ep�selv�, k�yt� ""muu"" ja selit� lyhyesti ep�varmuus tarkemmassa kuvauksessa.
   - �l� lis�� kentti�, joita skeemassa ei ole.

8) Muotoilu ja validius:
   - Varmista, ett� JSON on rakenteellisesti validi (lainausmerkit, pilkut, hakasulkeet).
   - �l� k�yt� rivinvaihtoja tai erikoismerkkej�, jotka rikkoisivat JSONin.
   - �l� kirjoita kuvaukseen 'kuvassa n�kyy' tai 'kuva sis�lt��', vaan kirjoita suoraan kuvaus.
   - Jos tunnistat kuvasta jotain yksil�iv��, kuten tuotemerkin tai mallin, mainitse se my�s title-kent�ss�.

9) Tietoturva:
   - �l� tee perusteettomia p��telmi� tuotemerkeist� tai malleista, jos logo tai malli ei ole selv�sti luettavissa.

Teht�v�si: analysoi kuva ohjeiden mukaan ja palauta vain yll� m��ritelty JSON-olio.
"),
                    msg
                ]);


            var descriptionJson = completion.Content[0].Text;

            var itemDto = JsonSerializer.Deserialize<ItemDto>(descriptionJson);

            var fullText = $"{itemDto.Title} {itemDto.Category} {itemDto.DetailedDescription} {string.Join(" ", itemDto.Colors)}";

            var result = await _embeddingClient.GenerateEmbeddingsAsync(new[] { itemDto.Title, itemDto.Category, itemDto.DetailedDescription, fullText });
            itemDto.TitleEmbedding = result.Value[0].ToFloats().ToArray();
            itemDto.CategoryEmbedding = result.Value[1].ToFloats().ToArray();
            itemDto.DetailedDescriptionEmbedding = result.Value[2].ToFloats().ToArray();
            itemDto.FullTextEmbedding = result.Value[3].ToFloats().ToArray();

            itemDto.id = Guid.NewGuid().ToString();
            itemDto.PartitionKey = "item";

            return itemDto;
        }


        public async Task<float[]> GetEmbeddingAsync(string input)
        {
            var r = await _embeddingClient.GenerateEmbeddingAsync(input);
            return r.Value.ToFloats().ToArray();
        }
    }
}