
using System;

namespace Glide
{
	/// <summary>
	/// A collection of stateless easing functions.
	/// </summary>
	public class Approach
	{
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
	}
}
