Shader "SnakeTiles/TurnShader"
{
	Properties
	{
		[Toggle] _TurnRight("TurnRight", Float) = 0

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
				float4 vertex : SV_POSITION;
			};

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			float _TurnRight;
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

			inline float2 TurnTransform (float2 uv) {
				uv.x = (1 - _TurnRight)*uv.x + _TurnRight*(1 - uv.x);

				uv = float2(length(uv), (2/PI) * atan(uv.y / uv.x));

				uv.x = (1 - _TurnRight)*uv.x + _TurnRight*(1 - uv.x);
				return uv;
			} 

			float BodyRadius (float v) {
				float rnd = RandFromId(_SnakeID);

				float aPhase = SnakePhase(rnd, _CurvRate, _TailN + 1);
				float bPhase = SnakePhase(rnd, _CurvRate, _TailN);

				float Ta = -_CurvAmplitude*_CurvRate * cos(aPhase);
				float Tb = -_CurvAmplitude*_CurvRate * cos(bPhase);

				float deltaA = - _CurvAmplitude * sin(aPhase);

				Ta *= (_HeadN != 1);
				float a = 1 - _EdgeOffset + (_HeadN != 1) * deltaA;
				float b = 1 - _EdgeOffset - _CurvAmplitude * sin(bPhase);

				Ta = (2 / PI) * Ta / a;
				Tb = (2 / PI) * Tb / b;
 
				float A = 2*(b - a) + (Ta + Tb);
				float B = 3*(a - b) - 2*Tb - Ta;
				float C = Tb;
				float D = b;

				float U = ((A*v + B)*v + C)*v + D;
				return U;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float2 uv = TurnTransform(i.uv);

				float t = 2 * PI * frac(4 * _Time.x);
				uv.x -= BodyRadius(uv.y)  - (1 - _EdgeOffset);

				fixed3 colorMask = SquamaTexture(uv + float2(-0.5, _TailN), 0.5 - _EdgeOffset, _EdgeBlur, _GyroidConfig);

				fixed4 col = 0;
				col.rgb += _SnakeColors[0] * colorMask.x;
				col.rgb += _SnakeColors[1] * colorMask.y;
				col.rgb += _SnakeColors[2] * colorMask.z;

				col.a = MakeRect(uv.x, _EdgeOffset, 1 - _EdgeOffset, _EdgeBlur);
				return col;
			}
			ENDCG
		}
	}
}
