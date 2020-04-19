using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Game.UI {

	public class ScoreUpdater : MonoBehaviour {
		private string textPart = null;

		private int m_Value;
		public int Value {
			get => m_Value;
			set {
				var textLable = GetComponent<TextMeshProUGUI>();
				if (textLable != null && textPart != null) {
					m_Value = value;
					textLable.text = textPart + m_Value.ToString();
				}
			}
		}

		private void Awake () {
			var textLable = GetComponent<TextMeshProUGUI>();
			var text = textLable.text.Trim();

			int nonDigitIndex = text.Length - 1;
			while (nonDigitIndex > 0 && char.IsDigit(text[nonDigitIndex])) {
				--nonDigitIndex;
			}

			textPart = text.Substring(0, nonDigitIndex + 1);
		}
	}
}
