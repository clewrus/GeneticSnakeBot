using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Visualizable;

namespace Simulator {
	public class FieldProjector {
		private FieldItem[,] field;

		private int width;
		private int height;
		private SquareTree fieldTree;
		private System.Func<int, SnakeInfo> GetSnakeInfo;

		#region Buffers

		private List<(int x, int y)> visiblePositions;

		#endregion

		public FieldProjector (FieldItem[,] field, System.Func<int, SnakeInfo> GetSnakeInfo) {
			this.field = field;
			this.width = field.GetLength(0);
			this.height = field.GetLength(1);

			this.GetSnakeInfo = GetSnakeInfo;
			visiblePositions = new List<(int x, int y)>();

			fieldTree = new SquareTree(-1, -1, this.width + 2, this.height + 2);
			AddBorder();
		}

		public void UpdateAtPositions (IEnumerable<(int x, int y)> changedItemPositions) {
			foreach (var pos in changedItemPositions) {
				if (field[pos.x, pos.y].type == FieldItem.ItemType.None) {
					fieldTree.Remove(pos.x, pos.y);
				} else {
					fieldTree.Add(pos.x, pos.y);
				}
			}
		}

		public Projection CalcSnakeView (
			(int x, int y) pos,
			MoveInfo.Direction dir,
			float cullingDistance,
			float halfViewAngle,
			int eyeQuality
		) {
			visiblePositions.Clear();
			fieldTree.FindItemsInCircle(pos.x, pos.y, cullingDistance, visiblePositions);

			var dirVec = DirToVec(dir);

			var continuousProj = GenerateContinuousProjection(pos, dirVec, halfViewAngle, field, visiblePositions);
			var discreteProj = GenerateDiscreteProjection(continuousProj, eyeQuality);

			return discreteProj;
		}

		private ContinuousProjection GenerateContinuousProjection (
					(int x, int y) centerPos,
					(int x, int y) dirVec,
					float halfAngle,
					FieldItem[,] field,
					IEnumerable<(int x, int y)> visiblePositions
		) {
			ContinuousProjection resultProjection = default;
			resultProjection.Initialize();

			var selfItem = field[centerPos.x, centerPos.y];

			double minVisibleXPorj = System.Math.Cos(halfAngle);
			foreach (var itemPos in visiblePositions) {
				var relPos = (x: itemPos.x-centerPos.x, y: itemPos.y-centerPos.y);
				var localPos = (x: dirVec.x*relPos.x + dirVec.y*relPos.y, y: -dirVec.y*relPos.x + dirVec.x*relPos.y);
				if (localPos.x == 0 && localPos.y == 0) continue;
				double oneOverR = 1 / System.Math.Sqrt(localPos.x*localPos.x + localPos.y*localPos.y);

				if (localPos.x * oneOverR < minVisibleXPorj) continue;
				if (!CalcBorderAngles(localPos, out float start, out float end)) continue;

				start = (start - ((float)System.Math.PI - halfAngle)) / (2 * halfAngle);
				end = (end - ((float)System.Math.PI - halfAngle)) / (2 * halfAngle);

				start = (start < 0) ? 0 : start;
				end = (1 < end) ? 1 : end;

				var tarItem = new FieldItem { type = FieldItem.ItemType.Wall };
				if (0<=itemPos.x && itemPos.x<field.GetLength(0) && 0<=itemPos.y && itemPos.y<field.GetLength(1)) {
					tarItem = field[itemPos.x, itemPos.y];
				}

				switch (tarItem.type) {
					case FieldItem.ItemType.Food: resultProjection.food.Add((start, end, (float)oneOverR)); break;
					case FieldItem.ItemType.Wall: resultProjection.obstacles.Add((start, end, (float)oneOverR)); break;
					case FieldItem.ItemType.Snake: {
						float kindShip = SnakeInfo.CalcKindship(GetSnakeInfo(selfItem.id), GetSnakeInfo(tarItem.id));
						resultProjection.snakes.Add((start, end, (float)oneOverR, kindShip));
					}; break;

					case FieldItem.ItemType.None: break;
				}
			}

			return resultProjection;
		}

