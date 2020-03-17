using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Visualizable;

public class FieldGenerator {

	public int width;
	public int height;
	public int seed;

	public System.Func<int> GetNextId;
	private System.Random rand;

	private double currentSpawnTrashold;
	private int spawnAttempts;

	#region Constants
	public readonly float OBSTACLES_SPAWN_ITERATIONS_PER_TILE = 1;
	public readonly float OBSTACLE_VOID_VALUE = 5;

	public readonly float AVERAGE_OBSTACLE_SIZE = 6;

	public readonly float INIT_VOIDS_PER_TILE = 0.01f;
	public readonly float INIT_VOIDS_VALUE = 10;

	public readonly int RELIEF_ATTEMPTS_TRASHOLD = 10;
	public readonly double RELIEF_STRENGTH = 0.1;
	#endregion

	public FieldGenerator (int seed, int width, int height, System.Func<int> idGenerator) {

		this.seed = seed;
		this.width = width;
		this.height = height;

		GetNextId = idGenerator;
	}

	public FieldItem[,] GenerateField () {
		Debug.Assert(width > 0 && height > 0, "Wrong field dimentions");

		rand = new System.Random(seed);
		var field = new FieldItem[width, height];

		int spawnIterations = (int)(width * height * OBSTACLES_SPAWN_ITERATIONS_PER_TILE);
		HashSet<FieldVoid> fieldVoids = GenerateInitialVoids();

		int spentIterations = 0;
		while (true) {
			var nwPos = EvaluateSuitedPos(fieldVoids, out int lastSpentIterations);
			spentIterations += lastSpentIterations;
			if (spawnIterations < spentIterations) break;

			if (field[nwPos.x, nwPos.y].type != FieldItem.ItemType.None) continue;

			SpawnObstacleAt(nwPos, field);
			fieldVoids.Add(new FieldVoid { pos = nwPos, value = OBSTACLE_VOID_VALUE });
		}

		return field;
	}

	private void SpawnObstacleAt (Vector2Int initPos, FieldItem[,] field) {
		var obstaclesTiles = new List<Vector2Int>(32) { initPos };
		while (true) {
			var probOfNewTile = 1 / (1 + System.Math.Exp(obstaclesTiles.Count - AVERAGE_OBSTACLE_SIZE));
			if (probOfNewTile < rand.NextDouble()) break;

			AddNewTileInObstacle(obstaclesTiles, field);
		}
	}

	private void AddNewTileInObstacle (List<Vector2Int> obstacles, FieldItem[,] field) {
		for (int attempts = 0; attempts < obstacles.Count; ++attempts) {
			var tarTile = obstacles[rand.Next(0, obstacles.Count)];
			var possibleNwTile = GetNeighbors(tarTile);

			possibleNwTile.RemoveAll(tile => !IsAllowedForAdding(tile, field));
			if (possibleNwTile.Count == 0) continue;

			var nwTilePosition = possibleNwTile[rand.Next(0, possibleNwTile.Count)];
			var nwWallTile = new FieldItem { id = GetNextId(), type = FieldItem.ItemType.Wall };
			obstacles.Add(nwTilePosition);
			field[nwTilePosition.x, nwTilePosition.y] = nwWallTile;

			return;
		}
	}

	private bool IsAllowedForAdding (Vector2Int nwTile, FieldItem[,] field) {
		if (!InsideField(nwTile) || field[nwTile.x, nwTile.y].type != FieldItem.ItemType.None) return false;
		var neighbors = GetNeighbors(nwTile);

		bool isAllowed = true;
		foreach (var t in neighbors) {
			if (!InsideField(t) || field[t.x, t.y].type != FieldItem.ItemType.None) continue;
			var anotherNeighbors = GetNeighbors(t);

			int freeNeighbors = 0;
			foreach (var tile in anotherNeighbors) {
				if (InsideField(tile) && (tile != nwTile && field[tile.x, tile.y].type == FieldItem.ItemType.None)) {
					++freeNeighbors;
				}
			}

			isAllowed = isAllowed && freeNeighbors > 1;
		}

		return isAllowed;
	}

	private List<Vector2Int> GetNeighbors (Vector2Int pos) {
		return new List<Vector2Int> { pos + Vector2Int.left, pos + Vector2Int.up,
									  pos + Vector2Int.right, pos + Vector2Int.down };
	}

	private bool InsideField (Vector2Int pos) {
		return 0 <= pos.x && pos.x < width && 0 <= pos.y && pos.y < height;
	}

	private HashSet<FieldVoid> GenerateInitialVoids () {
		var fieldVoids = new HashSet<FieldVoid>();
		int voidsNumber = (int)(width * height * INIT_VOIDS_PER_TILE);

		for (int i = 0; i < voidsNumber; i++) {
			Vector2Int nwPos = EvaluateSuitedPos(fieldVoids, out int spentIterations);
			fieldVoids.Add(new FieldVoid { pos = nwPos, value = INIT_VOIDS_VALUE });
		}

		return fieldVoids;
	}

	private Vector2Int EvaluateSuitedPos (HashSet<FieldVoid> fieldVoids, out int spentIterations) {
		spentIterations = 0;
		Vector2Int suitedPos;
		do {
			int x = rand.Next(0, width);
			int y = rand.Next(0, height);

			++spentIterations;
			suitedPos = new Vector2Int(x, y);
		} while (!IsPassSpawnTrashold(suitedPos, fieldVoids));

		return suitedPos;
	}

	private bool IsPassSpawnTrashold (Vector2Int pos, HashSet<FieldVoid> fieldVoids) {
		var spawnProb = CalcSpawnProb(pos, fieldVoids);

		var reliefCoef = System.Math.Min(1, System.Math.Exp((RELIEF_ATTEMPTS_TRASHOLD - spawnAttempts) * RELIEF_STRENGTH));
		if (currentSpawnTrashold * reliefCoef < spawnProb) {
			spawnAttempts = 0;
			currentSpawnTrashold = 0.5 + 0.5*rand.NextDouble();
			return true;
		}

		spawnAttempts += 1;
		return false;
	}

	private double CalcSpawnProb (Vector2Int pos, HashSet<FieldVoid> fieldVoids) {
		float prob = 1;
		foreach (var fieldVoid in fieldVoids) {
			float dist = Vector2Int.Distance(pos, fieldVoid.pos);
			float probToNotSpawn = 1 - 1 / (1 + Mathf.Pow(dist / fieldVoid.value, 2));
			prob *= probToNotSpawn;
		}

		return prob;
	}

	//private class Vector2IntComparer : IComparer<Vector2Int> {
	//	private bool yFirst;

	//	public Vector2IntComparer (bool yFirst) {
	//		this.yFirst = yFirst;
	//	}

	//	public int Compare (Vector2Int x, Vector2Int y) {
	//		if (yFirst) {
	//			int deltaY = x.y - y.y;
	//			if (deltaY != 0) return deltaY;

	//			return x.x - y.x;
	//		} else {
	//			int deltaX = x.x - y.x;
	//			if (deltaX != 0) return deltaX;

	//			return x.y - y.y;
	//		}
	//	}
	//}

	private struct FieldVoid {
		public Vector2Int pos;
		public float value;

		public override int GetHashCode () {
			return pos.GetHashCode() ^ value.GetHashCode();
		}
	}
}