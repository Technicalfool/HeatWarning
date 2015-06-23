using UnityEngine;
using System;


namespace HeatWarning
{
	public class LinearThermometer : ThermometerBase
	{
		private Rect thermometerSize;
		private Rect thermometerCurrentSize;
		private Rect thermometerForeground;
		private Rect thermometerOutlineSize;

		private Color thermometerBGCol;
		private Color thermometerFGCol;
		private Color thermometerFlashCol;
		private Texture2D BGTex;
		private Texture2D FGTex;
		private Texture2D FlashTex;
		private GUIStyle BGStyle;
		private GUIStyle FGStyle;
		private GUIStyle FlashStyle;

		private float animationTime;
		private float animationStarted;
		private float lastTick;
		private float thisTick;
		private float tickLength;

		private bool flashState;
		private float lastFlash;
		private float flashPeriod;
		private float flashOnPeriod;

		//Make sure the anchor variable gets set in the base constructor.
		public LinearThermometer(Part p) : base(p)
		{
			thermometerBGCol = new Color(0.3f,0.0f,0.0f,0.65f);
			thermometerFGCol = new Color(1.0f,0.5f,0.0f,0.65f);
			thermometerFlashCol = new Color(1.0f,1.0f,1.0f,0.65f);

			thermometerSize = new Rect(0,0,128,8); //Normal size.
			thermometerCurrentSize = new Rect(thermometerSize); //Current size during animations.
			thermometerForeground = new Rect(0,0,128,8); //Current size of highlighted section.
			thermometerOutlineSize = new Rect(0,0,130,10); //Current critical alert outline size.

			BGStyle = new GUIStyle();
			FGStyle = new GUIStyle();
			FlashStyle = new GUIStyle();

			BGTex = new Texture2D(1,1);
			BGTex.SetPixel(0,0,thermometerBGCol);
			BGTex.Apply();
			BGStyle.normal.background = BGTex;
			FGTex = new Texture2D(1,1);
			FGTex.SetPixel(0,0,thermometerFGCol);
			FGTex.Apply();
			FGStyle.normal.background = FGTex;
			FlashTex = new Texture2D(1,1);
			FlashTex.SetPixel(0,0,thermometerFlashCol);
			FlashTex.Apply();
			FlashStyle.normal.background = FlashTex;
			flashPeriod = 300;
			flashOnPeriod = 50;



			animationTime = 0.5f; //Open/close animations last for this many seconds.
			animationStarted = Time.realtimeSinceStartup;
			lastFlash = animationStarted;
			flashState = false;
		}
		public void tick()
		{
			thisTick = Time.realtimeSinceStartup;
			calculateBase();
			if (!(_state == ThermometerStates.OPENING) && !(_state == ThermometerStates.CLOSING))
			{

				if (_currentRatio >= _startRatio)
				{
					if (_state == ThermometerStates.INACTIVE)
					{
						show();
					}
					//thermometerForeground.width = thermometerSize.width * _currentRatio;
				}
				else
				{
					if (_state == ThermometerStates.ACTIVE)
					{
						hide();
					}
				}
			}
			tickLength = thisTick - lastTick;
			lastTick = thisTick;
			switch(_state)
			{
				case ThermometerStates.OPENING:
					_tick_opening();
					break;
				case ThermometerStates.CLOSING:
					_tick_closing();
					break;
			}
			if (visible())
			{
				//Set position.
				thermometerCurrentSize.x = anchor.transform.position.x;
				thermometerCurrentSize.y = anchor.transform.position.y;
				thermometerForeground.x = thermometerCurrentSize.x;
				thermometerForeground.y = thermometerCurrentSize.y;
				thermometerOutlineSize.x = thermometerCurrentSize.x - 1;
				thermometerOutlineSize.y = thermometerCurrentSize.y - 1;
				thermometerForeground.height = thermometerCurrentSize.height;
				//Clip temperature to a maximum value.
				double tempClipped = anchor.temperature;
				if (tempClipped > anchor.maxTemp)
				{
					tempClipped = anchor.maxTemp;
				}
				//Set the foreground rectangle width depending on temperature.
				double minTemp = anchor.maxTemp * _startRatio;
				double tempWidth = tempClipped - minTemp;
				double widthScale = thermometerSize.width / (anchor.maxTemp - minTemp);
				thermometerForeground.width = (float)(tempWidth * widthScale);
				//Calculate flash.
				if (thisTick - lastFlash > flashPeriod)
				{
					lastFlash = thisTick;
				}
				if (thisTick - lastFlash <= flashOnPeriod)
				{
					flashState = true;
				}
				else{
					flashState = false;
				}
			}
		}
		public override void draw()
		{
			/*
			 * Render the outer border if any of these are true.
			 */
			if (
				_state == ThermometerStates.OPENING ||
				_state == ThermometerStates.CLOSING ||
				flashState
			)
			{
				GUI.DrawTexture(thermometerOutlineSize,FlashTex);
			}
			/*
			 * Only need to do anything if visible.
			 */
			if (visible())
			{
				/*
				 * Render background box.
				 */
				GUI.DrawTexture(thermometerCurrentSize, BGTex);
				/*
				 * Render foreground box!
				 */
				GUI.DrawTexture(thermometerForeground, FGTex);
			}
		}
		public override void show()
		{
			Debug.Log("[HeatWarning] Showing thermometer for " + anchor.name);
			_state = ThermometerStates.OPENING;
			animationStarted = Time.realtimeSinceStartup;
		}
		public override void hide()
		{
			Debug.Log("[HeatWarning] Hiding thermometer for " + anchor.name);
			_state = ThermometerStates.CLOSING;
			animationStarted = Time.realtimeSinceStartup;
		}
		public bool visible()
		{
			return 	(
						(_state == ThermometerStates.ACTIVE) ||
						(_state == ThermometerStates.OPENING) ||
						(_state == ThermometerStates.CLOSING)
			);
		}
		private void _tick_opening()
		{
			double timeRatio = animationTime / (thisTick - animationStarted);
			if (timeRatio <= 1.0)
			{
				thermometerCurrentSize.width = (float)(thermometerSize.width * timeRatio);
				thermometerCurrentSize.height = (float)(thermometerSize.height * timeRatio);
				thermometerOutlineSize.width = thermometerCurrentSize.width + 2;
				thermometerOutlineSize.height = thermometerCurrentSize.height + 2;
			}
			else{
				thermometerCurrentSize.width = thermometerSize.width;
				thermometerCurrentSize.height = thermometerSize.height;
				thermometerOutlineSize.width = thermometerCurrentSize.width + 2;
				thermometerOutlineSize.height = thermometerCurrentSize.height + 2;
				_state = ThermometerStates.ACTIVE;
			}
		}
		private void _tick_closing()
		{
			double timeRatio = animationTime / (thisTick - animationStarted);
			if (timeRatio <= 1.0)
			{
				thermometerCurrentSize.width = thermometerSize.width - (float)(thermometerSize.width * timeRatio);
				thermometerCurrentSize.height = thermometerSize.height - (float)(thermometerSize.height * timeRatio);
				thermometerOutlineSize.width = thermometerCurrentSize.width + 2;
				thermometerOutlineSize.height = thermometerCurrentSize.height + 2;
			}
			else{
				thermometerCurrentSize.width = thermometerSize.width;
				thermometerCurrentSize.height = thermometerSize.height;
				thermometerOutlineSize.width = thermometerCurrentSize.width + 2;
				thermometerOutlineSize.height = thermometerCurrentSize.height + 2;
				_state = ThermometerStates.INACTIVE;
			}
		}
	}
}

