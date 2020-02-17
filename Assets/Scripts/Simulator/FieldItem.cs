

namespace Simulator {
	public struct FieldItem {
		public enum ItemType {None, Food, Snake};
		public enum Flag {None = 0, Head = 1}

		public uint flags;
		public ItemType type;
		public int id;

		bool HasFlag (Flag tarFlag) {
			return (flags & (uint)tarFlag) != 0;
		}

		void SetFlag (Flag tarFlag) {
			flags |= (uint)tarFlag;
		}

		void ClearFlags () {
			flags = 0;
		}
	}
}