Glide is a super-simple tweening library for C#.

# Installation
 1. Copy Glide.cs, GlideInfo.cs, and Ease.cs into your project folder.
 2. There is no step two.

# Use
Every frame, update the tweener.

    Glide.Update(ElapsedSeconds);

### Tweening
Tweening properties is done with a call to Tween. Pass the object to tween, an [anonymous type][1] instance containing value names and target values, and a duration, with an optional delay.

    :::csharp
    Glide.Tween(target, new { X = destination.X, Y = destination.Y }, duration, delay);

You can also use Glide to set up timed callbacks.

    :::csharp
    Glide.Timer(duration, delay).OnComplete(CompleteCallback);

### Control
Control functions accept any number of objects. If no parameters are passed, the call will affect every target in the tweener.

    :::csharp
    Glide.Cancel();
    Glide.Cancel(MyObject);
    Glide.CancelAndComplete(ObjectA, ObjectB);
    
    Glide.Pause();
    Glide.Pause(MyObject);
    Glide.PauseToggle();
    
    Glide.Resume(MyObject);
    Glide.Resume();

### Behavior
You can specify a number of special behaviors for a tween to use. Calls can be chained for setting more than one at a time.

    :::csharp
	//  Glide comes with a full complement of easing functions
    Glide.Tween(...).Ease(Ease.ElasticOut);
    
    Glide.Tween(...).OnComplete(() => Console.WriteLine("done"));
    Glide.Tween(...).OnUpdate(() => Console.WriteLine("updating"));
    
    //  Repeat twice
    Glide.Tween(...).Repeat(2);
    
    //  Repeat forever
    Glide.Tween(...).Repeat();
    
    //  Reverse the tween every other time it repeats
    Glide.Tween(...).Repeat().Reflect();
    
    //  Swaps the end and start values of a tween.
    //  This is helpful if you want to tween back to the current position.
    Glide.Tween(...).Reverse();
    
    //  Smoothly interpolate a rotation value past the end of an axis.
    Glide.Tween(...).Rotation();
    
    //  Round tweened properties to integer values
    Glide.Tween(...).Round();
    
If you have any questions, find a bug, or want to request a feature, leave a message here or hit me up on Twitter [@jacobalbano][2]!
[1]: http://msdn.microsoft.com/en-us/library/vstudio/bb397696.aspx
[2]: http://www.twitter.com/jacobalbano