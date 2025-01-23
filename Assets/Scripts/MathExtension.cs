using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;

public class MathExtension
{
    //bit-twiddling
    //----------
    public static readonly int[] MultiplyDeBruijnBitPosition =
    {
      0, 9, 1, 10, 13, 21, 2, 29, 11, 14, 16, 18, 22, 25, 3, 30,
      8, 12, 20, 28, 15, 17, 24, 7, 19, 27, 23, 6, 26, 5, 4, 31
    };

    public static int FastLog2Integer(int value)
    {
        //round down to a power of 2 minus 1
        value |= value >> 1;
        value |= value >> 2;
        value |= value >> 4;
        value |= value >> 8;
        value |= value >> 16;

        //wtf is 0x07C4ACDDU
        int r = MultiplyDeBruijnBitPosition[(int)(value * 0x07C4ACDDU) >> 27];
        return r;
    }

    public static int RequiredBits(int values)
    {
        return Mathf.CeilToInt(Mathf.Log(values) / Mathf.Log(2));
    }

    public static int RequiredBits(uint values)
    {
        return Mathf.CeilToInt(Mathf.Log(values) / Mathf.Log(2));
    }
    //----------

    //basic vector operations
    //----------
    //rotate a vector2 by a given angle
    public static Vector2 RotateVector2(Vector2 vector, float angle)
    {
        float xsn = Mathf.Sin(angle);
        float xcs = Mathf.Cos(angle);

        Vector2 v = vector;
        Vector2 r = Vector2.zero;

        r.x = -xcs * v.x + xsn * v.y;
        r.y = xsn * v.x + xcs * v.y;

        return r;
    }

    //calculates direction given 2 angles (no roll)
    public static Vector3 DirectionFromYawPitch(float yaw, float pitch)
    {
        float xsn = Mathf.Sin(yaw);
        float xcs = Mathf.Cos(yaw);

        float ysn = Mathf.Sin(pitch);
        float ycs = Mathf.Cos(pitch);

        Vector2 u = Vector2.up;

        u.x = -1.0f * ysn;
        u.y = 1.0f * ycs;

        Vector3 c = new Vector3(0.0f, u.x, -u.y);
        Vector3 f = c;

        f.x = -xcs * c.x + xsn * c.z;
        f.z = xsn * c.x + xcs * c.z;

        return f;
    }
    //----------

    //rotates an existing vector given 2 angles (no roll)
    public static Vector3 RotateWithYawPitch(Vector3 vector, float yaw, float pitch)
    {
        Vector3 right = Vector3.Cross(vector, Vector3.up);
        Vector3 up = Vector3.Cross(right, vector);

        Quaternion yawQuaternion = Quaternion.AngleAxis(yaw, up);
        Quaternion pitchQuaternion = Quaternion.AngleAxis(pitch, right);

        vector = yawQuaternion * vector;
        vector = pitchQuaternion * vector;

        return vector;
    }

    //character manipulation
    //----------
    public static string SanitiseSimplifiedAlphabetChar(char c)
    {
        switch (c)
        {
            case 'a': return "a";
            case 'b': return "b";
            case 'c': return "c";
            case 'd': return "d";
            case 'e': return "e";
            case 'f': return "f";
            case 'g': return "g";
            case 'h': return "h";
            case 'i': return "i";
            case 'j': return "j";
            case 'k': return "k";
            case 'l': return "l";
            case 'm': return "m";
            case 'n': return "n";
            case 'o': return "o";
            case 'p': return "p";
            case 'q': return "q";
            case 'r': return "r";
            case 's': return "s";
            case 't': return "t";
            case 'u': return "u";
            case 'v': return "v";
            case 'w': return "w";
            case 'x': return "x";
            case 'y': return "y";
            case 'z': return "z";
            case 'A': return "a";
            case 'B': return "b";
            case 'C': return "c";
            case 'D': return "d";
            case 'E': return "e";
            case 'F': return "f";
            case 'G': return "g";
            case 'H': return "h";
            case 'I': return "i";
            case 'J': return "j";
            case 'K': return "k";
            case 'L': return "l";
            case 'M': return "m";
            case 'N': return "n";
            case 'O': return "o";
            case 'P': return "p";
            case 'Q': return "q";
            case 'R': return "r";
            case 'S': return "s";
            case 'T': return "t";
            case 'U': return "u";
            case 'V': return "v";
            case 'W': return "w";
            case 'X': return "x";
            case 'Y': return "y";
            case 'Z': return "z";
        }

        return "";
    }

