using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
	// public Rigidbody2D rb;
	// float playerMinY = 8.7f; // Side where paddle moves
	// float playerMaxY = 17.5f; // Side where paddle moves
	public float playerMaxSpeed = 0f; // Paddle speed	
	public GameObject pole;		
	public int score = 0;

	Vector2 hjStartPosition;
	Quaternion hjStartRotation;
	Vector2 poleStartPosition;
	Quaternion poleStartRotation;
	Vector2 cartStartPosition;
	
	HingeJoint2D hjStart;
	Vector3 position;	
 
/*  void OnCollisionEnter2D(Collision2D col)
 {	 
    Debug.Log("OnCollisionEnter2D");
	 	if(col.gameObject.tag == "backwall")
	 	playerMaxSpeed = 0;
 } */

	void Start()
	{
		position = pole.GetComponent<HingeJoint2D>().transform.position;

		Debug.Log("HINGE POSITION = " + pole.GetComponent<HingeJoint2D>().transform.position);
		hjStartPosition = pole.GetComponent<HingeJoint2D>().transform.position;
		hjStartRotation = pole.GetComponent<HingeJoint2D>().transform.rotation;


		poleStartPosition = pole.transform.position;	
		poleStartRotation = pole.transform.rotation;	
		cartStartPosition = this.transform.position;	
	}
 
	void Update()
	{
		if(pole.GetComponent<PoleState>().dropped) ResetPole();	
		if(Input.GetKeyDown("space"))	ResetPole();			

		float translation = playerMaxSpeed * Input.GetAxis("Horizontal") * Time.deltaTime;			
		transform.Translate(translation, 0f, 0f);	 
	}

	private void ResetPole()
	{					
		pole.GetComponent<HingeJoint2D>().transform.position = hjStartPosition;		
		pole.GetComponent<HingeJoint2D>().transform.rotation = hjStartRotation;		
		
		this.transform.position = cartStartPosition;
		pole.transform.position = poleStartPosition;
		pole.transform.rotation = poleStartRotation;

		pole.GetComponent<PoleState>().dropped = false;

		this.GetComponent<Rigidbody2D>().angularVelocity = 0f;
		pole.GetComponent<Rigidbody2D>().angularVelocity = 0f;

		this.GetComponent<Rigidbody2D>().velocity = new Vector2(0f, 0f);
		pole.GetComponent<Rigidbody2D>().velocity = new Vector2(0f, 0f);
	}

}
