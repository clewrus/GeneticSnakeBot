using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simulator {
	public class SquareTree {

		private readonly int ITEMS_IN_CONTENT = 8;

		#region Fields
		private Dictionary<(int x, int y), SquareTree> allItems;

		private (int x, int y) minCorner;
		private (int x, int y) dimentions;

		private bool hasChildren;
		private SquareTree[] children;

		private int contentCount;
		private (int x, int y)[] content;
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

			if (hasChildren) {
				return children[FindChildIndex(x, y)].Add(x, y);
			}
			
			if (contentCount < ITEMS_IN_CONTENT) {
				content[contentCount++] = (x, y);
				allItems.Add((x, y), this);
				return true;
			}

			hasChildren = true;
			InitializeChildren();
			RedistributeContent();

			contentCount = 0;
			content = null;

			return children[FindChildIndex(x, y)].Add(x, y);
		}

		public int FindItemsInCircle (int x, int y, float R, List<(int x, int y)> foundItems) {
			Debug.Assert(foundItems != null, "Can't add items in null list.");
			if (!HasIntersection(x, y, R)) return 0;

			int foundCount = 0;

			if (hasChildren) {
				for (int i = 0; i < children.Length; i++) {
					foundCount += children[i].FindItemsInCircle(x, y, R, foundItems);
				}
			} else {
				for (int i = 0; i < contentCount; i++) {
					int deltaX = x - content[i].x, deltaY = y - content[i].y;
					if (deltaX * deltaX + deltaY * deltaY < R * R) {
						++foundCount;
						foundItems.Add(content[i]);
					}
				}
			}

			return foundCount;
		}

		public bool Remove (int x, int y) {
			if (!allItems.TryGetValue((x, y), out SquareTree tarNode)) return false;
			allItems.Remove((x, y));

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
			(int x, int y) maxCorner = (minCorner.x + dimentions.x - 1, minCorner.y + dimentions.y - 1);
			if (minCorner.x <= x && x <= maxCorner.x && minCorner.y <= y && y <= maxCorner.y) return true;

			System.Func<int, int, int> MinAbs = (int a, int b) => {
				int aAbs = (a < 0) ? -a : a;
				int bAbs = (b < 0) ? -b : b;
				return (aAbs < bAbs) ? aAbs : bAbs;
			};

			if (minCorner.x <= x && x <= maxCorner.x) {
				int absDelta = MinAbs(y - minCorner.y, y - maxCorner.y);
				return absDelta < R;
			} else if (minCorner.y <= y && y <= maxCorner.y) {
				int absDelta = MinAbs(x - minCorner.x, x - maxCorner.x);
				return absDelta < R;
			}

			int deltaX = MinAbs(x - minCorner.x, x - maxCorner.x);
			int deltaY = MinAbs(y - minCorner.y, y - maxCorner.y);

			return deltaX * deltaX + deltaY * deltaY <= R * R;

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

		private void RedistributeContent () {
			foreach (var item in content) {
				var suitedChild = children[FindChildIndex(item.x, item.y)];
				allItems.Remove(item);
				suitedChild.Add(item.x, item.y);
			}
		}

		private SquareTree GetNewNode (int minX, int minY, int width, int height) {
			var requestedNode = new SquareTree(this.allItems);

			requestedNode.minCorner = (minX, minY);
			requestedNode.dimentions = (width, height);

			return requestedNode;
		}
	}
}
