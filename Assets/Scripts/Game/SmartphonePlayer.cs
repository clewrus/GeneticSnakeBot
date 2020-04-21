using Simulator;
using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace Game {
	public class SmartphonePlayer : MonoBehaviour, IPlayer, IHumanPlayer {
		public SnakeInfo info;
		private MoveInfo moveInfo;

		public EventHandler DirectionSelected { get; set; }
		public bool NeedsProjection => false;

		private bool isTouching;
		private Vector2? touchStartPos;

		private void Awake () {
			info = new SnakeInfo {
				maxLength = 5,
				bodyWidth = 0.6f,
				maxValue = 1,
				halfViewAngle = 0.75f * 3.1416f,
				eyeQuality = 15,
				denseLayerSize = 0,
				cullingDistance = 5,
				scuamaPatern = new SnakeInfo.ScuamaPatern {
					giroid0 = (0.3f, 1.5f),
					giroid1 = (0.5f, 1.0f),
					giroid2 = (1.2f, 0.9f),

					backgroundColor = new Vector4(0.1f, 0.4f, 0.1f, 0),
					color1 = new Vector4(0.8f, 0.8f, 0.1f, 0),
					color2 = new Vector4(0.8f, 0.1f, 0.1f, 0),
				}
			};

			moveInfo.snakeInfo = info;
		}

		private void Update () {
			if (!isTouching) {
				if (Input.GetMouseButton(0)) {
					isTouching = true;
					touchStartPos = (Vector2)Input.mousePosition;
				}
			} else if (!Input.GetMouseButton(0)) {
				var curPos = (Vector2)Input.mousePosition;
				var delta = curPos - touchStartPos.Value;

				isTouching = false;
				touchStartPos = null;
				if (delta.magnitude > 20) {
					UpdateMoveInfo(delta);
				}
			}
		}

		private bool TrySelectNewTouch (out Touch nwTouch) {
			var touches = Input.touches;

			foreach (var touch in touches) {
				if (touch.phase == TouchPhase.Began) {
					nwTouch = touch;
					return true;
				}
			}

			nwTouch = default;
			return false;
		}

		public SnakeInfo GetSnakeInfo () {
			return info;
		}

		public void HandleMoveResult (MoveResult result) { }

		public MoveInfo MakeMove (MoveInfo.Direction dir, Projection playerInput) {
			return moveInfo;
		}

		private void OnDirectionSelected () {
			DirectionSelected?.Invoke(this, new EventArgs());
		}

		private void UpdateMoveInfo (Vector2 dir) {
			if (Mathf.Abs(dir.x) >= Mathf.Abs(dir.y)) {
				dir.y = 0;
			} else if (Mathf.Abs(dir.x) <= Mathf.Abs(dir.y)) {
				dir.x = 0;
			}

			dir = dir.normalized;
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

			if (dir != Vector2.zero) {
				OnDirectionSelected();
			}
		}
	}
}