Shader "RPG/Runescape URP"
{
    Properties
    {
        _Diffuse("Diffuse", 2D) = "white" {}
        _Float0("Vertex Snap", Float) = 90
        _DiffuseTint("Diffuse Tint", Color) = (1,1,1,1)
        _Shadow("Shadow", Color) = (0.5372549,0.5372549,0.5372549,1)
        _LightColorwhennoLight("Light Color when no Light", Color) = (0.9490196,0.9490196,0.9490196,0)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _Diffuse_ST;
            float _Float0;
            float4 _DiffuseTint;
            float4 _Shadow;
            float4 _LightColorwhennoLight;
        CBUFFER_END

        float3 SnapWorldPosition(float3 positionWS)
        {
            return _Float0 > 0.0 ? round(positionWS * _Float0) / _Float0 : positionWS;
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_Diffuse);
            SAMPLER(sampler_Diffuse);

            static const float BayerMatrix[64] =
            {
                 1, 49, 13, 61,  4, 52, 16, 64,
                33, 17, 45, 29, 36, 20, 48, 32,
                 9, 57,  5, 53, 12, 60,  8, 56,
                41, 25, 37, 21, 44, 28, 40, 24,
                 3, 51, 15, 63,  2, 50, 14, 62,
                35, 19, 47, 31, 34, 18, 46, 30,
                11, 59,  7, 55, 10, 58,  6, 54,
                43, 27, 39, 23, 42, 26, 38, 22
            };

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float fogFactor : TEXCOORD2;
            };

            float Dither8x8(float2 pixel)
            {
                uint x = (uint)pixel.x % 8;
                uint y = (uint)pixel.y % 8;
                return BayerMatrix[y * 8 + x] / 64.0;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = SnapWorldPosition(TransformObjectToWorld(input.positionOS.xyz));
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _Diffuse);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                Light mainLight = GetMainLight();
                bool hasLight = dot(mainLight.color, 1.0) > 0.0;

                float3 lightColor = hasLight ? mainLight.color : _LightColorwhennoLight.rgb;
                float3 lightDirection = hasLight ? mainLight.direction : float3(1.0, 1.0, 1.0);

                float3 baseColor = _DiffuseTint.rgb * SAMPLE_TEXTURE2D(_Diffuse, sampler_Diffuse, input.uv).rgb * lightColor;

                float ndotl = dot(lightDirection, normalize(input.normalWS));
                float lit = saturate(ndotl + smoothstep(0.0, 0.45, ndotl));
                float mask = step(Dither8x8(input.positionCS.xy), lit);

                float3 color = lerp(baseColor * _Shadow.rgb, baseColor, mask);
                color = MixFog(color, input.fogFactor);

                return half4(color, 1.0);
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
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            float4 vert(Attributes input) : SV_POSITION
            {
                float3 positionWS = SnapWorldPosition(TransformObjectToWorld(input.positionOS.xyz));
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));

                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif

                return positionCS;
            }

            half4 frag() : SV_Target
            {
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
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            float4 vert(Attributes input) : SV_POSITION
            {
                return TransformWorldToHClip(SnapWorldPosition(TransformObjectToWorld(input.positionOS.xyz)));
            }

            half4 frag() : SV_Target
            {
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack Off
}