		private bool CalcBorderAngles ((int x, int y) pos, out float start, out float end) {
			start = end = 0;
			if (pos.x == 0 && pos.y == 0) return false;

			double halfDiag = 0.7071;
			(double x, double y) v1, v2;
			if (pos.x == 0) {
				v1.y = v2.y = -System.Math.Sign(pos.y) * halfDiag;
				v1.x = halfDiag; v2.x = -halfDiag;
			} else if (pos.y == 0) {
				v1.x = v2.x = -System.Math.Sign(pos.x) * halfDiag;
				v1.y = halfDiag; v2.y = -halfDiag;
			} else {
				v1 = (System.Math.Sign(pos.x) * halfDiag, -System.Math.Sign(pos.y) * halfDiag);
				v2 = (-v1.x, -v1.y);
			}

			double phi1 = System.Math.Atan2(pos.y + v1.y, pos.x + v1.x) + System.Math.PI;
			double phi2 = System.Math.Atan2(pos.y + v2.y, pos.x + v2.x) + System.Math.PI;

			if (pos.y == 0 && pos.x < 0) {
				phi2 += 2 * System.Math.PI;
			}

			start = (float)System.Math.Min(phi1, phi2);
			end = (float)System.Math.Max(phi1, phi2);
			return true;
		}

		private Projection GenerateDiscreteProjection (ContinuousProjection source, int eyeQuality) {
			var flatFood = FlattenProjection<(float s, float e, float d)>(source.food, (info) => info, (oldInfo, nwInfo) => nwInfo);
			var flatWalls = FlattenProjection<(float s, float e, float d)>(source.obstacles, (info) => info, (oldInfo, nwInfo) => nwInfo);
			var flatSnakes = FlattenProjection<(float s, float e, float d, float k)>(source.snakes, (i) => (i.s, i.e, i.d), (oldI, nwI) => (nwI.start, nwI.end, nwI.density, oldI.k));

			float cellWidth = 1f / eyeQuality;
			var discreteProjection = new Projection(eyeQuality);

			FillDiscreteProjection(discreteProjection.food, flatFood, cellWidth);
			FillDiscreteProjection(discreteProjection.wall, flatWalls, cellWidth);
			FillDiscreteProjection(discreteProjection.snake, ExtractDensity(flatSnakes), cellWidth);
			FillDiscreteProjection(discreteProjection.kinship, ExtractKinship(flatSnakes), cellWidth, dontStretchOne:true);

			return discreteProjection;
		}

		private LinkedList<T> FlattenProjection<T> (
			SortedSet<T> projection, 
			System.Func<T, (float start, float end, float density)> GetProjectedPieceInfo,
			System.Func<T, (float start, float end, float density), T> UpdatePieceValue
		) {
			var flattenProj = new LinkedList<T>(projection);
			if (flattenProj.Count == 0) return flattenProj;

			var curPiece = flattenProj.First;
			var nextPiece = curPiece.Next;

			while (nextPiece != null) {
				var curPieceInfo = GetProjectedPieceInfo(curPiece.Value);
				var nextPieceInfo = GetProjectedPieceInfo(nextPiece.Value);

				if (curPieceInfo.end <= nextPieceInfo.start) {
					curPiece = nextPiece;
					nextPiece = curPiece.Next;
					continue;
				}

				bool isContainedInCurrent = (nextPieceInfo.end <= curPieceInfo.end);
				bool isOverlapCurrent = (curPieceInfo.density < nextPieceInfo.density);

				if (isContainedInCurrent && isOverlapCurrent) {
					var updatedCurPieceInfo = curPieceInfo;
					updatedCurPieceInfo.end = nextPieceInfo.start;
					curPiece.Value = UpdatePieceValue(curPiece.Value, updatedCurPieceInfo);

					curPieceInfo.start = nextPieceInfo.end;
					var nwPieceInfo = UpdatePieceValue(curPiece.Value, curPieceInfo);
					InsertCuttedPiece<T>(nextPiece, nwPieceInfo, GetProjectedPieceInfo);
					curPiece = nextPiece;
					nextPiece = curPiece.Next;

				} else if (isContainedInCurrent && !isOverlapCurrent) {
					flattenProj.Remove(nextPiece);
					nextPiece = curPiece.Next;

				} else if (!isContainedInCurrent && isOverlapCurrent) {
					curPieceInfo.end = nextPieceInfo.start;
					curPiece.Value = UpdatePieceValue(curPiece.Value, curPieceInfo);
					curPiece = nextPiece;
					nextPiece = curPiece.Next;

				} else if (!isContainedInCurrent && !isOverlapCurrent) {
					nextPieceInfo.start = curPieceInfo.end;
					var nwPieceInfo = UpdatePieceValue(nextPiece.Value, nextPieceInfo);

					flattenProj.Remove(nextPiece);
					InsertCuttedPiece<T>(curPiece, nwPieceInfo, GetProjectedPieceInfo);

					nextPiece = curPiece.Next;
				} else {
					throw new System.Exception("Imposible to get here");
				}
			}

			return flattenProj;
		}

