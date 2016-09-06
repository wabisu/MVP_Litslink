using HoloToolkit;
using System.Collections;
using UnityEngine;

/// <summary>
/// This keeps track of the various parts of the recording and text display process.
/// </summary>

[RequireComponent(typeof(AudioSource), typeof(MicrophoneManager))]
public class Communicator : MonoBehaviour
{
    [Tooltip("The sound to be played when the recording session starts.")]
    public AudioClip StartListeningSound;
    [Tooltip("The sound to be played when the recording session ends.")]
    public AudioClip StopListeningSound;

    [Tooltip("The icon to be displayed while recording is happening.")]
    public GameObject MicIcon;

    [Tooltip("A message to help the user understand what to do next.")]
    public Renderer MessageUIRenderer;

    [Tooltip("The waveform animation to be played while the microphone is recording.")]
    public Transform Waveform;
    [Tooltip("The meter animation to be played while the microphone is recording.")]
    public MovieTexturePlayer SoundMeter;

    private AudioSource dictationAudio;
    private AudioSource startAudio;
    private AudioSource stopAudio;

    private float origLocalScale;
    private bool animateWaveform;

    public enum Message
    {
        PressMic,
        PressStop,
        SendMessage
    };

    private MicrophoneManager microphoneManager;

	void Awake ()
	{
		dictationAudio = gameObject.GetComponent<AudioSource>();

		startAudio = gameObject.AddComponent<AudioSource>();
		stopAudio = gameObject.AddComponent<AudioSource>();

		startAudio.playOnAwake = false;
		startAudio.clip = StartListeningSound;
		stopAudio.playOnAwake = false;
		stopAudio.clip = StopListeningSound;

		microphoneManager = GetComponent<MicrophoneManager>();

		origLocalScale = Waveform.localScale.y;
		animateWaveform = false;
	}

    void Update()
    {
        if (animateWaveform)
        {
            Vector3 newScale = Waveform.localScale;
            newScale.y = Mathf.Sin(Time.time * 2.0f) * origLocalScale;
            Waveform.localScale = newScale;
        }
    }

	void OnEnable ()
    {
        // Turn the microphone on, which returns the recorded audio.
        dictationAudio.clip = microphoneManager.StartRecording();

        // Set proper UI state and play a sound.
        SetUI(true, Message.PressStop, startAudio);
    }

	void OnDisable ()
    {
        // Turn off the microphone.
        microphoneManager.StopRecording();

        // Set proper UI state and play a sound.
        SetUI(false, Message.SendMessage, stopAudio);
    }

    void ResetAfterTimeout()
    {
        // Set proper UI state and play a sound.
        SetUI(false, Message.PressMic, stopAudio);
		gameObject.SetActive (false);
		gameObject.SetActive (true);
    }

    private void SetUI(bool enabled, Message newMessage, AudioSource soundToPlay)
    {
        animateWaveform = enabled;
        SoundMeter.gameObject.SetActive(enabled);
        MicIcon.SetActive(enabled);

        StartCoroutine(ChangeLabel(newMessage));

        soundToPlay.Play();
    }

    private IEnumerator ChangeLabel(Message newMessage)
    {
        switch (newMessage)
        {
            case Message.PressMic:
                for (float i = 0.0f; i < 1.0f; i += 0.1f)
                {
                    MessageUIRenderer.material.SetFloat("_BlendTex01", Mathf.Lerp(1.0f, 0.0f, i));
                    yield return null;
                }
                break;
            case Message.PressStop:
                for (float i = 0.0f; i < 1.0f; i += 0.1f)
                {
                    MessageUIRenderer.material.SetFloat("_BlendTex01", Mathf.Lerp(0.0f, 1.0f, i));
                    yield return null;
                }
                break;
            case Message.SendMessage:
                for (float i = 0.0f; i < 1.0f; i += 0.1f)
                {
                    MessageUIRenderer.material.SetFloat("_BlendTex02", Mathf.Lerp(0.0f, 1.0f, i));
                    yield return null;
                }
                break;
        }
    }
}
