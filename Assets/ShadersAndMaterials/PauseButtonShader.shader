Shader "Unlit/PauseButtonShader"
{
	Properties
	{
		_Smooth ("Smoothness", Range(0, 0.2)) = 0.05
		_LineWidth ("Width of line", Range(0, 0.5)) = 0.1
		_R ("Button radius", Range(0, 1.5)) = 1
		_VertLinesH ("Height of vertical lines", Range(0, 1)) = 0.6
		_VertLinesOffset ("Offset of vert lines", Range(0, 1)) = 0.3

		_Color ("Color", Color) = (1, 1, 1, 1)
	}
	SubShader
	{
		Tags { "RenderType" = "Transparent" "Queue" = "AlphaTest"}
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

			#define S(a, b, c) (smoothstep((a), (b), (c)))

			float _Smooth;
			float _LineWidth;
			float _R;
			float _VertLinesH;
			float _VertLinesOffset;

			fixed4 _Color;

			fixed4 frag (v2f i) : SV_Target 
			{
				float2 uv = 3 * (i.uv - 0.5);
				float r = length(uv);

				float pauseMask = S(_R + _LineWidth + _Smooth, _R + _LineWidth, r);

				float vlOffset = _R * _VertLinesOffset;
				float vertLinesMask = S(vlOffset - _Smooth, vlOffset, abs(uv.x)) * 
									  S(vlOffset + _LineWidth + _Smooth, vlOffset + _LineWidth, abs(uv.x));

				float vlHeight = _VertLinesH * _R;
				vertLinesMask *= S(vlHeight, vlHeight - _Smooth, abs(uv.y));

				pauseMask *=  S(_R - _Smooth, _R, r) + vertLinesMask;

				fixed4 col = _Color * pauseMask;

				col += fixed4(0.3, 1, 0.3, 1) * min(0.5, max(0, (0.5 / r) * (1.5 - r)));

				return col;
			}
			ENDCG
		}
	}
}
