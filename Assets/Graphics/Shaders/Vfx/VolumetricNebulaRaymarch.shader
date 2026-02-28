Shader "Custom/Vfx/VolumetricNebulaRaymarch"
{
    Properties
    {
        [Header(Main Settings)]
        _MainColor ("Nebula Color (Main)", Color) = (0.3, 0.6, 1.0, 1.0)
        _CoreColor ("Nebula Core Color", Color) = (0.7, 0.9, 1.0, 1.0)
        _NoiseTex3D ("3D Noise (Texture3D)", 3D) = "gray" {}
        
        [Header(Volume Settings)]
        _DensityMultiplier ("Density Multiplier", Range(0, 10)) = 5.0
        _StepSize ("Ray Step Size", Range(0.01, 0.2)) = 0.05
        _EdgeSoftness ("Edge Softness", Range(0, 1)) = 0.8
        _Scale ("Noise Scale", Range(0.1, 10)) = 2.0
        
        [Header(Animation Settings)]
        _Speed ("Animation Speed", Float) = 0.5
        _ScrollDir ("Scroll Direction", Vector) = (0.2, 0.5, 0.1, 0)
        
        [Header(Rendering)]
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Source Blend", Float) = 5 // SrcAlpha
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dest Blend", Float) = 1 // One
        [Enum(UnityEngine.Rendering.CullMode)] _Cull ("Culling", Float) = 0 // Off
        [Enum(Off, 0, On, 1)] _ZWrite ("Z Write", Float) = 0 // Off
    }
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "RenderPipeline"="UniversalPipeline" 
            "IgnoreProjector"="True" 
        }
        
        Pass
        {
            Name "ForwardLit"
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            Cull [_Cull]
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR; // Particle Color
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float3 positionWS : TEXCOORD1;
            };

            TEXTURE3D(_NoiseTex3D);
            SAMPLER(sampler_NoiseTex3D);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainColor;
                float4 _CoreColor;
                float _DensityMultiplier;
                float _StepSize;
                float _EdgeSoftness;
                float _Scale;
                float _Speed;
                float3 _ScrollDir;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(OUT.positionWS);
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }

            // Pseudo-random function for jittering
            float rand(float2 co)
            {
                return frac(sin(dot(co.xy ,float2(12.9898,78.233))) * 43758.5453);
            }

            float4 frag(Varyings IN) : SV_Target
            {
                // We're rendering a quad particle. Remap UVs to centered coordinates for a sphere map
                float2 uvCentered = IN.uv * 2.0 - 1.0; 
                float r2 = dot(uvCentered, uvCentered);

                // Outside the circle? throw it away so it's a perfect sphere
                if (r2 > 1.0) discard; 

                // Z-thickness of sphere at this point
                float zThickness = sqrt(1.0 - r2);
                
                // Set up local ray
                float3 rayStart = float3(uvCentered, -zThickness);
                float3 rayEnd = float3(uvCentered, zThickness);
                float3 rayDir = float3(0,0,1); // Moving through the quad linearly
                
                // Number of volume steps to take
                int maxSteps = 30; // Maximum steps bounded
                float actualSteps = (zThickness * 2.0) / max(0.01, _StepSize);
                int steps = min(maxSteps, (int)ceil(actualSteps));
                if (steps <= 0) return float4(0,0,0,0);
                
                float stepZ = (zThickness * 2.0) / steps;
                
                // Jitter ray starting position to smooth out banding artifacts
                float jitter = rand(IN.uv + _Time.y) * stepZ;
                float3 currentPos = rayStart + rayDir * jitter;
                
                // Core accumulation variables
                float accumDensity = 0.0;
                float3 accumColor = float3(0,0,0);
                float transmittance = 1.0;

                // Time offsets for scrolling the nebula shape
                float3 timeOffset = _ScrollDir * (_Time.y * _Speed);

                UNITY_LOOP
                for(int i = 0; i < steps; i++)
                {
                    // Map volume pos (-1 to 1) to (0 to 1) UV space for texture 
                    float3 samplePos = (currentPos * 0.5 + 0.5) * _Scale; 
                    samplePos += timeOffset; // Animation
                    
                    // Sample raw density
                    float rawDensity = SAMPLE_TEXTURE3D_LOD(_NoiseTex3D, sampler_NoiseTex3D, samplePos, 0).r;
                    
                    // Fade out volume density from center to sphere edge
                    float distFromCenter = length(currentPos);
                    float edgeMask = 1.0 - smoothstep(1.0 - _EdgeSoftness, 1.0, distFromCenter);
                    
                    float density = rawDensity * edgeMask * _DensityMultiplier * stepZ;
                    
                    // Simple Beer-Lambert absorption
                    float dx = exp(-density);
                    
                    // Color progression (hot center, cool edges)
                    float colorFactor = saturate(rawDensity * 1.5 - distFromCenter);
                    float3 stepColor = lerp(_MainColor.rgb, _CoreColor.rgb, colorFactor);
                    
                    // Add to total accumulation 
                    accumColor += stepColor * density * transmittance;
                    accumDensity += density * transmittance;
                    
                    transmittance *= dx;
                    
                    // Early exit if totally opaque
                    if (transmittance < 0.01) break; 
                    
                    currentPos += rayDir * stepZ;
                }

                // Final alpha blend based on total density gathered
                float alpha = saturate(accumDensity);
                
                // Optional: apply particle alpha and color modifiers
                float3 finalRGB = accumColor * IN.color.rgb;
                float finalAlpha = alpha * IN.color.a * _MainColor.a;

                return float4(finalRGB, finalAlpha);
            }
            ENDHLSL
        }
    }
}
