using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using KSP.IO; // UNITY

namespace Kalculator {
	// Register plugin with KSP. Indicate that it only needs to be active during flight
	[KSPAddon(KSPAddon.Startup.Flight, false)] // UNITY

	public class KalculatorGUI : MonoBehaviour {
		// Selecting functions and getting info
		private Function selectedFunction = null;
		private string functionDescription = "";
		private Vector2 functionDescriptionSize = new Vector2(0, 0);

		// Editing functions
		private Function editFunction = null;
		private string editFunctionContent = "";
		private string editFunctionName = "maneuver";
		private string functionFile = "maneuver.math";
		//private string oldFunctionName = "maneuver";
		//private string oldFunctionFile = "maneuver.math";
		private string maneuverTemplate = "out: Δv_r\nout: Δv_n\nout: Δv_p\nout: Δt\n\nΔv_r = 0\nΔv_n = 0\nΔv_p = 0\nΔt = 0";
		
		// Running functions
		private Function runFunction = null;
		private string functionOutput = "";

		// For dragging windows
		private Rect titleBarRect = new Rect(0,0, 10000, 20);

		// Different scrollbars
		private Vector2 mainScrollPos = new Vector2(0, 0);
		private Vector2 editorScrollPos = new Vector2(0, 0);

		// Main Kalculator instance
		private Kalculator kalc;

		// Main button position
		private Rect mainButtonPos = new Rect(200, 5, 100, 20);

		// Window positions
		private Rect mainWindowPos = new Rect(0, 40, 300, 0);
		private bool mainWindowEnabled = false;
		private Rect editWindowPos = new Rect(260, 40, 350, 0);
		private bool editWindowEnabled = false;
		private Rect runWindowPos = new Rect(0, 310, 200, 0);
		private bool runWindowEnabled = false;

		// Dictionary containing all available functions
		Dictionary<string, Function> functions;
		string functionDir = "";

		/// <summary>Called by Unity when the Plugin is started</summary>
		void Start () {
			functionDir = Application.persistentDataPath + "/Kalculator";
			editFunctionContent = maneuverTemplate;

			if(!Directory.Exists(functionDir)) {
				print("Dir does not exist");
				Directory.CreateDirectory(functionDir);
			} else {
				print(functionDir +" exists!");
			}
			
			Scan();
			kalc = new Kalculator(functionDir);
		}

		/// <summary>Called by Unity to draw the GUI</summary>
		public void OnGUI() {
			// Draw the main button
			if(GUI.Button(mainButtonPos, "Kalculator"))
				mainWindowEnabled = !mainWindowEnabled;

			// Draw the windows (if enabled)
			if(mainWindowEnabled) {
				mainWindowPos = GUILayout.Window(93841, mainWindowPos, drawMainWindow, "Kalculator", GUILayout.ExpandHeight(true));
				mainWindowPos.height = 0;
			}

			if(editWindowEnabled) {
				editWindowPos = GUILayout.Window(93842, editWindowPos, drawEditWindow, "Function Editor", GUILayout.ExpandHeight(true));
				editWindowPos.height = 0;
			}

			if(runWindowEnabled) {
				runWindowPos = GUILayout.Window(93843, runWindowPos, drawRunWindow, "Run "+ runFunction.Id, GUILayout.ExpandHeight(true));
				runWindowPos.height = 0;
			}
		}

