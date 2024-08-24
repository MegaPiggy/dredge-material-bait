using UnityEngine;
using Winch.Core;

namespace MaterialBait
{
	public class MaterialBait : MonoBehaviour
	{
		public void Awake()
		{
			WinchCore.Log.Debug($"{nameof(MaterialBait)} has loaded!");
		}
	}
}
