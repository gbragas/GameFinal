Shader "Custom/MirrorURP"
{
    Properties
    {
        _MainTex ("Emissive Texture", 2D) = "black" {}
        _DetailTex ("Detail Texture", 2D) = "white" {}
        _Color ("Detail Tint Color", Color) = (1,1,1,1)
        _ReflectionColor ("Reflection Tint Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            sampler2D _DetailTex;
            float4 _Color;
            float4 _ReflectionColor;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.screenPos = ComputeScreenPos(output.positionCS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float4 screenUV = input.screenPos / input.screenPos.w;
                half4 detail = tex2D(_DetailTex, input.uv);
                half4 refl = tex2D(_MainTex, screenUV.xy);

                // No URP simplificado, somamos o detalhe com a reflexão
                half3 finalColor = (detail.rgb * _Color.rgb) + (refl.rgb * _ReflectionColor.rgb);
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}