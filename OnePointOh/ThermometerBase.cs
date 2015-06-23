using UnityEngine;
using System;

namespace HeatWarning
{
	public enum ThermometerStates{
		OPENING,
		ACTIVE,
		CLOSING,
		INACTIVE
	}
	public abstract class ThermometerBase
	{
		public ThermometerBase(Part p)
		{
			anchor = p;
			state = new ThermometerStates();
			state = ThermometerStates.INACTIVE;
		}

		/*
		 * The thermometer is aligned around this part.
		 */
		protected Part anchor;
		protected ThermometerStates _state;
		protected double _currentRatio;
		protected double _startRatio;
		protected double _criticalRatio;

		public ThermometerStates state{
			get{
				return _state;
			}
			protected set{
				_state = value;
			}
		}

		public double currentRatio{
			get{
				return _currentRatio;
			}
			protected set{
				_currentRatio = value;
			}
		}
		public double startRatio{
			get{
				return _startRatio;
			}
			protected set{
				_startRatio = value;
			}
		}
		public double criticalRatio{
			get{
				return _criticalRatio;
			}
			protected set{
				_criticalRatio = value;
			}
		}



		public abstract void draw();
		/*
		 * If the thermometer has any starting animations, trigger
		 * them in this method.
		 */
		public abstract void show();
		/*
		 * If the thermometer has any animations for closing, trigger
		 * them in this method.
		 */
		public abstract void hide();

		protected void calculateBase()
		{
			_currentRatio = anchor.skinMaxTemp / anchor.skinTemperature;
		}
	}
}

