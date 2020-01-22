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
            field.SetTileMaterial(new Vector2Int(15, 15), new Material(shaders.bodyShader));
        }

        public void Update () {
            
        }
    }

    [System.Serializable]
    public struct SnakeShaders {
        public Shader bodyShader;
    }
}