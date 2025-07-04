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
            new Location(42.3601, -71.0589),
            Distance.FromMiles(10)
        ));

        LoadRoutes();
    }

    private async void LoadRoutes()
    {
        var routes = await _mbtaApi.GetBusRoutesAsync();
        RoutePicker.ItemsSource = routes.Select(r => r.DisplayName).ToList();
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
