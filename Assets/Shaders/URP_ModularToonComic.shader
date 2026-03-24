Shader "Custom/URP/Modular Toon Comic"
{
    Properties
    {
        [Header(Base Maps)]
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [Toggle(_NORMALMAP)] _UseNormalMap("Use Normal Map", Float) = 1
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Strength", Range(0.0, 2.0)) = 2.0
        _NormalInfluence("Normal Influence", Range(0.0, 1.0)) = 1.0

        [Header(Toon Diffuse)]
        _DiffuseBands("Diffuse Bands", Range(2.0, 8.0)) = 2.0
        _ShadowThreshold("Shadow Threshold", Range(0.0, 1.0)) = 0.343
        _ShadowSmoothness("Shadow Smoothness", Range(0.001, 0.4)) = 0.155
        _ShadowColor("Shadow Tint", Color) = (1, 1, 1, 1)
        _LightInfluence("Light Influence", Range(0.0, 1.0)) = 0.0

        [Header(Ramp Shading)]
        [Toggle(_USE_RAMP)] _UseRamp("Use Ramp", Float) = 0
        [NoScaleOffset] _RampMap("Ramp Map (Horizontal)", 2D) = "gray" {}
        _RampInfluence("Ramp Influence", Range(0.0, 1.0)) = 0.975

        [Header(Specular)]
        [Toggle(_SPECULAR_ON)] _SpecularToggle("Use Specular", Float) = 1
        _SpecularColor("Specular Color", Color) = (1, 1, 1, 1)
        _SpecularSize("Specular Size", Range(0.0, 1.0)) = 0.758
        _SpecularThreshold("Specular Threshold", Range(0.0, 1.0)) = 0.64
        _SpecularSoftness("Specular Softness", Range(0.001, 0.25)) = 0.18
        _SpecularIntensity("Specular Intensity", Range(0.0, 4.0)) = 2.47

        [Header(Rim Light)]
        [Toggle(_RIM_ON)] _RimToggle("Use Rim", Float) = 1
        _RimColor("Rim Color", Color) = (0, 0, 0, 1)
        _RimPower("Rim Power", Range(0.5, 8.0)) = 0.5
        _RimThreshold("Rim Threshold", Range(0.0, 1.0)) = 0.0
        _RimSmoothness("Rim Smoothness", Range(0.001, 0.5)) = 0.225
        _RimIntensity("Rim Intensity", Range(0.0, 4.0)) = 2.29

        [Header(Texture Stylization)]
        [Toggle] _StylizeToggle("Use Texture Stylization", Float) = 1
        _StylizationBlend("Stylization Blend", Range(0.0, 1.0)) = 1.0
        _DetailFlatten("Detail Flatten", Range(0.0, 1.0)) = 1.0
        _PosterizeLevels("Posterize Levels", Range(2.0, 16.0)) = 9.54
        _ColorLevels("Color Levels", Range(2.0, 32.0)) = 32.0
        _QuantizeStrength("Quantize Strength", Range(0.0, 1.0)) = 0.597
        _Saturation("Saturation", Range(0.0, 2.0)) = 1.246
        _Contrast("Contrast", Range(0.5, 2.0)) = 1.142
        _StylizeBrightness("Stylize Brightness", Range(0.5, 2.0)) = 2.0
        _StylizeShadowFloor("Stylize Shadow Floor", Range(0.0, 0.5)) = 0.5
        _DarkPatchColor("Dark Patch Color", Color) = (0.58, 0.36, 0.08, 1)
        _DarkPatchThreshold("Dark Patch Threshold", Range(0.0, 1.0)) = 0.415
        _DarkPatchBlend("Dark Patch Blend", Range(0.0, 1.0)) = 0.542

        [Header(Emission)]
        [Toggle(_EMISSION_ON)] _EmissionToggle("Use Emission", Float) = 0
        [NoScaleOffset] _EmissionMap("Emission Map", 2D) = "black" {}
        [HDR] _EmissionColor("Emission Color", Color) = (0, 0, 0, 1)
        _EmissionIntensity("Emission Intensity", Range(0.0, 8.0)) = 0.0

        [Header(Artist Masks)]
        [Toggle(_MASKMAP_ON)] _MaskToggle("Use Artist Mask Map", Float) = 0
        [NoScaleOffset] _MaskMap("Mask Map (R:Shadow G:Spec B:Rim A:Emission)", 2D) = "white" {}
        _MaskStrength("Mask Strength", Range(0.0, 1.0)) = 1.0

        [Header(Comic Halftone)]
        [Toggle(_HALFTONE_ON)] _HalftoneToggle("Use Halftone", Float) = 1
        _HalftoneColor("Halftone Color", Color) = (0, 0, 0, 1)
        _HalftoneScale("Halftone Scale", Range(8.0, 256.0)) = 102.0
        _HalftoneSoftness("Halftone Softness", Range(0.001, 0.3)) = 0.3
        _HalftoneIntensity("Halftone Intensity", Range(0.0, 1.0)) = 0.869

        [Header(Comic Hatching)]
        [Toggle(_HATCHING_ON)] _HatchingToggle("Use Hatching", Float) = 1
        _HatchColor("Hatch Color", Color) = (0, 0, 0, 1)
        _HatchScale("Hatch Scale", Range(4.0, 256.0)) = 191.0
        _HatchThickness("Hatch Thickness", Range(0.01, 0.49)) = 0.425
        _HatchCross("Cross Hatch Amount", Range(0.0, 1.0)) = 0.991
        _HatchIntensity("Hatch Intensity", Range(0.0, 1.0)) = 0.445

        [Header(Comic Ink Breakup)]
        [Toggle(_INK_BREAKUP_ON)] _InkToggle("Use Ink Breakup", Float) = 1
        _InkScale("Ink Scale", Range(2.0, 128.0)) = 2.0
        _InkThreshold("Ink Threshold", Range(0.0, 1.0)) = 0.0
        _InkSoftness("Ink Softness", Range(0.001, 0.3)) = 0.001
        _InkStrength("Ink Strength", Range(0.0, 1.0)) = 0.288

        [Header(Ambient Gradient)]
        [Toggle(_AMBIENT_GRADIENT_ON)] _AmbientGradientToggle("Use Ambient Gradient", Float) = 1
        _AmbientTopColor("Ambient Top", Color) = (0.78, 0.86, 1.0, 1)
        _AmbientBottomColor("Ambient Bottom", Color) = (0.70, 0.80, 0.98, 1)
        _AmbientGradientOffset("Ambient Gradient Offset", Range(-1.0, 1.0)) = -1.0
        _AmbientIntensity("Ambient Intensity", Range(0.0, 2.0)) = 0.0

        [Header(Outline)]
        [Toggle(_OUTLINE_ON)] _OutlineToggle("Use Outline", Float) = 1
        _OutlineColor("Outline Color", Color) = (0, 0, 0, 1)
        _OutlineWidth("Outline Width (World)", Range(0.0, 0.1)) = 0.1

        [HideInInspector] _Cull("__Cull", Float) = 2.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 300

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        TEXTURE2D(_BumpMap);
        SAMPLER(sampler_BumpMap);
        TEXTURE2D(_RampMap);
        SAMPLER(sampler_RampMap);
        TEXTURE2D(_MaskMap);
        SAMPLER(sampler_MaskMap);
        TEXTURE2D(_EmissionMap);
        SAMPLER(sampler_EmissionMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _BaseMap_ST;

            float4 _ShadowColor;
            float4 _SpecularColor;
            float4 _RimColor;
            float4 _EmissionColor;
            float4 _HalftoneColor;
            float4 _HatchColor;
            float4 _AmbientTopColor;
            float4 _AmbientBottomColor;
            float4 _OutlineColor;
            float4 _DarkPatchColor;

            float _UseNormalMap;
            float _BumpScale;
            float _NormalInfluence;

            float _DiffuseBands;
            float _ShadowThreshold;
            float _ShadowSmoothness;
            float _LightInfluence;

            float _UseRamp;
            float _RampInfluence;

            float _SpecularToggle;
            float _SpecularSize;
            float _SpecularThreshold;
            float _SpecularSoftness;
            float _SpecularIntensity;

            float _RimToggle;
            float _RimPower;
            float _RimThreshold;
            float _RimSmoothness;
            float _RimIntensity;

            float _StylizeToggle;
            float _StylizationBlend;
            float _DetailFlatten;
            float _PosterizeLevels;
            float _ColorLevels;
            float _QuantizeStrength;
            float _Saturation;
            float _Contrast;
            float _StylizeBrightness;
            float _StylizeShadowFloor;
            float _DarkPatchThreshold;
            float _DarkPatchBlend;

            float _EmissionToggle;
            float _EmissionIntensity;

            float _MaskToggle;
            float _MaskStrength;

            float _HalftoneToggle;
            float _HalftoneScale;
            float _HalftoneSoftness;
            float _HalftoneIntensity;

            float _HatchingToggle;
            float _HatchScale;
            float _HatchThickness;
            float _HatchCross;
            float _HatchIntensity;

            float _InkToggle;
            float _InkScale;
            float _InkThreshold;
            float _InkSoftness;
            float _InkStrength;

            float _AmbientGradientToggle;
            float _AmbientGradientOffset;
            float _AmbientIntensity;

            float _OutlineToggle;
            float _OutlineWidth;
        CBUFFER_END

        struct MaskChannels
        {
            half shadow;
            half spec;
            half rim;
            half emission;
        };

        struct LightEvalResult
        {
            half3 color;
            half shadowSignal;
            half attenuation;
        };

        half Luma(half3 c)
        {
            return dot(c, half3(0.299h, 0.587h, 0.114h));
        }

        half3 AdjustSaturation(half3 c, half saturation)
        {
            half lum = Luma(c);
            return lerp(lum.xxx, c, saturation);
        }

        half3 AdjustContrast(half3 c, half contrast)
        {
            return (c - 0.5h) * contrast + 0.5h;
        }

        half3 PosterizeRGB(half3 c, half levels)
        {
            half lev = max(levels, 2.0h);
            return floor(saturate(c) * (lev - 1.0h) + 0.5h) / (lev - 1.0h);
        }

        half Hash12(float2 p)
        {
            float3 p3 = frac(float3(p.xyx) * 0.1031);
            p3 += dot(p3, p3.yzx + 33.33);
            return frac((p3.x + p3.y) * p3.z);
        }

        half3 StylizeTexture(half3 raw)
        {
            half3 src = saturate(raw);

            half3 flattened = lerp(src, Luma(src).xxx, saturate(_DetailFlatten));

            half3 poster = PosterizeRGB(flattened, _PosterizeLevels);
            half3 quant = PosterizeRGB(poster, _ColorLevels);
            half3 stylized = lerp(poster, quant, saturate(_QuantizeStrength));

            stylized = AdjustSaturation(stylized, _Saturation);
            stylized = AdjustContrast(stylized, _Contrast);
            stylized *= max((half)_StylizeBrightness, 0.01h);

            // Keep dark texels readable without leaking unprocessed color back.
            half srcLum = max(Luma(src), 0.001h);
            half floorStrength = saturate(_StylizeShadowFloor * 2.0h);
            half minLum = srcLum * lerp(0.06h, 0.52h, floorStrength);
            half outLum = max(Luma(stylized), 0.001h);
            stylized *= max(1.0h, minLum / outLum);

            // Extra deep-shadow protection: avoids black pinholes on realistic noisy maps.
            half deepMask = 1.0h - smoothstep(0.04h, 0.22h, srcLum);
            half3 deepFloorColor = max(_DarkPatchColor.rgb * 0.28h, half3(0.02h, 0.02h, 0.02h));
            stylized = max(stylized, deepFloorColor * deepMask * floorStrength);

            return saturate(stylized);
        }

        half ComputeSteppedDiffuse(half ndotl, float2 uv)
        {
            half smoothWidth = max((half)_ShadowSmoothness, 0.001h);
            half edge = smoothstep(_ShadowThreshold - smoothWidth, _ShadowThreshold + smoothWidth, ndotl);

            half bands = max((half)_DiffuseBands, 2.0h);
            half stepped = floor(edge * bands + 1e-4h) / (bands - 1.0h);
            stepped = saturate(stepped);

            half useRamp = step(0.5h, (half)_UseRamp);
            if (useRamp > 0.0h)
            {
                half ramp = SAMPLE_TEXTURE2D(_RampMap, sampler_RampMap, float2(stepped, 0.5)).r;
                stepped = lerp(stepped, ramp, saturate(_RampInfluence));
            }

            half useInk = step(0.5h, (half)_InkToggle);
            if (useInk > 0.0h)
            {
                half noise = Hash12(floor(uv * max(_InkScale, 1.0)));
                half breakup = smoothstep(_InkThreshold - _InkSoftness, _InkThreshold + _InkSoftness, noise);
                half shadowAmount = (1.0h - stepped) * lerp(1.0h, breakup, saturate(_InkStrength));
                stepped = 1.0h - saturate(shadowAmount);
            }

            return saturate(stepped);
        }

        half ComputeStylizedSpecular(half3 normalWS, half3 lightDir, half3 viewDir, half specMask, half toonDiffuse)
        {
            half useSpec = step(0.5h, (half)_SpecularToggle);
            if (useSpec <= 0.0h)
            {
                return 0.0h;
            }

            half3 halfDir = SafeNormalize(lightDir + viewDir);
            half ndoth = saturate(dot(normalWS, halfDir));

            half specExponent = lerp(256.0h, 8.0h, saturate(_SpecularSize));
            half raw = pow(ndoth, specExponent);
            half soft = max((half)_SpecularSoftness, 0.001h);
            half hard = smoothstep(_SpecularThreshold, _SpecularThreshold + soft, raw);

            // Reduce bright sparkles inside deepest toon shadow bands.
            half shadowGate = saturate(toonDiffuse + 0.15h);
            return hard * _SpecularIntensity * specMask * shadowGate;
        }

        half ComputeRim(half3 normalWS, half3 viewDir, half rimMask)
        {
            half useRim = step(0.5h, (half)_RimToggle);
            if (useRim <= 0.0h)
            {
                return 0.0h;
            }

            half fresnel = pow(1.0h - saturate(dot(normalWS, viewDir)), _RimPower);
            half soft = max((half)_RimSmoothness, 0.001h);
            half rim = smoothstep(_RimThreshold, _RimThreshold + soft, fresnel);
            return rim * _RimIntensity * rimMask;
        }

        half ComputeHalftone(float4 positionCS, half shadowAmount)
        {
            half useHalf = step(0.5h, (half)_HalftoneToggle);
            if (useHalf <= 0.0h)
            {
                return 0.0h;
            }

            float2 screenUV = positionCS.xy / max(positionCS.w, 1e-5);
            screenUV = screenUV * 0.5 + 0.5;
            float2 pixelUV = screenUV * _ScreenParams.xy / max(_HalftoneScale, 1.0);
            float2 cell = frac(pixelUV) - 0.5;

            half dist = length(cell);
            half radius = lerp(0.08h, 0.48h, saturate(shadowAmount));
            half dotMask = 1.0h - smoothstep(radius, radius + _HalftoneSoftness, dist);
            return dotMask * _HalftoneIntensity * saturate(shadowAmount);
        }

        half ComputeHatching(float2 uv, half shadowAmount)
        {
            half useHatch = step(0.5h, (half)_HatchingToggle);
            if (useHatch <= 0.0h)
            {
                return 0.0h;
            }

            float2 huv = uv * _HatchScale;
            half thickness = saturate(_HatchThickness);
            half lineA = 1.0h - smoothstep(thickness, thickness + 0.05h, abs(frac(huv.x + huv.y) - 0.5h));
            half lineB = 1.0h - smoothstep(thickness, thickness + 0.05h, abs(frac(huv.x - huv.y) - 0.5h));
            half hatch = saturate(lineA + lineB * saturate(_HatchCross));
            return hatch * _HatchIntensity * saturate(shadowAmount);
        }

        MaskChannels SampleMasks(float2 uv)
        {
            MaskChannels m;
            m.shadow = 1.0h;
            m.spec = 1.0h;
            m.rim = 1.0h;
            m.emission = 1.0h;

            half useMask = step(0.5h, (half)_MaskToggle);
            if (useMask > 0.0h)
            {
                half4 ms = SAMPLE_TEXTURE2D(_MaskMap, sampler_MaskMap, uv);
                half strength = saturate(_MaskStrength);
                m.shadow = lerp(1.0h, ms.r, strength);
                m.spec = lerp(1.0h, ms.g, strength);
                m.rim = lerp(1.0h, ms.b, strength);
                m.emission = lerp(1.0h, ms.a, strength);
            }

            return m;
        }

        LightEvalResult EvaluateToonLight(
            Light lightData,
            half3 normalWS,
            half3 viewDir,
            half3 albedo,
            MaskChannels masks,
            float2 uv)
        {
            LightEvalResult r;
            r.color = 0.0h;
            r.shadowSignal = 0.0h;
            r.attenuation = 0.0h;

            half attenuation = saturate(lightData.distanceAttenuation * lightData.shadowAttenuation);
            half ndotl = saturate(dot(normalWS, lightData.direction));
            half toon = ComputeSteppedDiffuse(ndotl, uv);

            // Mask controls how strongly toon shadow affects the object.
            half maskedToon = lerp(1.0h, toon, masks.shadow);
            half3 toonTint = lerp(_ShadowColor.rgb, half3(1.0h, 1.0h, 1.0h), maskedToon);
            half3 diffuseColor = albedo * toonTint * lightData.color * attenuation;

            half specAmount = ComputeStylizedSpecular(normalWS, lightData.direction, viewDir, masks.spec, maskedToon) * attenuation;
            half3 specColor = _SpecularColor.rgb * lightData.color * specAmount;

            r.color = diffuseColor + specColor;
            r.shadowSignal = (1.0h - maskedToon) * attenuation;
            r.attenuation = attenuation;
            return r;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardToonComic"
            Tags { "LightMode" = "UniversalForward" }

            Blend One Zero
            ZWrite On
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _USE_RAMP
            #pragma shader_feature_local_fragment _SPECULAR_ON
            #pragma shader_feature_local_fragment _RIM_ON
            #pragma shader_feature_local_fragment _EMISSION_ON
            #pragma shader_feature_local_fragment _MASKMAP_ON
            #pragma shader_feature_local_fragment _HALFTONE_ON
            #pragma shader_feature_local_fragment _HATCHING_ON
            #pragma shader_feature_local_fragment _INK_BREAKUP_ON
            #pragma shader_feature_local_fragment _AMBIENT_GRADIENT_ON
            #pragma shader_feature_local_fragment _OUTLINE_ON

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

                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs nrm = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                output.positionCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                output.normalWS = NormalizeNormalPerVertex(nrm.normalWS);

                real tangentSign = input.tangentOS.w * GetOddNegativeScale();
                output.tangentWS = half4(NormalizeNormalPerVertex(nrm.tangentWS), tangentSign);

                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(pos.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half3 baseColor = baseSample.rgb * _BaseColor.rgb;

                half3 stylizedOnly = StylizeTexture(baseColor);
                half stylizeMask = step(0.5h, (half)_StylizeToggle) * saturate(_StylizationBlend);
                half3 albedo = lerp(baseColor, stylizedOnly, stylizeMask);

                MaskChannels masks = SampleMasks(input.uv);

                half3 normalWS = normalize(input.normalWS);
                half useNormal = step(0.5h, (half)_UseNormalMap) * saturate(_NormalInfluence);
                if (useNormal > 0.0h)
                {
                    half3 tangentWS = normalize(input.tangentWS.xyz);
                    half tangentLenSq = dot(tangentWS, tangentWS);
                    if (tangentLenSq > 1e-5h)
                    {
                        half3 bitangentWS = normalize(cross(normalWS, tangentWS) * input.tangentWS.w);
                        half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv), _BumpScale);
                        half3 normalFromMap = normalize(TransformTangentToWorld(normalTS, half3x3(tangentWS, bitangentWS, normalWS)));
                        normalWS = normalize(lerp(normalWS, normalFromMap, useNormal));
                    }
                }

                half3 viewDirWS = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
                half4 shadowMask = half4(1.0h, 1.0h, 1.0h, 1.0h);

                Light mainLight;
                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE) || defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                    mainLight = GetMainLight(shadowCoord, input.positionWS, shadowMask);
                #else
                    mainLight = GetMainLight();
                #endif

                LightEvalResult mainEval = EvaluateToonLight(mainLight, normalWS, viewDirWS, albedo, masks, input.uv);
                half3 directColor = mainEval.color;
                half shadowAccum = mainEval.shadowSignal;
                half lightWeight = mainEval.attenuation;

                #if defined(_ADDITIONAL_LIGHTS)
                    uint additionalCount = GetAdditionalLightsCount();
                    [loop]
                    for (uint lightIndex = 0u; lightIndex < additionalCount; ++lightIndex)
                    {
                        Light addLight;
                        #if defined(_ADDITIONAL_LIGHT_SHADOWS)
                            addLight = GetAdditionalLight(lightIndex, input.positionWS, shadowMask);
                        #else
                            addLight = GetAdditionalLight(lightIndex, input.positionWS);
                        #endif

                        LightEvalResult addEval = EvaluateToonLight(addLight, normalWS, viewDirWS, albedo, masks, input.uv);
                        directColor += addEval.color;
                        shadowAccum += addEval.shadowSignal;
                        lightWeight += addEval.attenuation;
                    }
                #endif

                half shadowAmount = saturate(shadowAccum / max(lightWeight, 1e-4h));

                half3 shAmbient = max(SampleSH(normalWS), 0.0h);
                half gradT = saturate(normalWS.y * 0.5h + 0.5h + _AmbientGradientOffset);
                half3 gradAmbient = lerp(_AmbientBottomColor.rgb, _AmbientTopColor.rgb, gradT);
                half useAmbientGrad = step(0.5h, (half)_AmbientGradientToggle);
                half3 ambientLight = lerp(shAmbient, gradAmbient, useAmbientGrad) * _AmbientIntensity;
                half3 ambientColor = albedo * ambientLight;

                // Scene-safe floor so Light Influence never collapses model into near-black.
                half ambientFloor = 0.16h + saturate(_StylizeShadowFloor * 0.8h);
                ambientColor = max(ambientColor, albedo * ambientFloor);

                half3 litColor = ambientColor + directColor;

                half rimAmount = ComputeRim(normalWS, viewDirWS, masks.rim);
                litColor += _RimColor.rgb * rimAmount;

                half useEmission = step(0.5h, (half)_EmissionToggle);
                if (useEmission > 0.0h)
                {
                    half3 emissionTex = SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, input.uv).rgb;
                    litColor += emissionTex * _EmissionColor.rgb * _EmissionIntensity * masks.emission;
                }

                half halftone = ComputeHalftone(input.positionCS, shadowAmount);
                litColor = lerp(litColor, litColor * _HalftoneColor.rgb, halftone);

                half hatch = ComputeHatching(input.uv, shadowAmount);
                litColor = lerp(litColor, litColor * _HatchColor.rgb, hatch);

                half influence = saturate(_LightInfluence);
                half3 litSafe = max(litColor, albedo * 0.24h);
                half3 finalColor = lerp(albedo, litSafe, influence);
                finalColor = max(finalColor, albedo * 0.04h);

                // User tint for darkest fragments to avoid pure-black patches.
                half finalLum = Luma(saturate(finalColor));
                half darkMask = 1.0h - smoothstep(_DarkPatchThreshold, _DarkPatchThreshold + 0.16h, finalLum);
                half darkBlend = saturate(_DarkPatchBlend) * darkMask;
                // Blend toward user-defined tint directly (not multiplied by near-black albedo).
                half3 darkTinted = lerp(finalColor, _DarkPatchColor.rgb, 0.85h);
                finalColor = lerp(finalColor, darkTinted, darkBlend);

                finalColor = MixFog(finalColor, input.fogFactor);

                return half4(max(finalColor, 0.0h), baseSample.a * _BaseColor.a);
            }
            ENDHLSL
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Front
            ZWrite Off
            ZTest LEqual
            Blend One Zero

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex OutlineVert
            #pragma fragment OutlineFrag
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings OutlineVert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                half useOutline = step(0.5h, (half)_OutlineToggle);
                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 nrmWS = normalize(TransformObjectToWorldNormal(input.normalOS));
                posWS += nrmWS * (_OutlineWidth * useOutline);
                o.positionCS = TransformWorldToHClip(posWS);
                return o;
            }

            half4 OutlineFrag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                if (_OutlineToggle < 0.5)
                {
                    clip(-1.0);
                }

                return half4(_OutlineColor.rgb, 1.0h);
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
            #pragma shader_feature_local _NORMALMAP
            #pragma multi_compile_instancing
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #pragma multi_compile _ LOD_FADE_CROSSFADE
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthNormalsPass.hlsl"
            ENDHLSL
        }
    }

    Fallback Off
}