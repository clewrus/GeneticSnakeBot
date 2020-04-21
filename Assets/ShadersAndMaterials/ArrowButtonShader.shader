Shader "Unlit/ArrowButtonShader"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		_Smooth ("Smoothness", Range(0, 0.3)) = 0.1

		_CornerR ("Corner Radius", Range(0, 1)) = 0.1
		_ArrowParam ("(topPointY, bottPointY, K_coef)", Vector) = (0.2, 0.1, -1.5, 0)
		_EdgeOffset ("Edge offset", Range(0, 0.5)) = 0.1
		[Space]
		_Color ("Color property", Color) = (1, 1, 1, 1)
		_ArrowColor ("Arrow color", Color) = (0.1, 0.1, 0.1, 1)
		_ButtonColor ("Button color", Color) = (0.9, 0.9, 0.9, 1)
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue"="AlphaTest" }
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

			sampler2D _MainTex;
			float4 _MainTex_ST;

			float _Smooth;
			float _CornerR;
			float3 _ArrowParam;
			float _EdgeOffset;

			fixed4 _Color;
			fixed4 _ArrowColor;
			fixed4 _ButtonColor;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			#define S(a, b, c) (smoothstep((a), (b), (c)))

			fixed4 frag (v2f i) : SV_Target
			{
				float2 uv = 2 * abs(float2(i.uv.x - 0.5, i.uv.y - 0.5));

				float squareMask = S(1 - _EdgeOffset + _Smooth, 1 - _EdgeOffset, uv.x) * S(1 - _EdgeOffset + _Smooth, 1 - _EdgeOffset, uv.y);
				float cornO = 1 - _EdgeOffset - _CornerR;
				float r = length(float2(cornO - uv.x, cornO - uv.y));
				float roundCornerMask = S(_CornerR + _Smooth, _CornerR, r);

				float baseMask = lerp(squareMask, roundCornerMask, (uv.x >= cornO && uv.y >= cornO));

				uv = 2 * float2(abs(i.uv.x - 0.5), i.uv.y - 0.5);

				float l = (uv.y - _ArrowParam.x) / _ArrowParam.z;
				float arrowMask = S(l + _Smooth, l, uv.x);
				arrowMask *= S(_ArrowParam.y - _Smooth, _ArrowParam.y, uv.y);

				baseMask *= (1 - arrowMask);

				fixed4 col = _ButtonColor * baseMask + _ArrowColor * arrowMask;
				col *= _Color;

				return col;
			}
			ENDCG
		}
	}
}
