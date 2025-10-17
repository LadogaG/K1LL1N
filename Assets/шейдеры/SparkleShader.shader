Shader "Custom/SparkleShader"
{
    Properties
    {
        _MainTex ("Particle Texture", 2D) = "white" {} // Текстура частиц (например, круг с мягкими краями)
        _TintColor ("Tint Color", Color) = (1,1,1,1) // Цвет искр (светло-белый)
        _InvFade ("Soft Particles Factor", Range(0.01, 3.0)) = 1.0 // Затухание на расстоянии
    }
    Category
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask RGB
        Cull Off Lighting Off ZWrite Off

        SubShader
        {
            Pass
            {
                CGPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma multi_compile_particles
                #pragma multi_compile_fog

                #include "UnityCG.cginc"

                sampler2D _MainTex;
                fixed4 _TintColor;
                float _InvFade;

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
                    UNITY_FOG_COORDS(1)
                    #ifdef SOFTPARTICLES_ON
                    float4 projPos : TEXCOORD2;
                    #endif
                };

                v2f vert(appdata_t v)
                {
                    v2f o;
                    o.vertex = UnityObjectToClipPos(v.vertex);
                    o.color = v.color * _TintColor;
                    o.texcoord = v.texcoord;
                    UNITY_TRANSFER_FOG(o, o.vertex);
                    #ifdef SOFTPARTICLES_ON
                    o.projPos = ComputeScreenPos(o.vertex);
                    COMPUTE_EYEDEPTH(o.projPos.z);
                    #endif
                    return o;
                }

                sampler2D _CameraDepthTexture;
                float4 frag(v2f i) : SV_Target
                {
                    #ifdef SOFTPARTICLES_ON
                    float sceneZ = LinearEyeDepth(UNITY_SAMPLE_DEPTH(tex2Dproj(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos))));
                    float partZ = i.projPos.z;
                    float fade = saturate(_InvFade * (sceneZ - partZ));
                    i.color.a *= fade;
                    #endif

                    fixed4 col = 2.0f * i.color * tex2D(_MainTex, i.texcoord);
                    col.a = saturate(col.a); // Убеждаемся, что альфа не превышает 1
                    UNITY_APPLY_FOG(i.fogCoord, col);
                    return col;
                }
                ENDCG
            }
        }
    }
}