Shader "Custom/Flow+Wave" {
    Properties{
        _Color("Color", Color) = (1,1,1,1)
        [NoScaleOffset] _MainTex("Deriv (AG) Height (B)", 2D) = "black" {}
        [NoScaleOffset] _FlowMap("Flow (RG)", 2D) = "black" {}
        [Toggle(_DUAL_GRID)] _DualGrid("Dual Grid", Int) = 0
        _Tiling("Tiling", Float) = 1
        _TilingModulated("Tiling, Modulated", Float) = 1
        _GridResolution("Grid Resolution", Float) = 10
        _Speed("Speed", Float) = 1
        _FlowStrength("Flow Strength", Float) = 0.25
        _HeightScale("Height Scale, Constant", Float) = 0.25
        _HeightScaleModulated("Height Scale, Modulated", Float) = 0.75


        _WaveA("Wave A (Dir, Steepness, Wavelength", Vector) = (1, 0, 0.5, 10)
        _WaveB("Wave B", Vector) = (0, 1, 0.25, 20)
        _WaveC("Wave B", Vector) = (1, 1, 0.15, 10)

        _WaterFogColor("Water Fog Color", Color) = (0, 0, 0, 0)
        _WaterFogDensity ("Water Fog Density", Range(0, 2)) = 0.1
        _RefractionStrength ("Refraction Strength", Range(0, 1)) = 0.25

        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0
    }
    SubShader
    {
        Tags{ "RenderType" = "Transparent" "RenderQueue" = "Transparent" }
        LOD 200

        GrabPass{ "_WaterBackground" }

        CGPROGRAM
        #pragma surface surf Standard alpha vertex:vert finalcolor:ResetAlpha// fullforwardshadows  addshadow
        #pragma target 3.0
        #pragma shader_feature _DUAL_GRID

        #include "Flow.cginc"
        #include "ColorBelowWater.cginc"

        sampler2D _MainTex, _FlowMap;
        float _Tiling, _TilingModulated, _GridResolution, _Speed, _FlowStrength;
        float _HeightScale, _HeightScaleModulated;

        struct Input {
            float2 uv_MainTex;
            float4 screenPos;
        };

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;

        float _Amplitude;
        float4 _WaveA, _WaveB, _WaveC;

        UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_INSTANCING_BUFFER_END(Props)

// Flow
        float3 UnpackDerivativeHeight(float4 textureData)
        {
            float3 dh = textureData.agb;
            dh.xy = dh.xy * 2 - 1;
            return dh;
        }

        float3 FlowCell(float2 uv, float2 offset, float time, bool gridB)
        {
            float2 shift = 1 - offset;
            shift *= 0.5;
            offset *= 0.5;
            if (gridB)
            {
                offset += 0.25;
                shift -= 0.25;
            }

            float2x2 derivRotation;
            float2 uvTiled = (floor(uv * _GridResolution + offset) + shift) / _GridResolution;
            float3 flow = tex2D(_FlowMap, uvTiled).rgb;
            flow.xy = flow.xy * 2;// - 1;
            flow.z *= _FlowStrength;
            float tiling = flow.z * _TilingModulated + _Tiling;
            float2 uvFlow = DirectionalFlowUV(
                uv + offset, flow, tiling, time, derivRotation
            );
            float3 dh = UnpackDerivativeHeight(tex2D(_MainTex, uvFlow));
            dh.xy = mul(derivRotation, dh.xy);
            dh *= flow.z * _HeightScaleModulated + _HeightScale;
            return dh;
        }

        float3 FlowGrid(float2 uv, float time, bool gridB)
        {

            float3 dhA = FlowCell(uv, float2(0, 0), time, gridB);
            float3 dhB = FlowCell(uv, float2(1, 0), time, gridB);
            float3 dhC = FlowCell(uv, float2(0, 1), time, gridB);
            float3 dhD = FlowCell(uv, float2(1, 1), time, gridB);

            float2 t = uv * _GridResolution;
            if (gridB)
            {
                t += 0.25;
            }
            t = abs(2 * frac(t) - 1);

            float wA = (1 - t.x) * (1 - t.y);
            float wB = t.x * (1 - t.y);
            float wC = (1 - t.x) * t.y;
            float wD = t.x * t.y;

            return dhA * wA + dhB * wB + dhC * wC + dhD * wD;
        }

// Waves
        float3 GerstnerWave(float4 wave, float3 p, inout float3 tangent, inout float3 binormal)
        {
            float steepness = wave.z;
            float wavelength = wave.w;

            float k = 2 * UNITY_PI / wavelength;
            float c = sqrt(9.8 / k);
            float2 d = normalize(wave.xy);
            float f = k * (dot(d, p.xz) - c * _Time.y);
            float a = steepness / k;

            // Partial Derivated of F in the X dimension
            tangent += float3(
                -d.x * d.x * (steepness * sin(f)),
                d.x * (steepness * cos(f)),
                -d.x * d.y * (steepness * sin(f))
                );

            // Partial Derivated of F in the Z dimension
            binormal += float3(
                -d.x * d.y * (steepness * sin(f)),
                d.y * (steepness * cos(f)),
                1 - d.y * d.y * (steepness * sin(f))
                );

            return float3(
                d.x * (a * cos(f)),
                a * sin(f),
                d.y * (a * cos(f))
                );
        }

        void vert(inout appdata_full vertexData)
        {
            float3 gridPoint = vertexData.vertex.xyz;
            float3 tangent = float3(1, 0, 0);
            float3 binormal = float3(0, 0, 1);
            float3 p = gridPoint;
            p += GerstnerWave(_WaveA, gridPoint, tangent, binormal);
            p += GerstnerWave(_WaveB, gridPoint, tangent, binormal);
            p += GerstnerWave(_WaveC, gridPoint, tangent, binormal);

            float3 normal = normalize(cross(binormal, tangent));
            vertexData.vertex.xyz = p;
            vertexData.normal = normal;
        }

// Transparent
        void ResetAlpha(Input IN, SurfaceOutputStandard o, inout fixed4 color)
        {
            color.a = 1;
        }

        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            float time = _Time.y * _Speed;
            float2 uv = IN.uv_MainTex * _Tiling;
            float3 dh = FlowGrid(uv, time, false);

#if defined(_DUAL_GRID)
            dh = (dh + FlowGrid(uv, time, true)) * 0.5;
#endif

            fixed4 c = dh.z * dh.z * _Color;
            
            o.Albedo = c.rgb;
            o.Alpha = c.a;
            o.Emission = ColorBelowWater(IN.screenPos, o.Normal) * (1 - c.a);

            o.Normal = normalize(float3(-dh.xy, 1));
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
        }
        
        

        ENDCG
    }
    // FallBack "Diffuse"
}