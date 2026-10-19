// Personaje pintado (imagen generada por IA) mostrado como figura frente a la cámara.
// El vértice se deforma para animarlo como una marioneta de papel: inclinación, curvatura,
// ondulación de túnicas/tentáculos, estirar/aplastar y desplazamiento (saltitos al caminar).
// Recorte alfa con escritura de profundidad para que se ordene bien con el escenario 3D.
Shader "Umbral/Sprite"
{
    Properties
    {
        _BaseMap ("Imagen", 2D) = "white" {}
        _BaseColor ("Tinte", Color) = (1,1,1,1)
        _Cutoff ("Recorte alfa", Range(0,1)) = 0.45
        _Brightness ("Brillo", Float) = 1
        _FlashColor ("Color de destello", Color) = (1,1,1,1)
        _Flash ("Destello", Range(0,1)) = 0
        _Shear ("Inclinación", Float) = 0
        _Bend ("Curvatura", Float) = 0
        _Stretch ("Escala XY", Vector) = (1,1,0,0)
        _Offset ("Desplazamiento XY", Vector) = (0,0,0,0)
        _Wave ("Onda (amplitud, frecuencia, velocidad, altura)", Vector) = (0,6,3,0.5)
        _WaveTime ("Tiempo de onda", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "RenderPipeline"="UniversalPipeline" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            float4 _BaseColor;
            float _Cutoff;
            float _Brightness;
            float4 _FlashColor;
            float _Flash;
            float _Shear;
            float _Bend;
            float4 _Stretch;
            float4 _Offset;
            float4 _Wave;
            float _WaveTime;
        CBUFFER_END
        TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

        // La malla es una rejilla con x en [-0.5, 0.5] e y en [0, 1] (pies abajo)
        float3 Deform(float3 p)
        {
            float v = saturate(p.y);
            p.x *= _Stretch.x;
            p.y *= _Stretch.y;
            p.x += _Shear * v + _Bend * v * v;
            float low = saturate(1.0 - v / max(0.01, _Wave.w));
            p.x += _Wave.x * sin(_WaveTime * _Wave.z + v * _Wave.y) * low * low;
            p.xy += _Offset.xy;
            return p;
        }
        ENDHLSL

        Pass
        {
            Name "SpriteForward"
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            ZWrite On

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };

            Varyings vert(Attributes i)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(Deform(i.positionOS.xyz));
                o.uv = i.uv;
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                float4 c = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv) * _BaseColor;
                clip(c.a - _Cutoff);
                float3 col = c.rgb * _Brightness;
                col = lerp(col, _FlashColor.rgb * 1.6, _Flash);
                return half4(col, 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
