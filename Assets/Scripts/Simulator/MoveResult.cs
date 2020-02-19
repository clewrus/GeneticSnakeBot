

namespace Simulator {
	public struct MoveResult {
		public int id;
		public float value;
		
		public byte flag;
		public enum State {None=0, IsAlive=1, GotAFood=2, WasBited=4, Bited=8};		
	}
}