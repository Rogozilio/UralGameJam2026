Shader "Custom/URP/BlackMaskBaseLitDissolve"
{
    Properties
    {
        [Header(CORE CONTROLS)]
        _MaskProgress("MASK_PROGRESS", Range(0, 1)) = 0
        _MaskSoftness("MASK_SOFTNESS", Range(0.001, 0.3)) = 0.08

        _DissolveProgress("DISSOLVE_PROGRESS", Range(0, 1)) = 0
        _DissolveStart("DISSOLVE_START", Range(-1.0, 1.0)) = -0.08
        _DissolveEnd("DISSOLVE_END", Range(0.0, 2.0)) = 1.05
        _EdgeWidth("EDGE_WIDTH", Range(0.001, 0.3)) = 0.05

        _HeightMin("HEIGHT_MIN", Float) = -0.5
        _HeightMax("HEIGHT_MAX", Float) = 0.5
        _DirectionVector("DIRECTION_VECTOR", Vector) = (0, -1, 0, 0)

        _NoiseScale("NOISE_SCALE", Range(0.1, 20.0)) = 3.0
        _MaskNoiseStrength("MASK_NOISE_STRENGTH", Range(0.0, 1.0)) = 0.2
        _DissolveNoiseStrength("DISSOLVE_NOISE_STRENGTH", Range(0.0, 1.0)) = 0.2

        [Header(CORE LOOK CONTROLS)]
        _MaskBlackTint("MASK_BLACK_TINT", Color) = (0, 0, 0, 1)
        _MaskBlackStrength("MASK_BLACK_STRENGTH", Range(0, 1)) = 1
        [HDR] _EdgeColor("EDGE_COLOR", Color) = (2.5, 1.2, 0.2, 1)
        _EdgeEmission("EDGE_EMISSION", Range(0, 10)) = 2.0
        _DissolveColorInfluence("DISSOLVE_COLOR_INFLUENCE", Range(0, 1)) = 1.0

        [Header(Space)]
        [KeywordEnum(ObjectSpace, WorldSpace)] _HeightSpace("Height Space", Float) = 0

        [Header(Base)]
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
        _NoiseMap("Noise Map", 2D) = "gray" {}

        [Header(Normals)]
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Strength", Range(0, 2)) = 1

        [Header(Alpha Clip)]
        [Toggle(_ALPHATEST_ON)] _AlphaClip("Use Base Alpha Clip", Float) = 0
        _Cutoff("Base Alpha Cutoff", Range(0, 1)) = 0.5

        [HideInInspector] _Cull("__Cull", Float) = 2
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
        TEXTURE2D(_NoiseMap);
        SAMPLER(sampler_NoiseMap);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _BaseMap_ST;
            float4 _DirectionVector;

            float4 _MaskBlackTint;
            float4 _EdgeColor;

            float _MaskProgress;
            float _MaskSoftness;

            float _DissolveProgress;
            float _DissolveStart;
            float _DissolveEnd;
            float _EdgeWidth;

            float _HeightMin;
            float _HeightMax;
            float _NoiseScale;
            float _MaskNoiseStrength;
            float _DissolveNoiseStrength;

            float _MaskBlackStrength;
            float _EdgeEmission;
            float _DissolveColorInfluence;

            float _BumpScale;
            float _Cutoff;
        CBUFFER_END

        struct EffectData
        {
            float dissolveClip;
            float blackFactor;
            float edge;
            float noise;
        };

        float3 GetSafeDirectionRef()
        {
            float3 dir = _DirectionVector.xyz;
            if (dot(dir, dir) < 1e-5)
            {
                dir = float3(0, -1, 0);
            }
            return normalize(dir);
        }

        float3 GetRefPos(float3 posOS, float3 posWS)
        {
            #if defined(_HEIGHTSPACE_WORLDSPACE)
                return posWS;
            #else
                return posOS;
            #endif
        }

        float3 GetRefNormal(float3 normalOS, float3 normalWS)
        {
            #if defined(_HEIGHTSPACE_WORLDSPACE)
                return SafeNormalize(normalWS);
            #else
                return SafeNormalize(normalOS);
            #endif
        }

        float SampleNoise(float3 refPos, float3 refNormal)
        {
            float3 p = refPos * _NoiseScale;
            float3 w = abs(refNormal);
            w = pow(w, 3.0);
            w /= max(w.x + w.y + w.z, 1e-5);

            float nXY = SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, p.xy).r;
            float nXZ = SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, p.xz).r;
            float nYZ = SAMPLE_TEXTURE2D(_NoiseMap, sampler_NoiseMap, p.yz).r;
            return nXY * w.z + nXZ * w.y + nYZ * w.x;
        }

        float ComputeTopDownMask(float3 refPos, float3 dirRef)
        {
            float projected = dot(refPos, -dirRef);
            float range = max(abs(_HeightMax - _HeightMin), 1e-5);
            float h01 = saturate((projected - _HeightMin) / range);
            return 1.0 - h01;
        }

        EffectData EvaluateEffect(float3 posOS, float3 posWS, float3 normalOS, float3 normalWS)
        {
            EffectData o;

            float3 refPos = GetRefPos(posOS, posWS);
            float3 refNormal = GetRefNormal(normalOS, normalWS);
            float3 dirRef = GetSafeDirectionRef();

            float noise = SampleNoise(refPos, refNormal);
            float baseMask = ComputeTopDownMask(refPos, dirRef);

            float blackMask = saturate(baseMask + (noise * 2.0 - 1.0) * _MaskNoiseStrength);
            float dissolveMask = saturate(baseMask + (noise * 2.0 - 1.0) * _DissolveNoiseStrength);

            float dissolveThreshold = lerp(_DissolveStart, _DissolveEnd, saturate(_DissolveProgress));

            o.dissolveClip = dissolveMask - dissolveThreshold;
            o.blackFactor = saturate((saturate(_MaskProgress) - blackMask) / max(_MaskSoftness, 1e-5));
            o.edge = (1.0 - smoothstep(0.0, max(_EdgeWidth, 1e-5), o.dissolveClip)) * step(0.0, o.dissolveClip);
            o.noise = noise;
            return o;
        }

        half3 EvalLitLight(half3 albedo, half3 normalWS, Light lightData)
        {
            half ndotl = saturate(dot(normalWS, lightData.direction));
            half atten = lightData.distanceAttenuation * lightData.shadowAttenuation;
            return albedo * lightData.color * (ndotl * atten);
        }
        ENDHLSL

        Pass
        {
            Name "ForwardBlackMaskBaseLitDissolve"
            Tags { "LightMode" = "UniversalForward" }

            Blend One Zero
            ZWrite On
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local _HEIGHTSPACE_OBJECTSPACE _HEIGHTSPACE_WORLDSPACE
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
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
                float3 posOS : TEXCOORD1;
                float3 posWS : TEXCOORD2;
                half3 normalOS : TEXCOORD3;
                half3 normalWS : TEXCOORD4;
                half4 tangentWS : TEXCOORD5;
                half fogFactor : TEXCOORD6;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs n = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                o.positionCS = p.positionCS;
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                o.posOS = input.positionOS.xyz;
                o.posWS = p.positionWS;
                o.normalOS = SafeNormalize(input.normalOS);
                o.normalWS = NormalizeNormalPerVertex(n.normalWS);

                real sign = input.tangentOS.w * GetOddNegativeScale();
                o.tangentWS = half4(NormalizeNormalPerVertex(n.tangentWS), sign);
                o.fogFactor = ComputeFogFactor(o.positionCS.z);
                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                EffectData fx = EvaluateEffect(input.posOS, input.posWS, input.normalOS, input.normalWS);
                clip(fx.dissolveClip);

                half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half alpha = baseSample.a * _BaseColor.a;
                #if defined(_ALPHATEST_ON)
                    clip(alpha - _Cutoff);
                #endif

                half3 albedo = baseSample.rgb * _BaseColor.rgb;
                half blackAmount = saturate(fx.blackFactor * _MaskBlackStrength);
                half3 blackenedAlbedo = lerp(albedo, _MaskBlackTint.rgb, blackAmount);

                half3 nGeom = SafeNormalize(input.normalWS);
                half3 t = SafeNormalize(input.tangentWS.xyz);
                half3 b = SafeNormalize(cross(nGeom, t) * input.tangentWS.w);
                half3 nTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv), _BumpScale);
                half3 nWS = SafeNormalize(TransformTangentToWorld(nTS, half3x3(t, b, nGeom)));
                nWS = (dot(nWS, nWS) > 1e-4h) ? nWS : nGeom;

                half4 shadowMask = half4(1, 1, 1, 1);
                InputData inputData = (InputData)0;
                inputData.positionWS = input.posWS;
                inputData.normalWS = nWS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

                Light mainLight;
                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE) || defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    mainLight = GetMainLight(TransformWorldToShadowCoord(input.posWS), input.posWS, shadowMask);
                #else
                    mainLight = GetMainLight();
                #endif

                half3 lit = EvalLitLight(blackenedAlbedo, nWS, mainLight);

                #if defined(_ADDITIONAL_LIGHTS)
                    uint count = GetAdditionalLightsCount();
                    LIGHT_LOOP_BEGIN(count)
                        Light add;
                        #if defined(_ADDITIONAL_LIGHT_SHADOWS)
                            add = GetAdditionalLight(lightIndex, input.posWS, shadowMask);
                        #else
                            add = GetAdditionalLight(lightIndex, input.posWS);
                        #endif
                        lit += EvalLitLight(blackenedAlbedo, nWS, add);
                    LIGHT_LOOP_END
                #endif

                lit += blackenedAlbedo * max(half3(0, 0, 0), SampleSH(nWS));

                half edgeNoise = saturate(0.85h + (fx.noise * 2.0h - 1.0h) * 0.15h);
                half edgeMask = saturate(fx.edge * edgeNoise);
                edgeMask = edgeMask * edgeMask;
                half3 edgeEmissive = _EdgeColor.rgb * (edgeMask * _EdgeEmission * _DissolveColorInfluence);

                half3 finalColor = lit + edgeEmissive;
                finalColor = MixFog(finalColor, input.fogFactor);
                return half4(finalColor, 1.0h);
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
            #pragma target 3.0
            #pragma vertex vertShadow
            #pragma fragment fragShadow
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local _HEIGHTSPACE_OBJECTSPACE _HEIGHTSPACE_WORLDSPACE
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 posOS : TEXCOORD1;
                float3 posWS : TEXCOORD2;
                float3 normalOS : TEXCOORD3;
                float3 normalWS : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vertShadow(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(posWS);
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                o.posOS = input.positionOS.xyz;
                o.posWS = posWS;
                o.normalOS = SafeNormalize(input.normalOS);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return o;
            }

            half4 fragShadow(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                EffectData fx = EvaluateEffect(input.posOS, input.posWS, input.normalOS, input.normalWS);
                clip(fx.dissolveClip);

                #if defined(_ALPHATEST_ON)
                    half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * _BaseColor.a;
                    clip(alpha - _Cutoff);
                #endif
                return 0;
            }
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
            #pragma target 3.0
            #pragma vertex vertDepth
            #pragma fragment fragDepth
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local _HEIGHTSPACE_OBJECTSPACE _HEIGHTSPACE_WORLDSPACE
            #pragma multi_compile_instancing

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 posOS : TEXCOORD1;
                float3 posWS : TEXCOORD2;
                float3 normalOS : TEXCOORD3;
                float3 normalWS : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vertDepth(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(posWS);
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                o.posOS = input.positionOS.xyz;
                o.posWS = posWS;
                o.normalOS = SafeNormalize(input.normalOS);
                o.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return o;
            }

            half4 fragDepth(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                EffectData fx = EvaluateEffect(input.posOS, input.posWS, input.normalOS, input.normalWS);
                clip(fx.dissolveClip);

                #if defined(_ALPHATEST_ON)
                    half alpha = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).a * _BaseColor.a;
                    clip(alpha - _Cutoff);
                #endif
                return 0;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
