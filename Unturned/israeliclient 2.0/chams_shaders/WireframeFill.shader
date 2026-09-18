Shader "Custom/WireframeFill"
{
    Properties
    {
        _Color ("Fill Visible", Color) = (0.2, 0.6, 1.0, 0.6)
        _ColorBehind ("Fill Behind", Color) = (1.0, 0.2, 0.2, 0.4)
        _WireColor ("Wire Visible", Color) = (1, 1, 1, 1)
        _WireColorBehind ("Wire Behind", Color) = (1, 1, 1, 0.7)
        _WireSmooth ("Wire Thickness", Range(0.2, 4.0)) = 1.2
    }

    CGINCLUDE
    #include "UnityCG.cginc"

    float4 _Color, _ColorBehind;
    float4 _WireColor, _WireColorBehind;
    float _WireSmooth;

    struct appdata { float4 vertex : POSITION; };

    struct v2f {
        float4 pos : SV_POSITION;
    };

    // Geometry shader output — carries barycentric coordinates
    struct g2f {
        float4 pos  : SV_POSITION;
        float3 bary : TEXCOORD0;
    };

    v2f vert(appdata v) {
        v2f o;
        o.pos = UnityObjectToClipPos(v.vertex);
        return o;
    }

    // Geometry shader: assign barycentric coords to each triangle vertex
    [maxvertexcount(3)]
    void geom(triangle v2f input[3], inout TriangleStream<g2f> stream) {
        g2f o;
        o.pos = input[0].pos; o.bary = float3(1, 0, 0); stream.Append(o);
        o.pos = input[1].pos; o.bary = float3(0, 1, 0); stream.Append(o);
        o.pos = input[2].pos; o.bary = float3(0, 0, 1); stream.Append(o);
    }

    // Compute wire edge factor from barycentric coordinates
    // Returns 0.0 on edges, 1.0 in interior
    float wireEdge(float3 bary) {
        float3 d = fwidth(bary) * _WireSmooth;
        float3 a = smoothstep(float3(0,0,0), d, bary);
        return min(min(a.x, a.y), a.z);
    }

    // Behind walls: fill + wire blended in one pass
    float4 fragBehind(g2f i) : SV_Target {
        float edge = wireEdge(i.bary);
        return lerp(_WireColorBehind, _ColorBehind, edge);
    }

    // Visible: fill + wire blended in one pass
    float4 fragVisible(g2f i) : SV_Target {
        float edge = wireEdge(i.bary);
        return lerp(_WireColor, _Color, edge);
    }
    ENDCG

    SubShader
    {
        Tags { "Queue"="Overlay+1" "RenderType"="Transparent" }

        // Pass 0: Behind walls (fill + embedded wireframe)
        Pass {
            Name "Behind"
            ZTest Greater ZWrite Off Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment fragBehind
            #pragma target 4.0
            ENDCG
        }

        // Pass 1: Visible (fill + embedded wireframe)
        Pass {
            Name "Visible"
            ZTest LEqual ZWrite Off Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment fragVisible
            #pragma target 4.0
            ENDCG
        }
    }
    Fallback Off
}
