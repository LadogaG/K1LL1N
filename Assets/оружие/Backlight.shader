Shader "Custom/Backlight"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _OverlayTex ("Overlay Texture", 2D) = "white" {}
        _OverlayIntensity ("Overlay Intensity", Range(0, 1)) = 0.5
        _BlinkSpeed ("Blink Speed", Float) = 1.0
        _MinOverlayStrength ("Min Overlay Strength", Range(0, 1)) = 0.0
        _MaxOverlayStrength ("Max Overlay Strength", Range(0, 1)) = 1.0
        _GradientTex ("Gradient Texture", 2D) = "white" {}
        _GradientIntensity ("Gradient Intensity", Range(0, 1)) = 0.5
        _GradientSpeed ("Gradient Speed", Float) = 1.0
        _NormalMap ("Normal Map", 2D) = "bump" {}
        _NormalScale ("Normal Scale", Range(0, 1)) = 1.0
        _Metallic ("Metallic", Range(0, 1)) = 0.0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _OverlayTex;
        sampler2D _GradientTex;
        sampler2D _NormalMap;
        float _OverlayIntensity;
        float _BlinkSpeed;
        float _MinOverlayStrength;
        float _MaxOverlayStrength;
        float _GradientIntensity;
        float _GradientSpeed;
        float _NormalScale;
        float _Metallic;
        float _Smoothness;

        struct Input
        {
            float2 uv_MainTex;
            float2 uv_NormalMap;
        };

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 mainTex = tex2D(_MainTex, IN.uv_MainTex);
            float blink = (sin(_Time.y * _BlinkSpeed) + 1) * 0.5;
            fixed4 overlayTex = tex2D(_OverlayTex, IN.uv_MainTex);
            float overlayStrength = lerp(_MinOverlayStrength, _MaxOverlayStrength, blink * _OverlayIntensity);
            float2 gradUV = IN.uv_MainTex + float2(_Time.y * _GradientSpeed, 0);
            fixed4 gradientColor = tex2D(_GradientTex, gradUV);
            gradientColor = lerp(fixed4(1, 1, 1, 1), gradientColor, _GradientIntensity);
            fixed4 overlayColored = overlayTex * gradientColor;
            fixed4 finalColor = lerp(mainTex, overlayColored, overlayStrength * overlayTex.a);
            o.Albedo = finalColor.rgb;
            o.Alpha = finalColor.a;
            fixed3 normal = UnpackNormal(tex2D(_NormalMap, IN.uv_NormalMap));
            o.Normal = lerp(fixed3(0, 0, 1), normal, _NormalScale);
            o.Metallic = _Metallic;
            o.Smoothness = _Smoothness;
        }
        ENDCG
    }
    FallBack "Diffuse"
}