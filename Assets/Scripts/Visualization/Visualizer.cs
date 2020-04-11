using System;
using System.Collections;
using System.Collections.Generic;
using Visualizable;
using Simulator;
using UnityEngine;
using Assets.SnakeTiles;

namespace Visualization {
	public class Visualizer : MonoBehaviour, ISimulationObserver {
		private ISnakeField field;

		private IVisualizable lastSimulation;
		private Dictionary<int, SnakeInfo> idToInfo;
		private Dictionary<int, LinkedList<Vector2Int>> entityPlacement;
		private Dictionary<Vector2Int, int> positionToPlacementId;

		private Material commonFoodMaterial;
		public SnakeShaders shaders;

		private HashSet<IVisualizerObserver> observers;
		private HashSet<int> recentlyRemoved;

		private void Awake () {
			field = GetComponent<SnakeField>();
			commonFoodMaterial = new Material(shaders.foodShader);
		}

		public void SimulationUpdateHandler (IVisualizable simulation, IEnumerable<(int x, int y)> updatedPositions) {
			if (lastSimulation != simulation) {
				lastSimulation = simulation;
				SynchronizeWithSimulation(simulation);
			} else {
				recentlyRemoved.Clear();

				foreach (var pos in updatedPositions) {
					RedrawEntity(simulation, new Vector2Int(pos.x, pos.y));
				}
			}

			UpdateObservers();
		}

		#region Observer

		public void AddObserver (IVisualizerObserver nwObserver) {
			if (observers == null) {
				observers = new HashSet<IVisualizerObserver>();
			}
			observers.Add(nwObserver);
			nwObserver.FieldSize = field.FieldSize;
		}

		public void RemoveObserver (IVisualizerObserver oldObserver) {
			if (observers == null) return;
			observers.Remove(oldObserver);
		}

		public void UpdateObservers () {
			if (observers == null) return;

			foreach (var observer in observers) {
				int desiredId = observer.ExpectedPlacementId;
				if (entityPlacement.TryGetValue(desiredId, out LinkedList<Vector2Int> placement)) {
					observer.PlacementChangedHandler(placement, exists: true, wasRemovedRecently: false);
				} else {
					var isRecentlyRemoved = recentlyRemoved.Contains(desiredId);
					observer.PlacementChangedHandler(new List<Vector2Int>(), exists: false, isRecentlyRemoved);
				}
			}
		}

		private void UpdateObserversFieldSize () {
			foreach (var observer in observers) {
				observer.FieldSize = field.FieldSize;
			}
		}

		#endregion

		#region Entity Redrawing

		private void RedrawEntity (IVisualizable simulation, Vector2Int pos) {
			FieldItem tarItem = simulation.Field[pos.x, pos.y];
			
			switch (tarItem.type) {
				case FieldItem.ItemType.None: {
					RemoveFromPlacement(pos);
					field.ClearTileMaterial(pos);
				} break;

				case FieldItem.ItemType.Food: {
					RedrawFoodEntity(simulation, tarItem, pos);
				} break;

				case FieldItem.ItemType.Wall: {
					RedrawWallEntity(simulation, tarItem, pos);
				} break;

				case FieldItem.ItemType.Snake: {
					if ((byte)(tarItem.flags & (byte)FieldItem.Flag.Head) == (byte)FieldItem.Flag.Head) {
						RedrawSnakeEntity(simulation, tarItem, pos);
					}
				} break;
			}
		}

		private void RedrawFoodEntity (IVisualizable simulation, FieldItem item, Vector2Int pos) {
			if (entityPlacement.ContainsKey(item.id)) {
				RedrawAction(simulation, item.id, pos, 
					(Vector2Int p, bool isOld) => {
						if (isOld) {
							if (simulation.Field[p.x, p.y].type == FieldItem.ItemType.None) {
								field.ClearTileMaterial(p);
							}
						} else {
							field.SetTileMaterial(p, commonFoodMaterial);
						}
					}
				);
			} else {
				DrawFood(pos);

				var foodPlacement = new LinkedList<Vector2Int>();
				foodPlacement.AddLast(pos);
				AddInPlacement(item.id, foodPlacement);

			}
		}

