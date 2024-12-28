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
    
    [SlashCommand("flux", "Create an image using FLUX 1.1")]
    public async Task GenerateImage(
        string prompt,
        string? imagePrompt = null,
        int width = 1024,
        int height = 768,
        bool promptImprovement = true,
        int? seed = null,
        int safetyTolerance = 2,
        bool ultra = false)
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

        var imageUrl = await client.GenerateImageAndWaitForUrlAsync(request, ultra);
        
        using var httpClient = new HttpClient();
        await using var imageStream = await httpClient.GetStreamAsync(imageUrl);

        await FollowupWithFileAsync(imageStream, $"{prompt.Take(60).ToArray()}.jpg");
    }
    
    
}