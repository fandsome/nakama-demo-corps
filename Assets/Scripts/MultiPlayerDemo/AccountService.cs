using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Nakama;

public class AccountService : MonoBehaviour {

	// email
	public InputField emailInput;

	// password
	public InputField passwordInput;

	// register button
	public Button registerBtn;

	// login button
	public Button loginBtn;

	// message text
	public Text msgText;

	// next scene
	public string nextScene;

	// message string
	private string msg = "";

	// Use this for initialization
	void Start () {

		// create client
		if (Application.client == null) {
			Application.client = new NClient.Builder ("default_key")
				.Host ("127.0.0.1")
				.Port (7350)
				.SSL (false)
				.Build ();
		}
		
	}
	
	// Update is called once per frame
	void Update () {
		// display action message
		this.msgText.text = this.msg;

		// display next scene
		if (Application.userId != null) {
			SceneManager.LoadScene (nextScene, LoadSceneMode.Single);
		}
	}

	// application quit
	void OnApplicationQuit() {
		// disconnect client, clear Application
		if (Application.client != null) {
			Application.client.Disconnect ();
			Application.clear ();
		}
	}

	// register button clicked
	public void RegisterBtnClick() {
		// register
		var request = NAuthenticateMessage.Email(this.emailInput.text, this.passwordInput.text);
		Application.client.Register(request, (INSession session) => {
			Debug.Log("Player Registration successful");
			msg = "Player Registration successful";
		}, (INError error) => {
			Debug.LogErrorFormat("Email register '{0}' failed: {1}", this.emailInput.text, error);
			msg = "Player Registration failed";
		});
	}

	// login button clicked
	public void LoginBtnClick() {
		// login
		var request = NAuthenticateMessage.Email(this.emailInput.text, this.passwordInput.text);
		Application.client.Login(request, (INSession session) => {
			Debug.Log("Player Logged in successfully");
			msg = "Player Logged in successfully";
			// save session
			Application.session = session;
			// connect client
			Application.client.Connect (session);
			// get self informations
			this.PlayerGetSelf();
		}, (INError error) => {
			Debug.LogErrorFormat("Email login '{0}' failed: {1}", this.emailInput.text, error);
			msg = "Player Logged in failed";
		});
	}

	// get player self info
	public void PlayerGetSelf() {
		Application.client.Send(NSelfFetchMessage.Default(), (INSelf result) => {
			Debug.LogFormat ("Player handle: '{0}'.", result.Handle);
			Application.userId = result.Id;
		}, (INError error) => {
			Debug.LogErrorFormat ("Could not retrieve player self: '{0}'.", error.Message);
		});
	}
}
