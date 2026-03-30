Shader "SmartCampus/ArcGIS/URP/Terrain Surface"
{
    Properties
    {
        [MainTexture] _MainTex("Base Map", 2D) = "white" {}
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [NoScaleOffset] _BumpMap("Normal Map", 2D) = "bump" {}
        [NoScaleOffset] _ArcGISGlobalTerrainOcclusionDepthMap("Terrain Occlusion Depth", 2D) = "black" {}
        _ImageryRegion("Imagery Region", Vector) = (0, 0, 1, 1)
        _NormalMapRegion("Normal Map Region", Vector) = (0, 0, 1, 1)
        _MapAreaMin("Map Area Min", Vector) = (0, 0, 0, 0)
        _MapAreaMax("Map Area Max", Vector) = (0, 0, 0, 0)
        _HasFlattenedArea("Has Flattened Area", Float) = 0
        _FlattenedArea("Flattened Area", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _MainTex_ST;
                float4 _ImageryRegion;
                float4 _NormalMapRegion;
                float4 _MapAreaMin;
                float4 _MapAreaMax;
                float _HasFlattenedArea;
                float _FlattenedArea;
                float4x4 _ArcGISGlobalTerrainOcclusionViewProjMatrix;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 baseMap = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                return baseMap * _BaseColor;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
