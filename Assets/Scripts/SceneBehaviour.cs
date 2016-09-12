using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class SceneBehaviour : MonoBehaviour {
	public List<Sprite> ericTexts = new List<Sprite>();

	public List<AudioClip> ericVoice = new List<AudioClip> ();
	public List<AudioClip> investorVoice = new List<AudioClip> ();

	private int convStateIndex = -1;
	private struct ConversationState
	{
		public int currEricMenuState;
		public int currInvestorState;

		public int currEricAudioState;
		public int currInvestorAudioState;

		private Vector2 nextAllowedConvStates;

		public Vector2 NextAllowedConvStates
		{
			set {
				nextAllowedConvStates = value;
			}
		}

		public ConversationState (int ericMenuState, int investorState, int audioStateEric, int audioStateInvestor, Vector2 alowedConvStates)
		{
			currEricMenuState = ericMenuState;
			currInvestorState = investorState;
			nextAllowedConvStates = alowedConvStates;
			currEricAudioState = audioStateEric;
			currInvestorAudioState = audioStateInvestor;
		}

		public bool IsNextConvStateAllowed (int nextConvStateIndex)
		{
			if (nextAllowedConvStates.x == nextConvStateIndex || nextAllowedConvStates.y == nextConvStateIndex)
				return true;

			return false;
		}

		public int ConvertToDirectionalState (int alowedStateDirection)
		{
			if (alowedStateDirection >= 0) {
				return (int)nextAllowedConvStates.x;
			} 
			else {
				return (int)nextAllowedConvStates.y;
			}
		}
	}

	private List<ConversationState> possibleConvStates = new List<ConversationState>();

	public GameObject investorObj;
	public GameObject ericObj;
	public InvestorCommunicator communicatorScript;

	public Image textNavigationImage;

	//---------------------------------------------
	void Start () {
		//ToDo Create scenario FROM file loading mechanism
		ConversationState newState0 = new ConversationState (0, -3, 0, -1, new Vector2(1, -1));
		ConversationState newState1 = new ConversationState (1, -3, 1, -1, new Vector2(3, 2));
		ConversationState newState2 = new ConversationState (1, -3, 2, -1, new Vector2(3, -1));
		ConversationState newState3 = new ConversationState (2, -3, 3, -1, new Vector2(4, 1));

		ConversationState newState4 = new ConversationState (-1, -3, 4, -1, new Vector2(5, -1));

		ConversationState newState5 = new ConversationState (-1, -1, -1, 0, new Vector2(6, -1));
		ConversationState newState6 = new ConversationState (-1, 0, -1, -1, new Vector2(7, -1));
		ConversationState newState7 = new ConversationState (-1, 1, -1, 1, new Vector2(8, -1));
		ConversationState newState8 = new ConversationState (-1, -1, -1, 2, new Vector2(9, -1));
		ConversationState newState9 = new ConversationState (-1, 2, -1, -1, new Vector2(10, -1));
		ConversationState newState10 = new ConversationState (-1, 3, -1, -1, new Vector2(11, -1));
		ConversationState newState11 = new ConversationState (-1, 4, -1, -1, new Vector2(12, -1));
		ConversationState newState12 = new ConversationState (-1, -1, -1, 3, new Vector2(13, -1));
		ConversationState newState13 = new ConversationState (-1, 5, -1, -1, new Vector2(14, -1));

		ConversationState newState14 = new ConversationState (-1, -1, -1, 4, new Vector2(15, -1));

		ConversationState newState15 = new ConversationState (3, -2, -1, -1, new Vector2(16, -1));
		ConversationState newState16 = new ConversationState (4, -3, 5, -1, new Vector2(0, 15));

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
		possibleConvStates.Add (newState14);
		possibleConvStates.Add (newState15);
		possibleConvStates.Add (newState16);
		//-----------------------------------------------
	}

	public int GetCurrDictationState()
	{
		return  possibleConvStates [convStateIndex].currInvestorState;
	}

	public void GoNextState ()
	{
		int prevState = convStateIndex;
		convStateIndex++;

		if (convStateIndex >= possibleConvStates.Count) {
			convStateIndex = 0;
		}

		if (prevState == -1 && convStateIndex == 0 || possibleConvStates [prevState].IsNextConvStateAllowed (convStateIndex)) {
			OnStateChanged ();
		} else {
			convStateIndex = prevState;
		}
	}

	public void GoState (int stateIndex)
	{
		int prevState = convStateIndex;
		convStateIndex = stateIndex;

		if (prevState == -1 && convStateIndex == 0 || possibleConvStates [prevState].IsNextConvStateAllowed (convStateIndex)) {
			OnStateChanged ();
		} else {
			convStateIndex = prevState;
		}
	}

	public void GoDirectionalState (int alowedStateDirection)
	{
		int prevState = convStateIndex;
		convStateIndex = possibleConvStates [convStateIndex].ConvertToDirectionalState(alowedStateDirection);

		if (prevState == -1 && convStateIndex == 0 || convStateIndex > -1 && possibleConvStates [prevState].IsNextConvStateAllowed (convStateIndex)) {
			OnStateChanged ();
		} else {
			convStateIndex = prevState;
		}
	}

	public void OnCurrSentenceSaid ()
	{
		GoNextState ();
	}

	/// <summary>
	/// Called when our object is selected.  Generally called by
	/// a gesture management component.
	/// </summary>
	public void OnSelectTransfered()
	{
		GoNextState ();
	}

	private bool IsNextStateSameEricText ()
	{
		if (convStateIndex + 1 < possibleConvStates.Count) {
			return possibleConvStates [convStateIndex].currEricMenuState == possibleConvStates [convStateIndex + 1].currEricMenuState;				
		}

		return false;
	}

	private void OnStateChanged ()
	{
		//----------Audio part-----------
		CancelInvoke ();

		AudioSource audioEric = ericObj.GetComponent<AudioSource> ();
		AudioSource audioInvestor = investorObj.GetComponent<AudioSource> ();
		audioInvestor.Stop ();
		audioEric.Stop ();

		int audioEricIndex = possibleConvStates [convStateIndex].currEricAudioState;
		int audioInvestorIndex = possibleConvStates [convStateIndex].currInvestorAudioState;

		if (audioEricIndex >= 0) {
			audioEric.clip = ericVoice [audioEricIndex];
			audioEric.Play ();

			if (IsNextStateSameEricText ()) {
				Invoke ("GoNextState", audioEric.clip.length);
			}
		}
		else if (audioInvestorIndex >= 0) 
		{
			audioInvestor.clip = investorVoice [audioInvestorIndex];
			audioInvestor.Play ();

			//Auto change state after voice COMPLETED - for INVESTOR TALKING states
			if (possibleConvStates [convStateIndex].currInvestorState == -1) {
				Invoke ("GoNextState", audioInvestor.clip.length);
			}
		}
		//-------------------------------

		//Set Eric Text
		if (possibleConvStates [convStateIndex].currEricMenuState < 0) {
			textNavigationImage.gameObject.SetActive (false);
		} else {
			textNavigationImage.sprite = ericTexts [possibleConvStates [convStateIndex].currEricMenuState];
			textNavigationImage.gameObject.SetActive (true);
		}

		//Set Communicator Text
		if (possibleConvStates [convStateIndex].currInvestorState >= -1) {
			communicatorScript.gameObject.SetActive (true);
			investorObj.SetActive (true);
		} else {
			communicatorScript.gameObject.SetActive (false);
			investorObj.SetActive (false);
		}

		if (!Application.isEditor) {
			//Reset words offset for voice recognition system pronunciation check
			communicatorScript.ResetCorrectWordsOffset ();

			if (possibleConvStates [convStateIndex].currInvestorState >= 0) {
				communicatorScript.StartConversation ();
			} else {
				communicatorScript.StopConversation ();
			}
		}
	}
}
