using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Simulator {
	public class HumanPlayer : MonoBehaviour, IPlayer {
		SnakeInfo info;
		MoveInfo moveInfo;

		public int? KnownId { get; private set; }

		private void Awake () {
			info = new SnakeInfo {
				maxLength = 5,
				maxValue = 5,
				halfViewAngle = 0.75f * 3.1416f,
				eyeQuality = 15,
				denseLayerSize = 0,
				colorValues = new Vector3 (4, 3.23f, 8),
				cullingDistance = 5
			};

			moveInfo.snakeInfo = info;
		}

		private void Update () {
			UpdateMoveInfo();
		}

		public SnakeInfo GetSnakeInfo () {
			return info;
		}

		public void HandleMoveResult (MoveResult result) {
			if (KnownId == null) {
				KnownId = result.id;
			}

			Debug.Log($"HumanPlayer HandleMove {result}");
		}

		public MoveInfo MakeMove (MoveInfo.Direction dir, Projection playerInput) {
			return moveInfo;
		}

		private void UpdateMoveInfo () {
			var dir = Vector2.zero;
			if (Input.GetKeyDown(KeyCode.W)) {
				dir += Vector2.up;
			} if (Input.GetKeyDown(KeyCode.S)) {
				dir += Vector2.down;
			} if (Input.GetKeyDown(KeyCode.A)) {
				dir += Vector2.left;
			} if (Input.GetKeyDown(KeyCode.D)) {
				dir += Vector2.right;
			}

			if (Mathf.Abs(dir.x) + Mathf.Abs(dir.y) < 0) return;

			if (dir.x > 0) {
				moveInfo.dir = MoveInfo.Direction.Right;
			} else if (dir.x < 0) {
				moveInfo.dir = MoveInfo.Direction.Left;
			} else if (dir.y > 0) {
				moveInfo.dir = MoveInfo.Direction.Up;
			} else if (dir.y < 0) {
				moveInfo.dir = MoveInfo.Direction.Down;
			}
		}
	}
}