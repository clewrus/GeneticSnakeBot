Shader "SnakeTiles/WallShader"
{
	Properties {
		_Orientation ("Codded wall orientation", Int) = 30292
		_OffsetWidth ("Offset from the edge", Range(0,1)) = 0.2
		_CornerR ("Radius of corners", Range(0,1)) = 0.1
		_Smooth ("Smoothness", Range(0, 0.1)) = 0.01
		[Space]
		_CellSize ("CellSize parameter of Voronoi noise.", Float) = 10
		_BorderWidth ("Width of borders", Range(0.1, 2)) = 0.5
		_Color1 ("The first key color", Color) = (0.8, 0.7, 0.1, 1)
		_Color2 ("The second key color", Color) = (0.6, 0.5, 0.1, 1)
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

			#include "VoronoiNoise.cginc"

			uint _Orientation;
			float _OffsetWidth;
			float _CornerR;
			float _Smooth;

			float _CellSize;
			float _BorderWidth;

			fixed4 _Color1;
			fixed4 _Color2;

			fixed4 frag (v2f i) : SV_Target {
				float2 uv = 2 * (i.uv - 0.5);

				uint curQuater = ((uv.x > 0) + 2 * (uv.y > 0) + 2) & 3;
				curQuater = (curQuater & 2)? curQuater ^ 1 : curQuater;
				uint locOr = (_Orientation >> 4 * curQuater) & 15;

				uint rot = locOr & 3;
				switch (rot) {
					case 1: {
						uv = float2(-uv.y, uv.x);
					} break;
					case 2: {
						uv = -uv;
					} break;
					case 3: {
						uv = float2(uv.y, -uv.x);
					} break;
				}
				uv += float2(uv.x < 0, uv.y < 0);

				float mask = 1;
				uint type = (locOr >> 2) & 3;
				switch (type) {
					case 0: {
						mask = smoothstep(1-_OffsetWidth, 1-_OffsetWidth - _Smooth, uv.y);

						
					} break;

					case 1: {
						mask = smoothstep(1 - _OffsetWidth, 1 - _OffsetWidth - _Smooth, uv.y);
						mask *= smoothstep(_OffsetWidth, _OffsetWidth + _Smooth, uv.x);

						float2 r = uv - float2(_OffsetWidth + _CornerR, 1 - _OffsetWidth - _CornerR);
						mask = lerp(mask, smoothstep(_CornerR, _CornerR - _Smooth, length(r)), r.x < 0 && 0 < r.y);
					} break;

					case 2: {
						float2 r = uv - float2(_OffsetWidth - _CornerR, 1 - _OffsetWidth + _CornerR);
						mask = smoothstep(_OffsetWidth - _Smooth, _OffsetWidth , max(uv.x, 1 - uv.y));

						float innerRing = smoothstep(_CornerR - _Smooth, _CornerR , length(r));
						mask = lerp(mask, innerRing, r.x<_CornerR && -_CornerR<r.y && r.x > 0 && r.y < 0);
					} break;

					case 3: {
						mask = 1;
					} break;
				}

				float4 noise = VoronoiNoise(i.worldPos.xy, _CellSize, 228);
				fixed3 cellColor = lerp(_Color1.rgb, _Color2.rgb, noise.z);

				cellColor *= pow(noise.y, _BorderWidth);
				
				return mask * fixed4(cellColor, 1);
			}
			ENDCG
		}
	}
}
