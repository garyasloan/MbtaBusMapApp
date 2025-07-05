using MbtaBusMapApp.Models;
using System.Text.Json;

namespace MbtaBusMapApp.Services
{
    public class MbtaApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public MbtaApiService(string apiKey)
        {
            _apiKey = apiKey;
            _httpClient = new HttpClient();
        }

        public async Task<List<Route>> GetBusRoutesAsync()
        {
            string url = $"https://api-v3.mbta.com/routes?filter[type]=3&api_key={_apiKey}";
            var response = await _httpClient.GetStringAsync(url);
            using var doc = JsonDocument.Parse(response);

            var routes = doc.RootElement.GetProperty("data").EnumerateArray()
                .Select(route => new Route
                {
                    Id = route.GetProperty("id").GetString() ?? string.Empty,
                    LongName = route.GetProperty("attributes").GetProperty("long_name").GetString() ?? string.Empty
                })
                .OrderBy(r =>
                {
                    var numericPart = new string(r.Id.TakeWhile(char.IsDigit).ToArray());
                    if (int.TryParse(numericPart, out int num))
                    {
                        return num;
                    }
                    else
                    {
                        return int.MaxValue;
                    }
                })
                .ToList();
            return routes;
        }


        public async Task<List<Vehicle>> GetVehiclesAsync(string routeId)
        {
            //string url = $"https://api-v3.mbta.com/vehicles?filter[route]={routeId}&api_key={_apiKey}";
            string url = $"https://api-v3.mbta.com/vehicles?filter[route]={routeId}&include=trip&api_key={_apiKey}";
            var response = await _httpClient.GetStringAsync(url);
            using var doc = JsonDocument.Parse(response);
            return doc.RootElement.GetProperty("data").EnumerateArray()
                .Select(vehicle => new Vehicle
                {
                    Id = vehicle.GetProperty("id").GetString() ?? string.Empty,
                    Latitude = vehicle.GetProperty("attributes").GetProperty("latitude").GetDouble(),
                    Longitude = vehicle.GetProperty("attributes").GetProperty("longitude").GetDouble()
                }).ToList();
        }

        public async Task<List<Vehicle>> GetVehiclesByRouteAsync(string routeId)
        {
            string url = $"https://api-v3.mbta.com/vehicles?filter[route]={routeId}&include=trip&api_key={_apiKey}";

            var json = await _httpClient.GetStringAsync(url);
            Console.WriteLine($"Raw JSON: {json}");

            var wrapper = JsonSerializer.Deserialize<MbtaVehiclesResponse>(json);

            var tripLookup = wrapper?.Included?.ToDictionary(t => t.Id, t => t) ?? new();

            var vehicles = new List<Vehicle>();

            foreach (var v in wrapper?.Data ?? new())
            {
                var lat = v.Attributes.Latitude;
                var lng = v.Attributes.Longitude;

                if (double.IsNaN(lat) || lat < -90 || lat > 90 ||
                    double.IsNaN(lng) || lng < -180 || lng > 180)
                {
                    Console.WriteLine($"Skipping invalid vehicle {v.Id} lat={lat} lng={lng}");
                    continue;
                }

                var vehicle = new Vehicle
                {
                    Id = v.Id,
                    Latitude = lat,
                    Longitude = lng,
                    Trip = new Trip()
                    {
                        // Always get direction_id directly from attributes!
                        DirectionId = v.Attributes.DirectionId
                    }
                };

                // Optionally join trip data for headsign
                if (v.Relationships?.Trip?.Data?.Id != null &&
                    tripLookup.TryGetValue(v.Relationships.Trip.Data.Id, out var tripData))
                {
                    vehicle.Trip.Id = tripData.Id;
                    vehicle.Trip.Name = tripData.Attributes.Name;
                    vehicle.Trip.Headsign = tripData.Attributes.Headsign;

                    // Some feeds might use trip's direction_id if it differs:
                    if (tripData.Attributes.DirectionId != vehicle.Trip.DirectionId)
                    {
                        Console.WriteLine($"Trip override: {vehicle.Id} uses trip direction {tripData.Attributes.DirectionId}");
                        vehicle.Trip.DirectionId = tripData.Attributes.DirectionId;
                    }
                }
                else
                {
                    vehicle.Trip.Headsign = "Unknown";
                }

                vehicle.DisplayName = $"# {vehicle.Id.Replace("y", "")}\nto {vehicle.Trip.Headsign}";
                vehicles.Add(vehicle);
            }

            Console.WriteLine($"Returning {vehicles.Count} vehicles");
            return vehicles;
        }
    }
}