		private void RedrawWallEntity (IVisualizable simulation, FieldItem item, Vector2Int pos) {
			if (entityPlacement.ContainsKey(item.id)) {
				Debug.LogWarning("TODO: Realize wall redrawing");
			} else {
				Debug.LogWarning("TODO: Realize wall redrawing");
			}
		}

		private void RedrawSnakeEntity (IVisualizable simulation, FieldItem item, Vector2Int pos) {
			if (entityPlacement.ContainsKey(item.id)) {
				RedrawAction(simulation, item.id, pos,
					(Vector2Int p, bool isOld) => {
						if (isOld && (simulation.Field[p.x, p.y].type == FieldItem.ItemType.None)) {
							field.ClearTileMaterial(p);
						}
					}
				);

				DrawSnake(simulation, item, pos);
			} else {
				AddSnakeIntoInfoDictionary(item.id, simulation.GetSnakeInfo(item.id));

				AddInPlacement(item.id, FindSnakesPlacement(simulation, pos));
				DrawSnake(simulation, item, pos);
			}
		}

		private void RedrawAction (IVisualizable s, int id, Vector2Int nwPos, Action<Vector2Int, bool> redrawTile) {
			if (!entityPlacement.TryGetValue(id, out LinkedList<Vector2Int> tiles)) {
				Debug.LogError("Can't find placement with such id");
			}

			var firstOldTilePos = tiles.First.Value;
			var curPos = nwPos;
			var curItem = s.Field[curPos.x, curPos.y];

			LinkedListNode<Vector2Int> curNode = null;
			while (firstOldTilePos != curPos) {
				redrawTile(curPos, false);
				curNode = (curNode == null) ? tiles.AddFirst(curPos) : tiles.AddAfter(curNode, curPos);

				UpdatePositionToPlacement(curPos, id);

				if (curItem.prevNeighborPos < 0) break;
				curPos = new Vector2Int(curItem.prevNeighborPos % s.Width, curItem.prevNeighborPos / s.Width);
				curItem = s.Field[curPos.x, curPos.y];
			}

			if (curNode == null) {
				curNode = tiles.First;
			}

			while (true) {
				redrawTile(curPos, false);
				curNode = (curNode == tiles.Last) ? tiles.AddLast(curPos) : curNode.Next;
				curNode.Value = curPos;

				UpdatePositionToPlacement(curPos, id);

				if (curItem.prevNeighborPos < 0) break;
				curPos = new Vector2Int(curItem.prevNeighborPos % s.Width, curItem.prevNeighborPos / s.Width);
				curItem = s.Field[curPos.x, curPos.y];
			}

			while (curNode != tiles.Last) {
				redrawTile(curNode.Next.Value, true);
				if (positionToPlacementId.TryGetValue(curNode.Next.Value, out int tarId) && tarId == id) {
					positionToPlacementId.Remove(curNode.Next.Value);
				}
				tiles.Remove(curNode.Next);
			}
		}

		#endregion

		#region FieldRedrawing

		private void SynchronizeWithSimulation (IVisualizable simulation) {
			field.FieldSize = new Vector2Int(simulation.Width, simulation.Height);
			UpdateObserversFieldSize();
			entityPlacement = new Dictionary<int, LinkedList<Vector2Int>>();
			idToInfo = new Dictionary<int, SnakeInfo>();
			positionToPlacementId = new Dictionary<Vector2Int, int>();
			recentlyRemoved = new HashSet<int>();

			field.ClearTilesMaterials();
			DrawSurroundingWalls(simulation);

			var snakeHeads = new List<(FieldItem item, Vector2Int pos)>(32);
			for (int x = 0; x < simulation.Width; ++x) {
				for (int y = 0; y < simulation.Height; ++y) {
					var fieldItem = simulation.Field[x, y];
					var fieldItemPos = new Vector2Int(x, y);
					
					switch (fieldItem.type) {
						case FieldItem.ItemType.None: continue;

						case FieldItem.ItemType.Snake: {
							byte headMask = (byte)FieldItem.Flag.Head;
							if ((fieldItem.flags & headMask) == headMask) {
								snakeHeads.Add((fieldItem, fieldItemPos));

								AddSnakeIntoInfoDictionary(fieldItem.id, simulation.GetSnakeInfo(fieldItem.id));
								AddInPlacement(fieldItem.id, FindSnakesPlacement(simulation, fieldItemPos));
							}
						} break;

						case FieldItem.ItemType.Food: {
							DrawFood(fieldItemPos);

							var foodPlacement = new LinkedList<Vector2Int>();
							foodPlacement.AddLast(fieldItemPos);
							AddInPlacement(fieldItem.id, foodPlacement);
						} break;

						case FieldItem.ItemType.Wall: {
							DrawWall(simulation, fieldItemPos);

							var wallPlacement = new LinkedList<Vector2Int>();
							wallPlacement.AddLast(fieldItemPos);
							AddInPlacement(fieldItem.id, wallPlacement);
						} break;
					}
				}
			}

			foreach (var snake in snakeHeads) {
				DrawSnake(simulation, snake.item, snake.pos);
			}
		}

