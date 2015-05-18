using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;


namespace HeatWarning{

	/* 
	 * Holds the state of each thermometer.
	 * I may turn this into a struct at some stage,
	 * if only to make Chris happy.
	 */
	public class ThermometerState
	{
		public Rect thermBG; //Thermometer background.
		public Rect thermFG; //Thermometer foreground.
		public Rect flashBG; //Flash rectangle.
		public bool flashState; //Is the flasher enabled this tick?
		public float flashStarted; //When was the last flash cycle started?
		public bool FlashOn; //Is the flash being rendered this FixedUpdate?
		public double ratio; //Temperature ratio, where 0.0 == 0temp and 1.0 == maxTemp.
	}



	/*
	 * Pretty much everything in one class. It's big and ugly,
	 * hacked together and could probably use a beauty pass or two.
	 */
	[KSPAddon(KSPAddon.Startup.Flight, false)]
	public class HeatWarning : MonoBehaviour
	{
		public enum AlertButtonState{
			OFF,
			HEAT,
			CRITICAL
		};

		private ListMenu configMenu;

		private string nodeName;
		//private ConfigNode optionsStore;
		private Dictionary<MenuOptions, bool> options;

		private double tempThreshold;
		private double flashThreshold;

		private Color thermometerBGCol;
		private Color thermometerFGCol;
		private Color thermometerFlashCol;
		private float thermometerFlashOnPeriod;
		private float thermometerFlashPeriod;
		private Texture2D thermometerBGTex;
		private Texture2D thermometerFGTex;
		private Texture2D thermometerFlashTex;
		private GUIStyle thermometerBGStyle;
		private GUIStyle thermometerFGStyle;
		private GUIStyle thermometerFlashStyle;
		private Rect thermometerSize;
		private Rect flashSize;
		private Texture2D heatButtonTexture;
		private Texture2D criticalHeatButtonTexture;
		private ApplicationLauncherButton heatButton;
		//private ApplicationLauncherButton criticalHeatButton;
		private ApplicationLauncher.AppScenes visibleInScenes = ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.MAPVIEW;
		private AudioClip warnSound;
		private AudioClip criticalSound;
		private GameObject soundObject;
		private AudioSource src;
		private AudioSource criticalSrc;

		//Map view effects vars.
		private float mapViewPulseLast; //time value for the start of the last pulse.
		private float mapViewPulseLength; //Length in seconds for each pulse.
		private Color mapViewPulseStartColor;
		private Color mapViewPulseEndColor;
		private Color mapViewPulseCurrentColor;
		private Texture2D mapViewPulseTexture;
		private GUIStyle mapViewPulseStyle;

		//Animation helpers.
		//private float fixedUpdateDelta; //Time since the last FixedUpdate;
		private float fixedUpdateCurrentTime; //Set at the start of each FixedUpdate;

		//private List<ThermometerState> thermometerStates;
		private Dictionary<int, ThermometerState> thermometerStates;

		private bool overHeating;
		private bool heatOverride;
		private bool flashOverride;
		private bool flashing;
		private bool inMapView;
		private bool inIVA;
		private AlertButtonState buttonState;
		private bool stockGaugesDisabled;


