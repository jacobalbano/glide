using System;
using System.Collections.Generic;
using System.Reflection;

namespace GlideTween
{
    public class GlideManager : Glide.GlideManagerImpl
    {}

    public partial class Glide
    {
        public class GlideManagerImpl
        {
            static GlideManagerImpl()
            {
                _dummy = new {};
                Tweener = new Glide();
            }

            public GlideManagerImpl()
            {
                tweens = new Dictionary<object, List<Glide>>();
                toRemove = new List<Glide>();
                toAdd = new List<Glide>();
            }

            public static readonly Glide Tweener;
            private static object _dummy;
            private Dictionary<object, List<Glide>> tweens;
            private List<Glide> toRemove, toAdd;

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
                var glide = new Glide();

                glide.target = target;
                glide.duration = duration;
                glide.delay = delay;

                glide.parent = this;

                toAdd.Add(glide);

                if (values == null) // in case of timer
                    return glide;

                foreach (PropertyInfo property in values.GetType().GetProperties())
                {
                    var info = new GlideInfo(target, property.Name);
                    var to = new GlideInfo(values, property.Name, false).Value;

                    float s = info.Value;
                    float r = to - s;

                    glide.vars.Add(info);
                    glide.start.Add(s);
                    glide.range.Add(r);
                    glide.end.Add(to);
                }

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
                return Tween(_dummy, null, duration, delay);
            }

            /// <summary>
            /// Remove tweens from the tweener without calling their complete functions.
            /// </summary>
            public void Cancel()
            {
                ApplyAll(glide => toRemove.Add(glide));
            }

            /// <summary>
            /// Assign tweens their final value and remove them from the tweener.
            /// </summary>
            public void CancelAndComplete()
            {
                ApplyAll(glide =>
                {
                    glide.time = glide.duration;
                    glide.update = null;
                    toRemove.Add(glide);
                });
            }

            /// <summary>
            /// Set tweens to pause. They won't update and their delays won't tick down.
            /// </summary>
            public void Pause()
            {
                ApplyAll(glide => glide.paused = true);
            }

            /// <summary>
            /// Toggle tweens' paused value.
            /// </summary>
            public void PauseToggle()
            {
                ApplyAll(glide => glide.paused = !glide.paused);
            }

            /// <summary>
            /// Resumes tweens from a paused state.
            /// </summary>
            public void Resume()
            {
                ApplyAll(glide => glide.paused = false);
            }

            /// <summary>
            /// Updates the tweener and all objects it contains.
            /// </summary>
            /// <param name="secondsElapsed">Seconds elapsed since last update.</param>
            public void Update(float secondsElapsed)
            {
                ApplyAll(glide =>
                {
                    glide.elapsed = secondsElapsed;
                    glide.Update();
                });

                AddAndRemove();
            }

            internal void Remove(Glide glide)
            {
                toRemove.Add(glide);
            }

            private void ApplyAll(Action<Glide> action)
            {
                foreach (var list in tweens.Values)
                {
                    foreach (var glide in list)
                    {
                        action(glide);
                    }
                }
            }

            private void AddAndRemove()
            {
                foreach (var add in toAdd)
                {
                    if (!tweens.ContainsKey(add.target))
                    {
                        tweens[add.target] = new List<Glide>();
                    }

                    tweens[add.target].Add(add);
                }

                foreach (var remove in toRemove)
                {
                    List<Glide> list;
                    if (tweens.TryGetValue(remove.target, out list))
                    {
                        list.Remove(remove);
                        if (list.Count == 0)
                        {
                            tweens.Remove(remove.target);
                        }
                    }
                }

                toAdd.Clear();
                toRemove.Clear();
            }

            #region Bulk control

            private void ApplyBulkControl(object[] targets, Action<Glide> action)
            {
                foreach (var target in targets)
                {
                    List<Glide> list;
                    if (tweens.TryGetValue(target, out list))
                    {
                        foreach (var glide in list)
                        {
                            action(glide);
                        }
                    }
                }
            }

            /// <summary>
            /// Look up tweens by the objects they target, and cancel them.
            /// </summary>
            /// <param name="targets">The objects being tweened that you want to cancel.</param>
            public void TargetCancel(params object[] targets)
            {
                ApplyBulkControl(targets, glide => glide.Cancel());
            }

            /// <summary>
            /// Look up tweens by the objects they target, cancel them, set them to their final values, and call the complete callback.
            /// </summary>
            /// <param name="targets">The objects being tweened that you want to cancel and complete.</param>
            public void TargetCancelAndComplete(params object[] targets)
            {
                ApplyBulkControl(targets, glide => glide.CancelAndComplete());
            }


            /// <summary>
            /// Look up tweens by the objects they target, and pause them.
            /// </summary>
            /// <param name="targets">The objects being tweened that you want to pause.</param>
            public void TargetPause(params object[] targets)
            {
                ApplyBulkControl(targets, glide => glide.Pause());
            }

            /// <summary>
            /// Look up tweens by the objects they target, and toggle their paused states.
            /// </summary>
            /// <param name="targets">The objects being tweened that you want to toggle pause.</param>
            public void TargetPauseToggle(params object[] targets)
            {
                ApplyBulkControl(targets, glide => glide.PauseToggle());
            }


            /// <summary>
            /// Look up tweens by the objects they target, and resume them from paused.
            /// </summary>
            /// <param name="targets">The objects being tweened that you want to resume.</param>
            public void TargetResume(params object[] targets)
            {
                ApplyBulkControl(targets, glide => glide.Resume());
            }

            #endregion
        }
    }
}
