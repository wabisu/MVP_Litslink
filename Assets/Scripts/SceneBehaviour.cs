using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;

public class SceneBehaviour : MonoBehaviour {
	public List<Sprite> ericTexts = new List<Sprite>();

	public List<AudioClip> ericVoice = new List<AudioClip> ();
	public List<AudioClip> investorVoice = new List<AudioClip> ();

	private enum CharacterMovingState { MOVE_ERIC_TO_ERICPOS, MOVE_ERIC_TO_DAVIDPOS, MOVE_DAVID_TO_DAVIDPOS }
	private enum CharacterName { ERIC, DAVID, DAVID_END, DAVID_FROM_METHOD1, DAVID_FROM_METHOD2, DAVID_FROM_METHOD3 }

	private const string IDLE_ANIMATION_NAME = "Idle";
	private const float IDLE_WAIT_TIME = 10;

	private int convStateIndex = -1;

	private Coroutine nextStateAfterAnimCoroutine;

	//-----------------------------
	private class ConversationState
	{
		public bool autoSkipState = false;

		public int currEricMenuState;
		public int currInvestorState;

		public int currEricAudioState;
		public int currInvestorAudioState;

		public Dictionary <string, int> keywordStates;

		public List<CharacterMovingState> characterMovingState;

		public Dictionary<CharacterName, string> stateAnimations;

		public ConversationState (bool autoSkip, int ericMenuState, int investorState, int audioStateEric, int audioStateInvestor, Dictionary <string, int> keywordsDict, List<CharacterMovingState> moveTo, Dictionary<CharacterName, string> animationsList)
		{
			currEricMenuState = ericMenuState;
			currInvestorState = investorState;
			currEricAudioState = audioStateEric;
			currInvestorAudioState = audioStateInvestor;

			keywordStates = keywordsDict;

			characterMovingState = moveTo;

			stateAnimations = animationsList;

			autoSkipState = autoSkip;

			if (Application.isEditor) {
				autoSkipState = true;
			}
		}

		public ConversationState (bool autoSkip, int ericMenuState, int investorState, int audioStateEric, int audioStateInvestor, Dictionary <string, int> keywordsDict)
			: this (autoSkip, ericMenuState, investorState, audioStateEric, audioStateInvestor, keywordsDict, new List<CharacterMovingState>(), new Dictionary<CharacterName, string>())
		{}

		public ConversationState (int ericMenuState, int investorState, int audioStateEric, int audioStateInvestor, Dictionary <string, int> keywordsDict, Dictionary<CharacterName, string> animationsList)
			: this (false, ericMenuState, investorState, audioStateEric, audioStateInvestor, keywordsDict, new List<CharacterMovingState>(), animationsList)
		{}

		public ConversationState (int ericMenuState, int investorState, int audioStateEric, int audioStateInvestor, Dictionary <string, int> keywordsDict)
			: this (false, ericMenuState, investorState, audioStateEric, audioStateInvestor, keywordsDict, new List<CharacterMovingState>(), new Dictionary<CharacterName, string>())
		{}

		public ConversationState (int ericMenuState, int investorState, int audioStateEric, int audioStateInvestor, Dictionary <string, int> keywordsDict, List<CharacterMovingState> moveTo, Dictionary<CharacterName, string> animationsList)
			: this (false, ericMenuState, investorState, audioStateEric, audioStateInvestor, keywordsDict, moveTo, animationsList)
		{}
	}
	//-----------------------------

	private List<ConversationState> possibleConvStates = new List<ConversationState>();

	public GameObject investorObj;
	public GameObject ericObj;

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

	private Vector3 ERIC_POSITION = new Vector3 (-0.0754f, 0f, -0.0353f);
	private Vector3 DAVID_POSITION = new Vector3 (0f, 0f, -0.13f);
	//private const float MOVING_SPEED = 0.135f;
	private const float MOVING_SPEED = 0.054f;
	private int currMovingIndex = 0;
	private GameObject movingCharacter;
	private Vector3 movingCharacterFinalPos;

	private Text debugTxt;

