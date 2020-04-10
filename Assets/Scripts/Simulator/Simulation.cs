using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Visualizable;
using Visualization;

namespace Simulator {
	public partial class Simulation : IVisualizable {
		private List<ISimulationObserver> observers;

		public FieldProjector fieldProjector { get; private set; }
		public FieldGenerator fieldGenerator { get; private set; }
		public List<IPlayersPort> playersPorts { get; private set; }

		private HashSet<int> deadSnakes;
		private Dictionary<int, Vector2Int> idToFieldPos;
		private Dictionary<int, SnakeInfo> idToSnakeInfo;

		private Dictionary<int, int> idToCurLength;
		private Dictionary<int, IPlayersPort> idToPort;
		private Dictionary<int, float> idToValue;

		private int curFrame = 0;

		public readonly int width;
		public readonly int height;
		public readonly FieldItem[,] field;

		private System.Random rand;
		private int nextEntityId = 0;

		public int Width => width;
		public int Height => height;
		public FieldItem[,] Field => field;

#region Constants
		private readonly int SPAWN_BORDER = 1;

		private readonly float SPAWNED_FOOD_VALUE = 1;
		private readonly float FOOD_SPAWN_RATE = 0.05f;
		private readonly float HALF_FOOD_FRAME = 30;
#endregion

#region Buffers
		private List<MoveInfo> curMovesListBuffer = new List<MoveInfo>();
		private Dictionary<int, MoveInfo> curMovesDictBuffer = new Dictionary<int, MoveInfo>();

		private HashSet<(int x, int y)> updatedPositions = new HashSet<(int x, int y)>();
		private HashSet<int> removedEntities = new HashSet<int>();
#endregion

		public Simulation (int width, int height, int fieldGenerationSeed) {
			this.fieldGenerator = new FieldGenerator(fieldGenerationSeed, width, height, GetNextId);
			field = fieldGenerator.GenerateField();

			idToFieldPos = new Dictionary<int, Vector2Int>(this.fieldGenerator.spawnedObstacles);

			observers = new List<ISimulationObserver>();
			rand = new System.Random();

			this.width = width;
			this.height = height;

			fieldProjector = new FieldProjector(field, (id) => idToSnakeInfo[id]);
			fieldProjector.UpdateAtPositions(Vector2IntToTuple(idToFieldPos.Values));

			playersPorts = new List<IPlayersPort>();

			deadSnakes = new HashSet<int>();
			idToSnakeInfo = new Dictionary<int, SnakeInfo>();

			idToCurLength = new Dictionary<int, int>();
			idToPort = new Dictionary<int, IPlayersPort>();
			idToValue = new Dictionary<int, float>();
		}

		public void AddPlayerPort (IPlayersPort nwPort) {
			if (playersPorts.Contains(nwPort)) return;

			playersPorts.Add(nwPort);
			nwPort.GetNextId = this.GetNextId;
		}

		public SnakeInfo GetSnakeInfo (int id) {
			if (idToSnakeInfo.TryGetValue(id, out SnakeInfo requestedInfo)) {
				return requestedInfo;
			}

			return null;
		}

		public void AttachObserver (ISimulationObserver nwObserver) {
			if (observers.Contains(nwObserver)) return;
			observers.Add(nwObserver);
		}

		public void RemoveObserver (ISimulationObserver oldObserver) {
			if (!observers.Contains(oldObserver)) return;
			observers.Remove(oldObserver);
		}

		public void MakeStep () {
			LoadMovesToDictBuffer();
			MakeSimulationStep();

			fieldProjector.UpdateAtPositions(updatedPositions);
			SendStepResultsToPorts();

			UpdateObservers();
		}

		private void LoadMovesToDictBuffer () {
			curMovesListBuffer.Clear();
			foreach (var port in playersPorts) {
				var portMoves = port.MakeMove(fieldProjector);
				curMovesListBuffer.AddRange(portMoves);
			}

			curMovesDictBuffer.Clear();
			curMovesListBuffer.ForEach((e) => curMovesDictBuffer.Add(e.id, e));
		}

