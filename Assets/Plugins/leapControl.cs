using UnityEngine;
using System.Collections;
using Leap;

public class leapControl : MonoBehaviour {
	Controller leap;
	GameObject obj;
	Frame frame;
	// Use this for initialization
	void Start () {
		leap = new Controller();
		obj = GameObject.Find("OBJ");
	}
	
	Vector3 fazVector3(Hand vetorMao) {
		Vector3 vector3 = new Vector3(
		vetorMao.PalmPosition.x, 
		vetorMao.PalmPosition.y, 
		vetorMao.PalmPosition.z);
		return vector3;
	}
	
	// Update is called once per frame
	void Update () {
		frame = leap.Frame();
		if(frame.Hands.Count>=2){
			Hand rightHandNow = frame.Hands[0];
			Hand leftHandNow = frame.Hands[1];
			Hand rightHandLast = leap.Frame(1).Hands[0];
			Hand leftHandLast = leap.Frame(1).Hands[1];
			Vector3 lastVec = new Vector3( rightHandLast.PalmPosition.x - leftHandLast.PalmPosition.x, rightHandLast.PalmPosition.y - leftHandLast.PalmPosition.y, leftHandLast.PalmPosition.z - rightHandLast.PalmPosition.z);
			Vector3 currVec = new Vector3( rightHandNow.PalmPosition.x - leftHandNow.PalmPosition.x, rightHandNow.PalmPosition.y - leftHandNow.PalmPosition.y, leftHandNow.PalmPosition.z - rightHandNow.PalmPosition.z );
			if(lastVec != currVec){
				Vector3 axis = Vector3.Cross(currVec, lastVec);
				float lastDist = lastVec.magnitude;
				float currDist = currVec.magnitude;
				float axisDist = axis.magnitude;
				float angle = -Mathf.Asin(axisDist / (lastDist*currDist));
				//Passa os valores calculados para o objeto
				if(leftHandNow.Fingers.Count == 0 && rightHandNow.Fingers.Count == 0){
					obj.transform.RotateAround(axis/axisDist, angle);
				}
			}
		}
		
		/*
		 * Andrey Roth Ehrenberg
		 * 12/05/2013
		 * Trecho para os dedos:
		 * Ainda muito instável
		 * 
		 */
		if (frame.Hands.Count == 1)
		{
			if(frame.Hands[0].Fingers.Count >= 2)
			{
				Hand hand = frame.Hands[0];
				Finger rightFingerNow = hand.Fingers[0];
				Finger leftFingerNow = hand.Fingers[1];
				Finger rightFingerLast = leap.Frame(1).Hands[0].Fingers[0];
				Finger leftFingerLast = leap.Frame(1).Hands[0].Fingers[1];
				Vector3 lastVec = new Vector3(rightFingerLast.TipPosition.x - leftFingerLast.TipPosition.x, rightFingerLast.TipPosition.y - leftFingerLast.TipPosition.y, leftFingerLast.TipPosition.z - rightFingerLast.TipPosition.z);
				Vector3 currVec = new Vector3(rightFingerNow.TipPosition.x - leftFingerNow.TipPosition.x, rightFingerNow.TipPosition.y - leftFingerNow.TipPosition.y, leftFingerNow.TipPosition.z - rightFingerNow.TipPosition.z);
				if(lastVec != currVec){
					Vector3 axis = Vector3.Cross(currVec, lastVec);
					float lastDist = lastVec.magnitude;
					float currDist = currVec.magnitude;
					float axisDist = axis.magnitude;
					float angle = -Mathf.Asin(axisDist / (lastDist*currDist));
					obj.transform.RotateAround(axis/axisDist, angle);
				}
			}
		}
	}
}