	//---------------------------------------------
	void Update ()
	{
		if (Application.isEditor && Input.GetKeyDown ("space")) {
			OnSceneAirTap ();
		}

		//Perform Character movement
		if (movingCharacter != null) {
			Vector3 prevCharacterPos = movingCharacter.transform.position;
			movingCharacter.transform.localPosition = Vector3.MoveTowards (movingCharacter.transform.localPosition, movingCharacterFinalPos, MOVING_SPEED * Time.deltaTime);

			Vector3 distanceDelta = prevCharacterPos - movingCharacter.transform.position;
			//Check if movement is finished
			if (distanceDelta.magnitude < MOVING_SPEED * Time.deltaTime) {
				//Check more movement
				if (currMovingIndex < possibleConvStates [convStateIndex].characterMovingState.Count - 1) {
					InitMovement (currMovingIndex++);	
				} 
				//Finish movement
				else {
					movingCharacter.GetComponent<Animator> ().SetTrigger (IDLE_ANIMATION_NAME);
					currMovingIndex = 0;
					movingCharacter = null;

					if (possibleConvStates [convStateIndex].autoSkipState && possibleConvStates [convStateIndex].currEricAudioState < 0 && possibleConvStates [convStateIndex].currInvestorAudioState < 0) {
						GoNextState ();
					}
				}
			}
		}
	}

	private void InitMovement (int movIndex)
	{
		switch (possibleConvStates [convStateIndex].characterMovingState [movIndex]) {
		case CharacterMovingState.MOVE_ERIC_TO_DAVIDPOS:
			movingCharacter = ericObj;
			movingCharacterFinalPos = DAVID_POSITION;
			break;
		case CharacterMovingState.MOVE_ERIC_TO_ERICPOS:
			movingCharacter = ericObj;
			movingCharacterFinalPos = ERIC_POSITION;
			break;
		case CharacterMovingState.MOVE_DAVID_TO_DAVIDPOS:
			movingCharacter = investorObj;
			movingCharacterFinalPos = DAVID_POSITION;
			break;
		}
	}

	void LateUpdate ()
	{
		//Check objects look time
		if (convStateIndex >= 0 && investorObj.activeSelf && possibleConvStates [convStateIndex].currInvestorAudioState != -2) {
			totalTimeTalking += Time.deltaTime;

			if (HoloToolkit.Unity.GazeManager.Instance.IsFocusedObjectTag ("InvestorFace")) {
				investorFaceLookTime += Time.deltaTime;
			} else if (HoloToolkit.Unity.GazeManager.Instance.IsFocusedObjectTag ("TextToSay")) {
				scriptLookTime += Time.deltaTime;
			}

			if (possibleConvStates [convStateIndex].currInvestorState >= 0 && !microphoneManager.IsUserTalking () && !HoloToolkit.Unity.GazeManager.Instance.IsFocusedObjectTag ("TextToSay") 
				&& !HoloToolkit.Unity.GazeManager.Instance.IsFocusedObjectTag ("InvestorFace") && !IsInvoking() && nextStateAfterAnimCoroutine == null && !InvestorCommunicator.Instance.IsRecordedAudioPlaying () 
				&& !HoloToolkit.Unity.GazeManager.Instance.IsFocusedObjectTag ("UI") )
			{
				investorDoesNotLookTime += Time.deltaTime;
			} else {
				investorDoesNotLookTime = 0;
			}

			if (investorDoesNotLookTime >= IDLE_WAIT_TIME && !microphoneManager.IsUserTalking () && !IsInvoking() && nextStateAfterAnimCoroutine == null && !InvestorCommunicator.Instance.IsRecordedAudioPlaying ()) {
				investorDoesNotLookTime = 0;
				GoState (possibleConvStates.Count - 1);	
			}

			//**********DEBUG************
			debugTxt.text = "Memorization " + GetMemorization () + " " + "Contact " + GetEyeContact ();
			//***************************
        }
	}

