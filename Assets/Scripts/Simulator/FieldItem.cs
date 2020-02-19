
namespace Simulator {
	public struct FieldItem {
		public enum ItemType {None, Food, Snake};
		public enum Flag {None = 0, Head = 1}

		public int id;
		public int frameOfLastUpdate;
		public int prevNeighborPos;

		public byte flags;
		public ItemType type;
	}
}