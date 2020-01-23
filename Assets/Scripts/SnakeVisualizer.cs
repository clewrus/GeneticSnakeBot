using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SnakeVisual {
    public class SnakeVisualizer : MonoBehaviour {
        private ISnakeField field;

        public SnakeShaders shaders;

        private void Awake () {
            field = GetComponent<SnakeField>();
        }

        public void Start () {
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

            m1.SetFloat("_TailN", 1);
            m2.SetFloat("_TailN", 2);
            m3.SetFloat("_TailN", 0);
        }

        public void Update () {
            
        }
    }

    [System.Serializable]
    public struct SnakeShaders {
        public Shader bodyShader;
        public Shader turnShader;
        public Shader headShader;
    }
}