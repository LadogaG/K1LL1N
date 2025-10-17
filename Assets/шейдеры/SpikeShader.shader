Shader "Custom/SpikeShader"
{
    Properties
    {
        _MainTex ("Albedo (RGB)", 2D) = "white" {} // Основная текстура
        _Color ("Color", Color) = (1,1,1,1) // Цвет шипов
        _ShrinkAmount ("Shrink Amount", Range(0, 1)) = 0.5 // Уменьшение объекта (0 - без изменений, 1 - максимальное сжатие)
        _SpikeHeight ("Spike Height", Range(0, 2)) = 0.5 // Высота шипов
        _SpikeFrequency ("Spike Frequency", Range(1, 20)) = 5 // Частота шипов (как часто они появляются)
        _SpikeSharpness ("Spike Sharpness", Range(0.1, 5)) = 1 // Острота шипов
        _Glossiness ("Smoothness", Range(0, 1)) = 0.5 // Глянцевость
        _Metallic ("Metallic", Range(0, 1)) = 0.0 // Металличность
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        // Используем стандартный pipeline с поддержкой вершинного смещения
        #pragma surface surf Standard fullforwardshadows vertex:vert
        #pragma target 3.0

        sampler2D _MainTex;
        fixed4 _Color;
        float _ShrinkAmount;
        float _SpikeHeight;
        float _SpikeFrequency;
        float _SpikeSharpness;
        half _Glossiness;
        half _Metallic;

        struct Input
        {
            float2 uv_MainTex;
        };

        // Вершинный шейдер для изменения геометрии
        void vert(inout appdata_full v)
        {
            // 1. Уменьшение объекта: смещаем вершины ближе к центру
            float3 center = float3(0, 0, 0); // Предполагаем, что центр объекта в (0,0,0) в локальных координатах
            float3 directionToCenter = normalize(center - v.vertex.xyz);
            v.vertex.xyz += directionToCenter * _ShrinkAmount;

            // 2. Добавление шипов: используем синусоиду для симметричного эффекта
            // Вычисляем "шум" на основе позиции вершины для создания шипов
            float spikeNoise = sin(v.vertex.x * _SpikeFrequency) * sin(v.vertex.y * _SpikeFrequency) * sin(v.vertex.z * _SpikeFrequency);
            // Применяем остроту: делаем шипы более выраженными
            spikeNoise = pow(abs(spikeNoise), _SpikeSharpness) * sign(spikeNoise);
            // Смещаем вершины наружу вдоль нормали для создания шипов
            v.vertex.xyz += v.normal * spikeNoise * _SpikeHeight;
        }

        // Поверхностный шейдер для текстуры и освещения
        void surf(Input IN, inout SurfaceOutputStandard o)
        {
            // Применяем текстуру и цвет
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            o.Albedo = c.rgb;
            o.Metallic = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha = c.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}