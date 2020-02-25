using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Simulator {
	public class HumanPlayer : MonoBehaviour, IPlayer {
		SnakeInfo info;
		MoveInfo moveInfo;

		private void Start () {
			info = new SnakeInfo {
				maxLength = 7,
				maxValue = 20,
				halfViewAngle = 0,
				eyeQuality = 0,
				denseLayerSize = 0,
				colorValues = new Vector3 (4, 3.23f, 8),
				cullingDistance = 0
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
			
		}

		public MoveInfo MakeMove (Projection playerInput) {
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