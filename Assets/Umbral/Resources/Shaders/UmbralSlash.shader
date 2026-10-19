// Tajo de pincel en dos tonos, como en Hades II: borde exterior de tinta negra, núcleo de color
// brillante (HDR, para el bloom) y fibras de pincel. Se "dibuja" en barrido con _Head y se deshace por la cola.
// Si se asigna una textura pintada (_UseTex = 1) se usa su forma y color.
Shader "Umbral/Slash"
{
    Properties
    {
        _BaseMap ("Trazo pintado (opcional)", 2D) = "white" {}
        _UseTex ("Usar textura", Float) = 0
        _Core ("Núcleo", Color) = (0.7,0.5,1,1)
        _Edge ("Filo brillante", Color) = (1,1,1,1)
        _Ink ("Tinta", Color) = (0.02,0.01,0.04,1)
        _Intensity ("Intensidad HDR", Float) = 2.5
        _Head ("Cabeza del barrido", Range(0,1.4)) = 1
        _TailLen ("Longitud de la cola", Range(0.05,1.5)) = 1
        _Fade ("Opacidad", Range(0,1)) = 1
        _Seed ("Semilla", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Slash"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float _UseTex;
                float4 _Core, _Edge, _Ink;
                float _Intensity, _Head, _TailLen, _Fade, _Seed;
            CBUFFER_END
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            struct A { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct V { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            V vert(A i) { V o; o.positionCS = TransformObjectToHClip(i.positionOS.xyz); o.uv = i.uv; return o; }

            float hash(float2 p) { p = frac(p * float2(123.34, 456.21)); p += dot(p, p + 45.32); return frac(p.x * p.y); }
            float noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float a = hash(i), b = hash(i + float2(1, 0)), c = hash(i + float2(0, 1)), d = hash(i + float2(1, 1));
                float2 u = f * f * (3 - 2 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            half4 frag(V i) : SV_Target
            {
                float x = i.uv.x, y = i.uv.y;
                // ventana del barrido: la cabeza avanza y la cola se deshace
                float head = smoothstep(_Head + 0.02, _Head - 0.06, x);
                float tail = smoothstep(_Head - _TailLen, _Head - _TailLen + 0.25, x);
                float window = head * tail;
                float taper = pow(saturate(sin(x * 3.14159)), 0.6);
                if (_UseTex > 0.5)
                {
                    float4 t = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                    float3 col = t.rgb * lerp(1.0, _Intensity, saturate(dot(t.rgb, float3(0.33, 0.33, 0.33)) * 1.5));
                    return half4(col, t.a * window * _Fade);
                }
                // fibras del pincel solo en la parte interior del trazo
                float fib = noise(float2(x * 6.0 + _Seed, y * 30.0)) * 0.65 + noise(float2(x * 18.0 - _Seed, y * 60.0)) * 0.35;
                float inner = smoothstep(0.05, 0.6, y) * lerp(0.35, 1.0, fib);
                float alpha = (y > 0.6 ? 1.0 : inner) * taper * window;
                // borde exterior irregular de tinta
                float rim = 0.93 - noise(float2(x * 9.0 + _Seed * 2.0, 3.0)) * 0.07;
                alpha *= step(y, rim + 0.07);
                float3 col;
                if (y > 0.74) col = _Ink.rgb;                                        // tinta
                else if (y > 0.64) col = _Edge.rgb * (_Intensity + 0.8);              // filo blanco
                else col = lerp(_Core.rgb * 0.6, _Core.rgb * _Intensity, smoothstep(0.1, 0.64, y)); // núcleo de color
                col += _Edge.rgb * smoothstep(_Head - 0.1, _Head, x) * (1 - step(0.74, y));
                return half4(col, saturate(alpha) * _Fade);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
