Shader "Hidden/AfterImage"
{
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

        Blend One OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"

            float4x4 _MvpMatrix;
            sampler2D _ScreenspaceCasters;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = float4(v.uv * 2 - 1, 0, 1);
                o.vertex.y *= -1;
				float4 clipPos = mul(_MvpMatrix, v.vertex);
                o.uv = ComputeScreenPos(clipPos);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
                i.uv.xy /= i.uv.w;
				return tex2D(_ScreenspaceCasters, i.uv.xy);
			}
			ENDCG
		}
	}
}
