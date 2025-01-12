/* 
 * OneEuroFilter.cs
 * Author: Dario Mazzanti (dario.mazzanti@iit.it), 2016
 * 
 * This Unity C# utility is based on the C++ implementation of the OneEuroFilter algorithm by Nicolas Roussel (http://www.lifl.fr/~casiez/1euro/OneEuroFilter.cc)
 * More info on the 1€ filter by Géry Casiez at http://www.lifl.fr/~casiez/1euro/
 *
 */

using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

class LowPassFilter 
{
	float y, a, s;
	bool initialized;

	public void setAlpha(float _alpha) 
	{
		if (_alpha<=0.0f || _alpha>1.0f)
		{
			Debug.LogError("alpha should be in (0.0., 1.0]");
			return;
		}
		a = _alpha;
	}

	public LowPassFilter(float _alpha, float _initval=0.0f) 
	{
		y = s = _initval;
		setAlpha(_alpha);
		initialized = false;
	}

	public float Filter(float _value) 
	{
		float result;
		if (initialized)
			result = a*_value + (1.0f-a)*s;
		else 
		{
			result = _value;
			initialized = true;
		}
		y = _value;
		s = result;
		return result;
	}

	public float filterWithAlpha(float _value, float _alpha) 
	{
		setAlpha(_alpha);
		return Filter(_value);
	}

	public bool hasLastRawValue() 
	{
		return initialized;
	}

	public float lastRawValue() 
	{
		return y;
	}

};

// -----------------------------------------------------------------

public class OneEuroFilter 
{
	float freq;
	float mincutoff;
	float beta;
	float dcutoff;
	LowPassFilter x;
	LowPassFilter dx;
	float lasttime;

	// currValue contains the latest value which have been succesfully filtered
	// prevValue contains the previous filtered value
	public float currValue {get; protected set;}
	public float prevValue {get; protected set;}

	float alpha(float _cutoff) 
	{
		float te = 1.0f/freq;
		float tau = 1.0f/(2.0f*Mathf.PI*_cutoff);
		return 1.0f/(1.0f + tau/te);
	}

	void setFrequency(float _f) 
	{
		if (_f<=0.0f)
		{
			Debug.LogError("freq should be > 0");
			return;
		}
		freq = _f;
	}

	void setMinCutoff(float _mc) 
	{
		if (_mc<=0.0f)
		{
			Debug.LogError("mincutoff should be > 0");
			return;
		}
		mincutoff = _mc;
	}

	void setBeta(float _b) 
	{
		beta = _b;
	}

	void setDerivateCutoff(float _dc) 
	{
		if (_dc<=0.0f)
		{
			Debug.LogError("dcutoff should be > 0");
			return;
		}
		dcutoff = _dc;
	}

	public OneEuroFilter(float _freq, float _mincutoff=1.0f, float _beta=0.0f, float _dcutoff=1.0f) 
	{
		setFrequency(_freq);
		setMinCutoff(_mincutoff);
		setBeta(_beta);
		setDerivateCutoff(_dcutoff);
		x = new LowPassFilter(alpha(mincutoff));
		dx = new LowPassFilter(alpha(dcutoff));
		lasttime = -1.0f;

		currValue = 0.0f;
		prevValue = currValue;
	}

	public void UpdateParams(float _freq, float _mincutoff = 1.0f, float _beta = 0.0f, float _dcutoff = 1.0f)
	{
		setFrequency(_freq);
		setMinCutoff(_mincutoff);
		setBeta(_beta);
		setDerivateCutoff(_dcutoff);
		x.setAlpha(alpha(mincutoff));
		dx.setAlpha(alpha(dcutoff));	
	}

	public float Filter(float value, float timestamp = -1.0f) 
	{
		prevValue = currValue;

		// update the sampling frequency based on timestamps
		if (lasttime != -1.0f && timestamp != -1.0f)
		{
			freq = 1.0f / (timestamp - lasttime);
		}
		lasttime = timestamp;
		// estimate the current variation per second 
		float dvalue = x.hasLastRawValue() ? (value - x.lastRawValue())*freq : 0.0f; // FIXME: 0.0 or value? 
		float edvalue = dx.filterWithAlpha(dvalue, alpha(dcutoff));
		// use it to update the cutoff frequency
		float cutoff = mincutoff + beta*Mathf.Abs(edvalue);
		// filter the given value
		currValue = x.filterWithAlpha(value, alpha(cutoff));

		return currValue;
	}
}
// A struct for the LowPassFilter
[BurstCompile]
public struct ThreadedLowPassFilter
{
    public float Alpha;
    public float LastValue;
    public bool Initialized;

    public void Initialize(float alpha, float initialValue = 0.0f)
    {
        Alpha = alpha;
        LastValue = initialValue;
        Initialized = false;
    }

    public float Filter(float value)
    {
        if (!Initialized)
        {
            Initialized = true;
            LastValue = value;
        }
        else
        {
            LastValue = Alpha * value + (1.0f - Alpha) * LastValue;
        }
        return LastValue;
    }

    public float FilterWithAlpha(float value, float alpha)
    {
        Alpha = alpha;
        return Filter(value);
    }
}

// A job struct for processing OneEuroFilter in parallel
[BurstCompile]
public struct OneEuroFilterParallelJob : IJobParallelFor
{
    [ReadOnly] public NativeArray<float> InputValues;
    [WriteOnly] public NativeArray<float> OutputValues;

    public float Frequency;
    public float MinCutoff;
    public float Beta;
    public float DerivativeCutoff;
    public NativeArray<ThreadedLowPassFilter> PositionFilters;
    public NativeArray<ThreadedLowPassFilter> DerivativeFilters;

    public void Execute(int index)
    {
        // Estimate variation
        float inputValue = InputValues[index];
        float dValue = PositionFilters[index].Initialized ? (inputValue - PositionFilters[index].LastValue) * Frequency: 0.0f;
        float edValue = DerivativeFilters[index].FilterWithAlpha(dValue, Alpha(DerivativeCutoff, Frequency));

        // Update cutoff frequency
        float cutoff = MinCutoff + Beta * Mathf.Abs(edValue);

        // Filter input value
        OutputValues[index] = PositionFilters[index].FilterWithAlpha(inputValue, Alpha(cutoff, Frequency));
    }

    private float Alpha(float cutoff, float freq)
    {
        float te = 1.0f / freq;
        float tau = 1.0f / (2.0f * Mathf.PI * cutoff);
        return 1.0f / (1.0f + tau / te);
    }
}