		private void SendStepResultsToPorts () {
			var results = new Dictionary<IPlayersPort, List<MoveResult>> ();
			foreach (var id_port in idToPort) {
				var headDir = MoveInfo.Direction.None;
				if (idToFieldPos.TryGetValue(id_port.Key, out Vector2Int headPos)) {
					headDir = field[headPos.x, headPos.y].dir;
				}

				if (!results.TryGetValue(id_port.Value, out List<MoveResult> curPortResults)) {
					curPortResults = new List<MoveResult>(32);
					results.Add(id_port.Value, curPortResults);
				}

				curPortResults.Add(new MoveResult {
					id = id_port.Key,
					value = idToValue[id_port.Key],
					headPos = headPos,
					headDir = headDir,
					flag = ((removedEntities.Contains(id_port.Key)) ? (byte)0 : (byte)MoveResult.State.IsAlive),
				});
			}

			foreach (var port_result in results) {
				port_result.Key.HandleMoveResult(port_result.Value);
			}
		}

		private void UpdateObservers () {
			if (observers.Count == 0) return;
			
			foreach (var observer in observers) {
				observer.SimulationUpdateHandler(this as IVisualizable, updatedPositions);
			}
		}

		private void MakeSimulationStep () {
			curFrame += 1;
			updatedPositions.Clear();
			removedEntities.Clear();

			foreach (var id_moveInfo in curMovesDictBuffer) {
				UpdateSnake(id_moveInfo.Key, id_moveInfo.Value);
			}

			ScatterFood();
		}

#region Food scattering

		private void ScatterFood () {
			int nwPiecesAmount = FoodPiecesAmount(rand, curFrame, new Vector2Int(width, height));

			for (int i = 0; i < nwPiecesAmount; i++) {
				var nwPos = new Vector2Int(rand.Next(0, width), rand.Next(0, height));
				if (field[nwPos.x, nwPos.y].type != FieldItem.ItemType.None) continue;

				int nwId = GetNextId();
				idToFieldPos.Add(nwId, nwPos);

				UpdateFieldItem(nwPos.x, nwPos.y, new FieldItem {
					id = nwId,
					frameOfLastUpdate = curFrame,
					prevNeighborPos = -1,
					value = SPAWNED_FOOD_VALUE,

					type = FieldItem.ItemType.Food,
				});
			}
		}

		public int FoodPiecesAmount (System.Random rand, int frame, Vector2Int fieldSize) {
			float x = (float)frame / HALF_FOOD_FRAME;

			float maxPiecesPerCell = FOOD_SPAWN_RATE / (1 + x);
			float piecesPerCell = (float)(maxPiecesPerCell * rand.NextDouble());

			return (int)(piecesPerCell * fieldSize.x * fieldSize.y);
		}

#endregion

#region Snake position update

		private void UpdateSnake (int id, MoveInfo moveInfo) {
			Vector2Int oldHeadPos;
			bool isNewSnake = false;

			if (deadSnakes.Contains(id)) return;
			if (!idToFieldPos.TryGetValue(id, out oldHeadPos)) {
				oldHeadPos = AddNewSnake(id);
				idToPort.Add(id, FindSnakePort(id));
				isNewSnake = true;
			}

			var oldHeadItem = field[oldHeadPos.x, oldHeadPos.y];
			if (oldHeadItem.type == FieldItem.ItemType.None)
				throw new System.Exception();
			if (oldHeadItem.frameOfLastUpdate == curFrame) return;

			if (!ManageValueCost(id, moveInfo.valueUsed)) return;            

			var selectedDir = (moveInfo.dir==MoveInfo.Direction.None)? oldHeadItem.dir: moveInfo.dir;
			bool skipMove = (selectedDir == OppositDirection(oldHeadItem.dir));

			var newHeadPos = CalcNewHeadPos(oldHeadPos, selectedDir);

			var p = newHeadPos;
			bool wallHitted = p.x<0 || p.y<0 || width<=p.x || height<=p.y;
			
			FieldItem hittedItem = default(FieldItem);
			if (!wallHitted) {
				hittedItem = field[newHeadPos.x, newHeadPos.y];
				wallHitted = wallHitted || (hittedItem.type == FieldItem.ItemType.Wall);
			}           

			if (isNewSnake || skipMove || wallHitted) {
				UpdateTail(oldHeadPos, false);
				return;
			}
			
			int prevHeadPos = oldHeadPos.y * width + oldHeadPos.x;
			var newHeadItem = SampleNewHeadItem(oldHeadItem, prevHeadPos, selectedDir);

			MoveHead(oldHeadPos, newHeadPos, hittedItem, newHeadItem);
			idToFieldPos[id] = newHeadPos;
		}

