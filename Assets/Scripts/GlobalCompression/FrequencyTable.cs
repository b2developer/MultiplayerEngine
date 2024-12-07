using System.Collections;
using System.Collections.Generic;
using System.IO;

//tracks how frequently all byte values 0-255 occur
public class FrequencyTable
{
    public byte[] values;
    public uint[] occurences;
    public float[] probabilities;

    public FrequencyTable()
    {
        values = new byte[256];
        occurences = new uint[256];
        probabilities = new float[256];

        for (int i = 0; i < 256; i++)
        {
            values[i] = (byte)i;
            occurences[i] = 0;
            probabilities[i] = 1.0f;
        }
    }

    public void AddValue(byte value)
    {
        occurences[value]++;
    }

    public void GenerateProbabilities()
    {
        float sum = 0.0f;

        for (int i = 0; i < 256; i++)
        {
            sum += occurences[i];
        }

        for (int i = 0; i < 256; i++)
        {
            float probability = occurences[i] / sum;
            probabilities[i] = probability;
        }
    }

    public void WriteToFile(string path)
    {
        StreamWriter sw = new StreamWriter(path);

        for (int i = 0; i < 256; i++)
        {
            sw.Write(values[i].ToString());
            sw.Write(',');
            sw.Write(occurences[i].ToString());
            sw.Write(',');
            sw.Write(probabilities[i].ToString());
            sw.Write('\n');
        }

        sw.Close();
    }

    public void ReadFromFile(string path)
    {
        StreamReader sr = new StreamReader(path);

        for (int i = 0; i < 256; i++)
        {
            string line = sr.ReadLine();
            string[] parts = line.Split(',');

            values[i] = byte.Parse(parts[0]);
            occurences[i] = uint.Parse(parts[1]);

            //cut off '\n'
            probabilities[i] = float.Parse(parts[2]);
        }

        sr.Close();
    }
}