		/// <summary>Draws the main window that displays a list of available functions</summary>
		/// <param name="id">An unique number indentifying the window</param>
		public void drawMainWindow(int id) {
			// Close button at the top right corner
			mainWindowEnabled = !GUI.Toggle(new Rect(mainWindowPos.width - 25, 0, 20, 20), !mainWindowEnabled, "");

			GUILayout.Label ("Available functions:");

			mainScrollPos = GUILayout.BeginScrollView(mainScrollPos, GUILayout.Height(200));

			foreach(KeyValuePair<string, Function> f in functions) {
				GUILayout.BeginHorizontal();

				if(GUILayout.Button(f.Key)) { 
					selectedFunction = f.Value;
					functionDescription = FunctionDescription(f.Value);
					functionDescriptionSize = GUI.skin.GetStyle("label").CalcSize(new GUIContent(functionDescription));
				}

				if(GUILayout.Button("Edit", GUILayout.Width(40))) {
					// Load the function to be edited
					editFunction = f.Value;
					functionFile = functionDir +"/"+ f.Key +".math";
					//oldFunctionFile = functionFile;
					editFunctionName = f.Key;
					//oldFunctionName = editFunctionName;
					editFunctionContent = System.IO.File.ReadAllText(functionFile);
					editWindowEnabled = true;
				}

				if(GUILayout.Button("Run", GUILayout.Width(40))) {
					// Load the function to be run
					functionOutput = "";
					runFunction = f.Value;
					runWindowEnabled = true;
					functionDescription = FunctionDescription(f.Value);
					functionDescriptionSize = GUI.skin.GetStyle("label").CalcSize(new GUIContent(functionDescription));
				}

				GUILayout.EndHorizontal();

				// When a function is selected, display some info
				if(selectedFunction == f.Value) {
					GUILayout.Label("Function info:");
					GUILayout.Label(functionDescription, GUILayout.Width(functionDescriptionSize.x), GUILayout.Height(functionDescriptionSize.y)); 
				}
			}

			GUILayout.EndScrollView();

			GUILayout.Space(20);	

			GUILayout.BeginHorizontal ();
			if(GUILayout.Button("New function")) {
				editFunction = null;
				// Load template for an empty function
				functionFile = functionDir +"/unnamed.math";
				//oldFunctionFile = functionFile;
				editFunctionName = "unnamed";
				//oldFunctionName = editFunctionName;
				editFunctionContent = "";
				editWindowEnabled = true;
			}

			if(GUILayout.Button("New maneuver")) {
				editFunction = null;
				// Load template for a function controlling a maneuver node
				functionFile = functionDir +"/maneuver.math";
				//oldFunctionFile = functionFile;
				editFunctionName = "maneuver";
				//oldFunctionName = editFunctionName;
				editFunctionContent = maneuverTemplate;
				editWindowEnabled = true;
			}

			GUILayout.EndHorizontal ();

			GUI.DragWindow(titleBarRect);
		}
		
		/// <summary>Draws the edit window that allows basic text editing.</summary>
		/// <param name="id">An unique number indentifying the window</param>
		public void drawEditWindow(int id) {
			// Close button
			editWindowEnabled = !GUI.Toggle(new Rect(editWindowPos.width - 25, 0, 20, 20), !editWindowEnabled, "");

			GUILayout.BeginHorizontal ();
			
			if (GUILayout.Button ("Delete", GUILayout.MaxWidth(70))) {
				Delete();
			}

			editFunctionName = GUILayout.TextField(editFunctionName);
			
			if(GUILayout.Button("Save", GUILayout.MaxWidth(70))) {
				Save();

				if(editFunction == null)
					editFunction = functions[editFunctionName];
			}

			GUILayout.EndHorizontal ();

			editorScrollPos = GUILayout.BeginScrollView(editorScrollPos, GUILayout.Height(200), GUILayout.Width(400));
			editFunctionContent = GUILayout.TextArea(editFunctionContent, GUILayout.ExpandWidth(true));
			GUILayout.EndScrollView();

			GUI.DragWindow(titleBarRect);
		}

		/// <summary>Draws the run window that allows execution of a function.</summary>
		/// <param name="id">An unique number indentifying the window</param>
		public void drawRunWindow(int id) {
			// Close button
			runWindowEnabled = !GUI.Toggle(new Rect(runWindowPos.width - 25, 0, 20, 20), !runWindowEnabled, "");

			if(runFunction == null) {
				GUILayout.Label("ERROR: no function selected.");
			} else if(runFunction.InError) {
				GUILayout.Label("ERROR: "+ runFunction.ErrorString);
			} else {
				/*
				if(runFunction.Ins.Count == 0) {
					GUILayout.Label("Inputs: none.");
				} else {
					GUILayout.Label("Inputs:");
					for(int i=0; i<runFunction.Ins.Count; i++) {
						GUILayout.BeginHorizontal();
						GUILayout.Label(runFunction.Ins[i]);
						GUILayout.TextField("0");
						GUILayout.Label(runFunction.InDescriptions[i]);
						GUILayout.EndHorizontal();
					}

				}
				*/

				GUILayout.BeginHorizontal();

				if(GUILayout.Button("Run", GUILayout.MaxWidth(100))) {
					List<Variable> output = Run(runFunction);
					functionOutput = FormatOutput(runFunction, output);
				}
				
				if(GUILayout.Button("Make Node", GUILayout.MaxWidth(100))) {
					double dr = 0, dn = 0, dp = 0;
					double dt = 0;

					List<Variable> output = Run(runFunction);

					// Look at the resulting variables and create a maneuver node with them
					foreach(Variable var in output) {
						if(var.id == "Δv_r" || var.id == "dv_r")
							dr = var.val;
						else if(var.id == "Δv_n" || var.id == "dv_n")
							dn = var.val;
						else if(var.id == "Δv_p" || var.id == "dv_p")
							dp = var.val;
						else if(var.id == "Δt" || var.id == "dt")
							dt = var.val + Planetarium.GetUniversalTime(); // UNITY
					}

					PlaceManeuverNode(new Vector3d(dr, -dn, dp), dt); // UNITY
					functionOutput = FormatOutput(runFunction, output);
				}

				GUILayout.EndHorizontal ();

				GUILayout.Label(functionOutput);
			}

			GUI.DragWindow(titleBarRect);
		}


