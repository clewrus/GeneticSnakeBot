Shader "SurroundingTiles/WallShader"
{
	Properties {
		_Orientation ("Codded wall orientation", Int) = 30292
		_OffsetWidth ("Offset from the edge", Range(0, 1)) = 0.35
		_CornerR ("Radius of corners", Range(0,1)) = 0.3
		_Smooth ("Smoothness", Range(0, 0.1)) = 0.02
		[Space]
		_CellSize ("CellSize parameter of Voronoi noise.", Float) = 10
		_BorderWidth ("Width of borders", Range(0.1, 2)) = 0.25
		_EdgeShade ("Shade on the edges", Range(0.01, 2)) = 0.3
		_WallRoundness ("Roundness of wall edge", Range(0, 1)) = 1
		[Space]
		_Color ("Color multier", Color) = (1, 1, 1, 1)
		_Color1 ("The first key color", Color) = (1, 0.45, 0.05, 1)
		_Color2 ("The second key color", Color) = (1, 0.05, 0.05, 1)
	}

	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue" = "AlphaTest"}
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

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul(unity_ObjectToWorld, v.vertex);
				o.uv = v.uv;
				return o;
			}

			#include "SnakeTiles/VoronoiNoise.cginc"

			#define C (sqrt(2.)/2.)

			uint _Orientation;
			float _OffsetWidth;
			float _CornerR;
			float _Smooth;

			float _CellSize;
			float _BorderWidth;
			float _EdgeShade;
			float _WallRoundness;

			fixed4 _Color;
			fixed4 _Color1;
			fixed4 _Color2;

			fixed4 frag (v2f i) : SV_Target {
				float2 uv = 2 * (i.uv - 0.5);
				float2 worldUp = float2(0, 1);

				uint curQuater = ((uv.x > 0) + 2 * (uv.y > 0) + 2) & 3;
				curQuater = (curQuater & 2)? curQuater ^ 1 : curQuater;
				uint locOr = (_Orientation >> 4 * curQuater) & 15;

				uint rot = locOr & 3;
				switch (rot) {
					case 1: {
						uv = float2(-uv.y, uv.x);
						worldUp = float2(1, 0);
					} break;
					case 2: {
						uv = -uv;
						worldUp = float2(0, -1);
					} break;
					case 3: {
						uv = float2(uv.y, -uv.x);
						worldUp = float2(-1, 0);
					} break;
				}

				uv += float2(uv.x < 0.1, uv.y < -0.1);
				float2 uvP = float2(C*((uv.x-1) + uv.y), C*(-(uv.x-1) + uv.y));

				float offset = _OffsetWidth;
				uint type = (locOr >> 2) & 3;

				switch (type) {
					case 0: { } break;

					case 1: {
						float2 dirVec = uv - float2(_OffsetWidth + _CornerR, 1 - _OffsetWidth - _CornerR);
						
						if (dirVec.x < 0 && dirVec.y < 0) {
							worldUp = mul(float2x2(0, -1, 1, 0), worldUp);
						} else if (dirVec.x < 0 && dirVec.y > 0) {
							float a = -2 * atan(clamp(dirVec.x / dirVec.y, -1, 0));
							worldUp = mul(float2x2(cos(a), -sin(a), sin(a), cos(a)), worldUp);
						}

						uv.x = 0.5 + (2 / 3.1416) * atan(uvP.x / uvP.y);
						uv.y = (abs(uvP.x) > C*_CornerR) ? 
								(uvP.y + abs(uvP.x) - (sqrt(2) - 1) * _CornerR) :
								(uvP.y + _CornerR*(1 - sqrt(1 - pow(uvP.x / _CornerR, 2))));
						
						uv.y /= sqrt(2) - (sqrt(2) - 1) * _CornerR;
						offset = _OffsetWidth / (1 - (1 - 1/sqrt(2)) * _CornerR);
						uv.y = (uv.y - 1 + offset) * (1 - (1 - 1 / sqrt(2)) * _CornerR) + 1 - offset;
					} break;

					case 2: {
						float2 dirVec = uv - float2(_OffsetWidth - _CornerR, 1 - _OffsetWidth + _CornerR);
						if (dirVec.x > 0 && dirVec.y > 0) {
							worldUp = mul(float2x2(0, -1, 1, 0), worldUp);
						}
						else if (dirVec.x > 0 && dirVec.y < 0) {
							float a = -2 * atan(clamp(dirVec.x / dirVec.y, -1, 0));
							worldUp = mul(float2x2(cos(a), -sin(a), sin(a), cos(a)), worldUp);
						}

						uvP.y -= sqrt(2.);
						uv.x = 0.5 - (2 / 3.1416) * atan(uvP.x / uvP.y);
						uv.y = (abs(uvP.x) > C*_CornerR) ?
							(uvP.y - abs(uvP.x) + (sqrt(2) - 1) * _CornerR) :
							(uvP.y - _CornerR * (1 - sqrt(1 - pow(uvP.x / _CornerR, 2))));

						uv.y += sqrt(2);
						uv.y /= sqrt(2) - (sqrt(2) + 1) * _CornerR;
						offset = (_OffsetWidth - 2 * _CornerR) / (1 - (1 + 1 / sqrt(2)) * _CornerR);
						uv.y = (uv.y - 1 + offset) * (1 - (1 + 1 / sqrt(2)) * _CornerR) + 1 - offset;
					} break;

					case 3: {
						worldUp = 0;
					} break;
				}

				float mask = smoothstep(1 - offset, 1 - offset - _Smooth, uv.y);
				mask = max(mask, type == 3);

				float R = _CornerR * _WallRoundness;
				float Y = uv.y - (1 - offset - R);
				
				i.worldPos.xy += (0 < Y && Y < R) * worldUp * (R * asin(clamp(Y/R, -1, 1)) - Y);
				float4 noise = VoronoiNoise(i.worldPos.xy, _CellSize, 228);

				fixed3 cellColor = _Color.rgb * lerp(_Color1.rgb, _Color2.rgb, noise.z);
				cellColor *= max(type == 3, pow(clamp((1 - Y / R), 0, 1), _EdgeShade));

				cellColor *= _Color.a * pow(noise.y, _BorderWidth);

				return mask * fixed4(cellColor, 1);
			}
			ENDCG
		}
	}
}
