using UnityEngine;
using System.Collections;

public class PlayBtnBehaviour : MonoBehaviour {
	public Sprite playSpr;
	public Sprite stopSpr;

	private SpriteRenderer sprRenderer;

	void Awake ()
	{
		sprRenderer = GetComponent<SpriteRenderer> ();
	}

	/// <summary>
	/// Called when our object is selected.  Generally called by
	/// a gesture management component.
	/// </summary>
	public void OnSelect()
	{
		if (sprRenderer.sprite.Equals (playSpr)) {
			sprRenderer.sprite = stopSpr;
			Invoke ("OnClipEnded", InvestorCommunicator.Instance.PlayRecordedClipPressed ());
		} else {
			OnClipEnded ();
		}
	}

	private void OnClipEnded ()
	{
		CancelInvoke ();
		InvestorCommunicator.Instance.StopRecordedClipPressed ();
		sprRenderer.sprite = playSpr;
	}
}
