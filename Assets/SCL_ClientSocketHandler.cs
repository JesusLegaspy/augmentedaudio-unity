using UnityEngine;
using System.Collections;
using System.Net.Sockets;
using System;
using System.IO;
using System.Text;
using System.Threading;

public interface SCL_IClientSocketHandlerDelegate {
	// callback, will be NOT be invoked on main thread, so make sure to synchronize in Unity
	void msgParse(SCL_ClientSocketHandler handler, string message);
}

public class SCL_ClientSocketHandler {
	protected readonly TcpClient client;
	protected readonly string separatorSequence;
	protected readonly char [] separatorSequenceChars;
	protected readonly Encoding encoding;
	protected SCL_IClientSocketHandlerDelegate socketDelegate;
	
	public SCL_ClientSocketHandler (TcpClient client,  SCL_IClientSocketHandlerDelegate socketDelegate, 
										string separatorString, Encoding encoding) {
		this.client = client;
		this.encoding = encoding;
		this.separatorSequence = separatorString;
		this.separatorSequenceChars = separatorString.ToCharArray();
		this.socketDelegate = socketDelegate;
	}	

	public void Cleanup() {
		if (this.client != null) {
			Debug.Log ("Socket client closed (cleanup) ");
			this.client.Close();
		}
	}
	
	public bool sendStringToClient(String s) {
		byte[] arr = Encoding.ASCII.GetBytes(s);
		NetworkStream netStream = this.client.GetStream();
		netStream.Write(arr, 0, arr.Length);
		return true;
	}


	public void Run() {
		try {
			StringBuilder dataBuffer = new StringBuilder();

			while (true) {
				byte [] read_buffer = new byte[256];
				int read_length = this.client.Client.Receive(read_buffer);
				if(read_length > 0) {
					dataBuffer.Append(this.encoding.GetString(read_buffer, 0, read_length));
					string data = dataBuffer.ToString();
					if("quit" == data.ToLower())
						break;
					string [] tokens = data.Split(this.separatorSequenceChars);
					for(int i = 0; i < tokens.Length; i++) {
						this.socketDelegate.msgParse(this, tokens[i]);
					}
					dataBuffer = new StringBuilder();	//clear dataBuffer before next read
					read_length = 0;
				}
			}
		} catch (ThreadAbortException e) {
			Debug.LogError ("Thread aborted in client run loop: " + e);
		}  catch (SocketException e) {
			Debug.LogError ("Socket exception: " + e);
		} finally {
			this.client.Close();
			Debug.Log ("Socket client closed " + this.client.Client.RemoteEndPoint);
		}
	}
}
