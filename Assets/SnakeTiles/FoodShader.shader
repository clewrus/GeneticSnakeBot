Shader "SnakeTiles/FoodShader"
{
	Properties {
		_MaxR("Max piece radius", Range(0, 1)) = 0.8
		_FluctuationRate("Rate of fluacuation inside ball.", Range(0, 1)) = 0.5
		_ReferenceColor("Food Piece color", Color) = (0.1, 1, 0.1, 1)
	}

	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue" = "Transparent"}
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
				float2 worldPos : TEXCOORD1;
				float4 vertex : SV_POSITION;
			};

			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.uv = v.uv;
				return o;
			}

			#include "Noise3D.cginc"

			float _MaxR;
			float _FluctuationRate;
			fixed4 _ReferenceColor;

			float2 calcLocalOffset(float2 uv, float seed) {
				float3 arg = float3(uv, frac(_FluctuationRate * _Time.x));

				setSeed(10.2 + 13.567 * seed);
				float dx = noise3D(arg);
				setSeed(11.3 + 24.234 * seed);
				float dy = noise3D(arg);

				float2 offset = 2 * float2(dx, dy) - 1;
				return offset;
			}

			fixed4 frag (v2f i) : SV_Target {
				float2 uv = 2 * (i.uv - 0.5);
				fixed4 col = _ReferenceColor;

				float2 seed2 = floor(i.worldPos);
				seed2.x = frac(dot(seed2, float2(228.322, 322.228)));
				seed2.y = frac(dot(seed2, float2(322.228, 228.322)));
				float seed = frac(seed2.x + seed2.y);
				
				float waveValue = cos(8 * _SinTime.y + 2*3.1416*seed);
				float borderR = _MaxR * (0.95 + 0.05 * waveValue);

				float2 offset = calcLocalOffset(i.uv, seed);
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
