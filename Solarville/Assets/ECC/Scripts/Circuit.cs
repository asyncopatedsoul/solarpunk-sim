using System.Collections.Generic;
using UnityEngine;

namespace ECC
{
    [CreateAssetMenu(fileName = "Circuit", menuName = "ECC/Circuit", order = 15)]
    public class Circuit : BaseModule
    {
        [Header("Electrical")]
        [Tooltip("Conceptually this is anything that causes loss in a circuit such as resistance, conductivity, length of circuit and so on")]
        [Range(0,1)]
        public float Loss;
        [Tooltip("Maximum load capacity this circuit can accomodate")]
        public float AmperageRating;
    }

    [System.Serializable]
    public class CircuitObject
    {
        public bool SwitchedOn = true;
        public Circuit Circuit;
        public List<PowerConsumerObject> PowerConsumers;


        bool hasPower;
        public bool HasPower
        {
            get { return hasPower; }
            set { hasPower = value; }
        }


        public bool Operating
        {
            get { return SwitchedOn && HasPower; }
        }


        float capacityWatts;
        public float CapacityWatts
        {
            get { return capacityWatts; }
            set { capacityWatts = value; }
        }


        float loadWatts;
        public float LoadWatts
        {
            get { return loadWatts; }
            set { loadWatts = value; }
        }
    }
}
