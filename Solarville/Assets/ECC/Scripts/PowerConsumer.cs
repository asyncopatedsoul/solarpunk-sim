using UnityEngine;

namespace ECC
{
    [CreateAssetMenu(fileName = "Power Consumer", menuName = "ECC/Power Consumer", order = 20)]
    public class PowerConsumer : BaseModule
    {
        [Header("Electrical")]
        [Tooltip("How much power this device consumes")]
        public float Watts;
        [Tooltip("Minimum power consumption when turned off")]
        public float ParasiticDrainWattsMin;
        [Tooltip("Maximum power consumption when turned off")]
        public float ParasiticDrainWattsMax;
    }

    [System.Serializable]
    public class PowerConsumerObject
    {
        public bool SwitchedOn = true;
        public PowerConsumer PowerConsumer;


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


        public float ParasiticDrain
        {
            get { return PowerConsumer == null ? 0 : Random.Range(PowerConsumer.ParasiticDrainWattsMin, PowerConsumer.ParasiticDrainWattsMax); }
        }
    }
}
