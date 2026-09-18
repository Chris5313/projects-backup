Shader "Custom/Chams"
{
    Properties
    {
        _Color ("Visible Color", Color) = (0.2, 0.6, 1.0, 0.8)
        _ColorBehind ("Behind Color", Color) = (1.0, 0.2, 0.2, 0.5)
        _Emission ("Emission", Range(0, 3)) = 0.6
        _Pattern ("Pattern", Int) = 1
        _Scale ("Pattern Scale", Range(2, 40)) = 12
    }

    CGINCLUDE
    #include "UnityCG.cginc"

    float4 _Color, _ColorBehind;
    float _Emission, _Scale;
    int _Pattern;

    struct appdata { float4 vertex : POSITION; float3 normal : NORMAL; };
    struct v2f {
        float4 pos : SV_POSITION;
        float3 wn  : TEXCOORD0;
        float3 vd  : TEXCOORD1;
        float3 wp  : TEXCOORD2;
    };

    v2f vert(appdata v) {
        v2f o;
        o.pos = UnityObjectToClipPos(v.vertex);
        o.wp  = mul(unity_ObjectToWorld, v.vertex).xyz;
        o.wn  = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));
        o.vd  = normalize(_WorldSpaceCameraPos - o.wp);
        return o;
    }

    // ── Star of David: two overlapping equilateral triangles ──
    // Explicit half-plane test — not SDF. Clean, crisp, unmistakable.
    float inTriUp(float2 p, float r) {
        // Equilateral triangle pointing UP, centered at origin
        float h = r * 1.732; // height = r * sqrt(3)
        float bottom = step(-h * 0.333, p.y);               // above bottom edge
        float left   = step(p.y, 1.732 * p.x + h * 0.667);  // below left edge
        float right  = step(p.y, -1.732 * p.x + h * 0.667); // below right edge
        return bottom * left * right;
    }

    float starOfDavid(float2 p, float r) {
        float up   = inTriUp(p, r);
        float down = inTriUp(-p, r); // flipped = triangle pointing DOWN
        return max(up, down);        // union = Star of David
    }

    // Pattern 0: Star of David tiled on XY plane
    float4 patternStar(float4 col, float3 wp, float fresnel) {
        float s = _Scale * 0.08;
        float2 uv = wp.xy * s;
        float2 cell = frac(uv) - 0.5;
        float star = starOfDavid(cell, 0.35);

        float3 dark = col.rgb * 0.05;
        float3 bright = col.rgb * (1.5 + _Emission);
        col.rgb = lerp(dark, bright, star);
        col.rgb += pow(fresnel, 3.0) * col.rgb * _Emission * 0.2;
        col.a = saturate(col.a * (0.2 + star * 0.8));
        return col;
    }

    float4 patternFlat(float4 col, float fresnel) {
        col.rgb += pow(fresnel, 2.0) * col.rgb * _Emission;
        col.a = saturate(col.a + pow(fresnel, 2.0) * 0.25);
        return col;
    }

    float4 patternHologram(float4 col, float3 wp, float fresnel) {
        float scan = step(0.5, frac(wp.y * 40.0 + _Time.y * 2.0));
        float flicker = 0.92 + 0.08 * sin(_Time.y * 17.0);
        col.rgb *= (0.55 + 0.45 * scan) * flicker;
        col.rgb += pow(fresnel, 1.8) * col.rgb * _Emission;
        col.a = saturate(col.a * lerp(0.45, 0.7, scan) + pow(fresnel, 1.8) * 0.3);
        return col;
    }

    float4 patternGlow(float4 col, float fresnel) {
        float rim = pow(fresnel, 0.7);
        col.rgb = col.rgb * ((1.0 - rim) * 0.1 + rim * (1.5 + _Emission * 2.0));
        col.a = saturate(col.a * (0.15 + rim * 0.85));
        return col;
    }

    float4 applyPattern(float4 baseCol, v2f i) {
        float fresnel = 1.0 - saturate(dot(normalize(i.wn), normalize(i.vd)));
        if (_Pattern == 0) return patternStar(baseCol, i.wp, fresnel);
        if (_Pattern == 2) return patternHologram(baseCol, i.wp, fresnel);
        if (_Pattern == 3) return patternGlow(baseCol, fresnel);
        return patternFlat(baseCol, fresnel);
    }

    ENDCG

    SubShader {
        Tags { "Queue"="Overlay+1" "RenderType"="Transparent" }
        Pass {
            Name "Behind"
            ZTest Greater ZWrite Off Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            float4 frag(v2f i) : SV_Target { return applyPattern(_ColorBehind, i); }
            ENDCG
        }
        Pass {
            Name "Visible"
            ZTest LEqual ZWrite Off Cull Off
            Blend SrcAlpha OneMinusSrcAlpha
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            float4 frag(v2f i) : SV_Target { return applyPattern(_Color, i); }
            ENDCG
        }
    }
    Fallback Off
}
