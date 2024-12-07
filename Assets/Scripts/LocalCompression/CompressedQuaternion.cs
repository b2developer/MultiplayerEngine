using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//applys fixed point to a quaternion and replaces the 'w' component with a single bit
public class CompressedQuaternion
{
    public Quaternion quaternion;

    public FixedPoint x;
    public FixedPoint y;
    public FixedPoint z;

    //increment of possible values
    public float precision = 0.0f;

    public bool calculatedBitLength = false;
    public int bitLength = 0;

    public CompressedQuaternion()
    {
        //this is enough to represent a quaternion effectively
        precision = 2.0f / 65535.0f;

        x = new FixedPoint(-1.0f, 1.0f, precision);
        y = new FixedPoint(-1.0f, 1.0f, precision);
        z = new FixedPoint(-1.0f, 1.0f, precision);

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
        bitLength++; //w > 0 bit

        calculatedBitLength = true;

        return bitLength;
    }

    //converts to fixed point and writes to stream
    public void WriteToStream(ref BitStream stream)
    {
        x.value = quaternion.x;
        y.value = quaternion.y;
        z.value = quaternion.z;

        x.WriteFixedPoint(ref stream);
        y.WriteFixedPoint(ref stream);
        z.WriteFixedPoint(ref stream);

        //write the sign of the quaternion
        stream.WriteBool(quaternion.w > 0.0f);
    }

    //reads from stream and converts back to float
    public void ReadFromStream(ref BitStream stream)
    {
        x.ReadFixedPoint(ref stream);
        y.ReadFixedPoint(ref stream);
        z.ReadFixedPoint(ref stream);

        float wSign = stream.ReadBool() ? 1.0f : -1.0f;

        //quaternion property of being a unit vector can be exploited here
        //w^2 = sqrt(1 - x^2 - y^2 - z^2)
        float w2 = 1.0f - x.value * x.value - y.value * y.value - z.value * z.value;
        float w = 0.0f;

        if (w2 > 0.0f)
        {
            w = Mathf.Sqrt(w2) * wSign;
        }

        quaternion = new Quaternion(x.value, y.value, z.value, w);
    }

    //rounds the compression quaternion value according to it's limitations
    public void Quantize()
    {
        float wSign = Mathf.Sign(quaternion.w);

        x.Quantize();
        y.Quantize();
        z.Quantize();

        float w2 = 1.0f - x.value * x.value - y.value * y.value - z.value * z.value;
        float w = 0.0f;

        if (w2 > 0.0f)
        {
            w = Mathf.Sqrt(w2) * wSign;
        }

        quaternion = new Quaternion(x.value, y.value, z.value, w);
    }
}
