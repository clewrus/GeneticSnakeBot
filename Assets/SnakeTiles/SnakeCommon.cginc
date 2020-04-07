#ifndef SNAKE_COMMON
// Upgrade NOTE: excluded shader from DX11, OpenGL ES 2.0 because it uses unsized arrays
#pragma exclude_renderers d3d11 gles
#define SNAKE_COMMON

	#define PI 3.141592

	#define S(a, b, c) smoothstep(a, b, c)

	#define RandFromId(id) (frac((id) * 153.234 + 99.43 * frac(0.123 * (id))))

	#define SnakePhase(rnd, rate, v) (2*PI*frac(4*_Time.x) + (rate) * (v) + 100*(rnd))

	#define MakeRect(u, L, R, blur) (S(L, (L) + (blur), u) * S(R, (R) - (blur), u))

	#include "VoronoiNoise.cginc"

	static float Gyroid(float3 pos, float ratio) {
		pos *= ratio;
		return 0.5 + (1. / 6.) * dot(sin(pos.xyz / ratio), cos(pos.yzx));
	}

	float3 SquamaTexture (float2 uv, float R, float smooth, float3 gyroidConfig[2]) {
		float3 gyroidZ = gyroidConfig[0];
		float3 gyroidRatio = gyroidConfig[1];

		float relR = clamp((uv.x) / R, -1, 1);
		float2 pos = float2(R * asin(relR), uv.y);
	
		float mainMask = smoothstep(0.5, 0.5 + smooth, Gyroid(float3(20 * pos, gyroidZ.x), gyroidRatio.x));
		float mask1 = smoothstep(0.5, 0.5 + smooth, Gyroid(float3(20 * pos, gyroidZ.y), gyroidRatio.y));
		float mask2 = smoothstep(0.5, 0.5 + smooth, Gyroid(float3(20 * pos, gyroidZ.z), gyroidRatio.z));

		float3 col1 = lerp(float3(0.95, 0.05, 0.05), float3(0.05, 0.95, 0.05), mask1);
		float3 col2 = lerp(float3(0.95, 0.05, 0.05), float3(0.05, 0.05, 0.95), mask2);

		float3 mask = lerp(col1, col2, mainMask);

		float shadeK = 0.5;
		mask *= 1 - shadeK * abs(relR);

		return mask;
	}

#endif