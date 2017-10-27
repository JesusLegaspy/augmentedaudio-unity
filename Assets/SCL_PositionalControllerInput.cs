using UnityEngine;
using System;
using System.Globalization;
using System.Text;

public class SCL_PositionalControllerInput : MonoBehaviour, SCL_IClientSocketHandlerDelegate {
	public readonly static int maxObjects = 100;
	private SCL_SocketServer socketServer;
	private static myObj[] cubeArray = new myObj[maxObjects];
	
	public struct myObj {
		public bool valid;
		public GameObject cubeObj;
		public float[] position;				
		public bool cFlag;
		public bool dFlag;
		public bool mFlag;
	};
		
	
	//Initialization
	void Start () {
		for(int i = 0; i < maxObjects; i++) {
			cubeArray[i].valid = false;
			cubeArray[i].cubeObj = null;				//GameObject object that is physically placed in the Unity Engine
			cubeArray[i].cFlag = false;					//"create" flag for FixedUpdate() method
			cubeArray[i].dFlag = false;					//"destroy" flag for FixedUpdate() method
			cubeArray[i].mFlag = false;					//"move" flag for FixedUpdate() method
		}
		SCL_IClientSocketHandlerDelegate socketDelegate = this;
		int maxClients = 1;
		string separatorString = "+";
		int portNumber = 13000;
		Encoding encoding = Encoding.UTF8;
		this.socketServer = new SCL_SocketServer(socketDelegate, maxClients, separatorString, portNumber, encoding);
		this.socketServer.StartListeningForConnections();
		Debug.Log (String.Format (
			"Started socket server at {0} on port {1}", 
			this.socketServer.LocalEndPoint.Address, this.socketServer.PortNumber));
	}

	//Periodically executed by Unity. This program uses it to check
	//  flags to create & destroy objects when needed.
	void FixedUpdate() {
		for(int i = 0; i < maxObjects; i++) {
			//First check that cubeArray[i] is valid before trying to read from it
			if(false == cubeArray[i].valid)
				continue;
			bool internalmove = false;			//prevents "move" message from printing when object is created
			
			if(true == cubeArray[i].cFlag) {
				cubeArray[i].cubeObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
				cubeArray[i].mFlag = true;		//set flag so the if statement below will be entered, updating the position
				internalmove = true;
				cubeArray[i].cFlag = false;		//finished, no longer needs to be created
				Debug.Log("Success. The ID of the new object is " + i + ".");

                //script below is for adding sound and getting distCal to work
                cubeArray[i].cubeObj.tag = "Sound";
				//plz script does the magic boiiiiz
				cubeArray [i].cubeObj.AddComponent<plz> ();

            }
			
			if(true == cubeArray[i].dFlag) {
				cubeArray[i].dFlag = false;
				Destroy(cubeArray[i].cubeObj);
				cubeArray[i].valid = false;
				Debug.Log("Item " + i + " successfully destroyed.");
			}
			
			if(true == cubeArray[i].mFlag) {
				cubeArray[i].mFlag = false;
				cubeArray[i].cubeObj.transform.position = new Vector3(cubeArray[i].position[0],		//update position of object in engine
													cubeArray[i].position[1], cubeArray[i].position[2]);
				//If the object was just created, do not display a "Move successful" log,
				//  as the user never requested one.
				if(false == internalmove)
					Debug.Log("Item " + i + " successfully moved.");
			}
		}
	}
	
	//Executed upon application exit
	void OnApplicationQuit() {
		for(int i = 0; i < maxObjects; i++) {
			if(true == cubeArray[i].valid) {
				Destroy(cubeArray[i].cubeObj);
				cubeArray[i].valid = false;
			}
		}
		Debug.Log ("Cleaning up socket server");
		this.socketServer.Cleanup();
		this.socketServer = null;
	}
	
	//-------------------------------------------------------------------------------------//

	public int getUnusedID() {
		for(int i = 0; i < maxObjects; i++)
			if(false == cubeArray[i].valid)
				return i;
		return -1;		//all 10 objects in use
	}
	
	private void create_myObj(float[] pos) {
		int ID = getUnusedID();
		if(ID == -1) {
			Debug.LogError("Limit of maxObjects reached; cannot create object. Aborting");
			return;
		}
		cubeArray[ID].valid = true;
		cubeArray[ID].position = pos;
		cubeArray[ID].cFlag = true;				//set the "create" flag so FixedUpdate() will create it for us
	}
	
	//set the flag and let the FixedUpdate() method handle destruction.
	private void destroy_myObj(int ID) {
		cubeArray[ID].dFlag = true;		
	}
	
	//Update the position we want and set the flag so FixedUpdate() will move it for us.
	private void move_myObj(int ID, float[] pos) {
		cubeArray[ID].position = pos;
		cubeArray[ID].mFlag = true;
	}

	// this delegate method will be called on another thread, so use locks for synchronization
	public void msgParse(SCL_ClientSocketHandler handler, string message) {
		string[] pieces = message.Split('+');
		if(pieces.Length > maxObjects) {
			Debug.Log("Too many arguments to msgParse, terminating.");
			return;
		}
		for(int i = 0; i < pieces.Length; i++) {
			execCmd(pieces[i]);
		}
	}
	
	//input is the entire instruction (e.g., M1(0.2,1.2,-1.3)
	//output is string array of length 3 corresponding to x, y, and z
	private float[] coordsToFloats(string input){
		int ind1 = input.IndexOf("(");			//if not found?
		int ind2 = input.IndexOf(")");
		string coords = input.Substring(ind1 + 1, ind2 - ind1 - 1);
		string[] pieces = coords.Split(',');
		float[] retval = {0, 0, 0};
		if (pieces.Length == 3) {
			try {
				retval[0] = float.Parse(pieces[0], CultureInfo.InvariantCulture);
				retval[1] = float.Parse(pieces[1], CultureInfo.InvariantCulture);
				retval[2] = float.Parse(pieces[2], CultureInfo.InvariantCulture);
			} catch (FormatException e) {
				Debug.LogError ("Invalid number format in input: " + e);
			}
		} else 
			Debug.LogError ("Improper number of coordinates entered in command: " + input);
		return retval;
	}
	
	//args of the form An(x,y,z)
	//	"A" can take a value of C (create), D (destroy), M (move)
	//	"n" is the ID number of the object
	private void execCmd(string input) {
		string cmd = input.Substring(0, 1);
		switch (cmd) {
			case "C": {
				float[] pos = coordsToFloats(input);
				create_myObj(pos);
				break;
			} case "D": {
				int ID = -1;
				try {
					ID = int.Parse(input.Substring(1, input.Length - 1));
				} catch (Exception e) {
					Debug.LogError("ID could not be interpreted as integer: " + e);
					return;
				}
				destroy_myObj(ID);
				break;
			} case "M": {
				float[] pos = coordsToFloats(input);
				int len1 = input.IndexOf("(");
				int ID = -1;
				try {
					ID = int.Parse(input.Substring(1, len1 - 1));
				} catch (FormatException e) {
					Debug.LogError("ID could not be interpreted as integer: " + e);
					return;
				}
				move_myObj(ID, pos);
				break;
			} default: {
				break;
			}
		}
	}
}
