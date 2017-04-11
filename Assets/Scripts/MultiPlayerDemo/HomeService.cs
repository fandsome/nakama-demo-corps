using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Nakama;

using Newtonsoft.Json;

public class HomeService : MonoBehaviour {

	// room button prefab
	public GameObject matchGoPrefab;

	// rooms scroll view
	public ScrollRect matchsView;

	// match button
	public Button matchBtn;

	// action message text
	public Text msgText;

	// topic id, used to share room info
	private INTopicId topicId;

	// room button instance
	private Button matchRoomBtn;

	// is matching or not
	private bool matching = false;

	// added match room or not
	private bool matchRoomAdded = false;

	// next user enter number, used to tag player1 or player2 ... 
	private int userEnterId = 1;

	// match id
	private byte[] matchId;

	// message string
	private string msg = "";

	// Use this for initialization
	void Start () {
		if (Application.client != null) {
			
			// enter room
			this.EnterRoom ();

			// recieve events
			this.OnTopicMessage ();

		}

		// assign matching room
		this.matchRoomBtn = this.AddMatch ("NOT CREATED");
	}
	
	// Update is called once per frame
	void Update () {

		// reconnect if necessary
		if (Application.session == null || Application.client == null) {
			SceneManager.LoadScene ("MultiPlayerStart", LoadSceneMode.Single);
		}

		// set message
		this.msg = this.matching ? "matching" + new String ('.', DateTime.Now.Second % 4) : "";
		this.msgText.text = this.msg;

		// set room info
		if (this.matchId == null) {
			this.matchRoomAdded = false;
			this.matchRoomBtn.enabled = false;
			this.matchRoomBtn.GetComponentInChildren<Text> ().text = "NOT CREATED";
		} else if (!this.matchRoomAdded) {
			this.matchRoomAdded = true;
			this.matchRoomBtn.enabled = true;
			this.matchRoomBtn.GetComponentInChildren<Text> ().text = Encoding.UTF8.GetString(this.matchId);
		}

		// load next scene to play
		if (Application.data.ContainsKey("matchId") && Application.data ["matchId"] != null) {
			SceneManager.LoadScene ("Corps", LoadSceneMode.Single);
		}
	}

	[System.Serializable]
	public class CreateMatchMessage
	{
		// user enter range id
		public int userEnterId { get; set; }

		// match id
		public byte[] MatchId { get; set; }
	}

	// match button clicked
	public void MatchBtnClick() {
		// change matching status
		this.matching = !this.matching;
		Text matchBtnText = (Text)this.matchBtn.GetComponentInChildren (typeof(Text));

		// broadcast matching info
		if (this.matching) {
			matchBtnText.text = "Cancel Matching";

			// create match
			var message = NMatchCreateMessage.Default();
			Application.client.Send(message, (INMatch match) => {
				
				// user enter range id, to distinguish between player1 and player2...
				Application.data["userEnterId"] = this.userEnterId;

				// Use this match ID to reference the match later.
				Application.data["matchId"] = match.Id;

				this.matchId = match.Id;

				// broadcast
				this.sendTopic(this.matchId);

				Debug.Log("Successfully created match: " + Encoding.UTF8.GetString(this.matchId));
			}, (INError error) => {
				Debug.LogErrorFormat("Could not create match: '{0}'.", error.Message);
			});

		} else {
			// restore to default status
			matchBtnText.text = "Match";
			this.matchId = null;
		}
	}

	// entering match room
	public void GoBtnClick() {
		Debug.Log ("GoBtnClick");

		// join match
		var message = NMatchJoinMessage.Default(this.matchId);
		Application.client.Send(message, (INMatch match) => {
			// `match.Presence` is the list of current participants.
			// `match.Self` is the presence identifying the current user/session.
			Debug.Log("Successfully joined match");

			Application.data["userEnterId"] = this.userEnterId;

			Application.data["matchId"] = this.matchId;

		}, (INError error) => {
			Debug.LogErrorFormat("Could not join match: '{0}'.", error.Message);
		});
	}

	// set room
	protected Button AddMatch(string text) {
		GameObject goBtn = Instantiate(matchGoPrefab);
		goBtn.transform.SetParent (matchsView.content.transform);
		goBtn.transform.localPosition = new Vector3 (-100, -20, 0);

		Button btn = goBtn.GetComponent<Button> ();
		btn.GetComponentInChildren<Text> ().text = text;
		btn.onClick.AddListener (() => GoBtnClick());
		btn.enabled = false;
		return btn;
	}

	// enter room
	protected void EnterRoom() {
		var roomName = Encoding.UTF8.GetBytes("DefaultRoom");
		var message = new NTopicJoinMessage.Builder().TopicRoom(roomName).Build();
		Application.client.Send(message, (INTopic topic) => {
			
			this.topicId = topic.Topic;

			Debug.Log("Successfully joined the topic");

		}, (INError error) => {
			Debug.LogErrorFormat("Could not join topic: '{0}'.", error.Message);
		});
	}

	// send topic
	protected void sendTopic(byte[] matchId) {
		var createMatchMessage = new CreateMatchMessage ();
		createMatchMessage.MatchId = matchId;
		createMatchMessage.userEnterId = this.userEnterId + 1;

		var data = Encoding.UTF8.GetBytes (JsonConvert.SerializeObject (createMatchMessage));
		var message = NTopicMessageSendMessage.Default(this.topicId, data);

		Application.client.Send(message, (INTopicMessageAck ack) => {
			Debug.Log("Successfully sent message to topic");
		}, (INError error) => {
			Debug.LogErrorFormat("Could not send message to topic: '{0}'.", error.Message);
		});
	}

	// on topic message
	protected void OnTopicMessage() {
		Application.client.OnTopicMessage += (object src, NTopicMessageEventArgs args) => {
			var createMatchMessage = JsonConvert.DeserializeObject<CreateMatchMessage>(Encoding.UTF8.GetString(args.Message.Data));
			this.matchId = createMatchMessage.MatchId;
			this.userEnterId = createMatchMessage.userEnterId;
			Debug.LogFormat("Successfully receive message: {0}", createMatchMessage.MatchId);
		};
	}
}
