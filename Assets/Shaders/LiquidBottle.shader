Shader "Custom/URP/LiquidBottleBest"
{
    Properties
    {
        _FillAmount ("Fill Amount (World Y)", Float) = 0

        _Color ("Liquid Color", Color) = (0,0.5,1,1)
        _FoamColor ("Foam Color", Color) = (1,1,1,1)

        _FoamWidth ("Foam Width", Float) = 0.02

        _WaveStrength ("Wave Strength", Float) = 0.03
        _WaveSpeed ("Wave Speed", Float) = 2.0

        _Smoothness ("Smoothness", Range(0,1)) = 0.8

        _EmissionColor ("Emission Color", Color) = (0,0,0,0)
        _EmissionStrength ("Emission Strength", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalRenderPipeline" "Queue"="Transparent" }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite Off

            HLSLPROGRAM

            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
            };

            float _FillAmount;

            float4 _Color;
            float4 _FoamColor;
            float _FoamWidth;

            float _WaveStrength;
            float _WaveSpeed;

            float _Smoothness;

            float4 _EmissionColor;
            float _EmissionStrength;

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                float3 worldPos = IN.positionWS;

                // --- ВОЛНА (в мировых координатах) ---
                float wave = sin(_Time.y * _WaveSpeed + worldPos.x * 4 + worldPos.z * 4) * _WaveStrength;

                // --- УРОВЕНЬ ЖИДКОСТИ ---
                float level = _FillAmount + wave;

                // --- МАСКА ЖИДКОСТИ ---
                float liquid = worldPos.y < level ? 1.0 : 0.0;

                // --- ПЕНА ---
                float foam = smoothstep(level - _FoamWidth, level, worldPos.y);

                float3 baseColor = lerp(_Color.rgb, _FoamColor.rgb, foam);

                // --- ОСВЕЩЕНИЕ ---
                Light mainLight = GetMainLight();

                float3 normal = normalize(IN.normalWS);
                float3 lightDir = normalize(mainLight.direction);

                float NdotL = saturate(dot(normal, -lightDir));
                float3 diffuse = baseColor * NdotL * mainLight.color;

                // specular
                float3 viewDir = normalize(GetCameraPositionWS() - worldPos);
                float3 halfDir = normalize(viewDir - lightDir);
                float spec = pow(saturate(dot(normal, halfDir)), 32) * _Smoothness;

                float3 lighting = diffuse + spec;

                // --- EMISSION ---
                float3 emission = _EmissionColor.rgb * _EmissionStrength;

                float3 finalColor = lighting + emission;

                return float4(finalColor, liquid);
            }

            ENDHLSL
        }
    }
}