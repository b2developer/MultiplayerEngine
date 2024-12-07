using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//wrapped for 3 fixed points in a vector
public class FixedPoint3
{
    public Vector3 vector;

    public FixedPoint x;
    public FixedPoint y;
    public FixedPoint z;

    //increment of possible values
    public float precision = 0.0f;

    public bool calculatedBitLength = false;
    public int bitLength = 0;

    public FixedPoint3(float minX, float minY, float minZ, float maxX, float maxY, float maxZ, float _precision)
    {
        x = new FixedPoint(minX, maxX, _precision);
        y = new FixedPoint(minY, maxY, _precision);
        z = new FixedPoint(minZ, maxZ, _precision);

        precision = _precision;

        GetBitLength();
    }

    //gets the bits required to serialise in fixed point form
    public int GetBitLength()
    {
        //avoid using expensive logarithms more than once
        if (calculatedBitLength)
        {
            return bitLength;
        }

        bitLength += x.GetBitLength();
        bitLength += y.GetBitLength();
        bitLength += z.GetBitLength();
        calculatedBitLength = true;

        return bitLength;
    }

    //converts to fixed point and writes to stream
    public void WriteFixedPoint(ref BitStream stream)
    {
        x.value = vector.x;
        y.value = vector.y;
        z.value = vector.z;

        x.WriteFixedPoint(ref stream);
        y.WriteFixedPoint(ref stream);
        z.WriteFixedPoint(ref stream);
    }

    //reads from stream and converts back to float
    public void ReadFixedPoint(ref BitStream stream)
    {
        x.ReadFixedPoint(ref stream);
        y.ReadFixedPoint(ref stream);
        z.ReadFixedPoint(ref stream);

        vector = new Vector3(x.value, y.value, z.value);
    }

    //rounds the fixed point value according to it's limitations
    public void Quantize()
    {
        x.Quantize();
        y.Quantize();
        z.Quantize();
    }
}
