Shader "Custom/SpriteOutline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (0,1,0,1)
        _OutlineWidth ("Outline Width (px)", Range(0, 8)) = 1.5
        _OutlineEnabled ("Outline Enabled", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;
            fixed4 _Color;
            fixed4 _OutlineColor;
            float _OutlineWidth;
            float _OutlineEnabled;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            // Простая 4-семпловая проверка соседних пикселей по альфе:
            // если сам пиксель прозрачный, но рядом (по краю спрайта) есть непрозрачный —
            // рисуем цвет обводки вместо него. Дёшево и достаточно для 2D-спрайтов прототипа.
            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;

                if (_OutlineEnabled > 0.5 && c.a < 0.9)
                {
                    float2 texel = _MainTex_TexelSize.xy * _OutlineWidth;

                    float neighborAlpha = 0;
                    neighborAlpha += tex2D(_MainTex, IN.texcoord + float2(texel.x, 0)).a;
                    neighborAlpha += tex2D(_MainTex, IN.texcoord + float2(-texel.x, 0)).a;
                    neighborAlpha += tex2D(_MainTex, IN.texcoord + float2(0, texel.y)).a;
                    neighborAlpha += tex2D(_MainTex, IN.texcoord + float2(0, -texel.y)).a;

                    if (neighborAlpha > 0)
                    {
                        c = _OutlineColor;
                        c.a = 1;
                    }
                }

                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}
