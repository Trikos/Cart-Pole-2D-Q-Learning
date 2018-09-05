using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class Replay
{
	public List<double> states;
	public double reward;

	public Replay(double XPosition, double PoleX, double PoleVelocity, double Reward)
	{
		states = new List<double>();
		
		//Platform Movement
		states.Add(XPosition);
		
		//Ball position
		states.Add(PoleX);

		//AngularVelocity, in 2D hasnt a direction, is just a float rather than a Vector
		states.Add(PoleVelocity);
		reward = Reward;
	}
}

public class Brain : MonoBehaviour
{
	public GameObject pole;													//object to monitor	
	ANN nn;

	float reward = 0.0f;														//memory to associate with actions
	List<Replay> replayMemory = new List<Replay>();	//memory = list of past actions rewards
	int mCapacity = 10000;													//memory capacity

	float discount = 0.99f;													//how much future states affect rewards
	float exploreRate = 100.0f;											//chance of picking random actions
	float maxExploreRate = 100.0f;									//max chance value
	float minExploreRate = 0.01f;										//min chance value
	float exploreDecay = 0.001f;										//chance decay amount for each update

	Vector3 poleStartPosition;											//record start position of object
	int failCount = 0;															//count when the ball is dropped
	public float torque = 0.0f;                     //Add torque when ball will be reset
	public float maxSpeed = 0f;													//max angle to apply to tilting each update
	/* Make sure this is large enough so that the q value multiplied by it is enough to recover
		 balance when the ball gets a good speed up */
	float timer = 0;																//timer to keep track of balancing
	float maxBalanceTime = 0;												//record time ball is kept balanced
	
//------------------------------------------------------------------------------------------
//------------------------------------------------------------------------------------------

	// Use this for initialization
	void Start ()
	{		
		nn = new ANN(3, 1, 2, 5, 0.2f);
		// nn = new ANN(3, 1, 2, 6, 0.2f); //TOTRY
		poleStartPosition = pole.transform.position;
		Debug.Log("Pole start position: " + poleStartPosition);
		Time.timeScale = 1.0f;
	}

//------------------------------------------------------------------------------------------
//------------------------------------------------------------------------------------------

		GUIStyle guiStyle = new GUIStyle();
	void OnGUI()
	{
		guiStyle.fontSize = 25;
		guiStyle.normal.textColor = Color.white;
		GUI.BeginGroup (new Rect (10, 10, 600, 150));
		GUI.Box (new Rect (0, 0, 140, 140), "Stats", guiStyle);
		GUI.Label(new Rect (10, 25, 500, 30), "Epochs: " + failCount, guiStyle);
		GUI.Label(new Rect (10, 50, 500, 30), "Chance Rate: " + exploreRate, guiStyle);
		GUI.Label(new Rect (10, 75, 500, 30), "Last Best Balance: " + maxBalanceTime, guiStyle);
		GUI.Label(new Rect (10, 100, 500, 30), "This Balance: " + timer, guiStyle);
		GUI.Label(new Rect (10, 125, 500, 30), "Speed: " + Time.timeScale, guiStyle);
		GUI.EndGroup ();
	}

//------------------------------------------------------------------------------------------
//------------------------------------------------------------------------------------------

	// Update is called once per frame
	void Update ()
	{
		if(Input.GetKeyDown("space")) ResetPole();
		if(Input.GetKeyDown("r")) Time.timeScale = 1.0f;
		if(Input.GetKeyDown("p")) Time.timeScale += 5.0f;
		if(Input.GetKeyDown("m") && Time.timeScale > 1) Time.timeScale -= 5.0f;		
	}

//------------------------------------------------------------------------------------------
//------------------------------------------------------------------------------------------

