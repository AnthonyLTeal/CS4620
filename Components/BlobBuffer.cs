using System;

namespace CS4620IS.Components;

public class BlobBuffer<T> where T : struct
{
    public T[] Data;
    public int CurrentIndex = 0;
    
    public BlobBuffer(int size = 50000)
    {
        Data = new T[size];
    }
    
    public void AddDestination(T item)
    {
        Data[CurrentIndex] = item;
        CurrentIndex += 1;
    }
    
    public (int start, int count) AddDestinations(T[] destinations)
    {
        int start = CurrentIndex;
        int count = destinations.Length;
        
        if (start + count > Data.Length) throw new Exception("Blob Full!");
        
        Array.Copy(destinations, 0, Data, start, count);
        CurrentIndex += count;

        return (start, count);
    }
}

public class DestinationBlob : BlobBuffer<Destination>
{
    public DestinationBlob(int size) : base(size) { }

    public Destination GetDestination(DestinationPointer pointer)
    {
        return Data[pointer.Current + pointer.Start];
    }
}