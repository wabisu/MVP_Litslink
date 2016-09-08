using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class EricBehaviour : MonoBehaviour {
	public List<Sprite> ericTexts = new List<Sprite>();

	public Image textNavigationImage;

	public InvestorCommunicator communicatorScript;

	private int convStateIndex;
	private struct ConversationState
	{
		public int currEricMenuState;
		public int currDictationState;
		private Vector2 nextAllowedConvStates;

		public Vector2 NextAllowedConvStates
		{
			set {
				nextAllowedConvStates = value;
			}
		}

		public ConversationState (int ericMenuState, int dictationState, Vector2 alowedConvStates)
		{
			currEricMenuState = ericMenuState;
			currDictationState = dictationState;
			nextAllowedConvStates = alowedConvStates;
		}

		public bool IsNextConvStateAllowed (int nextConvStateIndex)
		{
			if (nextAllowedConvStates.x == nextConvStateIndex || nextAllowedConvStates.y == nextConvStateIndex)
				return true;

			return false;
		}

		public int GetNextAllowedConvState ()
		{
			return (int)nextAllowedConvStates.x;	
		}

		public int GetPrevAllowedConvState ()
		{
			return (int)nextAllowedConvStates.y;
		}
	}

	private List<ConversationState> possibleConvStates = new List<ConversationState>();

	//---------------------------------------------
	void Start () {
		//ToDo Create scenario FROM file loading mechanism
		ConversationState newState0 = new ConversationState (0, -3, new Vector2(1, -1));
		ConversationState newState1 = new ConversationState (1, -3, new Vector2(2, -1));
		ConversationState newState2 = new ConversationState (2, -3, new Vector2(3, 0));
		ConversationState newState3 = new ConversationState (-1, -1, new Vector2(4, -1));
		ConversationState newState4 = new ConversationState (-1, 0, new Vector2(5, -1));
		ConversationState newState5 = new ConversationState (-1, 1, new Vector2(6, -1));
		ConversationState newState6 = new ConversationState (-1, -1, new Vector2(7, -1));
		ConversationState newState7 = new ConversationState (-1, 2, new Vector2(8, -1));
		ConversationState newState8 = new ConversationState (-1, 3, new Vector2(9, -1));
		ConversationState newState9 = new ConversationState (-1, 4, new Vector2(10, -1));
		ConversationState newState10 = new ConversationState (-1, -1, new Vector2(11, -1));
		ConversationState newState11 = new ConversationState (-1, 5, new Vector2(12, -1));
		ConversationState newState12 = new ConversationState (3, -2, new Vector2(13, 0));
		ConversationState newState13 = new ConversationState (4, -3, new Vector2(0, -1));

		possibleConvStates.Add (newState0);
		possibleConvStates.Add (newState1);
		possibleConvStates.Add (newState2);
		possibleConvStates.Add (newState3);
		possibleConvStates.Add (newState4);
		possibleConvStates.Add (newState5);
		possibleConvStates.Add (newState6);
		possibleConvStates.Add (newState7);
		possibleConvStates.Add (newState8);
		possibleConvStates.Add (newState9);
		possibleConvStates.Add (newState10);
		possibleConvStates.Add (newState11);
		possibleConvStates.Add (newState12);
		possibleConvStates.Add (newState13);
		//-----------------------------------------------

		convStateIndex = 0;
		OnStateChanged ();
	}

	public int GetCurrDictationState()
	{
		return  possibleConvStates [convStateIndex].currDictationState;
	}

	public void GoNextState ()
	{
		int prevState = convStateIndex;
		convStateIndex++;

		if (convStateIndex >= possibleConvStates.Count) {
			convStateIndex = 0;
		}

		if (possibleConvStates [prevState].IsNextConvStateAllowed (convStateIndex)) {
			OnStateChanged ();
		} else {
			convStateIndex = prevState;
		}
	}

	public void GoDirectionalState (int alowedStateDirection)
	{
		if (alowedStateDirection >= 0) {
			convStateIndex = possibleConvStates [convStateIndex].GetNextAllowedConvState ();
		} 
		else {
			convStateIndex = possibleConvStates [convStateIndex].GetPrevAllowedConvState ();
		}

		OnStateChanged ();
	}

	public void OnCurrSentenceSaid ()
	{
		GoNextState ();
	}

	/// <summary>
	/// Called when our object is selected.  Generally called by
	/// a gesture management component.
	/// </summary>
	public void OnSelect()
	{
		GoNextState ();
	}

	private void OnStateChanged ()
	{
		//Set Eric Text
		if (possibleConvStates [convStateIndex].currEricMenuState < 0) {
			textNavigationImage.gameObject.SetActive (false);
		} else {
			textNavigationImage.sprite = ericTexts [possibleConvStates [convStateIndex].currEricMenuState];
			textNavigationImage.gameObject.SetActive (true);
		}

		//Set Communicator Text
		if (possibleConvStates [convStateIndex].currDictationState >= 0) {
			communicatorScript.StartConversation ();
		} else {
			communicatorScript.StopConversation ();
		}

		if (possibleConvStates [convStateIndex].currDictationState <= -2) {
			communicatorScript.gameObject.SetActive (false);
		} else if (possibleConvStates [convStateIndex].currDictationState >= -1) {
			communicatorScript.gameObject.SetActive (true);
		}
	}
}
