using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Nakama;

using Newtonsoft.Json;

public class PlaygroundService : MonoBehaviour {

	public GameObject playerPrefab;

	private Dictionary<string, GameObject> players;
	private Dictionary<string, Vector3> playerPositions;
	private Dictionary<string, bool> newUserIds;
	private string userIdString;

	// Use this for initialization
	void Start () {

		this.players = new Dictionary<string, GameObject> ();
		this.playerPositions = new Dictionary<string, Vector3> ();
		this.newUserIds = new Dictionary<string, bool> ();

		if (Application.client != null) {
			this.userIdString = Encoding.UTF8.GetString (Application.userId);

			// set self
			var player = Instantiate(playerPrefab);
			player.GetComponentInChildren<Canvas> ().GetComponentInChildren<Text> ().text = Encoding.UTF8.GetString (Application.userId);
			this.players[userIdString] = player;

			// set events
			this.OnMatchPresence ();
			this.OnMatchData ();
		}
	}
	
	// Update is called once per frame
	void Update () {
		
		// reconnect if necessary
		if (Application.session == null || Application.client == null) {
			SceneManager.LoadScene ("MultiPlayerStart", LoadSceneMode.Single);
		}

		foreach (var newUserId in new List<string>(this.newUserIds.Keys)) {
			if (this.newUserIds[newUserId]) {
				Debug.Log ("init other user");
				var player = Instantiate(playerPrefab);
				player.GetComponentInChildren<Canvas> ().GetComponentInChildren<Text> ().text = newUserId;
				this.players [newUserId] = player;
			} else {
				// delete object
				this.players.Remove(newUserId);
			}
			this.newUserIds.Remove (newUserId);
		}

		if (Application.userId != null) {
			var userId = this.userIdString;
			if (this.players.ContainsKey (userId) && this.players [userId] != null) {
				var oldposition = this.players [userId].transform.position;
				this.players[userId].transform.position = new Vector3 (oldposition.x + 0.01f, oldposition.y, oldposition.z);

				if (Application.data.ContainsKey ("matchId")) {
					this.SendMatchData ((byte[])Application.data ["matchId"], this.players[userId].transform.position);
				}
			}
		}

		foreach (var userId in this.playerPositions.Keys) {
			if (userId != this.userIdString && this.players.ContainsKey(userId)) {
				this.players [userId].transform.position = this.playerPositions[userId];
			}
		}
	}

	[System.Serializable]
	public class UpdatePositionMessage
	{
		public float x { get; set; }
		public float y { get; set; }
		public float z { get; set; }
	}

	protected void OnMatchPresence() {
		Application.client.OnMatchPresence += (object src, NMatchPresenceEventArgs args) => {
			for (int i = 0; i < args.MatchPresence.Join.Count; i++) {
				INUserPresence p = args.MatchPresence.Join [i];
				this.newUserIds[Encoding.UTF8.GetString(p.UserId)] = true;
				Debug.LogFormat ("User Enter Match {0}", p.UserId);
			}
			for (int i = 0; i < args.MatchPresence.Leave.Count; i++) {
				INUserPresence p = args.MatchPresence.Leave [i];
				this.newUserIds[Encoding.UTF8.GetString(p.UserId)] = false;
				Debug.LogFormat ("User Leave Match {0}", p.UserId);
			}
		};
	}

	protected void OnMatchData() {
		Application.client.OnMatchData += (object src, NMatchDataEventArgs args) =>
		{
			var updatePositionMessage = JsonConvert.DeserializeObject<UpdatePositionMessage>(Encoding.UTF8.GetString(args.Data.Data));;
			var userId = Encoding.UTF8.GetString (args.Data.Presence.UserId);
			if (!this.newUserIds.ContainsKey(userId) && !this.players.ContainsKey(userId)) {
				this.newUserIds[userId] = true;
//				Debug.Log("set new user id");
			} else {
//				this.players[args.Data.Presence.UserId].transform.position = new Vector3(updatePositionMessage.x, updatePositionMessage.y, updatePositionMessage.z);
				this.playerPositions[userId] = new Vector3(updatePositionMessage.x, updatePositionMessage.y, updatePositionMessage.z);
			}
//			Debug.LogFormat ("Receive Match Data {0}", updatePositionMessage.x);
		};
	}

	public void SendMatchData(byte[] matchId, Vector3 position) {
//		Debug.Log ("SendMatchData");

		long opCode = 1;
		UpdatePositionMessage updatePositionMessage = new UpdatePositionMessage ();
		updatePositionMessage.x = position.x;
		updatePositionMessage.y = position.y;
		updatePositionMessage.z = position.z;

		var data = Encoding.UTF8.GetBytes (JsonConvert.SerializeObject (updatePositionMessage));
		var message = NMatchDataSendMessage.Default(matchId, opCode, data);
		Application.client.Send(message, (bool complete) => {
//			Debug.Log("Successfully sent data to match.");
		}, (INError error) => {
			Debug.LogErrorFormat("Could not send data to match: '{0}'.", error.Message);
		});
	}
}