		public HeatWarning ()
		{
		}
		public void Awake()
		{
			
			Debug.Log("[HeatWarning] Waking up! Rise and shine! The time is " + DateTime.Now.ToLongTimeString());

			//configMenu = GameObject.Find("ListMenu").GetComponent<ListMenu>();

			//Set config file location.
			nodeName = "GameData/HeatWarning/options.cfg";
			//Init bools.
			overHeating = false;
			flashing = false;
			inMapView = false;
			inIVA = false;
			flashOverride = false;
			heatOverride = false;

			//Set button state.
			buttonState = AlertButtonState.OFF;


			//Load heat buttons.
			heatButtonTexture = GameDatabase.Instance.GetTexture("HeatWarning/heatButton", false);
			criticalHeatButtonTexture = GameDatabase.Instance.GetTexture("HeatWarning/criticalHeatButton", false);

			//Init sound stuff.
			warnSound = GameDatabase.Instance.GetAudioClip("HeatWarning/overheat_warn");
			criticalSound = GameDatabase.Instance.GetAudioClip("HeatWarning/critical_heat");
			soundObject = new GameObject();
			//AudioSource src = soundObject.AddComponent<AudioSource>();
			src = soundObject.AddComponent<AudioSource>();
			src.clip = warnSound;
			src.dopplerLevel = 0.0f;
			src.bypassEffects = true;
			src.loop = true;
			src.volume = 1.0f;
			criticalSrc = soundObject.AddComponent<AudioSource>();
			criticalSrc.clip = criticalSound;
			criticalSrc.dopplerLevel = 0.0f;
			criticalSrc.bypassEffects = true;
			criticalSrc.loop = true;
			criticalSrc.volume = 1.0f;


			//Debug.Log("[HeatWarning] Waking up! Resources loaded.");

			//Init thermometer styling.
			thermometerBGCol = new Color(0.3f,0.0f,0.0f,0.65f);
			thermometerFGCol = new Color(1.0f,0.5f,0.0f,0.65f);
			thermometerFlashCol = new Color(1.0f,1.0f,1.0f,0.65f);
			//Debug.Log("[HeatWarning] Waking up! Colours defined.");
			thermometerFlashOnPeriod = 0.1f;
			thermometerFlashPeriod = 0.2f;

			thermometerBGTex = new Texture2D(1,1);
			thermometerFGTex = new Texture2D(1,1);
			thermometerFlashTex = new Texture2D(1,1);
			//Debug.Log("[HeatWarning] Waking up! Textures defined.");

			thermometerBGStyle = new GUIStyle();
			thermometerFGStyle = new GUIStyle();
			thermometerFlashStyle = new GUIStyle();
			//Debug.Log("[HeatWarning] Waking up! Styles created.");
			thermometerBGTex.SetPixel(0,0,thermometerBGCol);
			thermometerBGTex.Apply();
			thermometerFGTex.SetPixel(0,0,thermometerFGCol);
			thermometerFGTex.Apply();
			thermometerFlashTex.SetPixel(0,0,thermometerFlashCol);
			thermometerFlashTex.Apply();
			//Debug.Log("[HeatWarning] Waking up! Texture pixels set.");
			thermometerBGStyle.normal.background = thermometerBGTex;
			thermometerFGStyle.normal.background = thermometerFGTex;
			thermometerFlashStyle.normal.background = thermometerFlashTex;
			//Debug.Log("[HeatWarning] Waking up! Thermometers styled. Trying to make new List<T>.");
			//thermometerStates = new List<ThermometerState>();
			thermometerStates = new Dictionary<int, ThermometerState>();
			//Debug.Log("[HeatWarning] Waking up! List<T> made.");

			tempThreshold = 0.7;
			flashThreshold = 0.9;
			thermometerSize = new Rect(0,0,64,8);
			flashSize = new Rect(0,0,66,10);

			/*
			 *  Init aninmation vars
			 */
			fixedUpdateCurrentTime = Time.realtimeSinceStartup;
			/*
			 * Map View visual warning variables.
			 */
			mapViewPulseLast = fixedUpdateCurrentTime;
			mapViewPulseLength = 1.0f; //Length of each map view pulse in seconds.
			mapViewPulseTexture = new Texture2D(1,1);
			mapViewPulseStyle = new GUIStyle();
			mapViewPulseStyle.normal.background = mapViewPulseTexture;
			//mapViewPulseStyle.hover.background = null;
			mapViewPulseStartColor = new Color(1.0f, 0.3f, 0.0f, 0.2f);
			mapViewPulseEndColor = new Color(1.0f, 0.3f, 0.0f, 0.0f);
			//Set game event callbacks up.
			GameEvents.OnMapEntered.Add(onMapEntered);
			GameEvents.OnMapExited.Add(onMapExited);
			GameEvents.onPartDestroyed.Add(onPartDestroyed);
			GameEvents.onVesselWasModified.Add(onVesselWasModified);
			Debug.Log("[HeatWarning] Populating MenuOptions bool dictionary.");
			/*
			 * Initialize the options dictionary as all-enabled.
			 */
			options = new Dictionary<MenuOptions, bool>();
			foreach (MenuOptions key in Enum.GetValues(typeof(MenuOptions)))
			{
				options.Add(key, true);
			}
			stockGaugesDisabled = false;
			Debug.Log("[HeatWarning] Awake() finished at: " + DateTime.Now.ToLongTimeString());
		}
		public void Update(){
			if (!configMenu)
			{
				Debug.Log("[HeatWarning] Grabbing ListMenu instance.");
				configMenu = ListMenu.Instance; //This is slow, so don't do it unless necessary.
			}
			if (configMenu) //If the config menu exists...
			{
				/*
				 * Set the options bools from the values in the config menu.
				 */
				foreach(KeyValuePair<MenuOptions, MenuOption> opt in configMenu.getOptions)
				{
					options[opt.Key] = opt.Value.state;
				}
			}
		}
		/*
		 * Physics related stuff goes in FixedUpdate, NEVER Update.
		 * Sorry, Chris.
		 */
		public void FixedUpdate()
		{
			//TimeSpan t = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));
			fixedUpdateCurrentTime = Time.realtimeSinceStartup;

			//iterateEveryPart();
			//calculateFlashes();
			checkIVA();
			if (options[MenuOptions.ENABLED])
			{
				calculateEveryPart();
				if (!stockGaugesDisabled)
				{
					disableStockGauges();
					stockGaugesDisabled = true;
				}
			}
			else{
				if (thermometerStates.Count > 0)
				{
					thermometerStates.Clear();
				}
				removeHeatOverrideButton();
				stockGaugesDisabled = false;
			}
			checkSound();
		}
		public void OnGUI()
		{
			if (options[MenuOptions.ENABLED])
			{
				if (options[MenuOptions.HEAT_GAUGES])
				{
					drawEveryPart();
				}
				if (inMapView && overHeating && options[MenuOptions.MAPVIEW_HEAT])
				{
					drawMapViewPulse();
				}
				if (inIVA && overHeating && options[MenuOptions.IVA_HEAT])
				{
					drawMapViewPulse();
				}
			}
		}
		/*
		 * Check whether we're currently in one of the IVA views.
		 * I don't like having to poll for this.
		 */
		private void checkIVA()
		{
			CameraManager.CameraMode camMode = CameraManager.Instance.currentCameraMode;
			if ((camMode == CameraManager.CameraMode.Internal) || ((camMode == CameraManager.CameraMode.IVA)))
			{
				inIVA = true;
			}
			else
			{
				inIVA = false;
			}
		}

