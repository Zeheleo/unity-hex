Shader "Custom/Terrain"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Terrain Texture Array", 2DArray) = "white" {}
        _GridTex ("Grid Texture", 2D) = "white" {}
        _Glossiness("Smoothness", Range(0,1)) = 0.5
        _Metallic("Metallic", Range(0,1)) = 0.0
    }
        SubShader
        {
            Tags { "RenderType" = "Opaque" }
            LOD 200

            CGPROGRAM
            #pragma surface surf Standard fullforwardshadows vertex:vert
            #pragma target 3.5 // To enable texture array in all platforms

            #pragma multi_compile _ GRID_ON

            UNITY_DECLARE_TEX2DARRAY(_MainTex);

        struct Input
        {
            float4 color : COLOR;
            float3 worldPos;
            float3 terrain;
        };

        void vert(inout appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.terrain = v.texcoord2.xyz;
        }

        float4 GetTerrainColor(Input IN, int index)
        {
            float3 uvw = float3(IN.worldPos.xz * 0.02, IN.terrain[index]);
            float4 c = UNITY_SAMPLE_TEX2DARRAY(_MainTex, uvw);
            return c * IN.color[index];
        }

        half _Glossiness;
        half _Metallic;
        fixed4 _Color;
        sampler2D _GridTex;

        UNITY_INSTANCING_BUFFER_START(Props)
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = GetTerrainColor(IN, 0) + GetTerrainColor(IN, 1) + GetTerrainColor(IN, 2);
           
            fixed4 grid = 1;

            #if defined(GRID_ON)
                float2 gridUV = IN.worldPos.xz;
                gridUV.x *= 1 / (1.33 * 4 * 10 * 0.866025); // 0.86605 = square root of 3 / 2
                gridUV.y *= 1 / (2 * 15.0);
                grid = tex2D(_GridTex, gridUV);
            #endif

            o.Albedo = c.rgb * grid * _Color;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