		private void MoveHead (Vector2Int oldPos, Vector2Int nwPos, FieldItem hitted, FieldItem nwItem) {
			bool bitedItsTail = (hitted.id == nwItem.id && hitted.prevNeighborPos == -1);

			if (hitted.type == FieldItem.ItemType.None || bitedItsTail) {
				unchecked { field[oldPos.x, oldPos.y].flags &= (byte)(~(uint)(FieldItem.Flag.Head)); }
				UpdateTail(oldPos, true);
				UpdateFieldItem(nwPos.x, nwPos.y, nwItem);

			} else if (hitted.type == FieldItem.ItemType.Food) {
				HandleMoveOnFood(oldPos, nwPos, hitted, nwItem);

			} else if (hitted.type == FieldItem.ItemType.Snake) {
				HandleMoveOnSnake(oldPos, nwPos, nwItem);
			}  
		}

		private void HandleMoveOnFood (Vector2Int oldPos, Vector2Int nwPos, FieldItem hitted, FieldItem nwItem) {
			removedEntities.Add(hitted.id);
			idToFieldPos.Remove(hitted.id);

			RemoveEntityTail(nwPos);
			idToValue[nwItem.id] += hitted.value;
			
			unchecked { field[oldPos.x, oldPos.y].flags &= (byte)(~(uint)(FieldItem.Flag.Head | FieldItem.Flag.Shortened)); }

			var moveForward = ((nwItem.flags & (byte)FieldItem.Flag.Shortened) == 0);
			UpdateTail(oldPos, moveForward);

			if (!moveForward) {
				idToCurLength[nwItem.id] += 1;

				if (idToSnakeInfo[nwItem.id].maxLength <= idToCurLength[nwItem.id]) {
					unchecked { nwItem.flags &= (byte)(~(uint)FieldItem.Flag.Shortened); }
				}
			}

			UpdateFieldItem(nwPos.x, nwPos.y, nwItem);
		}

		private void HandleMoveOnSnake (Vector2Int oldPos, Vector2Int nwPos, FieldItem nwItem) {
			UpdateTail(oldPos, false);
			uint initFlag = field[oldPos.x, oldPos.y].flags;
			var hitted = field[nwPos.x, nwPos.y];

			if (hitted.frameOfLastUpdate < curFrame) {
				UpdateSnake(hitted.id, curMovesDictBuffer[hitted.id]);
				hitted = field[nwPos.x, nwPos.y];

				if (removedEntities.Contains(nwItem.id)) return;
			}

			if (hitted.type == FieldItem.ItemType.Snake) {
				if ((hitted.flags & (byte)FieldItem.Flag.Head) == (byte)FieldItem.Flag.Head) {
					RemoveSnake(oldPos, nwItem.id);
					RemoveSnake(nwPos, hitted.id);
					return;
				}

				var hittedHeadPos = idToFieldPos[hitted.id];
				var hittedHead = field[hittedHeadPos.x, hittedHeadPos.y];

				if (hittedHead.prevNeighborPos == width*nwPos.y + nwPos.x) {
					RemoveSnake(hittedHeadPos, hitted.id);
				} else {
					BiteOffSnake(nwPos, hitted);
					unchecked { field[hittedHeadPos.x, hittedHeadPos.y].flags |= (byte)FieldItem.Flag.Shortened; }
				}

				hitted = field[nwPos.x, nwPos.y];
			}

			uint curFlag = field[oldPos.x, oldPos.y].flags;
			uint additionalFlags = initFlag ^ curFlag;
			nwItem.flags |= (byte)additionalFlags;

			unchecked { field[oldPos.x, oldPos.y].flags = (byte)(initFlag & (~(uint)(FieldItem.Flag.Head))); }
			MoveHead(oldPos, nwPos, hitted, nwItem);
		}

		private void RemoveSnake (Vector2Int headPos, int id) {
			RemoveEntityTail(headPos);
			deadSnakes.Add(id);
			removedEntities.Add(id);
			idToFieldPos.Remove(id);
		}