		private void InsertCuttedPiece<T> (
			LinkedListNode<T> rootPiece, 
			T nwPiece,
			System.Func<T, (float start, float end, float density)> GetProjectedPieceInfo
		) {
			var rootEnd = GetProjectedPieceInfo(rootPiece.Value).end;
			var curNode = rootPiece;
			var nextNode = curNode.Next;

			while (nextNode != null && GetProjectedPieceInfo(nextNode.Value).start < rootEnd) {
				curNode = nextNode;
				nextNode = curNode.Next;
			}

			rootPiece.List.AddAfter(curNode, nwPiece);
		}

		private void FillDiscreteProjection (
			float[] discrete,
			IEnumerable<(float start, float end, float value)> projection,
			float cellWidth,
			bool dontStretchOne=false
		) {
			foreach (var piece in projection) {
				int startIndex = (int)(piece.start / cellWidth);
				int endIndex = (int)(piece.end / cellWidth);

				if (startIndex == endIndex) {
					if (dontStretchOne && piece.value == 1) {
						discrete[startIndex] = 1;
					} else {
						discrete[startIndex] += piece.value * ((piece.end - piece.start) / cellWidth);
					}
					
					continue;
				}

				if (dontStretchOne && piece.value == 1) {
					discrete[startIndex] = 1;
				} else {
					discrete[startIndex] += piece.value * ((startIndex + 1) - piece.start / cellWidth);
				}

				for (int i = startIndex + 1; i < endIndex; i++) {
					discrete[i] += piece.value;
				}

				if (dontStretchOne && piece.value == 1) {
					discrete[endIndex] = 1;
				} else {
					discrete[endIndex] += piece.value * (piece.end / cellWidth - endIndex);
				}
			}
		}

		private (int x, int y) DirToVec (MoveInfo.Direction dir) {
			switch (dir) {
				case MoveInfo.Direction.Right: return (1, 0);
				case MoveInfo.Direction.Up: return (0, 1);
				case MoveInfo.Direction.Left: return (-1, 0);
				case MoveInfo.Direction.Down: return (0, -1);
				default: return (1, 0);
			}
		} 

		private void AddBorder () {
			for (int i = -1; i <= width; i++) {
				fieldTree.Add(i, -1);
				fieldTree.Add(i, height);
			}

			for (int i = 0; i < height; i++) {
				fieldTree.Add(-1, i);
				fieldTree.Add(width, i);
			}
		}

		private IEnumerable<(float start, float end, float value)> ExtractDensity (IEnumerable<(float s, float e, float d, float k)> input) {
			foreach (var i in input) {
				yield return (i.s, i.e, i.d);
			}
		}

		private IEnumerable<(float start, float end, float value)> ExtractKinship (IEnumerable<(float s, float e, float d, float k)> input) {
			foreach (var i in input) {
				yield return (i.s, i.e, i.k);
			}
		}

		private struct ContinuousProjection {
			public SortedSet<(float start, float end, float dencity)> obstacles;
			public SortedSet<(float start, float end, float dencity)> food;

			public SortedSet<(float start, float end, float dencity, float kindship)> snakes;

			public void Initialize () {
				var comparer = new ProjectionPieceComparer();

				obstacles = new SortedSet<(float start, float end, float dencity)>(comparer);
				food = new SortedSet<(float start, float end, float dencity)>(comparer);
				snakes = new SortedSet<(float start, float end, float dencity, float kindship)>(comparer);
			}

			private class ProjectionPieceComparer : IComparer<(float start, float, float)>,
													IComparer<(float start, float, float, float)> {

				public int Compare ((float start, float, float) x, (float start, float, float) y) {
					return (x.start > y.start) ? 1 : -1;
				}

				public int Compare ((float start, float, float, float) x, (float start, float, float, float) y) {
					return (x.start > y.start) ? 1 : -1;
				}
			}
		}
	}
}