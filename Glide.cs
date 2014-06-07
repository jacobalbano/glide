using System;
using System.Collections.Generic;
using System.Reflection;

namespace GlideTween
{
	public partial class Glide
	{
		[Flags]
		private enum Behavior
		{
			None,
			Reflect,
			Rotation,
			Round,
			HexColor
		}

#region Callbacks
		private Func<float, float> ease;
        private Action begin, update, complete;
#endregion

#region Timing
        private bool paused;
        private float delay;
        private float duration;

        private float time;
        private float elapsed;
#endregion

        private int repeatCount;
        private Behavior behavior;

        private List<float> start, range, end;
        private List<GlideInfo> vars;

        private object target;
        private GlideManagerImpl parent;
        
        public float TimeRemaining { get { return duration - time; } }
        public float Completion { get { var c = time / duration; return c < 0 ? 0 : (c > 1 ? 1 : c); } }
		
		public Glide()
		{
			elapsed = 0;
			
			vars = new List<GlideInfo>();
			start = new List<float>();
			range = new List<float>();
			end = new List<float>();
		}

        internal void Update()
		{	
			if (paused)
			{
				return;
			}
			
			if (delay > 0)
			{
				delay -= elapsed;
				return;
			}
			
			if (time == 0)
			{
				if (begin != null)
					begin();
			}
			
			
			if (update != null)
			{
				update();
			}
			
			time += elapsed;
			float t = time / duration;
			
			if (time >= duration)
			{
				if (repeatCount > 0)
				{
					--repeatCount;
					time = t = 0;
				}
				else if (repeatCount < 0)
				{
					if (complete != null)
					{
						complete();
					}
					
					time = t = 0;
				}
				else
				{
					if (complete != null)
					{
						complete();
					}
					
					time = duration;
					t = 1;
                    parent.Remove(this);
				}
				
				if (time == 0)
				{
					//	If the timer is zero here, we just restarted.
					//	If reflect mode is on, flip start to end
					if ((behavior & Behavior.Reflect) == Behavior.Reflect)
					{
						Reverse();
					}
				}
			}
			
			if (ease != null)
			{
				t = ease(t);
			}
			
			int i = vars.Count;			
			while (i --> 0)
			{
				float value = start[i] + range[i] * t;
				if ((behavior & Behavior.Round) == Behavior.Round)
				{
					value = (float) Math.Round(value);
				}
				
				if ((behavior & Behavior.Rotation) == Behavior.Rotation)
				{
					float angle = value % 360.0f;
			
					if (angle < 0)
					{
						angle += 360.0f;
					}
					
					value = angle;
				}
				
				if ((behavior & Behavior.HexColor) == Behavior.HexColor)
				{
					var from = (int) start[i];
					var to = from + (int) range[i];
					
					var r = from >> 16 & 0xFF;
					var g = from >> 8 & 0xFF;
					var b = from & 0xFF;
					var startR = r / 255f;
					var startG = g / 255f;
					var startB = b / 255f;
					var rangeR = ((to >> 16 & 0xFF) / 255f) - startR;
					var rangeG = ((to >> 8 & 0xFF) / 255f) - startG;
					var rangeB = ((to & 0xFF) / 255f) - startB;
					
					r = (int) ((startR + rangeR * t) * 255);
					g = (int) ((startG + rangeG * t) * 255);
					b = (int) ((startB + rangeB * t) * 255);
					value = r << 16 | g << 8 | b;
				}
				
				vars[i].Value = value;
			}
		}
		
#region Behavior
		
		/// <summary>
		/// Apply target values to a starting point before tweening.
		/// </summary>
		/// <param name="values">The values to apply, in an anonymous type ( new { prop1 = 100, prop2 = 0} ).</param>
		/// <returns>A reference to this.</returns>
		public Glide From(object values)
		{			
			foreach (PropertyInfo property in values.GetType().GetProperties())
			{
				int index = vars.FindIndex(i => String.Compare(i.Name, property.Name, true) == 0);
				if (index >= 0)
				{
					//	if we're already tweening this value, adjust the range
					var info = vars[index];
					
					var to = new GlideInfo(values, property.Name, false);
					info.Value = to.Value;
				
					start[index] = info.Value;
					range[index] = this.end[index] - start[index];
				}
				else
				{
					//	if we aren't tweening this value, just set it
					var info = new GlideInfo(target, property.Name, true);
					var to = new GlideInfo(values, property.Name, false);
					info.Value = to.Value;
				}
			}
			
			return this;
		}
		
