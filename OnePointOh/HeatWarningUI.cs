using System;
using System.Timers;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

namespace HeatWarning
{
	public enum MenuOptions{
		ENABLED,
		AUDIO,
		HEAT_GAUGES,
		MAPVIEW_HEAT,
		IVA_HEAT
	}

	/*
	 * The menu runs as its own plugin class.
	 */
	[KSPAddon(KSPAddon.Startup.MainMenu, true)]
	public class ListMenu : MonoBehaviour
	{
		private Timer tmrMakeButtonTimer;
		private Rect size;
		private int windowID;
		private bool isActive;
		private Dictionary<MenuOptions, MenuOption> options;
		private static ConfigNode optionsStore;
		private string nodeName;
		private static ApplicationLauncherButton menuButton;
		private Texture2D menuButtonTexture;
		private ApplicationLauncher.AppScenes visibleInScenes = ApplicationLauncher.AppScenes.ALWAYS;
		public static ListMenu Instance{
			get{
				//return this;
				return (ListMenu)FindObjectOfType(typeof(ListMenu));
			}
		}
		public Dictionary<MenuOptions, MenuOption> getOptions{get{return options;}}
		public ListMenu()
		{
			
		}
		public void Start()
		{
			DontDestroyOnLoad(this);
		}
		public void Awake()
		{
			windowID = Guid.NewGuid().GetHashCode();
			//DontDestroyOnLoad(this);
			menuButtonTexture = GameDatabase.Instance.GetTexture("HeatWarning/heatConfigButton", false);
			nodeName = "GameData/HeatWarning/options.cfg";
			//ConfigNode tmpNode = new ConfigNode();
			options = new Dictionary<MenuOptions, MenuOption>();
			_createOptions();

			size = new Rect(0,0,180,120);
			optionsStore = ConfigNode.Load(nodeName);
			//if (GameDatabase.Instance.ExistsConfigNode(nodeName))
			if (optionsStore != null)
			{
				Debug.Log("[HeatWarningUI] ConfigNode exists. Loading.");
				//optionsStore = GameDatabase.Instance.GetConfigNode(nodeName);
				_setOptions();
			}
			else{
				Debug.Log("[HeatWarningUI] ConfigNode does not exist. Creating.");
				optionsStore = new ConfigNode();
				_updateNode();
			}
			isActive = false;

			tmrMakeButtonTimer = new System.Timers.Timer(500);
			tmrMakeButtonTimer.Elapsed += addButtonEvent;
			tmrMakeButtonTimer.Enabled = true;

			//addButton();
			//GameEvents.onGUIApplicationLauncherReady.Add(addButton);
			//GameEvents.onGUIApplicationLauncherUnreadifying.Add(rbEvent);
		}
		/*public void OnLevelWasLoaded(int lvl)
		{
			Debug.Log("[HeatWarning] Level loaded: " + lvl);
			addButton();
		}*/
		/*public void OnDestroy()
		{
			GameEvents.onGUIApplicationLauncherReady.Remove(addButton);
			GameEvents.onGUIApplicationLauncherUnreadifying.Remove(rbEvent);
			removeButton();
		}*/

		private void addButtonEvent(object source, ElapsedEventArgs e)
		{
			addButton();
		}

		public void FixedUpdate()
		{
			if (menuButton && ApplicationLauncher.Ready)
			{
				//Vector3 btnPos = ApplicationLauncher.Instance.transform.position;
				//Vector3 btnPos = menuButton.transform.position;
				//size.x = btnPos.x - size.width + 38;
				size.x = Screen.width - size.width;
				if (HighLogic.LoadedSceneIsEditor)
				{
					size.y = Screen.height - size.height - 38;
				}
				else{
					size.y = 38; //Applicationlauncher buttons are 38x38 textures.
				}

			}
		}
		public void OnGUI()
		{
			if (isActive)
			{
				size = GUILayout.Window(windowID, size, draw, "HeatWarning options", GUI.skin.window);
			}
		}
		public void addButton()
		{
			//bool btnExists = false;
			if (ApplicationLauncher.Ready)
			{
				if (menuButton)
				{
					removeButton();
				}
				//if(ApplicationLauncher.Instance.Contains(menuButton, out btnExists) == false)
				if (!menuButton)
				{
					Debug.Log("[HeatWarning] Adding config button.");
					menuButton = ApplicationLauncher.Instance.AddModApplication(
						onBtnClickOn, //Callback when button is toggled on.
						onBtnClickOff, //Callback when button is toggled off.
						onBtnHover, //Callback when mouse is hovering over button.
						onBtnHoverOut, //Callback when mouse moves away from the button.
						onBtnEnable, //Callback for when the button is shown or enabled by the launcher.
						onBtnDisable, //Callback for when the button is hidden or disabled by the launcher.
						visibleInScenes, //The scenes this button will be visible in.
						menuButtonTexture //A 38x38 Texture oboect.
					);
					if (tmrMakeButtonTimer.Enabled)
					{
						tmrMakeButtonTimer.Enabled = false;
					}
					Debug.Log("[HeatWarning] Config button added.");
				}
			}
			else{
				Debug.Log("[HeatWarning] addButton() called but launcher not ready.");
			}
		}
		public void removeButton()
		{
			//if (ApplicationLauncher.Ready)
			//{
				if (menuButton)
				{
					try{
						ApplicationLauncher.Instance.RemoveModApplication(menuButton);
						menuButton = null;
					}
					catch (Exception e)
					{
						Debug.Log("[HeatWarning] There was an exception attempting to remove the applauncher button. Exception type follows...");
						Debug.Log("[HeatWarning] " + e.Message);
					}
				}
			//}
		}
		public void draw(int windowID)
		{
			//GUILayout.BeginArea(size,"HeatWarning options", GUI.skin.);
			//GUILayout.BeginArea(size);
			GUILayout.BeginVertical();
			foreach(KeyValuePair<MenuOptions, MenuOption> entry in options)
			{
				entry.Value.draw();
			}
			GUILayout.EndVertical();
			//GUILayout.EndArea();
		}
		public bool Active{
			get{
				return isActive;
			}
			set{
				isActive = value;
			}
		}

