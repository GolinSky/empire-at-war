Shader "EmpireAtWar/UI/Squadron Icon"
{
    Properties
    {
        [PerRendererData] _MainTex ("Icon", 2D) = "white" {}
        _IconScale ("Icon to Silhouette Scale", Float) = 1.0833333333
        _IconOffsetY ("Icon Offset / Height", Float) = 0.0277777778
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "CanUseSpriteAtlas"="False" }
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
            struct appdata { float4 vertex : POSITION; fixed4 color : COLOR; float2 uv : TEXCOORD0; };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 position : TEXCOORD1;
            };
            sampler2D _MainTex;
            float _IconScale;
            float _IconOffsetY;
            float4 _ClipRect;
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.position = v.vertex;
                o.color = v.color;
                o.uv = v.uv;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float shadowAlpha = tex2D(_MainTex,i.uv).a * i.color.a;
                float2 iconUv = (i.uv - 0.5) * _IconScale + 0.5;
                iconUv.y -= _IconOffsetY;
                fixed4 icon = tex2D(_MainTex,iconUv);
                icon.a *= step(0,iconUv.x) * step(iconUv.x,1) * step(0,iconUv.y) * step(iconUv.y,1);
                fixed4 result = fixed4(icon.rgb * icon.a + i.color.rgb * shadowAlpha * (1-icon.a),
                    icon.a + shadowAlpha * (1-icon.a));
                #ifdef UNITY_UI_CLIP_RECT
                result *= UnityGet2DClipping(i.position.xy,_ClipRect);
                #endif
                return result;
            }
            ENDCG
        }
    }
}
