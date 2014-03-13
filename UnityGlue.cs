using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace Kerbulator {
	/// <summary>Glue code when plugin is loaded in the Unity editor and KSP game
	/// assembly is not available.</summary>
	public class UnityGlue : MonoBehaviour, IGlue {
		private KerbulatorGUI gui = null;

		/// <summary>Called by Unity when the Plugin is started</summary>
		void Start() {
			gui = new KerbulatorGUI(this, true, true);
		}

		/// <summary>Called by Unity to draw the GUI</summary>
		public void OnGUI() {
			gui.OnGUI();
		}

        public void OnApplicationFocus(bool focused) {
			if(gui != null)
				gui.OnApplicationFocus(focused);
        }

		public void AddGlobals(Kerbulator kalc) {
		}

		public void PlaceNode(List<string> ids, List<System.Object> output) {
		}

		public Texture2D GetTexture(string id) {
			return (Texture2D)Resources.Load(id);
		}

		public void ChangeState(bool open) {
		}

		public void RunAsCoroutine(IEnumerator f) {
			StartCoroutine(f);
		}

		public void OnDestroy() {
			if(gui != null)
				gui.OnDestroy();
		}
	}
}
