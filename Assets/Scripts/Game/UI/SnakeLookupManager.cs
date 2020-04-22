using Simulator;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SnakeLookupManager : MonoBehaviour {
	[SerializeField] private SnakeScarecrow scarecrow = null;
	[SerializeField] private ScuamaSettingsManager scuamaManager = null;

	public SnakeInfo.ScuamaPatern ScuamaPatern => scuamaManager.CurrentScuamaPatern;
	public int SnakeLength => scarecrow.Length;
}