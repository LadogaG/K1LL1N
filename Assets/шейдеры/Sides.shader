Shader "Custom/Sides"
{
    Properties
    {
        _TextureDark ("Dark Texture", 2D) = "white" {}
        _TextureLight ("Light Texture", 2D) = "white" {}
        _ColorDark ("Dark Color", Color) = (0,0,0,1)
        _ColorLight ("Light Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _TextureDark; // Текстура темного цвета
            sampler2D _TextureLight; // Текстура светлого цвета
            fixed4 _ColorDark;
            fixed4 _ColorLight;

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
            };

            v2f vert(appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                // Получаем цвет из текстур
                half4 darkColor = tex2D(_TextureDark, i.uv) * _ColorDark;
                half4 lightColor = tex2D(_TextureLight, i.uv) * _ColorLight;

                return lerp(darkColor, lightColor, i.uv.y); // Линейная интерполяция по Y-координате
            }
            ENDCG
        }
    }
}
