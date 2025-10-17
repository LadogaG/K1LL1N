Shader "Custom/ElectricShader"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {} // Основная текстура
        _BaseColor ("Base Color", Color) = (1,1,1,1) // Базовый цвет объекта
        _ElectricColor ("Electric Color", Color) = (0,0.5,1,1) // Цвет электричества (голубо-синий)
        _AuraDistance ("Aura Distance", Range(0.1, 1)) = 0.3 // Расстояние ауры от объекта
        _ElectricSpeed ("Electric Speed", Range(0, 5)) = 1 // Скорость движения полос
        _ElectricFrequency ("Electric Frequency", Range(1, 20)) = 5 // Частота полос
        _ElectricIntensity ("Electric Intensity", Range(0, 5)) = 1 // Интенсивность свечения
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry+1" }
        LOD 300

        // Первый проход: рендеринг базового объекта
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            sampler2D _MainTex;
            float4 _BaseColor;
            float4 _ElectricColor;
            float _AuraDistance;
            float _ElectricSpeed;
            float _ElectricFrequency;
            float _ElectricIntensity;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 normal : TEXCOORD2;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Базовый цвет с текстурой
                fixed4 col = tex2D(_MainTex, i.uv) * _BaseColor;

                // Простое освещение
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float diff = max(0, dot(i.normal, lightDir));
                col.rgb *= diff * _LightColor0.rgb + UNITY_LIGHTMODEL_AMBIENT.rgb;
                return col;
            }
            ENDCG
        }

        // Второй проход: электрическая аура с Geometry Shader
        Pass
        {
            Tags { "Queue"="Transparent" "RenderType"="Transparent" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #pragma target 4.0

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _BaseColor;
            float4 _ElectricColor;
            float _AuraDistance;
            float _ElectricSpeed;
            float _ElectricFrequency;
            float _ElectricIntensity;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2g
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                float3 worldPos : TEXCOORD1;
            };

            struct g2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float electricGlow : TEXCOORD2;
            };

            v2g vert(appdata v)
            {
                v2g o;
                o.vertex = v.vertex;
                o.uv = v.uv;
                o.normal = v.normal;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            [maxvertexcount(6)]
            void geom(triangle v2g IN[3], inout TriangleStream<g2f> triStream)
            {
                g2f o;
                float3 normal = normalize(cross(IN[1].vertex - IN[0].vertex, IN[2].vertex - IN[0].vertex));
                float3 offset = normal * _AuraDistance;

                for (int i = 0; i < 3; i++)
                {
                    // Оригинальная вершина
                    o.pos = UnityObjectToClipPos(IN[i].vertex);
                    o.uv = IN[i].uv;
                    o.worldPos = IN[i].worldPos;
                    float electricPattern = sin(o.worldPos.y * _ElectricFrequency + _Time.y * _ElectricSpeed);
                    electricPattern = pow(abs(electricPattern), 2);
                    o.electricGlow = saturate(electricPattern * _ElectricIntensity);
                    triStream.Append(o);

                    // Вершина на расстоянии (аура)
                    o.pos = UnityObjectToClipPos(IN[i].vertex + float4(offset, 0));
                    o.worldPos = mul(unity_ObjectToWorld, IN[i].vertex + float4(offset, 0)).xyz;
                    electricPattern = sin(o.worldPos.y * _ElectricFrequency + _Time.y * _ElectricSpeed);
                    electricPattern = pow(abs(electricPattern), 2);
                    o.electricGlow = saturate(electricPattern * _ElectricIntensity);
                    triStream.Append(o);
                }
                triStream.RestartStrip();
            }

            fixed4 frag(g2f i) : SV_Target
            {
                fixed4 col = _ElectricColor;
                col.a = i.electricGlow;
                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
