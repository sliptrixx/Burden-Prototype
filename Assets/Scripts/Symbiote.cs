// Author: Hibnu Hishath
// Email:  hibnu.s@digipen.edu

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Symbiote : MonoBehaviour
{
	[Tooltip("The symbiote starts moving towards the player if within this radius")]
	[SerializeField] float AttractionRadius = 5;

	[Tooltip("The radius at which the root snaps to the player")]
	[SerializeField] float SnapRadius = 3.0f;

	[Tooltip("The radius at which the symbiote touches player")]
	[SerializeField] float TouchRadius = 2.5f;

	[Tooltip("The speed at which the symbiote shrinks")]
	[SerializeField] float ShrinkSpeed = 40.0f;

	[Tooltip("The speed at which the symbiote travels in projectile mode")]
	[SerializeField] float ProjectileSpeed = 1.0f;

	// The status of the symbiote
	Status status = Status.NOT_ATTRACTED;

	// A reference to the player that the symbiote will attract to
	Transform player;

	// A reference to the child sphere mesh
	Transform child;

	// the default scale of the object on the x-axis
	float defaultScale = 1.0f;

	// the area of the symbiote
	float area = 1.0f;

	// how far the projectile should travel
	float projectileDistance = 0.0f;

	MeshRenderer rend;

	// Called once at the start of the frame
	void Start()
	{
		// let's assume that the player is the only object with a CursorPlayer component
		player = FindObjectOfType<MovePlayer>().transform;

		// get a reference to the child object
		child = transform.GetChild(0).transform;

		// get the default scale of the symbiote on the y-axis
		defaultScale = transform.localScale.y;

		// calculate the area
		area = transform.localScale.x * transform.localScale.y;

		// get the renderer from the children
		rend = GetComponentInChildren<MeshRenderer>();

		// performing a null check in the editor mode and reporting it
		#if UNITY_EDITOR
		if(player == null)
		{
			Debug.LogWarning("Player not found in the scene. Exiting play mode.");
			UnityEditor.EditorApplication.ExitPlaymode();
		}
		#endif

		// Start with orienting the player... why? due to pivot reasons
		LookAtPlayer();
	}
	
	// Called once per frame
	private void Update()
	{
		// if the update process is done... stop anymore updates
		if(status == Status.DONE) 
		{
			player.GetComponent<MovePlayer>().CollectBurden();
			BurdenManager.Instance.DeleteReference(this);
			Destroy(gameObject); 
			return; 
		}

		// move towards the player as a projectile
		if(status == Status.PROJECTILE)
		{
			float deltaDistance = ProjectileSpeed * Time.deltaTime;
			transform.position += deltaDistance * -transform.up;
			projectileDistance -= deltaDistance;

			if(projectileDistance <= 0)
			{
				status = Status.DONE;
			}

			return;
		}

		// the symbiote has snapped, so process that information
		if(status == Status.SNAPPED)
		{
			// scale down to 0 by the given time
			Vector3 scale = transform.localScale;
			scale.y -= Time.deltaTime * ShrinkSpeed;
			transform.localScale = scale;

			if (scale.y <= defaultScale)
			{
				status = Status.PROJECTILE;
				projectileDistance = Vector3.Distance(transform.position, player.position);
			}

			return;
		}

		// check if the symbiote is attracted to the player
		float dist = Vector3.Distance(transform.position, player.position);

		// update the status based on the distance
		if(dist <= AttractionRadius) { status = Status.ATTRACTED; }
		else { status = Status.NOT_ATTRACTED; }

		// if the symbiote is attracted, update the transform accordingly
		if(status == Status.ATTRACTED)
		{
			// set the scale to the stretch value
			StretchTowardsPlayer(dist);

			// set the orientation of the symbiote
			LookAtPlayer();
		}

		// once the symbiot has touched the player, set a flag that the
		// symbiote has snapped from it's root
		if (dist <= SnapRadius)
		{
			status = Status.SNAPPED;
			ChangePivot();
		}
	}

	// Stretch towards the player
	private void StretchTowardsPlayer(float dist)
	{
		float stretch = dist.Remap(AttractionRadius, TouchRadius, 1, TouchRadius);
		Vector3 size = transform.localScale;
		size.x = area / stretch;
		size.y = stretch;
		transform.localScale = size;
	}

	// Look at the player
	private void LookAtPlayer()
	{
		float rot = Extensions.Angle(transform.position, player.position);
		Vector3 rot_vec = transform.eulerAngles;
		rot_vec.z = rot + 90;
		transform.eulerAngles = rot_vec;
	}

	// Change the pivot to be right alligned
	private void ChangePivot()
	{
		// move the pivot to be right aligned
		Vector3 childPos = child.localPosition;
		childPos.x = -childPos.x;
		childPos.y = -childPos.y;
		child.localPosition = childPos;

		// not only child position has to be changed, the main position
		// has to be changed as well
		transform.position += -transform.up * transform.localScale.x;
	}

	public void SetColor(Color color)
	{
		rend.material.SetColor("_Color", color);
	}

	// The status of the symbiote
	public enum Status
	{
		NOT_ATTRACTED,
		ATTRACTED,
		SNAPPED,
		PROJECTILE,
		DONE
	}
}
