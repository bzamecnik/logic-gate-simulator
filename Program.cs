using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace LogicNetwork
{

    // (De)serialization of three-state boolean values
    class TristateBool {
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

    abstract class Gate {

        // Port
        public class Port {
            bool? value;
            
            public bool? Value {
                get { return this.value; }
                set { this.value = value; }
            }

            public Port() {
                value = null;
            }

            public Port(bool? value) {
                this.value = value;
            }
        }

        // NOTE: Input and output ports share the space of their names
        protected Dictionary<string, Port> inputs; // input ports
        protected Dictionary<string, Port> outputs; // output ports

        protected Gate() {
            initialize();
        }

        // Copy constructor
        protected Gate(Gate other) : this() {
            initialize();
            foreach (KeyValuePair<string, Port> kvp in other.inputs) {
                inputs.Add(kvp.Key, kvp.Value);
            }
            foreach (KeyValuePair<string, Port> kvp in other.outputs) {
                outputs.Add(kvp.Key, kvp.Value);
            }
        }

        private void initialize() {
            inputs = new Dictionary<string, Port>();
            outputs = new Dictionary<string, Port>();
        }

        // Make one computing step and change outputs somehow
        // Return true if the gate and possibly all inner gates
        // have stabilized, ie. output values haven't changed in the tick
        public abstract bool tick();

        // Cloning support for the Prototype pattern
        public abstract Gate clone();

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
                ports.Add(portName, new Port(null));
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

        protected SimpleGate(Gate other) : base(other) {
            initialize();
        }

        private void initialize() {
            transitionTable = new Dictionary<string, string>();
        }

        public override bool tick() {
            // assign input values from input dictionary
            bool?[] inputValues = new bool?[inputs.Count];
            {
                int i = 0;
                foreach (KeyValuePair<string, Port> kvp in inputs) {
                    inputValues[i] = kvp.Value.Value;
                    i++;
                }
            }
            bool?[] newOutputValues = compute(inputValues);
            bool changed = !outputs.Equals(newOutputValues);
            // assign new output values to output dictionary
            {
                int i = 0;
                foreach (KeyValuePair<string, Port> kvp in outputs) {
                    if (i >= newOutputValues.Length) {
                        break;
                    }
                    kvp.Value.Value = newOutputValues[i];
                    i++;
                }
            }
            return changed;
        }

        public override Gate clone() {
            // TODO
            SimpleGate newGate = new SimpleGate(base);
            foreach (KeyValuePair<string, string> kvp in transitionTable) {
                newGate.transitionTable.Add(kvp.Key, kvp.Value);
            }
            return newGate;
        }

        // Create a simple gate prototype from string representation
        public static SimpleGate parseSimpleGate(StreamReader inputStream) {
            // TODO
            return null;
        }

        // Compute new output values based on input values
        // directly from transition table or default rules.
        bool?[] compute(bool?[] inputValues) {
            // TODO:
            string input = TristateBool.arrayToString(inputValues);
            bool?[] output = new bool?[outputs.Count];
            if (transitionTable.ContainsKey(input)) {
                // output values according to transition table
                return TristateBool.arrayFromString(transitionTable[input]);
            } else {
                // default output values
                bool? outputvalue;
                if (input.Contains("?")) {
                    // set all outputs to ?
                    outputvalue = null;
                } else {
                    // set all outputs to 0
                    outputvalue = false;
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
        Dictionary<string, Gate> gates; // name, Gate
        // Connections between inner gates' (or this gate's) ports.
        // In fact, data flow in direction: src->dest.
        // They are stored in reverse order in dictionary, because we will
        // usually query by destination.
        Dictionary<string, string> connections; // dest, src

        protected AbstractCompositeGate() {
            initialize();
        }

        protected AbstractCompositeGate(AbstractCompositeGate other) {
            initialize();
            foreach (KeyValuePair<string, string> kvp in other.connections) {
                connections.Add(kvp.Key, kvp.Value);
            }
        }

        private void initialize() {
            connections = new Dictionary<string, string>();
        }

        public override bool tick() {
            // TODO:
            // - transmit values to inner gates' inputs from ports which point to them
            // - for all inner gates: tick()
            // - transmit values from inner gates' outputs to ports where they point to
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
                return null;
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
                connections[dest] = src;
            } else {
                // error
            }
        }

        // Get Port by address, eg.: [gate.]port
        // If gate is not specified, currect gate is meant
        protected Port getPortByAddress(string address) {
            string[] parts = address.Split('.');
            if (parts.Length == 1) {
                return getPort(parts[0]);
            } else if (parts.Length == 2) {
                Gate gate = getGate(parts[0]);
                if (gate != null) {
                    return gate.getPort(parts[1]);
                } else {
                    return null;
                }
            }
            return null;
        }
    }

    class CompositeGate : AbstractCompositeGate {
        protected CompositeGate(AbstractCompositeGate g) : base(g) {
        }

        public override Gate clone() {
            return new CompositeGate(base);
        }

        // Create a composite gate prototype from string representation
        // NOTE: A common parsing code is in parseAbstractCompositeGate().
        public static CompositeGate parseCompositeGate(StreamReader inputStream) {
            // TODO
            return null;
        }
    }

    class Network : AbstractCompositeGate {
        protected Network(AbstractCompositeGate g)
            : base(g) {
        }

        public override Gate clone() {
            return new Network(base);
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
            // TODO: set inputs according to inputValues
            for (int ticks = 0; ticks < MAX_TICKS; ticks++) {
                if (tick()) break;
            }
            // TODO: return ("{0} {1}", ticks, outputs)
            return null;
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

    class SyntaxErrorException : ApplicationException { }

    class Program
    {
        static void Main(string[] args)     {
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
            }
        }
    }
}
