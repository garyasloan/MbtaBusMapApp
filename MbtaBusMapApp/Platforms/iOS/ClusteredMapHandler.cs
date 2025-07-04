using MapKit;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Maps.Handlers;
using Microsoft.Maui.Maps.Platform;
using UIKit;

namespace MbtaBusMapApp.Platforms.iOS;

public class ClusteredMapHandler : MapHandler
{
    protected override MauiMKMapView CreatePlatformView()
    {
        Console.WriteLine(" ClusteredMapHandler.CreatePlatformView called");

        var mapView = base.CreatePlatformView();

        if (OperatingSystem.IsIOSVersionAtLeast(11))
        {
            Console.WriteLine(" Registering clustering view");
            mapView.Register(typeof(MKMarkerAnnotationView), MKMapViewDefault.AnnotationViewReuseIdentifier);
        }

        //  Don't set your own Delegate!
        return mapView;
    }

    protected override void ConnectHandler(MauiMKMapView platformView)
    {
        base.ConnectHandler(platformView);

        Console.WriteLine(" Hooking native annotation handler");

        platformView.GetViewForAnnotation = (mapView, annotation) =>
        {
            if (annotation is MKUserLocation)
                return null;

            if (annotation is MKClusterAnnotation cluster)
            {
                Console.WriteLine($" Cluster: {cluster.MemberAnnotations.Length} pins");
                var clusterView = new MKMarkerAnnotationView(cluster, "Cluster");
                clusterView.MarkerTintColor = UIColor.Red;
                return clusterView;
            }

            var pinView = new MKMarkerAnnotationView(annotation, "Pin");
            pinView.ClusteringIdentifier = "busCluster";
            pinView.CanShowCallout = true;
            pinView.MarkerTintColor = UIColor.Blue;

            return pinView;
        };
    }
}
