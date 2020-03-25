using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Visualization {
	[RequireComponent(typeof(Camera))]
	public class SnakeCamera : MonoBehaviour, IVisualizerObserver {

		public Vector2Int FieldSize { set; private get; }

		public int ExpectedPlacementId { get; set; }

		public void PlacementChangedHandler (IEnumerable<Vector2Int> placement, bool exists, bool wasRemovedRecently) {
			if (!exists) return;

			Vector3 nwPos = CalcNewCameraPos(placement);
			nwPos.z = transform.localPosition.z;
			transform.localPosition = nwPos;
		}

		private Vector2 CalcNewCameraPos (IEnumerable<Vector2Int> placement) {
			Vector2 averagePos = default;
			int length = 0;

			foreach (var snakeTile in placement) {
				++length;
				averagePos += snakeTile;
			}

			averagePos /= length;
			var nwCameraPos = averagePos + 0.5f * (FieldSize.x * Vector2.left + FieldSize.y * Vector2.down + 3*Vector2.one);
			return nwCameraPos;
		}
	}
}


