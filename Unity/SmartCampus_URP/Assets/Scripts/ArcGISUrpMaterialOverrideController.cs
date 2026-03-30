using Esri.ArcGISMapsSDK.Authentication;
using Esri.ArcGISMapsSDK.Components;
using Esri.GameEngine;
using Esri.GameEngine.Layers.Base;
using Esri.GameEngine.Map;
using Esri.Unity;
using System;
using UnityEngine;

namespace SmartCampus.Rendering
{
    [DisallowMultipleComponent]
    [ExecuteAlways]
    [DefaultExecutionOrder(1000)]
    [RequireComponent(typeof(ArcGISMapComponent))]
    public sealed class ArcGISUrpMaterialOverrideController : MonoBehaviour
    {
        [SerializeField] private ArcGISMapComponent mapComponent;
        [SerializeField] private Material elevationMaterial;
        [SerializeField] private bool recreateBasemapOnFirstApply = true;
        [SerializeField] private bool retryLoadAfterApply = true;

        private ArcGISMap currentMap;
        private bool elevationMaterialApplied;
        private bool basemapReloadIssued;

        private void OnEnable()
        {
            ResolveMapComponent();
            TryApplyOverrides();
        }

        private void Update()
        {
            TryApplyOverrides();
        }

        private void OnValidate()
        {
            ResolveMapComponent();
        }

        private bool ResolveMapComponent()
        {
            if (mapComponent != null)
            {
                return true;
            }

            mapComponent = GetComponent<ArcGISMapComponent>();
            return mapComponent != null;
        }

        private void TryApplyOverrides()
        {
            if (!ResolveMapComponent())
            {
                return;
            }

            var map = mapComponent.View?.Map;
            if (map == null)
            {
                return;
            }

            if (!ReferenceEquals(currentMap, map))
            {
                currentMap = map;
                elevationMaterialApplied = false;
                basemapReloadIssued = false;
            }

            if (!TryApplyElevationMaterial(currentMap))
            {
                return;
            }

            if (!basemapReloadIssued && recreateBasemapOnFirstApply)
            {
                try
                {
                    RecreateConfiguredBasemap(currentMap);
                    basemapReloadIssued = true;
                }
                catch (Exception)
                {
                    return;
                }
            }

            if (!retryLoadAfterApply)
            {
                return;
            }

            try
            {
                RetryLoadIfNeeded(currentMap);
                RetryLoadIfNeeded(currentMap.Basemap);
                RetryLoadLayers(currentMap.Basemap?.BaseLayers);
                RetryLoadLayers(currentMap.Basemap?.ReferenceLayers);
            }
            catch (Exception)
            {
                // ArcGIS objects can be temporarily unavailable while the map is initializing in edit mode.
            }
        }

        private bool TryApplyElevationMaterial(ArcGISMap map)
        {
            if (elevationMaterialApplied || elevationMaterial == null || map?.Elevation == null)
            {
                return elevationMaterialApplied || elevationMaterial == null;
            }

            try
            {
                map.Elevation.MaterialReference = elevationMaterial;
                elevationMaterialApplied = true;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private void RecreateConfiguredBasemap(ArcGISMap map)
        {
            var basemapSource = mapComponent.Basemap;
            if (string.IsNullOrWhiteSpace(basemapSource))
            {
                return;
            }

            var apiKey = mapComponent.BasemapAuthenticationType == ArcGISAuthenticationType.APIKey
                ? mapComponent.APIKey
                : string.Empty;

            map.Basemap = mapComponent.BasemapType switch
            {
                BasemapTypes.ImageLayer => new ArcGISBasemap(basemapSource, ArcGISLayerType.ArcGISImageLayer, apiKey),
                BasemapTypes.VectorTileLayer => new ArcGISBasemap(basemapSource, ArcGISLayerType.ArcGISVectorTileLayer, apiKey),
                _ => new ArcGISBasemap(basemapSource, apiKey)
            };
        }

        private static void RetryLoadLayers(Esri.Unity.ArcGISCollection<ArcGISLayer> layers)
        {
            if (layers == null)
            {
                return;
            }

            for (ulong i = 0; i < layers.GetSize(); i++)
            {
                RetryLoadIfNeeded(layers.At(i));
            }
        }

        private static void RetryLoadIfNeeded(ArcGISLoadable loadable)
        {
            if (loadable == null)
            {
                return;
            }

            if (loadable.LoadStatus is ArcGISLoadStatus.NotLoaded or ArcGISLoadStatus.FailedToLoad)
            {
                loadable.RetryLoad();
            }
        }
    }
}
