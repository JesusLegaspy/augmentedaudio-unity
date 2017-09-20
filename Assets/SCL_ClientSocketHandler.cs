using UnityEngine;
using System.Collections;
using System.Net.Sockets;
using System;
using System.IO;
using System.Text;
using System.Threading;

public interface SCL_IClientSocketHandlerDelegate
{
	// callback, will be NOT be invoked on main thread, so make sure to synchronize in Unity
	void msgParse(SCL_ClientSocketHandler handler, string message);
}

public class SCL_ClientSocketHandler {

	protected readonly TcpClient client;
	protected readonly string separatorSequence;
	protected readonly char [] separatorSequenceChars;
	protected readonly Encoding encoding;
	protected SCL_IClientSocketHandlerDelegate socketDelegate;
	
	public static Queue createFlags = new Queue();
	public static Queue destroyFlags = new Queue();
	
	public SCL_ClientSocketHandler (TcpClient client, 
	                                SCL_IClientSocketHandlerDelegate socketDelegate, 
	                                string separatorString,
	                                Encoding encoding)
	{
		this.client = client;
		this.encoding = encoding;
		this.separatorSequence = separatorString;
		this.separatorSequenceChars = separatorString.ToCharArray();
		this.socketDelegate = socketDelegate;
	}	

	public void Cleanup()
	{
		if (this.client != null) {
			Debug.Log ("Socket client closed (cleanup) ");
			this.client.Close();
		}
	}

	public void Run()
	{
		try {
			StringBuilder dataBuffer = new StringBuilder();

			while (true) {
				byte [] read_buffer = new byte[256];
				int read_length = this.client.Client.Receive(read_buffer);
				if(read_length > 0) {
					//createFlags.Enqueue(12);
					//int yes = Convert.ToInt32(createFlags.Dequeue());
					//Debug.Log("The value in the queue was " + yes);
					dataBuffer.Append(this.encoding.GetString(read_buffer, 0, read_length));
					string data = dataBuffer.ToString();
					string [] tokens = data.Split(this.separatorSequenceChars);
					for(int i = 0; i < tokens.Length; i++) {
						this.socketDelegate.msgParse(this, tokens[i]);
					}
					dataBuffer = new StringBuilder();	//clear dataBuffer
					int yes = Convert.ToInt32(createFlags.Dequeue());
					Debug.Log("Read from queue successful. The value was: " + yes);
				}
				
				/*while (true == destroyFlags.Contains()) {
					createFlags.dequeue();
					UnityEngine.Object.Destroy(SCL_PositionalControllerInput.controlledObject);
				}*/
				//while (true == createFlags.Contains()) {
				//}
			}
		} 
		catch (ThreadAbortException exception) 
		{
			Debug.Log ("Thread aborted");
		} 
		catch (SocketException exception) 
		{
			Debug.Log ("Socket exception");
		} 
		finally 
		{
			this.client.Close();
			Debug.Log ("Socket client closed " + this.client.Client.RemoteEndPoint);
		}
	}

}
