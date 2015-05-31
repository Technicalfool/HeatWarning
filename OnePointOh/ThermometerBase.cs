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
	public class ThermometerBase
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
		protected float _currentRatio;
		protected float _startRatio;
		protected float _criticalRatio;

		public ThermometerStates state{
			get{
				return _state;
			}
			protected set{
				_state = value;
			}
		}

		public float currentRatio{
			get{
				return _currentRatio;
			}
			protected set{
				_currentRatio = value;
			}
		}
		public float startRatio{
			get{
				return _startRatio;
			}
			protected set{
				_startRatio = value;
			}
		}
		public float criticalRatio{
			get{
				return _criticalRatio;
			}
			protected set{
				_criticalRatio = value;
			}
		}



		public virtual void draw();
		/*
		 * If the thermometer has any starting animations, trigger
		 * them in this method.
		 */
		public virtual void show();
		/*
		 * If the thermometer has any animations for closing, trigger
		 * them in this method.
		 */
		public virtual void hide();

		protected void calculateBase()
		{
			_currentRatio = anchor.maxTemp / anchor.temperature;
		}
	}
}