	void Start () {
		debugTxt = GameObject.Find ("debug").GetComponent<Text> ();

		//ToDo Create scenario FROM file loading mechanism
		ConversationState newState0 = new ConversationState (true, 0, -3, 0, -1, new Dictionary<string, int>() { {"hey eric", 16} }, new List<CharacterMovingState>() { CharacterMovingState.MOVE_ERIC_TO_DAVIDPOS }, new Dictionary<CharacterName, string>() { {CharacterName.ERIC, "SmallStep"} });
		ConversationState newState1 = new ConversationState (true, 1, -3, 1, -1, new Dictionary<string, int>() { {"hey eric", 16}, {"hi eric", 3} });
		ConversationState newState2 = new ConversationState (1, -3, 2, -1, new Dictionary<string, int>() { {"hey eric", 16}, {"hi eric", 3} });
		ConversationState newState3 = new ConversationState (2, -3, 3, -1, new Dictionary<string, int>() { {"hey eric", 16}, {"repeat", 1}, {"lets do it", 4} });

		ConversationState newState4 = new ConversationState (true, -1, -3, 4, -1, new Dictionary<string, int>() { {"hey eric", 16} }, new List<CharacterMovingState>() { CharacterMovingState.MOVE_ERIC_TO_ERICPOS }, new Dictionary<CharacterName, string>() /*{ {CharacterName.ERIC, "Back_SmallStep"} }*/);
		ConversationState newState5 = new ConversationState (true, -1, -1, -1, -1, new Dictionary<string, int>() { {"hey eric", 16} }, new List<CharacterMovingState>() { CharacterMovingState.MOVE_DAVID_TO_DAVIDPOS }, new Dictionary<CharacterName, string>() { {CharacterName.DAVID, "SmallStep"} });

		ConversationState newState6 = new ConversationState (-1, -1, -1, 0, new Dictionary<string, int>() { {"hey eric", 16} }, new List<CharacterMovingState>(), new Dictionary<CharacterName, string>() { {CharacterName.DAVID, "1Alright"}});
		ConversationState newState7 = new ConversationState (-1, 0, -1, 1, new Dictionary<string, int>() { {"hey eric", 16} }, new Dictionary<CharacterName, string>() { {CharacterName.DAVID_END, "1Ok"}});
		ConversationState newState8 = new ConversationState (-1, 1, -1, -1, new Dictionary<string, int>() { {"hey eric", 16} },  new Dictionary<CharacterName, string>() { {CharacterName.DAVID_END, "1CuriousNod"}});
		ConversationState newState9 = new ConversationState (-1, -1, -1, 2, new Dictionary<string, int>() { {"hey eric", 16} }, new Dictionary<CharacterName, string>() { {CharacterName.DAVID, "1Interesting"}});
		ConversationState newState10 = new ConversationState (-1, 2, -1, -1, new Dictionary<string, int>() { {"hey eric", 16} });
		ConversationState newState11 = new ConversationState (-1, 3, -1, -1, new Dictionary<string, int>() { {"hey eric", 16} });
		ConversationState newState12 = new ConversationState (-1, 4, -1, -1, new Dictionary<string, int>() { {"hey eric", 16} });
		ConversationState newState13 = new ConversationState (-1, -1, -1, 3, new Dictionary<string, int>() { {"hey eric", 16} }, new Dictionary<CharacterName, string>() { {CharacterName.DAVID, "1AProductToDemo"}});
		ConversationState newState14 = new ConversationState (-1, 5, -1, -1, new Dictionary<string, int>() { {"hey eric", 16} }, new Dictionary<CharacterName, string>() { {CharacterName.DAVID_END, "1Surprised"}});

		ConversationState newState15 = new ConversationState (-1, -1, -1, -2, new Dictionary<string, int>() { {"hey eric", 16} }, new Dictionary<CharacterName, string>() { {CharacterName.DAVID_FROM_METHOD1, "1Excellent"}, {CharacterName.DAVID_FROM_METHOD2, "1NotBad"}, {CharacterName.DAVID_FROM_METHOD3, "1PoorPerformance"}});

		ConversationState newState16 = new ConversationState (3, -2, -1, -1, new Dictionary<string, int>() { {"hey eric", 16} });
		ConversationState newState17 = new ConversationState (4, -3, 5, -1, new Dictionary<string, int>() { {"hey eric", 16}, {"practice again", 1}, {"result", 15} });

		ConversationState newState18 = new ConversationState (-1, -1, -1, 7, new Dictionary<string, int>() { {"returnToStateAuto", -1} }, new Dictionary<CharacterName, string>() { {CharacterName.DAVID, "1IWillLeave"}});

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
		possibleConvStates.Add (newState18);
		//-----------------------------------------------

		if (Application.isEditor) {
			transform.position = new Vector3 (0, 0, 0);
			GetComponent<Placeable> ().ResetInitialPos ();
			GoState (0);
		}
	}

