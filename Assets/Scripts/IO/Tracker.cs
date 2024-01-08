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
    public RoomDimension roomDimension;
    public List<Tracker> collections;
}

