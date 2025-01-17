using Basis.Network.Core;
using BasisNetworkCore;
using LiteNetLib.Utils;
using System.Collections.Generic;
using static SerializableBasis;

public static class BasisNetworkResourceManagement
{
    public static ConcurrentList<string> UshortNetworkDatabase = new ConcurrentList<string>();
    public static void LoadResource(LocalLoadResource LocalLoadResource)
    {
        if (UshortNetworkDatabase.Contains(LocalLoadResource.LoadedNetID) == false)
        {
            NetDataWriter Writer = new NetDataWriter(true);
            LocalLoadResource.Serialize(Writer);
            UshortNetworkDatabase.Add(LocalLoadResource.LoadedNetID);
            BNL.Log("Adding Object " + LocalLoadResource.LoadedNetID);
           
            BasisNetworkServer.BroadcastMessageToClients(Writer,BasisNetworkCommons.LoadResourceMessage, BasisPlayerArray.GetSnapshot(), LiteNetLib.DeliveryMethod.ReliableSequenced);
        }
        else
        {
            BNL.LogError("Already have Object Loaded With " + LocalLoadResource.LoadedNetID);
        }
    }
    public static void UnloadResource(UnLoadResource UnLoadResource)
    {
        if (UshortNetworkDatabase.Remove(UnLoadResource.LoadedNetID))
        {
            NetDataWriter Writer = new NetDataWriter(true);
            UnLoadResource.Serialize(Writer);
            BNL.Log("Removing Object " + UnLoadResource.LoadedNetID);
            BasisNetworkServer.BroadcastMessageToClients(Writer, BasisNetworkCommons.LoadResourceMessage, BasisPlayerArray.GetSnapshot(), LiteNetLib.DeliveryMethod.ReliableSequenced);
        }
        else
        {
            BNL.LogError("Trying to unload a object that does not exist! " + UnLoadResource.LoadedNetID);
        }
    }
    public class ConcurrentList<T>
    {
        private readonly List<T> _list = new List<T>();
        private readonly object _lock = new object();

        public void Add(T item)
        {
            lock (_lock)
            {
                _list.Add(item);
            }
        }

        public bool Remove(T item)
        {
            lock (_lock)
            {
                return _list.Remove(item);
            }
        }

        public T Get(int index)
        {
            lock (_lock)
            {
                return _list[index];
            }
        }

        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _list.Count;
                }
            }
        }

        public List<T> ToList()
        {
            lock (_lock)
            {
                return new List<T>(_list);
            }
        }

        public bool Contains(T item)
        {
            lock (_lock)
            {
                return _list.Contains(item);
            }
        }
    }
}
