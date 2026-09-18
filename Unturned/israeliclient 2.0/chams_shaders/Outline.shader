Shader "Custom/Outline"
{
    Properties
    {
        _Color ("Visible Color", Color) = (1, 0, 1, 1)
        _ColorBehind ("Behind Color", Color) = (1, 0, 1, 0.6)
        _Width ("Outline Width", Range(0.005, 0.08)) = 0.025
    }

    CGINCLUDE
    #include "UnityCG.cginc"

    float4 _Color;
    float4 _ColorBehind;
    float _Width;

    struct appdata
    {
        float4 vertex : POSITION;
        float3 normal : NORMAL;
    };

    struct v2f
    {
        float4 pos : SV_POSITION;
        float3 worldNormal : TEXCOORD0;
        float3 viewDir : TEXCOORD1;
    };

    v2f vertOutline(appdata v)
    {
        v2f o;
        // Expand along object-space normal
        float3 expanded = v.vertex.xyz + normalize(v.normal) * _Width;
        o.pos = UnityObjectToClipPos(float4(expanded, 1.0));
        // Pass world normal of expanded mesh for fresnel calc
        o.worldNormal = normalize(UnityObjectToWorldNormal(v.normal));
        // View direction: camera to vertex in world space
        float3 worldPos = mul(unity_ObjectToWorld, float4(expanded, 1.0)).xyz;
        o.viewDir = normalize(_WorldSpaceCameraPos - worldPos);
        return o;
    }

    float4 fragOutline(v2f i, float4 baseColor)
    {
        float3 N = normalize(i.worldNormal);
        float3 V = normalize(i.viewDir);
        // Fresnel: 0 facing camera, 1 at edges (silhouette)
        float fresnel = 1.0 - saturate(dot(N, V));
        // Edge factor: sharpen the edge detection
        float edge = pow(fresnel, 0.6);
        // Alpha: center is transparent, edges are opaque
        float alpha = baseColor.a * smoothstep(0.15, 0.5, edge);
        return float4(baseColor.rgb, alpha);
    }
    ENDCG

    SubShader
    {
        Tags { "Queue"="Overlay+5" "RenderType"="Transparent" }

        // Pass 0: Behind walls — outline glow through geometry
        Pass
        {
            Name "Behind"
            ZTest Greater
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vertOutline
            #pragma fragment frag

            float4 frag(v2f i) : SV_Target
            {
                return fragOutline(i, _ColorBehind);
            }
            ENDCG
        }

        // Pass 1: Visible — outline glow on visible surfaces
        Pass
        {
            Name "Visible"
            ZTest LEqual
            ZWrite Off
            Cull Off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            #pragma vertex vertOutline
            #pragma fragment frag

            float4 frag(v2f i) : SV_Target
            {
                return fragOutline(i, _Color);
            }
            ENDCG
        }
    }

    Fallback Off
}
