using System;
using System.Collections.Generic;
using System.Reflection;
using Glide;

namespace Glide
{
	public partial class Tween
	{
		[Flags]
		public enum RotationUnit
		{
			Degrees,
			Radians
		}

#region Callbacks
		private Func<float, float> ease;
        private Action begin, update, complete;
#endregion

#region Timing
		public bool Paused { get; private set; }
        private float Delay;
        private float Duration;

        private float time;
        private float elapsed;
#endregion
		
		private bool firstUpdate;
        private int repeatCount;
        private Lerper.Behavior behavior;
        
        private List<GlideInfo> vars;
        private List<Lerper> lerpers;
        private List<object> start, end;
        private Dictionary<string, int> varHash;

        private object Target;
        private Tween.TweenerImpl parent;
        
		/// <summary>
		/// The time remaining before the tween ends or repeats.
		/// </summary>
        public float TimeRemaining { get { return Duration - time; } }
        
        /// <summary>
        /// A value between 0 and 1, where 0 means the tween has not been started and 1 means that it has completed.
        /// </summary>
        public float Completion { get { var c = time / Duration; return c < 0 ? 0 : (c > 1 ? 1 : c); } }
        
        public bool Looping { get { return repeatCount != 0; } }
		
		public Tween()
		{
			firstUpdate = true;
			elapsed = 0;
			
			varHash = new Dictionary<string, int>();
			vars = new List<GlideInfo>();
			lerpers = new List<Lerper>();
			start = new List<object>();
			end = new List<object>();
			behavior = Lerper.Behavior.None;
		}

        internal void Update()
		{
        	if (firstUpdate)
        	{
        		firstUpdate = false;
        		
				var i = vars.Count;
				while (i --> 0)
				{
					lerpers[i].Initialize(start[i], end[i], behavior);
				}
        	}
        	
			if (Paused)
				return;
			
			if (Delay > 0)
			{
				Delay -= elapsed;
				return;
			}
			
			if (time == 0)
			{
				if (begin != null)
					begin();
			}
			
			time += elapsed;
			float t = time / Duration;
			bool doComplete = false;
			
			if (time >= Duration)
			{
				if (repeatCount > 0)
				{
					--repeatCount;
					time = t = 0;
				}
				else if (repeatCount < 0)
				{
					doComplete = true;
					time = t = 0;
				}
				else
				{
					time = Duration;
					t = 1;
                    parent.Remove(this);
                    doComplete = true;
				}
				
				if (time == 0)
				{
					//	If the timer is zero here, we just restarted.
					//	If reflect mode is on, flip start to end
					if (behavior.HasFlag(Lerper.Behavior.Reflect))
						Reverse();
				}
			}
			
			if (ease != null)
				t = ease(t);
			
			Interpolate(t);
			
			if (update != null)
				update();
			
			if (doComplete && complete != null)
				complete();
		}
        
        protected void Interpolate(float t)
        {
			int i = vars.Count;			
			while (i --> 0)
			{
				vars[i].Value = lerpers[i].Interpolate(t, vars[i].Value, behavior);
			}
        }
		
#region Behavior
		
		/// <summary>
		/// Apply target values to a starting point before tweening.
		/// </summary>
		/// <param name="values">The values to apply, in an anonymous type ( new { prop1 = 100, prop2 = 0} ).</param>
		/// <returns>A reference to this.</returns>
		public Tween From(object values)
		{
			var fromProps = values.GetType().GetProperties();
			for (int i = 0; i < fromProps.Length; ++i)
			{
				var property = fromProps[i];
				var propValue = property.GetValue(values, null);
				
				int index = -1;
				if (varHash.TryGetValue(property.Name, out index))
				{
					//	if we're already tweening this value, adjust the range
					start[index] = propValue;
				}
				
				//	if we aren't tweening this value, just set it
				var info = new GlideInfo(Target, property.Name, true);
				info.Value = propValue;
			}
			
			return this;
		}
		
		/// <summary>
		/// Set the easing function.
		/// </summary>
		/// <param name="ease">The Easer to use.</param>
		/// <returns>A reference to this.</returns>
		public Tween Ease(Func<float, float> ease)
		{
			this.ease = ease;
			return this;
		}
		
		/// <summary>
		/// Set a function to call when the tween begins (useful when using delays).
		/// </summary>
		/// <param name="callback">The function that will be called when the tween starts, after the delay.</param>
		/// <returns>A reference to this.</returns>
		public Tween OnBegin(Action callback)
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
		public Tween OnComplete(Action callback)
		{
			complete = callback;
			return this;
		}
		
		/// <summary>
		/// Set a function to call as the tween updates.
		/// </summary>
		/// <param name="callback">The function to use.</param>
		/// <returns>A reference to this.</returns>
		public Tween OnUpdate(Action callback)
		{
			update = callback;
			return this;
		}
		
		/// <summary>
		/// Enable repeating.
		/// </summary>
		/// <param name="times">Number of times to repeat. Leave blank or pass a negative number to repeat infinitely.</param>
		/// <returns>A reference to this.</returns>
		public Tween Repeat(int times = -1)
		{
			repeatCount = times;
			return this;
		}
		
		/// <summary>
		/// Sets the tween to reverse every other time it repeats. Repeating must be enabled for this to have any effect.
		/// </summary>
		/// <returns>A reference to this.</returns>
		public Tween Reflect()
		{
			behavior |= Lerper.Behavior.Reflect;
			return this;
		}
		
		/// <summary>
		/// Swaps the start and end values of the tween.
		/// </summary>
		/// <returns>A reference to this.</returns>
		public Tween Reverse()
		{	
			int i = vars.Count;			
			while (i --> 0)
			{
				var s = start[i];
				var e = end[i];
				
				//	Set start to end and end to start
				start[i] = e;
				end[i] = s;
				
				lerpers[i].Initialize(e, s, behavior);
			}
			
			return this;
		}
		
		/// <summary>
		/// Whether this tween handles rotation.
		/// </summary>
		/// <returns>A reference to this.</returns>
		public Tween Rotation(RotationUnit unit = RotationUnit.Degrees)
		{
			behavior |= Lerper.Behavior.Rotation;
			behavior |= (unit == RotationUnit.Degrees) ? Lerper.Behavior.RotationDegrees : Lerper.Behavior.RotationRadians;

			return this;
		}
		
		/// <summary>
		/// Whether tweened values should be rounded to integer values.
		/// </summary>
		/// <returns>A reference to this.</returns>
		public Tween Round()
		{
			behavior |= Lerper.Behavior.Round;
			return this;
		}
#endregion
				
#region Control
		private void AddLerp(Lerper lerper, GlideInfo info, object from, object to)
		{
			varHash.Add(info.PropertyName, vars.Count);
			vars.Add(info);
			
			start.Add(from);
			end.Add(to);
			
			lerpers.Add(lerper);
		}
		
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
			time = Duration;
			update = null;
            parent.Remove(this);
		}
		
		/// <summary>
		/// Set tweens to pause. They won't update and their delays won't tick down.
		/// </summary>
		public void Pause()
		{
    		Paused = true;
		}
		
		/// <summary>
		/// Toggle tweens' paused value.
		/// </summary>
		public void PauseToggle()
		{
			Paused = !Paused;
		}
		
		/// <summary>
		/// Resumes tweens from a paused state.
		/// </summary>
		public void Resume()
		{
			Paused = false;
		}
#endregion
	}
}