Shader "EmpireAtWar/UI/Squadron Frame"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _CornerRadius ("Corner Radius", Float) = 3
        _BorderWidth ("Border Width", Float) = 1.5
        _BackgroundAlpha ("Background Alpha", Range(0,1)) = 0.5
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend One OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata { float4 vertex : POSITION; fixed4 color : COLOR; };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float4 position : TEXCOORD0;
                float2 halfSize : TEXCOORD1;
            };
            float _CornerRadius;
            float _BorderWidth;
            float _BackgroundAlpha;
            float4 _ClipRect;
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.position = v.vertex;
                o.halfSize = abs(v.vertex.xy);
                o.color = v.color;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float2 q = abs(i.position.xy) - i.halfSize + _CornerRadius;
                float distance = length(max(q,0)) + min(max(q.x,q.y),0) - _CornerRadius;
                float fill = saturate(-distance) * _BackgroundAlpha;
                float halfStrip = (_BorderWidth + 1) * 0.5;
                float border = saturate(halfStrip - abs(-distance - halfStrip)) * i.color.a;
                // Composite the existing border over the black translucent background.
                fixed4 result = fixed4(i.color.rgb * border, border + fill * (1 - border));
                #ifdef UNITY_UI_CLIP_RECT
                result *= UnityGet2DClipping(i.position.xy, _ClipRect);
                #endif
                return result;
            }
            ENDCG
        }
    }
}
