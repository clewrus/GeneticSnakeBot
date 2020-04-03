﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Visualization {
	[RequireComponent(typeof(Camera))]
	public class SnakeCamera : MonoBehaviour, IVisualizerObserver {

		public Vector2Int FieldSize { set; private get; }

		public int ExpectedPlacementId { get; set; }

		private float? timeOfLastUpdate;
		private float? previousMoveDuration;
		private Coroutine cameraMovingCoroutine;

		[SerializeField] [Range(0.1f, 1)] private float moveFluidity = 0.8f;
		[SerializeField] [Range(0.1f, 2)] private float massCenterOffset = 0.8f;
		[SerializeField] [Range(0, 1)] private float prevMoveDurInfluence = 0.3f;

		public void PlacementChangedHandler (IEnumerable<Vector2Int> placement, bool exists, bool wasRemovedRecently) {
			if (!exists) return;

			Vector3 nwPos = CalcNewCameraPos(placement);
			nwPos.z = transform.localPosition.z;

			float predictedMoveDuration = 0;

			if (!timeOfLastUpdate.HasValue) {
				transform.localPosition = nwPos;
			} else if (!previousMoveDuration.HasValue) {
				previousMoveDuration = Time.time - timeOfLastUpdate;
				predictedMoveDuration = previousMoveDuration.Value;
			} else {
				var curMoveDuration = Time.time - timeOfLastUpdate;
				predictedMoveDuration = PredictMoveDuration(previousMoveDuration.Value, curMoveDuration.Value);
				previousMoveDuration = curMoveDuration;
			}

			if (cameraMovingCoroutine != null) {
				StopCoroutine(cameraMovingCoroutine);
			}

			var curPos = transform.localPosition;
			cameraMovingCoroutine = StartCoroutine(CameraMove(curPos, nwPos, predictedMoveDuration));
			timeOfLastUpdate = Time.time;
		}

		private IEnumerator CameraMove (Vector3 oldPos, Vector3 nwPos, float timeToMove) {
			float t = Time.deltaTime / timeToMove;
			while (t < 1) {
				var curPos = Vector3.Lerp(oldPos, nwPos, Mathf.Pow(t, moveFluidity));
				transform.localPosition = curPos;
				t += Time.deltaTime / timeToMove;
				yield return null;
			}
			transform.localPosition = nwPos;
		}

		private float PredictMoveDuration (float prevDur, float curDur) {
			return Mathf.Lerp(curDur, prevDur, prevMoveDurInfluence);
		}

		private Vector2 CalcNewCameraPos (IEnumerable<Vector2Int> placement) {
			Vector2 averagePos = default;

			float totalMass = 0;
			float curMass = 1;

			foreach (var snakeTile in placement) {
				totalMass += curMass;
				averagePos += curMass * (Vector2)snakeTile;

				curMass *= massCenterOffset;
			}

			averagePos /= totalMass;
			var nwCameraPos = averagePos + 0.5f * (FieldSize.x * Vector2.left + FieldSize.y * Vector2.down + 3*Vector2.one);
			return nwCameraPos;
		}
	}
}


