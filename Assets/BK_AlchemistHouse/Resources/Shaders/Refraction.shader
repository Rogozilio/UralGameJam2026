Shader "Custom/Refraction"
{
    Properties
    {
        [MainTexture] _Albedo("Albedo", 2D) = "white" {}
        [Normal] _NormalMap("Normal Map", 2D) = "bump" {}
        _Opacity("Opacity", Range(0, 1)) = 0.35
        _Smoothness("Smoothness", Range(0, 1)) = 0.85
        _Metalness("Metalness", Range(0, 1)) = 0.0
        _NormalStrength("Normal Strength", Range(0, 2)) = 1.0
        _RefractionStrength("Refraction Strength", Range(0, 0.2)) = 0.05
        [Header(Refraction)]
        _IndexofRefraction("Index of Refraction", Range(-1, 1)) = 0.15
        _ChromaticAberration("Chromatic Aberration", Range(0, 0.3)) = 0.02
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            TEXTURE2D(_Albedo);
            SAMPLER(sampler_Albedo);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            CBUFFER_START(UnityPerMaterial)
            float4 _Albedo_ST;
            float4 _NormalMap_ST;
            float _Opacity;
            float _Smoothness;
            float _Metalness;
            float _NormalStrength;
            float _RefractionStrength;
            float _IndexofRefraction;
            float _ChromaticAberration;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                half3 normalWS : TEXCOORD3;
                half4 tangentWS : TEXCOORD4;
                half fogFactor : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionHCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.uv = input.uv;
                output.screenPos = ComputeScreenPos(output.positionHCS);
                output.normalWS = normalInputs.normalWS;
                output.tangentWS = half4(normalInputs.tangentWS, input.tangentOS.w * GetOddNegativeScale());
                output.fogFactor = ComputeFogFactor(output.positionHCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = TRANSFORM_TEX(input.uv, _Albedo);
                half4 albedo = SAMPLE_TEXTURE2D(_Albedo, sampler_Albedo, uv);

                float2 normalUv = TRANSFORM_TEX(input.uv, _NormalMap);
                half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, normalUv), _NormalStrength);

                half3 normalWSBase = normalize(input.normalWS);
                half3 tangentWS = normalize(input.tangentWS.xyz);
                half tangentSign = input.tangentWS.w;
                half3 bitangentWS = normalize(cross(normalWSBase, tangentWS) * tangentSign);
                half3x3 tbn = half3x3(tangentWS, bitangentWS, normalWSBase);
                half3 normalWS = normalize(TransformTangentToWorld(normalTS, tbn));

                float3 viewDirWS = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
                float3 normalVS = TransformWorldToViewDir(normalWS, true);
                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), 5.0);

                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                screenUV = UnityStereoTransformScreenSpaceTex(screenUV);

                float2 distortion = normalVS.xy * _IndexofRefraction * _RefractionStrength * (0.35 + fresnel);
                float2 uvR = saturate(screenUV + distortion * (1.0 + _ChromaticAberration));
                float2 uvG = saturate(screenUV + distortion);
                float2 uvB = saturate(screenUV + distortion * (1.0 - _ChromaticAberration));

                half sceneR = SampleSceneColor(uvR).r;
                half sceneG = SampleSceneColor(uvG).g;
                half sceneB = SampleSceneColor(uvB).b;
                half3 refracted = half3(sceneR, sceneG, sceneB);

                half tintAmount = saturate(albedo.a * _Opacity);
                half3 tint = lerp(half3(1.0, 1.0, 1.0), albedo.rgb, tintAmount);

                half3 specColor = lerp(half3(0.04, 0.04, 0.04), albedo.rgb, saturate(_Metalness));
                half3 finalColor = refracted * tint + specColor * (_Smoothness * fresnel);
                finalColor = MixFog(finalColor, input.fogFactor);

                // Refraction already samples the background, so alpha stays fully opaque.
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}
