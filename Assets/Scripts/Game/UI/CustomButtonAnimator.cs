using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI {
	public class CustomButtonAnimator : MonoBehaviour {

		[SerializeField] private Image buttonImage = null;
		[Space]
		[SerializeField] private float clickDuration = 0.2f;
		[SerializeField] [Range(0, 1)] private float minColorMultiplier = 0.7f;

		private Material buttonMaterial;

		private Coroutine buttonClickAnimationCoroutine;
		private Color initialColor;

		private void Awake () {
			if (buttonImage == null) return;
			var curMat = buttonImage.material;
			if (curMat == null) return;

			buttonMaterial = new Material(curMat);
			buttonImage.material = buttonMaterial;
			initialColor = buttonMaterial.GetColor("_Color");
		}

		private void OnDisable () {
			StopButtonClickAnimationCoroutime();
		}

		private void OnEnable () {
			StopButtonClickAnimationCoroutime();
		}

		private void StopButtonClickAnimationCoroutime () {
			if (buttonClickAnimationCoroutine != null) {
				StopCoroutine(buttonClickAnimationCoroutine);
				buttonClickAnimationCoroutine = null;
			}

			buttonMaterial?.SetColor("_Color", initialColor);
		}

		public void ObButtonClick () {
			StopButtonClickAnimationCoroutime();
			buttonClickAnimationCoroutine = StartCoroutine(ClickAnimationCoroutine());
		}

		private IEnumerator ClickAnimationCoroutine () {
			float t = 0;

			var extremumColor = initialColor * minColorMultiplier;
			while (t < 1) {
				float k = 2 * Mathf.Abs(t - 0.5f);
				var curColor = Color.Lerp(extremumColor, initialColor, k);
				buttonMaterial.SetColor("_Color", curColor);

				yield return null;
				t += Time.deltaTime / clickDuration;
			}
			buttonMaterial.SetColor("_Color", initialColor);
		}
	}
}