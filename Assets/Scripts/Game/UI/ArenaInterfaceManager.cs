using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Game.UI {
	public class ArenaInterfaceManager : MonoBehaviour {
		[SerializeField] private GameObject timerSlider = null;
		[SerializeField] private GameObject multiplierLable = null;
		[SerializeField] private GameObject lastSecondsLable = null;
		[Space]
		[SerializeField] private Color keyColor1 = default;
		[SerializeField] private Color keyColor2 = default;


		public float LastSecondsTimeTrashold { get; set; } = 5;
		public float MaxTime { get; set; }

		private float m_CurTime;
		public float CurTime {
			get => m_CurTime;
			set {
				m_CurTime = value;
				OnCurTimeUpdated();
			}
		}

		private float m_CurMultiplier;
		public float CurMultiplier {
			get => m_CurMultiplier;
			set {
				m_CurMultiplier = value;
				OnMultiplierUpdated();
			}
		}

		private void OnMultiplierUpdated () {
			if (TrySetLableText(multiplierLable, $"{Mathf.FloorToInt(CurMultiplier)}x")) {
				var tmpro = multiplierLable.GetComponent<TextMeshProUGUI>();
				(var textColor, var underlayColor) = EvaluateMultiplierColors(CurMultiplier);
				tmpro.color = textColor;
				tmpro.fontSharedMaterial.SetColor("_UnderlayColor", underlayColor);
			}
		}

		private void OnCurTimeUpdated () {
			TrySetLableText(lastSecondsLable, Mathf.CeilToInt(CurTime).ToString());

			float normTime = CurTime / MaxTime;
			TrySetSliderValue(timerSlider, normTime);

			timerSlider?.SetActive(true);
			lastSecondsLable?.SetActive(CurTime != 0 && CurTime < LastSecondsTimeTrashold);
		}

		private bool TrySetLableText (GameObject obj, string text) {
			if (obj == null) return false;
			var textMeshPro = obj.GetComponent<TextMeshProUGUI>();
			if (textMeshPro == null) return false;

			textMeshPro.text = text;
			return true;
		}

		private bool TrySetSliderValue (GameObject obj, float value) {
			if (obj == null) return false;
			var slider = obj.GetComponent<UnityEngine.UI.Slider>();
			if (slider == null) return false;

			slider.value = value;
			return true;
		}

		private (Color text, Color underlay) EvaluateMultiplierColors (float value) {
			Color.RGBToHSV(keyColor1, out var H1, out var S1, out var V1);
			Color.RGBToHSV(keyColor2, out var H2, out var S2, out var V2);
			if (H1 < H2) {
				H1 += 1;
			}

			float normValue = (value - 1) / 3f;
			float textH = Mathf.Lerp(H2, H1, normValue);
			float underlayH = textH - 0.2f;

			textH = (10 + textH) % 1;
			underlayH = (10 + underlayH) % 1;

			float S = Mathf.Lerp(Mathf.Min(S1, S2), Mathf.Max(S1, S2), normValue);
			float V = Mathf.Lerp(Mathf.Min(V1, V2), Mathf.Max(V1, V2), normValue);

			var textColor = Color.HSVToRGB(textH, S, V);
			var underlayColor = Color.HSVToRGB(underlayH, S, V);

			return (textColor, underlayColor);
		}
	}
}