using System;
using System.Collections;
using System.Collections.Generic;

//trades total bit length of a float for precision
//its nearly impossible to see the difference between 10 and 10.01
public class FixedPoint
{
    public float value = 0.0f;

    //range of possible values
    public float minValue = 0.0f;
    public float maxValue = 0.0f;

    //increment of the possible values
    public float precision = 0.0f;

    public bool calculatedBitLength = false;
    public int bitLength = 0;

    public FixedPoint(float _minValue, float _maxValue, float _precision)
    {
        minValue = _minValue;
        maxValue = _maxValue;
        precision = _precision;

        GetBitLength();
    }

    //gets total amount of possible values the compressed string could have
    public int TotalValues()
    {
        return (int)MathF.Floor((maxValue - minValue) / precision) + 1;
    }

    //gets the bits required to serialise in fixed point form
    public int GetBitLength()
    {
        //avoid using expensive logarithms more than once
        if (calculatedBitLength)
        {
            return bitLength;
        }

        int totalValues = TotalValues();

        bitLength = MathExtension.RequiredBits(totalValues);
        calculatedBitLength = true;

        return bitLength;
    }

    //converts to fixed point and writes to stream
    public void WriteFixedPoint(ref BitStream stream)
    {
        float clampedValue = MathF.Min(MathF.Max(value, minValue), maxValue);
        uint compressed = (uint)((int)MathF.Floor((value - minValue) / precision));

        stream.WriteUint(compressed, bitLength);
    }

    //reads from stream and converts back to float
    public void ReadFixedPoint(ref BitStream stream)
    {
        uint compressed = stream.ReadUint(bitLength);
        value = minValue + compressed * precision;
    }

    public FixedPoint Clone()
    {
        FixedPoint clone = new FixedPoint(minValue, maxValue, precision);
        clone.value = value;

        clone.calculatedBitLength = calculatedBitLength;
        clone.bitLength = bitLength;

        return clone;
    }

    //rounds the fixed point value according to it's limitations
    public void Quantize()
    {
        float clampedValue = MathF.Min(MathF.Max(value, minValue), maxValue);
        uint compressed = (uint)((int)MathF.Floor((value - minValue) / precision));
        value = minValue + compressed * precision;
    }
}
