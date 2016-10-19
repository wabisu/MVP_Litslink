using HoloToolkit;

using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows.Speech;
using UnityEngine.Networking;

using System.Reflection;
using System;

using HoloToolkit.Unity;

public class MoodsRecognizer : Singleton<MoodsRecognizer>
{
    // Using an empty string specifies the default microphone. 
	private string deviceName = string.Empty;
	private const int samplingRate = 8000;
    private const int messageLength = 20;

	private const string TOKEN_URL = "https://token.beyondverbal.com/token";
	private const string START_URL = "https://apiv3.beyondverbal.com/v1/recording/start";
	private const string SEND_DATA_URL = "https://apiv3.beyondverbal.com/v3/recording/";
	private const string API_KEY = "095913ac-d5f1-4097-bfd3-879e0fa59ec2";

	private const string ATTITUDE_TEMPER = "Temper";
	private const string ATTITUDE_VALENCE = "Valence";
	private const string ATTITUDE_AROUSAL = "Arousal";

	private const string MOOD_GROUP11 = "Group11";
	private const string MOOD_COMPOSITE = "Composite";
	private const string MOOD_PRIMARY = "Primary";
	private const string MOOD_SECONDARY = "Secondary";

	private const string MOOD_NONE = "none";

	private const string JSON_FIELD_TOKEN = "access_token";
	private const string JSON_FIELD_RECORDING_ID = "recordingId";

	private string token;
	private string recordingId;
	private JSONObject receivedJSON = null;

	//--------------------UI Fields-----------------------
	public Text primaryCompositeTxt;
	public Text secondaryCompositeTxt;
	public Text primaryGroup11Txt;

	public Text arousalEnergyTxt;
	public Text temperTxt;
	public Text valenceAttitudeTxt;
	//----------------------------------------------------

	private Text debug;
	private Text recordingCountdown;
	public Text debugForNone;

	private UnityWebRequest tokenRequest;
	private UnityWebRequest recordIdRequest;
	private UnityWebRequest dataSendRequest;

	private AudioClip audioToSend;

	#region Moods definition and parsing methods
	private enum MoodGroups 
	{ 
		NONE,
		SUPREMACY_ARROGANCE, 
		HOSTILITY_ANGER,
		CRITICISM_CYNICISM, 
		SELF_CONTROL_PRACTICALITY, 
		LEADERSHIP_CHARISMA, 
		CREATIVENESS_PASSION, 
		FRIENDLINESS_WARM,  
		LOVE_HAPPINESS, 
		LONELINESS_UNFULFILLMENT, 
		SADNESS_SORROW, 
		DEFENSIVENESS_ANXIETY 
	};

	//ToDo - is this the only way that works for UWP?
	private string GetEnumDescription(MoodGroups value)
	{
		switch (value) {
		case MoodGroups.SUPREMACY_ARROGANCE:
			return "Supremacy, Arrogance";
		case MoodGroups.HOSTILITY_ANGER:
			return "Hostility, Anger";
		case MoodGroups.CRITICISM_CYNICISM:
			return "Criticism, Cynicism";
		case MoodGroups.SELF_CONTROL_PRACTICALITY:
			return "Self-Control, Practicality";
		case MoodGroups.LEADERSHIP_CHARISMA:
			return "Leadership, Charisma";
		case MoodGroups.CREATIVENESS_PASSION:
			return "Creative, Passionate";
		case MoodGroups.FRIENDLINESS_WARM:
			return "Friendliness, Warm";
		case MoodGroups.LOVE_HAPPINESS:
			return "Love, Happiness";
		case MoodGroups.LONELINESS_UNFULFILLMENT:
			return "Loneliness, Unfulfillment";
		case MoodGroups.SADNESS_SORROW:
			return "Sadness, Sorrow";
		case MoodGroups.DEFENSIVENESS_ANXIETY:
			return "Defensivness, Anxiety";
		}

		return MOOD_NONE;
	}

	private static IEnumerable<T> EnumToList<T>()
	{
		Type enumType = typeof(T);

		//ToDo need to rewrite this code to work for UWP for safety check
		// Can't use generic type constraints on value types,
		// so have to do check like this
		/*if (!enumType.GetTy .IsEnum)
			throw new ArgumentException("T must be of type System.Enum");*/

		Array enumValArray = Enum.GetValues(enumType);
		List<T> enumValList = new List<T>(enumValArray.Length);

		foreach (int val in enumValArray)
		{
			enumValList.Add((T)Enum.Parse(enumType, val.ToString()));
		}

		return enumValList;
	}

