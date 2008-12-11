using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

// Logic gate network simulator
namespace LogicNetwork
{

    // TODO:
    // - AbstractCompositeGate.tick()
    // * Network.evaluate()
    // * cloning
    // - parsing from definition file
    // - errors -> exceptions or another handling

    abstract class Gate {

        // Port
        public class Port {
            bool? value; // three-state logic (true, false, null) = (1, 0, ?)
            
            public bool? Value {
                get { return this.value; }
                set { this.value = value; }
            }

            public Port() {
                value = null;
            }

            public Port(Port other) {
                value = other.value;
            }

            public Port(bool? value) {
                this.value = value;
            }
        }

        // NOTE: Input and output ports share the space of their names
        // NOTE: thehe two must be CLONED
        protected Dictionary<string, Port> inputs; // input ports
        protected Dictionary<string, Port> outputs; // output ports

        protected Gate() {
            initialize();
        }

        // Copy constructor
        protected Gate(Gate other) {
            initialize();
            foreach (KeyValuePair<string, Port> kvp in other.inputs) {
                inputs.Add(kvp.Key, new Port(kvp.Value)); // copy Ports
            }
            foreach (KeyValuePair<string, Port> kvp in other.outputs) {
                outputs.Add(kvp.Key, new Port(kvp.Value));
            }
        }

        private void initialize() {
            inputs = new Dictionary<string, Port>();
            outputs = new Dictionary<string, Port>();
        }

        // Cloning support for the Prototype pattern
        public abstract Gate clone();

        // Make one computing step and change outputs somehow.
        // Return true if the gate and possibly all inner gates
        // have stabilized, ie. output values haven't changed in the tick.
        public abstract bool tick();

        // Get a port
        public Port getPort(string portName) {
            if (inputs.ContainsKey(portName)) {
                return inputs[portName];
            } else if (outputs.ContainsKey(portName)) {
                return outputs[portName];
            } else {
                return null; // error, unknown port name
            }
        }

        // Get port group
        protected bool?[] getPortGroup(Dictionary<string, Port> ports) {
            bool?[] values = new bool?[ports.Count];
            int i = 0;
            foreach (KeyValuePair<string, Port> kvp in ports) {
                values[i] = kvp.Value.Value;
                i++;
            }
            return values;
        }

        // Get port group
        protected void setPortGroup(bool?[] portArray, Dictionary<string, Port> ports) {
            int i = 0;
            foreach (KeyValuePair<string, Port> kvp in ports) {
                if (i >= portArray.Length) {
                    break;
                }
                kvp.Value.Value = portArray[i];
                i++;
            }
        }

        public string[] getInputPortNames() {
            return getPortNames(inputs);
        }

        public string[] getOutputPortNames() {
            return getPortNames(outputs);
        }

        private string[] getPortNames(Dictionary<string, Port> ports) {
            List<string> names = new List<string>();
            foreach (string key in ports.Keys) {
                names.Add(key);
            }
            return names.ToArray();
        }

        // These functions are used when defining a gate prototype:

        // Add a new input port, set default value
        protected void addInputPort(string portName) {
            addPort(portName, inputs);
        }

        // Add a new output port, set default value
        protected void addOutputPort(string portName) {
            addPort(portName, outputs);
        }

        // Add a new port to given port group, set default value
        private void addPort(string portName, Dictionary<string, Port> ports) {
            if (!ports.ContainsKey(portName)) {
                ports.Add(portName, new Port((bool?)null));
            } else {
                // error, duplicate definition of a port
            }
        }

    }

    class SimpleGate : Gate {
        // Transition table
        // - key: values of inputs (eg.: 1 0 ? 1 0 1)
        // - value: values of outputs (eg.: 1 0 1)
        // - Dictionary<string, string> might be an overhead
        Dictionary<string, string> transitionTable;

        protected SimpleGate() {
            initialize();
        }

