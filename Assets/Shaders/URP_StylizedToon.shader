Shader "Custom/URP/Stylized Toon"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Bump Scale", Range(0.0, 2.0)) = 1.0

        [Header(Toon Lighting)]
        _ToonSteps("Toon Steps", Range(1.0, 8.0)) = 4.0
        _ShadowThreshold("Shadow Threshold", Range(0.0, 1.0)) = 0.5
        _ShadowSmoothness("Shadow Smoothness", Range(0.001, 0.5)) = 0.08
        _ShadowColor("Shadow Color", Color) = (0.62, 0.7, 0.85, 1.0)

        [Header(Texture Stylization)]
        _TextureFlattenStrength("Texture Flatten Strength", Range(0.0, 1.0)) = 0.45
        _ColorLevels("Color Levels", Range(2.0, 32.0)) = 6.0
        _QuantizeStrength("Quantize Strength", Range(0.0, 1.0)) = 0.55
        _Saturation("Saturation", Range(0.0, 2.0)) = 1.05
        _Contrast("Contrast", Range(0.5, 2.0)) = 1.08

        [Header(Mix Controls)]
        _ToonBlend("Toon Blend", Range(0.0, 1.0)) = 1.0
        _LightInfluence("Light Influence", Range(0.0, 1.0)) = 1.0
        _NormalInfluence("Normal Influence", Range(0.0, 1.0)) = 1.0

        [HideInInspector] _Cull("__Cull", Float) = 2.0
        [HideInInspector] _Cutoff("__Cutoff", Float) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        TEXTURE2D(_BumpMap);
        SAMPLER(sampler_BumpMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _BaseMap_ST;
            float4 _ShadowColor;
            float _BumpScale;
            float _ToonSteps;
            float _ShadowThreshold;
            float _ShadowSmoothness;
            float _TextureFlattenStrength;
            float _ColorLevels;
            float _QuantizeStrength;
            float _Saturation;
            float _Contrast;
            float _ToonBlend;
            float _LightInfluence;
            float _NormalInfluence;
            float _Cutoff;
        CBUFFER_END

        half Luma(half3 color)
        {
            return dot(color, half3(0.299h, 0.587h, 0.114h));
        }

        half3 AdjustSaturation(half3 color, half saturation)
        {
            half lum = Luma(color);
            return lerp(lum.xxx, color, saturation);
        }

        half3 AdjustContrast(half3 color, half contrast)
        {
            return (color - 0.5h) * contrast + 0.5h;
        }

        half3 StylizeTexture(half3 inputColor)
        {
            // 1) Flatten: reduce local color complexity while keeping hue identity.
            half lum = Luma(inputColor);
            half3 flattenedTarget = lum.xxx + (inputColor - lum.xxx) * 0.35h;
            half3 flattened = lerp(inputColor, flattenedTarget, saturate(_TextureFlattenStrength));

            // 2) Quantize: collapse intermediate tones.
            half levels = max(_ColorLevels, 2.0h);
            half invSteps = rcp(levels - 1.0h);
            half3 quantized = floor(flattened * (levels - 1.0h) + 0.5h) * invSteps;
            half3 stylized = lerp(flattened, quantized, saturate(_QuantizeStrength));

            // 3) Final art controls.
            stylized = AdjustSaturation(stylized, _Saturation);
            stylized = AdjustContrast(stylized, _Contrast);
            return saturate(stylized);
        }

        half ComputeToonFactor(half ndotl)
        {
            half smoothness = max(_ShadowSmoothness, 0.0001h);
            half thresholded = smoothstep(_ShadowThreshold - smoothness, _ShadowThreshold + smoothness, ndotl);

            half steps = max(_ToonSteps, 1.0h);
            half stepped = floor(thresholded * steps + 1e-4h) / max(steps - 1.0h, 1.0h);
            return saturate(stepped);
        }

        half3 EvaluateToonLight(half3 stylizedAlbedo, half3 normalWS, Light lightData)
        {
            half ndotl = saturate(dot(normalWS, lightData.direction));
            half toonFactor = ComputeToonFactor(ndotl);

            half3 toonTinted = lerp(stylizedAlbedo * _ShadowColor.rgb, stylizedAlbedo, toonFactor);
            half attenuation = lightData.distanceAttenuation * lightData.shadowAttenuation;

            return toonTinted * lightData.color * attenuation;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardStylizedToon"
            Tags { "LightMode" = "UniversalForward" }

            Blend One Zero
            ZWrite On
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _LIGHT_COOKIES
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

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
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half4 tangentWS : TEXCOORD3;
                half fogFactor : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = NormalizeNormalPerVertex(normalInputs.normalWS);

                real tangentSign = input.tangentOS.w * GetOddNegativeScale();
                output.tangentWS = half4(NormalizeNormalPerVertex(normalInputs.tangentWS), tangentSign);

                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half3 rawBaseColor = baseSample.rgb * _BaseColor.rgb;
                half3 stylizedAlbedo = StylizeTexture(rawBaseColor);

                // Build world normal from geometry + tangent-space normal map.
                half3 normalGeomWS = normalize(input.normalWS);
                half3 tangentWS = normalize(input.tangentWS.xyz);
                half3 bitangentWS = normalize(cross(normalGeomWS, tangentWS) * input.tangentWS.w);

                half4 packedNormal = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv);
                half3 normalTS = UnpackNormalScale(packedNormal, _BumpScale);
                half3 normalMapWS = normalize(TransformTangentToWorld(normalTS, half3x3(tangentWS, bitangentWS, normalGeomWS)));
                half3 normalWS = normalize(lerp(normalGeomWS, normalMapWS, saturate(_NormalInfluence)));

                half4 shadowMask = half4(1.0h, 1.0h, 1.0h, 1.0h);
                Light mainLight;
                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE) || defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                    mainLight = GetMainLight(shadowCoord, input.positionWS, shadowMask);
                #else
                    mainLight = GetMainLight();
                #endif

                half3 directLighting = EvaluateToonLight(stylizedAlbedo, normalWS, mainLight);

                #if defined(_ADDITIONAL_LIGHTS)
                    uint lightCount = GetAdditionalLightsCount();
                    [loop]
                    for (uint lightIndex = 0u; lightIndex < lightCount; ++lightIndex)
                    {
                        Light additionalLight;
                        #if defined(_ADDITIONAL_LIGHT_SHADOWS)
                            additionalLight = GetAdditionalLight(lightIndex, input.positionWS, shadowMask);
                        #else
                            additionalLight = GetAdditionalLight(lightIndex, input.positionWS);
                        #endif
                        directLighting += EvaluateToonLight(stylizedAlbedo, normalWS, additionalLight);
                    }
                #endif

                // Keep some ambient response so shadows stay readable.
                half3 ambientLighting = max(half3(0.0h, 0.0h, 0.0h), SampleSH(normalWS)) * stylizedAlbedo * _ShadowColor.rgb;
                half3 toonLitColor = ambientLighting + directLighting;

                half3 litStylizedMix = lerp(stylizedAlbedo, toonLitColor, saturate(_LightInfluence));
                half3 finalColor = lerp(rawBaseColor, litStylizedMix, saturate(_ToonBlend));

                finalColor = MixFog(finalColor, input.fogFactor);
                return half4(finalColor, baseSample.a * _BaseColor.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthNormalsPass.hlsl"
            ENDHLSL
        }
    }

    Fallback Off
}
