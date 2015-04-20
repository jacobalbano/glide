using System;
using System.Collections.Generic;
using System.Reflection;
using Glide;
using Indigo;
using Indigo.Core;

namespace Glide
{
    public class Tweener : Tween.TweenerImpl {}
    public partial class Tween
    {
        public class TweenerImpl
        {
            static TweenerImpl()
            {
                _dummy = new {};
                registeredLerpers = new Dictionary<Type, ConstructorInfo>();
				var numericTypes = new Type[] {
					typeof(Int16),
					typeof(Int32),
					typeof(Int64),
					typeof(UInt16),
					typeof(UInt32),
					typeof(UInt64),
					typeof(Single),
					typeof(Double)
				};
                
                for (int i = 0; i < numericTypes.Length; i++)
                	SetLerper<NumericLerper>(numericTypes[i]);
            }
            
            public static void SetLerper<T>(Type type) where T : Lerper, new()
			{
            	registeredLerpers[type] = typeof(T).GetConstructor(Type.EmptyTypes);
			}

            public TweenerImpl()
            {
                tweens = new Dictionary<object, List<Tween>>();
                toRemove = new List<Tween>();
                toAdd = new List<Tween>();
                allTweens = new List<Tween>();
            }
            
            private static object _dummy;
            private static Dictionary<Type, ConstructorInfo> registeredLerpers;
            private Dictionary<object, List<Tween>> tweens;
            private List<Tween> toRemove, toAdd, allTweens;

            /// <summary>
            /// <para>Tweens a set of properties on an object.</para>
            /// <para>To tween instance properties/fields, pass the object.</para>
            /// <para>To tween static properties/fields, pass the type of the object, using typeof(ObjectType) or object.GetType().</para>
            /// </summary>
            /// <param name="target">The object or type to tween.</param>
            /// <param name="values">The values to tween to, in an anonymous type ( new { prop1 = 100, prop2 = 0} ).</param>
            /// <param name="duration">Duration of the tween in seconds.</param>
            /// <param name="delay">Delay before the tween starts, in seconds.</param>
            /// <returns>The tween created, for setting properties on.</returns>
            public Tween Tween<T>(T target, object values, float duration, float delay = 0) where T : class
            {
            	if (target == null)
            		throw new ArgumentNullException("target");
            	
            	var targetType = target.GetType();
            	if (targetType.IsValueType)
            		throw new Exception("Target of tween cannot be a struct!");
            	
                var tween = new Tween();

                tween.Target = target;
                tween.Duration = duration;
                tween.Delay = delay;
                tween.parent = this;
            	toAdd.Add(tween);

                if (values == null) // in case of timer
                    return tween;

                var props = values.GetType().GetProperties();
                for (int i = 0; i < props.Length; ++i)
                {
                	var property = props[i];
                    var info = new GlideInfo(target, property.Name);
                    var to = new GlideInfo(values, property.Name, false);
                    var lerper = CreateLerper(info.PropertyType);
                    
                    tween.AddLerp(lerper, info, info.Value, to.Value);
                }

                return tween;
            }
            
			private Lerper CreateLerper(Type propertyType)
			{
				ConstructorInfo lerper = null;
				if (!registeredLerpers.TryGetValue(propertyType, out lerper))
					throw new Exception(string.Format("No Lerper found for type {0}.", propertyType.FullName));
				
				return (Lerper) lerper.Invoke(null);
			}

            /// <summary>
            /// Starts a simple timer for setting up callback scheduling.
            /// </summary>
            /// <param name="duration">How long the timer will run for, in seconds.</param>
            /// <param name="delay">How long to wait before starting the timer, in seconds.</param>
            /// <returns>The tween created, for setting properties.</returns>
            public Tween Timer(float duration, float delay = 0)
            {
                return Tween(_dummy, null, duration, delay);
            }

            /// <summary>
            /// Remove tweens from the tweener without calling their complete functions.
            /// </summary>
            public void Cancel()
            {
            	toRemove.AddRange(allTweens);
            }

            /// <summary>
            /// Assign tweens their final value and remove them from the tweener.
            /// </summary>
            public void CancelAndComplete()
            {
            	for (int i = 0; i < allTweens.Count; ++i)
            	{
            		var tween = allTweens[i];
                    tween.time = tween.Duration;
                    tween.update = null;
                    toRemove.Add(tween);
            	}
            }

            /// <summary>
            /// Set tweens to pause. They won't update and their delays won't tick down.
            /// </summary>
            public void Pause()
            {
            	for (int i = 0; i < allTweens.Count; ++i)
            	{
            		var tween = allTweens[i];
            		tween.Pause();
            	}
            }

            /// <summary>
            /// Toggle tweens' paused value.
            /// </summary>
            public void PauseToggle()
            {
            	for (int i = 0; i < allTweens.Count; ++i)
            	{
            		var tween = allTweens[i];
            		tween.PauseToggle();
            	}
            }

            /// <summary>
            /// Resumes tweens from a paused state.
            /// </summary>
            public void Resume()
            {
            	for (int i = 0; i < allTweens.Count; ++i)
            	{
            		var tween = allTweens[i];
            		tween.Resume();
            	}
            }

