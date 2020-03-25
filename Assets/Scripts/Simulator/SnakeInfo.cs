
using UnityEngine;

namespace Simulator {
	public class SnakeInfo {
		public int maxLength;
		public float maxValue;
		public float halfViewAngle;

		public int eyeQuality;
		public int denseLayerSize;

		public Vector3 colorValues;
		public float cullingDistance;

		public static float CalcKindship (SnakeInfo a, SnakeInfo b) {
			float distance = (a.colorValues - b.colorValues).sqrMagnitude;
			distance += Mathf.Pow(a.maxLength - b.maxLength, 2);
			distance += Mathf.Pow(a.halfViewAngle - b.halfViewAngle, 2);
			distance += Mathf.Pow(a.denseLayerSize - b.denseLayerSize, 2);

			return Mathf.Sqrt(distance);
		}
	}
}