		private void disableStockGauges()
		{
			try{
				if (TemperatureGagueSystem.Instance != null)
				{
					TemperatureGagueSystem.Instance.showGagues = false;
				}
			}
			catch(Exception e)
			{
				Debug.Log("[HeatWarning] Error disabling temperature UI. Message: " + e.Message);
				Debug.Log("[HeatWarning] It's probably safe to ignore this error if running KSP v1.0.0.");
			}
		}

		private void calculateMapViewPulse()
		{
			float pulseDelta = fixedUpdateCurrentTime - mapViewPulseLast;
			if (pulseDelta > mapViewPulseLength)
			{
				mapViewPulseLast = fixedUpdateCurrentTime;
				pulseDelta = 0.0f;
			}
			float tmp;
			float pulseDistance = (pulseDelta / mapViewPulseLength);
			tmp = lerp(mapViewPulseStartColor.r, mapViewPulseEndColor.r, pulseDistance);
			mapViewPulseCurrentColor.r = tmp;
			tmp = lerp(mapViewPulseStartColor.g, mapViewPulseEndColor.g, pulseDistance);
			mapViewPulseCurrentColor.g = tmp;
			tmp = lerp(mapViewPulseStartColor.b, mapViewPulseEndColor.b, pulseDistance);
			mapViewPulseCurrentColor.b = tmp;
			tmp = lerp(mapViewPulseStartColor.a, mapViewPulseEndColor.a, pulseDistance);
			mapViewPulseCurrentColor.a = tmp;
			mapViewPulseTexture.SetPixel(0,0,mapViewPulseCurrentColor);
			mapViewPulseTexture.Apply();
		}
		/*
		 * A general lerp function. Use it in your scripts, it's really useful!
		 */
		private float lerp(float startValue, float endValue, float timeValue)
		{
			return (1.0f-timeValue)*startValue + timeValue*endValue;
		}

