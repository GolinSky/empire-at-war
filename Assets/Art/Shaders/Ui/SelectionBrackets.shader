Shader "EmpireAtWar/UI/Selection Brackets"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Bracket Color", Color) = (0.63, 0.89, 1, 1)
        _LineWidth ("Line Width (Pixels)", Float) = 1.5
        _OutlineWidth ("Dark Edge (Pixels)", Float) = 0.75
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; fixed4 color : COLOR; };
            fixed4 _Color;
            float _LineWidth;
            float _OutlineWidth;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Measure each axis in screen pixels so zoom and compact markers keep the same stroke.
                float2 pixel = max(float2(length(float2(ddx(i.uv.x), ddy(i.uv.x))),
                    length(float2(ddx(i.uv.y), ddy(i.uv.y)))), 0.00001);
                float2 edge = min(i.uv, 1 - i.uv);
                float2 p = (edge - 0.08) / pixel;
                float2 arm = 0.14 / pixel;
                float horizontal = max(abs(p.y), max(-p.x, p.x - arm.x));
                float vertical = max(abs(p.x), max(-p.y, p.y - arm.y));
                float distance = min(horizontal, vertical);
                float ink = saturate(_LineWidth * 0.5 + 0.5 - distance);
                float outline = saturate(_LineWidth * 0.5 + _OutlineWidth + 0.5 - distance);
                fixed3 color = lerp(fixed3(0.025, 0.045, 0.07), i.color.rgb, ink);
                return fixed4(color, outline * i.color.a);
            }
            ENDCG
        }
    }
}
