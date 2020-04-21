using UnityEngine;

namespace Simulator {
	public struct MoveResult {
		public int id;
		public float value;
		public float eatenValue;

		public Vector2Int headPos;
		public MoveInfo.Direction headDir;
		
		public byte flag;
		public enum State {None=0, IsAlive=1, GotAFood=2, WasBited=4, Bited=8, EatSelf = 16 };

		public override string ToString () {
			return $"pos: {headPos.ToString()}, dir: {headDir.ToString()}\nval: {value}, flag: {flag}";
		}
	}
}