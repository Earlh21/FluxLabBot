namespace FluxLab;

using System;
using System.Threading.Tasks;
using RestSharp;
using Newtonsoft.Json;

public class FluxProRequest
{
    [JsonProperty("prompt")] public string? Prompt { get; set; }

    [JsonProperty("image_prompt")] public string? ImagePrompt { get; set; }

    [JsonProperty("width")] public int Width { get; set; } = 1024;

    [JsonProperty("height")] public int Height { get; set; } = 768;

    [JsonProperty("prompt_upsampling")] public bool PromptUpsampling { get; set; }

    [JsonProperty("seed")] public int? Seed { get; set; }

    [JsonProperty("safety_tolerance")] public int SafetyTolerance { get; set; } = 6;

    [JsonProperty("output_format")] public string OutputFormat { get; set; } = "jpeg";
}

public class FluxUltraRequest
{
    [JsonProperty("prompt")] public string? Prompt { get; set; }

    [JsonProperty("image_prompt")] public string? ImagePrompt { get; set; }

    [JsonProperty("seed")] public int? Seed { get; set; }
    
    [JsonProperty("aspect_ratio")] public string AspectRatio { get; set; } = "16:9";

    [JsonProperty("safety_tolerance")] public int SafetyTolerance { get; set; } = 6;

    [JsonProperty("output_format")] public string OutputFormat { get; set; } = "jpeg";
    
    [JsonProperty("raw")] public bool Raw { get; set; }
    
    [JsonProperty("image_prompt_strength")] public float ImagePromptStrength { get; set; } = 0.1f;
}

public class FluxProResponse
{
    [JsonProperty("id")] public string Id { get; set; }
}

public class GetResultResponse
{
    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("status")]
    public string Status { get; set; }

    [JsonProperty("result")]
    public GetResultResult? Result { get; set; }
}

public class GetResultResult
{
    [JsonProperty("sample")]
    public string Sample { get; set; }
}

public class FluxClient
{
    private readonly RestClient client;
    private readonly string token;

    public FluxClient(string token, string baseUrl = "https://api.bfl.ml/v1/")
    {
        this.token = token;
        client = new RestClient(baseUrl);
    }

    public async Task<string> GenerateImage(
        FluxUltraRequest request)
    {
        var submitResponse = await GenerateUltraAsync(request);
        var taskId = submitResponse.Id;

        if (string.IsNullOrEmpty(taskId))
        {
            throw new Exception("No Task ID returned from flux endpoint.");
        }

        return await WaitForImageUrlAsync(taskId);
    }
    
    public async Task<string> GenerateImage(
        FluxProRequest request)
    {
        var submitResponse = await GenerateProAsync(request);
        var taskId = submitResponse.Id;

        if (string.IsNullOrEmpty(taskId))
        {
            throw new Exception("No Task ID returned from flux endpoint.");
        }

        return await WaitForImageUrlAsync(taskId);
    }

    private async Task<string> WaitForImageUrlAsync(string taskId,
        int pollIntervalSeconds = 10,
        int maxPollAttempts = 60)
    {
        for (int i = 0; i < maxPollAttempts; i++)
        {
            var resultStatus = await GetResultAsync(taskId);

            if (resultStatus.Status.Equals("Ready", StringComparison.OrdinalIgnoreCase))
            {
                var sample = resultStatus.Result?.Sample;
                if (string.IsNullOrEmpty(sample))
                {
                    throw new Exception("Task is ready, but 'sample' was not found in the result object.");
                }

                return sample;
            }
            else if (resultStatus.Status.Equals("Error", StringComparison.OrdinalIgnoreCase) ||
                     resultStatus.Status.Equals("Task not found", StringComparison.OrdinalIgnoreCase))
            {
                throw new Exception($"Task failed or was not found. Status: {resultStatus.Status}");
            }

            await Task.Delay(pollIntervalSeconds * 1000);
        }

        throw new TimeoutException($"Task was not ready after {maxPollAttempts} polling attempts.");
    }
    
    private async Task<FluxProResponse> GenerateUltraAsync(FluxUltraRequest request)
    {
        return await GenerateImageAsync("flux-pro-1.1-ultra", request);
    }
    
    private async Task<FluxProResponse> GenerateProAsync(FluxProRequest request)
    {
        return await GenerateImageAsync("flux-pro-1.1", request);
    }

    private async Task<FluxProResponse> GenerateImageAsync(string model, object body)
    {
        var restRequest = new RestRequest(model, Method.Post);

        restRequest.AddHeader("Content-Type", "application/json");
        restRequest.AddHeader("X-Key", token);

        restRequest.AddJsonBody(body);

        var response = await client.ExecuteAsync<FluxProResponse>(restRequest);

        if (!response.IsSuccessful)
        {
            throw new Exception(
                $"Generation request failed. Status: {response.StatusCode}. Content: {response.Content}");
        }

        return response.Data;
    }

    private async Task<GetResultResponse> GetResultAsync(string taskId)
    {
        var restRequest = new RestRequest("get_result", Method.Get);

        restRequest.AddHeader("X-Key", token);
        restRequest.AddQueryParameter("id", taskId);

        var response = await client.ExecuteAsync<GetResultResponse>(restRequest);

        if (!response.IsSuccessful)
        {
            throw new Exception($"GetResult call failed. Status: {response.StatusCode}. Content: {response.Content}");
        }

        return response.Data;
    }
}