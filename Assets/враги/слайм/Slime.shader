Shader "Custom/Slime"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Albedo (RGB)", 2D) = "white" {}
        _JiggleAmount ("Jiggle Amount", Range(0, 0.1)) = 0.02
        _JiggleSpeed ("Jiggle Speed", Range(0, 10)) = 3.0
        _JiggleScale ("Jiggle Scale", Range(0, 1)) = 0.2
        _HorizontalStretch ("Horizontal Stretch", Range(0, 1)) = 0.1
        _VerticalStretch ("Vertical Stretch", Range(0, 1)) = 0.1
        _RestSquash ("Rest Squash", Range(0, 0.5)) = 0.3
        _ImpactSquash ("Impact Squash", Range(0, 1)) = 0.4
        _GroundSquash ("Ground Squash", Range(0, 1)) = 0.5
        _WaveFreq1 ("Wave Frequency 1", Float) = 5.0
        _WaveAmp1 ("Wave Amplitude 1", Float) = 0.1
        _WaveDir1 ("Wave Direction 1", Vector) = (1,0,0)
        _WaveFreq2 ("Wave Frequency 2", Float) = 3.0
        _WaveAmp2 ("Wave Amplitude 2", Float) = 0.05
        _WaveDir2 ("Wave Direction 2", Vector) = (0,1,0)
        _WaveFreq3 ("Wave Frequency 3", Float) = 7.0
        _WaveAmp3 ("Wave Amplitude 3", Float) = 0.03
        _WaveDir3 ("Wave Direction 3", Vector) = (0,0,1)
        _RimPower ("Rim Power", Range(1, 10)) = 5.0
        _RimColor ("Rim Color", Color) = (1,1,1,1)
        _Refraction ("Refraction", Range(0, 1)) = 0.1
        _MergeStrength ("Merge Strength", Range(0, 2)) = 1.0
        _MergeDistance ("Merge Distance", Float) = 2.0
        [PerRendererData] _NearbySlimePos1 ("Nearby Slime Pos 1", Vector) = (0,0,0,0)
        [PerRendererData] _NearbySlimePos2 ("Nearby Slime Pos 2", Vector) = (0,0,0,0)
        [PerRendererData] _NearbySlimePos3 ("Nearby Slime Pos 3", Vector) = (0,0,0,0)
        _CurrentSpeed ("Current Speed", Float) = 0
        _HorizontalSpeed ("Horizontal Speed", Float) = 0
        _FallSpeed ("Fall Speed", Float) = 0
        _MoveDir ("Move Direction", Vector) = (0,0,0,0)
        _IsGrounded ("Is Grounded", Float) = 0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200

        GrabPass { "_GrabTexture" }

        CGPROGRAM
        #pragma surface surf Standard alpha:fade fullforwardshadows vertex:vert
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _GrabTexture;
        fixed4 _Color;
        float _JiggleAmount;
        float _JiggleSpeed;
        float _JiggleScale;
        float _HorizontalStretch;
        float _VerticalStretch;
        float _RestSquash;
        float _ImpactSquash;
        float _GroundSquash;
        float _WaveFreq1;
        float _WaveAmp1;
        float3 _WaveDir1;
        float _WaveFreq2;
        float _WaveAmp2;
        float3 _WaveDir2;
        float _WaveFreq3;
        float _WaveAmp3;
        float3 _WaveDir3;
        float _RimPower;
        fixed4 _RimColor;
        float _Refraction;
        float _MergeStrength;
        float _MergeDistance;
        float3 _NearbySlimePos1;
        float3 _NearbySlimePos2;
        float3 _NearbySlimePos3;
        float _CurrentSpeed;
        float _HorizontalSpeed;
        float _FallSpeed;
        float3 _MoveDir;
        float _IsGrounded;

        // Псевдослучайное число
        float rand(float3 co)
        {
            return frac(sin(dot(co, float3(12.9898, 78.233, 45.5432))) * 43758.5453);
        }

        struct Input
        {
            float2 uv_MainTex;
            float3 viewDir;
            float4 screenPos;
        };

        // Вершинный шейдер
        void vert (inout appdata_full v)
        {
            float3 objectMoveDir = mul(unity_WorldToObject, float4(_MoveDir, 0)).xyz;
            float3 up = float3(0, 1, 0);
            float3 vertexPos = v.vertex.xyz;

            // Merge deformation к ближайшим слаймам
            float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
            float3 mergeOffset = float3(0,0,0);
            float mergeFactor = 0;

            // Проверяем до 3 ближайших слаймов
            float3 nearbyPositions[3] = { _NearbySlimePos1, _NearbySlimePos2, _NearbySlimePos3 };
            for (int i = 0; i < 3; i++)
            {
                if (length(nearbyPositions[i]) > 0.01)
                {
                    float3 objNearbyPos = mul(unity_WorldToObject, float4(nearbyPositions[i], 1)).xyz;
                    float dist = distance(vertexPos, objNearbyPos);
                    if (dist < _MergeDistance)
                    {
                        float influence = _MergeStrength * (1.0 - smoothstep(0.0, _MergeDistance, dist));
                        float3 dir = normalize(objNearbyPos - vertexPos);
                        mergeOffset += dir * influence * 0.5; // Усиленный эффект
                        mergeFactor += influence;
                    }
                }
            }

            // Применяем merge offset, усиленный на земле и при движении
            vertexPos += mergeOffset * (1 + _IsGrounded * 0.5 + _CurrentSpeed * 0.3);

            // Horizontal stretch
            if (_HorizontalSpeed > 0.01)
            {
                float3 dir = normalize(objectMoveDir);
                float stretch = _HorizontalSpeed * _HorizontalStretch;
                float proj = dot(vertexPos, dir);
                float heightFactor = max(0, vertexPos.y + 0.5) * 0.5;
                float scale_along = 1.0 + stretch * (1 - heightFactor);
                float3 along = proj * dir;
                float3 perp = vertexPos - along;
                float scale_perp = 1.0 / pow(scale_along, 0.5);
                vertexPos = along * scale_along + perp * scale_perp;
            }

            // Vertical stretch
            if (_FallSpeed > 0.01)
            {
                float stretch = _FallSpeed * _VerticalStretch;
                float3 along = dot(vertexPos, up) * up;
                float3 perp = vertexPos - along;
                float scale_along = 1.0 + stretch;
                float scale_perp = 1.0 / pow(scale_along, 0.5);
                vertexPos = along * scale_along + perp * scale_perp;
            }

            // Rest squash и ground squash
            float squash = _RestSquash + _ImpactSquash;
            if (_IsGrounded > 0.5 && _HorizontalSpeed <= 0.01 && _FallSpeed <= 0.01)
            {
                squash += _GroundSquash;
                float3 along = dot(vertexPos, up) * up;
                float3 perp = vertexPos - along;
                float heightFactor = max(0, -vertexPos.y + 0.5);
                float scale_along = 1.0 - squash * (1 + heightFactor);
                float scale_perp = 1.0 / pow(scale_along, 0.2);
                vertexPos = along * scale_along + perp * scale_perp;
            }
            else if (_HorizontalSpeed <= 0.01 && _FallSpeed <= 0.01)
            {
                float3 along = dot(vertexPos, up) * up;
                float3 perp = vertexPos - along;
                float scale_along = 1.0 - squash * 1.2;
                float scale_perp = 1.0 / pow(scale_along, 0.3);
                vertexPos = along * scale_along + perp * scale_perp;
            }

            // Jiggle с волнами и шумом
            float time = _Time.y * _JiggleSpeed;
            float jiggleIntensity = _CurrentSpeed * _JiggleScale + _JiggleAmount;

            float phase1 = dot(vertexPos, _WaveDir1) * _WaveFreq1 + time;
            float wave1 = sin(phase1) * _WaveAmp1 * jiggleIntensity;

            float phase2 = dot(vertexPos, _WaveDir2) * _WaveFreq2 + time * 1.5;
            float wave2 = sin(phase2) * _WaveAmp2 * jiggleIntensity;

            float phase3 = dot(vertexPos, _WaveDir3) * _WaveFreq3 + time * 0.8;
            float wave3 = sin(phase3) * _WaveAmp3 * jiggleIntensity;

            float3 seed = vertexPos * 2.0 + float3(time * 0.5, time * 0.3, time * 0.7);
            float noise = (rand(seed) - 0.5) * 0.05 * jiggleIntensity;

            float blobFactor = max(0, -vertexPos.y + 0.5);
            float totalOffset = (wave1 + wave2 + wave3 + noise) * (1 + blobFactor * 2.0);
            vertexPos += v.normal * totalOffset * (1 + _IsGrounded * 0.5);

            v.vertex.xyz = vertexPos;
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D(_MainTex, IN.uv_MainTex) * _Color;
            float2 grabUV = (IN.screenPos.xy / IN.screenPos.w);
            float distortion = _Refraction * (sin(_Time.y * _JiggleSpeed) * 0.05 + 0.05);
            grabUV += distortion * (o.Normal.xy);
            fixed4 refr = tex2D(_GrabTexture, grabUV);
            float rim = 1.0 - saturate(dot(normalize(IN.viewDir), o.Normal));
            rim = pow(rim, _RimPower);
            fixed4 rimColor = _RimColor * rim;

            o.Albedo = lerp(refr.rgb, c.rgb, 0.6) + rimColor.rgb;
            o.Alpha = c.a * 0.7 + rim;
            o.Metallic = 0.0;
            o.Smoothness = 0.9;
            o.Emission = rimColor.rgb * 0.5;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
