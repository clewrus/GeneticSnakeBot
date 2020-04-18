#ifndef NOISE_3D
#define NOISE_3D

static float3 size = float3(128, 128, 128);
static uint3 cellNum = int3(32, 32, 32);
static float3 cellSize = float3(128/32.0, 128/32.0, 128/32.0);

static float seed;

void setSize (float3 _size, int3 _cellNum) {
	size = _size;
	cellNum = _cellNum;
	cellSize = float3(_size.x/_cellNum.x, _size.y/_cellNum.y, _size.z/_cellNum.z);
}

void setSeed (float _seed) {
	seed = _seed;
}

float getRand (float x) {
	float k = sin(x);
	return frac(abs(frac(k * 7930.67450 * seed)) * 3497.97350 + k + seed);
}

float2 getRand2 (float2 r) {
	float res = getRand(1.84*r.x + frac(7.83*r.y) + 1.89*seed*r.x*r.y) * 6.28;
	return float2(cos(res), sin(res));
}

float3 getRand3 (float3 r) {
	float phi = getRand(1.84*r.x + 7.83*r.y + 6.430*frac(r.z) + seed*r.x*r.y*getRand(ceil(r.z))) * 6.28;
	float del = getRand(9.479*r.x + 7.283*r.y + 4.804*frac(r.z) + seed*r.x*r.y*getRand(ceil(r.z))) * 6.28;

	return float3(sin(phi), cos(phi)*cos(del), cos(phi)*sin(del));
}

float interpolationCoef (float k) {
	return (3 - 2 * k) * k * k;
}

int3 getClampedV3D (int3 pos) {
	pos = int3(
		(pos.x % cellNum.x + cellNum.x) % cellNum.x,
		(pos.y % cellNum.y + cellNum.y) % cellNum.y,
		(pos.z % cellNum.z + cellNum.z) % cellNum.z
	);

	return pos;
}

float noise3D (float3 pos) {
	pos = float3(pos.x * size.x, pos.y * size.y, pos.z * size.z);
	float3 v = float3(pos.x/cellSize.x, pos.y/cellSize.y, pos.z/cellSize.z);
	int3 v0 = getClampedV3D(floor(v));

	float hor[2], vert[2], rad[2];

	[unroll]
	for (int z = 0; z < 2; z++) {
		[unroll]
		for (int y = 0; y < 2; y++) {
			[unroll]
			for (int x = 0; x < 2; x++) {
				int3 tarV = v0 + int3(x, y, z);
				float3 relPos = v - tarV;

				hor[x] = clamp(dot(relPos, getRand3(getClampedV3D(tarV))),-1,1);
			}
			vert[y] = lerp(hor[0], hor[1], interpolationCoef(frac(v.x)));
		}
		rad[z] = lerp(vert[0], vert[1], interpolationCoef(frac(v.y)));
	}

	return 0.5 * lerp(rad[0], rad[1], interpolationCoef(frac(v.z))) + 0.5;
}

int2 getClampedV2D (int2 pos) {
	pos = int2(
		(pos.x % cellNum.x + cellNum.x) % cellNum.x,
		(pos.y % cellNum.y + cellNum.y) % cellNum.y
	);

	return pos;
}

float noise2D (float2 pos) {
	pos = float2(pos.x * size.x, pos.y * size.y);
	float2 v = float2(pos.x/cellSize.x, pos.y/cellSize.y);
	int2 v0 = getClampedV2D(floor(v));

	float hor[2], vert[2];

	[unroll]
	for (int y = 0; y < 2; y++) {
		[unroll]
		for (int x = 0; x < 2; x++) {
			int2 tarV = v0 + int2(x, y);
			float2 relPos = v - tarV;

			hor[x] = clamp(dot(relPos, getRand2(getClampedV2D(tarV))), -1, 1);
		}
		vert[y] = lerp(hor[0], hor[1], interpolationCoef(frac(v.x)));
	}

	return 0.5 * lerp(vert[0], vert[1], interpolationCoef(frac(v.y))) + 0.5;
}

int2 getClampedV1D (int pos) {
	return (pos % cellNum.x + cellNum.x) % cellNum.x;
}

float noise1D (float pos) {
	pos = pos * size.x;
	float v = pos / cellSize.x;
	int v0 = getClampedV1D(floor(v));

	float hor[2];

	[unroll]
	for (int x = 0; x < 2; x++) {
		int tarV = v0 + x;
		float relPos = v - tarV;

		hor[x] = clamp(dot(relPos, getRand2(getClampedV2D(tarV))), -1, 1);
	}

	return 0.5 * lerp(hor[0], hor[1], interpolationCoef(frac(v))) + 0.5;
}

#endif