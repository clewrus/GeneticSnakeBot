#ifndef VORONOI_NOISE
#define VORONOI_NOISE

static float2 Rand2 (float2 pos, float seed) {
	pos.x = frac(frac(543.35 * pos.x) + 234 * seed * frac(765.23 * pos.y) + 4*seed);
	pos.y = frac(234 * seed * frac(123.46 * pos.x) + frac(345.43 * pos.y) + 5*seed);

	pos.x *= dot(pos, float2(13.234, 4.234));
	pos.y *= dot(pos, float2(9.2354, 5.423));

	return frac(pos);
}

float3 VoronoiNoise (float2 uv, float2 cellSize, float seed) {
	uv *= cellSize;
	float2 cellPos = floor(uv);
	float2 selfPos = uv - cellPos;

	float2 dotPos = Rand2(cellPos, seed);
	float minDist = 10;
	float2 closeCell;

	[unroll]
	for (float x = -1; x <= 1; x++) {
		[unroll]
		for (float y = -1; y <= 1; y++) {
			float2 targetCell = cellPos + float2(x, y);
			float2 toNeighborDot = -selfPos + float2(x, y) + Rand2(targetCell, seed);

			float curLength = length(toNeighborDot);
			if (curLength < minDist) {
				minDist = curLength;
				closeCell = targetCell;
			}
		}
	}

	float2 rnd = Rand2(closeCell, seed);
	return float3(minDist, rnd.x, rnd.y);
}

#endif