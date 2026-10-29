Shader "EmpireAtWar/SpaceHazard"
{
    Properties
    {
        _BaseColor("Core", Color) = (0.525,0.345,0.722,0.35)
        _AccentColor("Edge and highlights", Color) = (0.69,0.31,0.58,1)
        _Ion("Electrical storm", Float) = 0
        _Speed("Drift", Float) = 0.01
        _Feather("Edge feather (m)", Float) = 24
        _WispReach("Wisp reach outside the edge (m)", Float) = 90
        _Stretch("Streak stretch along the flow", Float) = 0.4
        [NoScaleOffset] _Distance("Signed distance (m)", 2D) = "white" {}
        [NoScaleOffset] _Noise("Baked cloud noise", 2D) = "gray" {}
        [NoScaleOffset] _WarpNoise("Baked domain warp", 2D) = "gray" {}
        _FlowDirection("Flow direction (xz)", Vector) = (1,0,0,0)
        _NoiseScale("Noise frequency (1/m)", Float) = 0.004
        _LayerOpacity("Layer opacity", Float) = 1
        _NoiseOffset("Layer offset", Float) = 0
        _Inset("Layer inset (m)", Float) = 0
        _Softness("Layer edge softness", Float) = 1
        _Flash("Flash (x, z, radius, strength)", Vector) = (0,0,1,0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent-50" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; float3 world : TEXCOORD1; };
            TEXTURE2D(_Distance);
            SAMPLER(sampler_Distance);
            TEXTURE2D(_Noise);
            SAMPLER(sampler_Noise);
            TEXTURE2D(_WarpNoise);
            SAMPLER(sampler_WarpNoise);
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor, _AccentColor;
                float4 _FlowDirection, _Flash;
                float _Ion, _Speed, _Feather, _WispReach, _Stretch, _NoiseScale;
                float _LayerOpacity, _NoiseOffset, _Inset, _Softness;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.world = TransformObjectToWorld(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                // The distance texture is the gameplay boundary: the body ends exactly at sdf = 0.
                float sdf = SAMPLE_TEXTURE2D(_Distance, sampler_Distance, input.uv).r + _Inset;
                if (sdf > _WispReach) discard;
                float2 flow = normalize(_FlowDirection.xy);
                float2 p = input.world.xz;
                float2 local = float2(dot(p, flow), dot(p, float2(-flow.y, flow.x)));
                float2 s = local * _NoiseScale * float2(_Stretch, 1) + _NoiseOffset;
                float t = _Time.y * _Speed;
                // Domain warp curls the streaks into filaments that drift along the stream.
                float2 warp = float2(SAMPLE_TEXTURE2D(_WarpNoise, sampler_WarpNoise, (s + float2(t, 0)) / 8).r,
                    SAMPLE_TEXTURE2D(_WarpNoise, sampler_WarpNoise, (s - t * 0.6) / 8).g) - 0.5;
                float2 w = s + warp * 1.8;
                float n = SAMPLE_TEXTURE2D(_Noise, sampler_Noise, (w + float2(t * 0.5, 0)) / 8).r;
                float fine = SAMPLE_TEXTURE2D(_Noise, sampler_Noise, w / 8).g;

                float feather = _Feather * _Softness * (0.4 + n * 1.2);
                float body = saturate(-sdf / feather);
                body = body * body * (3 - 2 * body);
                float depth = saturate(-sdf / (_Feather * 6));
                float wisp = sdf > 0 ? (1 - sdf / _WispReach) * smoothstep(0.6, 0.85, fine) * 0.15 : 0;
                // Strong contrast leaves thin and dense pockets instead of an even wash.
                float density = saturate(n * 1.6 - 0.35 + (fine - 0.5) * 0.7) * lerp(0.55, 1, depth);

                half3 col = lerp(_AccentColor.rgb, _BaseColor.rgb, depth * 0.7) * (0.45 + density * 0.9);
                // Ridged noise peaks on thin lines: crackling filaments in an ion storm.
                float ridge = 1 - abs(SAMPLE_TEXTURE2D(_Noise, sampler_Noise, (w + t * (2 / 1.1)) / 8).b * 2 - 1);
                float filament = pow(saturate(ridge), 18) * smoothstep(0.45, 0.75, fine) * _Ion *
                    (0.6 + 0.4 * sin(_Time.y * 3 + n * 20)) * 2;
                float2 toFlash = p - _Flash.xy;
                float flash = _Flash.w * exp(-dot(toFlash, toFlash) / (_Flash.z * _Flash.z));
                float sparkle = pow(saturate(SAMPLE_TEXTURE2D(_Noise, sampler_Noise, p * (0.09 / 8)).a), 14) * (1 - _Ion) * 1.5;
                col += _AccentColor.rgb * (filament * 1.4 + flash) + sparkle;

                // The floor keeps the gameplay edge readable even where the noise thins out.
                float alpha = body * (0.25 + 0.75 * density) + wisp * (1 - body) + (filament * 0.4 + flash * 0.25) * body;
                return half4(col, saturate(alpha * _BaseColor.a * _LayerOpacity));
            }
            ENDHLSL
        }
    }
}
