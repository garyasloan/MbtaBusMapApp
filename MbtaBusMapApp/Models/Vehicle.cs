using System.Text.Json.Serialization;

namespace MbtaBusMapApp.Models
{
    public class MbtaVehiclesResponse
    {
        [JsonPropertyName("data")]
        public List<VehicleData> Data { get; set; } = new();

        [JsonPropertyName("included")]
        public List<TripData> Included { get; set; } = new();
    }

    public class VehicleData
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("attributes")]
        public VehicleAttributes Attributes { get; set; } = new();

        [JsonPropertyName("relationships")]
        public VehicleRelationships Relationships { get; set; } = new();
    }

    public class VehicleAttributes
    {
        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [JsonPropertyName("direction_id")]
        public int DirectionId { get; set; }
    }

    public class VehicleRelationships
    {
        [JsonPropertyName("trip")]
        public TripRelationship Trip { get; set; } = new();
    }

    public class TripRelationship
    {
        [JsonPropertyName("data")]
        public TripReference Data { get; set; } = new();
    }

    public class TripReference
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";
    }

    public class TripData
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = "";

        [JsonPropertyName("attributes")]
        public TripAttributes Attributes { get; set; } = new();
    }

    public class TripAttributes
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = "";

        [JsonPropertyName("headsign")]
        public string Headsign { get; set; } = "";

        [JsonPropertyName("direction_id")]
        public int DirectionId { get; set; }
    }

    // Your final working domain model
    public class Vehicle
    {
        public string Id { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public Trip Trip { get; set; } = new();
        public string DisplayName { get; set; } = "";
    }

    public class Trip
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Headsign { get; set; } = "";
        public int DirectionId { get; set; }

        public string Direction => DirectionId switch
        {
            0 => "Outbound",
            1 => "Inbound",
            _ => "Unknown"
        };
    }

}