		private void DrawSurroundingWalls (IVisualizable simulation) {
			for (int i = -1; i < simulation.Width; i++) {
				DrawWall(simulation, new Vector2Int(i, -1));
			}

			for (int i = -1; i < simulation.Height; i++) {
				DrawWall(simulation, new Vector2Int(simulation.Width, i));
			}

			for (int i = simulation.Width; -1 < i; i--) {
				DrawWall(simulation, new Vector2Int(i, simulation.Height));
			}

			for (int i = simulation.Height; -1 < i; i--) {
				DrawWall(simulation, new Vector2Int(-1, i));
			}
		}

		private LinkedList<Vector2Int> FindSnakesPlacement (IVisualizable simulation, Vector2Int headPos) {
			Debug.Assert((simulation.Field[headPos.x, headPos.y].flags & (byte)FieldItem.Flag.Head) != 0, "Head tile expected.");

			var placement = new LinkedList<Vector2Int>();

			placement.AddLast(headPos);
			int nextTile = simulation.Field[headPos.x, headPos.y].prevNeighborPos;

			int width = simulation.Width;
			while (nextTile >= 0) {
				var nextPos = new Vector2Int(nextTile % width, nextTile / width);
				var nextTileItem = simulation.Field[nextPos.x, nextPos.y];
				if (nextTileItem.type != FieldItem.ItemType.Snake) throw new System.Exception();

				placement.AddLast(nextPos);
				nextTile = nextTileItem.prevNeighborPos;
			}

			return placement;
		}

		private void DrawFood (Vector2Int foodPos) {
			field.SetTileMaterial(foodPos, commonFoodMaterial);
		}

		private void DrawWall (IVisualizable simulation, Vector2Int pos) {
			var simulationField = simulation.Field;
			Predicate<Vector2Int> IsWall = (p => {
				if (p.x < -1 || simulation.Width < p.x || p.y < -1 || simulation.Height < p.y) return false;
				if (p.x == -1 || p.x == simulation.Width || p.y == -1 || p.y == simulation.Height) return true;
				
				return simulationField[p.x, p.y].type == FieldItem.ItemType.Wall;
			});

			var nwWallMaterial = new Material(shaders.wallShader);
			var curOrientation = new WallOrientation(
				IsWall(pos + new Vector2Int(-1, 1)),
				IsWall(pos + new Vector2Int( 0, 1)),
				IsWall(pos + new Vector2Int( 1, 1)),
				IsWall(pos + new Vector2Int( 1, 0)),
				IsWall(pos + new Vector2Int( 1,-1)),
				IsWall(pos + new Vector2Int( 0,-1)),
				IsWall(pos + new Vector2Int(-1,-1)),
				IsWall(pos + new Vector2Int(-1, 0))
			);

			nwWallMaterial.SetInt("_Orientation", (int)curOrientation.Value);
			field.SetTileMaterial(pos, nwWallMaterial);
		}