		/// <summary>Run a function.</summary>
		/// <param name="f">The function to run</param>
		public List<Variable> Run(Function f) {
			// UNITY
			Vessel v = FlightGlobals.ActiveVessel;
			if(v != null) {
				Orbit o = v.orbit;
				kalc.AddGlobal(new Variable("Altitude", VarType.NUMBER, o.altitude));
				kalc.AddGlobal(new Variable("Apoapsis", VarType.NUMBER, o.ApA));
				kalc.AddGlobal(new Variable("Inclination", VarType.NUMBER, o.inclination));
				kalc.AddGlobal(new Variable("OrbitalSpeed", VarType.NUMBER, o.orbitalSpeed));
				kalc.AddGlobal(new Variable("Periapsis", VarType.NUMBER, o.PeA));
				kalc.AddGlobal(new Variable("TimeToAp", VarType.NUMBER, o.timeToAp));
				kalc.AddGlobal(new Variable("TimeToPe", VarType.NUMBER, o.timeToPe));
				kalc.AddGlobal(new Variable("VelocityX", VarType.NUMBER, o.vel[0]));
				kalc.AddGlobal(new Variable("VelocityY", VarType.NUMBER, o.vel[2]));
				kalc.AddGlobal(new Variable("VelocityZ", VarType.NUMBER, o.vel[1]));
				kalc.AddGlobal(new Variable("UT", VarType.NUMBER, Planetarium.GetUniversalTime()));
			}
			// /UNITY

			return kalc.Run(f);
		}

		// UNITY
		/// <summary>Place a maneuver node that represents a burn.</summary>
		/// <param name="dV">The dV of the burn in the directions (radial+, normal-, prograde)</param>
		/// <param name="UT">The absolute time at which to initiate the burn</param>
		private void PlaceManeuverNode(Vector3d dV, double UT)
		{
			Vessel vessel = FlightGlobals.ActiveVessel;
			if(vessel == null)
				return;

			//placing a maneuver node with bad dV values can really mess up the game, so try to protect against that
			//and log an exception if we get a bad dV vector:
			for (int i = 0; i < 3; i++)
			{
				if (double.IsNaN(dV[i]) || double.IsInfinity(dV[i]))
				{
					throw new Exception("MechJeb VesselExtensions.PlaceManeuverNode: bad dV: " + dV);
				}
			}

			if (double.IsNaN(UT) || double.IsInfinity(UT))
			{
				throw new Exception("MechJeb VesselExtensions.PlaceManeuverNode: bad UT: " + UT);
			}

			//It seems that sometimes the game can freak out if you place a maneuver node in the past, so this
			//protects against that.
			UT = Math.Max(UT, Planetarium.GetUniversalTime());

			//convert a dV in world coordinates into the coordinate system of the maneuver node,
			//which uses (x, y, z) = (radial+, normal-, prograde)
			ManeuverNode mn = vessel.patchedConicSolver.AddManeuverNode(UT);
			mn.OnGizmoUpdated(dV, UT);
		}
		// /UNITY

		/// <summary>Save the current function being edited.</summary>
		public void Save() {
			if(editFunction != null) {
				string oldFunctionFile = functionDir +"/"+ editFunction.Id +".math";
				Debug.Log(oldFunctionFile);
				if(System.IO.File.Exists(oldFunctionFile))
					System.IO.File.Delete(oldFunctionFile);
			}

			functionFile = functionDir +"/"+ editFunctionName +".math";
			System.IO.File.WriteAllText(functionFile, editFunctionContent);
			//oldFunctionFile = functionFile;

			Scan();
			/*
			functions = Function.Scan(functionDir);
			editFunction = functions[editFunctionName];

			if(runFunction != null && runFunction.Id == oldFunctionName) {
				runFunction = editFunction;
				functionOutput = "";
			}

			if(selectedFunction != null)
				selectedFunction = functions[selectedFunction.Id];

			functionDescription = FunctionDescription(editFunction);
			functionDescriptionSize = GUI.skin.GetStyle("label").CalcSize(new GUIContent(functionDescription));
			*/
		}

