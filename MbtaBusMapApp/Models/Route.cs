namespace MbtaBusMapApp.Models
{
    public class Route
    {
        public string Id { get; set; } = string.Empty;
        public string LongName { get; set; } = string.Empty;
        public string DisplayName => $"{Id} - {LongName}";

    }

}
