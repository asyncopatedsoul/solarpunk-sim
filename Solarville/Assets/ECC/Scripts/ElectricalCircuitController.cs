using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ECC
{
    public class ElectricalCircuitController : MonoBehaviour
    {
        public float RefreshRate = 1;
        public float TimeMultiplier = 1;
        public List<NetworkObject> Networks;

        public event EventHandler<CircuitObject> CircuitOverloaded;

        [SerializeField]
        int editorNetworkIndex;
        [SerializeField]
        int editorPowerSourceIndex;
        [SerializeField]
        int editorCircuitIndex;
        [SerializeField]
        int editorPowerConsumerIndex;

        const float secondsPerHour = 60 * 60;

        float timeDelta;


        void Update()
        {
            timeDelta += Time.deltaTime;

            if (timeDelta >= RefreshRate)
            {
                Process(timeDelta);
                timeDelta = 0;
            }
        }

        void Process(float timeDelta)
        {
            foreach (var network in Networks)
            {
                foreach (var powerSource in network.PowerSources)
                {
                    powerSource.HasPower = powerSource.SwitchedOn && powerSource.AmpHoursRemaining > 0 && network.SwitchedOn;

                    foreach (var circuit in powerSource.Circuits)
                    {
                        circuit.HasPower = circuit.SwitchedOn && powerSource.HasPower;

                        float wattage = 0;
                        float chargeNow = 0;

                        circuit.LoadWatts = 0;
                        circuit.CapacityWatts = circuit.Circuit.AmperageRating * powerSource.PowerSource.Volts;

                        foreach (var powerConsumer in circuit.PowerConsumers)
                        {
                            powerConsumer.HasPower = powerConsumer.SwitchedOn && circuit.HasPower;

                            if (circuit.HasPower)
                            {
                                wattage += powerConsumer.ParasiticDrain;

                                if (powerConsumer.SwitchedOn)
                                {
                                    wattage += powerConsumer.PowerConsumer == null ? 0 : powerConsumer.PowerConsumer.Watts;
                                }
                            }
                        }

                        float ampsPerHour = wattage / powerSource.PowerSource.Volts;
                        ampsPerHour += ampsPerHour * circuit.Circuit.Loss;
                        float ampsPerSecond = ampsPerHour / secondsPerHour;
                        float ampsNow = ampsPerSecond * timeDelta * TimeMultiplier;

                        powerSource.AmpHoursUsed = powerSource.AmpHoursUsed + ampsNow - chargeNow;

                        circuit.LoadWatts = wattage;

                        // Switch circuit off if overloaded (similar to tripping a circuit breaker)
                        if (wattage > circuit.CapacityWatts)
                        {
                            CircuitOverloaded?.Invoke(this, circuit);
                            circuit.SwitchedOn = false;
                        }
                    }
                }
            }
        }


        // ******************************************
        //
        // NetworkObject helper methods
        //
        // ******************************************

        public NetworkObject FirstNetwork()
        {
            return NetworkByIndex(0);
        }

        public int NetworkCount()
        {
            return Networks.Count;
        }

        public NetworkObject NetworkByName(string name)
        {
            return Networks.FirstOrDefault(x => x.Network != null && x.Network.Name == name);
        }

        public NetworkObject NetworkByIndex(int index)
        {
            return Networks.ElementAtOrDefault(index);
        }


        // ******************************************
        //
        // PowerSource helper methods
        //
        // ******************************************

        public PowerSourceObject FirstPowerSource(NetworkObject network)
        {
            return PowerSourceByIndex(network, 0);
        }

        public int PowerSourceCount(NetworkObject network)
        {
            return network == null || network.PowerSources == null ? 0 : network.PowerSources.Count;
        }

        public PowerSourceObject PowerSourceByName(NetworkObject network, string name)
        {
            return network?.PowerSources.FirstOrDefault(x => x.PowerSource.Name == name);
        }

        public PowerSourceObject PowerSourceByIndex(NetworkObject network, int index)
        {
            return network?.PowerSources.ElementAtOrDefault(index);
        }


        // ******************************************
        //
        // Circuit helper methods
        //
        // ******************************************

        public CircuitObject FirstCircuit(PowerSourceObject powerSource)
        {
            return CircuitByIndex(powerSource, 0);
        }

        public int CircuitCount(PowerSourceObject powerSource)
        {
            return powerSource == null || powerSource.Circuits == null ? 0 : powerSource.Circuits.Count;
        }

        public CircuitObject CircuitByName(PowerSourceObject powerSource, string name)
        {
            return powerSource?.Circuits.FirstOrDefault(x => x.Circuit.Name == name);
        }

        public CircuitObject CircuitByIndex(PowerSourceObject powerSource, int index)
        {
            return powerSource?.Circuits.ElementAtOrDefault(index);
        }


        // ******************************************
        //
        // Power Consumer helper methods
        //
        // ******************************************

        public PowerConsumerObject FirstPowerConsumer(CircuitObject circuit)
        {
            return PowerConsumerByIndex(circuit, 0);
        }

        public int PowerConsumerCount(CircuitObject circuit)
        {
            return circuit == null || circuit.PowerConsumers == null ? 0 : circuit.PowerConsumers.Count;
        }

        public PowerConsumerObject PowerConsumerByName(CircuitObject circuit, string name)
        {
            return circuit?.PowerConsumers.FirstOrDefault(x => x.PowerConsumer.Name == name);
        }

        public PowerConsumerObject PowerConsumerByIndex(CircuitObject circuit, int index)
        {
            return circuit?.PowerConsumers.ElementAtOrDefault(index);
        }
    }
}

