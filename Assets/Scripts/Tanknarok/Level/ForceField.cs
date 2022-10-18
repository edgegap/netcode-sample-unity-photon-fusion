using UnityEngine;

namespace FusionExamples.Tanknarok
{
	public class ForceField : MonoBehaviour
	{
		[SerializeField] Material forceFieldMaterial;

		[SerializeField] MeshRenderer meshRenderer;

		string[] toggleProperties = new string[4] {"_PLAYER1Toggle", "_PLAYER2Toggle", "_PLAYER3Toggle", "_PLAYER4Toggle"};
		string[] positionProperties = new string[4] {"_PositionPLAYER1", "_PositionPLAYER2", "_PositionPLAYER3", "_PositionPLAYER4"};

		void Awake()
		{
			forceFieldMaterial = new Material(forceFieldMaterial);
			meshRenderer.material = forceFieldMaterial;
		}

		void Update()
		{
			for (int i = 0; i < toggleProperties.Length; i++)
			{
				Player ply = PlayerManager.GetPlayerFromID(i);
				if (ply != null)
					forceFieldMaterial.SetVector(positionProperties[i], ply.transform.position);
				forceFieldMaterial.SetInt(toggleProperties[i], ply==null ? 0 : 1);
			}
		}
	}
}