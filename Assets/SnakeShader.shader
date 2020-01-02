Shader "Unlit/SnakeShader"
{
    Properties
    {
        _GridDimentions("Columns/Rows", Vector) = (10, 10, 0, 0)
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }

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

            float2 _GridDimentions;

            float calcBorderMask (float2 cellUV, float2 initialUV) {
                float2 borderDist = float2(
                    min(cellUV.x, 1 - cellUV.x),
                    min(cellUV.y, 1 - cellUV.y)
                );
                
                float mask = 1;
                float2 borderWidth = abs(float2(ddx(initialUV.x), ddy(initialUV.y))) * _GridDimentions;

                mask *= borderDist.x > borderWidth.x;
                mask *= borderDist.y > borderWidth.y;

                return mask;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = frac(i.uv * _GridDimentions);
                fixed4 col = 1;

                float borderMask = calcBorderMask(uv, i.uv);

                col *= borderMask;
                return col;
            }
            ENDCG
        }
    }
}
