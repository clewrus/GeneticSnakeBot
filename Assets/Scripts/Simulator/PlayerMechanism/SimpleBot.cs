using Simulator;
using UnityEngine;

namespace PlayerMechanism {
	public class SimpleBot : IPlayer {
		SnakeInfo info = new SnakeInfo {
				maxLength = 5,
				bodyWidth = 0.6f,
				maxValue = 5,
				halfViewAngle = 0.75f * 3.1416f,
				eyeQuality = 9,
				denseLayerSize = 0,
				cullingDistance = 7,
				scuamaPatern = new SnakeInfo.ScuamaPatern {
					giroid0 = (Random.Range(0.1f, 2), Random.Range(0.1f, 2)),
					giroid1 = (Random.Range(0.1f, 2), Random.Range(0.1f, 2)),
					giroid2 = (Random.Range(0.1f, 2), Random.Range(0.1f, 2)),

					backgroundColor = new Vector4 (Random.value, Random.value, Random.value, 0),
					color1 = new Vector4 (Random.value, Random.value, Random.value, 0),
					color2 = new Vector4 (Random.value, Random.value, Random.value, 0),
				}
		};

		public bool NeedsProjection => true;

		public SnakeInfo GetSnakeInfo () {
			return info;
		}

		public void HandleMoveResult (MoveResult result) {

		}

		public MoveInfo MakeMove (MoveInfo.Direction dir, Projection playerInput) {
			int tarIndex = SelectTheClosestFoodIndex(playerInput);
			float tarAngle = 2 * info.halfViewAngle * ((tarIndex+0.5f) / playerInput.eyeQuality) + (Mathf.PI - info.halfViewAngle);
			int dirIndex = Mathf.FloorToInt(((tarAngle + Mathf.PI / 4) % (2 * Mathf.PI)) / (Mathf.PI / 2));

			var locVec = DirIndexToVec(dirIndex);

			if (locVec == Vector2Int.down) {
				locVec = (tarAngle > Mathf.PI) ? Vector2Int.right : Vector2Int.left;
			}

			var worldVec = CalcWorldVec(dir, locVec);
			var worldDir = VecToDir(worldVec);

			return new MoveInfo {
				dir = worldDir,
				snakeInfo = info
			};
		}

		private int SelectTheClosestFoodIndex (Projection proj) {
			int selectedIndex = -1;
			float value = float.NegativeInfinity;

			int k = (proj.eyeQuality / 2);
			for (int i = 0; i < proj.eyeQuality; i++) {
				k += (i % 2 == 0) ? +i : -i;

				float foodValue = proj.food[k] - proj.wall[k];
				float snakeValue = (proj.kinship[k] < 0.01f) ? proj.snake[k] - proj.wall[k] : float.NegativeInfinity;

				float curValue = Mathf.Max(2 * snakeValue, foodValue);
				if (value < curValue) {
					selectedIndex = k;
					value = curValue;
				}
			}

			if (selectedIndex == -1) {
				return Random.Range(0, proj.eyeQuality);
			}

			return selectedIndex;
		}

		private Vector2Int DirIndexToVec (int index) {
			switch (index) {
				case 0: return Vector2Int.down;
				case 1: return Vector2Int.right;
				case 2: return Vector2Int.up;
				case 3: return Vector2Int.left;
			}
			return Vector2Int.up;
		}

		private Vector2Int CalcWorldVec (MoveInfo.Direction localRotation, Vector2Int locVec) {
			switch (localRotation) {
				case MoveInfo.Direction.Up: break;

				case MoveInfo.Direction.Right: {
					locVec = new Vector2Int(locVec.y, -locVec.x);
				} break;

				case MoveInfo.Direction.Down: {
					locVec = new Vector2Int(-locVec.x, -locVec.y);
				} break;

				case MoveInfo.Direction.Left: {
					locVec = new Vector2Int(-locVec.y, locVec.x);
				} break;
			}

			return locVec;
		}

		private MoveInfo.Direction VecToDir (Vector2Int vec) {
			if (vec.x == 0) {
				if (vec.y >= 0) {
					return MoveInfo.Direction.Up;
				} else {
					return MoveInfo.Direction.Down;
				}
			}

			if (vec.y == 0) {
				if (vec.x >= 0) {
					return MoveInfo.Direction.Right;
				} else {
					return MoveInfo.Direction.Left;
				}
			}

			return MoveInfo.Direction.None;
		}
	}
}