	private string GetJSONField (string fieldName, string jsonStr)
	{
		JSONObject json = new JSONObject (jsonStr);
		JSONObject jsonElem = json.GetField (fieldName);

		if (jsonElem == null) {
			return null;
		}

		return jsonElem.str;
	}

	private float GetAttitude (string attType)
	{
		if (receivedJSON == null)
			return -1;

		JSONObject jsonElem = receivedJSON.GetField ("result")
			.GetField ("analysisSegments")[0].GetField ("analysis")
			.GetField (attType).GetField ("Value");

		if (jsonElem == null) {
			return -1;
		}

		return float.Parse(jsonElem.str);
	}

	private MoodGroups GetMoodGroup11 (string moodType)
	{
		if (receivedJSON == null)
			return MoodGroups.NONE;

		JSONObject jsonElem = receivedJSON.GetField ("result").GetField ("analysisSegments") [0]
			.GetField ("analysis").GetField ("Mood").GetField (MOOD_GROUP11)
			.GetField (moodType).GetField ("Phrase");

		if (jsonElem == null) {
			return MoodGroups.NONE;
		}

		foreach (MoodGroups mood in EnumToList<MoodGroups>()) {
			if (GetEnumDescription (mood).Equals (jsonElem.str)) {
				return mood;
			}
		}

		debugForNone.text = "error with - " + jsonElem.str;
		return MoodGroups.NONE;
	}

	private string GetMoodComposite (string moodType)
	{
		if (receivedJSON == null)
			return MOOD_NONE;

		JSONObject jsonElem = receivedJSON.GetField ("result").GetField ("analysisSegments") [0]
			.GetField ("analysis").GetField ("Mood").GetField (MOOD_COMPOSITE)
			.GetField (moodType).GetField ("Phrase");

		if (jsonElem == null) {
			return MOOD_NONE;
		}

		return jsonElem.str;
	}
	#endregion

	#region send DATA to Rest API
	private UnityWebRequest CreateUnityWebRequest(string url, byte[] param) {
		UnityWebRequest requestU= new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
		UploadHandlerRaw uH= new UploadHandlerRaw(param);
		requestU.uploadHandler= uH;
		DownloadHandlerBuffer dH= new DownloadHandlerBuffer();
		requestU.downloadHandler= dH; //need a download handler so that I can read response data
		return requestU;
	}

