using System.Collections.Generic;
using UnityEngine;

namespace ECC
{
    [CreateAssetMenu(fileName = "Network", menuName = "ECC/Network", order = 5)]
    public class Network : BaseModule
    {
    }

    [System.Serializable]
    public class NetworkObject
    {
        public bool SwitchedOn = true;
        public Network Network;
        public List<PowerSourceObject> PowerSources;


        public bool Operating
        {
            get { return SwitchedOn; }
        }
    }
}
