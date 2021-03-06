

namespace Simulator {
	public struct MoveInfo {
		public enum Direction {None, Up, Right, Down, Left};

		public int id;
		public float valueUsed;

		public Direction dir;
		public SnakeInfo snakeInfo;

		public MoveInfo (int id, float valueUsed, Direction dir, SnakeInfo info=null) {
			this.id = id;
			this.valueUsed = valueUsed;
			this.dir = dir;
			this.snakeInfo = info;
		}

		public override string ToString () {
			return $"{id} {dir} {valueUsed}";
		}
	}
}