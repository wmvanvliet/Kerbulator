using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;
using UnityEngine;
using KSP;
using KSP.IO;

namespace Kerbulator {
	[KSPAddon(KSPAddon.Startup.EveryScene, false)]

	/// <summary>Glue code when plugin is loaded in KSP and game
	/// assembly is available.</summary>
	public class GameGlue : MonoBehaviour, IGlue {
		private KerbulatorGUI gui = null;
		private ApplicationLauncherButton mainButton = null;
		private IButton blizzyButton = null;
		private bool mainWindowEnabled = false;

		/// <summary>Called by Unity when the Plugin is loaded</summary>
		void Awake() {
			Debug.Log("[Kerbulator] Start");
			gui = new KerbulatorGUI(this, false, false);

			if(!ToolbarManager.ToolbarAvailable) {
				GameEvents.onGUIApplicationLauncherReady.Add(InitToolbarButton);
				GameEvents.onGameSceneLoadRequested.Add(RemoveToolbarButton);
			}

			Debug.Log("[Kerbulator] Start done");
		}

		void Start() {
			if(!ToolbarManager.ToolbarAvailable && mainButton == null) {
				InitToolbarButton();
			} else {
				InitBlizzyButton();
			}

			KACWrapper.InitKACWrapper();
		}

		/// <summary>Creates a toolbar button for KSP's toolbar</summary>
		void InitToolbarButton() {
			Debug.Log("[Kerbulator] InitToolbarButton");
			if(!ApplicationLauncher.Ready || mainButton != null)
				return;

			Debug.Log("[Kerbulator] AddModApplication");
			mainButton = ApplicationLauncher.Instance.AddModApplication(
				// Callback when enabled
				() => {
					gui.ChangeState(true);
				},

				// Callback when disabled
				() => {
					gui.ChangeState(false);
				},

				// Unused callbacks
				null,
				null,
				null,
				null,

				// Visible in these scenes
				ApplicationLauncher.AppScenes.ALWAYS,

				// Button texture
				GetTexture("kerbulator_38")
			);

			Debug.Log("[Kerbulator] Done!");

			//ApplicationLancher.Instance.AddOnShowCallback(() => {gui.ChangeState(true);});
			//ApplicationLancher.Instance.AddOnHideCallback(() => {gui.ChangeState(false);});
		}

		/// <summary>Creates a toolbar button for Blizzy's toolbar</summary>
		void InitBlizzyButton() {
			mainWindowEnabled = false;
			blizzyButton = ToolbarManager.Instance.add("Kerbulator", "Kerbulator");
			blizzyButton.TexturePath = "Kerbulator/Textures/kerbulator";
			blizzyButton.ToolTip = "Open a powerful calculator";
			blizzyButton.Visibility = new GameScenesVisibility(
				GameScenes.FLIGHT, GameScenes.SPACECENTER, GameScenes.EDITOR, GameScenes.TRACKSTATION
			);
			blizzyButton.OnClick += (e) => { gui.ChangeState(!mainWindowEnabled); };
		}

		public void RemoveToolbarButton(GameScenes SceneToLoad) {
			Debug.Log("[Kerbulator] RemoveToolbarButton");
			if(mainButton != null)
        		ApplicationLauncher.Instance.RemoveModApplication(mainButton);
			mainButton = null;
			if(gui != null)
				gui.ChangeState(false);
		}			

		/// <summary>Called by Unity to draw the GUI</summary>
		public void OnGUI() {
			//Debug.Log("[Kerbulator] OnGUI");
			if(gui != null)
				gui.OnGUI();
		}

        public void OnApplicationFocus(bool focused) {
			Debug.Log("[Kerbulator] OnApplicationFocus");
			if(gui != null)
				gui.OnApplicationFocus(focused);
        }

		/// <summary>Add/Update some useful globals</summary>
		public void AddGlobals(Kerbulator kalc) {
			Globals.Add(kalc); // UNITY
		}

		public void PlaceNode(List<string> ids, List<System.Object> output) { 
			double dr = 0, dn = 0, dp = 0;
			double UT = 0;

			// Look at the resulting variables and create a maneuver node with them
			for(int i=0; i<ids.Count; i++) {
				if(output[i].GetType() != typeof(double))
					continue;

				string id = ids[i];
				double val = (double) output[i];
				if(id == "Δv_r" || id == "dv_r")
					dr = val;
				else if(id == "Δv_n" || id == "dv_n")
					dn = val;
				else if(id == "Δv_p" || id == "dv_p")
					dp = val;
				else if(id == "Δt" || id == "dt")
					UT = val + Planetarium.GetUniversalTime();
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
			Debug.Log("[Kerbulator] Destroy");
			if(gui != null)
				gui.OnDestroy();
			gui = null;

			if(!ToolbarManager.ToolbarAvailable) {
				GameEvents.onGUIApplicationLauncherReady.Remove(InitToolbarButton);
				if(mainButton != null)
					ApplicationLauncher.Instance.RemoveModApplication(mainButton);
				mainButton = null;
			} else {
				if(blizzyButton != null) {
					blizzyButton.Destroy();
					blizzyButton = null;
				}
			}
		}

		public void ChangeState(bool open) {
            mainWindowEnabled = open;
		}

		public void RunAsCoroutine(IEnumerator f) {
			StartCoroutine(f);
		}

		public void AddAlarm(string name, List<string> ids, List<System.Object> output) {
			Debug.Log("[Kerbulator] AddAlarm " + name);
			if(KACWrapper.APIReady) {
				double UT = 0;

				// Look at the resulting variables and create an alarm with them
				for(int i=0; i<ids.Count; i++) {
					if(output[i].GetType() != typeof(double))
						continue;

					string id = ids[i];
					double val = (double) output[i];
					if(id == "Δt" || id == "dt")
						UT = val + Planetarium.GetUniversalTime();
					else if(id == "UT")
						UT = val;
				}

				// Create a raw alarm 
				String aID = KACWrapper.KAC.CreateAlarm(
					KACWrapper.KACAPI.AlarmTypeEnum.Raw, name, UT
				);
				 
				if(aID !="") {
					// If the alarm was made get the object so we can update it
					KACWrapper.KACAPI.KACAlarm a = KACWrapper.KAC.Alarms.First(z=>z.ID==aID);
					 
					// Now update some of the other properties
					a.Notes = "This alarm was placed by the Kerbulator function "+ name;
					a.AlarmAction = KACWrapper.KACAPI.AlarmActionEnum.KillWarp;
				}
			}
		}

		public bool CanAddNode() {
			return HighLogic.LoadedScene == GameScenes.FLIGHT;
		}

		public bool CanAddAlarm() {
			return KACWrapper.APIReady;
		}
	}
}
