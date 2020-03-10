using System.Collections;
using System.Collections.Generic;
using Simulator;
using UnityEngine;

namespace Visualization {
    public class Visualizer : MonoBehaviour, ISimulationObserver {
        private ISnakeField field;
        private Simulation lastSimulation;

        public SnakeShaders shaders;

        private void Awake () {
            field = GetComponent<SnakeField>();
        }

        public void SimulationUpdateHandler (Simulation simulation, HashSet<(int id, Vector2Int pos)> entities) {
            if (lastSimulation != simulation) {
                lastSimulation = simulation;
                SynchronizeWithSimulation(simulation);
            }

            SynchronizeWithSimulation(simulation);
            //field.ClearTilesMaterials();
            // foreach (var entity in entitiesIds) {
            //     UpdateField(simulation, entity);
            // }
        }

        private void DrawSnake (Simulation sim, FieldItem fieldItem, Vector2Int pos) {
            byte headMask = (byte)FieldItem.Flag.Head;
            int curInd = pos.y * sim.width + pos.x;
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

        private void AddSnakesTileToField (Simulation sim, FieldItem fieldItem, Stack<Material> snakesMaterials) {
            var curDir = fieldItem.dir;
            int curInd = fieldItem.prevNeighborPos;
            while (curInd != -1) {
                var pos = new Vector2Int(curInd % sim.width, curInd / sim.width);

                int baseDirIndex = (int)curDir - 1;
                fieldItem = sim.field[pos.x, pos.y];
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
                    } break;

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

        private void SynchronizeWithSimulation (Simulation simulation) {
            field.FieldSize = new Vector2Int(simulation.width, simulation.height);
            field.ClearTilesMaterials();

            var snakeHeads = new List<(FieldItem item, Vector2Int pos)>(32);
            for (int x = 0; x < simulation.width; ++x) {
                for (int y = 0; y < simulation.height; ++y) {
                    var fieldItem = simulation.field[x, y];
                    
                    switch (fieldItem.type) {
                        case FieldItem.ItemType.None: continue;

                        case FieldItem.ItemType.Snake: {
                            byte headMask = (byte)FieldItem.Flag.Head;
                            if ((fieldItem.flags & headMask) == headMask) {
                                snakeHeads.Add((fieldItem, new Vector2Int(x, y)));
                            }
                            break;
                        }

                        case FieldItem.ItemType.Food: {
                            var nwFoodMaterial = new Material(shaders.foodShader);
                            field.SetTileMaterial(new Vector2Int(x, y), nwFoodMaterial);
                            break;
                        }
                    }
                }
            }

            foreach (var snake in snakeHeads) {
                DrawSnake(simulation, snake.item, snake.pos);
            }
        }

        public void Update () {
            
        }

        private void DrawSnakeSample () {
            field.FieldSize = new Vector2Int(20, 30);
            field.ClearTilesMaterials();

            field.ClearTilesMaterials();

            var m1 = new Material(shaders.turnShader);
            field.SetTileMaterial(new Vector2Int(15, 15), m1);

            var m2 = new Material(shaders.bodyShader);
            field.SetTileMaterial(new Vector2Int(14, 15), m2);
            field.SetTileRotation(new Vector2Int(14, 15), 90);

            var m3 = new Material(shaders.bodyShader);
            field.SetTileMaterial(new Vector2Int(15, 14), m3);

            var m4 = new Material(shaders.headShader);
            field.SetTileMaterial(new Vector2Int(13, 15), m4);
            field.SetTileRotation(new Vector2Int(13, 15), 90);

            m1.SetFloat("_TailN", 1);
            m2.SetFloat("_TailN", 2);
            m3.SetFloat("_TailN", 0);
            m4.SetFloat("_TailN", 3);

            m1.SetFloat("_HeadN", 2);
            m2.SetFloat("_HeadN", 1);
            m3.SetFloat("_HeadN", 3);
            m4.SetFloat("_HeadN", 0);
        }
	}

    [System.Serializable]
    public struct SnakeShaders {
        public Shader bodyShader;
        public Shader turnShader;
        public Shader headShader;

        public Shader foodShader;
    }
}