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
        private Dictionary<int, float> idToValue;

        private int curFrame = 0;

        private readonly int width;
        private readonly int height;
        private readonly FieldItem[,] field;

        private System.Random rand;
        private int nextEntityId = 0;

#region Constants
        private readonly int SPAWN_BORDER = 3;

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

        }

        private void UpdateObservers () {

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

                field[nwPos.x, nwPos.y] = new FieldItem {
                    id = GetNextId(),
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
            }

            idToValue[id] -= moveInfo.valueUsed;
            if (idToValue[id] < 0) {
                removedEntities.Add(id);
                RemoveEntityTail(idToFieldPos[id]);
                return;
            }

            var oldHeadItem = field[oldHeadPos.x, oldHeadPos.y];
            var selectedDir = (moveInfo.dir==MoveInfo.Direction.None)? oldHeadItem.dir: moveInfo.dir;
            bool skipMove = (selectedDir == OppositDirection(oldHeadItem.dir));

            var newHeadPos = CalcNewHeadPos(oldHeadPos, selectedDir);
            FieldItem hittedItem = field[newHeadPos.x, newHeadPos.y];

            var p = newHeadPos;
            bool wallHitted = (hittedItem.type == FieldItem.ItemType.Wall);
            wallHitted = wallHitted || p.x<0 || p.y<0 || width<=p.x || height<=p.x;

            if (skipMove || wallHitted) {
                UpdateTail(oldHeadPos, false);
                updatedEntities.Add(id);
                return;
            }
            
            int prevHeadPos = oldHeadPos.y * width + oldHeadPos.x;
            var newHeadItem = SampleNewHeadItem(oldHeadItem, prevHeadPos, selectedDir);

            MoveHead(oldHeadPos, newHeadPos, hittedItem, newHeadItem);
            updatedEntities.Add(id);
        }

        private void MoveHead (Vector2Int oldPos, Vector2Int nwPos, FieldItem hitted, FieldItem nwItem) {
            if (hitted.type == FieldItem.ItemType.None) {
                unchecked { field[oldPos.x, oldPos.y].flags &= (byte)(~(uint)(FieldItem.Flag.Head)); }
                UpdateTail(oldPos, true);
            } else if (hitted.type == FieldItem.ItemType.Food) {
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

            } else if (hitted.type == FieldItem.ItemType.Snake) {
                UpdateTail(oldPos, false);
                if (hitted.frameOfLastUpdate < curFrame) {
                    UpdateSnake(hitted.id, curMovesDictBuffer[hitted.id]);
                    hitted = field[nwPos.x, nwPos.y];

                    if (removedEntities.Contains(nwItem.id)) return;
                }

                if (hitted.type == FieldItem.ItemType.Snake) {
                    if ((hitted.flags & (byte)FieldItem.Flag.Head) == (byte)FieldItem.Flag.Head) {
                        removedEntities.Add(nwItem.id);
                        removedEntities.Add(hitted.id);
                        BiteOffSnake(oldPos, field[oldPos.x, oldPos.y]);
                        BiteOffSnake(nwPos, hitted);

                        return;
                    } 

                    BiteOffSnake(nwPos, hitted);
                    hitted = field[nwPos.x, nwPos.y];
                }

                unchecked { field[oldPos.x, oldPos.y].flags &= (byte)(~(uint)(FieldItem.Flag.Head)); }
                MoveHead(oldPos, nwPos, hitted, nwItem);
            }

            field[nwPos.x, nwPos.y] = nwItem;            
        }

        private void BiteOffSnake (Vector2Int startPos, FieldItem hitted) {
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
                curPos = new Vector2Int(nextPosIndex%width, nextPosIndex/width);
            }
        }

#endregion

#region Snake creation

        private Vector2Int AddNewSnake (int id) {
            SnakeInfo info = null;
            foreach (var port in playersPorts) {
                info = port.GetSnakeInfo(id);
                if (info != null) {
                    idToSnakeInfo.Add(id, info);
                    break;
                }
            }

            idToCurLength.Add(id, info.maxLength);
            idToValue.Add(id, info.maxValue);
            Vector2Int pos = AddNewSnakeToField(id, info.maxLength);

            idToFieldPos.Add(id, pos);
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
                for (curLength = 0; curLength < targetLength; ++curLength) {
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

        public int GetNextId () {
            return nextEntityId++;
        }
    }
}