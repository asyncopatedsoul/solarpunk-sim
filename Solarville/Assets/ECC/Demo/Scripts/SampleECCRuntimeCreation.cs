using System.Collections.Generic;
using UnityEngine;
using ECC;
using Network = ECC.Network;

public class SampleECCRuntimeCreation : MonoBehaviour
{
    // Example sript showing how to create an ECC network at runtime


    ElectricalCircuitController ecc;

    void Start()
    {
        CreateNetwork();
    }

    void CreateNetwork()
    {
        ecc = new ElectricalCircuitController
        {
            RefreshRate = 0,         // ECC will update internally every frame
            TimeMultiplier = 0.25f,  // Time will run 4 times quicker

            Networks = new List<NetworkObject>
            {
                new NetworkObject
                {
                    SwitchedOn = false,
                    Network = LoadNetworkModule("Flashlight"),

                    PowerSources = new List<PowerSourceObject>
                    {
                       new PowerSourceObject
                       {
                           SwitchedOn = false,
                           PowerSource = LoadPowerSourceModule("AAA Battery"),

                           Circuits = new List<CircuitObject>
                           {
                               new CircuitObject
                               {
                                   SwitchedOn = false,
                                   Circuit = LoadCircuitModule("Basic Circuit"),

                                   PowerConsumers = new List<PowerConsumerObject>
                                   {
                                       new PowerConsumerObject
                                       {
                                           SwitchedOn = false,
                                           PowerConsumer = LoadPowerConsumerModule("White LED"),
                                       },
                                       new PowerConsumerObject
                                       {
                                           SwitchedOn = false,
                                           PowerConsumer = LoadPowerConsumerModule("Small Microprocessor"),
                                       }
                                   }
                               }
                           }
                       }
                    }
                }
            }
        };
    }

    Network LoadNetworkModule(string moduleName)
    {
        // find and load the required Network module here
        return null;
    }
    PowerSource LoadPowerSourceModule(string moduleName)
    {
        // find and load the required Power Source module here
        return null;
    }
    Circuit LoadCircuitModule(string moduleName)
    {
        // find and load the required Circuit module here
        return null;
    }
    PowerConsumer LoadPowerConsumerModule(string moduleName)
    {
        // find and load the required Power Consumer module here
        return null;
    }
}
