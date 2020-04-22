using Simulator;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SnakeScarecrow : MonoBehaviour {
	[SerializeField] private float tileSize = 100;
	[SerializeField] private int turnTileIndex = 5;
	[SerializeField] private float bodyWidth = 0.6f;
	[Space]
	[SerializeField] private ScuamaSettingsManager scuamaManager = null;
	[SerializeField] private Slider snakeLengthSlider = null;
	[Space]
	[SerializeField] private GameObject tileBasePrefab = null;
	[SerializeField] private SnakeShaders shaders = default;

	private LinkedList<RectTransform> tiles = new LinkedList<RectTransform>();

	private int m_Length;
	public int Length {
		get => m_Length;
		set {
			m_Length = value;
			PlayerPrefs.SetInt("SnakeLength", m_Length);
			OnLengthChanged();
		}
	}

	private void Start () {
		if (snakeLengthSlider != null) {
			snakeLengthSlider.value = PlayerPrefs.GetInt("SnakeLength", 5);
			Length = Mathf.RoundToInt(snakeLengthSlider.value);
		}

		scuamaManager.ScuamaPaternChanged -= ScuamaPaternChangedHandler;
		scuamaManager.ScuamaPaternChanged += ScuamaPaternChangedHandler;
	}

	public void OnSnakeLengthSliderChanged () {
		if (snakeLengthSlider != null) {
			Length = Mathf.RoundToInt(snakeLengthSlider.value);
		}
	}

	private void OnLengthChanged () {
		while (tiles.Count != m_Length) {
			if (tiles.Count > m_Length) {
				Destroy(tiles.Last.Value.gameObject);
				tiles.RemoveLast();
				
				if (tiles.Count == turnTileIndex + 1) {
					SetMaterial(tiles.Last.Value, shaders.bodyShader, rotation: 0);
				}

			} else {
				var nwTile = GetNwTile();
				tiles.AddLast(nwTile);

				if (tiles.Count == 1) {
					SetMaterial(nwTile, shaders.headShader, rotation: 0);
				} 

				if (1 < tiles.Count && tiles.Count <= turnTileIndex + 1) {
					SetMaterial(nwTile, shaders.bodyShader, rotation: 0);
				}

				if (tiles.Count == turnTileIndex + 2) {
					var mat = SetMaterial(tiles.Last.Previous.Value, shaders.turnShader, rotation: 90);
					mat.SetInt("_TurnRight", 1);
				}

				if (tiles.Count > turnTileIndex + 1) {
					SetMaterial(nwTile, shaders.bodyShader, rotation: 90);
				}
			}
		}

		UpdateScarecrowMaterials();
	}

	public void UpdateScarecrowMaterials () {
		int fromHeadPos = 0;
		var scuamaPatern = scuamaManager.CurrentScuamaPatern;
		foreach (var rectTrans in tiles) {
			var tarMat = rectTrans.GetComponent<RawImage>().material;

			tarMat.SetFloat("_SnakeID", 228);
			tarMat.SetFloat("_TailN", tiles.Count - 1 - fromHeadPos);
			tarMat.SetFloat("_HeadN", fromHeadPos);
			tarMat.SetFloat("_EdgeOffset", 0.5f * (1 - bodyWidth));

			tarMat.SetColorArray("_SnakeColors", scuamaPatern.GetColorArray());
			tarMat.SetVectorArray("_GyroidConfig", scuamaPatern.GetGyroidConfig());

			++fromHeadPos;
		}
	}

	private Material SetMaterial (RectTransform rectTrans, Shader shader, float rotation) {
		if (rectTrans == null) return null;
		var img = rectTrans.GetComponent<RawImage>();

		if (img == null) return null;
		img.material = new Material(shader);

		rectTrans.localRotation = Quaternion.Euler(0, 0, rotation);
		return img.material;
	}

	private RectTransform GetNwTile () {
		var tileObj = Instantiate(tileBasePrefab, transform, false);
		var rectTrans = tileObj.GetComponent<RectTransform>();
		rectTrans.sizeDelta = tileSize * Vector2.one;

		var posDeltaDir = (tiles.Count > turnTileIndex) ? Vector2.right : Vector2.down;
		if (tiles.Count > 0) {
			var prevTilePosition = tiles.Last.Value.anchoredPosition;
			rectTrans.anchoredPosition = prevTilePosition + tileSize * posDeltaDir;
		} else {
			rectTrans.anchoredPosition = Vector2.zero;
		}
		

		return rectTrans;
	}

	private void ScuamaPaternChangedHandler (object sender, System.EventArgs args) {
		UpdateScarecrowMaterials();
	}

	[System.Serializable]
	public struct SnakeShaders {
		public Shader bodyShader;
		public Shader turnShader;
		public Shader headShader;
	}
}