Shader "Custom/Line"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineThickness ("Outline Thickness", Range(0.0, 10)) = 1.0
        _BaseColor ("Base Color", Color) = (1,1,1,1) // Основной цвет
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 100

        // Первый проход для основного цвета
        Pass
        {
            Name "BASE"
            Blend SrcAlpha OneMinusSrcAlpha
            Cull Back
            ZWrite On

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            fixed4 _BaseColor;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, i.uv) * _BaseColor;
                return texColor; // Возвращаем основной цвет
            }
            ENDCG
        }

        // Второй проход для обводки
        Pass
        {
            Name "OUTLINE"
            Blend One OneMinusSrcAlpha
            Cull Front
            ZWrite On
            // Добавим еще один Pass для работы со скайбоксом
            Stencil {
                Ref 1
                Comp Always
                Pass Replace
            }

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float4 _OutlineColor;
            float _OutlineThickness;

            v2f vert(appdata_t v)
            {
                v2f o;
                float3 normal = normalize(v.vertex.xyz);
                // Расширяем вершин в направлении нормалей для обводки
                v.vertex.xyz += normal * _OutlineThickness;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                return _OutlineColor; // Цвет обводки
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
