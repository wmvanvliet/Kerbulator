using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace Kerbulator {
	/// <summary>Glue code to smooth over differences when plugin is loaded in
	/// the Unity editor versus when it is loaded in the actual game.</summary>
	public interface IGlue {
		void PlaceNode(List<string> ids, List<System.Object> output);
		void AddGlobals(Kerbulator kalc);
		Texture2D GetTexture(string id);
		void ChangeState(bool open);
	}

	public class KerbulatorGUI {
		// Error reporting
		string error = null;

		// Selecting functions and getting info
		JITFunction selectedFunction = null;
		string functionDescription = "";
		float functionDescriptionHeight = 0;

		// Editing functions
		JITFunction editFunction = null;
		string editFunctionContent = "";
		string editFunctionName = "maneuver";
		string functionFile = "maneuver.math";
		string maneuverTemplate = "out: Δv_r\nout: Δv_n\nout: Δv_p\nout: Δt\n\nΔv_r = 0\nΔv_n = 0\nΔv_p = 0\nΔt = 0";
		
		// Running functions
		JITFunction runFunction = null;
		JITFunction prevRunFunction = null;
		string functionOutput = "";
		ExecutionEnvironment env = null;
		List<string>arguments = new List<string>();

		// For dragging windows
		Rect titleBarRect = new Rect(0,0, 10000, 20);

		// Different scrollbars
		Vector2 mainScrollPos = new Vector2(0, 0);
		Vector2 editorScrollPos = new Vector2(0, 0);
		Vector2 runScrollPos = new Vector2(0, 0);

		// Main Kerbulator instance
		Kerbulator kalc;

		// Main button position
		bool drawMainButton = false;
		Rect mainButtonPos = new Rect(190, 0, 32, 32);

		// Window positions
		Rect mainWindowPos = new Rect(0, 60, 280, 400);
		bool mainWindowEnabled = false;
		Rect editWindowPos = new Rect(280, 60, 350, 300);
		bool editWindowEnabled = false;
		Rect runWindowPos = new Rect(0, 470, 200, 200);
		bool runWindowEnabled = false;

		// Dictionary containing all available functions
		Dictionary<string, JITFunction> functions = new Dictionary<string, JITFunction>();
		string functionDir = "";

		bool reload = false;

		// Math symbols
        string[] greekLetters = new[] {"α","β","γ","δ","ε","ζ","η","θ","ι","κ","λ","μ","ν","ξ","ο","π","ρ","σ","τ","υ","φ","χ","ψ","ω"};
		string[] greekUCLetters = new[] {"Α","Β","Γ","Δ","Ε","Ζ","Η","Θ","Ι","Κ","Λ","Μ","Ν","Ξ","Ο","Π","Ρ","Σ","Τ","Υ","Φ","Χ","Ψ","Ω"};
		string[] symbols = new[] {"=","+","-","*","×","·","/","÷","%", "√","^","(",")","[","]","{","}","⌊","⌋","⌈","⌉"};

		// GUI styles in use
		bool stylesInitiated = false;
		GUIStyle keyboard;
		GUIStyle defaultButton;

		IGlue glue;
		bool inEditor = false;

		// Icons
		Texture2D kerbulatorIcon;
		Texture2D editIcon;
		Texture2D runIcon;
		Texture2D saveIcon;
		Texture2D deleteIcon;
		Texture2D nodeIcon;

		public KerbulatorGUI(IGlue glue, bool inEditor, bool drawMainButton) {
			this.glue = glue;
			this.inEditor = inEditor;
			this.drawMainButton = drawMainButton;
			ChangeState(false);

			functionDir = Application.persistentDataPath + "/Kerbulator";

			// Sometimes, Application.persistentDataPath returns an empty string.
			// To not completely crash, create a KerbulatorFunctions directory in the users home dir
			if(functionDir == "/Kerbulator") {
				string homePath =
                    (Environment.OSVersion.Platform == PlatformID.Unix || 
                     Environment.OSVersion.Platform == PlatformID.MacOSX)
					 ? Environment.GetEnvironmentVariable("HOME")
					 : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%");
				functionDir = homePath +"/KerbulatorFunctions";
			}

			Debug.Log("Kerbulator function dir: "+ functionDir);

			editFunctionContent = maneuverTemplate;

			if(!Directory.Exists(functionDir)) {
				Directory.CreateDirectory(functionDir);
			}
			
			// Load icons
			kerbulatorIcon = glue.GetTexture("kerbulator");
			editIcon = glue.GetTexture("edit");
			runIcon = glue.GetTexture("run");
			nodeIcon = glue.GetTexture("node");
			saveIcon = glue.GetTexture("save");
			deleteIcon = glue.GetTexture("delete");

			kalc = new Kerbulator(functionDir);
			Scan();
		}

        public void ChangeState(bool open) {
            mainWindowEnabled = open;
            glue.ChangeState(open);
        }

		/// <summary>Draws the GUI</summary>
		public void OnGUI() {
			// Initiate styles
			if(!stylesInitiated) {
				keyboard = new GUIStyle(GUI.skin.GetStyle("button"));
				keyboard.padding = new RectOffset(0,0,2,2);

				defaultButton = new GUIStyle(GUI.skin.GetStyle("button"));
				defaultButton.padding = new RectOffset(4,4,4,4);
			}

			if(drawMainButton) {
				// Draw the main button
				if(GUI.Button(mainButtonPos, kerbulatorIcon, defaultButton)) {
					ChangeState(!mainWindowEnabled);
				}
			}

			if(reload) {
				// Rebuild the list of functions. It could be that they were edited outside of KSP
				JITFunction.Scan(functionDir, kalc);

				// Reload the function being edited
				if(editFunction != null)
					editFunctionContent = System.IO.File.ReadAllText(functionFile);

				reload = false;
			}

			// Draw the windows (if enabled)
			if(mainWindowEnabled) {
				mainWindowPos = GUILayout.Window(93841, mainWindowPos, DrawMainWindow, "Kerbulator", GUILayout.ExpandHeight(false));
			}

			if(editWindowEnabled) {
				editWindowPos = GUILayout.Window(93842, editWindowPos, DrawEditWindow, "Function Editor", GUILayout.ExpandHeight(false));
			}

			if(runWindowEnabled) {
				runWindowPos = GUILayout.Window(93843, runWindowPos, DrawRunWindow, "Run "+ RunFunction.Id, GUILayout.ExpandHeight(false));
			}
		}

		/// <summary>Draws the main window that displays a list of available functions</summary>
		/// <param name="id">An unique number indentifying the window</param>
		public void DrawMainWindow(int id) {
			// Close button at the top right corner
			ChangeState(!GUI.Toggle(new Rect(mainWindowPos.width - 25, 0, 20, 20), !mainWindowEnabled, ""));

			if(error != null)
				GUILayout.Label(error);

			GUILayout.Label("Available functions:");

			mainScrollPos = GUILayout.BeginScrollView(mainScrollPos, false, true, GUILayout.Height(300));

			bool runSomething = false;

			foreach(KeyValuePair<string, JITFunction> f in kalc.Functions) {
				GUILayout.BeginHorizontal();

				if(GUILayout.Button(f.Key, GUILayout.Height(24))) { 
					selectedFunction = f.Value;
					functionDescription = FunctionDescription(f.Value);
					functionDescriptionHeight = GUI.skin.GetStyle("label").CalcHeight(new GUIContent(functionDescription), 225);
				}

				if(GUILayout.Button(editIcon, defaultButton, GUILayout.Width(24), GUILayout.Height(24))) {
					// Load the function to be edited
					editFunction = f.Value;
					functionFile = functionDir +"/"+ f.Key +".math";
					editFunctionName = f.Key;
					editFunctionContent = System.IO.File.ReadAllText(functionFile);
					editWindowEnabled = true;
				}

				if(GUILayout.Button(runIcon, defaultButton, GUILayout.Width(24), GUILayout.Height(24))) {
					// Load the function to be run
					RunFunction = f.Value;
					runWindowEnabled = true;

					// Run it, but only after this loop finishes.
					// Run() calls Scan(), which updates the dictionary we're currently
					// enumerating over, which is not allowed.
					runSomething = true;
				}

				GUILayout.EndHorizontal();

				// When a function is selected, display some info
				if(selectedFunction == f.Value) {
					GUILayout.Label("Function info:");
					GUILayout.Label(functionDescription, GUILayout.Width(225), GUILayout.Height(functionDescriptionHeight)); 
				}
			}

			GUILayout.EndScrollView();

			GUILayout.Space(20);	

			GUILayout.BeginHorizontal();
			if(GUILayout.Button("New function")) {
				editFunction = null;

				// Load template for an empty function
				functionFile = functionDir +"/unnamed.math";
				editFunctionName = "unnamed";
				editFunctionContent = "";
				editWindowEnabled = true;
			}

			if(GUILayout.Button("New maneuver")) {
				editFunction = null;

				// Load template for a function controlling a maneuver node
				functionFile = functionDir +"/maneuver.math";
				editFunctionName = "maneuver";
				editFunctionContent = maneuverTemplate;
				editWindowEnabled = true;
			}

			GUILayout.EndHorizontal();

			GUI.DragWindow(titleBarRect);

			// Run button was pressed, run the function
			if(runSomething) {
				List<System.Object> output = Run();
				functionOutput = FormatOutput(output);
			}
		}
		
		/// <summary>Draws the edit window that allows basic text editing.</summary>
		/// <param name="id">An unique number indentifying the window</param>
		public void DrawEditWindow(int id) {
			// Close button
			editWindowEnabled = !GUI.Toggle(new Rect(editWindowPos.width - 25, 0, 20, 20), !editWindowEnabled, "");

			GUILayout.BeginHorizontal();
			
			if(GUILayout.Button(deleteIcon, defaultButton, GUILayout.Width(25), GUILayout.Height(24))) {
				Delete();
			}

			editFunctionName = GUILayout.TextField(editFunctionName, GUILayout.Height(24));
			
			if(GUILayout.Button(saveIcon, defaultButton, GUILayout.Width(24), GUILayout.Height(24))) {
				Save();

				if(editFunction == null)
					editFunction = kalc.Functions[editFunctionName];
			}

			if(GUILayout.Button(runIcon, defaultButton, GUILayout.Width(24), GUILayout.Height(24))) {
				// Save it
				Save();

				// Load the function to be run
				RunFunction = editFunction;
				runWindowEnabled = true;

				// Run it
				List<System.Object> output = Run();
				functionOutput = FormatOutput(output);
			}

			GUILayout.EndHorizontal();

			editorScrollPos = GUILayout.BeginScrollView(editorScrollPos, false, true, GUILayout.Height(200), GUILayout.Width(460));
			editFunctionContent = GUILayout.TextArea(editFunctionContent, GUILayout.ExpandWidth(true));
			TextEditor editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
			GUILayout.EndScrollView();

			GUILayout.BeginHorizontal();
			foreach(string s in greekLetters) {
				if(GUILayout.Button(s, keyboard, GUILayout.Width(15))) {
					editFunctionContent = editFunctionContent.Insert(editor.pos, s);
					editor.pos ++;
					editor.selectPos ++;
				}
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			foreach(string s in greekUCLetters) {
				if(GUILayout.Button(s, keyboard, GUILayout.Width(15))) {
					editFunctionContent = editFunctionContent.Insert(editor.pos, s);
					editor.pos ++;
					editor.selectPos ++;
				}
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			foreach(string s in symbols) {
				if(GUILayout.Button(s, keyboard, GUILayout.Width(15))) {
					editFunctionContent = editFunctionContent.Insert(editor.pos, s);
					editor.pos ++;
					editor.selectPos ++;
				}
			}
			GUILayout.EndHorizontal();

			GUI.DragWindow(titleBarRect);
		}

		/// <summary>Draws the run window that allows execution of a function.</summary>
		/// <param name="id">An unique number indentifying the window</param>
		public void DrawRunWindow(int id) {
			// Close button
			runWindowEnabled = !GUI.Toggle(new Rect(runWindowPos.width - 25, 0, 20, 20), !runWindowEnabled, "");
			runScrollPos = GUILayout.BeginScrollView(runScrollPos, GUILayout.Height(170));

			if(RunFunction == null) {
				GUILayout.Label("ERROR: no function selected.");
			} else if(RunFunction.InError) {
				GUILayout.Label("ERROR: "+ RunFunction.ErrorString);
			} else {
				if(RunFunction.Ins.Count > 0) {
					GUILayout.Label("Inputs:");
					for(int i=0; i<arguments.Count; i++) {
						GUILayout.BeginHorizontal();
						GUILayout.Label(RunFunction.Ins[i]);
						arguments[i] = GUILayout.TextField(arguments[i], GUILayout.Width(150));
						GUILayout.EndHorizontal();
					}

					/*
					Vector2 mousePos = Event.current.mousePosition;
					GUI.Label(new Rect(mousePos.x, mousePos.y -100, 100, 20), GUI.tooltip);
					*/
				}

				GUILayout.BeginHorizontal();

				if(GUILayout.Button(runIcon, defaultButton, GUILayout.Height(32))) {
					List<System.Object> output = Run();
					functionOutput = FormatOutput(output);
				}
				
				if(GUILayout.Button(nodeIcon, defaultButton, GUILayout.Height(32))) {
					List<System.Object> output = Run();
					glue.PlaceNode(RunFunction.Outs, output);
					functionOutput = FormatOutput(output);
				}

				GUILayout.EndHorizontal();

				GUILayout.Label(functionOutput);
			}

			GUILayout.EndScrollView();
			GUI.DragWindow(titleBarRect);
		}

		/// <summary>Run a function.</summary>
		/// <param name="f">The function to run</param>
		public List<System.Object> Run() {
			Debug.Log("Running "+ RunFunction.Id);

			if(RunFunction == editFunction)
				Save();

			foreach(string arg in arguments) {
				if(arg == "")
					return null;
			}

			glue.AddGlobals(kalc);

			Debug.Log("Creating new env");
			env = new ExecutionEnvironment(RunFunction, kalc);
			Debug.Log("Setting args");
			env.SetArguments(arguments);
			Debug.Log("Execute()!");
			return env.Execute();
		}

		/// <summary>Save the current function being edited.</summary>
		public void Save() {
			if(editFunction != null && editFunction.Id != editFunctionName) {
				// Changing function name, remove old function
				string oldFunctionFile = functionDir +"/"+ editFunction.Id +".math";
				if(System.IO.File.Exists(oldFunctionFile)) {
					try {
						System.IO.File.Delete(oldFunctionFile);
					} catch(Exception e) {
						error = "Cannot save function: "+ e.Message;
						return;
					}
				}

				kalc.Functions.Remove(editFunction.Id);

				if(selectedFunction != null && selectedFunction.Id == editFunction.Id) {
					selectedFunction = null;
				}

				if(RunFunction != null && RunFunction.Id == editFunction.Id)
					RunFunction = null;
			}

			// Save new function
			try {
				functionFile = functionDir +"/"+ editFunctionName +".math";
				System.IO.File.WriteAllText(functionFile, editFunctionContent);
			} catch(Exception e) {
				error = "Cannot save function: "+ e.Message;
				return;
			}

			// Compile new function
			JITFunction f = JITFunction.FromFile(functionFile, kalc);
			f.Compile();
			if(RunFunction != null && RunFunction.Id == f.Id)
				RunFunction = f;

			if(!kalc.Functions.ContainsKey(editFunctionName))
				kalc.Functions.Add(editFunctionName, f);
			else
				kalc.Functions[editFunctionName] = f;

            editFunction = f;
		}

		/// <summary>Delete the current function being edited.</summary>
		public void Delete() {
			if(editFunction != null) {
				string oldFunctionFile = functionDir +"/"+ editFunction.Id +".math";
				if(System.IO.File.Exists(oldFunctionFile)) {
					try {
						System.IO.File.Delete(oldFunctionFile);
					} catch(Exception e) {
						error = "Cannot save function: "+ e.Message;
						return;
					}
				}

				if(selectedFunction != null && selectedFunction.Id == editFunction.Id) {
					selectedFunction = null;
				}

				if(RunFunction != null && RunFunction.Id == editFunction.Id)
					RunFunction = null;

				kalc.Functions.Remove(editFunction.Id);
			}

			editFunction = null;
			editWindowEnabled = false;
		}

		/// <summary>Obtain some info of a function.</summary>
		/// <param name="f">The function to obtain the info of</param>
		public string FunctionDescription(JITFunction f) {
			if(f.InError)
				return "ERROR: "+ f.ErrorString;

			string desc = "";

			if(f.Ins.Count == 0) {
				desc += "Inputs: none.";
			} else {
				desc += "Inputs:\n";
				for(int i=0; i<f.Ins.Count; i++) {
					desc += f.Ins[i];
					if(i < f.InDescriptions.Count)
						desc += ": "+ f.InDescriptions[i];
					desc += "\n";
				}
			}

			if(f.Outs.Count == 0) {
				desc += "\nOutputs: none.";
			} else {
				desc += "\nOutputs:\n";
				for(int i=0; i<f.Outs.Count; i++) {
					desc += f.Outs[i];
					if(i < f.OutDescriptions.Count)
						desc += ": "+ f.OutDescriptions[i];
					desc += "\n";
				}
			}

			return desc;
		}

		/// <summary>Provide a string representation of the output resulting from executing a function.</summary>
		/// <param name="f">The function that was executed</param>
		/// <param name="output">The variables resuting from the execution</param>
		public string FormatOutput(List<System.Object> output) {
			if(env == null)
				return "";

			if(env.InError)
				return "ERROR: "+ env.ErrorString;

			if(output == null)
				return "";

			string desc = "Outputs:\n";
			if(output.Count == 0) {
				desc += "None.";
			} else {
				for(int i=0; i<output.Count; i++) {
					desc += env.Function.Outs[i]+" = "+ Kerbulator.FormatVar(output[i]) +"\n";
				}
			}

			return desc;
		}

		/// <summary>Called by Unity when the game gains or loses focus.</summary>
		/// <param name="focused">True when the game gained focues, otherwise false</summary>
		public void OnApplicationFocus(bool focused) {
			if(focused) {
				// Coming back from another app.
				// Rebuild the list of functions, but do this later, in the GUI thread.
				reload = true;
			} else {
				Save();
			}
		}

		/// <summary>Scan for available functions and update funtion references if needed.</summary>
		public void Scan() {
			try {
				JITFunction.Scan(functionDir, kalc);
			} catch(Exception e) {
				error = "Cannot access function dir ("+ functionDir +"): "+ e.Message;
				functions = new Dictionary<string, JITFunction>();
				throw e;
			}

			if(selectedFunction != null) {
				if(functions.ContainsKey(selectedFunction.Id)) {
					selectedFunction = functions[selectedFunction.Id];
					functionDescription = FunctionDescription(selectedFunction);
				} else {
					selectedFunction = null;
					functionDescription = "";
				}

				functionDescriptionHeight = GUI.skin.GetStyle("label").CalcHeight(new GUIContent(functionDescription), 225);
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
				else
					runFunction = null;

				functionOutput = "";
			}
		}

		JITFunction RunFunction {
			get { return runFunction; }
			set {
				if(value == prevRunFunction) {
					return;
				} else {
					prevRunFunction = value;
				}

				if(value == null) {
					arguments = new List<string>();
					runWindowEnabled = false;
					env = null;
				} else {
					if(value.InError)
						arguments = new List<string>();
					else if(value.Id != prevRunFunction.Id || arguments.Count != prevRunFunction.Ins.Count) {
						float maxWidth = 0; 
						arguments = new List<string>(value.Ins.Count);
						foreach(string arg in value.Ins) {
							arguments.Add("");
							Vector2 size = GUI.skin.GetStyle("label").CalcSize(new GUIContent(arg));
							if(size.x > maxWidth)
								maxWidth = size.x;
						}
						runWindowPos = new Rect(runWindowPos.x, runWindowPos.y, maxWidth + 200, runWindowPos.height);
					}
				}

				runFunction = value;
				functionOutput = "";
			}
		}

		JITFunction EditFunction {
			get { return editFunction; }
			set {
				editFunction = value;
			}
		}
	}
}
