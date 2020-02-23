
namespace Simulator {
	public struct FieldItem {
		public enum ItemType {None, Food, Snake, Wall};
		public enum Flag {None = 0, Head = 1, Shortened = 2};

		public int id;
		public int frameOfLastUpdate;
		public int prevNeighborPos;

		public float value;

		public byte flags;
		public ItemType type;
		public MoveInfo.Direction dir;
	}
}