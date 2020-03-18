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
		private Dictionary<int, LinkedList<Vector2Int>> entitysTiles;

		private Material commonFoodMaterial;
		public SnakeShaders shaders;

		private void Awake () {
			field = GetComponent<SnakeField>();
			commonFoodMaterial = new Material(shaders.foodShader);
		}

		public void SimulationUpdateHandler (IVisualizable simulation, HashSet<(int id, Vector2Int? pos)> entities) {
			if (lastSimulation != simulation) {
				lastSimulation = simulation;
				SynchronizeWithSimulation(simulation);
				return;
			}

			foreach (var entityInfo in entities) {
				bool entityRemoved = !entityInfo.pos.HasValue;
				var curPos = (entityRemoved) ? new Vector2Int(-1, -1) : entityInfo.pos.Value;
				RedrawEntity(simulation, entityInfo.id, entityRemoved, curPos) ;
			}
		}

		#region Entity Redrawing

		private void RedrawEntity (IVisualizable simulation, int id, bool removed, Vector2Int pos) {
			FieldItem tarItem = default;
			if (!removed) {
				tarItem = simulation.Field[pos.x, pos.y];
				Debug.Assert(tarItem.id == id, "Found item with wrong id.");
			}

			if (removed || (!removed && tarItem.type == FieldItem.ItemType.None)) {
				if (entitysTiles.TryGetValue(id, out LinkedList<Vector2Int> placement)) {
					ClearPlacementNoneTiles(simulation, placement);
					entitysTiles.Remove(id);
				}
			}
			
			switch (tarItem.type) {
				case FieldItem.ItemType.Food: {
					RedrawFoodEntity(simulation, tarItem, pos);
				} break;

				case FieldItem.ItemType.Wall: {
					RedrawWallEntity(simulation, tarItem, pos);
				} break;

				case FieldItem.ItemType.Snake: {
					RedrawSnakeEntity(simulation, tarItem, pos);
				} break;
			}
		}

		private void ClearPlacementNoneTiles (IVisualizable simulation, LinkedList<Vector2Int> placement) {
			foreach (var pos in placement) {
				if (simulation.Field[pos.x, pos.y].type == FieldItem.ItemType.None) {
					field.ClearTileMaterial(pos);
				}
			}
		}

		private void RedrawFoodEntity (IVisualizable simulation, FieldItem item, Vector2Int pos) {
			if (entitysTiles.TryGetValue(item.id, out LinkedList<Vector2Int> placement)) {
				RedrawAction(simulation, placement, pos, 
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
				entitysTiles.Add(item.id, foodPlacement);
			}
		}

		private void RedrawWallEntity (IVisualizable simulation, FieldItem item, Vector2Int pos) {
			if (entitysTiles.TryGetValue(item.id, out LinkedList<Vector2Int> placement)) {
				Debug.LogWarning("TODO: Realize wall redrawing");
			} else {
				Debug.LogWarning("TODO: Realize wall redrawing");
			}
		}

		private void RedrawSnakeEntity (IVisualizable simulation, FieldItem item, Vector2Int pos) {
			if (entitysTiles.TryGetValue(item.id, out LinkedList<Vector2Int> placement)) {
				RedrawAction(simulation, placement, pos,
					(Vector2Int p, bool isOld) => {
						if (isOld && (simulation.Field[p.x, p.y].type == FieldItem.ItemType.None)) {
							field.ClearTileMaterial(p);
						}
					}
				);

				DrawSnake(simulation, item, pos);
			} else {
				entitysTiles.Add(item.id, FindSnakesPlacement(simulation, pos));
				DrawSnake(simulation, item, pos);
			}
		}

		private void RedrawAction (IVisualizable s, LinkedList<Vector2Int> tiles, Vector2Int nwPos, Action<Vector2Int, bool> redrawTile) {
			var firstOldTilePos = tiles.First.Value;
			var curPos = nwPos;
			var curItem = s.Field[curPos.x, curPos.y];

			LinkedListNode<Vector2Int> curNode = null;
			while (firstOldTilePos != curPos) {
				redrawTile(curPos, false);
				curNode = (curNode == null) ? tiles.AddFirst(curPos) : tiles.AddAfter(curNode, curPos);

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

				if (curItem.prevNeighborPos < 0) break;
				curPos = new Vector2Int(curItem.prevNeighborPos % s.Width, curItem.prevNeighborPos / s.Width);
				curItem = s.Field[curPos.x, curPos.y];
			}

			while (curNode != tiles.Last) {
				redrawTile(curNode.Next.Value, true);
				tiles.Remove(curNode.Next);
			}
		}

		#endregion

		#region FieldRedrawing

		private void SynchronizeWithSimulation (IVisualizable simulation) {
			field.FieldSize = new Vector2Int(simulation.Width, simulation.Height);
			entitysTiles = new Dictionary<int, LinkedList<Vector2Int>>();

			field.ClearTilesMaterials();

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
								entitysTiles.Add(fieldItem.id, FindSnakesPlacement(simulation, fieldItemPos));
							}
						} break;

						case FieldItem.ItemType.Food: {
							DrawFood(fieldItemPos);

							var foodPlacement = new LinkedList<Vector2Int>();
							foodPlacement.AddLast(fieldItemPos);
							entitysTiles.Add(fieldItem.id, foodPlacement);
						} break;

						case FieldItem.ItemType.Wall: {
							DrawWall(simulation, fieldItemPos);

							var wallPlacement = new LinkedList<Vector2Int>();
							wallPlacement.AddLast(fieldItemPos);
							entitysTiles.Add(fieldItem.id, wallPlacement);
						} break;
					}
				}
			}

			foreach (var snake in snakeHeads) {
				DrawSnake(simulation, snake.item, snake.pos);
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
				placement.AddLast(nextPos);
				nextTile = simulation.Field[nextPos.x, nextPos.y].prevNeighborPos;
			}

			return placement;
		}

		private void DrawFood (Vector2Int foodPos) {
			field.SetTileMaterial(foodPos, commonFoodMaterial);
		}

		private void DrawWall (IVisualizable simulation, Vector2Int pos) {
			var simulationField = simulation.Field;
			Predicate<Vector2Int> IsWall = (p => {
				if (p.x < 0 || simulation.Width <= p.x || p.y < 0 || simulation.Height <= p.y) return true;
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

			int snakeLength = snakesMaterials.Count;
			for (int i = 0; i < snakeLength; i++) {
				var tarMat = snakesMaterials.Pop();
				tarMat.SetFloat("_SnakeID", fieldItem.id);
				tarMat.SetFloat("_TailN", i);
				tarMat.SetFloat("_HeadN", snakeLength - 1 - i);
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