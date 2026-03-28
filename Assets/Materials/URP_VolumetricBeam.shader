Shader "Custom/URP/Volumetric Beam"
{
    Properties
    {
        [HDR] _BeamColor("Beam Color", Color) = (0.92, 0.95, 1.0, 1.0)
        _Intensity("Intensity", Range(0, 8)) = 1.8
        _Opacity("Opacity", Range(0, 1)) = 0.65

        [Header(Beam Shape)]
        _CoreTightness("Core Tightness", Range(0.2, 8)) = 2.2
        _LengthFade("Length Fade", Range(0.05, 8)) = 2.4
        _StartBoost("Start Boost", Range(0, 4)) = 1.3
        _ReverseV("Reverse V (0/1)", Range(0, 1)) = 0

        [Header(Light Bands)]
        _BandScale("Band Scale", Range(1, 80)) = 20
        _BandSpeed("Band Speed", Range(-5, 5)) = 0.35
        _BandStrength("Band Strength", Range(0, 1)) = 0.35
        _BandSoftness("Band Softness", Range(0.2, 6)) = 2.0

        [Header(Breakup Noise)]
        _NoiseScale("Noise Scale", Range(0.1, 25)) = 7.0
        _NoiseStrength("Noise Strength", Range(0, 1)) = 0.25

        [Header(Dust)]
        _DustDensity("Dust Density", Range(0, 1)) = 0.22
        _DustSize("Dust Size", Range(0.02, 0.5)) = 0.14
        _DustTiling("Dust Tiling XY", Vector) = (16, 64, 0, 0)
        _DustSpeed("Dust Speed", Range(0, 5)) = 0.9
        _DustBrightness("Dust Brightness", Range(0, 6)) = 2.2
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
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha One
            ZWrite Off
            Cull Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
            float4 _BeamColor;
            float _Intensity;
            float _Opacity;
            float _CoreTightness;
            float _LengthFade;
            float _StartBoost;
            float _ReverseV;
            float _BandScale;
            float _BandSpeed;
            float _BandStrength;
            float _BandSoftness;
            float _NoiseScale;
            float _NoiseStrength;
            float _DustDensity;
            float _DustSize;
            float4 _DustTiling;
            float _DustSpeed;
            float _DustBrightness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half fogFactor : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);

                float a = Hash21(i);
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionHCS = posInputs.positionCS;
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = input.uv;
                float v = lerp(uv.y, 1.0 - uv.y, saturate(_ReverseV));

                float centerMask = saturate(1.0 - abs(uv.x * 2.0 - 1.0));
                centerMask = pow(centerMask, _CoreTightness);

                float lengthMask = exp2(-v * _LengthFade);
                float startBoost = 1.0 + (1.0 - v) * _StartBoost;
                float baseBeam = centerMask * lengthMask * _Opacity * startBoost;

                float phase = v * _BandScale - _Time.y * _BandSpeed;
                float bandA = sin(phase + (uv.x * 2.0 - 1.0) * 5.5) * 0.5 + 0.5;
                float bandB = sin(phase * 1.67 + uv.x * 11.0 + 1.31) * 0.5 + 0.5;
                float bands = lerp(1.0, pow(saturate(bandA * 0.7 + bandB * 0.3), _BandSoftness), _BandStrength);

                float noise = ValueNoise(float2(uv.x * _NoiseScale, v * _NoiseScale * 0.8 + _Time.y * 0.45));
                float breakup = lerp(1.0, saturate(noise * 1.35), _NoiseStrength);

                float beam = baseBeam * bands * breakup;

                float2 dustUV = float2(uv.x * _DustTiling.x, v * _DustTiling.y - _Time.y * _DustSpeed);
                float2 cell = floor(dustUV);
                float2 fracUV = frac(dustUV);
                float2 dustPoint = float2(Hash21(cell + 17.0), Hash21(cell + 53.0));
                float dotMask = smoothstep(_DustSize, _DustSize * 0.35, length(fracUV - dustPoint));
                float appear = step(Hash21(cell + 89.0), _DustDensity);
                float dust = dotMask * appear * centerMask * (0.55 + lengthMask * 0.45);

                float3 beamColor = _BeamColor.rgb * _Intensity;
                float3 color = beamColor * beam;
                color += beamColor * (_DustBrightness * dust);
                color = MixFog(color, input.fogFactor);

                float alpha = saturate(beam + dust * 0.35);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}

