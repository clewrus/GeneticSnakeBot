Shader "SurroundingTiles/BackgroundShader"
{
	Properties
	{
		_Color ("Color", Color) = (0.01, 0.01, 0.01, 1)
		_VignetteIntencity ("Intencity of Vignette", Range(0, 0.3)) = 0.05
		_VignetteColor ("Color of Vignette", Color) = (0.1, 0.1, 1)
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }

		CGPROGRAM
		#pragma surface surf Lambert
		#pragma target 3.0

		struct Input
		{
			float3 worldPos;
			float4 screenPos;
		};

		fixed4 _Color;
		float _VignetteIntencity;
		fixed3 _VignetteColor;


		float2 rand2(float2 pos, float seed) {
			pos.x = frac(frac(543.35 * pos.x) + 234 * seed * frac(765.23 * pos.y) + 4 * seed);
			pos.y = frac(234 * seed * frac(123.46 * pos.x) + frac(345.43 * pos.y) + 5 * seed);

			pos.x *= dot(pos, float2(13.234, 4.234));
			pos.y *= dot(pos, float2(9.2354, 5.423));

			return frac(pos);
		}

		#define PI (3.1416)

		float particleLayer(float2 pos, float cellSize, float seed) {
			pos /= cellSize;
			float2 cellPos = floor(pos);
			float2 r = rand2(cellPos, seed);

			float t = _Time.x / (10 * cellSize);
			float phi1 = 2 * PI * frac(t + r.x);
			float phi2 = 2 * PI * frac(5*(r.x+r.y) * (t + r.y));
			float radius = sin(phi2);

			float2 uv = 2 * (pos - cellPos) - 1;
			float2 partPos = float2(radius * cos(phi1), radius * sin(phi1));

			partPos = 0.15 + 0.7 * partPos;
			float d = length(uv - partPos);
			float mask = 0.006 * smoothstep(0.2, 0, d) / (max(0.001, d) * cellSize);

			float bSin = sin(2*PI * frac(0.1 * _Time.x + r.x + 2*r.y));
			mask *= smoothstep(-0.4, 0.4, bSin);
			return mask;
		}

		void surf (Input IN, inout SurfaceOutput o) {
			o.Emission = 0;

			float curLayer = particleLayer(IN.worldPos, 15, 228.322);
			o.Emission += 2 * curLayer;

			curLayer = particleLayer(IN.worldPos, 9, 322.228);
			o.Emission += 0.5 * curLayer;

			o.Albedo = _Color.rgb;
			o.Normal = float3(0, 0, -1);

			float2 screenUV = 2 * IN.screenPos.xy / max(0.01, IN.screenPos.w) - 1;
			o.Emission += _VignetteIntencity * _VignetteColor * pow(length(screenUV), 3);
		}
		ENDCG
	}
}
