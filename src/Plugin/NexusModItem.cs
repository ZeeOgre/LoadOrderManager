using System.IO;
using System.Text.Json;

namespace ZO.LoadOrderManager
{
    public class NexusModItem
    {
        public string Game { get; set; }
        public int ModId { get; set; }
        public int FileId { get; set; }
        public string Source { get; set; }
        public bool Enabled { get; set; }
        public string VortexId { get; set; }
        public string Name { get; set; }

        public static List<NexusModItem> LoadModList(string filePath)
        {
            var jsonString = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<List<NexusModItem>>(jsonString);
        }
    }


}