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

		public Dictionary <string, int> keywordStates;

		public ConversationState (int ericMenuState, int investorState, int audioStateEric, int audioStateInvestor, Dictionary <string, int> keywordsDict)
		{
			currEricMenuState = ericMenuState;
			currInvestorState = investorState;
			currEricAudioState = audioStateEric;
			currInvestorAudioState = audioStateInvestor;

			keywordStates = keywordsDict;
		}
	}

	private List<ConversationState> possibleConvStates = new List<ConversationState>();

	public GameObject investorObj;
	public GameObject ericObj;
	public InvestorCommunicator communicatorScript;

	public Image textNavigationImage;

	private float totalTimeTalking = 0;
	private float scriptLookTime = 0;
	private float investorFaceLookTime = 0;
	private float investorDoesNotLookTime = 0;

	private Vector3 finalResultsInvestorMarks = new Vector3 (70, 30, 0);
	private Vector3 finalResultsInvestorVoice = new Vector3 (4, 5, 6);

	public MicrophoneManager microphoneManager;

	public GameObject results;
	public Text eyeContactTxt;
	public Text memorizationTxt;

	private Text debugTxt;

	//---------------------------------------------
	void LateUpdate ()
	{
		//Check objects look time
		if (investorObj.activeSelf) {
			totalTimeTalking += Time.deltaTime;

			if (HoloToolkit.Unity.GazeManager.Instance.IsFocusedObjectTag ("InvestorFace")) {
				investorFaceLookTime += Time.deltaTime;
			} else if (HoloToolkit.Unity.GazeManager.Instance.IsFocusedObjectTag ("TextToSay")) {
				scriptLookTime += Time.deltaTime;
			}

			if (possibleConvStates [convStateIndex].currInvestorState >= 0 && !microphoneManager.IsUserTalking () && !HoloToolkit.Unity.GazeManager.Instance.IsFocusedObjectTag ("InvestorFace")) {
				investorDoesNotLookTime += Time.deltaTime;
			} else {
				investorDoesNotLookTime = 0;
			}

			if (investorDoesNotLookTime >= 5.0f) {
				investorDoesNotLookTime = 0;
				GoState (17);	
			}

			debugTxt.text = "Eye contact = " + GetEyeContact();
        }
	}

	void Start () {
		debugTxt = GameObject.Find ("debug").GetComponent<Text> ();

		//ToDo Create scenario FROM file loading mechanism
		ConversationState newState0 = new ConversationState (0, -3, 0, -1, new Dictionary<string, int>() { {"hey eric", 16} });
		ConversationState newState1 = new ConversationState (1, -3, 1, -1, new Dictionary<string, int>() { {"hey eric", 16}, {"hi eric", 3} });
		ConversationState newState2 = new ConversationState (1, -3, 2, -1, new Dictionary<string, int>() { {"hey eric", 16}, {"hi eric", 3} });
		ConversationState newState3 = new ConversationState (2, -3, 3, -1, new Dictionary<string, int>() { {"hey eric", 16}, {"repeat", 1}, {"lets do it", 4} });

		ConversationState newState4 = new ConversationState (-1, -3, 4, -1, new Dictionary<string, int>() { {"hey eric", 16} });

		ConversationState newState5 = new ConversationState (-1, -1, -1, 0, new Dictionary<string, int>() { {"hey eric", 16} });
		ConversationState newState6 = new ConversationState (-1, 0, -1, -1, new Dictionary<string, int>() { {"hey eric", 16} });
		ConversationState newState7 = new ConversationState (-1, 1, -1, 1, new Dictionary<string, int>() { {"hey eric", 16} });
		ConversationState newState8 = new ConversationState (-1, -1, -1, 2, new Dictionary<string, int>() { {"hey eric", 16} });
		ConversationState newState9 = new ConversationState (-1, 2, -1, -1, new Dictionary<string, int>() { {"hey eric", 16} });
		ConversationState newState10 = new ConversationState (-1, 3, -1, -1, new Dictionary<string, int>() { {"hey eric", 16} });
		ConversationState newState11 = new ConversationState (-1, 4, -1, -1, new Dictionary<string, int>() { {"hey eric", 16} });
		ConversationState newState12 = new ConversationState (-1, -1, -1, 3, new Dictionary<string, int>() { {"hey eric", 16} });
		ConversationState newState13 = new ConversationState (-1, 5, -1, -1, new Dictionary<string, int>() { {"hey eric", 16} });

		ConversationState newState14 = new ConversationState (-1, -1, -1, -2, new Dictionary<string, int>() { {"hey eric", 16} });

		ConversationState newState15 = new ConversationState (3, -2, -1, -1, new Dictionary<string, int>() { {"hey eric", 16} });
		ConversationState newState16 = new ConversationState (4, -3, 5, -1, new Dictionary<string, int>() { {"hey eric", 16}, {"practice again", 1}, {"result", 15} });

		ConversationState newState17 = new ConversationState (-1, -1, -1, 7, new Dictionary<string, int>() { {"returnToStateAuto", -1} });

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
		possibleConvStates.Add (newState17);
		//-----------------------------------------------
	}

	private int GetEyeContact ()
	{
		return (int)(investorFaceLookTime / totalTimeTalking * 100);
	}

	private int GetMemorization ()
	{
		return (int)((1 - scriptLookTime / totalTimeTalking) * 100);
	}

	private int GetFinalResultInvestorAudioIndex ()
	{
		if (GetEyeContact () >= finalResultsInvestorMarks.x) {
			return (int)finalResultsInvestorVoice.x;
		} else if (GetEyeContact () >= finalResultsInvestorMarks.y) {
			return (int)finalResultsInvestorVoice.y;
		} else {
			return (int)finalResultsInvestorVoice.z;
		}
	}

	public void OnKeywordSaid (string keyWord)
	{
		if (possibleConvStates [convStateIndex].keywordStates.ContainsKey (keyWord)) {
			GoState (possibleConvStates [convStateIndex].keywordStates [keyWord]);
		}
	}

	public int GetCurrDictationState()
	{
		return  possibleConvStates [convStateIndex].currInvestorState;
	}

	public void GoNextState ()
	{
		convStateIndex++;

		if (convStateIndex >= possibleConvStates.Count) {
			convStateIndex = 0;
		}

		if (possibleConvStates [convStateIndex].keywordStates.ContainsKey ("returnToStateAuto")) {
			GoNextState ();
			return;
		}

		OnStateChanged ();
	}

	public void GoState (int stateIndex)
	{
		if (possibleConvStates [stateIndex].keywordStates.ContainsKey ("returnToStateAuto")) {
			possibleConvStates [stateIndex].keywordStates ["returnToStateAuto"] = convStateIndex;
		}

		convStateIndex = stateIndex;
		OnStateChanged ();
	}

	private void ReturnToStateAuto ()
	{
		GoState(possibleConvStates [convStateIndex].keywordStates ["returnToStateAuto"]);
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
		if (!possibleConvStates [convStateIndex].keywordStates.ContainsKey ("returnToStateAuto")) {
			GoNextState ();
		}
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
				if (!possibleConvStates [convStateIndex].keywordStates.ContainsKey ("returnToStateAuto")) {
					Invoke ("GoNextState", audioInvestor.clip.length);
				} else {
					Invoke ("ReturnToStateAuto", audioInvestor.clip.length);
				}
			}
		}
		//Play final results audio
		else if (possibleConvStates [convStateIndex].currInvestorAudioState == -2) {
			audioInvestor.clip = investorVoice [GetFinalResultInvestorAudioIndex()];
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

			if (possibleConvStates [convStateIndex].currEricMenuState == 3) {
				memorizationTxt.text = GetMemorization ().ToString () + "%";
				eyeContactTxt.text = GetEyeContact ().ToString () + "%";
				results.SetActive (true);
			} else {
				results.SetActive (false);
			}
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

			if (convStateIndex == 0) {
				scriptLookTime = 0;
				investorFaceLookTime = 0;
				investorDoesNotLookTime = 0;
				totalTimeTalking = 0;
			}

			if (possibleConvStates [convStateIndex].currInvestorState >= 0) {
				communicatorScript.StartConversation ();
			} else {
				communicatorScript.StopConversation ();
			}
		}
	}
}
