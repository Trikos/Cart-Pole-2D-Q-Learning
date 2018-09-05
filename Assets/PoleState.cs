using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoleState : MonoBehaviour
{
	public bool dropped = false;	

	void OnCollisionEnter2D(Collision2D col)
	{
		if(col.gameObject.tag == "drop")
		{
			dropped = true;
			Debug.Log("COLLISION");;
		}
	}
	// Use this for initialization
	void Start ()
	{
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
