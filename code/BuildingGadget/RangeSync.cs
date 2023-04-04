using System;
using Vintagestory.API.Common;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using ProtoBuf;

namespace BuildingGadgets
{

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class RangeSync
    {
        public bool increase; // true if range + 1, false is range - 1
    }

    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class RangeSyncResponse
    {
        public bool response;
    }
}
