using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZO.LoadOrderManager
{
    public class NexusModItem
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("game")]
        public string Game { get; set; }

        [JsonPropertyName("modId")]
        public int ModId { get; set; }

        [JsonPropertyName("fileId")]
        public int FileId { get; set; }

        [JsonPropertyName("source")]
        public string Source { get; set; }

        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; }

        [JsonPropertyName("vortexId")]
        public string VortexId { get; set; }
    

    public static List<NexusModItem> LoadModList(string filePath)
        {
            var jsonString = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<List<NexusModItem>>(jsonString);
        }
    }


}