#ifndef VORONOI_NOISE
#define VORONOI_NOISE

static float2 Rand2 (float2 pos, float seed) {
	pos.x = frac(frac(543.35 * pos.x) + 234 * seed * frac(765.23 * pos.y) + 4*seed);
	pos.y = frac(234 * seed * frac(123.46 * pos.x) + frac(345.43 * pos.y) + 5*seed);

	pos.x *= dot(pos, float2(13.234, 4.234));
	pos.y *= dot(pos, float2(9.2354, 5.423));

	return frac(pos);
}

float4 VoronoiNoise (float2 uv, float2 cellSize, float seed) {
	uv *= cellSize;
	float2 cellPos = floor(uv);
	float2 curPoint = cellPos + Rand2(cellPos, seed);

	float distToClosePoint = 10000;
	float distToNextPoint;
	
	float2 closePoint;
	float2 nextPoint;

	[unroll]
	for (float x = -1; x <= 1; x++) {
		[unroll]
		for (float y = -1; y <= 1; y++) {
			float2 targetCell = cellPos + float2(x, y);
			float2 targetPoint = targetCell + Rand2(targetCell, seed);

			float curLength = length(targetPoint - uv);
			if (curLength < distToClosePoint) {
				nextPoint = closePoint;
				distToNextPoint = distToClosePoint;

				closePoint = targetPoint;
				distToClosePoint = curLength;
			} else if (curLength < distToNextPoint) {
				nextPoint = targetPoint;
				distToNextPoint = curLength;
			}
		}
	}

	float2 N = closePoint - nextPoint;
	float d = (dot(uv, N) + 0.5 * (dot(nextPoint, nextPoint) - dot(closePoint, closePoint))) / length(N);

	float2 rnd = Rand2(closePoint, seed);
	return float4(distToClosePoint, abs(d), rnd.x, rnd.y);
}

#endif