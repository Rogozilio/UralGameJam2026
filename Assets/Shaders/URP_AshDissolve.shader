Shader "Custom/URP/Disintegrate"
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
        _ClipThreshold("Clip Threshold", Range(-0.2, 0.2)) = 0
        _DirectionVector("Direction Vector", Vector) = (0, -1, 0, 0)
        _HeightMin("Height Min", Float) = 0
        _HeightMax("Height Max", Float) = 1

        [Header(Noise)]
        _NoiseTex("Noise Tex", 2D) = "gray" {}
        _NoiseScale("Noise Scale", Range(0.1, 20.0)) = 3.0
        _NoiseStrength("Noise Strength", Range(0.0, 1.0)) = 0.2

        [Header(Edge)]
        _EdgeWidth("Edge Width", Range(0.001, 0.3)) = 0.05
        [HDR] _EdgeColor("Edge Color", Color) = (2.5, 1.2, 0.2, 1.0)
        _EdgeEmission("Edge Emission", Range(0.0, 10.0)) = 2.0
        _EdgeStyle("Edge Style (0 Ash, 1 Hot)", Range(0.0, 1.0)) = 0.0
        _EdgeNoiseBoost("Edge Noise Boost", Range(0.0, 2.0)) = 0.6

        [Header(Ash)]
        _AshTint("Ash Tint", Color) = (0.25, 0.23, 0.2, 1)
        _BurnDarkening("Burn Darkening", Range(0.0, 1.0)) = 0.6
        _AshWidth("Ash Width", Range(0.01, 0.6)) = 0.18

        [Header(Vertex Motion)]
        _VertexOffset("Vertex Offset", Range(0.0, 0.2)) = 0.006
        _VertexBand("Vertex Band", Range(0.01, 0.4)) = 0.12
        _VertexDirectionInfluence("Direction Influence", Range(0.0, 1.0)) = 1.0
        _VertexNormalInfluence("Normal Influence", Range(0.0, 1.0)) = 0.08
        _VertexMaxWorldOffset("Max World Offset", Range(0.0, 0.05)) = 0.008
        _VertexJitterSpeed("Jitter Speed", Range(0.0, 20.0)) = 7.0

        [Header(Lighting)]
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.35
        _SpecularStrength("Specular Strength", Range(0.0, 2.0)) = 0.35

        [Header(Alpha Clip)]
        [Toggle(_ALPHATEST_ON)] _AlphaClip("Use Base Alpha Clip", Float) = 0
        _Cutoff("Base Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        [HideInInspector] _Cull("__Cull", Float) = 2
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "RenderPipeline" = "UniversalPipeline"
        }

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
            float4 _DirectionVector;
            float4 _EdgeColor;
            float4 _AshTint;

            float _BumpScale;

            float _DissolveProgress;
            float _ClipThreshold;
            float _HeightMin;
            float _HeightMax;

            float _NoiseScale;
            float _NoiseStrength;

            float _EdgeWidth;
            float _EdgeEmission;
            float _EdgeStyle;
            float _EdgeNoiseBoost;

            float _BurnDarkening;
            float _AshWidth;

            float _VertexOffset;
            float _VertexBand;
            float _VertexDirectionInfluence;
            float _VertexNormalInfluence;
            float _VertexMaxWorldOffset;
            float _VertexJitterSpeed;

            float _Smoothness;
            float _SpecularStrength;

            float _Cutoff;
        CBUFFER_END

        struct DissolveData
        {
            float mask;
            float threshold;
            float clipValue;
            float edge;
            float noise;
        };

        float3 GetSafeDirectionRef()
        {
            float3 dir = _DirectionVector.xyz;
            if (dot(dir, dir) < 1e-5)
            {
                dir = float3(0.0, -1.0, 0.0);
            }
            return normalize(dir);
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
                return SafeNormalize(normalWS);
            #else
                return SafeNormalize(normalOS);
            #endif
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
            float projected = dot(refPos, -dirRef);
            float range = max(abs(_HeightMax - _HeightMin), 1e-5);
            float t = saturate((projected - _HeightMin) / range);
            return 1.0 - t;
        }

        float SampleNoiseTriplanar(float3 refPos, float3 refNormal)
        {
            float3 p = refPos * _NoiseScale;
            float3 w = abs(refNormal);
            w = pow(w, 3.0);
            w /= max(w.x + w.y + w.z, 1e-5);

            float nXY = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, p.xy).r;
            float nXZ = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, p.xz).r;
            float nYZ = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, p.yz).r;
            return nXY * w.z + nXZ * w.y + nYZ * w.x;
        }

        float SampleNoiseTriplanarLOD(float3 refPos, float3 refNormal)
        {
            float3 p = refPos * _NoiseScale;
            float3 w = abs(refNormal);
            w = pow(w, 3.0);
            w /= max(w.x + w.y + w.z, 1e-5);

            float nXY = SAMPLE_TEXTURE2D_LOD(_NoiseTex, sampler_NoiseTex, p.xy, 0).r;
            float nXZ = SAMPLE_TEXTURE2D_LOD(_NoiseTex, sampler_NoiseTex, p.xz, 0).r;
            float nYZ = SAMPLE_TEXTURE2D_LOD(_NoiseTex, sampler_NoiseTex, p.yz, 0).r;
            return nXY * w.z + nXZ * w.y + nYZ * w.x;
        }

        DissolveData EvaluateDissolve(float3 positionOS, float3 positionWS, float3 normalOS, float3 normalWS)
        {
            DissolveData o;

            float3 refPos = GetReferencePosition(positionOS, positionWS);
            float3 refNormal = GetReferenceNormal(normalOS, normalWS);
            float3 dirRef = GetSafeDirectionRef();

            float baseMask = ComputeHeightMaskTopToBottom(refPos, dirRef);
            float noise = SampleNoiseTriplanar(refPos, refNormal);
            float noiseOffset = (noise * 2.0 - 1.0) * _NoiseStrength;

            o.mask = saturate(baseMask + noiseOffset);
            o.threshold = saturate(_DissolveProgress + _ClipThreshold);
            o.clipValue = o.mask - o.threshold;
            float edgeRamp = smoothstep(0.0, max(_EdgeWidth, 1e-5), o.clipValue);
            o.edge = (1.0 - edgeRamp) * step(0.0, o.clipValue);
            o.noise = noise;

            return o;
        }

        float3 ComputeDisplacementWS(float3 positionOS, float3 positionWS, float3 normalOS, float3 normalWS)
        {
            if (_VertexOffset <= 1e-6)
            {
                return float3(0.0, 0.0, 0.0);
            }

            float3 refPos = GetReferencePosition(positionOS, positionWS);
            float3 refNormal = GetReferenceNormal(normalOS, normalWS);
            float3 dirRef = GetSafeDirectionRef();

            float baseMask = ComputeHeightMaskTopToBottom(refPos, dirRef);
            float noiseA = SampleNoiseTriplanarLOD(refPos, refNormal);
            float noiseOffset = (noiseA * 2.0 - 1.0) * _NoiseStrength;
            float mask = saturate(baseMask + noiseOffset);
            float threshold = saturate(_DissolveProgress + _ClipThreshold);

            float front = saturate(1.0 - abs(mask - threshold) / max(_VertexBand, 1e-5));
            front *= front;

            float amplitude = front * _VertexOffset;
            if (amplitude <= 1e-6)
            {
                return float3(0.0, 0.0, 0.0);
            }

            float noiseB = SampleNoiseTriplanarLOD(refPos + refNormal * 0.23 + dirRef * 0.17, refNormal);
            float signedNoise = noiseB * 2.0 - 1.0;
            float jitter = sin(_Time.y * _VertexJitterSpeed + (noiseA + noiseB) * 6.2831853) * 0.12;

            float3 dirPart = dirRef * (amplitude * _VertexDirectionInfluence);
            float3 normalPart = refNormal * (amplitude * (signedNoise + jitter) * _VertexNormalInfluence * 0.2);

            float rangeScale = min(max(abs(_HeightMax - _HeightMin), 1e-4), 1.0);
            float3 offsetRef = (dirPart + normalPart) * rangeScale;
            float3 offsetWS = ReferenceToWorldDir(offsetRef);
            float maxOffset = max(_VertexMaxWorldOffset, 1e-5);
            float offsetLen = length(offsetWS);
            if (offsetLen > maxOffset)
            {
                offsetWS *= maxOffset / offsetLen;
            }
            return offsetWS;
        }

        half3 EvaluateLight(half3 albedo, half3 normalWS, half3 viewDirWS, Light lightData)
        {
            half attenuation = lightData.distanceAttenuation * lightData.shadowAttenuation;
            half ndotl = saturate(dot(normalWS, lightData.direction));

            half3 diffuse = albedo * lightData.color * (ndotl * attenuation);

            half3 halfVec = SafeNormalize(lightData.direction + viewDirWS);
            half specPower = exp2(10.0h * _Smoothness + 1.0h);
            half spec = pow(saturate(dot(normalWS, halfVec)), specPower) * _SpecularStrength * ndotl;
            half3 specular = lightData.color * (spec * attenuation);

            return diffuse + specular;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardDisintegrate"
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
                float3 sourceWS : TEXCOORD2;
                float3 sourceOS : TEXCOORD3;
                half3 normalWS : TEXCOORD4;
                half3 normalOS : TEXCOORD5;
                half4 tangentWS : TEXCOORD6;
                half fogFactor : TEXCOORD7;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                VertexNormalInputs n = GetVertexNormalInputs(input.normalOS, input.tangentOS);

                float3 sourceWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 offsetWS = ComputeDisplacementWS(input.positionOS.xyz, sourceWS, input.normalOS, n.normalWS);
                float3 displacedWS = sourceWS + offsetWS;

                o.positionCS = TransformWorldToHClip(displacedWS);
                o.positionWS = displacedWS;
                o.sourceWS = sourceWS;
                o.sourceOS = input.positionOS.xyz;
                o.normalWS = NormalizeNormalPerVertex(n.normalWS);
                o.normalOS = SafeNormalize(input.normalOS);

                real tangentSign = input.tangentOS.w * GetOddNegativeScale();
                o.tangentWS = half4(NormalizeNormalPerVertex(n.tangentWS), tangentSign);

                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                o.fogFactor = ComputeFogFactor(o.positionCS.z);

                return o;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                DissolveData d = EvaluateDissolve(input.sourceOS, input.sourceWS, input.normalOS, input.normalWS);
                clip(d.clipValue);

                half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half alpha = baseSample.a * _BaseColor.a;
                #if defined(_ALPHATEST_ON)
                    clip(alpha - _Cutoff);
                #endif

                half3 albedo = baseSample.rgb * _BaseColor.rgb;

                half3 nGeom = SafeNormalize(input.normalWS);
                half3 t = SafeNormalize(input.tangentWS.xyz);
                half3 b = SafeNormalize(cross(nGeom, t) * input.tangentWS.w);

                half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv), _BumpScale);
                half3 normalWS = SafeNormalize(TransformTangentToWorld(normalTS, half3x3(t, b, nGeom)));
                normalWS = (dot(normalWS, normalWS) > 1e-4h) ? normalWS : nGeom;
                float ashBand = saturate(1.0 - d.clipValue / max(_AshWidth, 1e-5));
                float ashNoise = saturate(0.75 + (d.noise * 2.0 - 1.0) * 0.25);
                half3 ashAlbedo = lerp(albedo, albedo * _AshTint.rgb, ashBand * ashNoise * _BurnDarkening);

                half3 viewDirWS = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));

                half3 lit = ashAlbedo * 0.12h;
                lit += ashAlbedo * max(half3(0.0h, 0.0h, 0.0h), SampleSH(normalWS));

                half4 shadowMask = half4(1.0h, 1.0h, 1.0h, 1.0h);
                Light mainLight;
                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE) || defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                    mainLight = GetMainLight(shadowCoord, input.positionWS, shadowMask);
                #else
                    mainLight = GetMainLight();
                #endif
                lit += EvaluateLight(ashAlbedo, normalWS, viewDirWS, mainLight);

                #if defined(_ADDITIONAL_LIGHTS)
                    uint lightCount = GetAdditionalLightsCount();
                    [loop]
                    for (uint i = 0u; i < lightCount; ++i)
                    {
                        Light add;
                        #if defined(_ADDITIONAL_LIGHT_SHADOWS)
                            add = GetAdditionalLight(i, input.positionWS, shadowMask);
                        #else
                            add = GetAdditionalLight(i, input.positionWS);
                        #endif
                        lit += EvaluateLight(ashAlbedo, normalWS, viewDirWS, add);
                    }
                #endif
                half edgePulse = 0.75h + 0.25h * sin(_Time.y * _VertexJitterSpeed + d.noise * 6.2831853);
                half edgeNoise = saturate(1.0h + (d.noise * 2.0h - 1.0h) * _EdgeNoiseBoost);
                half edgeMask = saturate(d.edge * edgeNoise);
                edgeMask = edgeMask * edgeMask;

                half3 edgeColorAsh = _EdgeColor.rgb * 0.6h;
                half3 edgeColorHot = _EdgeColor.rgb * 1.8h + half3(1.0h, 0.4h, 0.08h);
                half3 edgeColor = lerp(edgeColorAsh, edgeColorHot, saturate(_EdgeStyle));
                half3 edgeEmission = edgeColor * (edgeMask * _EdgeEmission * edgePulse);

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
                float3 sourceWS : TEXCOORD1;
                float3 sourceOS : TEXCOORD2;
                float3 normalWS : TEXCOORD3;
                float3 normalOS : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vertShadow(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float3 sourceWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 offsetWS = ComputeDisplacementWS(input.positionOS.xyz, sourceWS, input.normalOS, normalWS);
                float3 displacedWS = sourceWS + offsetWS;

                o.positionCS = TransformWorldToHClip(displacedWS);
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                o.sourceWS = sourceWS;
                o.sourceOS = input.positionOS.xyz;
                o.normalWS = normalWS;
                o.normalOS = input.normalOS;
                return o;
            }

            half4 fragShadow(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                DissolveData d = EvaluateDissolve(input.sourceOS, input.sourceWS, input.normalOS, input.normalWS);
                clip(d.clipValue);

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
                float3 sourceWS : TEXCOORD1;
                float3 sourceOS : TEXCOORD2;
                float3 normalWS : TEXCOORD3;
                float3 normalOS : TEXCOORD4;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vertDepth(Attributes input)
            {
                Varyings o = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float3 sourceWS = TransformObjectToWorld(input.positionOS.xyz);
                float3 offsetWS = ComputeDisplacementWS(input.positionOS.xyz, sourceWS, input.normalOS, normalWS);
                float3 displacedWS = sourceWS + offsetWS;

                o.positionCS = TransformWorldToHClip(displacedWS);
                o.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                o.sourceWS = sourceWS;
                o.sourceOS = input.positionOS.xyz;
                o.normalWS = normalWS;
                o.normalOS = input.normalOS;
                return o;
            }

            half4 fragDepth(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                DissolveData d = EvaluateDissolve(input.sourceOS, input.sourceWS, input.normalOS, input.normalWS);
                clip(d.clipValue);

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