        protected SimpleGate(SimpleGate other) : base(other) {
            initialize();
            //foreach (KeyValuePair<string, string> kvp in other.transitionTable) {
            //    transitionTable.Add(kvp.Key, kvp.Value);
            //}
            // transition table could be shared (I hope)
            transitionTable = other.transitionTable;
        }

        private void initialize() {
            transitionTable = new Dictionary<string, string>();
        }


        public override Gate clone() {
            return new SimpleGate(this);
        }

        public override bool tick() {
            // assign input values from input dictionary
            bool?[] inputValues = getPortGroup(inputs);
            // compute new values
            bool?[] newOutputValues = compute(inputValues);
            bool changed = !outputs.Equals(newOutputValues);
            // assign new output values to output dictionary
            setPortGroup(newOutputValues, outputs);
            return changed;
        }

        // Create a simple gate prototype from string representation
        public static SimpleGate parseSimpleGate(StreamReader inputStream) {
            // TODO
            return null;
        }

        // Compute new output values based on input values
        // directly from transition table or default rules.
        bool?[] compute(bool?[] inputValues) {
            string input = TristateBool.arrayToString(inputValues);
            bool?[] output = new bool?[outputs.Count];
            if (transitionTable.ContainsKey(input)) {
                // output values according to transition table
                return TristateBool.arrayFromString(transitionTable[input]);
            } else {
                // default output values
                bool? outputvalue;
                if (input.Contains("?")) {
                    outputvalue = null; // set all outputs to ?
                } else {
                    outputvalue = false; // set all outputs to 0
                }
                for (int i = 0; i < outputs.Count; i++) {
                    output[i] = outputvalue;
                }
            }
            return output;
        }
        //string compute(string inputValues) {
        //    // TODO
        //    return null;
        //}
    }

    abstract class AbstractCompositeGate : Gate {
        // Inner gates
        // NOTE: this must be CLONED
        Dictionary<string, Gate> gates; // name, Gate
        // Connections between inner gates' (or this gate's) ports.
        // In fact, data flow in direction: src->dest.
        // They are stored in reverse order in dictionary, because we will
        // usually query by destination.
        Dictionary<string, string> connections; // dest, src
        Dictionary<string, List<string>> reverseConnections; // src, list of dests

        protected AbstractCompositeGate() {
            initialize();
        }

        protected AbstractCompositeGate(AbstractCompositeGate other) : base(other) {
            initialize();
            // inner gates must be cloned
            foreach (KeyValuePair<string, Gate> kvp in other.gates) {
                gates.Add(kvp.Key, kvp.Value.clone());
            }
            //foreach (KeyValuePair<string, string> kvp in other.connections) {
            //    connections.Add(kvp.Key, kvp.Value);
            //}
            // connections could be shared (I hope)
            connections = other.connections;
            reverseConnections = other.reverseConnections;
        }

        private void initialize() {
            gates = new Dictionary<string, Gate>();
            connections = new Dictionary<string, string>();
            reverseConnections = new Dictionary<string, List<string>>();
        }

        public override bool tick() {
            // TODO:
            // transmit values to inner gates' inputs from ports which point to them
            foreach (KeyValuePair<string, Gate> kvp in gates) {
                string destGateName = kvp.Key;
                // get names of all input ports of the gate -> dest
                foreach (string destPortName in kvp.Value.getInputPortNames()) {
                    string dest = destGateName + '.' + destPortName;
                    // find which ports point to them (one at time) -> src
                    string src = connections[dest];
                    transmit(src, dest);
                }
            }

            // for all inner gates: tick()
            foreach (KeyValuePair<string, Gate> kvp in gates) {
                kvp.Value.tick();
            }
            
            // transmit values from inner gates' outputs to ports where they point to
            foreach (KeyValuePair<string, Gate> gateKVP in gates) {
                string srcGateName = gateKVP.Key;
                // get names of all output ports of the gate -> src
                foreach (string srcPortName in gateKVP.Value.getOutputPortNames()) {
                    // find to which ports this points (multiple) -> dest
                    string src = srcGateName + '.' + srcPortName;
                    if (reverseConnections.ContainsKey(src)){
                        List<string> dests = reverseConnections[src];
                        foreach (string dest in dests) {
                            transmit(src, dest);
                        }
                    }

                    // Without reverseConnections
                    // This is an O(n) search and might be too slow!
                    //foreach (KeyValuePair<string, string> connKVP in connections) {
                    //    if (connKVP.Value.Equals(src)) {
                    //        transmit(src, connKVP.Key);
                    //    }
                    //}
                }
            }
            return false;
        }

