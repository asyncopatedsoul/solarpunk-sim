using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace ECC
{
    [CustomEditor(typeof(ElectricalCircuitController))]
    public class ECCEditor : Editor
    {
        const string none = "None";
        const string unspecified = "Unspecified";
        const string switchOn = "SWITCH ON";
        const string switchOff = "SWITCH OFF";

        ElectricalCircuitController ecc;

        int networkIndex;
        int powerSourceIndex;
        int circuitIndex;
        int powerConsumerIndex;

        bool addingNetwork;
        bool removingNetwork;
        Network networkSO = null;
        bool addingPowerSource;
        bool removingPowerSource;
        PowerSource powerSourceSO = null;
        bool addingCircuit;
        bool removingCircuit;
        Circuit circuitSO = null;
        bool addingPowerConsumer;
        bool removingPowerConsumer;
        PowerConsumer powerConsumerSO = null;

        SerializedProperty editorNetworkIndex;
        SerializedProperty editorPowerSourceIndex;
        SerializedProperty editorCircuitIndex;
        SerializedProperty editorPowerConsumerIndex;


        void OnEnable()
        {
            editorNetworkIndex = serializedObject.FindProperty("editorNetworkIndex");
            editorPowerSourceIndex = serializedObject.FindProperty("editorPowerSourceIndex");
            editorCircuitIndex = serializedObject.FindProperty("editorCircuitIndex");
            editorPowerConsumerIndex = serializedObject.FindProperty("editorPowerConsumerIndex");
        }

        void AddVerticalSpace(int v = 5)
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.Space(v);
            EditorGUILayout.EndVertical();
        }

        string[] GetNetworks()
        {
            var strings = new List<string>();
            strings.Add(none);

            var count = 1;
            foreach (var network in ecc.Networks)
            {
                var s = network.Network == null ? unspecified : network.Network.Name;
                strings.Add(string.Format($"{count++}. {s}"));
            }

            return strings.ToArray();
        }

        string[] GetPowerSources(NetworkObject network)
        {
            var strings = new List<string>();
            strings.Add(none);

            var count = 1;
            foreach (var powerSource in network.PowerSources)
            {
                var s = powerSource.PowerSource == null ? unspecified : powerSource.PowerSource.Name;
                strings.Add(string.Format($"{count++}. {s}"));
            }

            return strings.ToArray();
        }

        string[] GetCircuits(PowerSourceObject powerSource)
        {
            var strings = new List<string>();
            strings.Add(none);

            var count = 1;
            foreach (var circuit in powerSource.Circuits)
            {
                var s = circuit.Circuit == null ? unspecified : circuit.Circuit.Name;
                strings.Add(string.Format($"{count++}. {s}"));
            }

            return strings.ToArray();
        }

        string[] GetPowerConsumers(CircuitObject circuit)
        {
            var strings = new List<string>();
            strings.Add(none);

            var count = 1;
            foreach (var powerConsumer in circuit.PowerConsumers)
            {
                var s = powerConsumer.PowerConsumer == null ? unspecified : powerConsumer.PowerConsumer.Name;
                strings.Add(string.Format($"{count++}. {s}"));
            }

            return strings.ToArray();
        }

        void DrawControls()
        {
            var colWidth = 110;
            var buttonWidth = 30;
            bool comboHasChanged;



            AddVerticalSpace(10);
            EditorGUILayout.LabelField("ECC - ELECTRICAL CIRCUIT CONTROLLER", EditorStyles.boldLabel);
            AddVerticalSpace(10);



            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Refresh Rate", GUILayout.Width(colWidth));
            ecc.RefreshRate = EditorGUILayout.FloatField(ecc.RefreshRate, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();
            AddVerticalSpace();



            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Time Multiplier", GUILayout.Width(colWidth));
            ecc.TimeMultiplier = EditorGUILayout.FloatField(ecc.TimeMultiplier, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();
            AddVerticalSpace();



            //EditorGUILayout.BeginHorizontal();
            //EditorGUILayout.LabelField("Run In Edit Mode", GUILayout.Width(colWidth));
            //ecc.RunInEditor = EditorGUILayout.Toggle(ecc.RunInEditor);
            //EditorGUILayout.EndHorizontal();
            //AddVerticalSpace();



            // ******************************************
            //
            // Networks maintenance
            //
            // ******************************************

            EditorGUILayout.BeginHorizontal();
            string[] networks = GetNetworks();
            EditorGUILayout.LabelField("Networks", GUILayout.Width(colWidth));

            EditorGUI.BeginChangeCheck();
            networkIndex = EditorGUILayout.Popup(networkIndex, networks);
            comboHasChanged = EditorGUI.EndChangeCheck();

            if (GUILayout.Button("+", GUILayout.Width(buttonWidth)))
                addingNetwork = true;
            if (GUILayout.Button("-", GUILayout.Width(buttonWidth)) && !addingNetwork && networkIndex > 0)
                removingNetwork = true;

            EditorGUILayout.EndHorizontal();

            if (addingNetwork)
            {
                EditorGUILayout.BeginHorizontal();
                networkSO = (Network)EditorGUILayout.ObjectField(networkSO, typeof(Network), false);
                if (GUILayout.Button("OK", GUILayout.Width(65)) && networkSO != null)
                {
                    var n = new NetworkObject { Network = networkSO, PowerSources = new List<PowerSourceObject>() };
                    ecc.Networks.Add(n);
                    addingNetwork = false;
                }
                if (GUILayout.Button("CANCEL", GUILayout.Width(65)))
                {
                    addingNetwork = false;
                }
                EditorGUILayout.EndHorizontal();
            }

            if (removingNetwork)
            {
                ecc.Networks.RemoveAt(networkIndex - 1);
                removingNetwork = false;
                networkIndex = 0;
                serializedObject.Update();
            }

            if (networkIndex == 0 || comboHasChanged)
            {
                powerSourceIndex = 0;
                circuitIndex = 0;
                powerConsumerIndex = 0;
                return;
            }
            AddVerticalSpace();



            // ******************************************
            //
            // Power Sources maintenance
            //
            // ******************************************

            EditorGUILayout.BeginHorizontal();
            var theNetwork = ecc.Networks[networkIndex - 1];
            string[] powerSources = GetPowerSources(theNetwork);
            EditorGUILayout.LabelField("Power Sources", GUILayout.Width(colWidth));

            EditorGUI.BeginChangeCheck();
            powerSourceIndex = EditorGUILayout.Popup(powerSourceIndex, powerSources);
            comboHasChanged = EditorGUI.EndChangeCheck();

            if (GUILayout.Button("+", GUILayout.Width(buttonWidth)))
                addingPowerSource = true;
            if (GUILayout.Button("-", GUILayout.Width(buttonWidth)) && !addingPowerSource && powerSourceIndex > 0)
                removingPowerSource = true;

            EditorGUILayout.EndHorizontal();

            if (addingPowerSource)
            {
                EditorGUILayout.BeginHorizontal();
                powerSourceSO = (PowerSource)EditorGUILayout.ObjectField(powerSourceSO, typeof(PowerSource), false);
                if (GUILayout.Button("OK", GUILayout.Width(65)) && powerSourceSO != null)
                {
                    var n = new PowerSourceObject { PowerSource = powerSourceSO, Circuits = new List<CircuitObject>() };
                    theNetwork.PowerSources.Add(n);
                    addingPowerSource = false;
                }
                if (GUILayout.Button("CANCEL", GUILayout.Width(65)))
                {
                    addingPowerSource = false;
                }
                EditorGUILayout.EndHorizontal();
            }

            if (removingPowerSource)
            {
                theNetwork.PowerSources.RemoveAt(powerSourceIndex - 1);
                removingPowerSource = false;
                powerSourceIndex = 0;
                serializedObject.Update();
            }

            if (powerSourceIndex == 0 || comboHasChanged)
            {
                circuitIndex = 0;
                powerConsumerIndex = 0;
                return;
            }
            AddVerticalSpace();



            // ******************************************
            //
            // Circuits maintenance
            //
            // ******************************************

            EditorGUILayout.BeginHorizontal();
            var thePowerSource = theNetwork.PowerSources[powerSourceIndex - 1];
            string[] circuits = GetCircuits(thePowerSource);
            EditorGUILayout.LabelField("Circuits", GUILayout.Width(colWidth));

            EditorGUI.BeginChangeCheck();
            circuitIndex = EditorGUILayout.Popup(circuitIndex, circuits);
            comboHasChanged = EditorGUI.EndChangeCheck();

            if (GUILayout.Button("+", GUILayout.Width(buttonWidth)))
                addingCircuit = true;
            if (GUILayout.Button("-", GUILayout.Width(buttonWidth)) && !addingCircuit && circuitIndex > 0)
                removingCircuit = true;

            EditorGUILayout.EndHorizontal();

            if (addingCircuit)
            {
                EditorGUILayout.BeginHorizontal();
                circuitSO = (Circuit)EditorGUILayout.ObjectField(circuitSO, typeof(Circuit), false);
                if (GUILayout.Button("OK", GUILayout.Width(65)) && circuitSO != null)
                {
                    var n = new CircuitObject { Circuit = circuitSO, PowerConsumers = new List<PowerConsumerObject>() };
                    thePowerSource.Circuits.Add(n);
                    addingCircuit = false;
                }
                if (GUILayout.Button("CANCEL", GUILayout.Width(65)))
                {
                    addingCircuit = false;
                }
                EditorGUILayout.EndHorizontal();
            }

            if (removingCircuit)
            {
                thePowerSource.Circuits.RemoveAt(circuitIndex - 1);
                removingCircuit = false;
                circuitIndex = 0;
                serializedObject.Update();
            }

            if (circuitIndex == 0 || comboHasChanged)
            {
                powerConsumerIndex = 0;
                return;
            }
            AddVerticalSpace();



            // ******************************************
            //
            // Power Consumers maintenance
            //
            // ******************************************

            EditorGUILayout.BeginHorizontal();
            var theCircuit = thePowerSource.Circuits[circuitIndex - 1];
            string[] powerConsumers = GetPowerConsumers(theCircuit);
            EditorGUILayout.LabelField("Power Consumers", GUILayout.Width(colWidth));

            EditorGUI.BeginChangeCheck();
            powerConsumerIndex = EditorGUILayout.Popup(powerConsumerIndex, powerConsumers);
            comboHasChanged = EditorGUI.EndChangeCheck();

            if (GUILayout.Button("+", GUILayout.Width(buttonWidth)))
                addingPowerConsumer = true;
            if (GUILayout.Button("-", GUILayout.Width(buttonWidth)) && !addingPowerConsumer && powerConsumerIndex > 0)
                removingPowerConsumer = true;

            EditorGUILayout.EndHorizontal();

            if (addingPowerConsumer)
            {
                EditorGUILayout.BeginHorizontal();
                powerConsumerSO = (PowerConsumer)EditorGUILayout.ObjectField(powerConsumerSO, typeof(PowerConsumer), false);
                if (GUILayout.Button("OK", GUILayout.Width(65)) && powerConsumerSO != null)
                {
                    var n = new PowerConsumerObject { PowerConsumer = powerConsumerSO };
                    theCircuit.PowerConsumers.Add(n);
                    addingPowerConsumer = false;
                }
                if (GUILayout.Button("CANCEL", GUILayout.Width(65)))
                {
                    addingPowerConsumer = false;
                }
                EditorGUILayout.EndHorizontal();
            }

            if (removingPowerConsumer)
            {
                theCircuit.PowerConsumers.RemoveAt(powerConsumerIndex - 1);
                removingPowerConsumer = false;
                powerConsumerIndex = 0;
                serializedObject.Update();
            }

        }

        void DrawState()
        {
            string name;
            GUIStyle style;

            var greenText = new GUIStyle(EditorStyles.label);
            greenText.normal.textColor = Color.green;

            var redText = new GUIStyle(EditorStyles.label);
            redText.normal.textColor = Color.red;

            var yellowText = new GUIStyle(EditorStyles.label);
            yellowText.normal.textColor = Color.yellow;

            var centerText = new GUIStyle(GUI.skin.label);
            centerText.alignment = TextAnchor.MiddleCenter;

            // no colored text unless playing
            if (!EditorApplication.isPlaying)
            {
                greenText = new GUIStyle(EditorStyles.label);
                redText = new GUIStyle(EditorStyles.label);
            }



            AddVerticalSpace(10);
            EditorGUILayout.LabelField("ELECTRICAL NETWORK STATE", EditorStyles.boldLabel);
            AddVerticalSpace(10);



            if (ecc.Networks == null || ecc.Networks.Count == 0)
            {
                EditorGUILayout.LabelField("No Electrical Networks Defined Yet", yellowText);
                return;
            }


            foreach (var network in ecc.Networks)
            {
                EditorGUILayout.BeginHorizontal();
                style = network.Operating ? greenText : redText;
                name = network.Network == null ? unspecified : network.Network.Name;
                EditorGUILayout.LabelField(name, style);
                GUILayout.FlexibleSpace();

                if (GUILayout.Button(network.SwitchedOn ? switchOff : switchOn, GUILayout.Width(90)))
                    network.SwitchedOn = !network.SwitchedOn;

                EditorGUILayout.EndHorizontal();
                AddVerticalSpace();


                EditorGUI.indentLevel += 1;
                foreach (var powerSource in network.PowerSources)
                {
                    EditorGUILayout.BeginHorizontal();
                    style = powerSource.Operating ? greenText : redText;
                    name = powerSource.PowerSource == null ? unspecified : powerSource.PowerSource.Name;
                    EditorGUILayout.LabelField(name, style);
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button(powerSource.SwitchedOn ? switchOff : switchOn, GUILayout.Width(90)))
                        powerSource.SwitchedOn = !powerSource.SwitchedOn;

                    EditorGUILayout.EndHorizontal();
                    AddVerticalSpace();


                    EditorGUI.indentLevel += 1;
                    foreach (var circuit in powerSource.Circuits)
                    {
                        EditorGUILayout.BeginHorizontal();
                        style = circuit.Operating ? greenText : redText;
                        name = circuit.Circuit == null ? unspecified : circuit.Circuit.Name;
                        EditorGUILayout.LabelField(name, style);
                        GUILayout.FlexibleSpace();

                        if (GUILayout.Button(circuit.SwitchedOn ? switchOff : switchOn, GUILayout.Width(90)))
                            circuit.SwitchedOn = !circuit.SwitchedOn;

                        EditorGUILayout.EndHorizontal();
                        AddVerticalSpace();


                        EditorGUI.indentLevel += 1;
                        foreach (var powerConsumer in circuit.PowerConsumers)
                        {
                            EditorGUILayout.BeginHorizontal();
                            style = powerConsumer.Operating ? greenText : redText;
                            name = powerConsumer.PowerConsumer == null ? unspecified : powerConsumer.PowerConsumer.Name;
                            EditorGUILayout.LabelField(name, style);
                            GUILayout.FlexibleSpace();

                            if (GUILayout.Button(powerConsumer.SwitchedOn ? switchOff : switchOn, GUILayout.Width(90)))
                                powerConsumer.SwitchedOn = !powerConsumer.SwitchedOn;

                            EditorGUILayout.EndHorizontal();
                            AddVerticalSpace();
                        }
                        EditorGUI.indentLevel -= 1;
                    }
                    EditorGUI.indentLevel -= 1;
                }
                EditorGUI.indentLevel -= 1;
            }



            AddVerticalSpace(10);
            EditorGUILayout.LabelField("ELECTRICAL NETWORK AMP METER", EditorStyles.boldLabel);
            AddVerticalSpace(10);



            foreach (var network in ecc.Networks)
            {
                foreach (var powerSource in network.PowerSources)
                {
                    var powerSourceName = powerSource.PowerSource == null ? unspecified : powerSource.PowerSource.Name;
                    var powerSourceUsed = "0";
                    var powerSourceRemaining = "0";

                    if (powerSource.PowerSource != null && EditorApplication.isPlaying)
                    {
                        powerSourceUsed = string.Format($"{powerSource.AmpHoursUsed:0.000000}");
                        powerSourceRemaining = string.Format($"{powerSource.PowerSource.AmpHours - powerSource.AmpHoursUsed:0.0000}");
                    }

                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(powerSourceName, GUILayout.Width(140));
                    EditorGUILayout.LabelField(powerSourceUsed, GUILayout.Width(100));
                    EditorGUILayout.LabelField(powerSourceRemaining, GUILayout.Width(100));
                    EditorGUILayout.EndHorizontal();
                    AddVerticalSpace();

                    foreach (var circuit in powerSource.Circuits)
                    {
                        var circuitName = circuit.Circuit == null ? unspecified : circuit.Circuit.Name;
                        var circuitLoad1 = "0 / 0";
                        var circuitLoad2 = "0 %";

                        if (circuit.Circuit != null && EditorApplication.isPlaying)
                        {
                            circuitLoad1 = string.Format($"{circuit.LoadWatts:0.00} / {circuit.CapacityWatts:0}");
                            circuitLoad2 = string.Format($"{circuit.LoadWatts / circuit.CapacityWatts * 100f:0.00} %");
                        }

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(circuitName, GUILayout.Width(140));
                        EditorGUILayout.LabelField(circuitLoad1, GUILayout.Width(100));
                        EditorGUILayout.LabelField(circuitLoad2, GUILayout.Width(100));
                        EditorGUILayout.EndHorizontal();
                        AddVerticalSpace();

                        foreach (var powerConsumer in circuit.PowerConsumers)
                        {
                        }
                    }
                }
            }
        }


        public override void OnInspectorGUI()
        {
            serializedObject.Update();


            ecc = (ElectricalCircuitController)target;
            if (ecc.Networks == null)
                ecc.Networks = new List<NetworkObject>();


            networkIndex = editorNetworkIndex.intValue;
            powerSourceIndex = editorPowerSourceIndex.intValue;
            circuitIndex = editorCircuitIndex.intValue;
            powerConsumerIndex = editorPowerConsumerIndex.intValue;


            DrawControls();
            DrawState();


            editorNetworkIndex.intValue = networkIndex;
            editorPowerSourceIndex.intValue = powerSourceIndex;
            editorCircuitIndex.intValue = circuitIndex;
            editorPowerConsumerIndex.intValue = powerConsumerIndex;


            serializedObject.ApplyModifiedProperties();


            Repaint();
        }
    }
}