	void FixedUpdate()
	{
		timer += Time.deltaTime;
		List<double> states = new List<double>();
		List<double> qs = new List<double>();

		states.Add(this.transform.rotation.z);
		states.Add(pole.transform.position.x);
		// states.Add(pole.GetComponent<Rigidbody>().angularVelocity.x);
		states.Add(pole.GetComponent<Rigidbody2D>().angularVelocity);

		qs = SoftMax(nn.CalcOutput(states));
		double maxQ = qs.Max();
		int maxQIndex = qs.ToList().IndexOf(maxQ);
		exploreRate = Mathf.Clamp(exploreRate - exploreDecay, minExploreRate, maxExploreRate);

		//Exploration taking a random action
		// if(Random.Range(0, 100) < exploreRate) maxQIndex = Random.Range(0, 2);
		
		/* ===================================================== */
		//WARNING To modify every time
		/* ===================================================== */		
		if(maxQIndex == 0) this.transform.position = new Vector3(this.transform.position.x * maxSpeed * (float)qs[maxQIndex], this.transform.position.y, this.transform.position.z);
		else if(maxQIndex == 1) this.transform.position = new Vector3(-this.transform.position.x * maxSpeed * (float)qs[maxQIndex], this.transform.position.y, this.transform.position.z);
		// if(maxQIndex == 0) this.transform.Translate(Vector3.right * maxSpeed * (float)qs[maxQIndex]);
		// else if(maxQIndex == 1) this.transform.Translate(Vector3.right * -maxSpeed * (float)qs[maxQIndex]);
		
		// if(maxQIndex == 0) this.transform.Rotate(Vector3.right, tiltSpeed * (float)qs[maxQIndex]);
		// else if(maxQIndex == 1) this.transform.Rotate(Vector3.right, -tiltSpeed * (float)qs[maxQIndex]);
		
		if(pole.GetComponent<PoleState>().dropped) reward = -1.0f;
		else reward = 0.1f;

		/* ===================================================== */
		//WARNING To modify every time
		/* ===================================================== */
		Replay lastMemory = new Replay(this.transform.position.x, 
														pole.transform.position.x,
														pole.GetComponent<Rigidbody2D>().angularVelocity,
														reward);

		if(replayMemory.Count > mCapacity) replayMemory.RemoveAt(0);

		replayMemory.Add(lastMemory);

		//Q-Learning part
		if(pole.GetComponent<PoleState>().dropped)
		{
			//Loop backwards
			for(int i = replayMemory.Count - 1; i >= 0; i--)
			{
				List<double> toutputsOld = new List<double>();
				List<double> toutputsNew = new List<double>();
				toutputsOld = SoftMax(nn.CalcOutput(replayMemory[i].states));

				double maxQOld = toutputsOld.Max();
				int action = toutputsOld.ToList().IndexOf(maxQOld);

				//Bellman's Equation
				double feedback;
				if(i == replayMemory.Count - 1 || replayMemory[i].reward == -1)
					feedback = replayMemory[i].reward;
				else
				{
					toutputsNew = SoftMax(nn.CalcOutput(replayMemory[i + 1].states));
					maxQ = toutputsNew.Max();
					feedback = (replayMemory[i].reward + discount * maxQ);
				}

				toutputsOld[action] = feedback;
				nn.Train(replayMemory[i].states, toutputsOld);
			}
			if(timer > maxBalanceTime) maxBalanceTime = timer;

			timer = 0;

			/* ===================================================== */
			//WARNING To modify every time
			/* ===================================================== */
			pole.GetComponent<PoleState>().dropped = false;
			this.transform.position = Vector2.zero;
			ResetPole();
			replayMemory.Clear();
			failCount++;
		}
	}

//------------------------------------------------------------------------------------------
//------------------------------------------------------------------------------------------

	void ResetPole()
	{		
		Rigidbody2D rb2d = pole.GetComponent<Rigidbody2D>();

		pole.transform.position = poleStartPosition;
		pole.transform.position = poleStartPosition + new Vector3(Random.Range(-3.5f, 3.5f), 0f, 0f);
		pole.GetComponent<Rigidbody2D>().velocity = new Vector2(0.0f, 0.0f);
		pole.GetComponent<Rigidbody2D>().angularVelocity = 0.0f;				
		
		float direction = Mathf.Floor(Random.Range(-1.0f, 1.0f));
		if(direction == 0) direction++;
		Debug.Log("Direction = " + direction);
		rb2d.AddTorque(torque * direction);	
	}

//------------------------------------------------------------------------------------------
//------------------------------------------------------------------------------------------

	List<double> SoftMax(List<double> values)
	{
		double max = values.Max();

		float scale = 0.0f;
		for(int i = 0; i < values.Count; i++)
			scale += Mathf.Exp((float)(values[i] - max));

		List<double> result = new List<double>();
		for(int i = 0; i < values.Count; i++)
			result.Add(Mathf.Exp((float)(values[i] - max)) / scale);

		return result;
	}

//------------------------------------------------------------------------------------------
//------------------------------------------------------------------------------------------
}