		private void loadOptions()
		{
			ConfigNode tmpNode = ConfigNode.Load(nodeName);
			MenuOptions tmpOpt;
			foreach(ConfigNode.Value o in tmpNode.values)
			{
				tmpOpt = (MenuOptions) Enum.Parse(typeof(MenuOptions), o.name, true);
				if (!options.ContainsKey(tmpOpt))
				{
					options.Add(tmpOpt, "true" == o.value);
				}
				else{
					options[tmpOpt] = ("true" == o.value);
				}
			}
		}

		/*
		 * Render the screen-size map view pulse rect.
		 */
		private void drawMapViewPulse()
		{
			Rect screenBox = new Rect(0,0,Screen.width - 1, Screen.height - 1);
			//GUI.Box(screenBox, GUIContent.none, mapViewPulseStyle);
			GUI.DrawTexture(screenBox, mapViewPulseTexture);
		}


		/*
		 * Call every FixedUpdate.
		 * Iterate through active thermometer states, called
		 * whether they are flashing or not.
		 */
		private void calculateFlashes()
		{
			flashing = false;
			float flashDelta;
			//for(int i=0; i<thermometerStates.Count; i++)
			foreach(KeyValuePair<int, ThermometerState> entry in thermometerStates)
			{
				//if (thermometerStates[i].ratio > flashThreshold)
				if (entry.Value.ratio > flashThreshold)
				{
					
					if (!entry.Value.flashState)
					{
						entry.Value.flashStarted = fixedUpdateCurrentTime;
						flashDelta = 0.0f;
					}
					flashing =  true;
					entry.Value.flashState = true;
					flashDelta = fixedUpdateCurrentTime - entry.Value.flashStarted;
					/*
					 * If you're setting a bool when something is true, you don't
					 * really need to bother with if blah else blah.
					 * 
					 * In this case, FlashOn turns on and off depending on the
					 * thermometerFlashOnPeriod value and flashDelta.
					 */
					entry.Value.FlashOn = (flashDelta <= thermometerFlashOnPeriod);
					if (entry.Value.FlashOn)
					{
						entry.Value.flashBG.x = entry.Value.thermFG.x - 1;
						entry.Value.flashBG.y = entry.Value.thermFG.y - 1;
					}
					if (flashDelta > thermometerFlashPeriod)
					{
						//Debug.Log("[HeatWarning] Resetting flashStarted value to " + fixedUpdateCurrentTime.ToString("F2") + " (delta " + flashDelta + ")");
						entry.Value.flashStarted = fixedUpdateCurrentTime;
						flashDelta = 0.0f;
					}
				}
				else{
					entry.Value.flashState = false;
				}
			}
			if (!flashing)
			{
				flashOverride = false; //Reset the audio alert if no parts were flashing.
			}
		}
		/*
		 * Jump here from OnFixedUpdate and call all the functions
		 * that calculate all the thermometers and set their positions.
		 */
		private void calculateEveryPart()
		{
			Vessel v = FlightGlobals.ActiveVessel;
			overHeating = false;
			foreach(Part p in v.parts)
			{
				calculateThermometer(p);
			}
			/*
			 * If not overheating, turn the override button off
			 * and reset the heat override.
			 */
			if (!overHeating)
			{
				heatOverride = false;
				removeHeatOverrideButton();
				buttonState = AlertButtonState.OFF;
			}
			/*
			 * Add an override button if we're overheating
			 * and the user hasn't clicked an override button.
			 * Also activate the buttonState enum.
			 */
			if (overHeating && !heatOverride)
			{
				if (heatButton)
				{
					if (buttonState == AlertButtonState.CRITICAL)
					{
						heatButton.SetTexture(heatButtonTexture);
						buttonState = AlertButtonState.HEAT;
					}
				}
				else
				{
					buttonState = AlertButtonState.HEAT;
					addHeatOverrideButton(heatButtonTexture);
				}
			}
			if (flashing && !flashOverride)
			{

				if (heatButton)
				{
					if (buttonState == AlertButtonState.HEAT)
					{
						heatButton.SetTexture(criticalHeatButtonTexture);
						buttonState = AlertButtonState.CRITICAL;
					}
				}
				else
				{
					buttonState = AlertButtonState.CRITICAL;
					addHeatOverrideButton(criticalHeatButtonTexture); //It's cheap but it works. 
				}
			}
			if (!flashing && overHeating && heatOverride && heatButton)
			{
				removeHeatOverrideButton();
				buttonState = AlertButtonState.OFF;
			}
			if (overHeating && (inMapView || inIVA))
			{
				calculateMapViewPulse();
			}
			calculateFlashes();
		}
		/*
		 * Call every OnGUI to draw any active thermometers and flash rects.
		 */
		private void drawEveryPart()
		{
			if (!inMapView && !inIVA && overHeating)
			{
				foreach(KeyValuePair<int, ThermometerState> entry in thermometerStates)
				{
					if (entry.Value.flashState && entry.Value.FlashOn)
					{
						GUI.Box(entry.Value.flashBG, GUIContent.none, thermometerFlashStyle);
					}
					GUI.Box(entry.Value.thermBG, GUIContent.none, thermometerBGStyle);
					GUI.Box(entry.Value.thermFG, GUIContent.none, thermometerFGStyle);
				}
			}
		}
		/*
		 * Add a thermometer state to the list or
		 * set an appropriate thermometer state for
		 * every part that is overheating. Call
		 * every fixed update.
		 */
		private void calculateThermometer(Part p)
		{
			
			double tempRatio = p.temperature / p.maxTemp;
			if (tempRatio > tempThreshold) //Only display if ratio > threshold value.
			{
				overHeating = true;
				//Debug.Log("[HeatWarning] main camera: " + cam.name);
				if (!inMapView && !inIVA)
				{
					//Limit temperature value to maxTemp.
					double tempClipped = p.temperature;
					if (tempClipped > p.maxTemp)
					{
						tempClipped = p.maxTemp;
					}
					/*
					 * Determine how wide the thermometer foreground should be.
					 */
					double minTemp = p.maxTemp * tempThreshold;
					double tempWidth = tempClipped - minTemp;
					double widthScale = thermometerSize.width / (p.maxTemp - minTemp);
					double scaledTempWidth = tempWidth * widthScale;

					/*
					 * Set thermometer position.
					 */
					Vector3 pPos = FlightCamera.fetch.mainCamera.WorldToScreenPoint(p.transform.position);

					//Add thermometer state if it does not exist.
					if (!thermometerStates.ContainsKey(p.GetInstanceID()))
					{
						/*
						 * Add new thermometerstate struct to the list.
						 */
						ThermometerState newState = new ThermometerState();
						newState.ratio = tempRatio;
						newState.thermFG = new Rect(pPos.x, Screen.height - pPos.y, (float)scaledTempWidth, thermometerSize.height);
						newState.thermBG = new Rect(pPos.x + newState.thermFG.width, Screen.height - pPos.y, thermometerSize.width - newState.thermFG.width, thermometerSize.height);
						newState.flashBG = new Rect(flashSize);
						thermometerStates[p.GetInstanceID()] = newState;
					}
					else{
						ThermometerState state = thermometerStates[p.GetInstanceID()];
						state.ratio = tempRatio;
						state.thermFG.x = pPos.x;
						state.thermFG.y = Screen.height - pPos.y;
						state.thermFG.width = (float)scaledTempWidth;
						state.thermBG.x = pPos.x + state.thermFG.width;
						state.thermBG.y = Screen.height - pPos.y;
						state.thermBG.width = thermometerSize.width - state.thermFG.width;
						//state.thermFG = new Rect(pPos.x, Screen.height - pPos.y, (float)scaledTempWidth, thermometerSize.height);
						//state.thermBG = new Rect(pPos.x + newState.thermFG.width, Screen.height - pPos.y, thermometerSize.width - newState.thermFG.width, thermometerSize.height);
					}
				}
				/*else{
					Debug.Log("[HeatWarning] Not rendering thermometer in map view.");
				}*/
			}
			else{ //Destroy thermometer state if it exists.
				thermometerStates.Remove(p.GetInstanceID());
			}
		}
		/* Not used */
		private void drawThermometer(Part p)
		{
			double tempRatio = p.temperature / p.maxTemp;
			if (tempRatio > tempThreshold) //Only display if ratio > threshold value.
			{
				overHeating = true;
				//Debug.Log("[HeatWarning] main camera: " + cam.name);
				if (!inMapView)
				{
					//Limit temperature value to maxTemp.
					double tempClipped = p.temperature;
					if (tempClipped > p.maxTemp)
					{
						tempClipped = p.maxTemp;
					}
					/*
					 * Determine how wide the thermometer foreground should be.
					 */
					double minTemp = p.maxTemp * tempThreshold;
					double tempWidth = tempClipped - minTemp;
					double widthScale = thermometerSize.width / (p.maxTemp - minTemp);
					double scaledTempWidth = tempWidth * widthScale;

					/*
					 * Set thermometer position.
					 */
					Vector3 pPos = FlightCamera.fetch.mainCamera.WorldToScreenPoint(p.transform.position);
					Rect thermFG = new Rect(pPos.x, Screen.height - pPos.y, (float)scaledTempWidth, thermometerSize.height);
					Rect thermBG = new Rect(pPos.x + thermFG.width, Screen.height - pPos.y, thermometerSize.width - thermFG.width, thermometerSize.height);

					/*
					 * Draw the thermometer.
					 */
					GUI.Box(thermBG, GUIContent.none, thermometerBGStyle);
					GUI.Box(thermFG, GUIContent.none, thermometerFGStyle);
				}
				/*else{
					Debug.Log("[HeatWarning] Not rendering thermometer in map view.");
				}*/
			}
		}
		/*
		 * Call every FixedUpdate.
		 * Make sure the audio warning is playing if
		 * there's a heat warning, but not if it's
		 * overridden.
		 */
		private void checkSound()
		{
			/*
			 * Only bother with sound if audio is enabled in the options.
			 */
			if (options[MenuOptions.AUDIO] && options[MenuOptions.ENABLED])
			{
				/*
				 * Move the sound to the position of the active vessel.
				 * There's probably a better way to keep the sound central.
				 */
				soundObject.transform.position = FlightGlobals.ActiveVessel.transform.position;
				/*
				 * And now a big mess of conditional logic to
				 * make sure the sounds play and stop correctly.
				 */
				if (overHeating) //If overheating...
				{
					if (!heatOverride) //If heat warning isn't overridden...
					{
						if (!flashing) //And not critical...
						{
							if (criticalSrc.isPlaying) //If critial warning is playing...
							{
								criticalSrc.Stop();
							}						
							if (!src.isPlaying) //If overheat warning isn't playing...
							{
								src.Play();
							}						
						}
						else{ //If heat is critical...
							if (!flashOverride) //And critical warning is not overridden...
							{
								if (src.isPlaying)
								{
									src.Stop();
								}
								if (!criticalSrc.isPlaying)
								{
									criticalSrc.Play();
								}
							}
							else{ //If critical warning is overridden...
								if (criticalSrc.isPlaying)
								{
									criticalSrc.Stop();
								}
								if (src.isPlaying)
								{
									src.Stop();
								}
							}
						}
					}
					else{ //If heat warning is overridden...
						if (flashing) //And heat is critical...
						{
							if (!flashOverride) //And no critical override...
							{
								if (src.isPlaying)
								{
									src.Stop();
								}
								if (!criticalSrc.isPlaying)
								{
									criticalSrc.Play();
								}
							}
							else{ //If critical heat warning is overridden...
								if (src.isPlaying)
								{
									src.Stop();
								}
								if (criticalSrc.isPlaying)
								{
									criticalSrc.Stop();
								}							
							}
						}
						else{ //If heat is not critical...
							if (src.isPlaying){
								src.Stop();
							}
							if (criticalSrc.isPlaying){
								criticalSrc.Stop();
							}
						}
					}
				}
				else{ //If not overheating...
					if (src.isPlaying){
						src.Stop();
					}
					if (criticalSrc.isPlaying){
						criticalSrc.Stop();
					}
				}
			}
			else{ //Make sure the sound will stop if audio is disabled mid-alert.
				if (src.isPlaying)
				{
					src.Stop();
				}
				if (criticalSrc.isPlaying){
					criticalSrc.Stop();
				}
			}
		}

