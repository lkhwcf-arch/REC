Shader "REC/InteractionHighlight"
{
    Properties
    {
        _Color ("Color", Color) = (1, 1, 1, 1)
        _Expansion ("Expansion", Float) = 0
        _MeshCenter ("Mesh Center", Vector) = (0, 0, 0, 0)
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _StencilPass ("Stencil Pass", Float) = 2
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
        }

        Pass
        {
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back

            Stencil
            {
                Ref 1
                ReadMask 1
                WriteMask [_StencilWriteMask]
                Comp [_StencilComp]
                Pass [_StencilPass]
            }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _MeshCenter;
                float _Expansion;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                float3 center = _MeshCenter.xyz;
                float3 position = center + (input.positionOS.xyz - center) * (1.0 + _Expansion);
                output.positionCS = TransformObjectToHClip(position);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                return _Color;
            }
            ENDHLSL
        }
    }
}