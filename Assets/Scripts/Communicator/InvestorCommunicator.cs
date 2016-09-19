using HoloToolkit;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using HoloToolkit.Unity;

/// <summary>
/// This keeps track of the various parts of the recording and text display process.
/// </summary>

[RequireComponent(typeof(AudioSource))]
public class InvestorCommunicator : Singleton<InvestorCommunicator>
{
	public Text textToSayLabel;
	public Text textSaidLabel;
	public Text textInvestorLabel;

    private AudioSource dictationAudio;

	public SceneBehaviour sceneScript;
	public MicrophoneManager microphoneManager;

	private const string DICTATION_START_TXT = "Dictation is starting. It may take time to display your text the first time, but begin speaking now...";
	private const string INVESTOR_TALKING_TXT = "Investor talking";

	private List<string> initialSentences = new List<string> ();
	private List<string[]> initialSentencesToWords = new List<string[]> ();

	private string[] saidSentenceToWords;

	private StringBuilder userSaidStringBuilder = new StringBuilder();
	private string richTextYellowOpen = "<color=#ffff00ff>";
	private string richTextYellowClose = "</color>";		

	private float lastSentenceCorrectPercent;

	private ReplayBtnBehaviour replayBtn;
	private PlaySampleBehaviour playSampleBtn;

	public AudioClip[] samples = new AudioClip[6];

	void Awake ()
	{
		playSampleBtn = GameObject.Find ("PlaySample").GetComponent<PlaySampleBehaviour> ();
		replayBtn = GameObject.Find ("ReplayBtn").GetComponent<ReplayBtnBehaviour> ();
		replayBtn.gameObject.SetActive (false);

		//ToDo Create scenario FROM file loading mechanism
		initialSentences.Add("We build AR communication training tools.");
		initialSentences.Add("Our holographic simulator helps you practice all forms of communication in different languages.");
		initialSentences.Add("Yes, so for example");
		initialSentences.Add("A Japanese business-woman can have couple scary-looking hardcore American clients popping up in front of her to practice English presentation as a challenge.");
		initialSentences.Add("We also incorporate API which can detect user's emotions based on the way he or she talks to have human holograms respond accordingly.");
		initialSentences.Add("Yes, you are actually doing it right now.");

		foreach (string str in initialSentences)
		{
			initialSentencesToWords.Add (ParseToWords (str));
		}
		//-------------------------------------------------

		dictationAudio = gameObject.GetComponent<AudioSource>();
	}

	private string FixSentenceToCompare (string strToFix)
	{
		string res = strToFix.Replace (".", "");
		res = res.Replace (",", "");
		res = res.Replace (":", "");
		res = res.Replace (";", "");
		res = res.Replace ("!", "");
		res = res.Replace ("?", "");
		res = res.Replace ("'", "");
		res = res.Replace ("-", " ");
		res = res.ToLower ();

		res = res.Replace ("1", "one");
		res = res.Replace ("2", "two");
		res = res.Replace ("3", "three");
		res = res.Replace ("4", "four");
		res = res.Replace ("5", "five");
		res = res.Replace ("6", "six");
		res = res.Replace ("7", "seven");
		res = res.Replace ("8", "eight");
		res = res.Replace ("9", "nine");
		res = res.Replace ("0", "zero");

		return res;
	}

	private string[] ParseToWords (string str)
	{
		return FixSentenceToCompare (str).Split (' ');
	}

	public void SetTextSaid (string textWasSaid)
	{
		textSaidLabel.text = textWasSaid;
	}

