
using UnityEngine;
using UnityEngine.UI;

public class GyroidInput : MonoBehaviour {
	[SerializeField] private Slider zSlider = null;
	[SerializeField] private Slider ratioSlider = null;

	private (float z, float ratio) m_CurrentValue;
	public (float z, float ratio) CurrentValue {
		get {
			PlayerPrefs.Save();
			return m_CurrentValue;
		}
		set { m_CurrentValue = value; }
	}

	public System.EventHandler GyroidValueChanged { get; set; }

	private bool initializing;

	public void Awake () {
		initializing = true;
		zSlider.value = PlayerPrefs.GetFloat($"{name}sliderZ", Random.Range(zSlider.minValue, zSlider.maxValue));
		ratioSlider.value = PlayerPrefs.GetFloat($"{name}sliderRatio", Random.Range(ratioSlider.minValue, ratioSlider.maxValue));
		initializing = false;

		CurrentValue = (zSlider.value, ratioSlider.value);
	}

	public void OnAnySliderChanged () {
		if (initializing) return;
		CurrentValue = (zSlider.value, ratioSlider.value);

		PlayerPrefs.SetFloat($"{name}sliderZ", zSlider.value);
		PlayerPrefs.SetFloat($"{name}sliderRatio", ratioSlider.value);

		OnGyroidValueChanged();
	}

	private void OnGyroidValueChanged () {
		GyroidValueChanged?.Invoke(this, new System.EventArgs());
	}
}