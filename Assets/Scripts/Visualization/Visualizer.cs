using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Visualization {
    public class Visualizer : MonoBehaviour, ISimulationObserver {
        private ISnakeField field;

        public SnakeShaders shaders;

        private void Awake () {
            field = GetComponent<SnakeField>();
        }

        public void SimulationUpdateHandler (HashSet<int> entitiesIds) {
            
        }

        public void Start () {
            
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