		private void DrawSnake (IVisualizable sim, FieldItem fieldItem, Vector2Int pos) {
			byte headMask = (byte)FieldItem.Flag.Head;
			var snakesMaterials = new Stack<Material>();

			Debug.Assert((fieldItem.flags & headMask) == headMask, "Head tile expected.");

			var curDir = fieldItem.dir;
			snakesMaterials.Push(new Material(shaders.headShader));
			field.SetTileMaterial(pos, snakesMaterials.Peek());
			field.SetTileRotation(pos, DirToRot(curDir));

			AddSnakesTileToField(sim, fieldItem, snakesMaterials);

			var snakeInfo = idToInfo[fieldItem.id];
			int snakeLength = snakesMaterials.Count;
			for (int i = 0; i < snakeLength; i++) {
				var tarMat = snakesMaterials.Pop();
				tarMat.SetFloat("_SnakeID", fieldItem.id);
				tarMat.SetFloat("_TailN", i);
				tarMat.SetFloat("_HeadN", snakeLength - 1 - i);
				tarMat.SetFloat("_EdgeOffset", 0.5f * (1 - snakeInfo.bodyWidth));

				tarMat.SetColorArray("_SnakeColors", snakeInfo.scuamaPatern.GetColorArray());
				tarMat.SetVectorArray("_GyroidConfig", snakeInfo.scuamaPatern.GetGyroidConfig());
			}
		}

		private void AddSnakesTileToField (IVisualizable sim, FieldItem fieldItem, Stack<Material> snakesMaterials) {
			var curDir = fieldItem.dir;
			int curInd = fieldItem.prevNeighborPos;
			while (curInd != -1) {
				var pos = new Vector2Int(curInd % sim.Width, curInd / sim.Width);

				int baseDirIndex = (int)curDir - 1;
				fieldItem = sim.Field[pos.x, pos.y];
				curDir = (fieldItem.prevNeighborPos == -1) ? curDir : fieldItem.dir;
				int curDirIndex = (int)curDir - 1;

				int relatedDir = (curDirIndex + 4 - baseDirIndex) % 4;

				switch (relatedDir) {
					case 0: snakesMaterials.Push(new Material(shaders.bodyShader)); break;
					case 1: snakesMaterials.Push(new Material(shaders.turnShader)); break;
					case 3: {
						var m = new Material(shaders.turnShader);
						m.SetInt("_TurnRight", 1);
						snakesMaterials.Push(m);
					}
					break;

					default: Debug.LogError("Unexpected related direction."); break;
				}

				field.SetTileMaterial(pos, snakesMaterials.Peek());
				field.SetTileRotation(pos, DirToRot(curDir));

				curInd = fieldItem.prevNeighborPos;
			}
		}

		private float DirToRot (MoveInfo.Direction dir) {
			switch (dir) {
				case MoveInfo.Direction.Up: return 0;
				case MoveInfo.Direction.Right: return -90;
				case MoveInfo.Direction.Down: return 180;
				case MoveInfo.Direction.Left: return 90;
			}

			Debug.LogError("Unexpected direction");
			return 0;
		}

		#endregion

		private void AddSnakeIntoInfoDictionary (int id, SnakeInfo snakeInfo) {
			idToInfo.Add(id, snakeInfo);
		}

		#region Placement utilities

		private void AddInPlacement (int id, LinkedList<Vector2Int> placement) {
			if (!entityPlacement.ContainsKey(id)) {
				entityPlacement.Add(id, placement);
			} else {
				Debug.LogError("Placement with such id already exist.");
			}

			foreach (var pos in placement) {
				UpdatePositionToPlacement(pos, id);
			}
		}

		private void UpdatePositionToPlacement (Vector2Int pos, int id) {
			if (positionToPlacementId.ContainsKey(pos)) {
				positionToPlacementId[pos] = id;
			} else {
				positionToPlacementId.Add(pos, id);
			}
		}

		private void RemoveFromPlacement (Vector2Int pos) {
			if (positionToPlacementId.TryGetValue(pos, out int id)) {
				var foundList = entityPlacement[id];

				recentlyRemoved.Add(id);
				foundList.Remove(pos);

				if (foundList.Count == 0) {
					positionToPlacementId.Remove(pos);
					entityPlacement.Remove(id);
				}				
			}
		}

		#endregion

	}

	[System.Serializable]
	public struct SnakeShaders {
		public Shader bodyShader;
		public Shader turnShader;
		public Shader headShader;

		public Shader foodShader;
		public Shader wallShader;
	}
}