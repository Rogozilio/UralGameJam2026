Shader "Custom/URP/Burn Disintegrate"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Strength", Range(0.0, 2.0)) = 1.0

        [Header(Burn)]
        _BurnProgress("Burn Progress", Range(0, 1)) = 0
        _BurnSoftness("Burn Softness", Range(0.001, 0.25)) = 0.06
        _NoiseScale("Noise Scale", Range(0.5, 20.0)) = 4.0

        [Header(Edge)]
        _EdgeWidth("Edge Width", Range(0.001, 0.2)) = 0.045
        [HDR] _EdgeColor("Edge Color", Color) = (3.0, 1.35, 0.2, 1.0)
        _EdgeEmission("Edge Emission", Range(0.0, 8.0)) = 2.0
        _EdgeFlickerSpeed("Edge Flicker Speed", Range(0.0, 12.0)) = 4.0
        _CharColor("Char Color", Color) = (0.08, 0.06, 0.05, 1.0)

        [Header(Disintegration)]
        _ChunkScale("Chunk Scale", Range(2.0, 64.0)) = 20.0
        _ScatterWidth("Scatter Width", Range(0.001, 0.35)) = 0.12
        _ParticleScatter("Particle Scatter", Range(0.0, 0.5)) = 0.08
        _ParticleJitter("Particle Jitter", Range(0.0, 0.2)) = 0.03
        _JitterSpeed("Jitter Speed", Range(0.0, 20.0)) = 9.0
        _UpBias("Up Bias", Range(0.0, 1.0)) = 0.35

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
        LOD 200

        Pass
        {
            Name "ForwardBurn"
            Tags { "LightMode" = "UniversalForward" }

            Blend One Zero
            ZWrite On
            Cull[_Cull]

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap);
            SAMPLER(sampler_BumpMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _BaseMap_ST;
                float4 _EdgeColor;
                float4 _CharColor;
                float _BumpScale;
                float _BurnProgress;
                float _BurnSoftness;
                float _NoiseScale;
                float _EdgeWidth;
                float _EdgeEmission;
                float _EdgeFlickerSpeed;
                float _ChunkScale;
                float _ScatterWidth;
                float _ParticleScatter;
                float _ParticleJitter;
                float _JitterSpeed;
                float _UpBias;
            CBUFFER_END

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

            float ValueNoise3D(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float n000 = Hash31(i + float3(0, 0, 0));
                float n100 = Hash31(i + float3(1, 0, 0));
                float n010 = Hash31(i + float3(0, 1, 0));
                float n110 = Hash31(i + float3(1, 1, 0));
                float n001 = Hash31(i + float3(0, 0, 1));
                float n101 = Hash31(i + float3(1, 0, 1));
                float n011 = Hash31(i + float3(0, 1, 1));
                float n111 = Hash31(i + float3(1, 1, 1));

                float nx00 = lerp(n000, n100, f.x);
                float nx10 = lerp(n010, n110, f.x);
                float nx01 = lerp(n001, n101, f.x);
                float nx11 = lerp(n011, n111, f.x);
                float nxy0 = lerp(nx00, nx10, f.y);
                float nxy1 = lerp(nx01, nx11, f.y);
                return lerp(nxy0, nxy1, f.z);
            }

            float DissolveSignal(float3 worldPos)
            {
                float n1 = ValueNoise3D(worldPos * _NoiseScale);
                float n2 = ValueNoise3D((worldPos + 17.37) * (_NoiseScale * 1.91));
                return saturate(lerp(n1, n2, 0.35));
            }

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
                float3 normalWS : TEXCOORD1;
                float3 positionWS : TEXCOORD2;
                float3 originWS : TEXCOORD3;
                half fogFactor : TEXCOORD4;
                half4 tangentWS : TEXCOORD5;
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
                float signal = DissolveSignal(worldPos);

                float3 cell = floor(worldPos * max(_ChunkScale, 1.0));
                float3 randDir = Hash33(cell) * 2.0 - 1.0;
                randDir = normalize(randDir + float3(0.0001, 0.0, 0.0));
                float3 driftDir = normalize(lerp(randDir, float3(0.0, 1.0, 0.0), saturate(_UpBias)));

                float scatterPhase = saturate(1.0 - abs(signal - _BurnProgress) / max(_ScatterWidth, 0.0001));
                float jitterRnd = Hash31(cell + 11.7);
                float jitterWave = sin(_Time.y * _JitterSpeed + jitterRnd * 6.2831853);

                float3 offset = driftDir * (_ParticleScatter * scatterPhase);
                offset += randDir * (_ParticleJitter * scatterPhase * jitterWave);

                float3 displacedWS = worldPos + offset;

                output.positionWS = displacedWS;
                output.originWS = worldPos;
                output.normalWS = NormalizeNormalPerVertex(normalInputs.normalWS);
                real tangentSign = input.tangentOS.w * GetOddNegativeScale();
                output.tangentWS = half4(NormalizeNormalPerVertex(normalInputs.tangentWS), tangentSign);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.positionCS = TransformWorldToHClip(displacedWS);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float signal = DissolveSignal(input.originWS);
                clip(signal - _BurnProgress);

                half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half3 albedo = baseSample.rgb * _BaseColor.rgb;

                half3 normalGeomWS = normalize(input.normalWS);
                half3 tangentWS = normalize(input.tangentWS.xyz);
                half3 bitangentWS = normalize(cross(normalGeomWS, tangentWS) * input.tangentWS.w);
                half4 packedNormal = SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, input.uv);
                half3 normalTS = UnpackNormalScale(packedNormal, _BumpScale);
                half3 normalWS = normalize(TransformTangentToWorld(normalTS, half3x3(tangentWS, bitangentWS, normalGeomWS)));

                half4 shadowMask = half4(1.0h, 1.0h, 1.0h, 1.0h);
                Light mainLight;
                #if defined(_MAIN_LIGHT_SHADOWS) || defined(_MAIN_LIGHT_SHADOWS_CASCADE) || defined(_MAIN_LIGHT_SHADOWS_SCREEN)
                    float4 shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                    mainLight = GetMainLight(shadowCoord, input.positionWS, shadowMask);
                #else
                    mainLight = GetMainLight();
                #endif

                half ndotl = saturate(dot(normalWS, mainLight.direction));
                half3 litColor = albedo * (0.22h + ndotl * 0.78h * mainLight.distanceAttenuation * mainLight.shadowAttenuation);

                float edgeDist = signal - _BurnProgress;
                float edgeMask = saturate(1.0 - edgeDist / max(_EdgeWidth, 0.0001));
                float flicker = 0.7 + 0.6 * ValueNoise3D(input.originWS * (_NoiseScale * 3.5) + _Time.y * _EdgeFlickerSpeed);
                float glow = pow(edgeMask, 3.0) * flicker;

                half3 color = lerp(litColor, _CharColor.rgb, edgeMask * 0.55);
                color += _EdgeColor.rgb * (glow * _EdgeEmission);
                color = MixFog(color, input.fogFactor);
                return half4(color, 1.0h);
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
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _BurnProgress;
                float _NoiseScale;
            CBUFFER_END

            float Hash31(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float ValueNoise3D(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float n000 = Hash31(i + float3(0, 0, 0));
                float n100 = Hash31(i + float3(1, 0, 0));
                float n010 = Hash31(i + float3(0, 1, 0));
                float n110 = Hash31(i + float3(1, 1, 0));
                float n001 = Hash31(i + float3(0, 0, 1));
                float n101 = Hash31(i + float3(1, 0, 1));
                float n011 = Hash31(i + float3(0, 1, 1));
                float n111 = Hash31(i + float3(1, 1, 1));

                float nx00 = lerp(n000, n100, f.x);
                float nx10 = lerp(n010, n110, f.x);
                float nx01 = lerp(n001, n101, f.x);
                float nx11 = lerp(n011, n111, f.x);
                float nxy0 = lerp(nx00, nx10, f.y);
                float nxy1 = lerp(nx01, nx11, f.y);
                return lerp(nxy0, nxy1, f.z);
            }

            float DissolveSignal(float3 worldPos)
            {
                float n1 = ValueNoise3D(worldPos * _NoiseScale);
                float n2 = ValueNoise3D((worldPos + 17.37) * (_NoiseScale * 1.91));
                return saturate(lerp(n1, n2, 0.35));
            }

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 originWS : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vertShadow(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                output.originWS = worldPos;
                output.positionCS = TransformWorldToHClip(worldPos);
                return output;
            }

            half4 fragShadow(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float signal = DissolveSignal(input.originWS);
                clip(signal - _BurnProgress);
                return 0;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
