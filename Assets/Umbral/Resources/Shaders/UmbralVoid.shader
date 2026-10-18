// El vacío entre las esferas: nebulosa en espiral, estrellas y esferas iridiscentes lejanas (Yog-Sothoth).
// Se dibuja en un plano enorme por debajo de las salas flotantes.
Shader "Umbral/Void"
{
    Properties
    {
        _ColorA ("Color profundo", Color) = (0.02,0.01,0.05,1)
        _ColorB ("Color nebulosa", Color) = (0.25,0.05,0.4,1)
        _ColorC ("Color brillo", Color) = (0.1,0.6,0.7,1)
        _Scale ("Escala", Float) = 0.02
        _Swirl ("Remolino", Float) = 1.2
        _Speed ("Velocidad", Float) = 0.03
        _StarDensity ("Densidad de estrellas", Float) = 0.985
        _Globes ("Esferas", Range(0,1)) = 1
        _Center ("Centro (xz)", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry-50" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Void"
            Tags { "LightMode"="UniversalForward" }
            ZWrite On
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ColorA, _ColorB, _ColorC;
                float _Scale, _Swirl, _Speed, _StarDensity, _Globes;
                float4 _Center;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionWS = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                return o;
            }

            float hash21(float2 p) { p = frac(p * float2(123.34, 456.21)); p += dot(p, p + 45.32); return frac(p.x * p.y); }
            float noise(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                float a = hash21(i), b = hash21(i + float2(1,0)), c = hash21(i + float2(0,1)), d = hash21(i + float2(1,1));
                float2 u = f * f * (3 - 2 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }
            float fbm(float2 p)
            {
                float v = 0, a = 0.5;
                for (int k = 0; k < 5; k++) { v += a * noise(p); p = p * 2.03 + 17.1; a *= 0.5; }
                return v;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float t = _Time.y * _Speed;
                float2 p = (i.positionWS.xz - _Center.xz) * _Scale;
                float r = length(p);
                float ang = atan2(p.y, p.x) + _Swirl / (r + 0.6) + t;
                float2 sp = float2(cos(ang), sin(ang)) * r;
                float n = fbm(sp * 3.0 + t);
                float n2 = fbm(sp * 6.0 - t * 1.7 + n);
                float3 col = lerp(_ColorA.rgb, _ColorB.rgb, smoothstep(0.35, 0.85, n));
                col += _ColorC.rgb * pow(saturate(n2 - 0.45) * 2.0, 3.0) * 1.6;

                // estrellas
                float2 sg = i.positionWS.xz * 0.6;
                float s = hash21(floor(sg));
                float tw = 0.6 + 0.4 * sin(_Time.y * (2.0 + s * 4.0) + s * 40.0);
                float2 fp = frac(sg) - 0.5;
                col += step(_StarDensity, s) * smoothstep(0.12, 0.0, length(fp)) * tw * 1.5;

                // esferas iridiscentes del Padre, flotando en el abismo
                if (_Globes > 0)
                {
                    float2 gg = i.positionWS.xz * 0.025 + float2(t * 0.3, -t * 0.2);
                    float2 cell = floor(gg);
                    float h = hash21(cell + 3.1);
                    if (h > 0.72)
                    {
                        float2 c = frac(gg) - 0.5 + (float2(hash21(cell + 7.7), hash21(cell + 1.3)) - 0.5) * 0.4;
                        float d = length(c);
                        float rad = 0.12 + h * 0.12;
                        float globe = smoothstep(rad, rad - 0.03, d);
                        float3 irid = 0.5 + 0.5 * cos(6.2831 * (d * 3.0 + h + _Time.y * 0.05 + float3(0, 0.33, 0.67)));
                        col = lerp(col, irid * (0.6 + 0.8 * (1 - d / rad)), globe * _Globes * 0.8);
                        col += irid * smoothstep(rad + 0.08, rad, d) * 0.15 * _Globes;
                    }
                }
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
