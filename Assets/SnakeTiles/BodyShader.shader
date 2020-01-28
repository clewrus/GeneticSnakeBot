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
        Tags { "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
        

            #include "UnityCG.cginc"
            #include "VoronoiNoise.cginc"

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
            
            fixed TailMask (float2 uv) {
                uv.x -= 0.5;
                float relY = _TailN + uv.y - _BodyL.x;
                float2 r = float2(uv.x, relY);

                float mask = lerp((length(r) < 0.5 - _EdgeOffset), 1, 0 < relY);
                return mask;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float t = 2 * PI * frac(4 * _Time.x);
 
                float rnd = frac(_SnakeID * 153.234 + 99.43 * frac(0.123 * _SnakeID));
                
                float headNextK = (float)(_HeadN == 1);
                float deltaX = _CurvAmplitude * sin(t + _CurvRate*(_TailN + uv.y) + 100*rnd);
                uv.x += deltaX * (1 - uv.y * headNextK);

                float b = _EdgeBlur;
                float h = _EdgeOffset;

                float R = 0.5 - _EdgeOffset;
                float2 noiseCords = float2(R * asin(clamp((uv.x-0.5)/R, -1, 1)), uv.y + _TailN);
                float3 noise = VoronoiNoise(noiseCords, 10, 228);

                fixed4 col = fixed4(noise, 0);

                col.a = smoothstep(h, h+b, uv.x) * smoothstep(1 - h, 1 -h-b, uv.x);
                
                col.a *= TailMask(uv);

                return col;
            }
            ENDCG
        }
    }
}
