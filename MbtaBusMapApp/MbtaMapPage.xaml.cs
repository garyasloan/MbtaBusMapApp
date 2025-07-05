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

        await LoadVehiclesAsync(_selectedRouteNumber);
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_selectedRouteNumber))
            return;

        await LoadVehiclesAsync(_selectedRouteNumber, true);
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
                FitMapToPins(validVehicles);
                NoBusesLabel.IsVisible = false;
            }
            else
            {
                NoBusesLabel.IsVisible = true;
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

        double latSpan = Math.Max(maxLat - minLat, 0.005) * 1.2;
        double lngSpan = Math.Max(maxLng - minLng, 0.005) * 1.2;

        BusMap.MoveToRegion(new MapSpan(
            new Location((minLat + maxLat) / 2, (minLng + maxLng) / 2),
            latSpan,
            lngSpan
        ));
    }

    private void OnShowRoutePickerClicked(object sender, EventArgs e)
    {
        RoutePicker.Focus();
    }
}
