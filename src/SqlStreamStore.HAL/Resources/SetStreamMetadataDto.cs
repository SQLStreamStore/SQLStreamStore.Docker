namespace SqlStreamStore.HAL.Resources
{
    using Newtonsoft.Json.Linq;

    internal class SetStreamMetadataDto
    {
        public JToken MetadataJson { get; set; }
        public int? MaxCount { get; set; }
        public int? MaxAge { get; set; }
    }
}