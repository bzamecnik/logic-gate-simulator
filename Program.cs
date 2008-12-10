using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace LogicNetwork
{

    abstract class Gate {
        // NOTE: Input and output ports share the space of their names
        Dictionary<string, bool?> inputs; // input ports
        Dictionary<string, bool?> outputs; // output ports

        // Make one computing step and change outputs somehow
        // Return true if the gate and possibly all inner gates
        // have stabilized, ie. output values haven't changed in the tick
        public abstract bool tick();

        // Cloning support for the Prototype pattern
        public abstract Gate clone();

        // Get value of a port
        public bool? getPortValue(string portName) {
            if (inputs.ContainsKey(portName)) {
                return inputs[portName];
            } else if (outputs.ContainsKey(portName)) {
                return outputs[portName];
            } else {
                return null; // error, unknown port name
            }
        }

        // Set value of a port
        public void setPortValue(string portName, bool? value) {
            if (inputs.ContainsKey(portName)) {
                inputs[portName] = value;
            } else if (outputs.ContainsKey(portName)) {
                outputs[portName] = value;
            } else {
                // error, unknown port name
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
        private void addPort(string portName, Dictionary<string, bool?> ports) {
            if (!ports.ContainsKey(portName)) {
                ports.Add(portName, null);
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

        public override bool tick() {
            // TODO
            return false;
        }

        public override Gate clone() {
            // TODO
            return null;
        }

        // Create a simple gate prototype from string representation
        public static SimpleGate parseSimpleGate(StreamReader inputStream) {
            // TODO
            return null;
        }

        // Compute new output values based on input values
        // directly from transition table or default rules.
        bool?[] compute(bool?[] inputValues) {
            // TODO
            return null;
        }
        string compute(string inputValues) {
            // TODO
            return null;
        }
    }

    abstract class AbstractCompositeGate : Gate {
        // Inner gates
        Dictionary<string, Gate> gates; // name, Gate
        // Connections between inner gates' (or this gate's) ports.
        // In fact, data flow in direction: src->dest.
        // They are stored in reverse order in dictionary, because we will
        // usually query by destination.
        Dictionary<string, string> connections; // dest, src

        public override bool tick() {
            // TODO
            return false;
        }

        // Create an abstract composite gate prototype from string representation
        // This is a common code for its descentants not to be called directly.
        // Specific details should be separated into virutal methods.
        AbstractCompositeGate parseAbstractCompositeGate(StreamReader inputStream) {
            // TODO
            return null;
        }

        // Transmit a value from source [gate.]port to destination [gate.]port
        void transmit(string src, string dest) {
            // TODO
        }
        // or: void transmit(string srcGate, string srcPort, string destGate, string destPort);

        // Add an inner gate
        // - check if gate is not a Network
        void addGate(string gateName, Gate gate) {
            if ((gate != null) && !(gate is Network)) {
                gates.Add(gateName, gate);
            } else {
                // error
            }
        }

        // Connect two ports
        void connect(string src, string dest) {
            // TODO
        }
    }

    class CompositeGate : AbstractCompositeGate {
        // Create a composite gate prototype from string representation
        // NOTE: A common parsing code is in parseAbstractCompositeGate().
        public static CompositeGate parseCompositeGate(StreamReader inputStream) {
            // TODO
            return null;
        }

        public override Gate clone() {
            // TODO
            return null;
        }
    }

    class Network : AbstractCompositeGate {
        // Create a network prototype from string representation
        public static Network parseNetwork(StreamReader inputStream) {
            // TODO
            return null;
        }

        public override Gate clone() {
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
            // TODO
            return null;
        }

        // The network as a property
        public Network Network {
            get { return network; }
        }

        // Register a new gate prototype
        void defineGate(string gateName, Gate gate) {
            // TODO
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
                    
                    // parse the config file
                    // - fill the GatePrototypeFactory with gate prototypes

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
