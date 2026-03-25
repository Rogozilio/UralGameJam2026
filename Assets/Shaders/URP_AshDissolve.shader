Shader "Custom/URP/Ash Dissolve"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1,1,1,1)
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Strength", Range(0.0, 2.0)) = 1.0

        [Header(Dissolve)]
        [KeywordEnum(ObjectSpace, WorldSpace)] _HeightSpace("Height Space", Float) = 0
        _DissolveProgress("Dissolve Progress", Range(0, 1)) = 0
        _DirectionVector("Direction Vector", Vector) = (0, -1, 0, 0)
        _HeightMin("Height Min", Float) = 0.0
        _HeightMax("Height Max", Float) = 1.0
        _ClipThreshold("Clip Threshold", Range(-0.2, 0.2)) = 0.0

        [Header(Noise)]
        _NoiseTex("Noise Tex", 2D) = "gray" {}
        _NoiseScale("Noise Scale", Range(0.1, 20.0)) = 3.0
        _NoiseStrength("Noise Strength", Range(0.0, 1.0)) = 0.25

        [Header(Edge)]
        _EdgeWidth("Edge Width", Range(0.001, 0.3)) = 0.06
        [HDR] _EdgeColor("Edge Color", Color) = (2.8, 1.35, 0.2, 1.0)
        _EdgeIntensity("Edge Intensity", Range(0.0, 8.0)) = 2.0
        _EdgeStyle("Edge Style (0 Ash, 1 Hot)", Range(0.0, 1.0)) = 0.0

        [Header(Ash)]
        _BurnDarkening("Burn Darkening", Range(0.0, 1.0)) = 0.65
        _AshTint("Ash Tint", Color) = (0.23, 0.22, 0.2, 1.0)

        [Header(Vertex Crumble)]
        _DustAmount("Dust Amount", Range(0.0, 2.0)) = 0.35
        _ParticleAmount("Particle Amount", Range(0.0, 0.2)) = 0.005
        _VertexFalloff("Vertex Falloff", Range(0.01, 0.5)) = 0.14
        _VertexJitterSpeed("Vertex Jitter Speed", Range(0.0, 20.0)) = 7.0

        [Header(Lighting)]
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.35
        _SpecularStrength("Specular Strength", Range(0.0, 2.0)) = 0.35

        [Header(Alpha Clip)]
        [Toggle(_ALPHATEST_ON)] _AlphaClip("Use Base Alpha Clip", Float) = 0
        _Cutoff("Base Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        [HideInInspector] _Cull("__Cull", Float) = 2.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 250

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);
        TEXTURE2D(_BumpMap);
        SAMPLER(sampler_BumpMap);
        TEXTURE2D(_NoiseTex);
        SAMPLER(sampler_NoiseTex);

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float4 _BaseMap_ST;
            float4 _EdgeColor;
            float4 _AshTint;
            float4 _DirectionVector;
            float _BumpScale;
            float _DissolveProgress;
            float _HeightMin;
            float _HeightMax;
            float _ClipThreshold;
            float _NoiseScale;
            float _NoiseStrength;
            float _EdgeWidth;
            float _EdgeIntensity;
            float _EdgeStyle;
            float _BurnDarkening;
            float _DustAmount;
            float _ParticleAmount;
            float _VertexFalloff;
            float _VertexJitterSpeed;
            float _Smoothness;
            float _SpecularStrength;
            float _Cutoff;
        CBUFFER_END

        struct DissolveData
        {
            float dissolveMask;
            float threshold;
            float clipValue;
            float edge;
            float noise;
        };

        float Hash31(float3 p)
        {
            p = frac(p * 0.1031);
            p += dot(p, p.yzx + 33.33);
            return frac((p.x + p.y) * p.z);
        }

        float3 Hash33(float3 p)
        {
            p = frac(p * 0.1031);
            p += dot(p, p.yxz + 33.33);
            return frac((p.xxy + p.yzz) * p.zyx);
        }

        float3 GetReferencePosition(float3 positionOS, float3 positionWS)
        {
            #if defined(_HEIGHTSPACE_WORLDSPACE)
                return positionWS;
            #else
                return positionOS;
            #endif
        }

        float3 GetReferenceNormal(float3 normalOS, float3 normalWS)
        {
            #if defined(_HEIGHTSPACE_WORLDSPACE)
                return normalize(normalWS);
            #else
                return normalize(normalOS);
            #endif
        }

        float3 GetSafeDirectionRef()
        {
            float3 dir = _DirectionVector.xyz;
            if (dot(dir, dir) < 1e-5)
            {
                dir = float3(0.0, -1.0, 0.0);
            }
            return normalize(dir);
        }

        float3 ReferenceToWorldDir(float3 dirRef)
        {
            #if defined(_HEIGHTSPACE_WORLDSPACE)
                return dirRef;
            #else
                return TransformObjectToWorldDir(dirRef);
            #endif
        }

        float ComputeHeightMaskTopToBottom(float3 refPos, float3 dirRef)
        {
            float projectedHeight = dot(refPos, -dirRef);
            float range = max(abs(_HeightMax - _HeightMin), 1e-5);
            float height01 = saturate((projectedHeight - _HeightMin) / range);
            return 1.0 - height01;
        }

        float SampleTriplanarNoiseLOD(float3 refPos, float3 refNormal)
        {
            float3 p = refPos * _NoiseScale;
            float3 weights = abs(refNormal);
            weights /= max(weights.x + weights.y + weights.z, 1e-4);

            float noiseXY = SAMPLE_TEXTURE2D_LOD(_NoiseTex, sampler_NoiseTex, p.xy, 0).r;
            float noiseXZ = SAMPLE_TEXTURE2D_LOD(_NoiseTex, sampler_NoiseTex, p.xz, 0).r;
            float noiseYZ = SAMPLE_TEXTURE2D_LOD(_NoiseTex, sampler_NoiseTex, p.yz, 0).r;

            return noiseXY * weights.z + noiseXZ * weights.y + noiseYZ * weights.x;
        }

        float SampleTriplanarNoise(float3 refPos, float3 refNormal)
        {
            float3 p = refPos * _NoiseScale;
            float3 weights = abs(refNormal);
            weights /= max(weights.x + weights.y + weights.z, 1e-4);

            float noiseXY = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, p.xy).r;
            float noiseXZ = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, p.xz).r;
            float noiseYZ = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, p.yz).r;

            return noiseXY * weights.z + noiseXZ * weights.y + noiseYZ * weights.x;
        }

        float ComputeDissolveMask(float topToBottomMask, float noiseSample)
        {
            float centeredNoise = (noiseSample * 2.0 - 1.0) * _NoiseStrength;
            return saturate(topToBottomMask + centeredNoise);
        }

        float GetDissolveThreshold()
        {
            return saturate(_DissolveProgress + _ClipThreshold);
        }

        DissolveData EvaluateDissolve(float3 originOS, float3 originWS, float3 normalOS, float3 normalWS)
        {
            DissolveData data;

            float3 refPos = GetReferencePosition(originOS, originWS);
            float3 refNormal = GetReferenceNormal(normalOS, normalWS);
            float3 dirRef = GetSafeDirectionRef();

            float noise = SampleTriplanarNoise(refPos, refNormal);
            float topToBottomMask = ComputeHeightMaskTopToBottom(refPos, dirRef);

            data.noise = noise;
            data.dissolveMask = ComputeDissolveMask(topToBottomMask, noise);
            data.threshold = GetDissolveThreshold();
            data.clipValue = data.dissolveMask - data.threshold;
            data.edge = saturate(1.0 - (data.clipValue / max(_EdgeWidth, 1e-5)));

            return data;
        }

        float3 ComputeDisplacementWS(float3 positionOS, float3 positionWS, float3 normalOS, float3 normalWS)
        {
            float3 refPos = GetReferencePosition(positionOS, positionWS);
            float3 refNormal = GetReferenceNormal(normalOS, normalWS);
            float3 dirRef = GetSafeDirectionRef();

            float noise = SampleTriplanarNoiseLOD(refPos, refNormal);
            float topToBottomMask = ComputeHeightMaskTopToBottom(refPos, dirRef);
            float dissolveMask = ComputeDissolveMask(topToBottomMask, noise);
            float threshold = GetDissolveThreshold();

            // Keep deformation only in a narrow band around dissolve front.
            float frontMask = saturate(1.0 - abs(dissolveMask - threshold) / max(_VertexFalloff, 1e-5));
            frontMask = frontMask * frontMask;

            float crumble = min(frontMask * _ParticleAmount, 0.04);
            if (crumble <= 1e-6)
            {
                return float3(0.0, 0.0, 0.0);
            }

            float displacementScale = min(max(abs(_HeightMax - _HeightMin), 1e-4), 1.5);

            // Continuous noise-driven offset avoids long stretched spikes between neighboring vertices.
            float noise2 = SampleTriplanarNoiseLOD(refPos + refNormal * 0.37 + dirRef * 0.19, refNormal);
            float signedNoise = (noise2 * 2.0 - 1.0);
            float jitter = sin(_Time.y * _VertexJitterSpeed + noise * 9.73 + noise2 * 6.11) * 0.2;

            float dirAmount = crumble;
            float normalAmount = crumble * (signedNoise + jitter) * 0.04;

            float3 offsetRef = (dirRef * dirAmount + refNormal * normalAmount)
                             * displacementScale
                             * (0.08 + _DustAmount * 0.10);

            return ReferenceToWorldDir(offsetRef);
        }

        half3 EvaluateLightContribution(half3 albedo, half3 normalWS, half3 viewDirWS, Light lightData)
        {
            half attenuation = lightData.distanceAttenuation * lightData.shadowAttenuation;
            half ndotl = saturate(dot(normalWS, lightData.direction));
            half3 diffuse = albedo * lightData.color * (ndotl * attenuation);

            half3 halfVec = SafeNormalize(lightData.direction + viewDirWS);
            half specPower = exp2(10.0h * _Smoothness + 1.0h);
            half specTerm = pow(saturate(dot(normalWS, halfVec)), specPower) * _SpecularStrength * ndotl;
            half3 specular = lightData.color * (specTerm * attenuation);

            return diffuse + specular;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardAshDissolve"
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
                float3 originWS : TEXCOORD2;
                float3 originOS : TEXCOORD3;
                half3 normalWS : TEXCOORD4;
                half3 normalOS : TEXCOORD5;
                half4 tangentWS : TEXCOORD6;
                half fogFactor : TEXCOORD7;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                float3 displacedWS = worldPos + ComputeDisplacementWS(input.positionOS.xyz, worldPos, input.normalOS, normalInputs.normalWS);

                output.positionCS = TransformWorldToHClip(displacedWS);
                output.positionWS = displacedWS;
                output.originWS = worldPos;
                output.originOS = input.positionOS.xyz;
                output.normalWS = NormalizeNormalPerVertex(normalInputs.normalWS);
                output.normalOS = normalize(input.normalOS);

                real tangentSign = input.tangentOS.w * GetOddNegativeScale();
                output.tangentWS = half4(NormalizeNormalPerVertex(normalInputs.tangentWS), tangentSign);

                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                DissolveData dissolve = EvaluateDissolve(input.originOS, input.originWS, input.normalOS, input.normalWS);
                clip(dissolve.clipValue);

                half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half alpha = baseSample.a * _BaseColor.a;
                #if defined(_ALPHATEST_ON)
                    clip(alpha - _Cutoff);
                #endif

                half3 albedo = baseSample.rgb * _BaseColor.rgb;

                half3 normalGeomWS = SafeNormalize(input.normalWS);
                half3 tangentWS = SafeNormalize(input.tangentWS.xyz);
                half3 bitangentWS = SafeNormalize(cross(normalGeomWS, tangentWS) * input.tangentWS.w);

                half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv), _BumpScale);
                half3 normalMapWS = SafeNormalize(TransformTangentToWorld(normalTS, half3x3(tangentWS, bitangentWS, normalGeomWS)));
                half3 normalWS = (dot(normalMapWS, normalMapWS) > 1e-4h) ? normalMapWS : normalGeomWS;
                normalWS = (dot(normalWS, normalWS) > 1e-4h) ? normalWS : half3(0.0h, 1.0h, 0.0h);

                float ashZone = saturate(1.0 - dissolve.clipValue / max(_EdgeWidth * 2.5, 1e-5));
                ashZone *= _BurnDarkening;

                float dustMask = saturate(dissolve.edge * _DustAmount * (0.5 + dissolve.noise));
                half3 ashedAlbedo = lerp(albedo, albedo * _AshTint.rgb, ashZone);
                ashedAlbedo = lerp(ashedAlbedo, _AshTint.rgb, dustMask * 0.35);

                half3 viewDirWS = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
                half3 lit = ashedAlbedo * 0.12h;
                lit += ashedAlbedo * max(half3(0.0h, 0.0h, 0.0h), SampleSH(normalWS));

                half4 shadowMask = half4(1.0h, 1.0h, 1.0h, 1.0h);
                Light mainLight;
                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE) || defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                    mainLight = GetMainLight(shadowCoord, input.positionWS, shadowMask);
                #else
                    mainLight = GetMainLight();
                #endif
                lit += EvaluateLightContribution(ashedAlbedo, normalWS, viewDirWS, mainLight);

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
                        lit += EvaluateLightContribution(ashedAlbedo, normalWS, viewDirWS, additionalLight);
                    }
                #endif

                half edgePulse = 0.7h + 0.3h * sin(_Time.y * _VertexJitterSpeed + dissolve.noise * 6.2831853);
                half edgeMask = pow(saturate(dissolve.edge), 2.0h);
                half3 hotEdgeColor = lerp(_EdgeColor.rgb * 0.55h, _EdgeColor.rgb * 1.75h + half3(1.2h, 0.45h, 0.08h), saturate(_EdgeStyle));
                half3 edgeEmission = hotEdgeColor * (edgeMask * _EdgeIntensity * edgePulse);

                half3 finalColor = lit + edgeEmission;
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
                float3 originWS : TEXCOORD1;
                float3 originOS : TEXCOORD2;
                float3 normalWS : TEXCOORD3;
                float3 normalOS : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vertShadow(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                float3 displacedWS = worldPos + ComputeDisplacementWS(input.positionOS.xyz, worldPos, input.normalOS, normalWS);

                output.positionCS = TransformWorldToHClip(displacedWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.originWS = worldPos;
                output.originOS = input.positionOS.xyz;
                output.normalWS = normalWS;
                output.normalOS = input.normalOS;
                return output;
            }

            half4 fragShadow(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                DissolveData dissolve = EvaluateDissolve(input.originOS, input.originWS, input.normalOS, input.normalWS);
                clip(dissolve.clipValue);

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
                float3 originWS : TEXCOORD1;
                float3 originOS : TEXCOORD2;
                float3 normalWS : TEXCOORD3;
                float3 normalOS : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vertDepth(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                float3 displacedWS = worldPos + ComputeDisplacementWS(input.positionOS.xyz, worldPos, input.normalOS, normalWS);

                output.positionCS = TransformWorldToHClip(displacedWS);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.originWS = worldPos;
                output.originOS = input.positionOS.xyz;
                output.normalWS = normalWS;
                output.normalOS = input.normalOS;
                return output;
            }

            half4 fragDepth(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                DissolveData dissolve = EvaluateDissolve(input.originOS, input.originWS, input.normalOS, input.normalWS);
                clip(dissolve.clipValue);

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






