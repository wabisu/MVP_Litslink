﻿using HoloToolkit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// This keeps track of the various parts of the recording and text display process.
/// </summary>

[RequireComponent(typeof(AudioSource))]
public class InvestorCommunicator : MonoBehaviour
{
    [Tooltip("The sound to be played when the recording session starts.")]
    public AudioClip StartListeningSound;
    [Tooltip("The sound to be played when the recording session ends.")]
    public AudioClip StopListeningSound;

	public Text textToSayLabel;
	public Text textSaidLabel;
	public Text textInvestorLabel;

    private AudioSource dictationAudio;
    private AudioSource startAudio;
    private AudioSource stopAudio;

	public EricBehaviour ericScript;
	public MicrophoneManager microphoneManager;

	private List<string> userSentences = new List<string> ();
	private const string DICTATION_START_TXT = "Dictation is starting. It may take time to display your text the first time, but begin speaking now...";
	private const string INVESTOR_TALKING_TXT = "Investor talking";

	void Awake ()
	{
		//ToDo Create scenario FROM file loading mechanism
		userSentences.Add("We build AR communication training tools.");
		userSentences.Add("Our holographic simulator helps you practice all forms of communication in different languages.");
		userSentences.Add("Yes, so for example");
		userSentences.Add("A Japanese business-woman can have couple scary-looking hardcore American clients popping up in front of her to practice English presentation as a challenge.");
		userSentences.Add("We also incorporate API which can detect user's emotions based on the way he or she talks to have human holograms respond accordingly.");
		userSentences.Add("Yes, you are actually doing it right now.");

		//userSentences.Add("One.");
		//userSentences.Add("Two.");
		//userSentences.Add("Three");
		//userSentences.Add("four");
		//userSentences.Add("Five");
		//userSentences.Add("Six.");
		//-------------------------------------------------

		dictationAudio = gameObject.GetComponent<AudioSource>();

		startAudio = gameObject.AddComponent<AudioSource>();
		stopAudio = gameObject.AddComponent<AudioSource>();

		startAudio.playOnAwake = false;
		startAudio.clip = StartListeningSound;
		stopAudio.playOnAwake = false;
		stopAudio.clip = StopListeningSound;
	}

	public void StartConversation()
	{
		textInvestorLabel.gameObject.SetActive (false);
		textToSayLabel.gameObject.SetActive (true);
		textSaidLabel.gameObject.SetActive (true);

		// Turn the microphone on, which returns the recorded audio.
		textToSayLabel.text = userSentences [ericScript.GetCurrDictationState()];
		textSaidLabel.text = DICTATION_START_TXT;

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

		if (ericScript.GetCurrDictationState() == -2) {
			microphoneManager.StartCoroutine ("RestartSpeechSystem");
		} else if (ericScript.GetCurrDictationState() == -1) {
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

	public void SetTextSaid (string textWasSaid)
	{
		textSaidLabel.text = textWasSaid;
	}

	private string FixSentenceToCompare (string strToFix)
	{
		string res = strToFix.Replace (".", "");
		res = res.Replace (",", "");
		res = res.Replace ("!", "");
		res = res.Replace ("?", "");
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

	public void RestartSpeechSystem ()
	{
		microphoneManager.StartCoroutine ("RestartSpeechSystem");	
	}

	public void SetAndVirefyTextSaid (string textWasSaid)
	{
		textSaidLabel.text = textWasSaid;

		if (FixSentenceToCompare (userSentences [ericScript.GetCurrDictationState()]).Equals (FixSentenceToCompare (textWasSaid))) {
			ericScript.OnCurrSentenceSaid ();

			if (ericScript.GetCurrDictationState() > 0) {
				textToSayLabel.text = userSentences [ericScript.GetCurrDictationState()];
				textSaidLabel.text = "";
			}
		}
	}
}
