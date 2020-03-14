Shader "SnakeTiles/FoodShader"
{
	Properties {
		_MaxR("Max piece radius", Range(0, 1)) = 0.8
		_FluctuationRate("Rate of fluacuation inside ball.", Range(0, 1)) = 0.5
		_ReferenceColor("Food Piece color", Color) = (0.1, 1, 0.1, 1)
	}

	SubShader
	{
		Tags { "RenderType"="Transparent" }
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			#include "Noise3D.cginc"

			float _MaxR;
			float _FluctuationRate;
			fixed4 _ReferenceColor;

			float2 calcLocalOffset(float2 uv) {
				float3 arg = float3(uv, frac(_FluctuationRate * _Time.x));

				setSeed(23.567);
				float dx = noise3D(arg);
				setSeed(34.234);
				float dy = noise3D(arg);

				float2 offset = 2 * float2(dx, dy) - 1;
				return offset;
			}

			fixed4 frag (v2f i) : SV_Target {
				float2 uv = 2 * (i.uv - 0.5);
				fixed4 col = _ReferenceColor;

				
				float waveValue = cos(8 * _SinTime.y);
				float borderR = _MaxR * (0.95 + 0.05 * waveValue);

				float2 offset = calcLocalOffset(i.uv);
				offset *= max(0, borderR - length(uv));
				float R = length(uv + offset);

				col.rgb *= (1 / max(0.1, R)) - (1 / borderR) * (0.9 + 0.1*waveValue);
				col *= smoothstep(borderR + 0.02, borderR, R);

				return col;
			}
			ENDCG
		}
	}
}
