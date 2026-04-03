Shader "Custom/Toon_Standard"
{
    Properties
    {   //AO通道为G，specular通道为R
        _BaseMap ("Albedo", 2D) = "white" {}
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _BaseColor ("Color", Color) = (1,1,1,1)
        _Ramp ("Toon Ramp", 2D) = "white" {}
        _AOMap("AO Map", 2D) = "white" {}
        _SpecMap("Specular Map", 2D) = "white" {}
        [HDR]_SpecColor ("Specular Color", Color) = (1,1,1,1)
        _Glossiness("Glossiness", Range(0,1)) = 0.5

        _StepMin ("Step Min Brightness", Range(0,1)) = 0.25
        _ShadowTint ("Shadow Tint", Color) = (0.15,0.15,0.18,1)
        _ShadowTint2 ("Shadow Tint2", Color) = (0.15,0.15,0.18,1)
        _ShadowDarkness ("Shadow Darkness", Range(0,5)) = 0.8
        _ShadowDepth ("Shadow Depth", Range(0,5)) = 0.5
        _BaseShadowSoftness ("Base Shadow Softness", Range(0.001,0.5)) = 0.25

        _ShadowMask("ShadowMask" ,2D) = "blue"{}
       

        // === Rim Light 参数 ===
        [HDR]_RimColor ("Rim Color", Color) = (1,1,1,1)
        _RimPower ("Rim Power", Range(0.1,8)) = 2.0
        _RimThreshold ("Rim Threshold", Range(0,1)) = 0.7
        _RimStrength ("Rim Strength", Range(0,2)) = 1.0

        // === AO 影响程度 ===
        _AOIntensity ("AO Intensity", Range(0,2)) = 1.0

        _DebugMode ("Debug Mode", Range(0,2)) = 0

        // === Face Shadow (SDF 阴影) ===
        [Toggle]_IsFace ("Is Face", Float) = 0
        _FaceDirection("Face Direction", Vector) = (0,0,1,0)
        _FaceSDFMap ("Face SDF Map", 2D) = "gray" {}
        _FaceShadowStrength ("Face Shadow Strength", Range(-1,1)) = 0.8
        _FaceShadowSoftness ("Face Shadow Softness", Range(0,0.5)) = 0.1


        //outline
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineThickness ("Outline Thickness", Range(0.0,0.1)) = 0.03
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "RenderPipeline"="UniversalPipeline"
        }

        Cull Off
        ZWrite On
        Blend Off

        Pass
        {
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
          
            };

            TEXTURE2D(_BaseMap);          SAMPLER(sampler_BaseMap);
            TEXTURE2D(_NormalMap);        SAMPLER(sampler_NormalMap);
            TEXTURE2D(_Ramp);             SAMPLER(sampler_Ramp);
            TEXTURE2D(_AOMap);            SAMPLER(sampler_AOMap);
            TEXTURE2D(_FaceSDFMap);       SAMPLER(sampler_FaceSDFMap);
            TEXTURE2D(_SpecMap);          SAMPLER(sampler_SpecMap);
            TEXTURE2D(_ShadowMask);          SAMPLER(sampler_ShadowMask);


            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float4 _NormalMap_ST;
                float4 _BaseColor;
                float4 _AOMap_ST;
                float _StepMin;
                float4 _ShadowTint;
                float4 _ShadowTint2;
                float _ShadowDarkness;
                float _ShadowDepth;
                float _DebugMode;

                // Specular
                float _Glossiness;
                float4 _SpecColor;

                // Face Shadow
                float _IsFace;
                float _FaceShadowStrength;
                float _FaceShadowSoftness;
                float4 _FaceSDFMap_ST;
                float _BaseShadowSoftness;
                float4 _FaceDirection;


                // Rim Light
                float4 _RimColor;
                float _RimPower;
                float _RimThreshold;
                float _RimStrength;

                // AO
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
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
            half3 normalWS = normalize(i.normalWS);
            half3 tangentWS = normalize(i.tangentWS);
            half3 bitangentWS = normalize(i.bitangentWS);
            half3 viewDir = normalize(_WorldSpaceCameraPos - i.positionWS);
            Light mainLight = GetMainLight();

            // 采样
            half4 baseColor = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);
            half4 normalMap = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, i.uv);
            half ao = SAMPLE_TEXTURE2D(_AOMap, sampler_AOMap, i.uv).g;

            //法线贴图转换
            half3 normalTS = UnpackNormal(normalMap);
            half3x3 TBN = half3x3(tangentWS, bitangentWS, normalWS);
            half3 finalNormal = normalize(mul(normalTS, TBN));

            //AO
            half aoEffective = lerp(1.0, ao, saturate(_AOIntensity));
            half NdotL = saturate(dot(finalNormal, mainLight.direction));

            // 阴影阈值 
            half shadowThreshold = lerp(0.2, 0.6, 1.0 - aoEffective);

            // Toon Step
            half brightness;

            #if defined(_ISFACE_ON)

            // XZ 平面上面部朝向和光线方向
            half3 FaceForward = SafeNormalize(half3(_FaceDirection.x, 0.0, _FaceDirection.z));//(0,0,1)
            half3 FaceLight = SafeNormalize(half3(mainLight.direction.x, 0.0, mainLight.direction.z));

            // 面向光线的余弦（-1..1）
            half FdotL_Face = saturate(dot(FaceForward, FaceLight) * 0.5 + 0.5); 

            // 根据光线左右决定是否翻转 UV
            half3 crossVec = cross(FaceForward, FaceLight);
            half FCrossL = crossVec.y; // >0 表示光在某一侧，若出现镜像，使用 -FCrossL

            half2 shadowUV = i.uv;
            half flipMask = step(0.0, FCrossL); // 0 or 1
            shadowUV.x = lerp(shadowUV.x, 1.0 - shadowUV.x, flipMask);

            // 采样 SDF，SDF 越大越接近亮区
            half sdf = SAMPLE_TEXTURE2D(_FaceSDFMap, sampler_FaceSDFMap, shadowUV).r;

            //  将 FdotL_Face 映射为阈值（0..1），并用 smoothstep 做软过渡
            // 光照角度决定的阈值（th）放在 [0,1]，并用 _FaceShadowSoftness 控制软
            //half angleThreshold = lerp(0.0, 1.0, FdotL_Face); // 当面朝光，阈值更低更容易被认定为亮
            half angleThreshold = 1.0 - saturate(FdotL_Face);

            //_FaceShadowSoftness 控制在 0.01 - 0.2 区间
            half edgeMin = angleThreshold - _FaceShadowSoftness;
            half edgeMax = angleThreshold + _FaceShadowSoftness;
            half sdfStep = smoothstep(edgeMin, edgeMax, sdf); // 越接近 1 越亮

            //微调：让光照角度对强度有额外影响
            // 这个乘子确保在极端侧光/背光时进一步衰减
            half angleBoost = saturate(FdotL_Face * 1.0 + 0.0); // 可做 bias/scale 调整
            sdfStep *= angleBoost;

            // 最终亮度
            brightness = lerp(_StepMin, 1.0, sdfStep);
            brightness = lerp(brightness, 1.0, _FaceShadowStrength);

       

            #else
                //Toon Step 
                half isLit = step(shadowThreshold, NdotL);
                isLit = smoothstep(shadowThreshold - _BaseShadowSoftness, shadowThreshold + _BaseShadowSoftness, NdotL);
                // _StepMin 控制最暗亮度
                brightness = lerp(_StepMin, 1.0, isLit);
            #endif

            // shadow color
            half3 shadowMask = SAMPLE_TEXTURE2D(_ShadowMask, sampler_ShadowMask, i.uv).r;
            half useSecond = step(0.5, shadowMask); // mask ≥ 0.5 时为 1，否则为 0
            half3 shadowTintRGB = lerp(_ShadowTint.rgb, _ShadowTint2.rgb, useSecond);

            //half3 shadowTintRGB = _ShadowTint.rgb * (shadowMask - 1) + _ShadowTint2.rgb * shadowMask;
            //half3 shadowTintRGB = _ShadowTint.rgb + _ShadowTint2.rgb;
            half3 shadowTarget = lerp(baseColor.rgb, baseColor.rgb * shadowTintRGB, saturate(_ShadowDarkness));
            shadowTarget *= saturate(_ShadowDepth);
            half3 litColor = baseColor.rgb * (_BaseColor.rgb * mainLight.color.rgb);
            half3 toonResult = lerp(shadowTarget, litColor, brightness);

            // Specular
            half3 specMap = SAMPLE_TEXTURE2D(_SpecMap, sampler_SpecMap, i.uv).r;
            half3 specColor = specMap * baseColor.rgb * _SpecColor.rgb;
            specColor *= 1 - smoothstep(specMap - 0.5, specMap + 0.5, NdotL);
            //specColor = speccolor * (1-smoothstep(specMap - 0.5 , specMap +0.5 , NdotL))

            // Rim Light 
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
            

            // Debug
            if (_DebugMode >= 1.0 && _DebugMode < 2.0) return half4(NdotL.xxx, 1.0);
            if (_DebugMode >= 2.0) return half4(brightness.xxx, 1.0);

            return half4(toonResult, _BaseColor.a);
    }

            ENDHLSL
        }


        // Outline Pass
        Pass
        {   
            Name "OutLine"
            Tags { "LightMode" ="SRPDefaultUnlit" }
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
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineThickness;
            CBUFFER_END

            v2f_outline OutlinePassVertex(appdata v)
            {
                v2f_outline o;

                // 使用 Object-space 的法线偏移顶点
                // 若模型有非均匀缩放，考虑将法线转换到世界空间并按世界方向偏移
                float3 nOS = normalize(v.normalOS);
                float3 offsetPosOS = v.positionOS.xyz + nOS * _OutlineThickness; // _OutlineThickness 为 Object-space 单位

                o.positionCS = TransformObjectToHClip(offsetPosOS);
                return o;
            }

            half4 OutlinePassFragment() : SV_Target
            {
                return half4(_OutlineColor.rgb, 1.0);
            }

            ENDHLSL
        }
    }

    //FallBack "Universal Render Pipeline/Simple Lit"
}