		private void addHeatOverrideButton(Texture2D btnTex)
		{
			if (ApplicationLauncher.Ready) //Only bother if the application launcher is ready.
			{
				//Debug.Log("[HeatWarning] ApplicationLauncher is ready.");
				/*
				 * A temporary bool for the Contains() call.
				 */
				bool btnExists = false;
				/*
				 * Check the launcher button doesn't exist before creating one.
				 */
				if(ApplicationLauncher.Instance.Contains(heatButton, out btnExists) == false)
				{
					//buttonState = AlertButtonState.HEAT;
					//Debug.Log("[HeatWarning] Adding override button.");
					heatButton = ApplicationLauncher.Instance.AddModApplication(
						onTempBtnClick, //Callback when button is toggled on.
						onTempBtnClick, //Callback when button is toggled off.
						onBtnHover, //Callback when mouse is hovering over button.
						onBtnHoverOut, //Callback when mouse moves away from the button.
						onBtnEnable, //Callback for when the button is shown or enabled by the launcher.
						onBtnDisable, //Callback for when the button is hidden or disabled by the launcher.
						visibleInScenes, //The scenes this button will be visible in.
						btnTex //A 38x38 Texture oboect.
					);	
				}
			}
		}
		/* Default placeholder */
		private void onBtnHover()
		{
		}
		/* Default placeholder */
		private void onBtnHoverOut()
		{
		}
		/* Default placeholder */
		private void onBtnEnable()
		{
		}
		/* Default placeholder */
		private void onBtnDisable()
		{
		}
		private void removeHeatOverrideButton()
		{
			if (ApplicationLauncher.Ready)
			{
				if (heatButton)
				{
					ApplicationLauncher.Instance.RemoveModApplication(heatButton);
					heatButton = null;
					//buttonState = AlertButtonState.OFF;
				}
			}
		}