            /// <summary>
            /// Updates the tweener and all objects it contains.
            /// </summary>
            /// <param name="secondsElapsed">Seconds elapsed since last update.</param>
            public void Update(float secondsElapsed)
            {
            	for (int i = 0; i < allTweens.Count; ++i)
            	{
            		var tween = allTweens[i];
                    tween.elapsed = secondsElapsed;
                    tween.Update();
            	}

                AddAndRemove();
            }

            internal void Remove(Tween tween)
            {
                toRemove.Add(tween);
            }

            private void AddAndRemove()
            {
                for (int i = 0; i < toAdd.Count; ++i)
                {
                	var add = toAdd[i];
                	List<Tween> list = null;
                	if (!tweens.TryGetValue(add.Target, out list))
                		tweens[add.Target] = list = new List<Tween>();

                    list.Add(add);
                    allTweens.Add(add);
                }

                for (int i = 0; i < toRemove.Count; ++i)
                {
                	var remove = toRemove[i];
                    List<Tween> list;
                    if (tweens.TryGetValue(remove.Target, out list))
                    {
                        list.Remove(remove);
                        if (list.Count == 0)
                        {
                            tweens.Remove(remove.Target);
                        }
                    }
                    
                    allTweens.Remove(remove);
                }

                toAdd.Clear();
                toRemove.Clear();
            }

            #region Bulk control
            /// <summary>
            /// Look up tweens by the objects they target, and cancel them.
            /// </summary>
            /// <param name="targets">The objects being tweened that you want to cancel.</param>
            public void TargetCancel(params object[] targets)
            {
            	for (int i = 0; i < targets.Length; ++i)
                {
            		var target = targets[i];
                    List<Tween> list;
                    if (tweens.TryGetValue(target, out list))
                    {
                    	for (int j = 0; j < list.Count; ++j)
                    		list[j].Cancel();
                    }
                }
            }

            /// <summary>
            /// Look up tweens by the objects they target, cancel them, set them to their final values, and call the complete callback.
            /// </summary>
            /// <param name="targets">The objects being tweened that you want to cancel and complete.</param>
            public void TargetCancelAndComplete(params object[] targets)
            {
            	for (int i = 0; i < targets.Length; ++i)
                {
            		var target = targets[i];
                    List<Tween> list;
                    if (tweens.TryGetValue(target, out list))
                    {
                        for (int j = 0; j < list.Count; ++j)
                        	list[j].CancelAndComplete();
                    }
                }
            }


            /// <summary>
            /// Look up tweens by the objects they target, and pause them.
            /// </summary>
            /// <param name="targets">The objects being tweened that you want to pause.</param>
            public void TargetPause(params object[] targets)
            {
            	for (int i = 0; i < targets.Length; ++i)
                {
            		var target = targets[i];
                    List<Tween> list;
                    if (tweens.TryGetValue(target, out list))
                    {
                        for (int j = 0; j < list.Count; ++j)
                        	list[j].Pause();
                    }
                }
            }

            /// <summary>
            /// Look up tweens by the objects they target, and toggle their paused states.
            /// </summary>
            /// <param name="targets">The objects being tweened that you want to toggle pause.</param>
            public void TargetPauseToggle(params object[] targets)
            {
            	for (int i = 0; i < targets.Length; ++i)
                {
            		var target = targets[i];
                    List<Tween> list;
                    if (tweens.TryGetValue(target, out list))
                    {
                        for (int j = 0; j < list.Count; ++j)
                        	list[j].PauseToggle();
                    }
                }
            }

            /// <summary>
            /// Look up tweens by the objects they target, and resume them from paused.
            /// </summary>
            /// <param name="targets">The objects being tweened that you want to resume.</param>
            public void TargetResume(params object[] targets)
            {
            	for (int i = 0; i < targets.Length; ++i)
                {
            		var target = targets[i];
                    List<Tween> list;
                    if (tweens.TryGetValue(target, out list))
                    {
                        for (int j = 0; j < list.Count; ++j)
                        	list[j].Resume();
                    }
                }
            }

            #endregion
            
			private class NumericLerper : Lerper
			{
				float from, to, range;
				
				public override void Initialize(object fromValue, object toValue, Behavior behavior)
				{
					from = Convert.ToSingle(fromValue);
					to = Convert.ToSingle(toValue);
					range = to - from;
					
					if (behavior.HasFlag(Behavior.Rotation))
					{
						float angle = from;
						if (behavior.HasFlag(Behavior.RotationRadians))
							angle *= DEG;
						
						if (angle < 0)
							angle = 360 + angle;
						
						float r = angle + range;
						float d = r - angle;
						float a = (float) Math.Abs(d);
						
						if (a >= 180) range = (360 - a) * (d > 0 ? -1 : 1);
						else range = d;
					}
				}
				
				public override object Interpolate(float t, object current, Behavior behavior)
				{
					var value = from + range * t;
					if (behavior.HasFlag(Behavior.Rotation))
					{
						if (behavior.HasFlag(Behavior.RotationRadians))
							value *= DEG;
						
						value %= 360.0f;
							
						if (value < 0)
							value += 360.0f;
						
						if (behavior.HasFlag(Behavior.RotationRadians))
							value *= RAD;
					}
					
					if (behavior.HasFlag(Behavior.Round)) value = (float) Math.Round(value);
					
					var type = current.GetType();
					return Convert.ChangeType(value, type);
				}
			}
        }
    }
}