		/// <summary>Delete the current function being edited.</summary>
		public void Delete() {
			if(editFunction != null) {
				string oldFunctionFile = functionDir +"/"+ editFunction.Id +".math";
				if(System.IO.File.Exists(oldFunctionFile))
					System.IO.File.Delete(oldFunctionFile);
			}

			Scan();
			editWindowEnabled = false;

			/*
			functions = Function.Scan(functionDir);

			if(runFunction != null && runFunction.Id == oldFunctionName) {
				runFunction = null;
				functionOutput = "";
			}

			if(selectedFunction != null && selectedFunction.Id == oldFunctionName) {
				selectedFunction = null;
				functionDescription = "";
				functionDescriptionSize = GUI.skin.GetStyle("label").CalcSize(new GUIContent(functionDescription));
			}
			*/
		}

		/// <summary>Obtain some info of a function.</summary>
		/// <param name="f">The function to obtain the info of</param>
		public string FunctionDescription(Function f) {
			string desc = "";

			if(f.Ins.Count > 0) {
				desc += "Inputs:\n";
				for(int i=0; i<f.Ins.Count; i++) {
					desc += f.Ins[i];
					if(i < f.InDescriptions.Count)
						desc += ": "+ f.InDescriptions[i];
					desc += "\n";
				}

			}

			if(f.Outs.Count > 0) {
				if(f.Ins.Count > 0)
					desc += "\n";

				desc += "Outputs:\n";
				for(int i=0; i<f.Outs.Count; i++) {
					desc += f.Outs[i];
					if(i < f.OutDescriptions.Count)
						desc += ": "+ f.OutDescriptions[i];
					desc += "\n";
				}
			}

			if(f.InError)
				desc += "\nERROR: "+ f.ErrorString;

			return desc;
		}

		/// <summary>Provide a string representation of the output resulting from executing a function.</summary>
		/// <param name="f">The function that was executed</param>
		/// <param name="output">The variables resuting from the execution</param>
		public string FormatOutput(Function f, List<Variable> output) {
			string desc = "Outputs:\n";
			if(f.Outs.Count == 0) {
				desc += "None.";
			} else {
				for(int i=0; i<f.Outs.Count; i++) {
					desc += f.Outs[i];
					if(i < output.Count)
						desc += ": "+ output[i].ToString();
					desc += "\n";
				}
			}

			if(f.InError)
				desc += "\nERROR: "+ f.ErrorString;

			return desc;
		}

		/// <summary>Called by Unity when the game gains or loses focus.</summary>
		/// <param name="focused">True when the game gained focues, otherwise false</summary>
		public void OnApplicationFocus(bool focused) {
			if(focused) {
				// Rebuild the list of functions. It could be that they were edited outside of KSP
				functions = Function.Scan(functionDir);

				// Reload the function being edited
				if(editFunction != null)
					editFunctionContent = System.IO.File.ReadAllText(functionFile);
			}
		}

		/// <summary>Scan for available functions and update funtion references if needed.</summary>
		public void Scan() {
			functions = Function.Scan(functionDir);

			if(selectedFunction != null) {
				if(functions.ContainsKey(selectedFunction.Id)) {
					selectedFunction = functions[selectedFunction.Id];
					functionDescription = FunctionDescription(selectedFunction);
				} else {
					selectedFunction = null;
					functionDescription = "";
				}

				functionDescriptionSize = GUI.skin.GetStyle("label").CalcSize(new GUIContent(functionDescription));
			}

			if(editFunction != null) {
				if(functions.ContainsKey(editFunction.Id))
					editFunction = functions[editFunction.Id];
				else {
					editFunction = null;
					editWindowEnabled = false;
				}
			}

			if(runFunction != null) {
				if(functions.ContainsKey(runFunction.Id))
					runFunction = functions[runFunction.Id];
				else {
					runFunction = null;
					runWindowEnabled = false;
				}
				functionOutput = "";
			}
		}
	}
}
