using Esri.ArcGISMapsSDK.Components;
using Esri.ArcGISMapsSDK.Utils.GeoCoord;
using Esri.GameEngine.Geometry;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class ArcGISMapCoordinateProjector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ArcGISMapComponent arcGISMap;

    private ArcGISSpatialReference wgs84;

    private void Awake()
    {
        arcGISMap ??= FindFirstObjectByType<ArcGISMapComponent>(FindObjectsInactive.Include);
        wgs84 = ArcGISSpatialReference.WGS84();
    }

    public bool IsReady => arcGISMap != null;

    public void ApplyGeographicPosition(ArcGISLocationComponent locationComponent, double latitude, double longitude, double altitudeMeters)
    {
        if (locationComponent == null)
        {
            return;
        }

        locationComponent.Position = new ArcGISPoint(longitude, latitude, altitudeMeters, wgs84);
        locationComponent.Rotation = new ArcGISRotation(0d, 0d, 0d);
    }
}