        // Create an abstract composite gate prototype from string representation
        // This is a common code for its descentants not to be called directly.
        // Specific details should be separated into virutal methods.
        protected AbstractCompositeGate parseAbstractCompositeGate(StreamReader inputStream) {
            // TODO
            return null;
        }

        // Transmit a value from source [gate.]port to destination [gate.]port
        protected void transmit(string src, string dest) {
            Port srcPort = getPortByAddress(src);
            Port destPort = getPortByAddress(dest);
            if ((srcPort != null) && (srcPort != null)) {
                destPort.Value = srcPort.Value;
            }
        }

        // Add an inner gate
        protected void addGate(string gateName, Gate gate) {
            if ((gate != null) && !(gate is Network)) {
                gates.Add(gateName, gate);
            } else {
                // TODO: error: gate is not a Network
            }
        }

        // Get gate by name
        protected Gate getGate(string gateName) {
            if (gates.ContainsKey(gateName)) {
                return gates[gateName];
            } else {
                return null; // no such an inner gate
            }
        }

        // Connect two ports
        protected void connect(string src, string dest) {
            Port srcPort = getPortByAddress(src);
            Port destPort = getPortByAddress(dest);
            // Check if src and dest are valid ports.
            // Check if the connection is not duplicate.
            if ((srcPort != null) && (srcPort != null)
                && !connections.ContainsKey(dest)) {
                // add a connection
                connections[dest] = src;
                // add a reverse connection
                if (!reverseConnections.ContainsKey(src)) {
                    reverseConnections.Add(src, new List<string>());
                }
                reverseConnections[src].Add(dest);
            } else {
                // error
            }
        }

        // Get Port by address, eg.: [gate.]port
        // If gate is not specified, currect gate is meant
        protected Port getPortByAddress(string address) {
            string[] parts = address.Split('.');
            if (parts.Length == 1) {
                // a port from this gate
                return getPort(parts[0]);
            } else if (parts.Length == 2) {
                Gate gate = getGate(parts[0]);
                if (gate != null) {
                    // a port from an inner gate
                    return gate.getPort(parts[1]);
                } else {
                    return null; // no such an inner gate
                }
            }
            return null; // invalid address format
        }
    }

    class CompositeGate : AbstractCompositeGate {
        protected CompositeGate(CompositeGate other) : base(other) {
        }

        public override Gate clone() {
            return new CompositeGate(this);
        }

        // Create a composite gate prototype from string representation
        // NOTE: A common parsing code is in parseAbstractCompositeGate().
        public static CompositeGate parseCompositeGate(StreamReader inputStream) {
            // TODO
            return null;
        }
    }

    class Network : AbstractCompositeGate {
        protected Network(Network other) : base(other) {
        }

        public override Gate clone() {
            return new Network(this);
        }

        // Create a network prototype from string representation
        public static Network parseNetwork(StreamReader inputStream) {
            // TODO
            return null;
        }

        // Maximum number of ticks before we decide the network can't stabilize.
        const int MAX_TICKS = 1000000;

        // Let the network compute
        // inputValues: <space separated input values>
        // return: <ticks> <space separated output values>
        public string evaluate(string inputValues) {
            // set inputs according to inputValues
            bool?[] inputsArray = TristateBool.arrayFromString(inputValues);
            setPortGroup(inputsArray, inputs);
            // cycle until bail-out
            int ticks = 0;
            for (; ticks < MAX_TICKS; ticks++) {
                if (tick()) break;
            }
            // return ("{0} {1}", ticks, outputs)
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("{0} ", ticks);
            sb.Append(TristateBool.arrayToString(getPortGroup(outputs)));
            return sb.ToString();
        }
    }

