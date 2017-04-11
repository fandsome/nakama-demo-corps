using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Nakama;

using Newtonsoft.Json;

public class CorpsService : MonoBehaviour {

	public GameObject warriorPrefab;

	private string userIdString;
	private int userEnterId;
	private Dictionary<string, Dictionary<int, PutWarriorMessage>> userWarriorBaseInfos;
	private Dictionary<string, Dictionary<int, GameObject>> userWarriors;

	// Use this for initialization
	void Start () {
		if (Application.client != null) {
			// set camera
			if (Application.data ["userEnterId"] != null) {
				this.userEnterId = (int)Application.data ["userEnterId"];
				foreach (Camera c in Camera.allCameras) {
					if (c.name == ("Camera" + this.userEnterId)) {
						c.enabled = true;
						c.GetComponentInChildren<AudioListener>().enabled = true;
					} else {
						c.enabled = false;
						c.GetComponentInChildren<AudioListener>().enabled = false;
					}
				}
			}
			// init variables
			this.userIdString = Encoding.UTF8.GetString (Application.userId);
			this.userWarriorBaseInfos = new Dictionary<string, Dictionary<int, PutWarriorMessage>> ();
			this.userWarriorBaseInfos [this.userIdString] = new Dictionary<int, PutWarriorMessage> ();
			this.userWarriors = new Dictionary<string, Dictionary<int, GameObject>> ();
			this.userWarriors [this.userIdString] = new Dictionary<int, GameObject> ();
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

		if (this.userWarriorBaseInfos != null) {
			foreach (var userWarriorBaseInfo in this.userWarriorBaseInfos) {
				string userIdString = userWarriorBaseInfo.Key;

//				Debug.Log (userIdString + " warriors num: " + userWarriorBaseInfo.Value.Count);

				foreach (var warriorBaseInfo in userWarriorBaseInfo.Value.Values) {
					if (this.userWarriors.ContainsKey (userIdString)) {
						GameObject obj = null;
						if (!this.userWarriors [userIdString].ContainsKey (warriorBaseInfo.id)) {
							Debug.Log ("new warrior");
							obj = Instantiate (this.warriorPrefab);
							obj.GetComponentInChildren<Warrior> ().userIdString = userIdString;
							obj.GetComponentInChildren<Warrior> ().speed = warriorBaseInfo.speed;
							obj.GetComponentInChildren<Warrior> ().attack = warriorBaseInfo.attack;
							obj.GetComponentInChildren<Warrior> ().health = warriorBaseInfo.health;
							GameObject destination = new GameObject ();
							destination.transform.position = new Vector3 (warriorBaseInfo.destX, warriorBaseInfo.destY, warriorBaseInfo.destZ);
							obj.GetComponentInChildren<Warrior> ().destination = destination;
							obj.GetComponentInChildren<Canvas> ().GetComponentInChildren<Text> ().text = userIdString;
							obj.transform.position = new Vector3 (warriorBaseInfo.x, warriorBaseInfo.y, warriorBaseInfo.z);
							obj.transform.Rotate (0, warriorBaseInfo.rotateY, 0);
							this.userWarriors [userIdString] [warriorBaseInfo.id] = obj;
						} else {
							obj = this.userWarriors [userIdString] [warriorBaseInfo.id];
						}
						if (obj.GetComponentInChildren<Warrior> ().status == WarriorStatus.DIE) {
							obj.SetActive (false);
//							this.userWarriors [userIdString] [warriorBaseInfo.id] = null;
						}
//						var oldPosition = obj.transform.position;
//						obj.transform.position = new Vector3 (oldPosition.x, oldPosition.y, oldPosition.z + warriorBaseInfo.speed * Time.deltaTime);
//						this.userWarriors [userIdString] [warriorBaseInfo.id].transform.position = obj.transform.position;
//						Debug.Log (obj.transform.position.x + ", " + obj.transform.position.y);
					} else {
						this.userWarriors [userIdString] = new Dictionary<int, GameObject> ();
					}
				}
			}
		}
	}

	[System.Serializable]
	public class PutWarriorMessage
	{
		public int id { get; set; }
		public float x { get; set; }
		public float y { get; set; }
		public float z { get; set; }
		public float destX { get; set; }
		public float destY { get; set; }
		public float destZ { get; set; }
		public float rotateY { get; set; }
		public float speed { get; set; }
		public float attack { get; set; }
		public float health { get; set; }
	}

	public void PutWarriorBtnClick() {
		PutWarriorMessage putWarriorMessage = new PutWarriorMessage ();
		putWarriorMessage.id = this.userWarriorBaseInfos[this.userIdString].Count + 1;
		putWarriorMessage.y = 1;
		putWarriorMessage.destY = 1;
		putWarriorMessage.attack = UnityEngine.Random.Range (1f, 10f);
		putWarriorMessage.health = 10f;
		float speed = UnityEngine.Random.Range (3f, 9f);
		if (this.userEnterId == 1) {
			putWarriorMessage.x = UnityEngine.Random.Range (-8f, 8f);
			putWarriorMessage.destX = 16f;
			putWarriorMessage.z = -40;
			putWarriorMessage.destZ = 27;
			putWarriorMessage.rotateY = 0;
			putWarriorMessage.speed = speed;
			putWarriorMessage.z = -40;
		} else if(this.userEnterId == 2) {
			putWarriorMessage.x = UnityEngine.Random.Range (8f, 24f);
			putWarriorMessage.destX = 0;
			putWarriorMessage.z = 27;
			putWarriorMessage.destZ = -40;
			putWarriorMessage.rotateY = 180;
			putWarriorMessage.speed = speed;
		}

		this.SendMatchData ((byte[])Application.data["matchId"], putWarriorMessage);
		this.userWarriorBaseInfos [this.userIdString] [putWarriorMessage.id] = putWarriorMessage;
	}

	protected void OnMatchPresence() {
		Application.client.OnMatchPresence += (object src, NMatchPresenceEventArgs args) => {
			
			// user join
			for (int i = 0; i < args.MatchPresence.Join.Count; i++) {
				Debug.Log("user enter");
				INUserPresence p = args.MatchPresence.Join [i];
				var userIdString = Encoding.UTF8.GetString(p.UserId);
				if (!this.userWarriorBaseInfos.ContainsKey (userIdString)) {
					this.userWarriorBaseInfos [userIdString] = new Dictionary<int, PutWarriorMessage> ();
				}
				if (!this.userWarriors.ContainsKey (userIdString)) {
					this.userWarriors [userIdString] = new Dictionary<int, GameObject> ();
				}
			}

			// user leave
			for (int i = 0; i < args.MatchPresence.Leave.Count; i++) {
				Debug.Log("user leave");
				INUserPresence p = args.MatchPresence.Leave [i];
				var userIdString = Encoding.UTF8.GetString(p.UserId);
				if (this.userWarriorBaseInfos.ContainsKey (userIdString)) {
					this.userWarriorBaseInfos.Remove (userIdString);
				}
				if (this.userWarriors.ContainsKey (userIdString)) {
					this.userWarriors.Remove (userIdString);
				}
			}
		};
	}

	protected void OnMatchData() {
		Application.client.OnMatchData += (object src, NMatchDataEventArgs args) =>
		{
			// decode
			var putWarriorMessage = JsonConvert.DeserializeObject<PutWarriorMessage>(Encoding.UTF8.GetString(args.Data.Data));;
			var userIdString = Encoding.UTF8.GetString (args.Data.Presence.UserId);

			// init if necessary
			if (!this.userWarriorBaseInfos.ContainsKey (userIdString)) {
				this.userWarriorBaseInfos [userIdString] = new Dictionary<int, PutWarriorMessage> ();
			}
			if (!this.userWarriors.ContainsKey (userIdString)) {
				this.userWarriors [userIdString] = new Dictionary<int, GameObject> ();
			}

			// update user warrior base infos
			this.userWarriorBaseInfos [userIdString] [putWarriorMessage.id] = putWarriorMessage;

//			Debug.LogFormat ("Receive Match Data {0}", userIdString);
		};
	}

	public void SendMatchData(byte[] matchId, PutWarriorMessage putWarriorMessage) {
//		Debug.Log ("SendMatchData " + Encoding.UTF8.GetString(matchId));

		long opCode = 1;
		var data = Encoding.UTF8.GetBytes (JsonConvert.SerializeObject (putWarriorMessage));
		var message = NMatchDataSendMessage.Default(matchId, opCode, data);
		Application.client.Send(message, (bool complete) => {
//			Debug.Log("Successfully sent data to match.");
		}, (INError error) => {
			Debug.LogErrorFormat("Could not send data to match: '{0}'.", error.Message);
		});
	}
}
