using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;
using UnityEngine;
using KSP;
using KSP.IO;
using KSP.UI.Screens;

namespace Kerbulator {
	#region Starter Classes
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class KerbulatorFlight : GameGlue { }
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class KerbulatorEditor : GameGlue { }
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class KerbulatorSpaceCenter : GameGlue { }
    [KSPAddon(KSPAddon.Startup.TrackingStation, false)]
    public class KerbulatorTrackingStation : GameGlue { }
	#endregion

	/// <summary>Glue code when plugin is loaded in KSP and game
	/// assembly is available.</summary>
	public class GameGlue : MonoBehaviour, IGlue {
		private KerbulatorGUI gui = null;
		private ApplicationLauncherButton mainButton = null;
		private IButton blizzyButton = null;
		private bool mainWindowEnabled = false;
		private KerbulatorOptions options = null;
		private Dictionary<string, bool> locks = new Dictionary<string, bool>();

		/// <summary>Called by Unity when the Plugin is loaded</summary>
		void Awake() {
			Debug.Log("[Kerbulator] Start");
			options = LoadConfig();
			gui = new KerbulatorGUI(this, false, false, options);

			if(!ToolbarManager.ToolbarAvailable) {
				GameEvents.onGUIApplicationLauncherReady.Add(InitToolbarButton);
				GameEvents.onGUIApplicationLauncherUnreadifying.Add(OnGuiApplicationLauncherUnreadifying);
			}

			Debug.Log("[Kerbulator] Start done");
		}

		void Start() {
			if(ToolbarManager.ToolbarAvailable) {
				// Create a toolbar button using Blizzy's toolbar
				InitBlizzyButton();
			}

			KACWrapper.InitKACWrapper();
		}

		void OnDisable() {
			SaveConfig();
		}

		void SaveConfig() {
			// Save config
			PluginConfiguration config = KSP.IO.PluginConfiguration.CreateForType<Kerbulator>(null);

			config["mainWindowX"] = (int)options.mainWindowPos.x;
			config["mainWindowY"] = (int)options.mainWindowPos.y;
			config["mainWindowWidth"] = (int)options.mainWindowPos.width;
			config["mainWindowHeight"] = (int)options.mainWindowPos.height;
			config["editWindowX"] = (int)options.editWindowPos.x;
			config["editWindowY"] = (int)options.editWindowPos.y;
			config["editWindowWidth"] = (int)options.editWindowPos.width;
			config["editWindowHeight"] = (int)options.editWindowPos.height;
			config["runWindowX"] = (int)options.runWindowPos.x;
			config["runWindowY"] = (int)options.runWindowPos.y;
			config["runWindowWidth"] = (int)options.runWindowPos.width;
			config["runWindowHeight"] = (int)options.runWindowPos.height;
			config["repeatWindowX"] = (int)options.repeatWindowPos.x;
			config["repeatWindowY"] = (int)options.repeatWindowPos.y;
			config["repeatWindowWidth"] = (int)options.repeatWindowPos.width;
			config["repeatWindowHeight"] = (int)options.repeatWindowPos.height;

			config.save();
		}

		KerbulatorOptions LoadConfig() {
			PluginConfiguration config = KSP.IO.PluginConfiguration.CreateForType<Kerbulator>(null);
			config.load();

			options = new KerbulatorOptions();

			try {
				options.mainWindowPos.x = config.GetValue<int>("mainWindowX", 0);
				options.mainWindowPos.y = config.GetValue<int>("mainWindowY", 60);
				options.mainWindowPos.width = config.GetValue<int>("mainWindowWidth", 280);
				options.mainWindowPos.height = config.GetValue<int>("mainWindowHeight", 400);
				options.editWindowPos.x = config.GetValue<int>("editWindowX", 280);
				options.editWindowPos.y = config.GetValue<int>("editWindowY", 60);
				options.editWindowPos.width = config.GetValue<int>("editWindowWidth", 500);
				options.editWindowPos.height = config.GetValue<int>("editWindowHeight", 400);
				options.runWindowPos.x = config.GetValue<int>("runWindowX", 0);
				options.runWindowPos.y = config.GetValue<int>("runWindowY", 470);
				options.runWindowPos.width = config.GetValue<int>("runWindowWidth", 200);
				options.runWindowPos.height = config.GetValue<int>("runWindowHeight", 200);
				options.repeatWindowPos.x = config.GetValue<int>("repeatWindowX", 200);
				options.repeatWindowPos.y = config.GetValue<int>("repeatWindowY", 470);
				options.repeatWindowPos.width = config.GetValue<int>("repeatWindowWidth", 200);
				options.repeatWindowPos.height = config.GetValue<int>("repeatWindowHeight", 100);
			} catch(ArgumentException) {
			}

			return options;
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

		public void OnGuiApplicationLauncherUnreadifying(GameScenes SceneToLoad) {
			RemoveToolbarButton();
		}

		public void RemoveToolbarButton() {
			Debug.Log("[Kerbulator] RemoveToolbarButton");

			if(!ToolbarManager.ToolbarAvailable) {
				GameEvents.onGUIApplicationLauncherReady.Remove(InitToolbarButton);
				GameEvents.onGUIApplicationLauncherUnreadifying.Remove(OnGuiApplicationLauncherUnreadifying);
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

			Vector3d dV = new Vector3d(dr, dn, dp);

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

			RemoveToolbarButton();
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

		public string GetFunctionDir() {
			return KSPUtil.ApplicationRootPath + "/PluginData/Kerbulator";
		}

		public void PreventClickthrough(Rect windowRect, string lockName) {
			Vector2 mousePos = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
			bool cursorOnWindow = windowRect.Contains(mousePos);
			if(cursorOnWindow) {
				if(HighLogic.LoadedSceneIsEditor) {
					EditorLogic.fetch.Lock(true, true, true, lockName);
				} else {
					InputLockManager.SetControlLock(ControlTypes.All, lockName);
				}
			} else {
				if(HighLogic.LoadedSceneIsEditor) {
					EditorLogic.fetch.Unlock(lockName);
				} else {
					InputLockManager.RemoveControlLock(lockName);
				}
			}
		}

		public void EnsureLockReleased(string lockName) {
			if(HighLogic.LoadedSceneIsEditor) {
				EditorLogic.fetch.Unlock(lockName);
			} else {
				InputLockManager.RemoveControlLock(lockName);
			}
		}
	}
}
