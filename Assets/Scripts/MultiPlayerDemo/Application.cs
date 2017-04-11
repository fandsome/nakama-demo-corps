using System;
using System.Collections;
using System.Collections.Generic;
using Nakama;

public class Application {

	// session
	public static INSession session;

	// client
	public static NClient client;

	// user id
	public static byte[] userId;

	// extra data
	public static Dictionary<string, Object> data = new Dictionary<string, Object>();

	// clear all application cached data
	public static void clear() {
		session = null;
		client = null;
		data = new Dictionary<string, Object> ();
	}

}
