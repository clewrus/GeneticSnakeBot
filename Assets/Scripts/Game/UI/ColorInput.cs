
using UnityEngine;
using UnityEngine.UI;

public class ColorInput : MonoBehaviour {
	[SerializeField] private Slider rSlider = null;
	[SerializeField] private Slider gSlider = null;
	[SerializeField] private Slider bSlider = null;

	private Color m_CurrentColor;
	public Color CurrentColor {
		get {
			PlayerPrefs.Save();
			return m_CurrentColor;
		}
		set { m_CurrentColor = value; }
	}

	public System.EventHandler ColorChanged { get; set; }

	private bool initializing;

	public void Awake () {
		initializing = true;
		rSlider.value = PlayerPrefs.GetFloat($"{name}sliderR", Random.value);
		gSlider.value = PlayerPrefs.GetFloat($"{name}sliderG", Random.value);
		bSlider.value = PlayerPrefs.GetFloat($"{name}sliderB", Random.value);
		initializing = false;

		CurrentColor = new Color(rSlider.value, gSlider.value, bSlider.value, 1);
	}

	public void OnAnySliderChanged () {
		if (initializing) return;
		CurrentColor = new Color(rSlider.value, gSlider.value, bSlider.value, 1);

		 PlayerPrefs.SetFloat($"{name}sliderR", rSlider.value);
		 PlayerPrefs.SetFloat($"{name}sliderG", gSlider.value);
		 PlayerPrefs.SetFloat($"{name}sliderB", bSlider.value);

		OnColorChanged();
	}

	public void OnColorChanged () {
		ColorChanged?.Invoke(this, new System.EventArgs());
	}
}