// Author: Hibnu Hishath
// Email:  hibnu.s@digipen.edu

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovePlayer : MonoBehaviour
{
	[Tooltip("The speed at which the player moves")]
	[SerializeField] float speed = 2;

	[Header("Light Player Properties")]
	[Tooltip("The mesh representing a light player")]
	[SerializeField] Mesh LightMesh;

	[Tooltip("The color of the light player")]
	[SerializeField] Color LightColor;

	[Header("Dark Player Properties")]
	[Tooltip("The mesh representing a dark player")]
	[SerializeField] Mesh DarkMesh;

	[Tooltip("The color of the dark player")]
	[SerializeField] Color DarkColor;

	public int BurdenCount { get; protected set; }

	PlayerType type = PlayerType.LIGHT;

	MeshFilter meshFilter;

	MeshRenderer meshRenderer;

	// called once at the start
	private void Start()
	{
		meshFilter = GetComponent<MeshFilter>();
		meshRenderer = GetComponent<MeshRenderer>();
	}

	// Update is called once per frame
	void Update()
	{
		Vector3 posDelta = Vector3.zero;
		
		// calculate the coordinate delta
		if(Input.GetKey(KeyCode.W)) { posDelta.y += 1; }
		if(Input.GetKey(KeyCode.A)) { posDelta.x -= 1; }
		if(Input.GetKey(KeyCode.S)) { posDelta.y -= 1; }
		if(Input.GetKey(KeyCode.D)) { posDelta.x += 1; }

		transform.position += (speed * Time.deltaTime) * posDelta;
	}

	public void CollectBurden()
	{
		// increment the count
		BurdenCount++;

		// when the player has collected enough burdens, swap the player type
		if(BurdenCount == 5)
		{
			BurdenCount = 0;
			if(type == PlayerType.LIGHT) 
			{ 
				type = PlayerType.DARK;
				meshFilter.mesh = DarkMesh;
				meshRenderer.material.SetColor("_Color", DarkColor);
			}
			else 
			{ 
				type = PlayerType.DARK;
				meshFilter.mesh = LightMesh;
				meshRenderer.material.SetColor("_Color", LightColor);
			}
		}

		BurdenManager.Instance.SwapBurdenType(type);
	}

	// The type of the current player
	public enum PlayerType { LIGHT, DARK }
}
