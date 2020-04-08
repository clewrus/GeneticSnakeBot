
using UnityEngine;

namespace Simulator {
	public class SnakeInfo {
		public int maxLength;
		public float bodyWidth;

		public float maxValue;
		public float halfViewAngle;

		public int eyeQuality;
		public int denseLayerSize;

		public float cullingDistance;
		public ScuamaPatern scuamaPatern;

		public static float CalcKindship (SnakeInfo a, SnakeInfo b) {
			float distance = 0.7f * ScuamaPatern.SqrDistance(a.scuamaPatern, b.scuamaPatern);
			distance += 0.3f * Mathf.Pow(a.maxLength - b.maxLength, 2);

			return 1f / (1 + Mathf.Sqrt(distance));
		}

		public struct ScuamaPatern {
			public (float z, float ratio) giroid0;
			public (float z, float ratio) giroid1;
			public (float z, float ratio) giroid2;

			public Vector4 backgroundColor;
			public Vector4 color1;
			public Vector4 color2;

			private Vector4[] giroidConfig;
			private Color[] colorArray;


			public Vector4[] GetGyroidConfig () {
				if (giroidConfig == null) {
					giroidConfig = new Vector4[] {
						new Vector4(giroid0.z, giroid1.z, giroid2.z, 0),
						new Vector4(giroid0.ratio, giroid1.ratio, giroid2.ratio, 0)
					};
				}
				return giroidConfig;
			}

			public Color[] GetColorArray () {
				if (colorArray == null) {
					colorArray = new Color[] { backgroundColor, color1, color2 };
				}
				return colorArray;
			}

			public static float SqrDistance (ScuamaPatern a, ScuamaPatern b) {
				float res = 0;

				var gyroidA = a.GetGyroidConfig();
				var gyroidB = b.GetGyroidConfig();

				res += (gyroidA[0] - gyroidB[0]).sqrMagnitude;
				res += (gyroidA[1] - gyroidB[1]).sqrMagnitude;

				var colorsA = a.GetColorArray();
				var colorsB = b.GetColorArray();

				for (int i = 0; i < colorsA.Length; i++) {
					res += ((Vector4)(colorsA[i] - colorsB[i])).sqrMagnitude;
				}

				return res;
			}
		}
	}
}