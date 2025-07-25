using OpenAI;
using OpenAI.Chat;

namespace SnapBoxApi.Services
{
    public class ImageDescriptionService
    {
        private readonly OpenAIClient _openAIClient;

        public ImageDescriptionService(OpenAIClient openAIClient)
        {
            _openAIClient = openAIClient;
        }

        public async Task<string> GetImageDescriptionAsync(IFormFile file)
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            BinaryData imageBinary = new BinaryData(memoryStream.ToArray());
            var messages = new ChatMessage[]
            {
                new UserChatMessage(ChatMessageContentPart.CreateTextPart("What does this image represent?")),
                new UserChatMessage(ChatMessageContentPart.CreateImagePart(imageBinary,"image/png"))
            };

            var chatRequest = new ChatCompletionOptions();
            var client = _openAIClient.GetChatClient("");
            var response = await client.CompleteChatAsync(messages, chatRequest);
            var description = response.Value.Content;
            return description.ToString();
        }
    }
}