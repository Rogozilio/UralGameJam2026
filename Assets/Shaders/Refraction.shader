Shader "Custom/Refraction"
{
    Properties
    {
        [MainTexture] _MainTex("Albedo (Legacy)", 2D) = "white" {}
        _Albedo("Albedo", 2D) = "white" {}
        [Normal] _NormalMap("Normal Map", 2D) = "bump" {}
        [Normal] _BumpMap("Normal Map (Legacy)", 2D) = "bump" {}
        _Color("Color (Legacy)", Color) = (1, 1, 1, 1)
        _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        _Opacity("Opacity", Range(0, 1)) = 0.35
        _Smoothness("Smoothness", Range(0, 1)) = 0.85
        _Metalness("Metalness", Range(0, 1)) = 0.0
        _NormalStrength("Normal Strength", Range(0, 2)) = 1.0
        _RefractionStrength("Refraction Strength", Range(-0.2, 0.2)) = 0.0
        [HideInInspector] _Refraction("Refraction (Legacy)", Range(-1, 1)) = 0.05
        [HideInInspector] _RefractionExponent("Refraction Exponent (Legacy)", Range(-1, 1)) = 0.0
        [HideInInspector] _RefractionIndex("Refraction Index (Legacy)", Range(-1, 1)) = 0.0
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

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_Albedo);
            SAMPLER(sampler_Albedo);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            float4 _Albedo_ST;
            float4 _NormalMap_ST;
            float4 _BumpMap_ST;
            float4 _Color;
            float4 _BaseColor;
            float _Opacity;
            float _Smoothness;
            float _Metalness;
            float _NormalStrength;
            float _RefractionStrength;
            float _Refraction;
            float _RefractionExponent;
            float _RefractionIndex;
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

            half4 SampleAlbedo(float2 uv)
            {
                const half4 legacyMain = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, TRANSFORM_TEX(uv, _MainTex));
                const half4 modernAlbedo = SAMPLE_TEXTURE2D(_Albedo, sampler_Albedo, TRANSFORM_TEX(uv, _Albedo));

                const half legacyLooksDefault = step(2.999h, legacyMain.r + legacyMain.g + legacyMain.b) * step(0.999h, legacyMain.a);
                const half modernLooksDefault = step(2.999h, modernAlbedo.r + modernAlbedo.g + modernAlbedo.b) * step(0.999h, modernAlbedo.a);
                const half useModernFallback = legacyLooksDefault * (1.0h - modernLooksDefault);

                const half4 sampled = lerp(legacyMain, modernAlbedo, useModernFallback);
                return sampled * (_BaseColor * _Color);
            }

            half3 SampleNormalTS(float2 uv)
            {
                const half3 modernNormal = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, TRANSFORM_TEX(uv, _NormalMap)),
                    _NormalStrength
                );

                const half3 legacyNormal = UnpackNormalScale(
                    SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, TRANSFORM_TEX(uv, _BumpMap)),
                    _NormalStrength
                );

                const half modernDeviation = length(modernNormal.xy);
                const half legacyDeviation = length(legacyNormal.xy);
                const half useLegacyFallback = step(modernDeviation, 0.001h) * step(0.001h, legacyDeviation);

                return normalize(lerp(modernNormal, legacyNormal, useLegacyFallback));
            }

            half ResolveRefractionStrength()
            {
                // Keep old materials functional: if modern property is not set, use legacy _Refraction.
                return (abs(_RefractionStrength) > 0.0001h) ? _RefractionStrength : _Refraction;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                const half4 albedo = SampleAlbedo(input.uv);
                const half3 normalTS = SampleNormalTS(input.uv);

                const half3 normalWSBase = normalize(input.normalWS);
                const half3 tangentWS = normalize(input.tangentWS.xyz);
                const half tangentSign = input.tangentWS.w;
                const half3 bitangentWS = normalize(cross(normalWSBase, tangentWS) * tangentSign);
                const half3x3 tbn = half3x3(tangentWS, bitangentWS, normalWSBase);
                const half3 normalWS = normalize(TransformTangentToWorld(normalTS, tbn));

                const float3 viewDirWS = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
                const float3 normalVS = TransformWorldToViewDir(normalWS, true);

                const half fresnelPower = max(1.0h, 5.0h + (_RefractionExponent * 8.0h));
                const half fresnel = pow(1.0h - saturate(dot(normalWS, viewDirWS)), fresnelPower);

                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                screenUV = UnityStereoTransformScreenSpaceTex(screenUV);

                const half refractionStrength = ResolveRefractionStrength();
                const half ior = _IndexofRefraction + (_RefractionIndex * 0.15h);
                const float2 distortion = normalVS.xy * ior * refractionStrength * (0.35h + fresnel);

                const float2 uvR = saturate(screenUV + distortion * (1.0h + _ChromaticAberration));
                const float2 uvG = saturate(screenUV + distortion);
                const float2 uvB = saturate(screenUV + distortion * (1.0h - _ChromaticAberration));

                const half sceneR = SampleSceneColor(uvR).r;
                const half sceneG = SampleSceneColor(uvG).g;
                const half sceneB = SampleSceneColor(uvB).b;
                const half3 refracted = half3(sceneR, sceneG, sceneB);

                const half tintAmount = saturate(albedo.a * _Opacity);
                const half3 tint = lerp(half3(1.0h, 1.0h, 1.0h), albedo.rgb, tintAmount);

                const half3 specColor = lerp(half3(0.04h, 0.04h, 0.04h), albedo.rgb, saturate(_Metalness));
                half3 finalColor = refracted * tint + specColor * (_Smoothness * fresnel);
                finalColor = MixFog(finalColor, input.fogFactor);

                // Refraction is screen-color based, so we keep opaque alpha for stable blending.
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}