	public void SetAndVirefyTextSaid (string textUserSaid)
	{
		if (!sceneScript.OnKeywordSaid (textUserSaid.ToLower ())) 
		{
			userSaidStringBuilder.Remove (0, userSaidStringBuilder.Length);
			saidSentenceToWords = ParseToWords (textUserSaid);
			bool[] correctWordsIndexes = new bool[saidSentenceToWords.Length];
			int correctWordsCount = 0;

			for (int i = 0; i < saidSentenceToWords.Length; i++) {
				for (int j = 0; j < initialSentencesToWords [sceneScript.GetCurrDictationState ()].Length; j++) {
					if (initialSentencesToWords [sceneScript.GetCurrDictationState ()] [j].Equals (saidSentenceToWords [i]) && !correctWordsIndexes[i]) {
						correctWordsIndexes [i] = true;
						correctWordsCount++;
						break;
					}
				}

				if (!correctWordsIndexes [i])
					userSaidStringBuilder.Append (richTextYellowOpen);
				userSaidStringBuilder.Append (saidSentenceToWords[i]);
				if (!correctWordsIndexes [i])
					userSaidStringBuilder.Append (richTextYellowClose);
				userSaidStringBuilder.Append (" ");
			}

			textSaidLabel.text = userSaidStringBuilder.ToString();

			lastSentenceCorrectPercent = (float)correctWordsCount / (float)initialSentencesToWords [sceneScript.GetCurrDictationState ()].Length;

			if (lastSentenceCorrectPercent >= 0.6f) {
				sceneScript.OnCurrSentenceSaid ();
				replayBtn.gameObject.SetActive (false);
			} else {
				replayBtn.gameObject.SetActive (true);
			}
		}
	}

	public string GetLastCorrectPronouncePercent ()
	{
		if (lastSentenceCorrectPercent > 0) {
			return ", Pronunciation = " + (int)(lastSentenceCorrectPercent * 100) + "%";
		}

		return "";
	}

	public bool IsRecordedAudioPlaying ()
	{
		return dictationAudio.isPlaying;
	}

	public float PlayRecordedClipPressed ()
	{
		if (IsRecordedAudioPlaying()) {
			dictationAudio.Stop ();
			playSampleBtn.OnExtraStop ();
		}

		StopConversation ();
		dictationAudio.Play ();
		return dictationAudio.clip.length;
	}

	public void StopRecordedClipPressed ()
	{
		dictationAudio.Stop ();

		if (sceneScript.GetCurrDictationState () >= 0) {
			StartConversation ();
		}
	}

	public float PlaySamplePressed ()
	{
		if (IsRecordedAudioPlaying()) {
			dictationAudio.Stop ();
			replayBtn.OnExtraStop ();
		}

		StopConversation ();
		dictationAudio.clip = samples [sceneScript.GetCurrDictationState ()];
		dictationAudio.Play ();
		return dictationAudio.clip.length;
	}

	public void StopSamplePressed ()
	{
		dictationAudio.Stop ();

		if (sceneScript.GetCurrDictationState () >= 0) {
			StartConversation ();
		}
	}

	public void StartConversation()
	{
		textInvestorLabel.gameObject.SetActive (false);
		textToSayLabel.gameObject.SetActive (true);
		textSaidLabel.gameObject.SetActive (true);

		// Turn the microphone on, which returns the recorded audio.
		textToSayLabel.text = initialSentences [sceneScript.GetCurrDictationState()];
		textSaidLabel.text = "";

		if (!microphoneManager.IsDictationRunning ()) {
			dictationAudio.clip = microphoneManager.StartRecording ();
		}
	}

	public void StopConversation ()
	{
		// Turn off the microphone.
		if (microphoneManager.IsDictationRunning ()) {
			microphoneManager.StopRecording ();
		}

		if (sceneScript.GetCurrDictationState() == -2) {
			microphoneManager.StartCoroutine ("RestartSpeechSystem");
		} else if (sceneScript.GetCurrDictationState() == -1) {
			textInvestorLabel.text = INVESTOR_TALKING_TXT;

			textInvestorLabel.gameObject.SetActive (true);
			textToSayLabel.gameObject.SetActive (false);
			textSaidLabel.gameObject.SetActive (false);
		}
	}

	public void ResetAfterTimeout()
	{
		microphoneManager.StopRecording();
		dictationAudio.clip = microphoneManager.StartRecording();
	}

	public void RestartSpeechSystem ()
	{
		microphoneManager.StartCoroutine ("RestartSpeechSystem");	
	}
}
