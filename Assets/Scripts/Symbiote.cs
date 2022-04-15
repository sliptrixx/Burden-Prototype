// Author: Hibnu Hishath
// Email:  hibnu.s@digipen.edu

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

public class Symbiote : MonoBehaviour
{
	[Tooltip("The symbiote starts moving towards the player if within this radius")]
	[SerializeField] float AttractionRadius = 5;

	[Tooltip("The maximum size the symbiote will stretch")]
	[SerializeField] float MaxSize = 2.5f;

	[Tooltip("The radius at which the root snaps to the player")]
	[SerializeField] float SnapRadius = 3.0f;

	[Header("Speeds")]
	[Tooltip("The speed at which the symbiote stretches")]
	[SerializeField] float StretchSpeed = 40.0f;

	[Tooltip("The speed at which the symbiote shrinks")]
	[SerializeField] float ShrinkSpeed = 40.0f;

	[Tooltip("The speed at which the symbiote travels in projectile mode")]
	[SerializeField] float ProjectileSpeed = 1.0f;

	[Header("Optional")]
	[Tooltip("Should the symbiote follow the player after snapping?")]
	[SerializeField] bool FollowPlayer = false;

	// The status of the symbiote
	Status status = Status.NOT_ATTRACTED;

	// A reference to the player that the symbiote will attract to
	Transform player;

	// A reference to the child sphere mesh
	Transform child;

	// A reference to the bottom most child
	Transform bottom;

	// The default scale of the object on the x-axis
	float scaleRatio = 1.0f;

	// The area of the symbiote
	float area = 1.0f;

	// A datastructure that stores symbiotes line-of-sight data
	private (bool HitSomething, bool HitPlayer, Vector3 HitPoint) playerLOSData;

	// A flag representing whether the symbiote has hit the player
	bool CollidedWithPlayer = false;

	// The object that the symbiote is looking at
	private Transform hitTransform = null;

	// Called once at the start of the frame
	void Start()
	{
		// let's assume that the player is the only object with a CursorPlayer component
		player = FindObjectOfType<MovePlayer>().transform;

		// get a reference to the child 
		child = transform.GetChild(0).transform;

		// find the bottom most child
		bottom = FindBottomMostChild();

		// calculate the default scale and the ratio
		float defaultScale = Vector3.Distance(transform.position, GetTip());
		scaleRatio = MaxSize / defaultScale;

		// calculate the area
		area = transform.localScale.x * transform.localScale.y;

		// performing a null check in the editor mode and reporting it
		#if UNITY_EDITOR
		if (player == null)
		{
			Debug.LogWarning("Player not found in the scene. Exiting play mode.");
			EditorApplication.ExitPlaymode();
		}
		#endif

		// Start with orienting the player... why? due to pivot reasons
		LookAtPlayer();
	}

	// Called once per frame
	private void Update()
	{
		// if the update process is done... stop anymore updates
		if (status == Status.DONE)
		{
			// if the symbiote collided with the player, collect the burden
			if (CollidedWithPlayer)
			{
				player.GetComponent<MovePlayer>().CollectBurden();
			}

			// remove self from the manager and destroy the symbiote
			BurdenManager.Instance.DeleteReference(this);
			Destroy(gameObject);
			return;
		}

		// move as a projectile with the given projectile speed
		if (status == Status.PROJECTILE)
		{
			// when requested follow the player, the symbiote will active
			// adjust it's rotation to follow the player
			if(FollowPlayer) { LookAtPlayer(); }

			// move in the direction the symbiote is looking at
			float deltaDistance = ProjectileSpeed * Time.deltaTime;
			transform.position += deltaDistance * -transform.up;

			return;
		}

		// the symbiote has snapped, so process that information
		if (status == Status.SNAPPED)
		{
			// scale down to 0 by the given time
			Vector3 scale = transform.localScale;
			scale.y -= Time.deltaTime * ShrinkSpeed;
			scale.x = area / scale.y;
			transform.localScale = scale;

			if (scale.y <= 1)
			{
				status = Status.PROJECTILE;
			}

			return;
		}

		// the symbiote has to be actively looking at the player
		LookAtPlayer();

		// check if the symbiote is attracted to the player
		float dist = Vector3.Distance(transform.position, player.position);

		// update the status based on the distance
		if (dist <= AttractionRadius && playerLOSData.HitPlayer)
		{
			status = Status.ATTRACTED;
		}
		else 
		{
			status = Status.NOT_ATTRACTED;

			// actively retract it's shape to it's default one when it's no
			// longer being attracted
			Vector3 scale = transform.localScale;
			if(scale.y > 1)
			{
				scale.y -= ShrinkSpeed * Time.deltaTime;
				scale.x = area / scale.y;
				transform.localScale = scale;
			}

			return;
		}

		// if the symbiote is attracted, update the transform accordingly
		if (status == Status.ATTRACTED)
		{
			// set the scale to the stretch value
			StretchTowardsPlayer(dist);
		}

		// once the symbiot has touched the player, set a flag that the
		// symbiote has snapped from it's root
		if (dist <= SnapRadius)
		{
			status = Status.SNAPPED;
			SwapPivot();
		}
	}

