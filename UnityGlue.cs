using System.Collections.Generic;
using UnityEngine;

namespace Kalculator {
	/// <summary>Glue code when plugin is loaded in the Unity editor and KSP game
	/// assembly is not available.</summary>
	public class UnityGlue : MonoBehaviour, IGlue {
		private KalculatorGUI gui;

		/// <summary>Called by Unity when the Plugin is started</summary>
		void Start() {
			gui = new KalculatorGUI(this, true, true);
		}

		/// <summary>Called by Unity to draw the GUI</summary>
		public void OnGUI() {
			gui.OnGUI();
		}

		public void AddGlobals(Kalculator kalc) {
		}

		public void PlaceNode(List<Variable> output) {
		}

		public Texture2D GetTexture(string id) {
			return (Texture2D)Resources.Load(id);
		}
	}
}
