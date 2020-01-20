using UnityEngine;


namespace SnakeVisual {
    public interface ISnakeField {
        Vector2Int FieldSize {get; set;}

        void ClearTilesMaterials ();

        Material GetTileMaterial (Vector2Int pos);
        void SetTileMaterial (Vector2Int pos, Material mat);
    }

    public class SnakeField : MonoBehaviour, ISnakeField {

#region Fields
        [SerializeField] private GameObject tilePrefab = null;

        private Vector2Int m_fieldSize = default(Vector2Int);
        [SerializeField] public Vector2Int FieldSize {
            get => m_fieldSize;
            set {
                m_fieldSize = value;
                UpdateFieldSize();
            }
        }

        private GameObject[,] instancedTiles = new GameObject[0,0];

        [Space]
        [SerializeField] private Shader transparentShader= null; 
#endregion

#region Public
        public void ClearTilesMaterials () {
            foreach (var tileObj in instancedTiles) {
                if (tileObj == null || tileObj.GetComponent<MeshRenderer>() == null) continue;
                tileObj.GetComponent<MeshRenderer>().material = new Material(transparentShader);
            }
        }

        public Material GetTileMaterial (Vector2Int pos) {
            if (!ContainsIndex(pos)) {
                return null;
            }

            var targetObj = instancedTiles[pos.x, pos.y];
            if (targetObj != null && targetObj.GetComponent<MeshRenderer>() != null) {
                return targetObj.GetComponent<MeshRenderer>().material;
            }

            return null;
        }

        public void SetTileMaterial (Vector2Int pos, Material mat) {
            if (!ContainsIndex(pos)) return;
            if (instancedTiles[pos.x, pos.y] == null) return;
            
            var renderer = instancedTiles[pos.x, pos.y].GetComponent<MeshRenderer>();
            if (renderer == null) return;

            renderer.material = mat;
        }
#endregion

#region Private
        private bool ContainsIndex (Vector2Int pos) {
            return  0 < pos.x && 0 < pos.y &&
                    pos.x < instancedTiles.GetLength(0) && 
                    pos.y < instancedTiles.GetLength(1);
        }

        private void UpdateFieldSize () {
            foreach (var tile in instancedTiles) {
                if (tile == null) continue;
                Destroy(tile);
            }

            instancedTiles = new GameObject[FieldSize.x, FieldSize.y];

            FillField();
        }

        private void FillField () {
            for (int i = 0; i < FieldSize.x; i++) {
                for (int j = 0; j < FieldSize.y; j++) {
                    var nwTile = GameObject.Instantiate(tilePrefab, this.transform, false);
                    instancedTiles[i, j] = nwTile;

                    TransformTile(nwTile, i, j);
                }
            }
        }

        private void TransformTile (GameObject tile, int i, int j) {
            var rectTrans = tile.GetComponent<RectTransform>();

            var sizeDelta = GetComponent<RectTransform>().sizeDelta;
            var side = Mathf.Min(sizeDelta.x / FieldSize.x, sizeDelta.y / FieldSize.y);

            rectTrans.localScale = side * Vector3.one;

            var offset = -side * (new Vector2(FieldSize.x / 2f, FieldSize.y / 2f));
            offset += (side / 2f) * Vector2.one;

            rectTrans.anchoredPosition = side * (new Vector2(i, j)) + offset;
        }
#endregion
    }
}
