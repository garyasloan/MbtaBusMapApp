//using Android.Graphics;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using MbtaBusMapApp.Models;
using MbtaBusMapApp.Services;
using System.Net.NetworkInformation;
using System.Linq;
using MbtaBusMapApp.Helpers;
using System.Diagnostics;

namespace MbtaBusMapApp;


public partial class MbtaMapPage : ContentPage
{
    private readonly MbtaApiService _mbtaApi;
    //private string _selectedRoute = null!;

    public MbtaMapPage()
    {
        InitializeComponent();
        string apiKey = ConfigHelper.GetApiKey();
        _mbtaApi = new MbtaApiService(apiKey);

        //  Center map on Boston on startup
        BusMap.MoveToRegion(MapSpan.FromCenterAndRadius(
            new Location(42.3601, -71.0589),
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

    //  Button click shows picker wheel
    private void OnShowRoutePickerClicked(object sender, EventArgs e)
    {
        RoutePicker.Focus();
    }

    private string _selectedRouteNumber = null!;
    private async void OnRouteSelected(object sender, EventArgs e)
    {
        var fullRouteName = RoutePicker.SelectedItem?.ToString()?.Trim();
        if (string.IsNullOrWhiteSpace(fullRouteName))
            return;

        // Show full DisplayName in the label
        SelectedRouteLabel.Text = fullRouteName;

        // Extract just the route number for API calls
        _selectedRouteNumber = fullRouteName.Split(" -")[0].Trim();

        // Enable the Refresh button now that we have a selection
        RefreshButton.IsEnabled = true;
        RefreshButton.IsVisible = true;

        // Load vehicles for the selected route
        await LoadVehiclesAsync(_selectedRouteNumber);
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(SelectedRouteLabel.Text))
        {
            await DisplayAlert("No route", "Please select a route first.", "OK");
            return;
        }

        // Extract just the route number for the API call
        string selectedRouteNumber = SelectedRouteLabel.Text.Split(" -")[0].Trim();
        await LoadVehiclesAsync(selectedRouteNumber, true);
    }

    //  Helper: shrink font to fit on one line
    private async Task AdjustLabelFontSizeToFit(Label label, string text)
    {
        await Task.Delay(50); // Wait for layout to be ready

        double availableWidth = label.Width;
        double fontSize = 48;

        while (fontSize > 12)
        {
            var testLabel = new Label
            {
                Text = text,
                FontSize = fontSize
            };

            var sizeRequest = testLabel.Measure(double.PositiveInfinity, double.PositiveInfinity);
            if (sizeRequest.Request.Width <= availableWidth)
                break;

            fontSize -= 1;
        }

        label.FontSize = fontSize;
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

            if (!validVehicles.Any())
            {
                Console.WriteLine("No valid vehicles — fallback to Boston.");

                NoBusesLabel.IsVisible = true; //  SHOW the message

                BusMap.MoveToRegion(MapSpan.FromCenterAndRadius(
                    new Location(42.3601, -71.0589),
                    Distance.FromMiles(10)
                ));
                BusMap.Pins.Clear();
                return;
            }
            else
            {
                NoBusesLabel.IsVisible = false; //  HIDE the message if vehicles found
            }

            BusMap.Pins.Clear();
            foreach (var v in validVehicles)
            {
                BusMap.Pins.Add(new Pin
                {
                    Label = v.DisplayName,
                    Location = new Location(v.Latitude, v.Longitude)
                });
            }

            if (isRefreshing)
                return;


            double minLat = validVehicles.Min(v => v.Latitude);
            double maxLat = validVehicles.Max(v => v.Latitude);
            double minLng = validVehicles.Min(v => v.Longitude);
            double maxLng = validVehicles.Max(v => v.Longitude);

            double centerLat = (minLat + maxLat) / 2.0;
            double centerLng = (minLng + maxLng) / 2.0;

            double distanceLat = Distance.BetweenPositions(
                new Location(minLat, centerLng),
                new Location(maxLat, centerLng)
            ).Miles;

            double distanceLng = Distance.BetweenPositions(
                new Location(centerLat, minLng),
                new Location(centerLat, maxLng)
            ).Miles;

            double radius = Math.Max(distanceLat, distanceLng) / 2.0;
            radius = Math.Max(radius * 1.2, 0.5); // Add margin

            BusMap.MoveToRegion(MapSpan.FromCenterAndRadius(
                new Location(centerLat, centerLng),
                Distance.FromMiles(radius)
            ));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"LoadVehiclesAsync failed: {ex.Message}");
            await DisplayAlert("Error", ex.Message, "OK");

            BusMap.MoveToRegion(MapSpan.FromCenterAndRadius(
                new Location(42.3601, -71.0589),
                Distance.FromMiles(10)
            ));
        }
    }
}

