using System;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Linq;

namespace Kerbulator {
	/// <summary>Glue code to smooth over differences when plugin is loaded in
	/// the Unity editor versus when it is loaded in the actual game.</summary>
	public interface IGlue {
		void PlaceNodes(List<string> ids, List<object> maneuverNodes, List<System.Object> output);
		void AddGlobals(Kerbulator kalc);
		Texture2D GetTexture(string id);
		void ChangeState(bool open);
		void RunAsCoroutine(IEnumerator f);
		void AddAlarms(string name, List<string> ids, List<System.Object> alarms, List<System.Object> output);
		bool CanAddAlarm();
		bool CanAddNode();
		string GetFunctionDir();
		void PreventClickthrough(Rect r, string lockName);
		void EnsureLockReleased(string lockName);
	}

	public class KerbulatorGUI {
		// Error reporting
		string error = null;
		string functionNameError = null;

		// Tooltips
		string tooltip = "";

		// Selecting functions and getting info
		JITFunction selectedFunction = null;
		string functionDescription = "";
		float functionDescriptionHeight = 0;

		// Editing functions
		JITFunction editFunction = null;
		string editFunctionContent = "";
		string editFunctionName = "unnamed";
		string functionFile = "unnamed.math";
		string maneuverTemplate = "maneuver: node\n\nΔv_r = 0\nΔv_n = 0\nΔv_p = 0\nΔt = 0\n\nnode = [Δv_r, Δv_n, Δv_p, Δt]";

		// Running functions
		bool running = false;
		JITFunction runFunction = null;
		JITFunction prevRunFunction = null;
		string functionOutput = "";
		ExecutionEnvironment env = null;
		List<string>arguments = new List<string>();
		Dictionary<int, ExecutionEnvironment> envs = new Dictionary<int, ExecutionEnvironment>();

		// Unique window id
		int windowId = 93841;

		// For dragging windows
		Rect titleBarRect = new Rect(0,0, 10000, 20);

		// For resizing windows
		int resizing = 0;
		Rect resizeStart = new Rect();
		GUIContent gcDrag = new GUIContent("////", "Drag to resize window");

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
		Vector2 minMainWindowSize = new Vector2(280, 400);
		bool mainWindowEnabled = false;

		Vector2 minEditWindowSize = new Vector2(300, 200);
		bool editWindowEnabled = false;

		Vector2 minRunWindowSize = new Vector2(100, 100);
		bool runWindowEnabled = false;

		Vector2 minRepeatWindowSize = new Vector2(75, 75);

		// Dictionary containing all available functions
		Dictionary<string, JITFunction> functions = new Dictionary<string, JITFunction>();
		string functionDir = "";

		bool reload = false;

		// Math symbols
        string[] greekLetters = new[] {"α","β","γ","δ","ε","ζ","η","θ","ι","κ","λ","μ","ν","ξ","ο","π","ρ","σ","τ","υ","φ","χ","ψ","ω"};
		string[] greekUCLetters = new[] {"Α","Β","Γ","Δ","Ε","Ζ","Η","Θ","Ι","Κ","Λ","Μ","Ν","Ξ","Ο","Π","Ρ","Σ","Τ","Υ","Φ","Χ","Ψ","Ω"};
		string[] symbols = new[] {"×","·","÷","√","≤","≥","≠","¬","∧","∨","⌊","⌋","⌈","⌉"};

		// GUI styles in use
		bool stylesInitiated = false;
		GUIStyle keyboard;
		GUIStyle defaultButton;
		GUIStyle tooltipStyle;
		GUIStyle editFunctionStyle;
		GUIStyle validFunctionName;
		GUIStyle invalidFunctionName;

		IGlue glue;
		bool inEditor = false;

		KerbulatorOptions options = null;

		// Icons
		Texture2D kerbulatorIcon;
		Texture2D editIcon;
		Texture2D runIcon;
		Texture2D repeatIcon;
		Texture2D saveIcon;
		Texture2D deleteIcon;
		Texture2D nodeIcon;
		Texture2D alarmIcon;

