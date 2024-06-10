Shader "Custom/BRG"
{
    Properties
    {
        _SpriteSheet ("Sprite Sheet", 2D) = "white" {}
        _Index ("Index", int) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline" "Queue"="Transparent"
        }

        Pass
        {
            Name "Forward"
            Tags
            {
                "LightMode"="UniversalForward"
            }

            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma exclude_renderers gles gles3 glcore
            #pragma target 4.5
            #pragma vertex UnlitPassVertex
            #pragma fragment UnlitPassFragment
            #pragma multi_compile_instancing
            #pragma instancing_options renderinglayer
            #pragma multi_compile _ DOTS_INSTANCING_ON
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            CBUFFER_START(UnityPerMaterial)
            float4 _SpriteSheet_ST;
            int _Index;
            CBUFFER_END

            TEXTURE2D(_SpriteSheet);
            SAMPLER(sampler_SpriteSheet);

            #ifdef UNITY_DOTS_INSTANCING_ENABLED
                UNITY_DOTS_INSTANCING_START(MaterialPropertyMetadata)
                    UNITY_DOTS_INSTANCED_PROP(int, _Index)
                UNITY_DOTS_INSTANCING_END(MaterialPropertyMetadata)
                #define _Index UNITY_ACCESS_DOTS_INSTANCED_PROP_WITH_DEFAULT(int, _Index)
            #endif

            Varyings UnlitPassVertex(Attributes input)
            {
                Varyings output;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                const VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;

                int width = 36;
                int x = _Index % width;
                int y = _Index / width;

                output.uv = TRANSFORM_TEX(input.uv * float2(0.0277777777777778, 0.027027027027027) + float2(0.0277777777777778 * x, 0.027027027027027 * y), _SpriteSheet);
                return output;
            }

            float4 UnlitPassFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                float4 color = SAMPLE_TEXTURE2D(_SpriteSheet, sampler_SpriteSheet, input.uv);
                return float4(color.xyz, color.w * color.w);
            }
            ENDHLSL
        }
    }
}
