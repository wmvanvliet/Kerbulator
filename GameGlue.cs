using System.Collections.Generic;
using System;
using UnityEngine;
using KSP.IO;

namespace Kerbulator {
	// Register plugin with KSP. Indicate that it only needs to be active during flight
	[KSPAddon(KSPAddon.Startup.Flight, false)]

	/// <summary>Glue code when plugin is loaded in KSP and game
	/// assembly is available.</summary>
	public class GameGlue : MonoBehaviour, IGlue {
		private KerbulatorGUI gui;
		private IButton mainButton;
		private bool guiEnabled = true;

		/// <summary>Called by Unity when the Plugin is started</summary>
		void Start() {
			if(ToolbarManager.ToolbarAvailable) {
				guiEnabled = false;
				mainButton = ToolbarManager.Instance.add("Kerbulator", "Kerbulator");
				mainButton.TexturePath = "Kerbulator/Textures/kerbulator";
				mainButton.ToolTip = "Open a powerful calculator";
                mainButton.Visibility = new GameScenesVisibility(GameScenes.FLIGHT);
				mainButton.OnClick += (e) => {
					guiEnabled = !guiEnabled;
				};
			}

			gui = new KerbulatorGUI(this, false, !ToolbarManager.ToolbarAvailable);
		}

		/// <summary>Called by Unity to draw the GUI</summary>
		public void OnGUI() {
			if(guiEnabled)
				gui.OnGUI();
		}

        public void OnApplicationFocus(bool focused) {
            gui.OnApplicationFocus(focused);
        }

		/// <summary>Add/Update some useful globals</summary>
		public void AddGlobals(Kerbulator kalc) {
			Globals.Add(kalc); // UNITY
		}
		public void PlaceNode(List<Variable> output) { 
			double dr = 0, dn = 0, dp = 0;
			double UT = 0;

			// Look at the resulting variables and create a maneuver node with them
			foreach(Variable var in output) {
				if(var.id == "Δv_r" || var.id == "dv_r")
					dr = var.val;
				else if(var.id == "Δv_n" || var.id == "dv_n")
					dn = var.val;
				else if(var.id == "Δv_p" || var.id == "dv_p")
					dp = var.val;
				else if(var.id == "Δt" || var.id == "dt")
					UT = var.val + Planetarium.GetUniversalTime();
			}

			Vector3d dV = new Vector3d(dr, -dn, dp);

			Vessel vessel = FlightGlobals.ActiveVessel;
			if(vessel == null)
				return;

			//placing a maneuver node with bad dV values can really mess up the game, so try to protect against that
			//and log an exception if we get a bad dV vector:
			for(int i = 0; i < 3; i++)
			{
				if(double.IsNaN(dV[i]) || double.IsInfinity(dV[i]))
				{
					throw new Exception("Kerbulator: bad dV: " + dV);
				}
			}

			if(double.IsNaN(UT) || double.IsInfinity(UT))
			{
				throw new Exception("Kerbulator: bad UT: " + UT);
			}

			//It seems that sometimes the game can freak out if you place a maneuver node in the past, so this
			//protects against that.
			UT = Math.Max(UT, Planetarium.GetUniversalTime());

			//convert a dV in world coordinates into the coordinate system of the maneuver node,
			//which uses (x, y, z) = (radial+, normal-, prograde)
			ManeuverNode mn = vessel.patchedConicSolver.AddManeuverNode(UT);
			mn.OnGizmoUpdated(dV, UT);
		}

		public Texture2D GetTexture(string id) {
			return GameDatabase.Instance.GetTexture("Kerbulator/Textures/"+ id, false);
		}

		/// <summary>Called by Unity when plugin is unloaded</summary>
		public void OnDestroy() {
			mainButton.Destroy();
		}
	}
}
