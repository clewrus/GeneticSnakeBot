using Simulator;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScuamaSettingsManager : MonoBehaviour {
	[SerializeField] private GyroidInput gyroid1 = null;
	[SerializeField] private GyroidInput gyroid2 = null;
	[SerializeField] private GyroidInput gyroid3 = null;
	[Space]
	[SerializeField] private ColorInput color1 = null;
	[SerializeField] private ColorInput color2 = null;
	[SerializeField] private ColorInput color3 = null;

	public SnakeInfo.ScuamaPatern CurrentScuamaPatern => new SnakeInfo.ScuamaPatern {
		giroid0 = gyroid1.CurrentValue,
		giroid1 = gyroid2.CurrentValue,
		giroid2 = gyroid3.CurrentValue,

		backgroundColor = color1.CurrentColor,
		color1 = color2.CurrentColor,
		color2 = color3.CurrentColor,
	};
	public System.EventHandler ScuamaPaternChanged { get; set; }

	private void Awake () {
		gyroid1.GyroidValueChanged += OnGyroidChanged;
		gyroid2.GyroidValueChanged += OnGyroidChanged;
		gyroid3.GyroidValueChanged += OnGyroidChanged;

		color1.ColorChanged += OnColorChanged;
		color2.ColorChanged += OnColorChanged;
		color3.ColorChanged += OnColorChanged;

		OnGyroidChanged(this, new System.EventArgs());
		OnColorChanged(this, new System.EventArgs());
	}

	private void OnGyroidChanged (object sender, System.EventArgs args) {
		ScuamaPaternChanged?.Invoke(this, new System.EventArgs());
	}

	private void OnColorChanged (object sender, System.EventArgs args) {
		ScuamaPaternChanged?.Invoke(this, new System.EventArgs());
	}
}