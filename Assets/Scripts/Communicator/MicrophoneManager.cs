using HoloToolkit;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows.Speech;

public class MicrophoneManager : MonoBehaviour
{
    private DictationRecognizer dictationRecognizer;

    // Use this string to cache the text currently displayed in the text box.
	private StringBuilder textSaid;

    // Using an empty string specifies the default microphone. 
    private static string deviceName = string.Empty;
    private int samplingRate;
    private const int messageLength = 15;

	private KeywordManager keyWordManager;

	private bool isUserTalking = false;

    void Awake()
    {
		keyWordManager = GetComponent<KeywordManager> ();

        /* TODO: DEVELOPER CODING EXERCISE 3.a */

        // 3.a: Create a new DictationRecognizer and assign it to dictationRecognizer variable.
        dictationRecognizer = new DictationRecognizer();

        // 3.a: Register for dictationRecognizer.DictationHypothesis and implement DictationHypothesis below
        // This event is fired while the user is talking. As the recognizer listens, it provides text of what it's heard so far.
        dictationRecognizer.DictationHypothesis += DictationRecognizer_DictationHypothesis;

        // 3.a: Register for dictationRecognizer.DictationResult and implement DictationResult below
        // This event is fired after the user pauses, typically at the end of a sentence. The full recognized string is returned here.
        dictationRecognizer.DictationResult += DictationRecognizer_DictationResult;

        // 3.a: Register for dictationRecognizer.DictationComplete and implement DictationComplete below
        // This event is fired when the recognizer stops, whether from Stop() being called, a timeout occurring, or some other error.
        dictationRecognizer.DictationComplete += DictationRecognizer_DictationComplete;

        // 3.a: Register for dictationRecognizer.DictationError and implement DictationError below
        // This event is fired when an error occurs.
        dictationRecognizer.DictationError += DictationRecognizer_DictationError;

        // Query the maximum frequency of the default microphone. Use 'unused' to ignore the minimum frequency.
        int unused;
        Microphone.GetDeviceCaps(deviceName, out unused, out samplingRate);

        // Use this string to cache the text currently displayed in the text box.
		textSaid = new StringBuilder();
    }

	public bool IsDictationRunning ()
	{
		return dictationRecognizer != null && dictationRecognizer.Status == SpeechSystemStatus.Running;
	}

	public bool IsUserTalking ()
	{
		return isUserTalking;
	}

    /// <summary>
    /// Turns on the dictation recognizer and begins recording audio from the default microphone.
    /// </summary>
    /// <returns>The audio clip recorded from the microphone.</returns>
    public AudioClip StartRecording()
    {
        // 3.a Shutdown the PhraseRecognitionSystem. This controls the KeywordRecognizers
		keyWordManager.StopKeywordRecognizer();
        PhraseRecognitionSystem.Shutdown();

        // 3.a: Start dictationRecognizer
        dictationRecognizer.Start();

		// Start recording from the microphone for messageLength seconds.
        return Microphone.Start(deviceName, false, messageLength, samplingRate);
    }

    /// <summary>
    /// Ends the recording session.
    /// </summary>
    public void StopRecording()
    {
        // 3.a: Check if dictationRecognizer.Status is Running and stop it if so
        if (dictationRecognizer.Status == SpeechSystemStatus.Running)
        {
            dictationRecognizer.Stop();
        }

        Microphone.End(deviceName);
    }

    /// <summary>
    /// This event is fired while the user is talking. As the recognizer listens, it provides text of what it's heard so far.
    /// </summary>
    /// <param name="text">The currently hypothesized recognition.</param>
    private void DictationRecognizer_DictationHypothesis(string text)
    {
		isUserTalking = true;

		textSaid.Remove (0, textSaid.Length);

		textSaid.Append (text);
		textSaid.Append ("...");

        // 3.a: Set DictationDisplay text to be textSoFar and new hypothesized text
        // We don't want to append to textSoFar yet, because the hypothesis may have changed on the next event
		InvestorCommunicator.Instance.SetTextSaid(textSaid.ToString());
    }

    /// <summary>
    /// This event is fired after the user pauses, typically at the end of a sentence. The full recognized string is returned here.
    /// </summary>
    /// <param name="text">The text that was heard by the recognizer.</param>
    /// <param name="confidence">A representation of how confident (rejected, low, medium, high) the recognizer is of this recognition.</param>
    private void DictationRecognizer_DictationResult(string text, ConfidenceLevel confidence)
    {
		// 3.a: Set DictationDisplay text to be textSoFar
		InvestorCommunicator.Instance.SetAndVirefyTextSaid(text);
		isUserTalking = false;
    }

    /// <summary>
    /// This event is fired when the recognizer stops, whether from Stop() being called, a timeout occurring, or some other error.
    /// Typically, this will simply return "Complete". In this case, we check to see if the recognizer timed out.
    /// </summary>
    /// <param name="cause">An enumerated reason for the session completing.</param>
    private void DictationRecognizer_DictationComplete(DictationCompletionCause cause)
    {
        // If Timeout occurs, the user has been silent for too long.
        // With dictation, the default timeout after a recognition is 20 seconds.
        // The default timeout with initial silence is 5 seconds.
		if (cause == DictationCompletionCause.TimeoutExceeded) {
			InvestorCommunicator.Instance.ResetAfterTimeout ();
		}
    }

    /// <summary>
    /// This event is fired when an error occurs.
    /// </summary>
    /// <param name="error">The string representation of the error reason.</param>
    /// <param name="hresult">The int representation of the hresult.</param>
    private void DictationRecognizer_DictationError(string error, int hresult)
    {
        // 3.a: Set DictationDisplay text to be the error string
		InvestorCommunicator.Instance.ResetAfterTimeout ();
    }

	private IEnumerator RestartSpeechSystem()
	{
		while (dictationRecognizer != null && dictationRecognizer.Status == SpeechSystemStatus.Running)
		{
			yield return null;
		}

		keyWordManager.StartKeywordRecognizer();
	}
}