		/* GameEvents callback. */
		private void onMapEntered()
		{
			inMapView = true;
		}
		/* GameEvents callback. */
		private void onMapExited()
		{
			inMapView = false;
		}
			
		private void onTempBtnClick()
		{
			heatOverride = true;
			if (flashing)
			{
				flashOverride = true;
			}
			removeHeatOverrideButton();
		}
		private void onCritBtnClick()
		{
			heatOverride = true;
			flashOverride = true;
			removeHeatOverrideButton();
		}
		private void onPartDestroyed(Part p)
		{
			thermometerStates.Remove(p.GetInstanceID());
		}
		/*
		 * Remove any thermometerstates that are not part of the current
		 * vessel any more.
		 */
		private void onVesselWasModified(Vessel v)
		{
			if (v.id == FlightGlobals.ActiveVessel.id) //Just in case the modified vessel isn't the active vessel.
			{
				//This rubs me the wrong way.
				List<int> pIDs = new List<int>();
				List<int> removeThese = new List<int>();
				//This many loops makes me sad.
				foreach(Part p in v.parts)
				{
					pIDs.Add(p.GetInstanceID());
				}
				foreach(KeyValuePair<int, ThermometerState> entry in thermometerStates)
				{
					if (!pIDs.Contains(entry.Key))
					{
						removeThese.Add(entry.Key);
					}
				}
				foreach(int k in removeThese)
				{
					thermometerStates.Remove(k);
				}
			}
		}

		/*
		 * Just a sketch to remember the template for applauncher buttons.
		 */
		/*
		public ApplicationLauncherButton AddModApplication(
			RUIToggleButton.OnTrue onTrue,
			RUIToggleButton.OnFalse onFalse,
			RUIToggleButton.OnHover onHover,
			RUIToggleButton.OnHoverOut onHoverOut,
			RUIToggleButton.OnEnable onEnable,
			RUIToggleButton.OnDisable onDisable,
			ApplicationLauncher.AppScenes visibleInScenes,
			Texture texture
		)
		{
		}
		*/
	}
}