		public KerbulatorGUI(IGlue glue, bool inEditor, bool drawMainButton, KerbulatorOptions options) {
			this.glue = glue;
			this.inEditor = inEditor;
			this.drawMainButton = drawMainButton;
			this.options = options;
			ChangeState(false);

			// Use the game base directory + PluginData as base folder for plugin data
			functionDir = glue.GetFunctionDir();
			functionDir = functionDir.Replace("\\", "/");

			Debug.Log("Kerbulator function dir: "+ functionDir);

			editFunctionContent = maneuverTemplate;

			if(!Directory.Exists(functionDir)) {
				Directory.CreateDirectory(functionDir);
			}

			// Load icons
			kerbulatorIcon = glue.GetTexture("kerbulator");
			editIcon = glue.GetTexture("edit");
			runIcon = glue.GetTexture("run");
			repeatIcon = glue.GetTexture("repeat");
			nodeIcon = glue.GetTexture("node");
			alarmIcon = glue.GetTexture("alarm");
			saveIcon = glue.GetTexture("save");
			deleteIcon = glue.GetTexture("delete");

			kalc = new Kerbulator(functionDir);
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

				tooltipStyle = new GUIStyle(GUI.skin.GetStyle("label"));
				Texture2D texBack = new Texture2D(1, 1, TextureFormat.ARGB32, false);
				texBack.SetPixel(0, 0, new Color(0.0f, 0.0f, 0.0f, 1f));
				texBack.Apply();
				tooltipStyle.normal.background = texBack;

				validFunctionName = new GUIStyle(GUI.skin.GetStyle("TextField"));
				editFunctionStyle = validFunctionName;
				invalidFunctionName = new GUIStyle(GUI.skin.GetStyle("TextField"));
				invalidFunctionName.normal.textColor = Color.red;
				invalidFunctionName.active.textColor = Color.red;
				invalidFunctionName.hover.textColor = Color.red;
				invalidFunctionName.focused.textColor = Color.red;

				stylesInitiated = true;
			}

			if(drawMainButton) {
				// Draw the main button
				if(GUI.Button(mainButtonPos, kerbulatorIcon, defaultButton)) {
					ChangeState(!mainWindowEnabled);
				}
			}

			if(reload) {
				reload = false;

				// Rebuild the list of functions. It could be that they were edited outside of KSP
				JITFunction.Scan(functionDir, kalc);

				// Reload the function being edited
				if(editFunction != null)
					editFunctionContent = System.IO.File.ReadAllText(functionFile);
			}

			// Draw the windows (if enabled)
			if(mainWindowEnabled) {
				options.mainWindowPos = GUI.Window(windowId, options.mainWindowPos, DrawMainWindow, "Kerbulator");
				glue.PreventClickthrough(options.mainWindowPos, "KerbulatorMainWindow");
			} else {
				glue.EnsureLockReleased("KerbulatorMainWindow");
			}

			if(editWindowEnabled) {
				options.editWindowPos = GUI.Window(windowId + 1, options.editWindowPos, DrawEditWindow, "Function Editor");
				glue.PreventClickthrough(options.editWindowPos, "KerbulatorEditWindow");
			} else {
				glue.EnsureLockReleased("KerbulatorEditWindow");
			}

			if(runWindowEnabled) {
				options.runWindowPos = GUI.Window(windowId + 2, options.runWindowPos, DrawRunWindow, "Run "+ RunFunction.Id);
				glue.PreventClickthrough(options.runWindowPos, "KerbulatorRunWindow");
			} else {
				glue.EnsureLockReleased("KerbulatorRunWindow");
			}

			if(running) {
				foreach(KeyValuePair<int, ExecutionEnvironment> pair in envs) {
					pair.Value.windowPos = GUI.Window(pair.Key, pair.Value.windowPos, DrawRepeatedWindow, pair.Value.func.Id);
					glue.PreventClickthrough(pair.Value.windowPos, "KerbulatorEnvironment" + pair.Value.func.Id);
				}
			}

			DrawToolTip();
		}

