Shader "EmpireAtWar/UI/Squadron Frame"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _PlateColor ("Plate Color", Color) = (0.063, 0.102, 0.161, 0.98)
        _EdgeColor ("Plate Edge Color", Color) = (0.184, 0.229, 0.294, 0.98)
        _SelectionColor ("Selection Color", Color) = (1, 1, 1, 1)
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
            // UV1 packs the position relative to the frame centre and the ship frame style.
            struct appdata { float4 vertex : POSITION; fixed4 color : COLOR; float3 frame : TEXCOORD1; };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float4 position : TEXCOORD0;
                float2 halfSize : TEXCOORD1;
                float3 frame : TEXCOORD2;
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
                o.frame = v.frame;
                o.halfSize = abs(v.frame.xy);
                o.color = v.color;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.frame.xy / (2 * i.halfSize) + 0.5;
                float ship = i.frame.z;
                float2 pixel = fwidth(uv);
                float aa = max(pixel.x, pixel.y);

                // Use the same circular corners on the square and wide plates.
                float2 center = float2(0.5, lerp(0.46, 0.5, ship));
                float2 extent = lerp(float2(0.42, 0.42), float2(0.445, 0.405), ship);
                float2 aspect = i.halfSize / min(i.halfSize.x, i.halfSize.y);
                float radius = 0.14;
                float2 edge = (abs(uv - center) - extent) * aspect + radius;
                float plateDistance = length(max(edge, 0)) + min(max(edge.x, edge.y), 0) - radius;
                float platePixels = plateDistance / max(fwidth(plateDistance), 0.00001);
                float plate = saturate(0.5 - platePixels);
                // Vertex alpha carries selection. Keep the team edge visible inside the white outline.
                float selected = step(0.75, i.color.a) * _SelectionColor.a;
                float outlineWidth = 2.0;
                float edgeBlend = saturate(lerp(1.0, 1.75, ship) + platePixels + outlineWidth * selected);
                half4 edgeColor = lerp(_EdgeColor, half4(i.color.rgb, 1), ship);
                half4 plateColor = lerp(_PlateColor, edgeColor, edgeBlend);

                // A single slanted tab carries the team color at every zoom level.
                float tabSlope = (uv.y - 0.855) * 0.58;
                float tabDistance = max(max(0.855 - uv.y, uv.y - 0.965),
                    max(0.09 + tabSlope - uv.x, uv.x - 0.375 - tabSlope));
                float tab = saturate(0.5 - tabDistance / aa) * (1 - ship);

                float plateAlpha = plate * plateColor.a;
                half4 result = lerp(half4(plateColor.rgb * plateAlpha, plateAlpha), half4(i.color.rgb, 1), tab);
                float outline = plate * saturate(outlineWidth + platePixels) * selected;
                result = lerp(result, half4(_SelectionColor.rgb, 1), outline);
                #ifdef UNITY_UI_CLIP_RECT
                result *= UnityGet2DClipping(i.position.xy, _ClipRect);
                #endif
                return result;
            }
            ENDCG
        }
    }
}
