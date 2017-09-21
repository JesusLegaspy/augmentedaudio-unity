using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Globalization;
using System.Text;

public class SCL_PositionalControllerInput : MonoBehaviour, SCL_IClientSocketHandlerDelegate {
	public struct myObj {
		public bool valid;
		public GameObject cubeObj;		//GameObject object that is physically placed in the Unity Engine
		public float[] position;		//3-element array containing position
	};
	public readonly static int maxObjects = 10;
	private static myObj[] cubeArray = { new myObj(), new myObj(), new myObj(), new myObj(), new myObj(),
											new myObj(), new myObj(), new myObj(), new myObj(), new myObj()};
	
	private readonly object positionLock = new object();
	private Vector3 position = new Vector3();

	private SCL_SocketServer socketServer;
	
	public int getUnusedID() {
		int i = 0; 
		while(i < maxObjects) {
			if(false == cubeArray[i].valid)
				return i;
			i++;
		}
		return -1;		//all 10 objects in use
	}
	
	private myObj get_myObj(int ID) {
		return cubeArray[ID];
	}
	
	//used to update or add object; does not check for overwrite.
	private void update_myObj (myObj input, int ID) {
		cubeArray[ID] = input;
	}
	
	private void destroy_myObj(int ID) {
		myObj dest = cubeArray[ID];
		dest.valid = false;
		Destroy(dest.cubeObj);
	}
	// Use this for initialization
	void Start () {
		for(int i = 0; i < maxObjects; i++) {
			//myObj init = new myObj();
			myObj init = cubeArray[i];
			init.valid = false;
			//this.update_myObj(init, i);
		}
		SCL_IClientSocketHandlerDelegate socketDelegate = this;
		int maxClients = 1;
		string separatorString = "+";
		int portNumber = 13000;
		Encoding encoding = Encoding.UTF8;

		this.socketServer = new SCL_SocketServer(
			socketDelegate, maxClients, separatorString, portNumber, encoding);

		this.socketServer.StartListeningForConnections();

		Debug.Log (String.Format (
			"Started socket server at {0} on port {1}", 
			this.socketServer.LocalEndPoint.Address, this.socketServer.PortNumber));
	}

	void OnApplicationQuit() {
		Debug.Log ("Cleaning up socket server");
		this.socketServer.Cleanup();
		this.socketServer = null;
	}

	void FixedUpdate() {
		for(int i = 0; i < maxObjects; i++)
			if(true == cubeArray[i].valid)
				cubeArray[i].cubeObj.transform.position = this.GetPositionValue();			
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
			Debug.LogError ("Too many coordinates entered in command: " + input);
		return retval;
	}
	
	//args of the form An(x,y,z)
	//	"A" can take a value of C (create), D (destroy), M (move)
	//	"n" is the ID number of the object
	private void execCmd(string input) {
		string cmd = input.Substring(0, 1);
		switch (cmd) {
			case "C": {
				GameObject newGO = GameObject.CreatePrimitive(PrimitiveType.Cube);;
				float[] pos = coordsToFloats(input);
				Instantiate(newGO, new Vector3(pos[0], pos[1], pos[2]), Quaternion.identity);
				
				myObj q = new myObj();
				q.valid = true;
				q.cubeObj = newGO;
				q.position = pos;
				update_myObj(q, getUnusedID());
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
