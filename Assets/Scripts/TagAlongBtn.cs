using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using DG.Tweening;

public class TagAlongBtn : MonoBehaviour 
{
	public Image buttonHighlightImg;
	private Image buttonImg;
	private Color transparent = new Color(1.0f, 1.0f, 1.0f, 0.0f);

	void Awake ()
	{
		buttonImg = GetComponent<Image> ();
	}

	void OnSelect ()
	{
		if (gameObject.name.Equals("PitchingBtn"))
			GameObject.Find ("Playground").GetComponent<SceneBehaviour> ().OnPitchingPracticeTap ();
		else if (gameObject.name.Equals("EmotionalBtn"))
			GameObject.Find ("Playground").GetComponent<SceneBehaviour> ().OnEmotionalAnalysisTap ();
	}

	void OnGazeEnter ()
	{
		buttonHighlightImg.DOKill ();
		buttonHighlightImg.DOColor (Color.white, 0.25f);
		buttonHighlightImg.transform.DOKill ();
		buttonHighlightImg.transform.DOScale (1.2f, 0.25f).SetDelay (0.25f);

		buttonImg.DOKill ();
		buttonImg.DOColor (transparent, 0.25f);
	}

	void OnGazeLeave ()
	{
		buttonHighlightImg.transform.DOKill ();
		buttonHighlightImg.transform.DOScale (1.0f, 0.25f);
		buttonHighlightImg.DOKill ();
		buttonHighlightImg.DOColor (transparent, 0.25f).SetDelay (0.25f);

		buttonImg.DOKill ();
		buttonImg.DOColor (Color.white, 0.25f).SetDelay (0.25f);
	}
}
