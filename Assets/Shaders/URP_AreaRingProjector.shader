Shader "UralGameJam/Area Ring Projector"
{
    Properties
    {
        [HDR] _Color("Color", Color) = (1, 0.02, 0, 0.9)
        _InnerRadius("Inner Radius", Range(0, 1)) = 0.9
        _EdgeSoftness("Edge Softness", Range(0.0001, 0.1)) = 0.005
        _NormalCutoff("Ground Normal Cutoff", Range(0, 1)) = 0.08
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent-100"
            "DisableBatching" = "True"
        }

        Pass
        {
            Name "AreaRingProjector"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            Cull Front
            ZWrite Off
            ZTest Always

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _InnerRadius;
                float _EdgeSoftness;
                float _NormalCutoff;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 screenUV = GetNormalizedScreenSpaceUV(input.positionCS);
                float rawDepth = SampleSceneDepth(screenUV);

                #if UNITY_REVERSED_Z
                    float deviceDepth = rawDepth;
                    clip(rawDepth - 0.00001);
                #else
                    float deviceDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0, rawDepth);
                    clip(0.99999 - rawDepth);
                #endif

                float3 positionWS = ComputeWorldSpacePosition(
                    screenUV,
                    deviceDepth,
                    UNITY_MATRIX_I_VP);
                float3 positionOS = TransformWorldToObject(positionWS);

                float3 volumeDistance = 0.5 - abs(positionOS);
                clip(min(volumeDistance.x, min(volumeDistance.y, volumeDistance.z)));

                float3 surfaceNormalWS = normalize(cross(ddy(positionWS), ddx(positionWS)));
                float3 projectorUpWS = normalize(TransformObjectToWorldDir(float3(0, 1, 0)));
                clip(abs(dot(surfaceNormalWS, projectorUpWS)) - _NormalCutoff);

                float radialDistance = length(positionOS.xz * 2.0);
                float edge = max(fwidth(radialDistance), _EdgeSoftness);
                float outerMask = 1.0 - smoothstep(1.0 - edge, 1.0 + edge, radialDistance);
                float innerMask = smoothstep(_InnerRadius - edge, _InnerRadius + edge, radialDistance);
                float ringMask = outerMask * innerMask;
                clip(ringMask - 0.001);

                return half4(_Color.rgb, _Color.a * ringMask);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
