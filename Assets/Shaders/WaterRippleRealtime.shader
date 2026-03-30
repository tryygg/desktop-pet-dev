Shader "Custom/WaterRippleRealtime"
{
    Properties
    {
        _ShallowColor("Shallow Color", Color) = (0.38, 0.74, 0.95, 0.75)
        _DeepColor("Deep Color", Color) = (0.04, 0.16, 0.25, 0.92)
        _SpecularColor("Specular Color", Color) = (0.90, 0.97, 1.00, 1.00)
        _NormalMap("Ripple Normal", 2D) = "bump" {}
        _NormalStrength("Normal Strength", Range(0, 3)) = 1.2
        _TilingA("Tiling A", Vector) = (1.7, 1.7, 0, 0)
        _TilingB("Tiling B", Vector) = (3.8, 3.8, 0, 0)
        _SpeedA("Speed A", Vector) = (0.05, 0.04, 0, 0)
        _SpeedB("Speed B", Vector) = (-0.03, 0.06, 0, 0)
        _WaveMix("Wave Mix", Range(0, 2)) = 1.0
        _Distortion("Distortion", Range(0, 1)) = 0.18
        _Opacity("Opacity", Range(0, 1)) = 0.82
        _FresnelPower("Fresnel Power", Range(0.1, 8)) = 3.0
        _FresnelStrength("Fresnel Strength", Range(0, 2)) = 0.8
        _SpecularPower("Specular Power", Range(8, 256)) = 96
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            CBUFFER_START(UnityPerMaterial)
                half4 _ShallowColor;
                half4 _DeepColor;
                half4 _SpecularColor;
                float4 _NormalMap_ST;
                float4 _TilingA;
                float4 _TilingB;
                float4 _SpeedA;
                float4 _SpeedB;
                half _NormalStrength;
                half _WaveMix;
                half _Distortion;
                half _Opacity;
                half _FresnelPower;
                half _FresnelStrength;
                half _SpecularPower;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float4 tangentWS : TEXCOORD3;
                half fogFactor : TEXCOORD4;
            };

            float3 UnpackRippleNormal(float4 packedNormal, float strength)
            {
                float3 normalTS = packedNormal.xyz * 2.0 - 1.0;
                normalTS.xy *= strength;
                normalTS.z = sqrt(saturate(1.0 - dot(normalTS.xy, normalTS.xy)));
                return normalize(normalTS);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);

                output.positionHCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.tangentWS = float4(TransformObjectToWorldDir(input.tangentOS.xyz), input.tangentOS.w * GetOddNegativeScale());
                output.uv = TRANSFORM_TEX(input.uv, _NormalMap);
                output.fogFactor = ComputeFogFactor(positionInputs.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uvA = input.uv * _TilingA.xy + _Time.y * _SpeedA.xy;
                float2 uvB = input.uv * _TilingB.xy + _Time.y * _SpeedB.xy;

                float3 rippleA = UnpackRippleNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uvA), _NormalStrength);
                float3 rippleB = UnpackRippleNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uvB), _NormalStrength * 0.85);

                float2 combinedXY = (rippleA.xy + rippleB.xy) * _WaveMix;
                float3 rippleTS = normalize(float3(combinedXY, sqrt(saturate(1.0 - dot(combinedXY, combinedXY)))));

                float3 normalWS = normalize(input.normalWS);
                float3 tangentWS = normalize(input.tangentWS.xyz);
                float3 bitangentWS = normalize(cross(normalWS, tangentWS) * input.tangentWS.w);
                float3x3 tbn = float3x3(tangentWS, bitangentWS, normalWS);
                float3 waterNormalWS = normalize(mul(rippleTS, tbn));

                float3 viewDirWS = normalize(GetWorldSpaceViewDir(input.positionWS));
                Light mainLight = GetMainLight();
                float3 lightDirWS = normalize(mainLight.direction);
                float3 halfDirWS = normalize(lightDirWS + viewDirWS);

                float fresnel = pow(1.0 - saturate(dot(viewDirWS, waterNormalWS)), _FresnelPower) * _FresnelStrength;
                float ndotl = saturate(dot(waterNormalWS, lightDirWS));
                float specular = pow(saturate(dot(waterNormalWS, halfDirWS)), _SpecularPower) * ndotl;

                float horizon = saturate(waterNormalWS.y * 0.5 + 0.5 + dot(rippleTS.xy, float2(0.5, 0.5)) * _Distortion);
                float3 waterColor = lerp(_DeepColor.rgb, _ShallowColor.rgb, horizon);
                waterColor += _SpecularColor.rgb * specular;
                waterColor += _SpecularColor.rgb * fresnel * 0.35;

                waterColor = MixFog(waterColor, input.fogFactor);
                float alpha = saturate(_Opacity + fresnel * 0.15);

                return half4(waterColor, alpha);
            }
            ENDHLSL
        }
    }
}
