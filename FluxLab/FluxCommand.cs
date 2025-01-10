using System.Net;
using Discord.Interactions;
using RestSharp;

namespace FluxLab;

public class FluxCommand : InteractionModuleBase
{
    private readonly HttpClient httpClient;
    private readonly FluxClient client;

    public FluxCommand(FluxClient fluxClient)
    {
        httpClient = new HttpClient();
        client = fluxClient;
    }

    // Helper method to download and convert image to Base64
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
            // Optionally log the exception
            await FollowupAsync($"Failed to process imagePrompt URL: {ex.Message}");
            return null;
        }
    }

    [SlashCommand("flux", "Create an image using FLUX 1.1 Pro")]
    public async Task GenerateFluxImage(
        string prompt,
        string? imagePrompt = null,
        int width = 1024,
        int height = 768,
        bool promptImprovement = true,
        int? seed = null,
        int safetyTolerance = 6)
    {
        await DeferAsync();

        string? imagePromptBase64 = await GetImagePromptBase64Async(imagePrompt);

        var request = new FluxProRequest
        {
            Prompt = prompt,
            ImagePrompt = imagePromptBase64,
            Width = width,
            Height = height,
            PromptUpsampling = promptImprovement,
            Seed = seed,
            SafetyTolerance = safetyTolerance,
            OutputFormat = "jpeg"
        };

        try
        {
            var imageUrl = await client.GenerateImage(request);

            await using var imageStream = await httpClient.GetStreamAsync(imageUrl);
            await FollowupWithFileAsync(imageStream, $"{Truncate(prompt, 60)}.jpg");
        }
        catch (Exception e)
        {
            await FollowupAsync($"Error generating image: {e.Message}");
        }
    }

    [SlashCommand("fluxultra", "Create an image using FLUX 1.1 Pro Ultra")]
    public async Task GenerateFluxUltraImage(
        string prompt,
        string? imagePrompt = null,
        string aspectRatio = "1:1",
        int? seed = null,
        int safetyTolerance = 6,
        bool raw = false,
        float imagePromptStrength = 0.1f)
    {
        await DeferAsync();

        string? imagePromptBase64 = await GetImagePromptBase64Async(imagePrompt);

        var request = new FluxUltraRequest
        {
            Prompt = prompt,
            ImagePrompt = imagePromptBase64,
            Seed = seed,
            SafetyTolerance = safetyTolerance,
            OutputFormat = "jpeg",
            Raw = raw,
            AspectRatio = aspectRatio,
            ImagePromptStrength = imagePromptStrength
        };

        try
        {
            var imageUrl = await client.GenerateImage(request);

            await using var imageStream = await httpClient.GetStreamAsync(imageUrl);
            await FollowupWithFileAsync(imageStream, $"{Truncate(prompt, 60)}.jpg");
        }
        catch (Exception e)
        {
            await FollowupAsync($"Error generating image: {e.Message}");
        }
    }

    // Utility method to truncate the filename if necessary
    private string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return "image";
        return value.Length <= maxLength ? value : value.Substring(0, maxLength);
    }
}