Shader "SnakeTiles/BodyShader"
{
	Properties
	{
		_SnakeID("SnakeID", Float) = 0
		_TailN("Index from tail", Float) = 0
		_HeadN("Index from head", Float) = 0

		[Space]
		_EdgeBlur("EdgeBlur", Float) = 0.01
		_EdgeOffset("EdgeOffset", Float) = 0.2
		
		[Space]
		_CurvRate("CurvRate", Float) = 2
		_CurvAmplitude("CurvAmplitude", Float) = 0.05

		[Space]
		_BodyL("TailN of tail at T0, T0, Average shortaning", Vector) = (0.5, 0, 0, 0)
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue"="Transparent"}
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

			float _SnakeID;

			float _TailN;
			float _HeadN;

			float _EdgeBlur;
			float _EdgeOffset;

			float _CurvRate;
			float _CurvAmplitude;

			float4 _BodyL;

			float3 _GyroidConfig[2];
			fixed4 _SnakeColors[3];

			#include "SnakeCommon.cginc"
			
			fixed TailMask (float2 uv) {
				uv.x -= 0.5;
				float relY = _TailN + uv.y - _BodyL.x;
				float r = length(float2(uv.x, relY));

				float bodyR = 0.5 - _EdgeOffset;

				float shadeK = 0.8;
				float shade = (bodyR - shadeK * r) / (bodyR - shadeK * abs(uv.x));
				float tailTipMask = shade * S(_EdgeBlur, 0, r - bodyR);

				float mask = lerp(tailTipMask, 1, 0 < relY);
				return mask;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float2 uv = i.uv;

				float rnd = RandFromId(_SnakeID);                
				float deltaX = _CurvAmplitude * sin( SnakePhase(rnd, _CurvRate, _TailN + uv.y) );
				uv.x += deltaX * pow(lerp(1, 1 - uv.y, _HeadN == 1), 0.5);

				fixed3 colorMask = SquamaTexture(uv + float2(-0.5, _TailN), 0.5 - _EdgeOffset, _EdgeBlur, _GyroidConfig);

				fixed4 col = 0;
				col.rgb += _SnakeColors[0] * colorMask.x;
				col.rgb += _SnakeColors[1] * colorMask.y;
				col.rgb += _SnakeColors[2] * colorMask.z;

				col.a = TailMask(uv) * MakeRect(uv.x, _EdgeOffset, 1 - _EdgeOffset, _EdgeBlur);
				return col;
			}
			ENDCG
		}
	}
}
