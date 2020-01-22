Shader "SnakeTiles/TurnShader"
{
    Properties
    {
        [Toggle] _TurnRight("TurnRight", Float) = 0

        _SnakeID("SnakeID", Float) = 0
        _TailN("Index from tail", Float) = 0
        _HeadN("Index from head", Float) = 0

        _EdgeBlur("EdgeBlur", Float) = 0.05
        _EdgeOffset("EdgeOffset", Float) = 0.2

        _offset("offset", Float) = 0
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

            float _offset;

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;
                
                uv.x = (1 - _TurnRight)*uv.x + _TurnRight*(1 - uv.x);

                float R = length(uv);
                float Phi = atan(uv.y / uv.x);
                uv = float2(R, Phi);

                uv.x = (1 - _TurnRight)*uv.x + _TurnRight*(1 - uv.x);

                float t = 2 * PI * frac(4 * _Time.x);
                fixed4 col = fixed4(0.5*sin(5*t) + 0.5, 0.5*sin(1-t) + 0.5, sin(t), 1);

                float rnd = frac(_SnakeID * 153.234 + 99.43 * frac(0.123 * _SnakeID));
                
                float a = 2 * R;
                float k = (16*(1-a)*Phi*Phi + 8*PI*(a-1)*Phi + pow(PI, 2)) / pow(PI, 2); 
                uv.x += 0.05  * sin(t + 10 * (_TailN + uv.y + 0.57) + rnd * 100);
                
                // for debug purposes
                // col.b = 0;

                float b = _EdgeBlur;
                float h = _EdgeOffset;
                col.a = smoothstep(h, h+b, uv.x) * smoothstep(1 - h, 1 -h-b, uv.x);

                return col;
            }
            ENDCG
        }
    }
}
