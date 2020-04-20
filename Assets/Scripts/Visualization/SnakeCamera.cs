using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Visualization {
	[RequireComponent(typeof(Camera))]
	public class SnakeCamera : MonoBehaviour, IVisualizerObserver {

		public Vector2Int FieldSize { set; private get; }

		public int ExpectedPlacementId { get; set; }
		public System.EventHandler ObservationFinished { get; set; }

		private float? timeOfLastUpdate;
		private float? previousMoveDuration;
		private Coroutine cameraMovingCoroutine;

		private Transform backgroundTransform;

		[SerializeField] [Range(0.1f, 1)] private float moveFluidity = 0.8f;
		[SerializeField] [Range(0.1f, 2)] private float massCenterOffset = 0.8f;
		[SerializeField] [Range(0, 1)] private float prevMoveDurInfluence = 0.3f;
		[Space]
		[SerializeField] private Material backgroundMaterial = null;

		public void Restart () {
			if (cameraMovingCoroutine != null) {
				StopCoroutine(cameraMovingCoroutine);
				cameraMovingCoroutine = null;
			}

			timeOfLastUpdate = null;
			previousMoveDuration = null;
		}

		public void PlacementChangedHandler (IEnumerable<Vector2Int> placement, bool exists, bool wasRemovedRecently) {
			if (wasRemovedRecently) OnObservationFinished();

			if (cameraMovingCoroutine != null) {
				StopCoroutine(cameraMovingCoroutine);
				cameraMovingCoroutine = null;
			}

			if (!exists) return;

			Vector3 curPos = transform.localPosition;
			Vector3 nwPos = CalcNewCameraPos(placement);
			nwPos.z = transform.localPosition.z;

			previousMoveDuration = (previousMoveDuration.HasValue) ? (float?)Mathf.Min(1, previousMoveDuration.Value) : null;
			float predictedMoveDuration = PredictMoveDuration(out previousMoveDuration);
			cameraMovingCoroutine = StartCoroutine(CameraMove(curPos, nwPos, predictedMoveDuration));

			timeOfLastUpdate = Time.time;
		}

		private IEnumerator CameraMove (Vector3 oldPos, Vector3 nwPos, float timeToMove) {
			if (timeToMove > 0) {
				float t = Time.deltaTime / timeToMove;
				while (t < 1) {
					var curPos = Vector3.Lerp(oldPos, nwPos, Mathf.Pow(t, moveFluidity));
					transform.localPosition = curPos;
					t += Time.deltaTime / timeToMove;
					yield return null;
				}
			}

			transform.localPosition = nwPos;
		}

		private float PredictMoveDuration (out float? prevMoveDuration) {
			if (!timeOfLastUpdate.HasValue) {
				prevMoveDuration = null;
				return 0;
			} else if (!previousMoveDuration.HasValue) {
				prevMoveDuration = Time.time - timeOfLastUpdate;
				return previousMoveDuration.Value;
			} else {
				var curMoveDuration = Time.time - timeOfLastUpdate;
				prevMoveDuration = curMoveDuration;
				return Mathf.Lerp(curMoveDuration.Value, previousMoveDuration.Value, prevMoveDurInfluence);
			}
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

		private void OnObservationFinished () {
			ObservationFinished?.Invoke(this, new System.EventArgs());
		}

		private void OnPreRender () {
			if (backgroundMaterial != null) {
				UpdateBackground();
			}
		}

		private void UpdateBackground () {
			if (backgroundTransform == null) {
				InitializeBackground();
			}

			var cam = GetComponent<Camera>();
			backgroundTransform.localScale = new Vector3(
				2 * cam.aspect * cam.orthographicSize + 1,
				2 * cam.orthographicSize + 1,
				1
			);
		}

		private void InitializeBackground () {
			var background = new GameObject("Background", typeof(MeshFilter), typeof(MeshRenderer));
			background.layer = gameObject.layer;

			background.GetComponent<MeshFilter>().mesh = new Mesh() {
				vertices = new Vector3[] { -0.5f*Vector2.one, Vector2.up-0.5f*Vector2.one, 0.5f*Vector2.one, Vector2.right-0.5f*Vector2.one },
				triangles = new int[] { 0, 1, 3, 3, 1, 2 }
			};

			background.GetComponent<MeshRenderer>().material = backgroundMaterial;

			backgroundTransform = background.transform;
			backgroundTransform.SetParent(transform, false);

			backgroundTransform.localPosition =  (Mathf.Abs(transform.localPosition.z) + 1) * Vector3.forward;
		}
	}
}


