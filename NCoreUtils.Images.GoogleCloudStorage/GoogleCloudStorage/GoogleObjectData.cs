using System.Text.Json.Serialization;

namespace NCoreUtils.Images.GoogleCloudStorage
{
    public class GoogleObjectData
    {
        [JsonPropertyName("cacheControl")]
        public string? CacheControl { get; set; }

        [JsonPropertyName("contentType")]
        public string? ContentType { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}