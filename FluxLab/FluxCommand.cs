using Discord.Interactions;
using RestSharp;

namespace FluxLab;

public class FluxCommand : InteractionModuleBase
{
    private FluxClient client;
    
    public FluxCommand(FluxClient client)
    {
        this.client = client;
    }
    
    [SlashCommand("flux", "Create an image using FLUX 1.1 Pro")]
    public async Task GenerateImage(
        string prompt,
        string? imagePrompt = null,
        int width = 1024,
        int height = 768,
        bool promptImprovement = true,
        int? seed = null,
        int safetyTolerance = 6)
    {
        var request = new FluxProRequest
        {
            Prompt = prompt,
            ImagePrompt = imagePrompt,
            Width = width,
            Height = height,
            PromptUpsampling = promptImprovement,
            Seed = seed,
            SafetyTolerance = safetyTolerance,
            OutputFormat = "jpeg"
        };

        await DeferAsync();

        try
        {
            var imageUrl = await client.GenerateImage(request);
            
            using var httpClient = new HttpClient();
            await using var imageStream = await httpClient.GetStreamAsync(imageUrl);

            await FollowupWithFileAsync(imageStream, $"{prompt.Take(60).ToArray()}.jpg");
        }
        catch (Exception e)
        {
            await FollowupAsync(e.Message);
        }
    }
    
    [SlashCommand("fluxultra", "Create an image using FLUX 1.1 Pro Ultra")]
    public async Task GenerateImage(
        string prompt,
        string? imagePrompt = null,
        string aspectRatio = "1:1",
        int? seed = null,
        int safetyTolerance = 6,
        bool raw = false,
        float imagePromptStrength = 0.1f)
    {
        var request = new FluxUltraRequest
        {
            Prompt = prompt,
            ImagePrompt = imagePrompt,
            Seed = seed,
            SafetyTolerance = safetyTolerance,
            OutputFormat = "jpeg",
            Raw = raw,
            AspectRatio = aspectRatio,
            ImagePromptStrength = imagePromptStrength
        };

        await DeferAsync();

        try
        {
            var imageUrl = await client.GenerateImage(request);
            
            using var httpClient = new HttpClient();
            await using var imageStream = await httpClient.GetStreamAsync(imageUrl);

            await FollowupWithFileAsync(imageStream, $"{prompt.Take(60).ToArray()}.jpg");
        }
        catch (Exception e)
        {
            await FollowupAsync(e.Message);
        }
    }
}