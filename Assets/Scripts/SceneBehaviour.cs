using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;

public class SceneBehaviour : MonoBehaviour {
	public List<Sprite> ericTexts = new List<Sprite>();

	public List<AudioClip> ericVoice = new List<AudioClip> ();
	public List<AudioClip> investorVoice = new List<AudioClip> ();

	private enum CharacterMovingState { MOVE_ERIC_TO_ERICPOS, MOVE_ERIC_TO_DAVIDPOS, MOVE_DAVID_TO_DAVIDPOS, MOVE_ERIC_HEY_ERIC_POS, MOVE_DAVID_HEY_ERIC_POS }
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

		public ConversationState (bool autoSkip, int ericMenuState, int investorState, int audioStateEric, int audioStateInvestor, List<CharacterMovingState> moveTo, Dictionary<CharacterName, string> animationsList)
			: this (autoSkip, ericMenuState, investorState, audioStateEric, audioStateInvestor, new Dictionary <string, int>(), moveTo, animationsList)
		{}
	}
	//-----------------------------

	private List<ConversationState> possibleConvStates = new List<ConversationState>();

	public GameObject davidObj;
	public GameObject ericObj;

	public Image textNavigationImage;

	private float totalTimeTalking = 0;
	private float totalTimeUserTalking = 0;
	private float scriptLookTime = 0;
	private float investorFaceLookTime = 0;
	private float investorDoesNotLookTime = 0;

	private Vector3 finalResultsInvestorMarks = new Vector3 (70, 30, 0);
	private Vector3 finalResultsInvestorVoice = new Vector3 (4, 5, 6);

	public MicrophoneManager microphoneManager;

	public GameObject results;
	public Text eyeContactTxt;
	public Text memorizationTxt;

	private Vector3 INITIAL_POSITION = new Vector3 (0f, 0f, 0f);
	private Vector3 ERIC_POSITION = new Vector3 (-0.0754f, 0f, -0.0353f);
	private Vector3 HEY_ERIC_POSITION = new Vector3 (-0.0754f, 0f, -0.13f);
	private Vector3 DAVID_POSITION = new Vector3 (0f, 0f, -0.13f);

	private const float MOVING_SPEED = 0.065f;
	private List<GameObject> movingCharacters = new List<GameObject>();
	private List<Vector3> movingCharactersFinalPos = new List<Vector3>();

	private Text debug;

	public ReplayBtnBehaviour replayBtn;
	public PlaySampleBehaviour playSampleBtn;

	//---------------------------------------------
	private bool IsBackwardWalk (GameObject currChar)
	{
		if (currChar.Equals (davidObj)) {
			if (possibleConvStates [convStateIndex].stateAnimations [CharacterName.DAVID].Contains ("Back"))
				return true;
		} 
		else {
			if (possibleConvStates [convStateIndex].stateAnimations [CharacterName.ERIC].Contains ("Back"))
				return true;			
		}

		return false;
	}

	void Update ()
	{
		if (Application.isEditor && Input.GetKeyDown ("space")) {
			OnSceneAirTap ();
		}

		//Perform Character movement
		int i = 0;
		while (i < movingCharacters.Count) 
		{
			GameObject currChar = movingCharacters [i];
			Vector3 currCharEndPos = movingCharactersFinalPos [i];

			Vector3 prevCharacterPos = currChar.transform.localPosition;
			currChar.transform.localPosition = Vector3.MoveTowards (currChar.transform.localPosition, currCharEndPos, MOVING_SPEED * Time.deltaTime);

			Vector3 distanceDelta = currCharEndPos - currChar.transform.localPosition;
			float currCharDist = distanceDelta.magnitude;

			// rotate moving character to the moving direction
			if (currCharDist > 0.01f) {
				distanceDelta.y = 0;

				if (!IsBackwardWalk(currChar))
					currChar.transform.localRotation = Quaternion.Lerp (currChar.transform.localRotation, Quaternion.LookRotation (distanceDelta), Time.deltaTime * 30);
				else
					currChar.transform.localRotation = Quaternion.Lerp (currChar.transform.localRotation, Quaternion.LookRotation (distanceDelta) * Quaternion.Euler(0, 180, 0), Time.deltaTime * 30);
			}

			//Check if movement is finished
			if (currCharDist < MOVING_SPEED * Time.deltaTime) {
				//Finish movement
				currChar.GetComponent<Animator> ().SetTrigger (IDLE_ANIMATION_NAME);
				currChar.GetComponent<FacingCamera> ().enabled = true;
				movingCharacters.Remove (currChar);
				movingCharactersFinalPos.Remove (currCharEndPos);

				if (possibleConvStates [convStateIndex].autoSkipState && possibleConvStates [convStateIndex].currEricAudioState < 0 && possibleConvStates [convStateIndex].currInvestorAudioState < 0) {
					GoNextState ();
				}
			} else {
				i++;
			}
		}
	}

	private void InitMovement ()
	{
		foreach (CharacterMovingState charMoveState in possibleConvStates [convStateIndex].characterMovingState)
		{
			switch (charMoveState) {
			case CharacterMovingState.MOVE_ERIC_TO_DAVIDPOS:
				movingCharacters.Add (ericObj);
				movingCharactersFinalPos.Add (DAVID_POSITION);
				break;
			case CharacterMovingState.MOVE_ERIC_TO_ERICPOS:
				movingCharacters.Add (ericObj);
				movingCharactersFinalPos.Add (ERIC_POSITION);
				break;
			case CharacterMovingState.MOVE_DAVID_TO_DAVIDPOS:
				movingCharacters.Add (davidObj);
				movingCharactersFinalPos.Add (DAVID_POSITION);
				break;
			case CharacterMovingState.MOVE_ERIC_HEY_ERIC_POS:
				movingCharacters.Add (ericObj);
				movingCharactersFinalPos.Add (HEY_ERIC_POSITION);
				break;
			case CharacterMovingState.MOVE_DAVID_HEY_ERIC_POS:
				movingCharacters.Add (davidObj);
				movingCharactersFinalPos.Add (INITIAL_POSITION);
				break;
			}

			movingCharacters[movingCharacters.Count - 1].GetComponent<FacingCamera> ().enabled = false;
		}
	}

	void LateUpdate ()
	{
		//Check objects look time
		if (convStateIndex >= 0 && possibleConvStates [convStateIndex].currInvestorState >= -1 && possibleConvStates [convStateIndex].currInvestorAudioState != -2) {
			totalTimeTalking += Time.deltaTime;

			if (possibleConvStates [convStateIndex].currInvestorState >= 0)
			{
				totalTimeUserTalking += Time.deltaTime;

				if (HoloToolkit.Unity.GazeManager.Instance.IsFocusedObjectTag ("TextToSay"))
				{
					scriptLookTime += Time.deltaTime;
				}
			}

			if (HoloToolkit.Unity.GazeManager.Instance.IsFocusedObjectTag ("InvestorFace")) {
				investorFaceLookTime += Time.deltaTime;
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
			//debug.text = "Memorization " + GetMemorization () + " " + "Contact " + GetEyeContact ();
			//***************************
        }
	}

	void Start () {
		debug = GameObject.Find ("debug").GetComponent<Text> ();

		//ToDo Create scenario FROM file loading mechanism
		ConversationState newState0 = new ConversationState (true, 0, -3, 0, -1, new Dictionary<string, int>(), new List<CharacterMovingState>() { CharacterMovingState.MOVE_ERIC_TO_DAVIDPOS }, new Dictionary<CharacterName, string>() { {CharacterName.ERIC, "SmallStep"} });
		ConversationState newState1 = new ConversationState (1, -3, -1, -1, new Dictionary<string, int>() { {"hi eric", 2} });
		ConversationState newState2 = new ConversationState (true, -1, -3, 1, -1, new Dictionary<string, int>());
		ConversationState newState3 = new ConversationState (true, -1, -3, 2, -1, new Dictionary<string, int>());
		ConversationState newState4 = new ConversationState (true, -1, -3, 3, -1, new Dictionary<string, int>());
		ConversationState newState5 = new ConversationState (2, -3, -1, -1, new Dictionary<string, int>() { {"yes", 2}, {"no", 6} });

		ConversationState newState6 = new ConversationState (true, -1, -1, 4, -1, new List<CharacterMovingState>() { CharacterMovingState.MOVE_ERIC_TO_ERICPOS, CharacterMovingState.MOVE_DAVID_TO_DAVIDPOS }, new Dictionary<CharacterName, string>() { {CharacterName.ERIC, "Back_SmallStep"}, {CharacterName.DAVID, "SmallStep"} });

		ConversationState newState7 = new ConversationState (-1, -1, -1, 0, new Dictionary<string, int>(), new Dictionary<CharacterName, string>() { {CharacterName.DAVID, "1Alright"}});
		ConversationState newState8 = new ConversationState (-1, 0, -1, 1, new Dictionary<string, int>() { {"hey eric", 18} }, new Dictionary<CharacterName, string>() { {CharacterName.DAVID_END, "1Ok"}});
		ConversationState newState9 = new ConversationState (-1, 1, -1, -1, new Dictionary<string, int>() { {"hey eric", 18} },  new Dictionary<CharacterName, string>() { {CharacterName.DAVID_END, "1CuriousNod"}});
		ConversationState newState10 = new ConversationState (-1, -1, -1, 2, new Dictionary<string, int>(), new Dictionary<CharacterName, string>() { {CharacterName.DAVID, "1Interesting"}});
		ConversationState newState11 = new ConversationState (-1, 2, -1, -1, new Dictionary<string, int>() { {"hey eric", 18} });
		ConversationState newState12 = new ConversationState (-1, 3, -1, -1, new Dictionary<string, int>() { {"hey eric", 18} }, new Dictionary<CharacterName, string>() { { CharacterName.DAVID_END, "1Ok" } });
		ConversationState newState13 = new ConversationState (-1, 4, -1, -1, new Dictionary<string, int>() { {"hey eric", 18} });
		ConversationState newState14 = new ConversationState (-1, -1, -1, 3, new Dictionary<string, int>(), new Dictionary<CharacterName, string>() { {CharacterName.DAVID, "1AProductToDemo"}});
		ConversationState newState15 = new ConversationState (-1, 5, -1, -1, new Dictionary<string, int>() { {"hey eric", 18} }, new Dictionary<CharacterName, string>() { {CharacterName.DAVID_END, "1Surprised"}});

		ConversationState newState16 = new ConversationState (-1, -1, -1, -2, new Dictionary<string, int>(), new Dictionary<CharacterName, string>() { {CharacterName.DAVID_FROM_METHOD1, "1Excellent"}, {CharacterName.DAVID_FROM_METHOD2, "1NotBad"}, {CharacterName.DAVID_FROM_METHOD3, "1PoorPerformance"}});

		ConversationState newState17 = new ConversationState (3, -2, -1, -1, new Dictionary<string, int>() { {"hey eric", 18} });
		ConversationState newState18 = new ConversationState (4, -2, 5, -1, new Dictionary<string, int>() { {"practice again", 0}, {"result", 17} }, new List<CharacterMovingState>() { CharacterMovingState.MOVE_ERIC_HEY_ERIC_POS, CharacterMovingState.MOVE_DAVID_HEY_ERIC_POS }, new Dictionary<CharacterName, string>() { {CharacterName.ERIC, "SmallStep"}, {CharacterName.DAVID, "Back_SmallStep"} });

		ConversationState newState19 = new ConversationState (-1, -1, -1, 7, new Dictionary<string, int>() { {"returnToStateAuto", -1} }, new Dictionary<CharacterName, string>() { {CharacterName.DAVID, "1IWillLeave"}});

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
		possibleConvStates.Add (newState19);
		//-----------------------------------------------

		Vector3 lookPos = davidObj.transform.position - Camera.main.transform.position;
		lookPos.y = 0;
		davidObj.transform.localRotation = Quaternion.LookRotation (lookPos) * Quaternion.Euler (0, 180, 0);
		ericObj.transform.localRotation = Quaternion.LookRotation (lookPos) * Quaternion.Euler (0, 180, 0);

		if (Application.isEditor) {
			transform.position = INITIAL_POSITION;
			GetComponent<Placeable> ().ResetInitialPos ();
			GoState (0);
		}
	}

	private int GetEyeContact ()
	{
		return (int)(investorFaceLookTime / totalTimeTalking * 100 + 5);
	}

	private int GetMemorization ()
	{
		if (totalTimeUserTalking == 0) {
			return 0;
		}

		return (int)((1 - scriptLookTime / totalTimeUserTalking) * 100);
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
			if (keyWord.Equals ("practice again")) {
				scriptLookTime = 0;
				investorFaceLookTime = 0;
				investorDoesNotLookTime = 0;
				totalTimeTalking = 0;
				totalTimeUserTalking = 0;
				replayBtn.gameObject.SetActive (false);
			}

			GoState (possibleConvStates [convStateIndex].keywordStates [keyWord]);
			return true;
		}
		else if (keyWord.Equals("sample"))
		{
			playSampleBtn.OnSelect ();
			return true;
		}
		else if (keyWord.Equals("replay"))
		{
			replayBtn.OnSelect ();;
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
			convStateIndex = 6;

			scriptLookTime = 0;
			investorFaceLookTime = 0;
			investorDoesNotLookTime = 0;
			totalTimeTalking = 0;
			totalTimeUserTalking = 0;
			replayBtn.gameObject.SetActive (false);
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
				AudioSource audioInvestor = davidObj.GetComponent<AudioSource> ();
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
		Animator animator = davidObj.GetComponent<Animator> ();
		do
		{
			//"Waiting for transition"
			yield return null;
		} while(animator.IsInTransition(0));

		yield return new WaitForSeconds (animator.GetCurrentAnimatorClipInfo (0) [0].clip.length);
		GoNextState ();
	}

	private bool OnAirTapSkipAllowed ()
	{
		if (possibleConvStates [convStateIndex].currInvestorState >= 0 || possibleConvStates [convStateIndex].keywordStates.Count > 0 || Application.isEditor) {
			return true;
		}

		return false;
	}

	/// <summary>
	/// Called when our object is selected.  Generally called by
	/// a gesture management component.
	/// </summary>
	public void OnSceneAirTap()
	{
		if (InvestorCommunicator.Instance.IsRecordedAudioPlaying () || !OnAirTapSkipAllowed())
			return;

		if (!possibleConvStates [convStateIndex].keywordStates.ContainsKey ("returnToStateAuto")) {

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

		if (convStateIndex == 0) {
			davidObj.transform.localPosition = INITIAL_POSITION;
		}

		if (convStateIndex == 1) {
			debug.text = "";
		}

		CancelInvoke ();
		StopAllCoroutines ();

		//Fast finish character movement
		for (int i = 0; i < movingCharacters.Count; i++) {
			movingCharacters [i].transform.localPosition = movingCharactersFinalPos [i];
			movingCharacters [i].GetComponent<FacingCamera> ().enabled = true;

			if (possibleConvStates [convStateIndex].stateAnimations.ContainsKey (CharacterName.ERIC)) {
				ericObj.GetComponent<Animator> ().SetTrigger (IDLE_ANIMATION_NAME);
			}

			if (possibleConvStates [convStateIndex].stateAnimations.ContainsKey (CharacterName.DAVID) || possibleConvStates [convStateIndex].stateAnimations.ContainsKey (CharacterName.DAVID_END)) {
				davidObj.GetComponent<Animator> ().SetTrigger (IDLE_ANIMATION_NAME);
			}
		}

		movingCharacters.Clear ();
		movingCharactersFinalPos.Clear ();
	}

	private void OnAfterStateChange ()
	{
		//Set Communicator Text
		if (possibleConvStates [convStateIndex].currInvestorState >= -1)
		{
			davidObj.SetActive (true);

			if (!possibleConvStates [convStateIndex].stateAnimations.ContainsKey (CharacterName.ERIC)) {
				InvestorCommunicator.Instance.gameObject.SetActive (true);
			} else {
				InvestorCommunicator.Instance.gameObject.SetActive (false);
			}
		} else {
			InvestorCommunicator.Instance.gameObject.SetActive (false);
		}

		//----------Audio part-----------
		AudioSource audioEric = ericObj.GetComponent<AudioSource> ();
		AudioSource audioInvestor = davidObj.GetComponent<AudioSource> ();
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
		} 
		else if (audioInvestorIndex >= 0 && !possibleConvStates [convStateIndex].stateAnimations.ContainsKey(CharacterName.DAVID_END)) {
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

		//Conversation RESET
		if (!Application.isEditor) {
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
			davidObj.GetComponent<Animator> ().SetTrigger (possibleConvStates [convStateIndex].stateAnimations [CharacterName.DAVID]);
		}

		if (possibleConvStates [convStateIndex].stateAnimations.ContainsKey (CharacterName.DAVID_FROM_METHOD1)) {
			davidObj.GetComponent<Animator> ().SetTrigger (possibleConvStates [convStateIndex].stateAnimations [GetFinalResultAnimKey()]);
		}
		//--------------------------------------

		//----------Character Movement----------
		if (possibleConvStates [convStateIndex].characterMovingState.Count > 0) {
			InitMovement();
		}
		//--------------------------------------
	}
}
