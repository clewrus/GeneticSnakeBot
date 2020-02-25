using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Visualization;

namespace Simulator {
    public partial class Simulation {
        public List<ISimulationObserver> observers;

        public FieldProjector fieldProjector { get; private set; }
        public List<IPlayersPort> playersPorts { get; private set; }

        private Dictionary<int, Vector2Int> idToFieldPos;
        private Dictionary<int, SnakeInfo> idToSnakeInfo;

        private Dictionary<int, int> idToCurLength;
        private Dictionary<int, IPlayersPort> idToPort;
        private Dictionary<int, float> idToValue;

        private int curFrame = 0;

        private readonly int width;
        private readonly int height;
        private readonly FieldItem[,] field;

        private System.Random rand;
        private int nextEntityId = 0;

#region Constants
        private readonly int SPAWN_BORDER = 1;

        private readonly float SPAWNED_FOOD_VALUE = 1;
        private readonly float FOOD_SPAWN_RATE = 0.1f;
        private readonly float HALF_FOOD_FRAME = 30;
#endregion

#region Buffers
        private List<MoveInfo> curMovesListBuffer = new List<MoveInfo>();
        private Dictionary<int, MoveInfo> curMovesDictBuffer = new Dictionary<int, MoveInfo>();

        private HashSet<int> updatedEntities = new HashSet<int>();
        private HashSet<int> removedEntities = new HashSet<int>();
#endregion

        public Simulation (int width, int height) {
            observers = new List<ISimulationObserver>();
            field = new FieldItem[width, height];
            rand = new System.Random();

            this.width = width;
            this.height = height;

            fieldProjector = new FieldProjector(field);
            playersPorts = new List<IPlayersPort>();

            idToFieldPos = new Dictionary<int, Vector2Int>();
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
                if (!results.ContainsKey(id_port.Value)) {
                    results.Add(id_port.Value, new List<MoveResult>(32));
                }

                var headPos = idToFieldPos[id_port.Key];
                var headDir = field[headPos.x, headPos.y].dir;

                results[id_port.Value].Add(new MoveResult {
                    id = id_port.Key,
                    value = idToValue[id_port.Key],
                    headPos = headPos,
                    headDir = headDir,
                    flag =  ((removedEntities.Contains(id_port.Key)) ? (byte)0 : (byte)MoveResult.State.IsAlive),
                });
            }

            foreach (var port_result in results) {
                port_result.Key.HandleMoveResult(port_result.Value);
            }
        }

        private void UpdateObservers () {
            foreach (var observer in observers) {
                observer.SimulationUpdateHandler(updatedEntities);
            }
        }

