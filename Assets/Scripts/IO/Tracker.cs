using System.Collections.Generic;
using System;

public class Tracker
{
    public List<TrackedObject.Data> objects;
}

[Serializable]
public struct RoomConfiguration
{
    public RoomDimension roomDimension;
    public List<Tracker> collections;
}

