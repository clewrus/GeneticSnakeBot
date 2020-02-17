

namespace Simulator {
	public class MoveInfo {
		public enum Direction {None, Forward, Right, Stop, Left};

		public int id;
		public Direction dir = Direction.None;
		public SnakeInfo snakeInfo = null;

		public MoveInfo (int id, Direction dir, SnakeInfo info=null) {
			this.id = id;
			this.dir = dir;
			this.snakeInfo = info;
		}
	}
}