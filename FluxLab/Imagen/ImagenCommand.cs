using Discord.Interactions;

namespace FluxLab.Imagen;

public class ImagenCommand : InteractionModuleBase
{
    private readonly HttpClient httpClient;
    private readonly ImagenClient client;

    public ImagenCommand(ImagenClient imagenClient)
    {
        httpClient = new HttpClient();
        client = imagenClient;
    }

    private async Task<string?> GetImagePromptBase64Async(string? imagePromptUrl)
    {
        if (string.IsNullOrWhiteSpace(imagePromptUrl))
            return null;

        try
        {
            var response = await httpClient.GetAsync(imagePromptUrl);
            response.EnsureSuccessStatusCode();
            var imageBytes = await response.Content.ReadAsByteArrayAsync();
            return Convert.ToBase64String(imageBytes);
        }
        catch (Exception ex)
        {
            await FollowupAsync($"Failed to process imagePrompt URL: {ex.Message}");
            return null;
        }
    }

    [SlashCommand("imagen", "Create an image using the Imagen API")]
    public async Task GenerateImagenImage(
        string prompt,
        string? imagePrompt = null,
        int numberOfImages = 4,
        ImageAspectRatio aspectRatio = ImageAspectRatio.OneToOne,
        ImagenSafetyFilterLevel safetyFilterLevel = ImagenSafetyFilterLevel.BlockOnlyHigh,
        ImagenPersonGeneration personGeneration = ImagenPersonGeneration.AllowAdult)
    {
        await DeferAsync();

        string? imagePromptBase64 = await GetImagePromptBase64Async(imagePrompt);

        var request = new ImagenRequest
        {
            Prompt = prompt,
            NumberOfImages = numberOfImages,
            AspectRatio = aspectRatio,
            SafetyFilterLevel = safetyFilterLevel,
            PersonGeneration = personGeneration,
            // TODO: Incorporate imagePromptBase64 if the Imagen API supports it.
        };

        try
        {
            var imageUrl = await client.GenerateImage(request);
            // TODO: Process the response from ImagenClient further.
            throw new NotImplementedException("Further processing of Imagen API response is not implemented.");
            // Example code if processing were complete:
            // await using var imageStream = await httpClient.GetStreamAsync(imageUrl);
            // await FollowupWithFileAsync(imageStream, $"{Truncate(prompt, 60)}.jpg");
        }
        catch (Exception e)
        {
            await FollowupAsync($"Error generating image: {e.Message}");
        }
    }

    private string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return "image";
        return value.Length <= maxLength ? value : value.Substring(0, maxLength);
    }
}