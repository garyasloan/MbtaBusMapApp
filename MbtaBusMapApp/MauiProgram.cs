using CommunityToolkit.Maui;

#if IOS
using MbtaBusMapApp.Platforms.iOS; // Only for iOS
#endif

using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls.Handlers.Items;
using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;
using SkiaSharp.Views.Maui.Controls.Hosting;
using Syncfusion.Maui.Toolkit.Hosting;

namespace MbtaBusMapApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureSyncfusionToolkit()
            .UseSkiaSharp()
            .UseMauiMaps()
            .ConfigureMauiHandlers(handlers =>
            {
#if IOS
                handlers.AddHandler(typeof(Microsoft.Maui.Controls.Maps.Map), typeof(ClusteredMapHandler));
#endif

#if IOS || MACCATALYST
                handlers.AddHandler<CollectionView, CollectionViewHandler>();
#endif
            })
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("SegoeUI-Semibold.ttf", "SegoeSemibold");
                fonts.AddFont("FluentSystemIcons-Regular.ttf", Fonts.FluentUI.FontFamily);
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
