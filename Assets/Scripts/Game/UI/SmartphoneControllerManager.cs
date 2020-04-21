using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SmartphoneControllerManager : MonoBehaviour {
	public EventHandler<InputRecievedEventArgs> InputRecieved { get; set; }

	private void Awake () {
		gameObject.SetActive(false);

		#if UNITY_ANDROID
			gameObject.SetActive(true);
		#endif
	}

	public void OnUpArrowDown () {
		InputRecieved?.Invoke(this, new InputRecievedEventArgs() { Direction = Vector2.up });
	}

	public void OnRightArrowDown () {
		InputRecieved?.Invoke(this, new InputRecievedEventArgs() { Direction = Vector2.right });
	}

	public void OnLeftArrowDown () {
		InputRecieved?.Invoke(this, new InputRecievedEventArgs() { Direction = Vector2.left });
	}

	public void OnDownArrowDown () {
		InputRecieved?.Invoke(this, new InputRecievedEventArgs() { Direction = Vector2.down });
	}

	public class InputRecievedEventArgs : EventArgs {
		public Vector2 Direction;
	}
}