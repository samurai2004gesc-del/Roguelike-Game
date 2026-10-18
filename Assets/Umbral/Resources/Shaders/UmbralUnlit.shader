// Shader sin iluminación para efectos: brillos, partículas, avisos de ataque, arcos de tajo, runas.
// El modo de mezcla se controla desde código (aditivo / alfa).
Shader "Umbral/Unlit"
{
    Properties
    {
        _BaseColor ("Color", Color) = (1,1,1,1)
        _BaseMap ("Textura", 2D) = "white" {}
        _Intensity ("Intensidad (HDR)", Float) = 1
        _VertexColorWeight ("Peso color de vértice", Range(0,1)) = 1
        _ScrollSpeed ("Desplazamiento UV", Vector) = (0,0,0,0)
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src", Float) = 5
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst", Float) = 1
        [Enum(Off,0,On,1)] _ZWrite ("ZWrite", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Cull", Float) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 4
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" "IgnoreProjector"="True" }

        Pass
        {
            Name "Unlit"
            Tags { "LightMode"="UniversalForward" }
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            ZTest [_ZTest]
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float _Intensity;
                float _VertexColorWeight;
                float4 _ScrollSpeed;
                float _SrcBlend;
                float _DstBlend;
                float _ZWrite;
                float _Cull;
                float _ZTest;
            CBUFFER_END
            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);

            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; float4 color : COLOR; UNITY_VERTEX_INPUT_INSTANCE_ID };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float4 color : TEXCOORD1; UNITY_VERTEX_INPUT_INSTANCE_ID };

            Varyings vert(Attributes i)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(i);
                UNITY_TRANSFER_INSTANCE_ID(i, o);
                o.positionCS = TransformObjectToHClip(i.positionOS.xyz);
                o.uv = i.uv * _BaseMap_ST.xy + _BaseMap_ST.zw + _ScrollSpeed.xy * _Time.y;
                o.color = lerp(float4(1,1,1,1), i.color, _VertexColorWeight);
                return o;
            }

            half4 frag(Varyings i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                float4 t = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                float4 c = t * _BaseColor * i.color;
                c.rgb *= _Intensity;
                return c;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
