/*
Created by: Lawrence Muller
Date: 3/1/2014
Usage: Feel free to use this for your projects and/or modify it.
 */

using UnityEngine;
using System.Collections;

/// <summary>
/// User interface radio group for NGUI.
/// Allows for data to be added to each radio button
/// Gives information on current and previous radio buttons selected.
/// 
/// Radiobuttons usually have some value associated with them, with this script, you can easily set each radio buttons 
/// value as a string, retrieve those values, and modify those values. To store integers, floats, etc... Write it as a string
/// and parse it upon retrieval to what you need.
/// 
/// INSTRUCTIONS:
/// 1. Select a radio button group value, this value will automatically be assigned to each toggle button at runtime
/// so there is no need to set each one manually.
/// 2. Place toggle buttons into the toggles array in the inspector
/// 3. Insert the data for each toggle button into the Data array in the inspector. The data must be placed
/// at the index corresponding to the toggle button it belong to
/// 
/// </summary>
[AddComponentMenu("NGUI/Interaction/Radio Group")]
public class UIRadioGroup : MonoBehaviour {
	/// <summary>
	/// Group UIToggle group number used to react to UIToggle OnChanged events
	/// </summary>
	public int group = 1;
	/// <summary>
	/// Toggle buttons for group
	/// </summary>
	[SerializeField]
	private UIToggle[] toggles;
	/// <summary>
	/// Data for each toggle button.
	/// Must be in same order as toggles
	/// </summary>
	[SerializeField]
	private string[] data;
	/// <summary>
	/// The previous toggle selected
	/// </summary>
	private UIToggle previousToggle;
	/// <summary>
	/// The current toggle selected
	/// </summary>
	private UIToggle currentToggle;
	/// <summary>
	/// Returns the currently clicked toggle button
	/// Event occurs when the radio selection has changed
	/// </summary>
	private event System.Action<UIToggle> OnToggle;
	/// <summary>
	/// Holds amount of times OnRadioChanged was called when first initializing
	/// </summary>
	private int onChangedAmount = 0;

	public UIToggle CurrentToggle{
		get{return currentToggle;}
	}

	public UIToggle PreviousToggle{
		get{return previousToggle;}
	}

	public UIToggle[] Toggles{
		get{return toggles;}
	}

	/// <summary>
	/// Gets the data from toggle button
	/// </summary>
	/// <returns>The data.</returns>
	/// <param name="toggle">Toggle.</param>
	public string GetData(UIToggle toggle){
		for(int i = 0; i < toggles.Length; i++){
			if(toggles[i] == toggle)
				return data[i];
		}
		Debug.LogError("Warning: Invalid UIToggle was used to get data.");
		return "Invalid Toggle";
	}

	/// <summary>
	/// Sets the data for the given toggle button
	/// </summary>
	/// <param name="toggle">Toggle.</param>
	/// <param name="newData">New data.</param>
	public void SetData(UIToggle toggle, string newData){
		for(int i = 0; i < toggles.Length; i++){
			if(toggles[i] == toggle){
				data[i] = newData;
				return;
			}
			Debug.LogError("Warning: Invalid UIToggle was used to set data.");
		}
	}

	/// <summary>
	/// Registers to UIToggle change events
	/// Sets toggle buttons group values
	/// </summary>
	void Awake(){
		EventDelegate toggleEvent = new EventDelegate();
		toggleEvent.target = this;
		toggleEvent.methodName = "OnRadioChanged";

		foreach(UIToggle toggle in toggles){
			toggle.onChange.Add(toggleEvent);
			toggle.group = group;
			if(toggle.startsActive)
				currentToggle = toggle;
		}

		previousToggle = currentToggle;
	}

	/// <summary>
	/// Raised by the toggle buttons onChange callback
	/// Raises OnToggle
	/// </summary>
	private void OnRadioChanged(){
		//This IF is to make sure when the toggle buttons are intialized, the events don't set the currentToggle to something different
		if(onChangedAmount > toggles.Length){
			previousToggle = currentToggle;
			currentToggle = UIToggle.current;
			if(OnToggle != null)
				OnToggle(currentToggle);
		}else
			onChangedAmount++;
	}

	public void RegisterRadioChangeListener(System.Action<UIToggle> listener){
		OnToggle += listener;
	}

	public void UnRegisterRadioChangeListener(System.Action<UIToggle> listener){
		OnToggle -= listener;
	}
}