		/// <summary>
		/// Set the easing function.
		/// </summary>
		/// <param name="ease">The Easer to use.</param>
		/// <returns>A reference to this.</returns>
		public Glide Ease(Func<float, float> ease)
		{
			this.ease = ease;
			return this;
		}
		
		/// <summary>
		/// Set a function to call when the tween begins (useful when using delays).
		/// </summary>
		/// <param name="callback">The function that will be called when the tween starts, after the delay.</param>
		/// <returns>A reference to this.</returns>
		public Glide OnBegin(Action callback)
		{
			begin = callback;
			return this;
		}
		
		/// <summary>
		/// Set a function to call when the tween finishes.
		/// If the tween repeats infinitely, this will be called each time; otherwise it will only run when the tween is finished repeating.
		/// </summary>
		/// <param name="callback">The function that will be called on tween completion.</param>
		/// <returns>A reference to this.</returns>
		public Glide OnComplete(Action callback)
		{
			complete = callback;
			return this;
		}
		
		/// <summary>
		/// Set a function to call as the tween updates.
		/// </summary>
		/// <param name="callback">The function to use.</param>
		/// <returns>A reference to this.</returns>
		public Glide OnUpdate(Action callback)
		{
			update = callback;
			return this;
		}
		
		/// <summary>
		/// Enable repeating.
		/// </summary>
		/// <param name="times">Number of times to repeat. Leave blank or pass a negative number to repeat infinitely.</param>
		/// <returns>A reference to this.</returns>
		public Glide Repeat(int times = -1)
		{
			repeatCount = times;
			return this;
		}
		
		/// <summary>
		/// Sets the tween to reverse every other time it repeats. Repeating must be enabled for this to have any effect.
		/// </summary>
		/// <returns>A reference to this.</returns>
		public Glide Reflect()
		{
			behavior |= Behavior.Reflect;
			return this;
		}
		
		/// <summary>
		/// Whether the property should be tweened as a color.
		/// </summary>
		/// <returns>A reference to this.</returns>
		public Glide HexColor()
		{
			behavior |= Behavior.HexColor;
			return this;
		}
		
		/// <summary>
		/// Swaps the start and end values of the tween.
		/// </summary>
		/// <returns>A reference to this.</returns>
		public Glide Reverse()
		{	
			int count = vars.Count;			
			while (count --> 0)
			{
				float s = start[count];
				float r = range[count];
				
				//	Set start to end and end to start
				start[count] = s + r;
				range[count] = s - (s + r);
			}
			
			return this;
		}
		
		/// <summary>
		/// Whether this tween handles rotation.
		/// </summary>
		/// <returns>A reference to this.</returns>
		public Glide Rotation()
		{
			behavior |= Behavior.Rotation;
			
			int count = vars.Count;			
			while (count --> 0)
			{
				float angle = start[count];
				float r = angle + range[count];
					
				float d = r - angle;
				float a = (float) Math.Abs(d);
				
				if (a > 181)
				{
					r = (360 - a) * (d > 0 ? -1 : 1);
				}
				else if (a < 179)
				{
					r = d;
				}
				else
				{
					r = 180;
				}
				
				range[count] = r;
			}
			
			return this;
		}
		
		/// <summary>
		/// Whether tweened values should be rounded to integer values.
		/// </summary>
		/// <returns>A reference to this.</returns>
		public Glide Round()
		{
			behavior |= Behavior.Round;
			return this;
		}
#endregion
				
#region Control
		/// <summary>
		/// Remove tweens from the tweener without calling their complete functions.
		/// </summary>
		public void Cancel()
		{
            parent.Remove(this);
		}
		
		/// <summary>
		/// Assign tweens their final value and remove them from the tweener.
		/// </summary>
		public void CancelAndComplete()
		{
			time = duration;
			update = null;
            parent.Remove(this);
		}
		
		/// <summary>
		/// Set tweens to pause. They won't update and their delays won't tick down.
		/// </summary>
		public void Pause()
		{
    		paused = true;
		}
		
		/// <summary>
		/// Toggle tweens' paused value.
		/// </summary>
		public void PauseToggle()
		{
			paused = !paused;
		}
		
		/// <summary>
		/// Resumes tweens from a paused state.
		/// </summary>
		public void Resume()
		{
			paused = false;
		}
#endregion
	}
}