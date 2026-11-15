Shader "EmpireAtWar/UI/Squadron Frame"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _PlateColor ("Plate Color", Color) = (0.107, 0.125, 0.153, 0.94)
        _EdgeColor ("Plate Edge Color", Color) = (0.247, 0.282, 0.333, 0.82)
        _SelectionColor ("Selection Color", Color) = (0.949, 0.941, 0.902, 1)
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
            // UV1 = vertex position relative to the frame centre (SquadronFrameMeshEffect): markers share one canvas,
            // so the canvas-space vertex is not centred on the marker.
            struct appdata { float4 vertex : POSITION; fixed4 color : COLOR; float2 local : TEXCOORD1; };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float4 position : TEXCOORD0;
                float2 halfSize : TEXCOORD1;
                float2 local : TEXCOORD2;
            };
            half4 _PlateColor;
            half4 _EdgeColor;
            half4 _SelectionColor;
            float4 _ClipRect;
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.position = v.vertex;
                o.local = v.local;
                o.halfSize = abs(v.local);
                o.color = v.color;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.local / (2 * i.halfSize) + 0.5;
                float2 pixel = fwidth(uv);
                float aa = max(pixel.x, pixel.y);

                // Clip the plate corners and keep its neutral edge one screen pixel wide.
                float2 edge = abs(uv - float2(0.5, 0.465)) - float2(0.445, 0.365);
                float plateDistance = max(max(edge.x, edge.y), edge.x + edge.y + 0.075);
                float plate = saturate(0.5 - plateDistance / aa);
                float edgeBlend = saturate(1 + plateDistance / aa);
                half4 plateColor = lerp(_PlateColor, _EdgeColor, edgeBlend);
                plate *= plateColor.a;

                // A single slanted tab carries the team color at every zoom level.
                float tabSlope = (uv.y - 0.83) * 0.58;
                float tabDistance = max(max(0.83 - uv.y, uv.y - 0.965),
                    max(0.065 + tabSlope - uv.x, uv.x - 0.35 - tabSlope));
                float tab = saturate(0.5 - tabDistance / aa);

                // Frame vertex alpha carries selection, leaving the ownership tab unchanged.
                float2 corner = 0.5 - abs(uv - 0.5);
                float2 arms = max(corner - float2(0.15, 0.15), abs(corner.yx - 0.03) - 0.014);
                float bracket = min(arms.x, arms.y);
                float selection = saturate(0.5 - bracket / aa) * step(0.75, i.color.a) * _SelectionColor.a;

                half4 result = lerp(half4(plateColor.rgb * plate, plate), half4(i.color.rgb, 1), tab);
                result = lerp(result, half4(_SelectionColor.rgb, 1), selection);
                #ifdef UNITY_UI_CLIP_RECT
                result *= UnityGet2DClipping(i.position.xy, _ClipRect);
                #endif
                return result;
            }
            ENDCG
        }
    }
}
