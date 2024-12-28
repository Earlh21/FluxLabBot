namespace FluxLab;

using System;
using System.Threading.Tasks;
using RestSharp;
using Newtonsoft.Json;

/// <summary>
/// Represents the request body for calling FLUX 1.1 [pro].
/// </summary>
public class FluxProRequest
{
    [JsonProperty("prompt")] public string? Prompt { get; set; }

    [JsonProperty("image_prompt")] public string? ImagePrompt { get; set; }

    [JsonProperty("width")] public int Width { get; set; } = 1024;

    [JsonProperty("height")] public int Height { get; set; } = 768;

    [JsonProperty("prompt_upsampling")] public bool PromptUpsampling { get; set; }

    [JsonProperty("seed")] public int? Seed { get; set; }

    [JsonProperty("safety_tolerance")] public int SafetyTolerance { get; set; } = 2;

    [JsonProperty("output_format")] public string OutputFormat { get; set; } = "jpeg";
}

/// <summary>
/// The response from the FLUX 1.1 [pro] "create task" call.
/// </summary>
public class FluxProResponse
{
    [JsonProperty("id")] public string Id { get; set; }
}

/// <summary>
/// The response from the "get_result" endpoint.
/// </summary>
public class GetResultResponse
{
    /// <summary>
    /// The task ID associated with this result.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; }

    /// <summary>
    /// The status of the task. Possible values:
    /// "Task not found", "Pending", "Request Moderated", 
    /// "Content Moderated", "Ready", "Error".
    /// </summary>
    [JsonProperty("status")]
    public string Status { get; set; }

    /// <summary>
    /// Result object (JSON) which, once Ready, should contain a field 
    /// named "sample" with the image URL.
    /// </summary>
    [JsonProperty("result")]
    public GetResultResult? Result { get; set; }
}

public class GetResultResult
{
    [JsonProperty("sample")]
    public string Sample { get; set; }
}

/// <summary>
/// RestSharp-based client for the FLUX 1.1 [pro] endpoint.
/// </summary>
public class FluxClient
{
    private readonly RestClient client;
    private readonly string token;

    /// <summary>
    /// Creates a new client for FLUX 1.1 [pro].
    /// </summary>
    /// <param name="token">API token to include in requests.</param>
    /// <param name="baseUrl">Optional base URL if different from the default.</param>
    public FluxClient(string token, string baseUrl = "https://api.bfl.ml/v1/")
    {
        this.token = token;
        client = new RestClient(baseUrl);
    }

    /// <summary>
    /// 1) Submit an image generation request (flux-pro-1.1)
    /// 2) Poll the get_result endpoint until the status is Ready
    /// 3) When Ready, return the 'sample' URL from the response
    /// </summary>
    /// <param name="request">Request with prompt, size, etc.</param>
    /// <param name="pollIntervalSeconds">
    /// How many seconds to wait between polling attempts.
    /// </param>
    /// <param name="maxPollAttempts">
    /// Maximum number of times to poll before giving up.
    /// </param>
    /// <returns>The URL (sample) once the image is ready.</returns>
    public async Task<string> GenerateImageAndWaitForUrlAsync(
        FluxProRequest request,
        bool ultra = false,
        int pollIntervalSeconds = 10,
        int maxPollAttempts = 60)
    {
        // 1) Submit a new task to generate the image
        var submitResponse = await GenerateImageAsync(request);
        var taskId = submitResponse.Id;

        if (string.IsNullOrEmpty(taskId))
        {
            throw new Exception("No Task ID returned from flux-pro-1.1 endpoint.");
        }

        // 2) Poll the get_result endpoint
        for (int i = 0; i < maxPollAttempts; i++)
        {
            var resultStatus = await GetResultAsync(taskId);

            if (resultStatus.Status.Equals("Ready", StringComparison.OrdinalIgnoreCase))
            {
                // 3) Return the 'sample' from result
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

            // Wait a bit before polling again
            await Task.Delay(pollIntervalSeconds * 1000);
        }

        throw new TimeoutException($"Task was not ready after {maxPollAttempts} polling attempts.");
    }

    /// <summary>
    /// Submits an image generation task using FLUX 1.1 [pro].
    /// </summary>
    /// <param name="request">Request body with prompt and parameters.</param>
    /// <returns>A <see cref="FluxProResponse"/> representing the created task (ID).</returns>
    private async Task<FluxProResponse> GenerateImageAsync(FluxProRequest request, bool ultra = false)
    {
        var model = ultra ? "flux-pro-1.1-ultra" : "flux-pro-1.1";
        
        var restRequest = new RestRequest("flux-pro-1.1", Method.Post);

        restRequest.AddHeader("Content-Type", "application/json");
        restRequest.AddHeader("X-Key", token);

        restRequest.AddJsonBody(request);

        var response = await client.ExecuteAsync<FluxProResponse>(restRequest);

        if (!response.IsSuccessful)
        {
            throw new Exception(
                $"Generation request failed. Status: {response.StatusCode}. Content: {response.Content}");
        }

        return response.Data;
    }

    /// <summary>
    /// Calls the get_result endpoint to check the status (and any result data).
    /// </summary>
    /// <param name="taskId">ID of the task to retrieve.</param>
    /// <returns>The status and result object.</returns>
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