    public static byte ConvertSimplifiedAlphabetToByte(char c)
    {
        switch (c)
        {
            case 'a': return 0;
            case 'b': return 1;
            case 'c': return 2;
            case 'd': return 3;
            case 'e': return 4;
            case 'f': return 5;
            case 'g': return 6;
            case 'h': return 7;
            case 'i': return 8;
            case 'j': return 9;
            case 'k': return 10;
            case 'l': return 11;
            case 'm': return 12;
            case 'n': return 13;
            case 'o': return 14;
            case 'p': return 15;
            case 'q': return 16;
            case 'r': return 17;
            case 's': return 18;
            case 't': return 19;
            case 'u': return 20;
            case 'v': return 21;
            case 'w': return 22;
            case 'x': return 23;
            case 'y': return 24;
            case 'z': return 25;
        }

        return 0;
    }

    public static char ConvertByteToSimplifiedAlphabet(byte b)
    {
        switch (b)
        {
            case 0: return 'a';
            case 1: return 'b';
            case 2: return 'c';
            case 3: return 'd';
            case 4: return 'e';
            case 5: return 'f';
            case 6: return 'g';
            case 7: return 'h';
            case 8: return 'i';
            case 9: return 'j';
            case 10: return 'k';
            case 11: return 'l';
            case 12: return 'm';
            case 13: return 'n';
            case 14: return 'o';
            case 15: return 'p';
            case 16: return 'q';
            case 17: return 'r';
            case 18: return 's';
            case 19: return 't';
            case 20: return 'u';
            case 21: return 'v';
            case 22: return 'w';
            case 23: return 'x';
            case 24: return 'y';
            case 25: return 'z';
        }

        return 'a';
    }
    //----------

    //wrapping operations
    //----------
    public static int DiffWrapped(int a, int b, int limit)
    {
        int diff = b - a;
        int absDiff = System.Math.Abs(diff);

        if (absDiff < limit / 2)
        {
            //regular difference
            return diff;
        }
        else
        {
            //wrap around difference
            if (a > b)
            {
                a -= limit;
            }
            else
            {
                b -= limit;
            }

            return a - b;
        }
    }

