Shader "SnakeTiles/HeadShader"
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

		[Space]
		_HeadConfig("h, W, p, noseWidth", Vector) = (0.1, 0.34, 0.7, 0.12)
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
			
			#include "SnakeCommon.cginc"

			float _TurnRight;
			float _SnakeID;

			float _TailN;
			float _HeadN;

			float _EdgeBlur;
			float _EdgeOffset;

			float _CurvRate;
			float _CurvAmplitude;

			float4 _BodyL;

			float4 _HeadConfig;

			float f1 (float v, float a) {
				float u = - 2 * (_HeadConfig.y - a) / pow(_HeadConfig.x, 3);
				u = u * v + 3 * (_HeadConfig.y - a) / pow(_HeadConfig.x, 2);
				u = u * v + 0;
				u = u * v + a;

				return u;
			}

			float f2 (float v) {
				float w = _HeadConfig.w;
				float d = _HeadConfig.z - _HeadConfig.x;

				float kPrime = -(1 - _EdgeOffset - _HeadConfig.z) * ((PI/2) / w);
				float k =  1 / kPrime;

				float D = _HeadConfig.y;
				float C = 0;
				float B = (3*(w - D) - k*d) / (d*d);
				float A = (k - 2*B*d) / (3 * d*d);

				float u = A;
				u = u*v + B;
				u = u*v + C;
				u = u*v + D;

				return u;
			}

			float f3 (float u) {
				float A = 1 - _EdgeOffset - _HeadConfig.z;
				
				return A * cos((PI/2) * u/_HeadConfig.w);
			}

			float f3Op (float v) {
				float A = 1 - _EdgeOffset - _HeadConfig.z;
				return (2 / PI) * _HeadConfig.w * acos(clamp(-1, 1, v / A));
			}

			fixed4 SnakeHeadMask (float2 iuv) {
				float2 uv = float2(abs(iuv.x), iuv.y);

				float h = _HeadConfig.x;
				float p = _HeadConfig.z;
				float w = _HeadConfig.w;

				fixed4 col = fixed4(1, 1, 1, 0);
				float bodyR = 0;

				if (uv.y < h) {
					float f1Val = f1(uv.y, 0.5-_EdgeOffset);
					col.a = S(0, _EdgeBlur, f1Val - uv.x);

					bodyR = f1Val;
				}                

				uv.y -= h;
				if (0 < uv.y && uv.y < p-h) {
					float f2Val = f2(uv.y);
					col.a = S(0, _EdgeBlur, f2Val - uv.x);

					bodyR = f2Val;
				}

				uv.y -= p - h;
				if (0 < uv.y && uv.x < w) {
					float f3Val = f3(uv.x);
					col.a = S(0, _EdgeBlur, f3Val - uv.y);

					bodyR = f3Op(uv.y);
				}
				
				col.rgb = SquamaTexture(iuv + float2(0, _TailN), bodyR);
				return col;
			}

			fixed4 SnakeEye (float2 uv) {
				uv.x += (uv.x < 0) * 2*(_HeadConfig.w * 1.1);
				uv -= float2(_HeadConfig.w * 1.1, _HeadConfig.z * 0.8);

				float R = length(uv);

				float eyeSize = 0.09;
				fixed4 eye = 1;

				float ballSize = 0.05;

				float A = (eyeSize - ballSize - 0.01) * sin(RandFromId(_SnakeID) + _Time.y);
				float phi = 2 * PI * frac(_Time.y);

				float2 ballPos = A * float2(cos(phi), sin(phi));
				eye.rgb = S(ballSize, ballSize + _EdgeBlur, length(uv - ballPos));

				eye.a = S(eyeSize + _EdgeBlur, eyeSize, R);

				return eye;
			}

			fixed4 SnakeHead (float2 uv) {
				fixed4 col = 1;

				col = SnakeHeadMask(uv);
				

				fixed4 eye = SnakeEye(uv);
				col = lerp(col, eye, eye.a);

				return col;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float2 uv = i.uv;                
				uv.x = uv.x - 0.5;

				fixed4 col = SnakeHead(uv);

				return col;
			}
			ENDCG
		}
	}
}
