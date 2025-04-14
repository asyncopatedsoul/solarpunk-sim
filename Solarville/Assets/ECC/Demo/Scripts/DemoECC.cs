using UnityEngine;
using UnityEngine.UI;
using ECC;

public class DemoECC : MonoBehaviour
{
    [Header("References")]
    public GameObject Lightbeam;
    public ElectricalCircuitController Flashlight;

    [Header("Text References")]
    public Text NetworkName;
    public Text PowerSourceName;
    public Text CircuitName;
    public Text PowerConsumer1Name;
    public Text PowerConsumer2Name;
    public Text AmpsUsedData;
    public Text AmpsRemainingData;
    public Text CircuitLoadData;
    public Text CircultLoadPercentData;

    [Header("Buttons")]
    public Button NetworkSwitch;
    public Text NetworkSwitchText;
    public Button PowerSourceSwitch;
    public Text PowerSourceSwitchText;
    public Button CircuitSwitch;
    public Text CircuitSwitchText;
    public Button PowerConsumer1Switch;
    public Text PowerConsumer1SwitchText;
    public Button PowerConsumer2Switch;
    public Text PowerConsumer2SwitchText;

    const string colorTag = "</color>";
    const string onColorTag = "<color=lime>";
    const string offColorTag = "<color=red>";
    const string switchOn = "SWITCH ON";
    const string switchOff = "SWITCH OFF";

    NetworkObject network;
    PowerSourceObject powerSource;
    CircuitObject circuit;
    PowerConsumerObject powerConsumer1;
    PowerConsumerObject powerConsumer2;


    void Start()
    {
        Flashlight.CircuitOverloaded += OnCircuitOverloaded;

        NetworkSwitch.onClick.AddListener(DoNetworkSwitch);
        PowerSourceSwitch.onClick.AddListener(DoPowerSourceSwitch);
        CircuitSwitch.onClick.AddListener(DoCircuitSwitch);
        PowerConsumer1Switch.onClick.AddListener(DoPowerConsumer1Switch);
        PowerConsumer2Switch.onClick.AddListener(DoPowerConsumer2Switch);

        network = Flashlight.FirstNetwork();
        powerSource = Flashlight.FirstPowerSource(network);
        circuit = Flashlight.FirstCircuit(powerSource);
        powerConsumer1 = Flashlight.PowerConsumerByIndex(circuit, 0);
        powerConsumer2 = Flashlight.PowerConsumerByIndex(circuit, 1);
    }

    void Update()
    {
        RefreshValues();
    }

    void RefreshValues()
    {
        string c;

        c = network.Operating ? onColorTag : offColorTag;
        NetworkName.text = string.Format($"Network Name: {c}{network.Network.Name}{colorTag}");

        c = powerSource.Operating ? onColorTag : offColorTag;
        PowerSourceName.text = string.Format($"Power Source Name: {c}{powerSource.PowerSource.Name}{colorTag}");

        c = circuit.Operating ? onColorTag : offColorTag;
        CircuitName.text = string.Format($"Circuit Name: {c}{circuit.Circuit.Name}{colorTag}");

        c = powerConsumer1.Operating ? onColorTag : offColorTag;
        PowerConsumer1Name.text = string.Format($"{c}{powerConsumer1.PowerConsumer.Name}{colorTag}");

        c = powerConsumer2.Operating ? onColorTag : offColorTag;
        PowerConsumer2Name.text = string.Format($"{c}{powerConsumer2.PowerConsumer.Name}{colorTag}");

        NetworkSwitchText.text = network.SwitchedOn ? switchOff : switchOn;
        PowerSourceSwitchText.text = powerSource.SwitchedOn ? switchOff : switchOn;
        CircuitSwitchText.text = circuit.SwitchedOn ? switchOff : switchOn;
        PowerConsumer1SwitchText.text = powerConsumer1.SwitchedOn ? switchOff : switchOn;
        PowerConsumer2SwitchText.text = powerConsumer2.SwitchedOn ? switchOff : switchOn;

        AmpsUsedData.text = string.Format($"{powerSource.AmpHoursUsed * 1000:0.000000}");
        AmpsRemainingData.text = string.Format($"{(powerSource.PowerSource.AmpHours - powerSource.AmpHoursUsed) * 1000:0.0000}");

        CircuitLoadData.text = string.Format($"{circuit.LoadWatts:0.00} / {circuit.CapacityWatts:0}");
        CircultLoadPercentData.text = string.Format($"{circuit.LoadWatts / circuit.CapacityWatts * 100f:0.00} %");

        Lightbeam.SetActive(powerConsumer1.Operating);
    }

    void OnCircuitOverloaded(object sender, CircuitObject circuit)
    {
        Debug.Log("Circuit Overloaded event received");
    }

    void DoNetworkSwitch()
    {
        network.SwitchedOn = !network.SwitchedOn;
    }

    void DoPowerSourceSwitch()
    {
        powerSource.SwitchedOn = !powerSource.SwitchedOn;
    }

    void DoCircuitSwitch()
    {
        circuit.SwitchedOn = !circuit.SwitchedOn;
    }

    void DoPowerConsumer1Switch()
    {
        powerConsumer1.SwitchedOn = !powerConsumer1.SwitchedOn;
    }

    void DoPowerConsumer2Switch()
    {
        powerConsumer2.SwitchedOn = !powerConsumer2.SwitchedOn;
    }
}
