using System.Collections.Generic;
using System;

public class Tracker
{
    public List<TrackedObject.Data> objects;
}

public class RoomTracker
{
    public RoomConfiguration room;
}

[Serializable]
public struct RoomConfiguration
{
    public int width;
    public int height;
    public int depth; 
    public List<Tracker> collections;
}

