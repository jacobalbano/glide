Glide is a super-simple tweening library for C#.

# Installation
 1. Copy Glide.cs, GlideInfo.cs, and Ease.cs into your project folder.
 2. There is no step two.

# Use
Create a Glide instance and use it to manage tweens:
    
	:::csharp
	var Tweener = new Glide();
	Tweener.Tween(...);
	
Every frame, update the tweener.

    :::csharp
    Tweener.Update(ElapsedSeconds);

### Tweening
Tweening properties is done with a call to Tween. Pass the object to tween, an [anonymous type][1] instance containing value names and target values, and a duration, with an optional delay.

    :::csharp
    Twener.Tween(target, new { X = toX, Y = toY }, duration, delay);

You can also use Glide to set up timed callbacks.

    :::csharp
    Tweener.Timer(duration, delay).OnComplete(CompleteCallback);

### Control
If you need to control tweens after they are launched (for example, pausing or cancelling), you can hold a reference to the object returned By Tween():

    :::csharp
	var myTween = Tweener.Tween(object, new {X = toX, Y = toY}, duration);
	
You can later use this object to control the tween:
    
	:::csharp
    myTween.Cancel();
    myTween.CancelAndComplete();
    
    myTween.Pause();
    myTween.PauseToggle();
	
    myTween.Resume();
	
Calling a control method on a tweener will affect all tweens it manages.

### Behavior
You can specify a number of special behaviors for a tween to use. Calls can be chained for setting more than one at a time.

    :::csharp
	//  Glide comes with a full complement of easing functions
    Tweener.Tween(...).Ease(Ease.ElasticOut);
    
    Tweener.Tween(...).OnComplete(() => Console.WriteLine("done"));
    Tweener.Tween(...).OnUpdate(() => Console.WriteLine("updating"));
    
    //  Repeat twice
    Tweener.Tween(...).Repeat(2);
    
    //  Repeat forever
    Tweener.Tween(...).Repeat();
    
    //  Reverse the tween every other time it repeats
    Tweener.Tween(...).Repeat().Reflect();
    
    //  Swaps the end and start values of a tween.
    //  This is helpful if you want to set an object's properties to one set of values, and then tween back to the previous values.
    Tweener.Tween(...).Reverse();
    
    //  Smoothly interpolate a rotation value past the end of an axis.
    Tweener.Tween(...).Rotation();
    
    //  Round tweened properties to integer values
    Tweener.Tween(...).Round();
    
If you have any questions, find a bug, or want to request a feature, leave a message here or hit me up on Twitter [@jacobalbano][2]!
[1]: http://msdn.microsoft.com/en-us/library/vstudio/bb397696.aspx
[2]: http://www.twitter.com/jacobalbano