		/// <summary>Draws the main window that displays a list of available functions</summary>
		/// <param name="id">An unique number indentifying the window</param>
		public void DrawMainWindow(int id) {
			// Close button at the top right corner
			if(drawMainButton)
				ChangeState(!GUI.Toggle(new Rect(options.mainWindowPos.width - 25, 0, 20, 20), !mainWindowEnabled, ""));

			if(error != null)
				GUILayout.Label(error);

			GUILayout.Label("Available functions:");

			mainScrollPos = GUILayout.BeginScrollView(mainScrollPos, false, true, GUILayout.Height(options.mainWindowPos.height - 110));

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

			options.mainWindowPos = ResizeWindow(id, options.mainWindowPos, minMainWindowSize);
			GUI.DragWindow(titleBarRect);

			if(Event.current.type == EventType.Repaint)
				tooltip = GUI.tooltip;

			// Run button was pressed, run the function
			if(runSomething) {
				Run();
				functionOutput = FormatOutput(env);
				GUI.FocusWindow(windowId + 2);
				runSomething = false;
			}
		}

		/// <summary>Draws the edit window that allows basic text editing.</summary>
		/// <param name="id">An unique number indentifying the window</param>
		public void DrawEditWindow(int id) {
			// Close button
			editWindowEnabled = !GUI.Toggle(new Rect(options.editWindowPos.width - 25, 0, 20, 20), !editWindowEnabled, "");

			GUILayout.BeginHorizontal();

			if(GUILayout.Button(deleteIcon, defaultButton, GUILayout.Width(25), GUILayout.Height(24))) {
				Delete();
			}

			editFunctionName = GUILayout.TextField(editFunctionName, editFunctionStyle, GUILayout.Height(24));

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
				Run();
				functionOutput = FormatOutput(env);
			}

			GUILayout.EndHorizontal();

			if(functionNameError != null)
				GUILayout.Label("Cannot save function:" + functionNameError, invalidFunctionName);

			editorScrollPos = GUILayout.BeginScrollView(editorScrollPos, false, true, GUILayout.Height(options.editWindowPos.height - 140)); //, GUILayout.Width(460));
			editFunctionContent = GUILayout.TextArea(editFunctionContent, GUILayout.ExpandWidth(true));
			TextEditor editor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
			GUILayout.EndScrollView();

