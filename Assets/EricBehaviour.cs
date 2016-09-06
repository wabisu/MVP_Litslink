using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class EricBehaviour : MonoBehaviour {
	public Sprite ericText1;
	public Sprite ericText2;
	public Sprite ericText3;
	public Sprite ericText4;
	public Sprite ericText5;
	public Sprite investorTalking;
	public Sprite youTalking;

	public Image textNavigationImage;

	private int currState = 0;
	private Vector3 nextPossibleStates = new Vector3 ();

	public GameObject communicatorObject;

	// Use this for initialization
	void Start () {
		GoState (1);
	}
	
	public void GoState (int stateIndex)
	{
		currState = stateIndex;
		OnStateChanged ();
	}

	public void GoNextState ()
	{
		currState++;

		if (currState > 7) {
			currState = 1;
		}

		OnStateChanged ();
	}

	private void OnStateChanged ()
	{
		switch (currState) {
		case 1:
			OnState1 ();
			break;
		case 2:
			OnState2 ();
			break;
		case 3:
			OnState3 ();
			break;
		case 4:
			OnInvestorTalking ();
			break;
		case 5:
			OnYouTalking ();
			break;
		case 6:
			OnState6 ();
			break;
		case 7:
			OnState7 ();
			break;
		}		
	}

	public void OnState1()
	{
		textNavigationImage.sprite = ericText1;
		nextPossibleStates.x = 2;
	}

	public void OnState2()
	{
		textNavigationImage.sprite = ericText2;
		nextPossibleStates.x = 3;
	}

	public void OnState3()
	{
		textNavigationImage.sprite = ericText3;
		nextPossibleStates.x = 2;
		nextPossibleStates.y = 4;
	}

	public void OnInvestorTalking()
	{
		textNavigationImage.sprite = investorTalking;
	}

	public void OnYouTalking()
	{
		communicatorObject.SetActive (true);
		textNavigationImage.sprite = youTalking;
	}

	public void OnState6()
	{
		communicatorObject.SetActive (false);
		textNavigationImage.sprite = ericText4;
	}

	public void OnState7()
	{
		textNavigationImage.sprite = ericText5;
	}

	/// <summary>
	/// Called when our object is selected.  Generally called by
	/// a gesture management component.
	/// </summary>
	public void OnSelect()
	{
		GoNextState ();
	}
}