		private void BiteOffSnake (Vector2Int startPos, FieldItem hitted) {
			var initPos = idToFieldPos[hitted.id];
			var curPos = initPos;

			var initItem = field[curPos.x, curPos.y];
			var curItem = initItem;

			int bitedTile = startPos.y * width + startPos.x;

			while (curItem.prevNeighborPos != bitedTile) {
				if (curItem.type != FieldItem.ItemType.Snake) throw new System.Exception(); 
				int prevTile = curItem.prevNeighborPos;

				curPos = new Vector2Int(prevTile % width, prevTile / width);
				curItem = field[curPos.x, curPos.y];
			}
			field[curPos.x, curPos.y].prevNeighborPos = -1;

			var clearedPos = new List<Vector2Int>(16);
			RemoveEntityTail(startPos, (pos) => clearedPos.Add(pos));

			ScatterSnakeTail(hitted, clearedPos);			
		}

		private void ScatterSnakeTail (FieldItem hitted, List<Vector2Int> foodPos) {
			var foodVal = idToValue[hitted.id] / idToCurLength[hitted.id];
			idToCurLength[hitted.id] -= foodPos.Count;

			var totalValueDelta = foodPos.Count * foodVal;
			idToValue[hitted.id] -= totalValueDelta;

			foreach (var nwPos in foodPos) {
				var nwId = GetNextId();
				idToFieldPos.Add(nwId, nwPos);

				UpdateFieldItem(nwPos.x, nwPos.y, new FieldItem{
					id = nwId,
					frameOfLastUpdate = curFrame,
					prevNeighborPos = -1,

					value = foodVal * 0.8f,
					type = FieldItem.ItemType.Food
				});
			}
		}

		private FieldItem SampleNewHeadItem (FieldItem old, int ppos, MoveInfo.Direction dir) {
			return new FieldItem {
				id = old.id,
				frameOfLastUpdate = curFrame,
				prevNeighborPos = ppos,

				flags = old.flags,
				type = FieldItem.ItemType.Snake,
				dir = dir                
			};
		}

		private void RemoveEntityTail (Vector2Int tarPos, System.Action<Vector2Int> onRemove=null) {
			var tarItem = field[tarPos.x, tarPos.y];

			UpdateFieldItem(tarPos.x, tarPos.y, default);
			onRemove?.Invoke(tarPos);

			var nxtPos = tarItem.prevNeighborPos;
			
			while (nxtPos >= 0 && tarItem.type != FieldItem.ItemType.None) {
				tarPos = new Vector2Int(nxtPos % width, nxtPos / width);
				tarItem = field[tarPos.x, tarPos.y];

				UpdateFieldItem(tarPos.x, tarPos.y, default);
				onRemove?.Invoke(tarPos);

				if (nxtPos == tarItem.prevNeighborPos) throw new System.Exception();

				nxtPos = tarItem.prevNeighborPos;
			}
		}

		private bool ManageValueCost (int id, float valueUsed) {
			float currentValue = idToValue[id];
			if (currentValue >= 0) {				
				currentValue = (float)System.Math.Round((double)(currentValue - valueUsed), 6);
				idToValue[id] = currentValue;
			}

			if (currentValue < 0) {
				deadSnakes.Add(id);
				removedEntities.Add(id);

				if (idToFieldPos.TryGetValue(id, out Vector2Int headPos)) {
					RemoveEntityTail(headPos);
				}

				idToFieldPos.Remove(id);
				return false;
			}

			return true;
		}

		private MoveInfo.Direction OppositDirection (MoveInfo.Direction dir) {
			switch (dir) {
				case MoveInfo.Direction.Up: return MoveInfo.Direction.Down;
				case MoveInfo.Direction.Right: return MoveInfo.Direction.Left;
				case MoveInfo.Direction.Down: return MoveInfo.Direction.Up;
				case MoveInfo.Direction.Left: return MoveInfo.Direction.Right;
			}

			throw new System.ArgumentException("Can't find opposit direction.");
		}

		private Vector2Int CalcNewHeadPos (Vector2Int oldPos, MoveInfo.Direction dir) {
			switch (dir) {
				case MoveInfo.Direction.Up: return oldPos + Vector2Int.up;
				case MoveInfo.Direction.Right: return oldPos + Vector2Int.right;
				case MoveInfo.Direction.Down: return oldPos + Vector2Int.down;
				case MoveInfo.Direction.Left: return oldPos + Vector2Int.left;
			}

			return oldPos;
		}