    class GatePrototypeFactory {
        // Singleton

        private static GatePrototypeFactory instance = null;

        private GatePrototypeFactory() {
            gates = new Dictionary<string, Gate>();
            network = null;
        }

        public static GatePrototypeFactory getInstance() {
            if (instance == null) {
                instance = new GatePrototypeFactory();
            }
            return instance;
        }

        // Defined gate prototypes
        Dictionary<string, Gate> gates; // gateName, gate

        // The network (there's only one)
        Network network;

        // Parse a file with definition of all the gates and a network
        // Can throw SyntaxErrorException
        public void parseGates(StreamReader inputStream) {
            // TODO
        }
        
        // Create a clone of a defined gate prototype
        public Gate createGate(string gateName) {
            if (gates.ContainsKey(gateName)) {
                return gates[gateName].clone();
            }
            return null; // error: no gate of such a name
        }

        // The network as a property
        public Network Network {
            get { return network; }
        }

        // Register a new gate prototype
        void defineGate(string gateName, Gate gate) {
            if (gate == null) {
                // TODO: error: gate is null
            } else if (gate is Network) {
                if (network == null) {
                    network = (Network) gate;
                } else {
                    // TODO: error: more than one network
                }
            } else {
                if (!gates.ContainsKey(gateName)) {
                    gates.Add(gateName, gate);
                } else {
                    // TODO: error: duplicate gate definition
                }
            }
        }

    }

    // (De)serialization of three-state boolean values
    class TristateBool
    {
        public static string toString(bool? value) {
            if (value.HasValue) {
                if (value.Value) return "1";
                else return "0";
            } else {
                return "?";
            }
        }

        public static bool? fromString(string str) {
            if (str.Equals("1")) {
                return true;
            } else if (str.Equals("1")) {
                return false;
            } else {
                return null;
            }
        }

        public static string arrayToString(bool?[] array) {
            StringBuilder sb = new StringBuilder();
            foreach (bool? value in array) {
                sb.Append(TristateBool.toString(value));
            }
            return sb.ToString().TrimEnd();
        }

        public static bool?[] arrayFromString(string array) {
            string[] parts = array.Split(' ');
            List<bool?> values = new List<bool?>();
            foreach (string part in parts) {
                values.Add(TristateBool.fromString(part));
            }
            return values.ToArray();
        }
    }

    class SyntaxErrorException : ApplicationException { }

    class Program
    {
        static void Main(string[] args) {
            if (args.Length == 1) {
                GatePrototypeFactory gateFactory = GatePrototypeFactory.getInstance();

                FileStream fs = null;
                try {
                    fs = new FileStream(args[0], FileMode.Open, FileAccess.Read);
                    StreamReader reader = new StreamReader(fs);
                    
                    // Parse the config file.
                    // Fill the GatePrototypeFactory with gate prototypes.

                    gateFactory.parseGates(reader);

                } catch (FileNotFoundException) {
                    Console.WriteLine("File not found: {0}", args[0]);
                    return;
                } finally {
                    if (fs != null) {
                        fs.Close();
                    }
                }
                
                // create an instance of the network
                Network network = gateFactory.Network;

                if (network == null) {
                    Console.WriteLine("Error: Syntax error. No network specified.");
                    return;
                }
                
                // main evaluating loop
                string line = "";
                while ((line = Console.ReadLine()) != null) {
                    if (line.Equals("end")) {
                        break;
                    }
                    Console.WriteLine(network.evaluate(line));
                }
            } else {
                // error: no file specified
                Console.WriteLine("Usage: LogicNetwork.exe definition_file.txt");
            }
        }
    }
}
