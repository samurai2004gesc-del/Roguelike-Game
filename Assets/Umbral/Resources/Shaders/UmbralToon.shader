// Sombreado "cel" con contorno de tinta, inspirado en el look pintado a mano de Hades:
// bandas de luz duras, sombras teñidas, luz de borde (rim) y contorno negro de grosor constante en pantalla.
Shader "Umbral/Toon"
{
    Properties
    {
        _BaseColor ("Color base", Color) = (1,1,1,1)
        _BaseMap ("Textura", 2D) = "white" {}
        _ShadeColor ("Color de sombra (multiplica)", Color) = (0.32,0.24,0.45,1)
        _ShadeThreshold ("Umbral de sombra", Range(-1,1)) = 0.1
        _ShadeSoftness ("Suavidad de sombra", Range(0.001,1)) = 0.06
        _RimColor ("Luz de borde", Color) = (0.6,0.5,1,1)
        _RimPower ("Potencia de borde", Range(0.5,8)) = 3
        _EmissionColor ("Emisión", Color) = (0,0,0,1)
        _EmissionMap ("Mapa de emisión", 2D) = "white" {}
        _EmissionMapWeight ("Peso mapa emisión", Range(0,1)) = 0
        _OutlineColor ("Color de contorno", Color) = (0.03,0.02,0.05,1)
        _OutlineWidth ("Grosor de contorno", Range(0,4)) = 1
        _VertexColorWeight ("Peso color de vértice", Range(0,1)) = 0
        _FlashColor ("Color de destello", Color) = (1,1,1,1)
        _Flash ("Destello", Range(0,1)) = 0
    }

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    CBUFFER_START(UnityPerMaterial)
        float4 _BaseColor;
        float4 _BaseMap_ST;
        float4 _ShadeColor;
        float _ShadeThreshold;
        float _ShadeSoftness;
        float4 _RimColor;
        float _RimPower;
        float4 _EmissionColor;
        float4 _EmissionMap_ST;
        float _EmissionMapWeight;
        float4 _OutlineColor;
        float _OutlineWidth;
        float _VertexColorWeight;
        float4 _FlashColor;
        float _Flash;
    CBUFFER_END

    TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
    TEXTURE2D(_EmissionMap); SAMPLER(sampler_EmissionMap);
    ENDHLSL

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        Pass
        {
            Name "ToonForward"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float4 color : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes i)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);
                o.positionWS = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(o.positionWS);
                o.normalWS = TransformObjectToWorldNormal(i.normalOS);
                o.uv = i.uv;
                o.color = i.color;
                return o;
            }

            float Band(float ndl)
            {
                return smoothstep(_ShadeThreshold - _ShadeSoftness, _ShadeThreshold + _ShadeSoftness, ndl);
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float3 n = normalize(i.normalWS);
                float3 v = normalize(GetWorldSpaceViewDir(i.positionWS));
                float4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv * _BaseMap_ST.xy + _BaseMap_ST.zw);
                float3 vc = lerp(float3(1,1,1), i.color.rgb, _VertexColorWeight);
                float3 albedo = _BaseColor.rgb * tex.rgb * vc;

                float4 shadowCoord = TransformWorldToShadowCoord(i.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                float ndl = dot(n, mainLight.direction);
                float lit = Band(ndl) * lerp(0.35, 1.0, mainLight.shadowAttenuation);
                float3 shade = albedo * _ShadeColor.rgb;
                float3 col = lerp(shade, albedo, lit) * mainLight.color;

                // Luces adicionales (antorchas, hongos brillantes, proyectiles): bandas suaves y aditivas
                #if defined(_ADDITIONAL_LIGHTS)
                uint count = GetAdditionalLightsCount();
                for (uint li = 0u; li < count; li++)
                {
                    Light l = GetAdditionalLight(li, i.positionWS);
                    float a = l.distanceAttenuation * l.shadowAttenuation;
                    float b = smoothstep(0.0, 0.25, dot(n, l.direction)) * smoothstep(0.0, 0.08, a);
                    col += albedo * l.color * b * saturate(a * 4.0);
                }
                #endif

                // Ambiente mínimo para que las sombras nunca sean negras puras
                col += albedo * SampleSH(n) * 0.35;

                float rim = pow(saturate(1.0 - dot(n, v)), _RimPower);
                col += _RimColor.rgb * rim * (0.4 + 0.6 * lit);

                float3 emi = _EmissionColor.rgb * lerp(1.0, SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, i.uv * _EmissionMap_ST.xy + _EmissionMap_ST.zw).rgb, _EmissionMapWeight);
                col += emi;
                col = lerp(col, _FlashColor.rgb * 2.0, _Flash);
                return half4(col, 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(i);
                float4 pos = TransformObjectToHClip(i.positionOS.xyz);
                if (_OutlineWidth <= 0.0001)
                {
                    o.positionCS = float4(2, 2, 2, 1); // fuera del volumen de recorte
                    return o;
                }
                float3 nWS = TransformObjectToWorldNormal(i.normalOS);
                float2 nCS = mul((float3x3)UNITY_MATRIX_VP, nWS).xy;
                float l = length(nCS);
                nCS = l > 0.0001 ? nCS / l : float2(0, 0);
                nCS.x *= _ScreenParams.y / _ScreenParams.x;
                pos.xy += nCS * _OutlineWidth * 0.0035 * pos.w;
                o.positionCS = pos;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                return half4(lerp(_OutlineColor.rgb, _FlashColor.rgb, _Flash * 0.5), 1);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(i);
                float3 posWS = TransformObjectToWorld(i.positionOS.xyz);
                float3 nWS = TransformObjectToWorldNormal(i.normalOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDir = normalize(_LightPosition - posWS);
                #else
                    float3 lightDir = _LightDirection;
                #endif
                float4 pos = TransformWorldToHClip(ApplyShadowBias(posWS, nWS, lightDir));
                #if UNITY_REVERSED_Z
                    pos.z = min(pos.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    pos.z = max(pos.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                o.positionCS = pos;
                return o;
            }

            half4 frag(Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; };
            Varyings vert(Attributes i) { Varyings o; UNITY_SETUP_INSTANCE_ID(i); o.positionCS = TransformObjectToHClip(i.positionOS.xyz); return o; }
            half4 frag(Varyings i) : SV_Target { return 0; }
            ENDHLSL
        }
    }
    FallBack Off
}
