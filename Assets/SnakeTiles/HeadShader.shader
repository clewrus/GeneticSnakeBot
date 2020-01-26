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
            
            #define PI 3.141592

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

                float kPrime = -(1 - _EdgeOffset - _HeadConfig.z) * ((3.1416/2) / w);
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
                float w = _HeadConfig.w;
                float A = 1 - _EdgeOffset - _HeadConfig.z;
                
                return A * cos((3.1416/2) * u/w);
            }

            float SnakeHeadMask (float2 uv) {
                float a = 0.5 - _EdgeOffset;

                float h = _HeadConfig.x;
                float p = _HeadConfig.z;
                float w = _HeadConfig.w;

                float mask = 0;
                mask += (uv.y < h) * (uv.x < f1(uv.y, a));

                uv.y -= h;
                mask += (0 < uv.y && uv.y < p-h) * (uv.x < f2(uv.y));

                uv.y -= p - h;
                mask += (0 < uv.y && uv.x < w) * (uv.y < f3(uv.x));

                return mask;
            }

            fixed4 SnakeHead (float2 uv) {
                fixed4 col = 0;

                float mask = SnakeHeadMask(uv);

                float2 eyeC = float2(_HeadConfig.w * 1.1, _HeadConfig.z * 0.8);
                float r = length(uv - eyeC);

                float4 eye = float4(sin(100*r + _Time.y), sin(100*r + 5*_Time.y), 0.5, 1);
                col = lerp(mask, eye, smoothstep(0.089, 0.088, r));

                return col;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;

                float t = 2 * PI * frac(4 * _Time.x);
                fixed4 col = fixed4(0.5*sin(5*t) + 0.5, 0.5*sin(1-t) + 0.5, sin(t), 1);

                float rnd = frac(_SnakeID * 153.234 + 99.43 * frac(0.123 * _SnakeID));
                
                uv.x = abs(uv.x - 0.5);
                col = SnakeHead(uv);

                return col;
            }
            ENDCG
        }
    }
}
