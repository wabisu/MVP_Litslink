using UnityEngine;
using System.Collections;

public class OnSelectTransfer : MonoBehaviour 
{
	public GameObject objToTransfer;

	/// <summary>
	/// Called when our object is selected.  Generally called by
	/// a gesture management component.
	/// </summary>
	public void OnSelect()
	{
		objToTransfer.SendMessage("OnSelectTransfered");
	}
}
