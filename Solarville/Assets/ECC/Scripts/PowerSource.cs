using System.Collections.Generic;
using UnityEngine;

namespace ECC
{
    [CreateAssetMenu(fileName = "Power Source", menuName = "ECC/Power Source", order = 10)]
    public class PowerSource : BaseModule
    {
        [Header("Electrical")]
        public float Volts;
        public float AmpHours;
    }

    [System.Serializable]
    public class PowerSourceObject
    {
        public bool SwitchedOn = true;
        public PowerSource PowerSource;
        public List<CircuitObject> Circuits;


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


        float ampHoursUsed;
        public float AmpHoursUsed
        {
            get { return ampHoursUsed; }
            set { ampHoursUsed = PowerSource == null ? 0 : Mathf.Clamp(value, 0, PowerSource.AmpHours); }
        }


        public float AmpHoursRemaining
        {
            get { return PowerSource == null ? 0 : PowerSource.AmpHours - ampHoursUsed; }
        }
    }
}