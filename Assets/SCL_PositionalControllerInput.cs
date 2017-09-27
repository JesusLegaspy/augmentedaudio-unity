using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Globalization;
using System.Text;

public class SCL_PositionalControllerInput : MonoBehaviour, SCL_IClientSocketHandlerDelegate {
	public readonly static int maxObjects = 10;
	private readonly object positionLock = new object();
	private Vector3 position = new Vector3();
	private SCL_SocketServer socketServer;
	private static myObj[] cubeArray = { new myObj(), new myObj(), new myObj(), new myObj(), new myObj(),
										new myObj(), new myObj(), new myObj(), new myObj(), new myObj()};
	
	public struct myObj {
		public bool valid;
		public GameObject cubeObj;
		public float[] position;				
		public bool cFlag;
		public bool dFlag;
	};
	
	//Initialization
	void Start () {
		for(int i = 0; i < maxObjects; i++) {
			myObj q = cubeArray[i];
			q.valid = false;
			q.cubeObj = null;					//GameObject object that is physically placed in the Unity Engine
			q.cFlag = false;					//"create" flag for FixedUpdate() method
			q.dFlag = false;					//"destroy" flag for FixedUpdate() method
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
		//float log = Time.deltaTime;
		for(int i = 0; i < maxObjects; i++) {
			myObj q = cubeArray[i];
			if(true == q.cFlag) {
				q.cFlag = false;		//finished, no longer needs to be created
				Debug.LogError("Handler entered.");
				q.cubeObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
				q.cubeObj.transform.position = new Vector3(q.position[0], q.position[1], q.position[2]);		//update position of object in engine
			}
			if(true == q.dFlag) {
				q.dFlag = false;
				Destroy(q.cubeObj);
				cubeArray[i] = new myObj();		//create new object, leaving old reference to garbage collector
			}
		}
	}
	
	//Executed upon application exit
	void OnApplicationQuit() {
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
	
	private myObj get_myObj(int ID) {
		return cubeArray[ID];
	}
	
	private void create_myObj(float[] pos) {
		myObj q = new myObj();
		q.valid = true;
		q.position = pos;
		q.cFlag = true;				//set the "create" flag so FixedUpdate() will create it for us
		int ID = getUnusedID();
		cubeArray[ID] = q;
		//update_myObj(q, getUnusedID());
	}
	
	private void destroy_myObj(int ID) {
		myObj dest = cubeArray[ID];
		dest.dFlag = true;		//set the flag and let the FixedUpdate() method handle destruction.
	}

	// this delegate method will be called on another thread, so use locks for synchronization
	public void msgParse(SCL_ClientSocketHandler handler, string message) {
		string[] pieces = message.Split('+');
		if(pieces.Length > maxObjects) {
			Debug.Log("Too many arguments to msgParse, terminating.");
			return;
		}
		for(int i = 0; i < pieces.Length; i++) {
			Debug.Log("Piece #" + (i + 1) +" :" + pieces[i]);
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
					ID = int.Parse(input.Substring(1, input.Length));
				} catch (FormatException e) {
					Debug.LogError("ID could not be interpreted as integer: " + e);
					return;
				}
				destroy_myObj(ID);
				break;
			} case "M": {
				float[] pos = coordsToFloats(input);
				int len1 = input.IndexOf("(");
				int ID;
				try {
					ID = int.Parse(input.Substring(1, len1));
				} catch (FormatException e) {
					Debug.LogError("ID could not be interpreted as integer: " + e);
					return;
				}
				myObj upd = get_myObj(ID);
				upd.position = pos;
				break;
			} default: {
				Debug.Log("Cmd" + cmd + " not found.");
				break;
			}
		}
	}
	
	protected Vector3 GetPositionValue() {
		Vector3 vec;
		lock(this.positionLock) {
			vec = new Vector3(this.position.x, this.position.y, this.position.z);
		}
		return vec;
	}

	protected void SetPositionValue(float x, float y, float z)
	{
		lock(this.positionLock) {
			this.position = new Vector3(x,y,z);
		}
	}

}
