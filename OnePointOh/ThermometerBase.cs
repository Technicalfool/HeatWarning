using UnityEngine;
using System;

namespace HeatWarning
{
	public class ThermometerBase : MonoBehaviour
	{
		/*
		 * The thermometer is aligned around this part.
		 */
		protected Part anchor;
		private float _currentRatio;
		private float _startRatio;
		private float _criticalRatio;
		protected float currentRatio{
			get{
				return _currentRatio;
			}
			set{
				_currentRatio = value;
			}
		}
		protected float startRatio{
			get{
				return _startRatio;
			}
			set{
				_startRatio = value;
			}
		}
		protected float criticalRatio{
			get{
				return _criticalRatio;
			}
			set{
				_criticalRatio = value;
			}
		}

		public ThermometerBase ()
		{
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