		private void UpdateTail (Vector2Int oldHeadPos, bool moveForward) {
			bool hasTail = false;

			Vector2Int prevPos = new Vector2Int(-1, -1);
			Vector2Int curPos = oldHeadPos;

			int count = 0;
			while (true) {
				if (count++ > 100) throw new System.Exception();
				
				field[curPos.x, curPos.y].frameOfLastUpdate = curFrame;
				int nextPosIndex = field[curPos.x, curPos.y].prevNeighborPos;

				if (nextPosIndex == -1) {
					if (!moveForward) break;

					if (hasTail) {
						field[prevPos.x, prevPos.y].prevNeighborPos = -1;
					}

					UpdateFieldItem(curPos.x, curPos.y, default(FieldItem));
					break;
				}

				hasTail = true;
				prevPos = curPos;
				curPos = new Vector2Int(nextPosIndex%width, nextPosIndex/width);
			}
		}

#endregion

#region Snake creation

		private Vector2Int AddNewSnake (int id) {
			SnakeInfo info = FindSnakePort(id).GetSnakeInfo(id);
			
			idToCurLength.Add(id, info.maxLength);
			idToValue.Add(id, info.maxValue);
			Vector2Int pos = AddNewSnakeToField(id, info.maxLength);

			idToFieldPos.Add(id, pos);
			idToSnakeInfo.Add(id, info);

			return pos;
		}

		private Vector2Int AddNewSnakeToField (int snakeId, int targetLength) {
			var alteredCells = new HashSet<int>();

			while (true) {
				int prevPos = -1;

				bool vertPlacement = Random.value > 0.5f;
				bool invertedPlacement = Random.value > 0.5f;
				MoveInfo.Direction dir = ChooseDirection(vertPlacement, invertedPlacement);

				int curX, curY;
				if (invertedPlacement) {
					curX = Random.Range(((vertPlacement)? 0: targetLength)+SPAWN_BORDER, width-SPAWN_BORDER);
					curY = Random.Range(((vertPlacement)? targetLength: 0)+SPAWN_BORDER, height-SPAWN_BORDER);
				} else {
					curX = Random.Range(SPAWN_BORDER, width-SPAWN_BORDER-((vertPlacement)? 0: targetLength));
					curY = Random.Range(SPAWN_BORDER, height-SPAWN_BORDER-((vertPlacement)? targetLength: 0));
				}
				
				int curLength = 0;
				alteredCells.Clear();
				while (curLength < targetLength) {
					if (width <= curX || height <= curY || curX < 0 || curY < 0) break;
					if (field[curX, curY].type != FieldItem.ItemType.None) break;
					int curPos = curY*width + curX;

					alteredCells.Add(curPos);
					UpdateFieldItem(curX, curY, new FieldItem() {
						id = snakeId,
						frameOfLastUpdate = curFrame - 1,
						prevNeighborPos = prevPos,
						flags = (curLength==targetLength-1)? (byte)FieldItem.Flag.Head: (byte)0,
						type = FieldItem.ItemType.Snake,
						dir = dir
					});

					++curLength;
					if (curLength == targetLength) break;

					prevPos = curPos;
					curX = (vertPlacement)? curX : (curX + ((invertedPlacement)? -1: 1));
					curY = (vertPlacement)? (curY + ((invertedPlacement)? -1: 1)) : curY;                    
				}

				if (curLength == targetLength) {
					return new Vector2Int(curX, curY);
				} else {
					foreach (var cellPos in alteredCells) {
						UpdateFieldItem(cellPos%width, cellPos/width, default);
					}
				}
			}
		}

		private MoveInfo.Direction ChooseDirection (bool vert, bool inv) {
			if (vert && !inv) {
				return MoveInfo.Direction.Up;
			} else if (vert && inv) {
				return MoveInfo.Direction.Down;
			} else if (!vert && !inv) {
				return MoveInfo.Direction.Right;
			} else if (!vert && inv) {
				return MoveInfo.Direction.Left;
			}

			return MoveInfo.Direction.None;
		}

#endregion

		private IEnumerable<(int x, int y)> Vector2IntToTuple (IEnumerable<Vector2Int> input) {
			foreach (var vector in input) {
				yield return (vector.x, vector.y);
			}
		}

		private void UpdateFieldItem (int x, int y, FieldItem nwItem) {
			field[x, y] = nwItem;
			updatedPositions.Add((x, y));
		}

		private IPlayersPort FindSnakePort (int id) {
			foreach (var port in playersPorts) {
				if (port.GetSnakeInfo(id) != null) {
					return port;
				}
			}
			
			return null;
		}

		public int GetNextId () {
			return nextEntityId++;
		}
	}
}