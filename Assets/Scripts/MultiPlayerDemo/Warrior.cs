using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum WarriorStatus {
	STAY,
	WALK,
	RUN,
	ATTACK,
	BACK,
	DIE,
}

public class Warrior : MonoBehaviour {

	public string userIdString;
	public float speed;
	public float health;
	public float attack;
	public GameObject destination;
	public WarriorStatus status;

	protected float lastWatchTime = 0;

	// Use this for initialization
	void Start () {
		this.status = WarriorStatus.WALK;
	}
	
	// Update is called once per frame
	void Update () {

		if (this.health <= 0) {
			this.status = WarriorStatus.DIE;
			return;
		}

		if (this.status == WarriorStatus.DIE) {
			return;
		}

//		if (Time.time - this.lastWatchTime < 2f) {
//			return;
//		}

		// set last watch time
//		this.lastWatchTime = Time.time;

		// most dangerous enemy
		Collider mostDanderousEnemy = null;
		float mostDanderousEnemyDistance = float.MaxValue;

		// watching enemies
		Collider[] hitColliders = Physics.OverlapSphere (this.transform.position, 10);
		foreach (Collider c in hitColliders) {
			// filter non warriors
			if (c.tag != "Warrior")
				continue;
			
			// filter non enemies
			Warrior warrior = c.gameObject.GetComponentInChildren<Warrior> ();
			if (warrior == this || warrior.status == WarriorStatus.DIE)
				continue;
			
			if (warrior.userIdString == this.userIdString)
				continue;
			
			// check distance
			float distance = Vector3.Distance (c.transform.position, this.transform.position);
			if (distance < 2) {
				this.status = WarriorStatus.ATTACK;
			} else {
				this.status = WarriorStatus.RUN;
			}
			if (distance < mostDanderousEnemyDistance) {
				mostDanderousEnemy = c;
			}
		}

		// go to most dangerous enemy
		if (mostDanderousEnemy != null) {
			this.transform.LookAt (mostDanderousEnemy.transform);
			if (this.status == WarriorStatus.ATTACK && mostDanderousEnemy.gameObject.GetComponentInChildren<Warrior> ().health > 0) {
				mostDanderousEnemy.gameObject.GetComponentInChildren<Warrior> ().health -= this.attack * 0.1f;
//				Debug.Log (mostDanderousEnemy.gameObject.GetComponentInChildren<Warrior> ().health);
			}
		} else {
			this.status = WarriorStatus.WALK;
			this.transform.LookAt(this.destination.transform);
		}

		// update transform
		switch (this.status) {
		case WarriorStatus.WALK:
			this.transform.Translate (Vector3.forward * speed * 0.01f);
			break;
		case WarriorStatus.RUN:
			this.transform.Translate (Vector3.forward * speed * 0.03f);
			break;
		case WarriorStatus.ATTACK:
			break;
		case WarriorStatus.DIE:
//			this.enabled = false;
//			this.transform.Translate (Vector3.forward * speed);
			break;
		}
	}
}
