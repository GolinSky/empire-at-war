using System;
using UnityEngine;

namespace ScriptUtils.Math
{
    public abstract class Range<TValue>  
    {
    	[field:SerializeField] public TValue Min { get; private set;}  
    	[field:SerializeField] public TValue Max { get; private set;}


	    public abstract bool IsInRange(TValue value);
    }

    [Serializable]
    public class IntRange : Range<int>
    {
	    public override bool IsInRange(int value)
	    {
		    if (value > Max || value < Min )
		    {
			    return false;
		    }

		    return true;
	    }
    }
    
    [Serializable]
    public class FloatRange : Range<float>
    {
	    public override bool IsInRange(float value)
	    {
		    if (value > Max || value < Min )
		    {
			    return false;
		    }

		    return true;
	    }
    }
}