    public static bool IsGreaterWrapped(int a, int b, int limit)
    {
        int diff = b - a;
        int absDiff = System.Math.Abs(diff);

        if (absDiff < limit / 2)
        {
            //regular difference
            return a > b;
        }
        else
        {
            //wrap around difference
            if (a > b)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
    //----------

    //collision detection (for frustum culling)
    //----------
    public static bool PointInsideBox(Vector3 point, Vector3 centre, Quaternion rotation, Vector3 size)
    {
        Quaternion inverse = Quaternion.Inverse(rotation);

        Vector3 inversePoint = inverse * (point - centre);

        Vector3 halfSize = size * 0.5f;

        if (inversePoint.x < -halfSize.x || inversePoint.x > halfSize.x)
        {
            return false;
        }

        if (inversePoint.y < -halfSize.y || inversePoint.y > halfSize.y)
        {
            return false;
        }

        if (inversePoint.z < -halfSize.z || inversePoint.z > halfSize.z)
        {
            return false;
        }

        return true;
    }

    public static bool PointInsidePlane(Plane plane, Vector3 point)
    {
        float dot = Vector3.Dot(plane.normal, point);
        return dot >= plane.distance;
    }

    public static bool SphereInsidePlane(Plane plane, Vector3 centre, float radius)
    {
        //point inside plane check, but with radius leeway
        float dot = Vector3.Dot(plane.normal, centre) - plane.distance;
        return dot >= -radius;
    }

    public static bool BoxInsidePlane(Plane plane, Vector3 centre, Quaternion rotation, Vector3 size)
    {
        Vector3[] points = new Vector3[8];

        Vector3 right = rotation * Vector3.right * size.x;
        Vector3 up = rotation * Vector3.up * size.y;
        Vector3 forward = rotation * Vector3.forward * size.z;

        //check every corner to see if it's on the other side of the plane
        for (int i = 0; i < 8; i++)
        {
            //bit-twiddling to get every permutation of -1 and 1 for the axes
            int xSign = (i & 0x1) * 2 - 1;
            int ySign = ((i & 0x2) >> 1) * 2 - 1;
            int zSign = ((i & 0x4) >> 2) * 2 - 1;

            Vector3 point = centre + right * xSign + up * ySign + forward * zSign;

            //early exit
            if (PointInsidePlane(plane, point))
            {
                return true;
            }
        }

        return false;
    }

    public static bool CapsuleInsidePlane(Plane plane, Vector3 centre, float radius, Vector3 direction, float height)
    {
        Vector3 diff = 0.5f * direction * height;

        Vector3 c1 = centre + diff;
        Vector3 c2 = centre - diff;

        //check the 2 spheres of the plane, the span in-between them is irrelevant
        return SphereInsidePlane(plane, c1, radius) || SphereInsidePlane(plane, c2, radius);
    }

    public static bool PointInsideCylinder(Vector3 point, Vector3 centre, float radius, float height)
    {
        Vector3 relative = point - centre;

        float halfHeight = height * 0.5f;

        if (relative.y > halfHeight)
        {
            return false;
        }

        if (relative.y < -halfHeight)
        {
            return false;
        }

        Vector2 flat = new Vector2(relative.x, relative.z);

        return flat.sqrMagnitude < radius * radius;
    }

    public static Vector3 ClosestPointOnCylinder(Vector3 point, Vector3 centre, float radius, float height)
    {
        Vector3 relative = point - centre;

        float halfHeight = height * 0.5f;

        if (relative.y > halfHeight)
        {
            relative.y = halfHeight;
        }

        if (relative.y < -halfHeight)
        {
            relative.y = -halfHeight;
        }

        Vector2 flat = new Vector2(relative.x, relative.z);

        if (flat.sqrMagnitude > radius * radius)
        {
            flat = flat.normalized * radius;
        }

        relative.x = flat.x;
        relative.z = flat.y;

        return centre + relative;
    }
    //----------

    //advanced quaternion operations
    //----------
    public static void RollTowardsDirection(ref Quaternion quaternion, Vector3 centre = default)
    {
        
    }
    //----------
}

public class MovingAverage
{
    public List<float> values;

    public int maxValues = 1;

    //efficiency and accuracy check
    float sum = 0.0f;
    int lossy = 0;

    public MovingAverage(int _maxValues)
    {
        values = new List<float>();
        maxValues = _maxValues;
    }

    public void Update(float newValue)
    {
        values.Add(newValue);
        sum += newValue;

        if (values.Count > maxValues)
        {
            sum -= values[0];
            values.RemoveAt(0);
        }
    }

    public float GetAverage()
    {
        lossy++;

        if (lossy < 100)
        {
            int count = values.Count;

            if (count == 0)
            {
                return 0.0f;
            }

            return sum / (float)count;
        }
        else
        {
            lossy = 0;

            int count = values.Count;
            float tempSum = 0.0f;

            if (count == 0)
            {
                return 0.0f;
            }

            for (int i = 0; i < count; i++)
            {
                tempSum += values[i];
            }

            sum = tempSum;

            return tempSum / (float)count;
        }
    }
}

public class Plane
{
    public Vector3 normal;
    public float distance;

    public Plane(Vector3 point, Vector3 normal)
    {
        this.normal = normal.normalized;
        distance = Vector3.Dot(normal, point);
    }
}