//templated class to allows indexes to access elements in O(1) time
public class LookupTable<T>
{
    public T[] data;

    public LookupTable(int max)
    {
        data = new T[max];
    }

    public void Place(T obj, int index)
    {
        data[index] = obj;
    }

    public T Grab(int index)
    {
        return data[index];
    }

    public void Remove(int index)
    {
        data[index] = default(T);
    }
}
