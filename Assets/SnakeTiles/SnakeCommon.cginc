#ifndef SNAKE_COMMON
#define SNAKE_COMMON

	#define PI 3.141592

	#define S(a, b, c) smoothstep(a, b, c)

	#define RandFromId(id) (frac((id) * 153.234 + 99.43 * frac(0.123 * (id))))

	#define SnakePhase(rnd, rate, v) (2*PI*frac(4*_Time.x) + (rate) * (v) + 100*(rnd))

	#define MakeRect(u, L, R, blur) (S(L, (L) + (blur), u) * S(R, (R) - (blur), u))

	#include "VoronoiNoise.cginc"

	float3 SquamaTexture (float2 uv, float R) {

		float2 noiseCords = float2(R * asin(clamp((uv.x) / R, -1, 1)), uv.y);
		float4 noise = VoronoiNoise(noiseCords, 10, 228);

		return noise.xzw;
	}
	

#endif