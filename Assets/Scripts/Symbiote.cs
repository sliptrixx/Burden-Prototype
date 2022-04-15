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

	[Tooltip("The speed at which the symbiote shrinks")]
	[SerializeField] float ShrinkSpeed = 40.0f;

	[Tooltip("The speed at which the symbiote travels in projectile mode")]
	[SerializeField] float ProjectileSpeed = 1.0f;

	[Header("Optional")]
	[SerializeField] bool FollowPlayer = false;

	// The status of the symbiote
	Status status = Status.NOT_ATTRACTED;

	// A reference to the player that the symbiote will attract to
	Transform player;

	// A reference to the child sphere mesh
	Transform child;

	// A reference to the bottom most child
	Transform bottom;

	// the default scale of the object on the x-axis
	float scaleRatio = 1.0f;

	// the area of the symbiote
	float area = 1.0f;

	private (bool HitSomething, bool HitPlayer, Vector3 HitPoint) playerLOSData;

	bool CollidedWithPlayer = false;

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

		// move towards the player as a projectile
		if (status == Status.PROJECTILE)
		{
			// when requested follow the player, the symbiote will active
			// adjust it's rotation to follow the player
			if(FollowPlayer) { LookAtPlayer(); }

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

		// check if the symbiote is attracted to the player
		float dist = Vector3.Distance(transform.position, player.position);
		LookAtPlayer();

		// update the status based on the distance
		if (dist <= AttractionRadius && playerLOSData.HitPlayer)
		{
			status = Status.ATTRACTED;
		}
		else 
		{ 
			status = Status.NOT_ATTRACTED;
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
		playerLOSData = DirectLOSToPlayer();
	}

	private void OnCollisionEnter(Collision collision)
	{
		if(status == Status.PROJECTILE)
		{
			status = Status.DONE;
			CollidedWithPlayer = collision.gameObject.CompareTag("Player");
		}
	}

	// Stretch towards the player
	private void StretchTowardsPlayer(float dist)
	{
		float stretch = dist.Remap(AttractionRadius, SnapRadius, 1, scaleRatio);
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

	private Transform hitTransform = null;
	private Vector3 debugTextOffset = new Vector3(1, -0.5f, 0);
	private void OnDrawGizmos()
	{
		if (!isDebugDrawActive) { return; }

		if(!Application.isPlaying)
		{
			Debug.DrawRay(transform.position, -transform.up * AttractionRadius, Color.green);
			Debug.DrawRay(transform.position, -transform.up * SnapRadius, Color.blue);
			Debug.DrawRay(transform.position, -transform.up * MaxSize, Color.red);

			return;
		}

		if(player == null) { return; }

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
		RaycastHit hit;
		if(Physics.Raycast(GetTip(), -transform.up, out hit))
		{
			hitTransform = hit.transform;
			if(hit.transform.CompareTag("Player"))
			{
				return (true, true, hit.point);
			}

			return (true, false, hit.point);
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

		GUILayout.Space(5);
		if(GUILayout.Button("Toggle debug draw", GUILayout.Height(25)))
		{
			Symbiote symbiote = target as Symbiote;
			symbiote.ToggleDebugDraw();
		}
	}
}

#endif
