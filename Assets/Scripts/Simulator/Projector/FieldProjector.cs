using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        public Projection CalcSnakeView (Vector2Int pos, Vector2 dir, float halfViewAngle) {

            return default(Projection);
        }
    }
}