			GUILayout.BeginHorizontal();
			foreach(string s in greekLetters) {
				if(GUILayout.Button(s, keyboard, GUILayout.Width(15))) {
					editFunctionContent = editFunctionContent.Insert(editor.cursorIndex, s);
					editor.cursorIndex ++;
					editor.selectIndex ++;
					//editor.UpdateScrollOffsetIfNeeded();
				}
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			foreach(string s in greekUCLetters) {
				if(GUILayout.Button(s, keyboard, GUILayout.Width(15))) {
					editFunctionContent = editFunctionContent.Insert(editor.cursorIndex, s);
					editor.cursorIndex ++;
					editor.selectIndex ++;
					//editor.UpdateScrollOffsetIfNeeded();
				}
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			foreach(string s in symbols) {
				if(GUILayout.Button(s, keyboard, GUILayout.Width(15))) {
					editFunctionContent = editFunctionContent.Insert(editor.cursorIndex, s);
					editor.cursorIndex ++;
					editor.selectIndex ++;
					//editor.UpdateScrollOffsetIfNeeded();
				}
			}
			GUILayout.EndHorizontal();

			options.editWindowPos = ResizeWindow(id, options.editWindowPos, minEditWindowSize);
			GUI.DragWindow(titleBarRect);

			if(Event.current.type == EventType.Repaint)
				tooltip = GUI.tooltip;
		}

		/// <summary>Draws the run window that allows execution of a function.</summary>
		/// <param name="id">An unique number indentifying the window</param>
		public void DrawRunWindow(int id) {
			// Close button
			runWindowEnabled = !GUI.Toggle(new Rect(options.runWindowPos.width - 25, 0, 20, 20), !runWindowEnabled, "");
			runScrollPos = GUILayout.BeginScrollView(runScrollPos, GUILayout.Height(options.runWindowPos.height - 40));

			GUILayout.BeginHorizontal();

			if(GUILayout.Button(runIcon, defaultButton, GUILayout.Height(32))) {
				Run();
				functionOutput = FormatOutput(env);
			}

			if(GUILayout.Button(repeatIcon, defaultButton, GUILayout.Height(32))) {
				RunRepeated();
			}

			if(glue.CanAddNode()) {
				if(GUILayout.Button(nodeIcon, defaultButton, GUILayout.Height(32))) {
					Debug.Log("[Kerbulator] Adding maneuver node");
					List<System.Object> output = Run();
                    if(!RunFunction.InError) {
                        List<System.Object> maneuvers = env.GetOutputsOfType(OutputType.Maneuver);
                        glue.PlaceNodes(RunFunction.Outs, maneuvers, output);
                    }

					functionOutput = FormatOutput(env);
				}
			}

			if(glue.CanAddAlarm()) {
				if(GUILayout.Button(alarmIcon, defaultButton, GUILayout.Height(32))) {
					Debug.Log("[Kerbulator] Adding alarm");
					List<System.Object> output = Run();
					if(!RunFunction.InError){
						List<System.Object> alarms = env.GetOutputsOfType(OutputType.Alarm);
                        glue.AddAlarms(RunFunction.Id, RunFunction.Outs, alarms, output);
					}
					functionOutput = FormatOutput(env);
				}
			}

			GUILayout.EndHorizontal();

			if(RunFunction == null) {
				GUILayout.Label("ERROR: no function selected.");
			} else if(RunFunction.InError) {
				GUILayout.Label("ERROR: "+ RunFunction.ErrorString);
			} else {
				if(RunFunction.Ins.Count > 0) {
					for(int i=0; i<arguments.Count && i < RunFunction.Ins.Count; i++) {
						GUILayout.BeginHorizontal();

						if(i < RunFunction.InDescriptions.Count)
							GUILayout.Label(new GUIContent(RunFunction.Ins[i], RunFunction.InDescriptions[i]));
						else
							GUILayout.Label(RunFunction.Ins[i]);

						arguments[i] = GUILayout.TextField(arguments[i], GUILayout.Width(150));
						GUILayout.EndHorizontal();
					}
				}

				GUILayout.Label(functionOutput);
			}

			GUILayout.EndScrollView();

			options.runWindowPos = ResizeWindow(id, options.runWindowPos, minRunWindowSize);
			GUI.DragWindow(titleBarRect);

			if(Event.current.type == EventType.Repaint)
				tooltip = GUI.tooltip;
		}

		public void DrawRepeatedWindow(int id) {
			ExecutionEnvironment e = envs[id];

			// Close button
			e.enabled = !GUI.Toggle(new Rect(e.windowPos.width - 25, 0, 20, 20), !e.enabled, "");
			if(!e.enabled) {
				envs.Remove(id);
				glue.EnsureLockReleased("KerbulatorEnvironment" + e.func.Id);
				return;
			}

			e.scrollPos = GUILayout.BeginScrollView(e.scrollPos, false, false, GUILayout.Height(e.windowPos.height - 30));

			if(e.InError)
				GUILayout.Label("ERROR: "+ e.ErrorString);
			else
				GUILayout.Label(FormatOutput(e));

			GUILayout.EndScrollView();

			e.windowPos = ResizeWindow(id, e.windowPos, minRepeatWindowSize);
			GUI.DragWindow(titleBarRect);

			if(Event.current.type == EventType.Repaint)
				tooltip = GUI.tooltip;
		}

		/// <summary>Run a function.</summary>
		public List<System.Object> Run() {
			if(RunFunction == editFunction)
				Save();

			functionOutput = "";
			env = new ExecutionEnvironment(RunFunction, kalc);

			foreach(string arg in arguments) {
				if(arg == "")
					return null;
			}

			env.SetArguments(arguments);

			glue.AddGlobals(kalc);
			return env.Execute();
		}

		/// <summary>Run a function in a separate window.</summary>
		public void RunRepeated() {
			if(RunFunction == editFunction)
				Save();

			foreach(string arg in arguments) {
				if(arg == "")
					return;
			}

			Rect pos = options.repeatWindowPos;

			// Stop the function if already running, and remove from the list
			int id = WindowIdOfRepeatingFunction(RunFunction.Id);
			if(id != -1) {
				pos = envs[id].windowPos;
				envs.Remove(id);
			}

			// Don't run the function if it is in error state
			if(RunFunction.InError)
				return;

			ExecutionEnvironment e = new ExecutionEnvironment(RunFunction, kalc);
			e.SetArguments(arguments);
			e.windowPos = pos;

			// Add the ExecutionEnvironment to the list
			envs.Add(windowId + 4 + envs.Count, e);

			// Start executing it
			if(!running) {
				running = true;
				glue.RunAsCoroutine(RepeatedExecute());
			}
		}

		public IEnumerator RepeatedExecute() {
			while(running) {
				glue.AddGlobals(kalc);
				foreach(ExecutionEnvironment e in envs.Values)
					e.Execute();

				yield return new WaitForSeconds(0.2F);
			}
		}

		/// <summary>Save the current function being edited.</summary>
		public void Save() {
			if(!IsValidName(editFunctionName)) {
				editFunctionStyle = invalidFunctionName;
				return;
			} else {
				editFunctionStyle = validFunctionName;
			}

			int id;
			if(editFunction != null && editFunction.Id != editFunctionName) {
				// Changing function name, remove old function
				string oldFunctionFile = functionDir +"/"+ editFunction.Id +".math";
				if(System.IO.File.Exists(oldFunctionFile)) {
					try {
						System.IO.File.Delete(oldFunctionFile);
						error = null;
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

				id = WindowIdOfRepeatingFunction(editFunction.Id);
				if(id != -1)
					envs.Remove(id);
			}

			// Save new function
			try {
				functionFile = functionDir +"/"+ editFunctionName +".math";
				System.IO.File.WriteAllText(functionFile, editFunctionContent);
				error = null;
			} catch(Exception e) {
				error = "Cannot save function: "+ e.Message;
				return;
			}

			// Compile new function
			JITFunction f = JITFunction.FromFile(functionFile, kalc);
			f.Compile();
			if(RunFunction != null && RunFunction.Id == f.Id)
				RunFunction = f;

			id = WindowIdOfRepeatingFunction(f.Id);
			if(id != -1)
				envs[id].func = f;

			if(!kalc.Functions.ContainsKey(editFunctionName))
				kalc.Functions.Add(editFunctionName, f);
			else
				kalc.Functions[editFunctionName] = f;

            editFunction = f;
		}

		private bool IsValidName(string name) {
			if(name.Length == 0)
				return true;
			foreach(Operator o in kalc.Operators.Values) {
				if(o.id == "or" || o.id == "and")
					continue;
				if(name.Contains(o.id)) {
					functionNameError = "function name cannot contain the symbol '"+ o.id +"'";
					return false;
				}
			}
			if(name.Contains(" ")) {
				functionNameError = "function name cannot contain spaces";
				return false;
			}
			if(name.Contains("\\")) {
				functionNameError = "function name cannot contain (back)slashes";
				return false;
			}
			if(Char.IsDigit(name[0])) {
				functionNameError = "function name cannot start with a number";
				return false;
			}
			if(name[0] == '.') {
				functionNameError = "function name cannot start with a dot";
				return false;
			}
			functionNameError = null;
			return true;
		}

		/// <summary>Delete the current function being edited.</summary>
		public void Delete() {
			if(editFunction != null) {
				string oldFunctionFile = functionDir +"/"+ editFunction.Id +".math";
				if(System.IO.File.Exists(oldFunctionFile))
					System.IO.File.Delete(oldFunctionFile);

				if(selectedFunction != null && selectedFunction.Id == editFunction.Id) {
					selectedFunction = null;
				}

				if(RunFunction != null && RunFunction.Id == editFunction.Id)
					RunFunction = null;

				kalc.Functions.Remove(editFunction.Id);
			}

			editFunction = null;
			editWindowEnabled = false;
			editFunctionContent = "";
			editFunctionName = "unnamed";
		}

		/// <summary>Obtain some info of a function.</summary>
		/// <param name="f">The function to obtain the info of</param>
		public string FunctionDescription(JITFunction f) {
			if(f.InError)
				return "ERROR: "+ f.ErrorString;

			string desc = "";

			if(f.Ins.Count == 0) {
				desc += "Inputs:\nnone\n";
			} else {
				desc += "Inputs:\n";
				for(int i=0; i<f.Ins.Count; i++) {
					desc += f.Ins[i];
					if(i < f.InDescriptions.Count && f.InDescriptions[i] != "")
						desc += ": "+ f.InDescriptions[i];
					desc += "\n";
				}
			}

			if(f.Outs.Count == 0) {
				desc += "\nOutputs:\nnone\n";
			} else {
				desc += "\nOutputs:\n";
				for(int i=0; i<f.Outs.Count; i++) {
					desc += f.Outs[i];
					if(i < f.OutDescriptions.Count && f.OutDescriptions[i] != "")
						desc += ": "+ f.OutDescriptions[i];
					desc += "\n";
				}
			}

			return desc;
		}

		/// <summary>Provide a string representation of the output resulting from executing a function.</summary>
		public string FormatOutput(ExecutionEnvironment env) {
			if(env == null)
				return "";

			if(env.InError)
				return "ERROR: "+ env.ErrorString;

			if(env.Output == null)
				return "";

			if(env.Output.Count == 0) {
				return "None.";
			}

			if(env.Output.Count != env.func.Outs.Count) {
				return "None.";
			}
		   
			string desc = "";
			for(int i=0; i<env.Output.Count; i++){
				if(env.func.OutputTypes[i] == OutputType.Value)
					desc += env.func.OutPrefixes[i] + Kerbulator.FormatVar(env.Output[i]) + env.func.OutPostfixes[i] +"\n";
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
				error = null;
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

			List<int> envsToRemove = new List<int>();
			foreach(KeyValuePair<int, ExecutionEnvironment> pair in envs)  {
				if(functions.ContainsKey(pair.Value.func.Id))
					pair.Value.func = functions[pair.Value.func.Id];
				else
					envsToRemove.Add(pair.Key);
			}
			foreach(int id in envsToRemove)
				envs.Remove(id);
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
						options.runWindowPos = new Rect(options.runWindowPos.x, options.runWindowPos.y, maxWidth + 200, options.runWindowPos.height);
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

		void DrawToolTip() {
			if(tooltip == "")
				return;

			Rect pos = new Rect();
			pos.x = Event.current.mousePosition.x + 10;
			pos.y = Event.current.mousePosition.y + 20;
			Vector2 size = GUI.skin.box.CalcSize(new GUIContent(tooltip));
			pos.width = size.x;
			pos.height= size.y;

			GUI.Window(windowId + 3, pos, DrawToolTipWindow, "", tooltipStyle);
		}

		void DrawToolTipWindow(int id) {
			GUILayout.Label(tooltip);
			GUI.BringWindowToFront(id);
		}

		public void OnDestroy() {
			glue.EnsureLockReleased("KerbulatorMainWindow");
			glue.EnsureLockReleased("KerbulatorEditWindow");
			glue.EnsureLockReleased("KerbulatorRunWindow");
			foreach(KeyValuePair<int, ExecutionEnvironment> pair in envs) {
				glue.EnsureLockReleased("KerbulatorEnvironment" + pair.Value.func.Id);
			}
			envs.Clear();
			running = false;
		}

		Rect ResizeWindow(int id, Rect windowRect, Vector2 minWindowSize) {
			Vector2 mouse = GUIUtility.ScreenToGUIPoint(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y));
			//Rect r = GUILayoutUtility.GetRect(gcDrag, GUI.skin.window);
			Rect r = new Rect(windowRect.width-20, windowRect.height-20, 20, 20);

			if(Event.current.type == EventType.MouseDown && r.Contains(mouse))
			{
				resizing = id;
				resizeStart = new Rect( mouse.x, mouse.y, windowRect.width, windowRect.height );
			} else if(Event.current.type == EventType.MouseUp && resizing == id)
				resizing = 0;
			else if(!Input.GetMouseButton(0))
				resizing = 0;
			else if(resizing == id) {
				windowRect.width = Mathf.Max(minWindowSize.x, resizeStart.width + (mouse.x - resizeStart.x));
				windowRect.height = Mathf.Max(minWindowSize.y, resizeStart.height + (mouse.y - resizeStart.y));
				windowRect.xMax = Mathf.Min(Screen.width, windowRect.xMax);  // modifying xMax affects width, not x
				windowRect.yMax = Mathf.Min(Screen.height, windowRect.yMax);  // modifying yMax affects height, not y
			}

			GUI.Button(r, gcDrag, GUI.skin.label);

			return windowRect;
		}

		int WindowIdOfRepeatingFunction(string functionName) {
			foreach(KeyValuePair<int, ExecutionEnvironment> pair in envs) {
				if(pair.Value.func.Id == functionName) {
					return pair.Key;
				}
			}

			return -1;
		}
	}
}