	private IEnumerator SendVerbalData() {
		debug.text = "Obtain Token...";
		Debug.Log ("Obtain Token...");

		//Get TOKEN from API-KEY
		if (String.IsNullOrEmpty (token)) {
			using (tokenRequest = CreateUnityWebRequest (TOKEN_URL, Encoding.UTF8.GetBytes ("apiKey=" + API_KEY + "&grant_type=client_credentials"))) {
				yield return tokenRequest.Send ();

				if (tokenRequest.isError) {
					debug.text = tokenRequest.error;
					Debug.Log (tokenRequest.error);
					yield break;
				} else {
					token = GetJSONField (JSON_FIELD_TOKEN, tokenRequest.downloadHandler.text);

					if (String.IsNullOrEmpty (token)) {
						debug.text = "Token isNullOrEmpty";
						Debug.Log (tokenRequest.downloadHandler.text);
						yield break;
					}
				}
			}
		}

		Debug.Log ("Obtain Recording Id...");
		debug.text = "Obtain Recording Id...";

		//Create new session and Get it's ID
		using (recordIdRequest = CreateUnityWebRequest (START_URL, Encoding.UTF8.GetBytes ("{ dataFormat: { type: \"WAV\" } }"))) {
			recordIdRequest.SetRequestHeader ("Authorization", "Bearer " + token);
			yield return recordIdRequest.Send ();

			if (recordIdRequest.isError) {
				debug.text = recordIdRequest.error;
				Debug.Log (recordIdRequest.error);
				yield break;
			} else {
				recordingId = GetJSONField (JSON_FIELD_RECORDING_ID, recordIdRequest.downloadHandler.text);

				if (String.IsNullOrEmpty (recordingId)) {
					debug.text = "RecordingId isNullOrEmpty";
					Debug.Log (recordIdRequest.downloadHandler.text);
					yield break;
				}
			}
		}

		Debug.Log ("Sending and waiting response...");
		debug.text = "Sending and waiting response...";

		using (dataSendRequest = CreateUnityWebRequest (SEND_DATA_URL + recordingId, WavToBytes.GetByteArray (audioToSend))) {
			dataSendRequest.SetRequestHeader ("Authorization", "Bearer " + token);
			yield return dataSendRequest.Send();

			if(dataSendRequest.isError) {
				debug.text = dataSendRequest.error;
				Debug.Log(dataSendRequest.error);
				yield break;
			}
			else {
				receivedJSON = new JSONObject (dataSendRequest.downloadHandler.text);
				debug.text = "Parsing JSON...";
				Debug.Log (receivedJSON.Print(true));

				if (receivedJSON != null) {
					JSONObject status = receivedJSON.GetField ("status");

					if (status.str.Equals ("success") && receivedJSON.GetField ("result").GetField ("analysisSegments") == null) {
						primaryCompositeTxt.text = "Too much silence";
						secondaryCompositeTxt.text = "";
						primaryGroup11Txt.text = "";
						arousalEnergyTxt.text = "";
						temperTxt.text = "";
						valenceAttitudeTxt.text = "";					
					} else if (status.str.Equals ("success")) {
						float temper = GetAttitude (ATTITUDE_TEMPER);
						float valence = GetAttitude (ATTITUDE_VALENCE);
						float arousal = GetAttitude (ATTITUDE_AROUSAL);
						MoodGroups primaryMood = GetMoodGroup11 (MOOD_PRIMARY);
						MoodGroups secondaryMood = GetMoodGroup11 (MOOD_SECONDARY);
						string primaryCompositeMood = GetMoodComposite (MOOD_PRIMARY);
						string secondaryCompositeMood = GetMoodComposite (MOOD_SECONDARY);

						primaryCompositeTxt.text = primaryCompositeMood;
						secondaryCompositeTxt.text = secondaryCompositeMood;
						primaryGroup11Txt.text = GetEnumDescription (primaryMood);

						arousalEnergyTxt.text = "Arousal = " + arousal;
						temperTxt.text = "Temper = " + temper;
						valenceAttitudeTxt.text = "Attitude = " + valence;
					} else {
						debug.text = "Response status failed";
						Debug.Log ("Response status failed");
					}
				} else {
					debug.text = "JSON is Null";
					Debug.Log ("JSON is Null");
				}
			}
		}

		audioToSend = null;
	}
	#endregion

	void Awake ()
	{
		debug = GameObject.Find ("debug").GetComponent<Text> ();
		recordingCountdown = GameObject.Find ("RecordingCountdown").GetComponent<Text> ();
		MoodsRecognizer.Instance.gameObject.SetActive (false);
	}

	void OnEnable() 
	{
		primaryCompositeTxt.text = "";
		secondaryCompositeTxt.text = "";
		primaryGroup11Txt.text = "";
		arousalEnergyTxt.text = "";
		temperTxt.text = "";
		valenceAttitudeTxt.text = "";

		GetComponent<AudioSource> ().clip = null;
		StartRecording ();
	}

	void OnDisable ()
	{
		if (Microphone.IsRecording(deviceName))
			Microphone.End(deviceName);

		recordingCountdown.text = "";
		Debug.Log ("Stopped.");
		debug.text = "Stopped.";
	}

	/// <summary>
	/// Turns on the dictation recognizer and begins recording audio from the default microphone.
	/// </summary>
	/// <returns>The audio clip recorded from the microphone.</returns>
	private  void StartRecording()
	{
		// Start recording from the microphone for messageLength seconds.
		GetComponent<AudioSource> ().clip = Microphone.Start(deviceName, false, messageLength, samplingRate);
		StartCoroutine (SendWavCoroutine ());
	}

	private IEnumerator SendWavCoroutine ()
	{
		Debug.Log ("Recording...");
		debug.text = "Recording...";

		for (int i = 1; i <= messageLength; i++) {
			yield return new WaitForSeconds (1);
			recordingCountdown.text = "Recording " + i + " seconds";
		}

		Microphone.End (deviceName);

		audioToSend = GetComponent<AudioSource> ().clip;
		yield return new WaitForSeconds (0.5f);
		StartRecording ();

		Debug.Log ("Sending...");
		debug.text = "Sending...";
		yield return SendVerbalData ();
		Debug.Log ("Done.");
		debug.text = "Done.";
	}
}