        private void MakeSimulationStep () {
            curFrame += 1;
            updatedEntities.Clear();
            removedEntities.Clear();

            foreach (var move in curMovesDictBuffer) {
                UpdateSnake(move.Key, move.Value);
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
                updatedEntities.Add(nwId);

                field[nwPos.x, nwPos.y] = new FieldItem {
                    id = nwId,
                    frameOfLastUpdate = curFrame,
                    prevNeighborPos = -1,
                    value = SPAWNED_FOOD_VALUE,

                    type = FieldItem.ItemType.Food,
                };
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
            if (!idToFieldPos.TryGetValue(id, out oldHeadPos)) {
                oldHeadPos = AddNewSnake(id);
                idToPort.Add(id, FindSnakePort(id));
            }

            if (!ManageValueCost(id, moveInfo.valueUsed)) return;            

            var oldHeadItem = field[oldHeadPos.x, oldHeadPos.y];
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

            if (skipMove || wallHitted) {
                UpdateTail(oldHeadPos, false);
                updatedEntities.Add(id);
                return;
            }
            
            int prevHeadPos = oldHeadPos.y * width + oldHeadPos.x;
            var newHeadItem = SampleNewHeadItem(oldHeadItem, prevHeadPos, selectedDir);

            MoveHead(oldHeadPos, newHeadPos, hittedItem, newHeadItem);
            idToFieldPos[id] = newHeadPos;
            updatedEntities.Add(id);
        }

        private void MoveHead (Vector2Int oldPos, Vector2Int nwPos, FieldItem hitted, FieldItem nwItem) {
            if (hitted.type == FieldItem.ItemType.None) {
                unchecked { field[oldPos.x, oldPos.y].flags &= (byte)(~(uint)(FieldItem.Flag.Head)); }
                UpdateTail(oldPos, true);

            } else if (hitted.type == FieldItem.ItemType.Food) {
                HandleMoveOnFood(oldPos, nwPos, hitted, nwItem);

            } else if (hitted.type == FieldItem.ItemType.Snake) {
                if (!HandleMoveOnSnake(oldPos, nwPos, nwItem)) return;

            }

            field[nwPos.x, nwPos.y] = nwItem;            
        }

        private void HandleMoveOnFood (Vector2Int oldPos, Vector2Int nwPos, FieldItem hitted, FieldItem nwItem) {
            removedEntities.Add(hitted.id);
            updatedEntities.Add(hitted.id);
            RemoveEntityTail(nwPos);
            idToValue[nwItem.id] += hitted.value;
            
            unchecked { field[oldPos.x, oldPos.y].flags &= (byte)(~(uint)(FieldItem.Flag.Head)); }
            UpdateTail(oldPos, (nwItem.flags & (byte)FieldItem.Flag.Shortened) != 0);

            idToCurLength[nwItem.id] += 1;
            if (idToCurLength[nwItem.id] >= idToSnakeInfo[nwItem.id].maxLength) {
                unchecked {nwItem.flags &= (byte) ~(uint)FieldItem.Flag.Shortened;}
            }
        }

        private bool HandleMoveOnSnake (Vector2Int oldPos, Vector2Int nwPos, FieldItem nwItem) {
            UpdateTail(oldPos, false);
            var hitted = field[nwPos.x, nwPos.y];

            if (hitted.frameOfLastUpdate < curFrame) {
                UpdateSnake(hitted.id, curMovesDictBuffer[hitted.id]);
                hitted = field[nwPos.x, nwPos.y];

                if (removedEntities.Contains(nwItem.id)) return false;
            }

            if (hitted.type == FieldItem.ItemType.Snake) {
                if ((hitted.flags & (byte)FieldItem.Flag.Head) == (byte)FieldItem.Flag.Head) {
                    removedEntities.Add(nwItem.id);
                    removedEntities.Add(hitted.id);
                    BiteOffSnake(oldPos, field[oldPos.x, oldPos.y]);
                    BiteOffSnake(nwPos, hitted);

                    return false;
                } 

                BiteOffSnake(nwPos, hitted);
                hitted = field[nwPos.x, nwPos.y];
            }

            unchecked { field[oldPos.x, oldPos.y].flags &= (byte)(~(uint)(FieldItem.Flag.Head)); }
            MoveHead(oldPos, nwPos, hitted, nwItem);

            return true;
        }

        private void BiteOffSnake (Vector2Int startPos, FieldItem hitted) {
            var curPos = idToFieldPos[hitted.id];
            var curItem = field[curPos.x, curPos.y];

            int bitedTile = startPos.y * width + startPos.x;
            while (curItem.prevNeighborPos != bitedTile) {
                int prevTile = curItem.prevNeighborPos;

                curPos = new Vector2Int(prevTile % width, prevTile / width);
                curItem = field[curPos.x, curPos.y];
            }
            field[curPos.x, curPos.y].prevNeighborPos = -1;

            var clearedPos = new List<Vector2Int>(16);
            RemoveEntityTail(startPos, (pos) => clearedPos.Add(pos));

            var foodVal = idToValue[hitted.id] / idToCurLength[hitted.id];
            var totalValueDelta = clearedPos.Count * foodVal;

            idToValue[hitted.id] -= totalValueDelta;
            foreach (var nwPos in clearedPos) {
                field[nwPos.x, nwPos.y] = new FieldItem{
                    id = GetNextId(),
                    frameOfLastUpdate = curFrame,
                    prevNeighborPos = -1,

                    value = foodVal * 0.8f,
                    type = FieldItem.ItemType.Food
                };
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

            field[tarPos.x, tarPos.y] = default(FieldItem);
            onRemove?.Invoke(tarPos);

            var nxtPos = tarItem.prevNeighborPos;
            
            while (nxtPos >= 0 && tarItem.type != FieldItem.ItemType.None) {
                tarPos = new Vector2Int(nxtPos % width, nxtPos / width);
                tarItem = field[tarPos.x, tarPos.y];

                field[tarPos.x, tarPos.y] = default(FieldItem);
                onRemove?.Invoke(tarPos);

                nxtPos = tarItem.prevNeighborPos;
            }
        }

        private bool ManageValueCost (int id, float valueUsed) {
            float currentValue = idToValue[id];
            if (currentValue >= 0) {
                if (currentValue < valueUsed) {
                    RemoveEntityTail(idToFieldPos[id]);
                }
                
                currentValue = (float)System.Math.Round((double)(currentValue - valueUsed), 6);
                idToValue[id] = currentValue;
            }

            if (currentValue < 0) {
                removedEntities.Add(id);
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

            while (true) {
                field[curPos.x, curPos.y].frameOfLastUpdate = curFrame;
                int nextPosIndex = field[curPos.x, curPos.y].prevNeighborPos;

                if (nextPosIndex == -1) {
                    if (!moveForward) break;

                    if (hasTail) {
                        field[prevPos.x, prevPos.y].prevNeighborPos = -1;
                    }

                    field[curPos.x, curPos.y] = default(FieldItem);
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

                int curX = 0;
                int curY = 0;

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
                    field[curX, curY] = new FieldItem() {
                        id = snakeId,
                        frameOfLastUpdate = curFrame - 1,
                        prevNeighborPos = prevPos,
                        flags = (curLength==targetLength-1)? (byte)FieldItem.Flag.Head: (byte)0,
                        type = FieldItem.ItemType.Snake,
                        dir = dir
                    };

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
                        field[cellPos%width, cellPos/width] = default(FieldItem);
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