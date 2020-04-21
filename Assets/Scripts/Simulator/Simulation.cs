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

		private Dictionary<int, int> foodIdToOwnerSnakeId;

		private int curFrame = 0;

		public readonly int width;
		public readonly int height;
		public readonly FieldItem[,] field;

		private System.Random rand;
		private int nextEntityId = 0;

		public int Width => width;
		public int Height => height;
		public FieldItem[,] Field => field;

		#region Options

		public int SPAWN_BORDER { get; set; } = 1;

		public float SPAWNED_FOOD_VALUE { get; set; } = 1;
		public float FOOD_SPAWN_RATE { get; set; } = 0.05f;
		public float HALF_FOOD_FRAME { get; set; } = 30;

		#endregion

		#region Buffers

		private List<MoveInfo> curMovesListBuffer = new List<MoveInfo>();
		private Dictionary<int, MoveInfo> curMovesDictBuffer = new Dictionary<int, MoveInfo>();

		private HashSet<(int x, int y)> updatedPositions = new HashSet<(int x, int y)>();
		private HashSet<int> removedEntities = new HashSet<int>();
		private Dictionary<int, float> eatenValue = new Dictionary<int, float>();

		private HashSet<int> canibalSnakes = new HashSet<int>();

		#endregion

		#region Public

		public Simulation (int width, int height, int fieldGenerationSeed) {
			this.fieldGenerator = new FieldGenerator(fieldGenerationSeed, width, height, GetNextId);
			field = fieldGenerator.GenerateField();

			idToFieldPos = new Dictionary<int, Vector2Int>(this.fieldGenerator.spawnedObstacles);

			observers = new List<ISimulationObserver>();
			rand = new System.Random();

			this.width = width;
			this.height = height;

			fieldProjector = new FieldProjector(field, (id) => (idToSnakeInfo.TryGetValue(id, out SnakeInfo snakeInfo)) ? snakeInfo : null);
			fieldProjector.UpdateAtPositions(Vector2IntToTuple(idToFieldPos.Values));

			playersPorts = new List<IPlayersPort>();

			deadSnakes = new HashSet<int>();
			idToSnakeInfo = new Dictionary<int, SnakeInfo>();

			idToCurLength = new Dictionary<int, int>();
			idToPort = new Dictionary<int, IPlayersPort>();
			idToValue = new Dictionary<int, float>();
			foodIdToOwnerSnakeId = new Dictionary<int, int>();
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

		#endregion

		#region Ports managment

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

				float snakesNewValue = (eatenValue.TryGetValue(id_port.Key, out float value)) ? value : 0;
				curPortResults.Add(new MoveResult {
					id = id_port.Key,
					value = idToValue[id_port.Key],
					headPos = headPos,
					headDir = headDir,
					eatenValue = snakesNewValue,
					flag = (byte)(((deadSnakes.Contains(id_port.Key)) ? 0 : (byte)MoveResult.State.IsAlive)
							| ((snakesNewValue > 0) ? (byte)MoveResult.State.GotAFood : 0)
							| ((canibalSnakes.Contains(id_port.Key)) ? (byte)MoveResult.State.EatSelf : 0 )),
				});
			}

			foreach (var port_result in results) {
				port_result.Key.HandleMoveResult(port_result.Value);
			}
		}

		#endregion

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
			eatenValue.Clear();
			canibalSnakes.Clear();

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
			if (deadSnakes.Contains(id)) return;

			if (!idToFieldPos.TryGetValue(id, out Vector2Int oldHeadPos)) {
				oldHeadPos = AddNewSnake(id);
				idToPort.Add(id, FindSnakePort(id));

				UpdateTail(oldHeadPos, false);
				return;
			}

			var oldHeadItem = field[oldHeadPos.x, oldHeadPos.y];
			if (oldHeadItem.type == FieldItem.ItemType.None) throw new System.Exception();
			if (oldHeadItem.frameOfLastUpdate == curFrame) return;

			if (!ManageValueCost(id, moveInfo.valueUsed)) return;            

			var selectedDir = (moveInfo.dir==MoveInfo.Direction.None)? oldHeadItem.dir: moveInfo.dir;
			bool skipMove = (selectedDir == OppositDirection(oldHeadItem.dir));

			var newHeadPos = CalcNewHeadPos(oldHeadPos, selectedDir);

			var p = newHeadPos;
			bool wallHitted = p.x<0 || p.y<0 || width<=p.x || height<=p.y;
			
			var hittedItem = default(FieldItem);
			if (!wallHitted) {
				hittedItem = field[newHeadPos.x, newHeadPos.y];
				wallHitted = wallHitted || (hittedItem.type == FieldItem.ItemType.Wall);
			}           

			if (skipMove || wallHitted) {
				updatedPositions.Add((oldHeadPos.x, oldHeadPos.y));
				UpdateTail(oldHeadPos, false);
				return;
			}
			
			int prevHeadPos = oldHeadPos.y * width + oldHeadPos.x;
			var newHeadItem = SampleNewHeadItem(oldHeadItem, prevHeadPos, selectedDir);

			MoveHead(oldHeadPos, newHeadPos, hittedItem, newHeadItem, out bool isAlive);
			if (isAlive) idToFieldPos[id] = newHeadPos;
		}

		private void MoveHead (Vector2Int oldPos, Vector2Int nwPos, FieldItem hitted, FieldItem nwItem, out bool isAlive) {
			bool bitedItsTail = (hitted.id == nwItem.id && hitted.prevNeighborPos == -1);

			if (hitted.type == FieldItem.ItemType.None || bitedItsTail) {
				unchecked { field[oldPos.x, oldPos.y].flags &= (byte)(~(uint)(FieldItem.Flag.Head)); }
				isAlive = true;
				UpdateTail(oldPos, true);
				UpdateFieldItem(nwPos.x, nwPos.y, nwItem);

			} else if (hitted.type == FieldItem.ItemType.Food) {
				isAlive = true;
				HandleMoveOnFood(oldPos, nwPos, hitted, nwItem);

			} else if (hitted.type == FieldItem.ItemType.Snake) {
				HandleMoveOnSnake(oldPos, nwPos, nwItem, out isAlive);

			} else {
				throw new System.Exception("Unexpected field item");
			}
		}

		private void HandleMoveOnFood (Vector2Int oldPos, Vector2Int nwPos, FieldItem hitted, FieldItem nwItem) {
			if (foodIdToOwnerSnakeId.TryGetValue(hitted.id, out int ownerId) && ownerId == nwItem.id) {
				canibalSnakes.Add(nwItem.id);
				foodIdToOwnerSnakeId.Remove(hitted.id);
			}

			removedEntities.Add(hitted.id);
			idToFieldPos.Remove(hitted.id);

			RemoveEntityTail(nwPos);
			idToValue[nwItem.id] += hitted.value;
			eatenValue.Add(nwItem.id, hitted.value);
			
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

		private void HandleMoveOnSnake (Vector2Int oldPos, Vector2Int nwPos, FieldItem nwItem, out bool isAlive) {
			UpdateTail(oldPos, false);
			uint initFlag = field[oldPos.x, oldPos.y].flags;
			var hitted = field[nwPos.x, nwPos.y];

			if (hitted.frameOfLastUpdate < curFrame && curMovesDictBuffer.TryGetValue(hitted.id, out var hittedMoveInfo)) {
				UpdateSnake(hitted.id, hittedMoveInfo);
				hitted = field[nwPos.x, nwPos.y];

				if (removedEntities.Contains(nwItem.id)) {
					isAlive = false;
					return;
				}
			}

			if (hitted.type == FieldItem.ItemType.Snake) {
				if ((hitted.flags & (byte)FieldItem.Flag.Head) == (byte)FieldItem.Flag.Head) {
					RemoveSnake(oldPos, nwItem.id);
					RemoveSnake(nwPos, hitted.id);
					isAlive = false;
					return;
				}

				var hittedHeadPos = idToFieldPos[hitted.id];
				var hittedHead = field[hittedHeadPos.x, hittedHeadPos.y];

				if (hittedHead.prevNeighborPos == width * nwPos.y + nwPos.x) {
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
			MoveHead(oldPos, nwPos, hitted, nwItem, out isAlive);
		}

		private void RemoveSnake (Vector2Int headPos, int id) {
			ScatterSnakeTail(headPos, id);

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

			ScatterSnakeTail(startPos, hitted.id);			
		}

		private void ScatterSnakeTail (Vector2Int startPos, int targetId) {
			var foodPos = new List<Vector2Int>(16);
			RemoveEntityTail(startPos, (pos) => foodPos.Add(pos));

			var foodVal = 0.8f * idToValue[targetId] / idToCurLength[targetId];
			idToCurLength[targetId] -= foodPos.Count;

			var totalValueDelta = foodPos.Count * foodVal;
			idToValue[targetId] -= totalValueDelta;

			var pieceValue = (foodVal < SPAWNED_FOOD_VALUE) ? SPAWNED_FOOD_VALUE : foodVal;
			foreach (var nwPos in foodPos) {
				var nwId = GetNextId();
				idToFieldPos.Add(nwId, nwPos);
				foodIdToOwnerSnakeId.Add(nwId, targetId);

				UpdateFieldItem(nwPos.x, nwPos.y, new FieldItem{
					id = nwId,
					frameOfLastUpdate = curFrame,
					prevNeighborPos = -1,

					value = pieceValue,
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

		private Vector2Int AddNewSnakeToField (int id, int targetLength) {
			var boxDimentions = EvaluateSpawnBoxDimentions(targetLength);

			var toMinCorner = new Vector2Int(-(boxDimentions.x - 1) / 2, -(boxDimentions.y - 1) / 2);
			var diagOffset = boxDimentions - Vector2Int.one;

			int count = 0;
			var projResult = new List<(int x, int y)>(128);
			while (true) {
				++count;
				var testPosition = new Vector2Int(rand.Next(diagOffset.x, width - diagOffset.x), rand.Next(diagOffset.y, width - diagOffset.y));

				bool isValidPosition = true;
				for (int i = 0; isValidPosition && i < boxDimentions.x * boxDimentions.y; i++) {
					var curPos = testPosition + toMinCorner + new Vector2Int(i % boxDimentions.x, i / boxDimentions.x);
					var curItemType = field[curPos.x, curPos.y].type;
					isValidPosition = isValidPosition && (curItemType == FieldItem.ItemType.Food || curItemType == FieldItem.ItemType.None);
				}

				if (!isValidPosition) continue;
				projResult.Clear();

				int R = Mathf.Max(boxDimentions.x, boxDimentions.y);
				fieldProjector.FindItemsInCircle(testPosition.x, testPosition.y, 2 * R, projResult);

				foreach ((int x, int y) in projResult) {
					if (x < 0 || y < 0 || x >= width || y >= width) continue;
					var closeItemType = field[x, y].type;
					isValidPosition = isValidPosition && (closeItemType != FieldItem.ItemType.Snake);
					if (!isValidPosition) break;
				}

				if (isValidPosition || count > 100) {
					var headPos = SpawnSnakeInBox(testPosition + toMinCorner, testPosition + toMinCorner + diagOffset, id, targetLength);
					fieldProjector.UpdateAtPositions(IterateThroughBox(testPosition + toMinCorner, boxDimentions));
					return headPos;
				}
			}
		}

		private Vector2Int EvaluateSpawnBoxDimentions (int targetLength) {
			int spawnAreaA = Mathf.CeilToInt(Mathf.Sqrt(targetLength));
			int spawnAreaB = Mathf.CeilToInt((float)targetLength / spawnAreaA);

			if (rand.NextDouble() < 0.5) {
				return new Vector2Int(spawnAreaA, spawnAreaB);
			} else {
				return new Vector2Int(spawnAreaB, spawnAreaA);
			}
		}

		private Vector2Int SpawnSnakeInBox (Vector2Int minCorner, Vector2Int maxCorner, int id, int length) {
			int rollInfo = rand.Next(0, 8);
			int curFace = ((rollInfo + 1) / 2) % 4;
			bool isClockWise = (rollInfo % 2 == 1);

			Vector2Int? curPos = null;
			var nextPos = SelectCornerAndDirection(minCorner, maxCorner, curFace, isClockWise, out Vector2Int spiralDir);
			var headPos = nextPos;
			for (int i = 0; i < length; i++) {
				bool isHead = !curPos.HasValue;
				curPos = nextPos;

				if (i < length - 1) {
					if (!BoxContains(minCorner, maxCorner, curPos.Value + spiralDir)) {
						ShringBox(ref minCorner, ref maxCorner, curFace);
						curFace = (curFace + ((isClockWise) ? 1 : 3)) % 4;
						nextPos = SelectCornerAndDirection(minCorner, maxCorner, curFace, isClockWise, out spiralDir);
					} else {
						nextPos = curPos.Value + spiralDir;
					}
				}

				var snakeDir = SpiralToSnakeDir(spiralDir);
				int prevPos = (i == length - 1) ? -1 : nextPos.y * width + nextPos.x;
				
				var snakeItem = MakeSnakeItem(id, prevPos, isHead, snakeDir);
				UpdateFieldItem(curPos.Value.x, curPos.Value.y, snakeItem);
			}

			return headPos;
		}

		private Vector2Int SelectCornerAndDirection (Vector2Int minC, Vector2Int maxC, int face, bool isClockWise, out Vector2Int dir) {
			switch (face) {
				case 0: {
					dir = (isClockWise) ? Vector2Int.up : Vector2Int.down;
					return (isClockWise) ? minC : new Vector2Int(minC.x, maxC.y);
				}

				case 1: {
					dir = (isClockWise) ? Vector2Int.right : Vector2Int.left;
					return (isClockWise) ? new Vector2Int(minC.x, maxC.y) : maxC;
				}

				case 2: {
					dir = (isClockWise) ? Vector2Int.down : Vector2Int.up;
					return (isClockWise) ? maxC : new Vector2Int(maxC.x, minC.y);
				}

				case 3: {
					dir = (isClockWise) ? Vector2Int.left : Vector2Int.right;
					return (isClockWise) ? new Vector2Int(maxC.x, minC.y) : minC;
				}
			}

			throw new System.Exception();
		}

		private FieldItem MakeSnakeItem (int snakeId, int prevPos, bool isHead, MoveInfo.Direction dir) {
			return new FieldItem() {
				id = snakeId,
				frameOfLastUpdate = curFrame - 1,
				prevNeighborPos = prevPos,
				flags = (isHead) ? (byte)FieldItem.Flag.Head : (byte)0,
				type = FieldItem.ItemType.Snake,
				dir = dir
			};
		}

		private bool BoxContains (Vector2Int minCorner, Vector2Int maxCorner, Vector2Int pos) {
			return minCorner.x <= pos.x && pos.x <= maxCorner.x && minCorner.y <= pos.y && pos.y <= maxCorner.y;
		}

		private void ShringBox(ref Vector2Int minCorner, ref Vector2Int maxCorner, int face) {
			switch (face) {
				case 0: minCorner.x += 1; break;
				case 1: maxCorner.y -= 1; break;
				case 2: maxCorner.x -= 1; break;
				case 3: minCorner.y += 1; break;

				default: throw new System.Exception();
			}
		}

		private MoveInfo.Direction SpiralToSnakeDir (Vector2Int spiralDir) {
			if (spiralDir == Vector2Int.up) return MoveInfo.Direction.Down;
			if (spiralDir == Vector2Int.left) return MoveInfo.Direction.Right;
			if (spiralDir == Vector2Int.down) return MoveInfo.Direction.Up;
			if (spiralDir == Vector2Int.right) return MoveInfo.Direction.Left;

			throw new System.Exception();
		}

		private IEnumerable<(int x, int y)> IterateThroughBox (Vector2Int minCorner, Vector2Int dimentions) {
			for (int i = 0; i < dimentions.x * dimentions.y; i++) {
				yield return (minCorner.x + i % dimentions.x, minCorner.y + i / dimentions.x);
			}
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