using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Game.UI {
	public class ElipsecAnimation : MonoBehaviour {
		[SerializeField] private TextMeshProUGUI targetTextMeshPro = null;
		[SerializeField] private float animationFrameRate = 8;

		private Coroutine animationCoroutine;
		private WaitForSeconds animationDelay;

		private void OnEnable () {
			if (animationCoroutine == null) {
				animationCoroutine = StartCoroutine(AnimationCoroutine());
			}
		}

		private void OnDisable () {
			if (animationCoroutine != null) {
				StopCoroutine(animationCoroutine);
				animationCoroutine = null;
			}
		}

		private IEnumerator AnimationCoroutine () {
			int curStep = 0;
			while (true) {
				if (targetTextMeshPro != null) {
					var curText = targetTextMeshPro.text.Trim();

					int textEndIndex = curText.Length - 1;
					while (textEndIndex > 0 && !char.IsLetterOrDigit(curText[textEndIndex])) {
						--textEndIndex;
					}

					curText = curText.Substring(0, textEndIndex + 1);
					var animationSuffix = " ";
					for (int i = 0; i < curStep % 4; i++) {
						animationSuffix += '.';
					}

					targetTextMeshPro.text = curText + animationSuffix;
				}

				++curStep;

				if (animationDelay == null) {
					animationDelay = new WaitForSeconds(1f / animationFrameRate);
				}
				yield return animationDelay;
			}
		}
	}
}

