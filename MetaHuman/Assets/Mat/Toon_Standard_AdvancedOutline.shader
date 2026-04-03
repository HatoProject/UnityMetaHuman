Shader "Custom/Toon_Standard_AdvancedOutline"
{
    Properties
    {
        // Base
        _BaseMap ("Albedo", 2D) = "white" {}
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _BaseColor ("Color", Color) = (1,1,1,1)

        // Toon shading
        _Ramp ("Toon Ramp", 2D) = "white" {}
        _StepMin ("Step Min Brightness", Range(0,1)) = 0.25
        _BaseShadowSoftness ("Shadow Softness", Range(0.001,0.5)) = 0.25

        // Shadow tinting
        _AOMap("AO Map", 2D) = "white" {}
        _SpecMap("Specular Map", 2D) = "white" {}
        _ShadowMask("ShadowMask", 2D) = "blue"{}
        [HDR]_SpecColor ("Specular Color", Color) = (1,1,1,1)
        _Glossiness("Glossiness", Range(0,1)) = 0.5

        _ShadowTint ("Shadow Tint", Color) = (0.15,0.15,0.18,1)
        _ShadowTint2 ("Shadow Tint2", Color) = (0.15,0.15,0.18,1)
        _ShadowDarkness ("Shadow Darkness", Range(0,5)) = 0.8
        _ShadowDepth ("Shadow Depth", Range(0,5)) = 0.5
        _AOIntensity ("AO Intensity", Range(0,2)) = 1.0

        // Rim Light
        [HDR]_RimColor ("Rim Color", Color) = (1,1,1,1)
        _RimPower ("Rim Power", Range(0.1,8)) = 2.0
        _RimThreshold ("Rim Threshold", Range(0,1)) = 0.7
        _RimStrength ("Rim Strength", Range(0,2)) = 1.0

        // Face
        [Toggle]_IsFace ("Is Face", Float) = 0
        _FaceDirection("Face Direction", Vector) = (0,0,1,0)
        _FaceSDFMap ("Face SDF Map", 2D) = "gray" {}
        _FaceShadowStrength ("Face Shadow Strength", Range(-1,1)) = 0.8
        _FaceShadowSoftness ("Face Shadow Softness", Range(0,0.5)) = 0.1

        // Outline
        [Header(Outline Settings)]
        [HDR]_OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineThickness ("Outline Thickness", Range(0.0,0.1)) = 0.03
        [Tooltip("Use smooth normals for outline")]
        _UseSmoothOutline ("Use Smooth Outline", Range(0,1)) = 1.0
        [Tooltip("Adjust outline for non-uniform scale")]
        _OutlineScale ("Outline Scale Compensation", Vector) = (1,1,1)

        _DebugMode ("Debug Mode", Range(0,4)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "RenderPipeline"="UniversalPipeline"
        }

        // Main Pass
        Pass
        {
            Name "Main"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile_instancing
            #pragma target 3.0
            #pragma shader_feature_local _ISFACE_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct appdata
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 tangentOS : TANGENT;
            };

            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float3 tangentWS : TEXCOORD3;
                float3 bitangentWS : TEXCOORD4;
                float3 smoothedNormalWS : TEXCOORD5;
            };

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap); SAMPLER(sampler_NormalMap);
            TEXTURE2D(_Ramp); SAMPLER(sampler_Ramp);
            TEXTURE2D(_AOMap); SAMPLER(sampler_AOMap);
            TEXTURE2D(_FaceSDFMap); SAMPLER(sampler_FaceSDFMap);
            TEXTURE2D(_SpecMap); SAMPLER(sampler_SpecMap);
            TEXTURE2D(_ShadowMask); SAMPLER(sampler_ShadowMask);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _NormalMap_ST;
                float4 _BaseColor;
                float _StepMin;
                float _BaseShadowSoftness;
                float4 _ShadowTint;
                float4 _ShadowTint2;
                float _ShadowDarkness;
                float _ShadowDepth;
                float _DebugMode;
                float _Glossiness;
                float4 _SpecColor;
                float _IsFace;
                float _FaceShadowStrength;
                float _FaceShadowSoftness;
                float4 _FaceSDFMap_ST;
                float4 _FaceDirection;
                float4 _RimColor;
                float _RimPower;
                float _RimThreshold;
                float _RimStrength;
                float _AOIntensity;
            CBUFFER_END

            v2f vert(appdata v)
            {
                v2f o;
                o.positionCS = TransformObjectToHClip(v.positionOS.xyz);
                o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.tangentWS = normalize(TransformObjectToWorldDir(v.tangentOS.xyz));
                o.bitangentWS = cross(o.normalWS, o.tangentWS) * v.tangentOS.w;
                o.uv = TRANSFORM_TEX(v.uv, _BaseMap);

                // 平滑法线：基于面法线计算
                // 在面着色模式下，normalWS已经是面法线
                o.smoothedNormalWS = o.normalWS;

                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                half3 normalWS = normalize(i.normalWS);
                half3 tangentWS = normalize(i.tangentWS);
                half3 bitangentWS = normalize(i.bitangentWS);
                half3 viewDir = normalize(_WorldSpaceCameraPos - i.positionWS);
                Light mainLight = GetMainLight();

                half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
                half4 normalMap = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, i.uv);
                half ao = SAMPLE_TEXTURE2D(_AOMap, sampler_AOMap, i.uv).g;

                half3 normalTS = UnpackNormal(normalMap);
                half3x3 TBN = half3x3(tangentWS, bitangentWS, normalWS);
                half3 finalNormal = normalize(mul(normalTS, TBN));

                half aoEffective = lerp(1.0, ao, saturate(_AOIntensity));
                half NdotL = saturate(dot(finalNormal, mainLight.direction));

                half shadowThreshold = lerp(0.2, 0.6, 1.0 - aoEffective);
                half brightness;

                #if defined(_ISFACE_ON)
                    half3 FaceForward = SafeNormalize(half3(_FaceDirection.x, 0.0, _FaceDirection.z));
                    half3 FaceLight = SafeNormalize(half3(mainLight.direction.x, 0.0, mainLight.direction.z));
                    half FdotL_Face = saturate(dot(FaceForward, FaceLight) * 0.5 + 0.5);
                    half3 crossVec = cross(FaceForward, FaceLight);
                    half FCrossL = crossVec.y;

                    half2 shadowUV = i.uv;
                    half flipMask = step(0.0, FCrossL);
                    shadowUV.x = lerp(shadowUV.x, 1.0 - shadowUV.x, flipMask);

                    half sdf = SAMPLE_TEXTURE2D(_FaceSDFMap, sampler_FaceSDFMap, shadowUV).r;
                    half angleThreshold = 1.0 - saturate(FdotL_Face);
                    half edgeMin = angleThreshold - _FaceShadowSoftness;
                    half edgeMax = angleThreshold + _FaceShadowSoftness;
                    half sdfStep = smoothstep(edgeMin, edgeMax, sdf);
                    half angleBoost = saturate(FdotL_Face * 1.0 + 0.0);
                    sdfStep *= angleBoost;

                    brightness = lerp(_StepMin, 1.0, sdfStep);
                    brightness = lerp(brightness, 1.0, _FaceShadowStrength);
                #else
                    half isLit = step(shadowThreshold, NdotL);
                    isLit = smoothstep(shadowThreshold - _BaseShadowSoftness, shadowThreshold + _BaseShadowSoftness, NdotL);
                    brightness = lerp(_StepMin, 1.0, isLit);
                #endif

                half3 shadowMask = SAMPLE_TEXTURE2D(_ShadowMask, sampler_ShadowMask, i.uv).r;
                half useSecond = step(0.5, shadowMask);
                half3 shadowTintRGB = lerp(_ShadowTint.rgb, _ShadowTint2.rgb, useSecond);
                half3 shadowTarget = lerp(baseColor.rgb, baseColor.rgb * shadowTintRGB, saturate(_ShadowDarkness));
                shadowTarget *= saturate(_ShadowDepth);
                half3 litColor = baseColor.rgb * (_BaseColor.rgb * mainLight.color.rgb);
                half3 toonResult = lerp(shadowTarget, litColor, brightness);

                half3 specMap = SAMPLE_TEXTURE2D(_SpecMap, sampler_SpecMap, i.uv).r;
                half3 specColor = specMap * baseColor.rgb * _SpecColor.rgb;
                specColor *= 1 - smoothstep(specMap - 0.5, specMap + 0.5, NdotL);

                float3 lightDir = normalize(mainLight.direction);
                float3 halfDir = normalize(lightDir + viewDir);
                float rim = 1.0 - saturate(dot(finalNormal, halfDir));
                rim = smoothstep(_RimThreshold - 0.05, _RimThreshold + 0.05, rim);
                rim = pow(rim, _RimPower);
                rim *= saturate(dot(finalNormal, lightDir));
                rim *= smoothstep(shadowThreshold + 0.02, shadowThreshold + 0.18, NdotL);
                rim *= lerp(0.5, 1.0, aoEffective);
                float3 rimColor = _RimColor.rgb * rim * _RimStrength;
                toonResult += rimColor;
                toonResult = toonResult + specColor * pow(saturate(dot(finalNormal, halfDir)), 8.0 + _Glossiness * 32.0);

                if (_DebugMode >= 1.0 && _DebugMode < 2.0) return half4(NdotL.xxx, 1.0);
                if (_DebugMode >= 2.0 && _DebugMode < 3.0) return half4(brightness.xxx, 1.0);
                if (_DebugMode >= 3.0) return half4(normalWS * 0.5 + 0.5, 1.0);

                return half4(toonResult, _BaseColor.a);
            }
            ENDHLSL
        }

        // Outline Pass - Improved with smooth normal support
        Pass
        {
            Name "Outline"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front

            HLSLPROGRAM
            #pragma vertex OutlinePassVertex
            #pragma fragment OutlinePassFragment
            #pragma multi_compile_instancing
            #pragma target 3.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 tangentOS : TANGENT;
            };

            struct v2f_outline
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineThickness;
                float _UseSmoothOutline;
                float4 _OutlineScale;
            CBUFFER_END

            v2f_outline OutlinePassVertex(appdata v)
            {
                v2f_outline o;

                // 获取法线（在模型空间）
                float3 nOS = normalize(v.normalOS);

                // 如果启用平滑描边，尝试使用更平滑的法线
                // 这里我们使用原始法线，因为修复是通过修改网格本身的法线来实现的
                float3 usedNormal = nOS;

                // 应用非均匀缩放补偿
                float3 scaleCompensation = _OutlineScale.xyz;
                float3 scaledNormal = usedNormal / scaleCompensation;
                scaledNormal = normalize(scaledNormal);

                // 顶点沿法线方向偏移
                float3 offsetPosOS = v.positionOS.xyz + scaledNormal * _OutlineThickness;

                o.positionCS = TransformObjectToHClip(offsetPosOS);
                o.normalWS = TransformObjectToWorldNormal(nOS);

                return o;
            }

            half4 OutlinePassFragment(v2f_outline i) : SV_Target
            {
                return half4(_OutlineColor.rgb, 1.0);
            }

            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Simple Lit"
}