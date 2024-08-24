using UnityEngine;

namespace MaterialBait
{
	public class Loader
	{
		/// <summary>
		/// This method is run by Winch to initialize your mod
		/// </summary>
		public static void Initialize()
		{
			var gameObject = new GameObject(nameof(MaterialBait));
			gameObject.AddComponent<MaterialBait>();
			GameObject.DontDestroyOnLoad(gameObject);
		}
	}
}