		public void onBtnClickOn()
		{
			isActive = true;
		}
		public void onBtnClickOff()
		{
			isActive = false;
		}
		public void onBtnHover()
		{
		}
		public void onBtnHoverOut()
		{
		}
		public void onBtnEnable()
		{
		}
		public void onBtnDisable()
		{
		}


		public void onToggleCallBack()
		{
			_updateNode();
		}

		private void _addOption(MenuOptions o, Rect r, bool t, string s)
		{
			options.Add(
				o,
				new MenuOption(
					r,
					t,
					s,
					onToggleCallBack
				)
			);
		}
		/*
		 * Populate the options Dictionary with default values.
		 */
		private void _createOptions()
		{
			Rect optionRect = new Rect(0,0,178,20);
			_addOption(MenuOptions.ENABLED, optionRect, true, "Enable heat alert system.");
			_addOption(MenuOptions.HEAT_GAUGES, optionRect, true, "Enable heat gauges.");
			_addOption(MenuOptions.AUDIO, optionRect, true, "Enable audio alerts.");
			_addOption(MenuOptions.MAPVIEW_HEAT, optionRect, true, "Enable map view effects.");
			_addOption(MenuOptions.IVA_HEAT, optionRect, true, "Enable IVA view effects.");
		}
		/*
		 * Set the options Dictionary from the options ConfigNode.
		 */
		private void _setOptions()
		{
			MenuOptions currentOption;
			foreach(ConfigNode.Value vl in optionsStore.values)
			{
				currentOption = (MenuOptions)Enum.Parse(typeof(MenuOptions), vl.name);
				options[currentOption].state = Boolean.Parse(vl.value);
			}
		}
		/*
		 * Set the options ConfigNode from the options Dictionary.
		 */
		private void _updateNode()
		{
			Debug.Log("[HeatWarning] _updateNode called.");
			foreach(KeyValuePair<MenuOptions, MenuOption> entry in options)
			{
				Debug.Log("[HeatWarning] _updateNode: Updating: " + entry.Value.getDescription);
				Debug.Log("[HeatWarning] _updateNode: countValues: " + optionsStore.CountValues);
				optionsStore.SetValue(Enum.GetName(typeof(MenuOptions), entry.Key), entry.Value.state.ToString().ToLower(), true);
			}
			_storeNode();
		}
		private void _storeNode()
		{
			//optionsStore.Save();
			optionsStore.Save(nodeName);
		}
	}
	/*
	 * Menu options are stored in ListMenu as instances of MenuOption.
	 */
	public class MenuOption
	{
		private Rect size;
		private bool _state;
		private string description;
		private Action callBack;
		public MenuOption(Rect sizeRect, bool defaultState, string desc, Action cback)
		{
			size = new Rect(sizeRect);
			_state = defaultState;
			description = desc;
			callBack = cback;
		}
		public bool state
		{
			get {return _state;}
			set {_state = value;}
		}
		public string getDescription{
			get {return description;}
		}
		public Rect getSize{
			get{return size;}
		}
		//Call in OnGUI.
		public void draw()
		{
			bool oldState = _state;
			_state = GUILayout.Toggle(_state, description, GUILayout.ExpandWidth(true));
			//state = GUI.Toggle(size, state, description);
			if (_state != oldState)
			{
				callBack();
			}
		}
	}

}