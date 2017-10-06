using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DIstanceCalculation : MonoBehaviour {

	GameObject cam;
	GameObject[] obj;
	Vector3 currentPos;
	Vector3 nowPos;


	void Start () {
		//plare character, assuming first person view therefore they should have MainCamera
		cam = GameObject.Find("Main Camera");//.FindGameObjectWithTag ("MainCamera");
		//place all sound emitting objects in scene into an array
		obj = GameObject.FindGameObjectsWithTag("Sound");//.FindGameObjectsWithTag("Sound");
	}

	IEnumerator waitPlz () {
		currentPos = cam.transform.position;
		yield return new WaitForSeconds (1f);
		nowPos = cam.transform.position;
	}
	
	// Update is called once per frame
	void Update () {
		//Vector3 currentPos = cam.transform.position;
		//yield return new WaitForSeconds (1f);
		//Vector3 nowPos = cam.transform.position;
		StartCoroutine(waitPlz());
		if (currentPos != nowPos) {
			for (int k = 0; k < obj.Length; k++) {
				Debug.Log(Vector3.Distance(cam.transform.position, obj[k].transform.position));
			}
		}
	}
}
