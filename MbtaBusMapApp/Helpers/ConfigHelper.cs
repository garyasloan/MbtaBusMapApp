using System.Text.Json;

namespace MbtaBusMapApp.Helpers;

public static class ConfigHelper
{
    public static string GetApiKey()
    {
#if IOS || MACCATALYST
        var path = Foundation.NSBundle.MainBundle.PathForResource("appsettings", "json");
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            throw new FileNotFoundException("appsettings.json not found in iOS bundle");
        var json = File.ReadAllText(path);

#elif ANDROID
        using var stream = Android.App.Application.Context.Assets.Open("appsettings.json");
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();

#else
        // Windows or other .NET targets
        var filePath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(filePath))
            throw new FileNotFoundException("appsettings.json not found in output directory");
        var json = File.ReadAllText(filePath);
#endif

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("ApiKey").GetString() ?? "";
    }
}