	private void GoState0 ()
	{
		GoState (0);
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

	private CharacterName GetFinalResultAnimKey ()
	{
		if (GetEyeContact () >= finalResultsInvestorMarks.x) {
			return CharacterName.DAVID_FROM_METHOD1;
		} else if (GetEyeContact () >= finalResultsInvestorMarks.y) {
			return CharacterName.DAVID_FROM_METHOD2;
		} else {
			return CharacterName.DAVID_FROM_METHOD3;
		}
	}

	public bool OnKeywordSaid (string keyWord)
	{
		if (possibleConvStates [convStateIndex].keywordStates.ContainsKey (keyWord)) {
			GoState (possibleConvStates [convStateIndex].keywordStates [keyWord]);
			return true;
		}

		return false;
	}

	public int GetCurrDictationState()
	{
		return  possibleConvStates [convStateIndex].currInvestorState;
	}

	public void GoNextState ()
	{
		nextStateAfterAnimCoroutine = null;
		OnPreStateChange ();
		convStateIndex++;

		if (convStateIndex >= possibleConvStates.Count) {
			convStateIndex = 0;
		}

		if (possibleConvStates [convStateIndex].keywordStates.ContainsKey ("returnToStateAuto")) {
			GoNextState ();
			return;
		}

		OnAfterStateChange ();
	}

	public void GoState (int stateIndex)
	{
		if (possibleConvStates [stateIndex].keywordStates.ContainsKey ("returnToStateAuto")) {
			possibleConvStates [stateIndex].keywordStates ["returnToStateAuto"] = convStateIndex;
		}

		OnPreStateChange ();
		convStateIndex = stateIndex;
		OnAfterStateChange ();
	}

	private void ReturnToStateAuto ()
	{
		GoState(possibleConvStates [convStateIndex].keywordStates ["returnToStateAuto"]);
	}

	public void OnCurrSentenceSaid ()
	{
		if (possibleConvStates [convStateIndex].stateAnimations.ContainsKey (CharacterName.DAVID_END)) {
			string clipName = possibleConvStates [convStateIndex].stateAnimations [CharacterName.DAVID_END];
			//investorObj.GetComponent<Animator> ().SetTrigger (clipName);

			int audioInvestorIndex = possibleConvStates [convStateIndex].currInvestorAudioState;
			if (audioInvestorIndex >= 0) {
				AudioSource audioInvestor = investorObj.GetComponent<AudioSource> ();
				audioInvestor.clip = investorVoice [audioInvestorIndex];
				audioInvestor.Play ();

				Invoke ("GoNextState", audioInvestor.clip.length);
				return;
			}

			//nextStateAfterAnimCoroutine = StartCoroutine (GoNextStateAfterAnim ());
			GoNextState ();
		} else {
			GoNextState ();
		}
	}

	IEnumerator GoNextStateAfterAnim()
	{
		Animator animator = investorObj.GetComponent<Animator> ();
		do
		{
			//"Waiting for transition"
			yield return null;
		} while(animator.IsInTransition(0));

		yield return new WaitForSeconds (animator.GetCurrentAnimatorClipInfo (0) [0].clip.length);
		GoNextState ();
	}

	/// <summary>
	/// Called when our object is selected.  Generally called by
	/// a gesture management component.
	/// </summary>
	public void OnSceneAirTap()
	{
		if (InvestorCommunicator.Instance.IsRecordedAudioPlaying ())
			return;

		if (!possibleConvStates [convStateIndex].keywordStates.ContainsKey ("returnToStateAuto") && possibleConvStates [convStateIndex].currEricMenuState != 0) {

			if (possibleConvStates [convStateIndex].currInvestorState >= 0) {
				OnCurrSentenceSaid ();
			} else {
				GoNextState ();
			}
		}
	}

	private void OnPreStateChange ()
	{
		if (convStateIndex < 0) {
			return;
		}

		CancelInvoke ();
		StopAllCoroutines ();

		//Fast finish character movement
		if (movingCharacter != null) {
			InitMovement (possibleConvStates [convStateIndex].characterMovingState.Count - 1);
			movingCharacter.transform.localPosition = movingCharacterFinalPos;
			currMovingIndex = 0;
			movingCharacter = null;

			if (possibleConvStates [convStateIndex].stateAnimations.ContainsKey (CharacterName.ERIC)) {
				ericObj.GetComponent<Animator> ().SetTrigger (IDLE_ANIMATION_NAME);
			}

			if (possibleConvStates [convStateIndex].stateAnimations.ContainsKey (CharacterName.DAVID) || possibleConvStates [convStateIndex].stateAnimations.ContainsKey (CharacterName.DAVID_END)) {
				investorObj.GetComponent<Animator> ().SetTrigger (IDLE_ANIMATION_NAME);
			}
		}
	}

	private void OnAfterStateChange ()
	{
		//----------Audio part-----------
		AudioSource audioEric = ericObj.GetComponent<AudioSource> ();
		AudioSource audioInvestor = investorObj.GetComponent<AudioSource> ();
		audioInvestor.Stop ();
		audioEric.Stop ();

		int audioEricIndex = possibleConvStates [convStateIndex].currEricAudioState;
		int audioInvestorIndex = possibleConvStates [convStateIndex].currInvestorAudioState;

		if (audioEricIndex >= 0) {
			audioEric.clip = ericVoice [audioEricIndex];
			audioEric.Play ();

			if (possibleConvStates [convStateIndex].autoSkipState) {
				Invoke ("GoNextState", audioEric.clip.length);
			}
		} else if (audioInvestorIndex >= 0 && !possibleConvStates [convStateIndex].stateAnimations.ContainsKey(CharacterName.DAVID_END)) {
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
			audioInvestor.clip = investorVoice [GetFinalResultInvestorAudioIndex ()];
			audioInvestor.Play ();

			//Auto change state after voice COMPLETED - for INVESTOR TALKING states
			if (possibleConvStates [convStateIndex].currInvestorState == -1) {
				Invoke ("GoNextState", audioInvestor.clip.length);
			}
		}
		//-------------------------------

		//Set Eric (UI) Text
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
		if (!possibleConvStates [convStateIndex].stateAnimations.ContainsKey (CharacterName.ERIC) && possibleConvStates [convStateIndex].currInvestorState >= -1) {
			InvestorCommunicator.Instance.gameObject.SetActive (true);
			investorObj.SetActive (true);
		} else {
			InvestorCommunicator.Instance.gameObject.SetActive (false);
			investorObj.SetActive (false);
		}

		//Conversation RESET
		if (!Application.isEditor) {
			if (convStateIndex == 0) {
				scriptLookTime = 0;
				investorFaceLookTime = 0;
				investorDoesNotLookTime = 0;
				totalTimeTalking = 0;
			}

			if (possibleConvStates [convStateIndex].currInvestorState >= 0) {
				InvestorCommunicator.Instance.StartConversation ();
			} else {
				InvestorCommunicator.Instance.StopConversation ();
			}
		}

		//----------Character Animations----------
		if (possibleConvStates [convStateIndex].stateAnimations.ContainsKey (CharacterName.ERIC)) {
			ericObj.GetComponent<Animator> ().SetTrigger (possibleConvStates [convStateIndex].stateAnimations[CharacterName.ERIC]);
		} 

		if (possibleConvStates [convStateIndex].stateAnimations.ContainsKey (CharacterName.DAVID)) {
			investorObj.GetComponent<Animator> ().SetTrigger (possibleConvStates [convStateIndex].stateAnimations [CharacterName.DAVID]);
		}

		if (possibleConvStates [convStateIndex].stateAnimations.ContainsKey (CharacterName.DAVID_FROM_METHOD1)) {
			investorObj.GetComponent<Animator> ().SetTrigger (possibleConvStates [convStateIndex].stateAnimations [GetFinalResultAnimKey()]);
		}
		//--------------------------------------

		//----------Character Movement----------
		if (possibleConvStates [convStateIndex].characterMovingState.Count > 0) {
			InitMovement(currMovingIndex);
		}
		//--------------------------------------
	}
}
