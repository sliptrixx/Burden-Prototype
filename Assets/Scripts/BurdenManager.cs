using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BurdenManager : MonoBehaviour
{
	[Tooltip("The color when the player is in dark state")]
	[SerializeField] Color LightColor;

	[Tooltip("The color when the player is in the light state")]
	[SerializeField] Color DarkColor;

	List<Symbiote> Symbiotes = new List<Symbiote>();

	public static BurdenManager Instance { get; protected set; }

	// Start is called before the first frame update
	void Start()
	{
		Instance = this;
		Symbiotes = FindObjectsOfType<Symbiote>().ToList();
	}
	
	// delete a reference from the list of symbiotes
	public void DeleteReference(Symbiote symbiote)
	{
		Symbiotes.Remove(symbiote);
	}

	public void SwapBurdenType(MovePlayer.PlayerType playerType)
	{
		Color colorToChange;
		if (playerType == MovePlayer.PlayerType.LIGHT)
		{
			colorToChange = DarkColor;
		}
		else
		{
			colorToChange = LightColor;
		}

		foreach(Symbiote symbiote in Symbiotes)
		{
			symbiote.SetColor(colorToChange);
		}
	}
}
