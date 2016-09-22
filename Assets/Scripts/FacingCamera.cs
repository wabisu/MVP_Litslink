using UnityEngine;
using System.Collections;

public class FacingCamera : MonoBehaviour {

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void FixedUpdate () {
		Vector3 lookPos = transform.position - Camera.main.transform.position;
		lookPos.y = 0;
		transform.rotation = Quaternion.Lerp (transform.rotation, Quaternion.LookRotation(lookPos) * Quaternion.Euler(0, 180, 0), Time.deltaTime * 15);
	}
}
