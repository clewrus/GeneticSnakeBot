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

            float BodyRadius (float rnd, float t, float R, float v) {
                float Ta = -_CurvAmplitude*_CurvRate * cos(t + _CurvRate*(_TailN+1) + 100*rnd);
                float Tb = -_CurvAmplitude*_CurvRate * cos(t + _CurvRate*(_TailN) + 100*rnd);
                
                float arcTa = pow(atan(Ta), 2);
                float arcTb = pow(atan(Tb), 2);

                // lerp is empiric solution
                Ta = sign(Ta) * (lerp(1, 2, R) / PI) * sqrt(arcTa / (1 + arcTa));
                Tb = sign(Tb) * (lerp(1, 2, R) / PI) * sqrt(arcTb / (1 + arcTb));
                
                float commonParam = t + _CurvRate*_TailN + 100*rnd;
                float a = -_CurvAmplitude * sin(commonParam + _CurvRate);
                float b = -_CurvAmplitude * sin(commonParam);

                a += 1 - _EdgeOffset;
                b += 1 - _EdgeOffset;

                float A = 2*(b - a) + (Ta + Tb);
                float B = 3*(a - b) - 2*Tb - Ta;
                float C = Tb;
                float D = b;

                float U = ((A*v + B)*v + C)*v + D;
                return U;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                
                uv.x = (1 - _TurnRight)*uv.x + _TurnRight*(1 - uv.x);

                float R = length(uv);
                float Phi = atan(uv.y / uv.x);
                uv = float2(R, (2/PI) * Phi);

                uv.x = (1 - _TurnRight)*uv.x + _TurnRight*(1 - uv.x);

                float t = 2 * PI * frac(4 * _Time.x);
                fixed4 col = fixed4(0.5*sin(5*t) + 0.5, 0.5*sin(1-t) + 0.5, sin(t), 1);

                float rnd = frac(_SnakeID * 153.234 + 99.43 * frac(0.123 * _SnakeID));
                
                float U = BodyRadius(rnd, t, R, uv.y);
                float Ul = U - (1 - 2*_EdgeOffset);
                
                col.a = smoothstep(Ul, Ul + _EdgeBlur, uv.x) * smoothstep(U, U - _EdgeBlur, uv.x);

                return col;
            }
            ENDCG
        }
    }
}