	private void FixedUpdate()
	{
		// physics raycast is being performed, so this code has to be
		// executed in FixedUpdate
		playerLOSData = DirectLOSToPlayer();
	}

	private void OnCollisionEnter(Collision collision)
	{
		// Using a flags here to handle changes instead of instantly applying
		// them because this collision check runs on the physics thread and
		// can probably cause issues with main update thread
		if(status == Status.PROJECTILE)
		{
			status = Status.DONE;
			CollidedWithPlayer = collision.gameObject.CompareTag("Player");
		}
	}

	// Stretch towards the player
	private void StretchTowardsPlayer(float dist)
	{
		// calculate the expected stretch value
		float stretch = dist.Remap(AttractionRadius, SnapRadius, 1, scaleRatio);
		
		// the expected scale of the object
		Vector3 scale = transform.localScale;
		scale.y = stretch;
		scale.x = area / scale.y;

		// instead of instantly stretching, the symbiote gradually increases
		// to it's expected scale given by the shrink speed
		transform.localScale = Vector3.Lerp(transform.localScale, scale, Time.deltaTime * StretchSpeed);
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
	private void SwapPivot()
	{
		// move the pivot to be right aligned
		Vector3 childPos = child.localPosition;
		childPos.y = -childPos.y;
		child.localPosition = childPos;

		// not only child position has to be changed, the main position
		// has to be changed as well 
		if(childPos.y > 0)
		{
			transform.position -= transform.up * (child.localScale.y * transform.localScale.y);
		}
		else
		{
			transform.position += transform.up * (child.localScale.y * transform.localScale.y);
		}
	}

	// Get the tip of the entire object
	private Vector3 GetTip()
	{
		return GetTip(bottom);
	}

	// Variant of get tip that gets the tip of a particular transform
	private Vector3 GetTip(Transform obj)
	{
		Mesh mesh = obj.GetComponent<MeshFilter>().mesh;
		float scale = transform.localScale.y * (obj.localScale.y / 2);
		return obj.position - (scale * mesh.bounds.size.y * transform.up);
	}

	// set the color of the symbiote
	public void SetColor(Color color)
	{
		// get the mesh renders and set the color values
		var rends = GetComponentsInChildren<MeshRenderer>();

		// loop through all mesh renderers and set the color
		foreach (MeshRenderer rend in rends)
		{
			rend.material.SetColor("_Color", color);
		}
	}

	// fixes the pivots of the children
	public void FixChildrenPivot()
	{
		// initialize cache values
		Transform baseChild = transform.GetChild(0);
		Vector3 offset = default;

		// calculate offset
		float currentY = baseChild.localPosition.y;
		float expectedY = -baseChild.localScale.y / 2.0f;
		offset.y = expectedY - currentY;

		// apply offset on all children
		foreach (Transform child in transform)
		{
			child.localPosition += offset;
		}
	}

	// EDITOR ONLY FUNCTION to help visualize the internal values of the symbiote
	#if UNITY_EDITOR
	bool isDebugDrawActive = false;
	public void ToggleDebugDraw()
	{
		isDebugDrawActive = !isDebugDrawActive;
	}

	private Vector3 debugTextOffset = new Vector3(1, -0.5f, 0);
	private void OnDrawGizmos()
	{
		if (!isDebugDrawActive) { return; }

		// Debug stuff drawn when not in play mode to visualize different values
		if(!Application.isPlaying)
		{
			Debug.DrawRay(transform.position, -transform.up * AttractionRadius, Color.green);
			Debug.DrawRay(transform.position, -transform.up * SnapRadius, Color.blue);
			Debug.DrawRay(transform.position, -transform.up * MaxSize, Color.red);

			return;
		}

		// sanity check
		if(player == null) { return; }

		// when the player is in the line-of-sight of the symbiote, it will draw all elements
		// else we only draw a line representing which object the symbiote looks at
		if(playerLOSData.HitPlayer)
		{
			Debug.DrawRay(transform.position, -transform.up * AttractionRadius, Color.green);
			Debug.DrawRay(transform.position, -transform.up * SnapRadius, Color.blue);
			Debug.DrawRay(transform.position, -transform.up * MaxSize, Color.red);
		}
		else if(playerLOSData.HitSomething)
		{
			Debug.DrawLine(transform.position, playerLOSData.HitPoint, Color.green);
		}

		// Additional useful debug texts to figure out any potential issues
		float playerDist = Vector3.Distance(transform.position, player.position);
		float scale = Vector3.Distance(transform.position, GetTip());

		Vector3 debugTip = GetTip();
		Debug.DrawLine(transform.position, GetTip(), Color.magenta);
		Debug.DrawLine(debugTip - new Vector3(0.25f, 0, 0), debugTip + new Vector3(0.25f, 0, 0), Color.magenta);

		string hitName = hitTransform == null ? "NULL" : hitTransform.name;

		Handles.Label(transform.position + debugTextOffset,
			$"Distance From Player: {playerDist}\n" +
			$"Local Scale: {transform.localScale.y}\n" +
			$"Ratio: {scaleRatio}\n" +
			$"Size: {scale}\n" +
			$"Object Hit: {hitName}");
	}

	#endif

	// function that finds the bottom most child
	private Transform FindBottomMostChild()
	{
		// initialize the return value
		Transform ret = null;

		// Get all children transforms and remove self from this list
		List<Transform> transforms = GetComponentsInChildren<Transform>().ToList();
		transforms.Remove(transform);

		// if it has at least one, we set the first value as the default
		// return value
		if (transforms.Count > 0)
		{
			ret = transforms[0];
		}

		// now loop through the rest of the transforms to find the child transform
		// with the smallest tip
		foreach (Transform obj in transforms)
		{
			if (GetTip(ret).y > GetTip(obj).y)
			{
				ret = obj;
			}
		}

		return ret;
	}

	// check if there's any other object between the symbiote and the player
	private (bool HitSomething, bool HitPlayer, Vector3 HitPoint) DirectLOSToPlayer()
	{
		// perfrom a physics raycast from the tip of the symbiote in the
		// direction it's looking at to see if the player is in direct line
		// of sight
		RaycastHit hit;
		if(Physics.Raycast(GetTip(), -transform.up, out hit))
		{
			hitTransform = hit.transform;
			bool hitPlayer = hit.transform.CompareTag("Player");
			return (true, hitPlayer, hit.point);
		}

		hitTransform = null;
		return (false, false, Vector3.zero);
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

#if UNITY_EDITOR

// Custom editor to fix the pivots on scaling
[CanEditMultipleObjects]
[CustomEditor(typeof(Symbiote))]
public class SymbioteEditor : Editor
{
	public override void OnInspectorGUI()
	{
		// draw the default editor first
		DrawDefaultInspector();
		GUILayout.Space(10);

		// draw a button that fixes the pivots of the child object
		if(GUILayout.Button("Fix children pivots", GUILayout.Height(25)))
		{
			Symbiote symbiote = target as Symbiote;
			symbiote.FixChildrenPivot();
		}

		// draw a button to toggle debug draw feature
		GUILayout.Space(5);
		if(GUILayout.Button("Toggle debug draw", GUILayout.Height(25)))
		{
			Symbiote symbiote = target as Symbiote;
			symbiote.ToggleDebugDraw();
		}
	}
}

#endif
