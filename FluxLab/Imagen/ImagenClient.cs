using System;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using RestSharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace FluxLab.Imagen;

[JsonConverter(typeof(StringEnumConverter))]
public enum ImageAspectRatio
{
    [EnumMember(Value = "1:1")] OneToOne,
    [EnumMember(Value = "3:4")] ThreeToFour,
    [EnumMember(Value = "4:3")] FourToThree,
    [EnumMember(Value = "9:16")] NineToSixteen,
    [EnumMember(Value = "16:9")] SixteenToNine
}

[JsonConverter(typeof(StringEnumConverter))]
public enum ImagenSafetyFilterLevel
{
    [EnumMember(Value = "BLOCK_LOW_AND_ABOVE")]
    BlockLowAndAbove,

    [EnumMember(Value = "BLOCK_MEDIUM_AND_ABOVE")]
    BlockMediumAndAbove,

    [EnumMember(Value = "BLOCK_ONLY_HIGH")]
    BlockOnlyHigh
}

[JsonConverter(typeof(StringEnumConverter))]
public enum ImagenPersonGeneration
{
    [EnumMember(Value = "DONT_ALLOW")] DontAllow,
    [EnumMember(Value = "ALLOW_ADULT")] AllowAdult
}

public class ImagenRequest
{
    [JsonProperty("prompt")] public string Prompt { get; set; }

    [JsonProperty("number_of_images")] public int NumberOfImages { get; set; } = 4;

    [JsonProperty("aspect_ratio")] public ImageAspectRatio AspectRatio { get; set; } = ImageAspectRatio.OneToOne;

    [JsonProperty("safety_filter_level")]
    public ImagenSafetyFilterLevel SafetyFilterLevel { get; set; } = ImagenSafetyFilterLevel.BlockOnlyHigh;

    [JsonProperty("person_generation")]
    public ImagenPersonGeneration PersonGeneration { get; set; } = ImagenPersonGeneration.AllowAdult;
}

public class ImagenResponse
{
    // TODO: Define response properties based on the Imagen API response schema.
}

public class ImagenClient
{
    private readonly RestClient client;
    private readonly string apiKey;

    public ImagenClient(string apiKey, string baseUrl = "https://api.imagen.com/v1/")
    {
        this.apiKey = apiKey;
        client = new RestClient(baseUrl);
    }

    public async Task<ImagenResponse> GenerateImage(ImagenRequest request)
    {
        var restRequest = new RestRequest("generate_images", Method.Post);
        restRequest.AddHeader("Content-Type", "application/json");
        restRequest.AddHeader("X-Api-Key", apiKey);
        restRequest.AddJsonBody(request);

        var response = await client.ExecuteAsync<ImagenResponse>(restRequest);

        if (!response.IsSuccessful)
        {
            throw new Exception(
                $"Generation request failed. Status: {response.StatusCode}. Content: {response.Content}");
        }

        // TODO: Process the response and implement further handling as needed.
        throw new NotImplementedException("Further processing of Imagen API response is not implemented.");
    }
}