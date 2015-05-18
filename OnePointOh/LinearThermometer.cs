using System;


namespace HeatWarning
{
	public class LinearThermometer : ThermometerBase
	{
		private Rect thermometerSize;

		public LinearThermometer ()
		{
		}
		public void FixedUpdate()
		{
			calculateBase();
		}
	}
}

