using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Assets.Scripts.Simulator.Projector {
	public class SquareTree {

		#region Fields
		private Dictionary<(int x, int y), SquareTree> allItems;

		private (int x, int y) minCorner;
		private (int x, int y) dimentions;

		private bool hasChildren;
		private SquareTree[] children;

		private int contentCount;
		private (int x, int y)[] content;


		private readonly int ITEMS_IN_CONTENT = 8;
		#endregion

		private SquareTree (Dictionary<(int x, int y), SquareTree> itemsDictionary) {
			this.allItems = itemsDictionary;

			children = new SquareTree[4];
			contentCount = 0;
			content = new (int x, int y)[ITEMS_IN_CONTENT];
		}

		#region Public

		public SquareTree (int minX, int minY, int width, int height) : this(new Dictionary<(int x, int y), SquareTree>()) {
			minCorner = (minX, minY);
			dimentions = (width, height);
		}

		public bool Add (int x, int y) {
			(int x, int y) relPos = (x - minCorner.x, y - minCorner.y);
			if (relPos.x < 0 || relPos.y < 0 || dimentions.x <= relPos.x || dimentions.y <= relPos.y) return false;

			if (allItems.ContainsKey((x, y))) return true;

			if (contentCount < ITEMS_IN_CONTENT) {
				content[contentCount++] = (x, y);
				allItems.Add((x, y), this);
				return true;
			}

			if (!hasChildren) {
				hasChildren = true;
				InitializeChildren();
			}

			return children[FindChildIndex(x, y)].Add(x, y);
		}

		public int FindItemsInCircle (int x, int y, float R, List<(int x, int y)> foundItems) {
			Debug.Assert(foundItems != null, "Can't add items in null list.");
			if (!HasIntersection(x, y, R)) return 0;

			int foundCount = 0;
			for (int i = 0; i < contentCount; i++) {
				if (System.Math.Pow(x - content[i].x, 2) + System.Math.Pow(y - content[i].y, 2) < R * R) {
					++foundCount;
					foundItems.Add(content[i]);
				}
			}

			if (hasChildren) {
				for (int i = 0; i < children.Length; i++) {
					foundCount += children[i].FindItemsInCircle(x, y, R, foundItems);
				}
			}

			return foundCount;
		}

		public bool Remove (int x, int y) {
			if (!allItems.TryGetValue((x, y), out SquareTree tarNode)) return false;

			bool itemFound = false;
			for (int i = 0; i < tarNode.contentCount; i++) {
				if (itemFound) {
					tarNode.content[i - 1] = tarNode.content[i];
				}
				itemFound = itemFound || (tarNode.content[i] == (x, y));
			}

			tarNode.contentCount -= 1;
			return true;
		}

		public void Clear () {
			contentCount = 0;

			if (hasChildren) {
				for (int i = 0; i < children.Length; i++) {
					children[i].Clear();
				}
			}
		}

		#endregion

		private bool HasIntersection (int x, int y, float R) {
			(float x, float y) halfDim = ((float)dimentions.x / 2f, (float)dimentions.y / 2f);
			(float x, float y) center = (minCorner.x + halfDim.x, minCorner.y + halfDim.y);

			float circumscribedRSquared = halfDim.x * halfDim.x + halfDim.y * halfDim.y;
			return (center.x - x)*(center.x - x) + (center.y - y)*(center.y - y) <= circumscribedRSquared;
		}

		private int FindChildIndex (int x, int y) {
			(int x, int y) relPos = (x - minCorner.x, y - minCorner.y);
			if (relPos.x < dimentions.x / 2) {
				if (relPos.y < dimentions.y / 2) {
					return 2;
				} else {
					return 1;
				}
			} else {
				if (relPos.y < dimentions.y / 2) {
					return 3;
				} else {
					return 0;
				}
			}
		}

		private void InitializeChildren () {
			var d = dimentions;
			var m = minCorner;

			children[0] = GetNewNode(m.x + d.x/2, m.y + d.y/2, (d.x+1)/2, (d.y+1)/2);
			children[1] = GetNewNode(m.x, m.y + d.y/2, d.x/2, (d.y+1)/2);
			children[2] = GetNewNode(m.x, m.y, d.x/2, d.y/2);
			children[3] = GetNewNode(m.x + d.x/2, m.y, (d.x+1)/2, d.y/2);
		}

		private SquareTree GetNewNode (int minX, int minY, int width, int height) {
			var requestedNode = new SquareTree(this.allItems);

			requestedNode.minCorner = (minX, minY);
			requestedNode.dimentions = (width, height);

			return requestedNode;
		}
	}
}
