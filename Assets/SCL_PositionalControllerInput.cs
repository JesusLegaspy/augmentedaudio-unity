using UnityEngine;
using System;
using System.Threading;
using System.Globalization;
using System.Text;

public class SCL_PositionalControllerInput : MonoBehaviour, SCL_IClientSocketHandlerDelegate {

	//public GameObject controlledObject;
	//public GameObject cube;
	public GameObject[] cubeArray;
	private readonly object positionLock = new object();
	private Vector3 position = new Vector3();

	private SCL_SocketServer socketServer;

	// Use this for initialization
	void Start () {
		for(int i = 0; i < 10; i++)
			cubeArray[i] = GameObject.CreatePrimitive(PrimitiveType.Cube);
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
		this.controlledObject.transform.position = this.GetPositionValue();
	}

	// this delegate method will be called on another thread, so use locks for synchronization
	public void msgParse(SCL_ClientSocketHandler handler, string message) {
		string[] pieces = message.Split('+');
		if(pieces.Length > 10) {
			Debug.Log("Too many arguments to msgParse, terminating.");
			return;
		}
		for(int i = 0; i < pieces.Length; i++) {
			Debug.Log("Piece #" + (i + 1) +" :" + pieces[i]);
			execCmd(pieces[i]);
		}
	}
	
	//args of the form An(x,y,z)
	//	"A" can take a value of C (create), D (destroy), M (move)
	//	"n" is the ID number of the object
	private void execCmd(string input) {
		string cmd = input.Substring(0, 1);
		
		//Debug.Log("coords contains: " + coords);
		
		//Debug.Log("The ID is: " + ID);
		
		//string coords = input.Substring(len + 2, 11);	//grabs coordinates, i.e., "1.0,1.0,1.0"
		
		//Debug.Log("The coordinates are: " + coords);
		
		string[] pieces = coords.Split(',');
		float x = 0;
		float y = 0;
		float z = 0;
		if (pieces.Length == 3) {
			try {
				x = float.Parse(pieces[0], CultureInfo.InvariantCulture);
				y = float.Parse(pieces[1], CultureInfo.InvariantCulture);
				z = float.Parse(pieces[2], CultureInfo.InvariantCulture);
				//Debug.Log (String.Format("x: {0}, y: {1}, z: {2}", x, y, z));
			} catch (FormatException e) {
				Debug.Log ("Invalid number format: " + input);
			}
		} else 
			Debug.Log ("Length is invalid: " + input);
		
		Debug.Log("x,y,z: " + x +  "," + y + "," + z);
		
		//Now that all the parsing of the message is done, execute the command based on what it is
		switch (cmd) {
			case "C": {
				//Instantiate(this.cube, new Vector3(x, y, z), Quaternion.identity);
				int newid = getUnusedID();
				SCL_ClientSocketHandler.createFlags.Enqueue(newid);
				break;
			} case "D": {
				try {
					int id = int.Parse(input.Substring(1, 1));
				} catch (FormatException e) {
					Debug.Log("Unable to Parse ID for Destroy: " + input.Substring(1,1));
					return;
				}
				SCL_ClientSocketHandler.destroyFlags.Enqueue(ID);
				break;
			} case "M": {
				//Debug.Log("The command is: " + cmd);
				int ind1 = input.IndexOf("(");
				int ind2 = input.IndexOf(")");
				string coords = input.Substring(ind1 + 1, ind2 - ind1 - 1);
				
				//Find length of ID. This is found by taking the overall length, subtracting the 
				//  coordinates and parentheses, and subtracting the cmd character.
				int len = input.Length - (ind2 - ind1 + 1) - 1;
				string ID_string = input.Substring(1, len);
				int ID;
				try {
					ID = int.Parse(ID_string);
				} catch (FormatException e) {
					Debug.Log("String subset could not be transfererd to integer: " + ID_string + ", exiting.");
					return;
				}
				this.SetPositionValue(x,y,z);
				break;
			} default: {
				Debug.Log("Cmd" + cmd + " not found.");
				break;
			}
		}
	}
	
	private int getUnusedID() {
		int i = 0; 
		while(i < 10) {
			if(cubeArray[i] != -1)
				return i;
			i++;
		}
		return -1;		//all 10 objects in use
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
