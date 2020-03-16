using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.SnakeTiles {
	public struct WallOrientation {
		public UInt32 Value { get; private set; }

		public WallOrientation (params bool[] neighbors) {
			Debug.Assert(neighbors.Length == 8, "Wall orientation must have 8 neighbors as input.");

			Value = 0;
			var n = neighbors;
			for (uint i = 0; i < 4; ++i) {
				UInt32 curOrientation = GetOrientation(n[(2 * i + 7) % 8], n[2 * i], n[(2 * i + 1) % 8]);

				UInt32 curRot = curOrientation & 3;
				UInt32 nwRot = (curRot + i) & 3;

				curOrientation = (curOrientation & ~((UInt32)3)) | nwRot;
				Value |= (curOrientation << (int)(4 * i));
			}
		}

		private UInt32 GetOrientation (bool left, bool corner, bool top) {
			if (left && corner && top) {
				return 12; // 11 00 
			}else if (!left && corner && top) {
				return  3; // 00 11 
			} else if (left && !corner && top) {
				return 8; // 10 00
			} else if (!left && !corner && top) {
				return  3; // 00 11
			} else if (left && corner && !top) {
				return  0; // 00 00
			} else if (!left && corner && !top) {
				return  4; // 01 00
			} else if (left && !corner && !top) {
				return  0; // 00 00
			} else if (!left && !corner && !top) {
				return  4; // 01 00
			}

			throw new System.Exception("Imposible code executed");
		}
	}
}
