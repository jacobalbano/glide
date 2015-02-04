
using System;

namespace Glide
{
	/// <summary>
	/// A collection of stateless easing functions.
	/// </summary>
	public class Approach
	{
		private const float DEG = -180 / (float) Math.PI;
		private const float RAD = (float) Math.PI / -180;
		
		/// <summary>
		/// Approach one value to another by a constant distance scalar.
		/// </summary>
		/// <param name="target">The current value.</param>
		/// <param name="to">The desired value.</param>
		/// <param name="amount">The amount to move each time. Default to 0.1 (1/10 of the distance)</param>
		/// <returns>The new value;</returns>
		public void TowardsWithDecay(ref float target, float to, float amount = 0.1f)
		{
			target += (to - target) * amount;
		}
		
		
		public float Angle(float x, float y, float targetX, float targetY, float currentAngle, float lookEase = 0.3f)
		{
			currentAngle *= RAD;
			float cx = (float) Math.Cos(currentAngle), cy = (float) Math.Sin(currentAngle);
			float tnormX = targetX - x, tnormY = targetY - y;
			Normalize(ref tnormX, ref tnormY);
			
			cx += (tnormX - cx) * lookEase;
			cy += (tnormY - cy) * lookEase;
			
			var angle = (float) Math.Atan2(cy, cx) * DEG;
			angle %= 360;
			if (angle < 0)
				angle += 360;
			
			return angle;
		}
		
		static void Normalize(ref float x, ref float y)
		{
			if (x == 0 && y == 0)
				return;
			
			var length = (float) Math.Sqrt(x * x + y * y);
			var scale = 1f / length;
			x = x * scale;
			y = y * scale;
		}
	}
}
