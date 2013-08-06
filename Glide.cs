using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace GlideTween
{
	public class Glide
	{
		[Flags]
		private enum Behavior
		{
			None,
			Reflect,
			Rotation,
			Round
		}

#region Callbacks
		public delegate float Easer(float t);
		public delegate void Callback();
		
		private Easer ease;
		private Callback update;
		private Callback complete;
#endregion

#region Timing
		private bool paused;
		private float delay;
		private float duration;
		
		private float time;
#endregion
		
		private int repeatCount;
		private Behavior behavior;
		
		private List<float> start, range;
		private List<GlideInfo> vars;
		
		private object target;
		private Glide parent;
		
#region Some stuff
		private List<Glide> tweens;
		private List<Glide> toRemove, toAdd;
		private float elapsed;
		
		static Glide()
		{
			Tweener = new Glide();
		}
		
		public static readonly Glide Tweener;
		
		/// <summary>
		/// Tweens a set of numeric properties on an object.
		/// </summary>
		/// <param name="target">The object to tween.</param>
		/// <param name="values">The values to tween to, in an anonymous type ( new { prop1 = 100, prop2 = 0} ).</param>
		/// <param name="duration">Duration of the tween in seconds.</param>
		/// <param name="delay">Delay before the tween starts, in seconds.</param>
		/// <returns>The tween created, for setting properties on.</returns>
		public Glide Tween(object target, object values, float duration, float delay = 0)
		{
			if (parent != null)
				throw new Exception("Tween can only be called from standalone instances!");
			
			var glide = new Glide();
			
			glide.target = target;
			glide.duration = duration;
			glide.delay = delay;
			
			foreach (PropertyInfo property in values.GetType().GetProperties())
			{
				var info = new GlideInfo(target, property.Name);
				var to = new GlideInfo(values, property.Name, false);
			
				float start = info.Value;
				float range = to.Value - start;
				
				glide.vars.Add(info);
				glide.start.Add(start);
				glide.range.Add(range);
			}
			
			glide.parent = this;
			
			toAdd.Add(glide);
			
			return glide;
		}
		
		/// <summary>
		/// Starts a simple timer for setting up callback scheduling.
		/// </summary>
		/// <param name="duration">How long the timer will run for, in seconds.</param>
		/// <param name="delay">How long to wait before starting the timer, in seconds.</param>
		/// <returns>The tween created, for setting properties.</returns>
		public Glide Timer(float duration, float delay)
		{
			return Tween(new object(), new {}, duration, delay);
		}
		
		/// <summary>
		/// Updates the tweener and all objects it contains.
		/// </summary>
		/// <param name="secondsElapsed">Seconds elapsed since last update.</param>
		public void Update(float secondsElapsed)
		{
			foreach (var glide in tweens)
			{
				glide.elapsed = secondsElapsed;
				glide.Update();
			}
			
			foreach (var add in toAdd)
			{
				tweens.Add(add);
			}
			
			foreach (var remove in toRemove)
			{
				tweens.Remove(remove);
			}
			
			toAdd.Clear();
			toRemove.Clear();
		}
		
#endregion
		public Glide()
		{
			tweens = new List<Glide>();
			toRemove = new List<Glide>();
			toAdd = new List<Glide>();
			elapsed = 0;
			
			vars = new List<GlideInfo>();
			start = new List<float>();
			range = new List<float>();
		}
		
		private void Update()
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
					
					time = t = 1;
					parent.toRemove.Add(this);
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
				}
				
				vars[i].Value = value;
			}
		}
		
#region Behavior
		
		/// <summary>
		/// Set the easing function.
		/// </summary>
		/// <param name="ease">The Easer to use.</param>
		/// <returns>A reference to this.</returns>
		public Glide Ease(Easer ease)
		{
			this.ease = ease;
			return this;
		}
		
		/// <summary>
		/// Set a function to call when the tween finishes.
		/// If the tween repeats infinitely, this will be called each time; otherwise it will only run when the tween is finished repeating.
		/// </summary>
		/// <param name="callback">The function that will be called on tween completion.</param>
		/// <returns>A reference to this.</returns>
		public Glide OnComplete(Callback callback)
		{
			complete = callback;
			return this;
		}
		
		/// <summary>
		/// Set a function to call as the tween updates.
		/// </summary>
		/// <param name="callback">The function to use.</param>
		/// <returns>A reference to this.</returns>
		public Glide OnUpdate(Callback callback)
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
			if (parent == null)
			{
				tweens.Clear();
			}
			else
			{
				parent.toRemove.Add(this);
			}
		}
		
		/// <summary>
		/// Assign tweens their final value and remove them from the tweener.
		/// </summary>
		public void CancelAndComplete()
		{
			if (parent == null)
			{
				foreach (var glide in tweens)
				{
					glide.time = glide.duration;
					glide.Update();
				}
				
				tweens.Clear();
			}
			else
			{
				time = duration;
				Update();
				parent.toRemove.Add(this);
			}
		}
		
		/// <summary>
		/// Set tweens to pause. They won't update and their delays won't tick down.
		/// </summary>
		public void Pause()
		{
			if (parent == null)
			{
				foreach (var tween in tweens)
				{
					tween.paused = true;
				}
			}
			else
			{
				paused = true;
			}
		}
		
		/// <summary>
		/// Toggle tweens' paused value.
		/// </summary>
		public void PauseToggle(params object[] targets)
		{
			if (parent == null)
			{
				foreach (var tween in tweens)
				{
					tween.paused = !tween.paused;
				}
			}
			else
			{
				paused = !paused;
			}
		}
		
		/// <summary>
		/// Resumes tweens from a paused state.
		/// </summary>
		/// <param name="targets">The tweens to resume. Pass no parameters to resume all paused tweens.</param>
		public void Resume(params object[] targets)
		{
			if (parent == null)
			{
				foreach (var tween in tweens)
				{
					tween.paused = false;
				}
			}
			else
			{
				paused = false;
			}
		}
#endregion

	}
}