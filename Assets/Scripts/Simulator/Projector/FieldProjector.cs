using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Visualizable;

namespace Simulator {
	public class FieldProjector {
		private FieldItem[,] field;

		private int width;
		private int height;

		public FieldProjector (FieldItem[,] field) {
			this.field = field;
			this.width = field.GetLength(0);
			this.height = field.GetLength(1);
		}

		public Projection CalcSnakeView (Vector2Int pos, MoveInfo.Direction dir, float halfViewAngle) {

			return default(Projection);
		}
	}
}