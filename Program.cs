using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace LogicNetwork
{

    abstract class Gate {
        Dictionary<string, bool?> inputs; // input ports
        Dictionary<string, bool?> outputs; // output ports

        public bool? getPortValue(string portName);
        public void setPortValue(string portName, bool? value);

        // Make one computing step and change outputs somehow
        // Return true if the gate and possibly all inner gates
        // have stabilized, ie. output values haven't changed in the tick
        public abstract bool tick();

        // cloning support for Prototype pattern
        public abstract void clone();

        // Used when defining a gate prototype:

        // Add new input port, set default value
        void addInputPort(string portName);

        // Add new output port, set default value
        void addOutputPort(string portName);
    }

    class SimpleGate : Gate {
        // Transition table
        // - key: values of inputs (eg.: 1 0 ? 1 0 1)
        // - value: values of outputs (eg.: 1 0 1)
        // - Dictionary<string, string> might be an overhead
        Dictionary<string, string> transitionTable;

        public override bool tick();

        public override void clone();

        // Create a simple gate prototype from string representation
        public static SimpleGate parseSimpleGate(StreamReader inputStream);

        // Compute new output values based on input values
        // directly from transition table or default rules.
        bool?[] compute(bool?[] inputValues);
        string compute(string inputValues);
    }

    class AbstractCompositeGate : Gate {
        // Inner gates
        Dictionary<string, Gate> gates; // name, Gate
        // Connections between inner gates' (or this gate's) ports.
        // In fact, data flow in direction: src->dest.
        // They are stored in reverse order in dictionary, because we will
        // usually query by destination.
        Dictionary<string, string> connections; // dest, src

        public override bool tick();

        public override void clone();

        // Create an abstract composite gate prototype from string representation
        // This is a common code for its descentants not to be called directly.
        // Specific details should be separated into virutal methods.
        AbstractCompositeGate parseAbstractCompositeGate(StreamReader inputStream);

        // Transmit a value from source [gate.]port to destination [gate.]port
        void transmit(string src, string dest);
        // or: void transmit(string srcGate, string srcPort, string destGate, string destPort);

        // Add an inner gate
        // - check if gate is not a Network
        void addGate(Gate gate);

        // Connect two ports
        void connect(string src, string dest);
    }

    class CompositeGate : AbstractCompositeGate {
        // Create a composite gate prototype from string representation
        // NOTE: A common parsing code is in parseAbstractCompositeGate().
        public static CompositeGate parseCompositeGate(StreamReader inputStream);
    }

    class Network : AbstractCompositeGate {
        // Create a network prototype from string representation
        public static Network parseNetwork(StreamReader inputStream);

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
        public void parseGates(StreamReader inputStream);
        
        // Create a clone of a defined gate prototype
        public Gate createGate(string gateName);

        // Get the network
        public Network createNetwork();

        // Register a new gate prototype
        void defineGate(string gateName, Gate gate);

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

                } catch (FileNotFoundException ex) {
                    Console.WriteLine("File not found: {0}", args[0]);
                    return;
                } finally {
                    if (fs != null) {
                        fs.Close();
                    }
                }
                
                // create an instance of the network
                Network network = gateFactory.createNetwork();
                
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
