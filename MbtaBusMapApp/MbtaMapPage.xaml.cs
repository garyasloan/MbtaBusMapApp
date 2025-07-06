using MbtaBusMapApp.Helpers;
using MbtaBusMapApp.Models;
using MbtaBusMapApp.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

namespace MbtaBusMapApp;

public partial class MbtaMapPage : ContentPage
{
    private readonly MbtaApiService _mbtaApi;
    private string _selectedRouteNumber = null!;

    public MbtaMapPage()
    {
        InitializeComponent();

        string apiKey = ConfigHelper.GetApiKey();
        _mbtaApi = new MbtaApiService(apiKey);

        BusMap.MoveToRegion(MapSpan.FromCenterAndRadius(
            new Location(42.3199, -71.0589),
            Distance.FromMiles(10)
        ));

        LoadRoutes();
    }

    private async void LoadRoutes()
    {
        var routes = await _mbtaApi.GetBusRoutesAsync();

        RoutePicker.ItemsSource = routes
            .Select(r =>
            {
                if (r.DisplayName.StartsWith("708"))
                    return r.DisplayName.Replace("708", "708 - (CT3)");
                if (r.DisplayName.StartsWith("747"))
                    return r.DisplayName.Replace("747", "747 - (CT2)");
                if (r.DisplayName.StartsWith("746"))
                    return r.DisplayName.Replace("746", "746 - (SLW)");
                if (r.DisplayName.StartsWith("749"))
                    return r.DisplayName.Replace("749", "749 - (SL5)");
                if (r.DisplayName.StartsWith("751"))
                    return r.DisplayName.Replace("751", "751 - (SL4)");
                if (r.DisplayName.StartsWith("743"))
                    return r.DisplayName.Replace("743", "743 - (SL3)");
                if (r.DisplayName.StartsWith("742"))
                    return r.DisplayName.Replace("742", "742 - (SL2)");
                if (r.DisplayName.StartsWith("741"))
                    return r.DisplayName.Replace("741", "741 - (SL1)");

                return r.DisplayName;
            })
            .ToList();
    }


    private async void OnRouteSelected(object sender, EventArgs e)
    {
        var fullRouteName = RoutePicker.SelectedItem?.ToString()?.Trim();
        if (string.IsNullOrWhiteSpace(fullRouteName))
            return;

        SelectedRouteLabel.Text = fullRouteName;
        _selectedRouteNumber = fullRouteName.Split(" -")[0].Trim();

        RefreshButton.IsEnabled = true;
        RefreshButton.IsVisible = true;
        ShowAllBusesButton.IsEnabled = true;
        ShowAllBusesButton.IsVisible = true;

        await LoadVehiclesAsync(_selectedRouteNumber);
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_selectedRouteNumber))
            return;

        await LoadVehiclesAsync(_selectedRouteNumber, true);
    }

    private async void OnShowAllBusesClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_selectedRouteNumber))
            return;

        await LoadVehiclesAsync(_selectedRouteNumber, false);
    }
    
    private async Task LoadVehiclesAsync(string selectedRouteNumber, bool isRefreshing = false)
    {
        try
        {
            var vehicles = await _mbtaApi.GetVehiclesByRouteAsync(selectedRouteNumber);

            var validVehicles = vehicles.Where(v =>
                !double.IsNaN(v.Latitude) && v.Latitude >= -90 && v.Latitude <= 90 &&
                !double.IsNaN(v.Longitude) && v.Longitude >= -180 && v.Longitude <= 180
            ).ToList();

            Console.WriteLine($" ########################### Total vehicles fetched: {vehicles.Count}");

            BusMap.Pins.Clear();

            foreach (var v in validVehicles)
            {
                BusMap.Pins.Add(new Pin
                {
                    Label = v.DisplayName,
                    Location = new Location(v.Latitude, v.Longitude)
                });
            }

            if (validVehicles.Any())
            {
                if (!isRefreshing)
                    FitMapToPins(validVehicles);
                NoBusesLabel.IsVisible = false;
                ShowAllBusesButton.IsVisible = true;
                RefreshButton.IsVisible = true;

            }
            else
            {
                NoBusesLabel.IsVisible = true;
                ShowAllBusesButton.IsVisible = false;
                RefreshButton.IsVisible = false;

                BusMap.MoveToRegion(
                    MapSpan.FromCenterAndRadius(
                        new Location(42.3199, -71.0589),
                        Distance.FromMiles(10)));

            }
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", ex.Message, "OK");
        }
    }

    void FitMapToPins(List<Vehicle> vehicles)
    {
        double minLat = vehicles.Min(v => v.Latitude);
        double maxLat = vehicles.Max(v => v.Latitude);
        double minLng = vehicles.Min(v => v.Longitude);
        double maxLng = vehicles.Max(v => v.Longitude);

        // Calculate span in degrees
        double latSpan = maxLat - minLat;
        double lngSpan = maxLng - minLng;

        // Add ~30 feet buffer in degrees
        double feetToDegreesLat = 60.0 / 364000.0;
        double avgLatRad = ((minLat + maxLat) / 2) * Math.PI / 180.0;
        double feetToDegreesLng = 60.0 / (288200.0 * Math.Cos(avgLatRad));

        latSpan = Math.Max(latSpan, feetToDegreesLat);
        lngSpan = Math.Max(lngSpan, feetToDegreesLng);

        // Convert 1 mile to degrees (~69 miles per degree latitude)
        double milesToDegreesLat = 1.0 / 69.0;  // ≈ 0.0725 degrees
        double milesToDegreesLng = 1.0 / (Math.Cos(avgLatRad) * 69.172);

        // Ensure minimum 1 mile radius
        latSpan = Math.Max(latSpan, milesToDegreesLat);
        lngSpan = Math.Max(lngSpan, milesToDegreesLng);

        // Add slight extra padding
        latSpan *= 1.2;
        lngSpan *= 1.2;

        var centerLat = (minLat + maxLat) / 2;
        var centerLng = (minLng + maxLng) / 2;

        BusMap.MoveToRegion(new MapSpan(
            new Location(centerLat, centerLng),
            latSpan, lngSpan
        ));
    }


    private void OnShowRoutePickerClicked(object sender, EventArgs e)
    {
        RoutePicker.Focus();
    }
}
