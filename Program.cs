using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

// Logic gate network simulator
//
// Author: Bohumir Zamecnik <bohumir@zamecnik.org>
// Date: 2009/12
namespace LogicNetwork
{

    // TODO:
    // * AbstractCompositeGate.tick()
    //   * return information about stabilizing
    // * Network.evaluate()
    // * cloning
    // - write testing code
    // * write parsing from definition file
    // * errors -> exceptions or other handling
    //   - translate inner exceptions to other exceptions
    //   * how to find out the line number where syntax error occured?
    //   * count lines to know where a syntax exception occured
    //   - make exceptions know about the line numbers
    // - write more comments
    // * define implicit constant gates 0, 1 in the network
    //   - try to move the code to Network class
    // - ToString() should speak less
    // - make a check in parseConnection()
    // - fix isCorrecltyParsed()
    // * put parsing functions to nested (inner) classes
    //   * make a hierarchy
    //   * there would be current line number (of the stream)

    // Abstract base for all logic gates.
    // Part of Composite design pattern.
    abstract class Gate {

        // Port
        // Represents a port with a three-state logic value.
        // A wrapper class is only to simplify getting and setting the value.
        // It is an inner class, because there's no need to use it
        // from outside of Gate and its descendants.
        public class Port {
            // Port value
            bool? value; // three-state logic (true, false, null) = (1, 0, ?)
            
            public bool? Value {
                get { return this.value; }
                set { this.value = value; }
            }

            public Port() {
                value = null;
            }

            public Port(Port other) {
                if (other != null) {
                    value = other.value;
                }
            }

            public Port(bool? value) {
                this.value = value;
            }

            public override string ToString() {
                return TristateBool.toString(value);
            }
        }

        public class Parser {
            Gate gate;

            Gate ParsedGate {
                get { return gate; }
            }

            int currentLineNumber;

            // Number of lines read from input stream
            // in this parser functions invocations.
            public int Line {
                get { return currentLineNumber; }
                set { currentLineNumber = value; }
            }

            public Parser(Gate gate) {
                this.gate = gate;
                currentLineNumber = 0;
            }

            // Parse a line with port definitions (common code)
            protected void parsePorts(string definition, Dictionary<string, Port> ports) {
                string[] parts = definition.Trim().Split(' ');
                foreach (string part in parts) {
                    if (isValidIdentifier(part)) {
                        gate.addPort(part, ports);
                    }
                }
            }

            // Check identifier validity.
            // Identifier might be a name of a gate type, gate instance or port.
            // Return true if valid.
            protected static bool isValidIdentifier(string identifier) {
                char[] badCharacters = new char[] { ' ', '\t', '\n', '\r', '\f', '.', ';' };
                return (identifier.IndexOfAny(badCharacters) == -1)
                    && !identifier.Contains("->")
                    && !identifier.StartsWith("end");
            }

            // Read a line of useful information.
            // Skip lines ingored by some rules and count total lines read.
            // Return first line being not ignored or null if the stream finished.
            protected string readUsefulLine(StreamReader inputStream) {
                string line = "";
                while ((line = inputStream.ReadLine()) != null) {
                    Line++;
                    // ingore empty lines (containing possibly whitespace)
                    // or a comments (starting with ';')
                    line = line.Trim();
                    if ((line.Length != 0) && (line[0] != ';')) {
                        break;
                    }
                }
                return line;
            }
        }

        // Input and output ports
        // NOTE: Input and output ports share the space of their names
        // NOTE: The two must be cloned.
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

        // Make one computing step (or tick) and change outputs somehow.
        // Return true if the gate and all inner gates have
        // stabilized, ie. output values haven't changed during the tick.
        public abstract bool tick();

        // Get a port
        public Port getPort(string portName) {
            if (inputs.ContainsKey(portName)) {
                return inputs[portName];
            } else if (outputs.ContainsKey(portName)) {
                return outputs[portName];
            } else {
                throw new ArgumentException("Unknown port name");
            }
        }

        // Get values of a whole group of ports
        protected bool?[] getPortGroup(Dictionary<string, Port> ports) {
            bool?[] values = new bool?[ports.Count];
            int i = 0;
            foreach (KeyValuePair<string, Port> kvp in ports) {
                values[i] = kvp.Value.Value;
                i++;
            }
            return values;
        }

        // Set values of a whole group of ports
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

        // Get names of input ports
        public string[] getInputPortNames() {
            return getPortNames(inputs);
        }

        // Get names of output ports
        public string[] getOutputPortNames() {
            return getPortNames(outputs);
        }

        // Get names of ports from selected group
        private string[] getPortNames(Dictionary<string, Port> ports) {
            List<string> names = new List<string>();
            // NOTE: there might a problem that ports.Keys
            // does not guarantee the order of elements
            foreach (string key in ports.Keys) {
                names.Add(key);
            }
            return names.ToArray();
        }

        // Add a new port to given port group, set default value
        protected void addPort(string portName, Dictionary<string, Port> ports) {
            if (!ports.ContainsKey(portName)) {
                ports.Add(portName, new Port((bool?)null));
            } else {
                throw new GateInconsistenceException("Duplicate port definition.");
            }
        }
        
        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.Append("Gate {\n");
            sb.Append("inputs: [");
            foreach(KeyValuePair<string, Port> kvp in inputs) {
                sb.AppendFormat("{0}: {1}, ", kvp.Key, kvp.Value);
            }
            sb.AppendFormat("]\n");
            sb.Append("outputs: [");
            foreach (KeyValuePair<string, Port> kvp in outputs) {
                sb.AppendFormat("{0}: {1}, ", kvp.Key, kvp.Value);
            }
            sb.AppendFormat("]\n}}\n");
            return sb.ToString();
        }

    }

    class SimpleGate : Gate {
        new public class Parser : Gate.Parser
        {
            SimpleGate gate;

            SimpleGate ParsedGate {
                get { return gate; }
            }

            public Parser(SimpleGate gate)
                : base(gate) {
                this.gate = gate;
            }

            // Create a simple gate prototype from string representation.
            // Format:
            //   inputs <...> - once
            //   outputs <...> - once
            //   <transitions> - zero or more times
            //   end
            // Return: number of lines read
            public static int parseSimpleGate(
                StreamReader inputStream,
                out SimpleGate parsedGate)
            {
                SimpleGate gate = new SimpleGate();
                Parser parser = new Parser(gate);
                try {
                    string line = parser.readUsefulLine(inputStream);
                    if (line.StartsWith("inputs")) {
                        parser.parsePorts(line.Substring("inputs".Length), gate.inputs);
                    } else {
                        throw new SyntaxErrorException("Missing keyword (inputs).");
                    }

                    line = parser.readUsefulLine(inputStream);
                    if (line.StartsWith("outputs")) {
                        parser.parsePorts(line.Substring("outputs".Length), gate.outputs);
                    } else {
                        throw new SyntaxErrorException("Missing keyword (outputs).");
                    }

                    while (((line = parser.readUsefulLine(inputStream)) != null)) {
                        if (!line.StartsWith("end")) {
                            parser.parseTransitionFunction(line);
                        } else {
                            break;
                        }
                    }
                }
                catch (IOException) {
                    throw new SyntaxErrorException();
                }
                parsedGate = parser.ParsedGate;
                return parser.Line;
            }

            // Parse one line of transition function.
            // If correct add it to transition table.
            protected void parseTransitionFunction(string definition) {
                int inputsCount = gate.inputs.Count;
                int outputsCount = gate.outputs.Count;
                string[] parts = definition.Trim().Split(' ');
                if (parts.Length != (inputsCount + outputsCount)) {
                    throw new SyntaxErrorException("Transition: wrong number of values.");
                }
                string inputDef = String.Join(" ", parts, 0, inputsCount);
                string outputDef = String.Join(" ", parts, inputsCount, outputsCount);
                if (!TristateBool.isValidArray(inputDef) ||
                    !TristateBool.isValidArray(outputDef)) {
                    throw new SyntaxErrorException("Transition: invalid values.");
                }
                if (gate.transitionTable.ContainsKey(inputDef)) {
                    throw new SyntaxErrorException("Duplicate transition definition.");
                }
                gate.transitionTable.Add(inputDef, outputDef);
            }
        
        }

        // Transition table
        // - key: values of inputs (eg.: 1 0 ? 1 0 1)
        // - value: values of outputs (eg.: 1 0 1)
        // - Dictionary<string, string> might be an overhead
        //   but it is easy to work with
        Dictionary<string, string> transitionTable;

        public static SimpleGate TRUE_CONSTANT_GATE;
        public static SimpleGate FALSE_CONSTANT_GATE;

        static SimpleGate() {
            // NOTE: There might be a "problem" (not serious)
            // that propagating the value could take 1 tick (instad of 0).
            // Try to test it.
            // This is not a functional issue, but it could
            // result in behavior different from specification.
            TRUE_CONSTANT_GATE = new SimpleGate();
            TRUE_CONSTANT_GATE.addPort("o", TRUE_CONSTANT_GATE.outputs);
            TRUE_CONSTANT_GATE.getPort("o").Value = true;
            TRUE_CONSTANT_GATE.transitionTable.Add("", "1");

            FALSE_CONSTANT_GATE = new SimpleGate();
            FALSE_CONSTANT_GATE.addPort("o", FALSE_CONSTANT_GATE.outputs);
            FALSE_CONSTANT_GATE.getPort("o").Value = false;
            FALSE_CONSTANT_GATE.transitionTable.Add("", "0");
        }

        protected SimpleGate() {
            initialize();
        }

        protected SimpleGate(SimpleGate other) : base(other) {
            // Transition table could be shared (I hope).
            // NOTE: Call to initialize() is not needed, because we
            // will eventualy replace it with another object.
            transitionTable = other.transitionTable;
        }

        private void initialize() {
            transitionTable = new Dictionary<string, string>();
        }

        // Cloning support
        public override Gate clone() {
            return new SimpleGate(this);
        }

        public override bool tick() {
            // assign input values from input dictionary
            bool?[] inputValues = getPortGroup(inputs);
            bool?[] oldOutputValues = getPortGroup(outputs);
            // compute new values
            bool?[] newOutputValues = compute(inputValues);
            bool stabilized = true;
            for (int i = 0; i < oldOutputValues.Length; i++) {
                if (oldOutputValues[i] != newOutputValues[i]) {
                    stabilized = false;
                    break;
                }
            }
            // assign new output values to output dictionary
            setPortGroup(newOutputValues, outputs);
            return stabilized;
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

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.Append("SimpleGate {\n");
            sb.Append(base.ToString());
            sb.Append("transition table: [");
            foreach (KeyValuePair<string, string> kvp in transitionTable) {
                sb.AppendFormat("{0}: {1}, ", kvp.Key, kvp.Value);
            }
            sb.AppendFormat("]\n}}\n");
            return sb.ToString();
        }
    }

    abstract class AbstractCompositeGate : Gate {
        public new abstract class Parser : Gate.Parser
        {
            AbstractCompositeGate gate;

            AbstractCompositeGate ParsedGate {
                get { return gate; }
            }

            public Parser(AbstractCompositeGate gate)
                : base(gate) {
                this.gate = gate;
            }

            // Create an abstract composite gate prototype from string representation
            // This is a common code for its descentants, not to be called directly
            // but rather by parsing funtions of its descendants.
            // Specific rules are separated into virutal functions.
            // Template Method design pattern.
            // Format:
            //   inputs <...> - once
            //   outputs <...> - once
            //   gate <...> - at least once
            //   <connections> - zero or more times
            //   end
            protected void parseAbstractCompositeGate(StreamReader inputStream) {
                try {
                    // input ports (once)
                    string line = readUsefulLine(inputStream);
                    if (!line.StartsWith("inputs")) {
                        throw new SyntaxErrorException("Missing keyword (inputs).");
                    }
                    parsePorts(line.Substring("inputs".Length), gate.inputs);

                    // output ports (one)
                    line = readUsefulLine(inputStream);
                    if (!line.StartsWith("outputs")) {
                        throw new SyntaxErrorException("Missing keyword (outputs).");
                    }
                    parsePorts(line.Substring("outputs".Length), gate.outputs);

                    // inner gates (one or more)
                    while (((line = readUsefulLine(inputStream)) != null)) {
                        if (!line.StartsWith("gate")) {
                            break;
                        }
                        parseInnerGate(line.Substring("gate".Length));
                    }

                    // connections between inner gates (zero or more)
                    do {
                        if (line.StartsWith("end")) {
                            break;
                        }
                        parseConnection(line);
                    } while (((line = readUsefulLine(inputStream)) != null));
                }
                catch (IOException) {
                    throw new SyntaxErrorException();
                }
                // descentant specific rules apply here
                if (!isCorrecltyParsed()) {
                    throw new SyntaxErrorException("Binding rule broken.");
                }
            }

            // A hook for parseAbstractCompositeGate() with
            // class specific details and rules
            protected abstract bool isCorrecltyParsed();

            // Parse a line of inner gate definition.
            // Format:
            //   <gate instance name> <gate type>
            protected void parseInnerGate(string definition) {
                string[] parts = definition.Trim().Split(' ');
                if ((parts.Length != 2) || !isValidIdentifier(parts[0])) {
                    throw new SyntaxErrorException();
                }
                GatePrototypeFactory factory = GatePrototypeFactory.getInstance();
                Gate innerGate = factory.createGate(parts[1]);
                if (innerGate == null) {
                    throw new GateInconsistenceException(String.Format(
                        "Gate of type '{0}' was not defined yet.", parts[1]));
                }
                gate.addGate(parts[0], innerGate);
            }

            // Parse a line defining a connection between two inner gates
            // or and inner gate and this gate's port.
            // Possible variants:
            //   gate1.i->gate2.o
            //   gate.i->i
            //   o->gate.o
            protected void parseConnection(string definition) {
                string[] parts = definition.Trim().Split(new string[] { "->" },
                    StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length != 2) {
                    throw new SyntaxErrorException("Invalid gate connection format");
                }
                // TODO: check if it is ok to connect the two ports
                // else -> Binding rule broken
                gate.connect(parts[1], parts[0]);
            }
            
        }

        // Inner gates
        // NOTE: this must be CLONED
        protected Dictionary<string, Gate> gates; // name, Gate

        // Connections between inner gates' (or this gate's) ports.
        // form: port adress->port address 
        // port address:[gate.]port
        // In fact, data flow in direction: src->dest.
        // They are stored in both orders in two dictionary,
        // because we will query by both directions.
        protected Dictionary<string, string> connections; // dest, src
        protected Dictionary<string, List<string>> reverseConnections; // src, list of dests

        protected AbstractCompositeGate() {
            initialize();
        }

        // Copy constructor
        protected AbstractCompositeGate(AbstractCompositeGate other) : base(other) {
            initialize(); // TODO: it is only needed to initialize gates
            // inner gates must be cloned
            foreach (KeyValuePair<string, Gate> kvp in other.gates) {
                gates.Add(kvp.Key, kvp.Value.clone());
            }
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
            bool stabilized = true;
            bool?[] oldOutputValues = getPortGroup(outputs);

            // transmit values to inner gates' inputs from ports which point to them
            foreach (KeyValuePair<string, Gate> kvp in gates) {
                string destGateName = kvp.Key;
                // get names of all input ports of the gate -> dest
                foreach (string destPortName in kvp.Value.getInputPortNames()) {
                    string dest = destGateName + '.' + destPortName;
                    // find which ports point to them (one at time) -> src
                    if (connections.ContainsKey(dest)) {
                        string src = connections[dest];
                        transmit(src, dest);
                    }
                }
            }

            // for all inner gates: tick()
            foreach (KeyValuePair<string, Gate> kvp in gates) {
                stabilized &= kvp.Value.tick();
            }
            
            // transmit values from inner gates' outputs to ports where they point to
            foreach (KeyValuePair<string, Gate> gateKVP in gates) {
                string srcGateName = gateKVP.Key;
                // get names of all output ports of the gate -> src
                foreach (string srcPortName in gateKVP.Value.getOutputPortNames()) {
                    // find to which ports this one  points (multiple) -> dest
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
            // return true, if output values have not changed during tick()
            // ie. the gate and its subgates have stabilized
            if (!stabilized) {
                return false;
            } else {
                bool?[] newOutputValues = getPortGroup(outputs);
                for (int i = 0; i < oldOutputValues.Length; i++) {
                    if (oldOutputValues[i] != newOutputValues[i]) {
                        return false;
                    }
                }
            }
            return true;
        }

        // Connect two ports
        protected void connect(string src, string dest) {
            Port srcPort = getPortByAddress(src);
            Port destPort = getPortByAddress(dest);
            if ((srcPort == null) || (srcPort == null)) {
                // src or dest is not a valid port
                throw new SyntaxErrorException("Invalid port.");
            }
            if (connections.ContainsKey(dest)) {
                throw new GateInconsistenceException("Duplicate connection.");
            }
            // add a connection
            connections.Add(dest, src);
            // add a reverse connection
            if (!reverseConnections.ContainsKey(src)) {
                reverseConnections.Add(src, new List<string>());
            }
            reverseConnections[src].Add(dest);
        }

        // Transmit a value from source [gate.]port to destination [gate.]port
        protected void transmit(string src, string dest) {
            Port srcPort = getPortByAddress(src);
            Port destPort = getPortByAddress(dest);
            if ((srcPort == null) || (srcPort == null)) {
                throw new ArgumentException();
            }
            destPort.Value = srcPort.Value;
        }

        // Add an inner gate instance
        protected void addGate(string gateName, Gate gate) {
            if (gate == null) {
                throw new ArgumentException();
            }
            if (gate is Network) {
                throw new ArgumentException("Cannot add a network inside another gate.");
            }
            if (gates.ContainsKey(gateName)) {
                throw new GateInconsistenceException("Duplicate inner gate instantiation.");
            }
            gates.Add(gateName, gate);
        }

        // Get inner gate instance by name
        protected Gate getGate(string gateName) {
            if (!gates.ContainsKey(gateName)) {
                throw new ArgumentException("No such a gate.");
            }
            return gates[gateName];
        }

        // Get Port by address, eg.: [gate.]port
        // If gate is not specified, current gate is meant
        protected Port getPortByAddress(string address) {
            string[] parts = address.Split('.');
            if (parts.Length == 1) {
                // implicit constant gate
                // TODO: Its a bit ugly to put this rule here.
                // It should be in Network code.
                // There is a presumption, that there exist
                // gates named "0" and "1" with a port named "o".
                if (((parts[0] == "0") && gates.ContainsKey("0")) ||
                    ((parts[0] == "1") && gates.ContainsKey("1"))) {
                    return getGate(parts[0]).getPort("o");
                }
                // a port from this gate
                return getPort(parts[0]);
            } else if (parts.Length == 2) {
                Gate gate = getGate(parts[0]);
                if (gate == null) {
                    throw new ArgumentException("No such an inner gate.");
                }
                // a port from an inner gate
                return gate.getPort(parts[1]);
            }
            throw new SyntaxErrorException("Invalid format of a port address.");
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.Append("AbstractCompositeGate {\n");
            sb.Append("gates: [\n");
            foreach (KeyValuePair<string, Gate> kvp in gates) {
                sb.AppendFormat("{0}: {1}", kvp.Key, kvp.Value);
            }
            sb.AppendFormat("]\n");
            sb.Append("connections: [");
            foreach (KeyValuePair<string, string> kvp in connections) {
                sb.AppendFormat("{0}->{1}, ", kvp.Key, kvp.Value);
            }
            sb.AppendFormat("]\n");
            sb.Append("reverse connections: [");
            foreach (KeyValuePair<string, List<string>> kvp in reverseConnections) {
                sb.AppendFormat("{0}->[", kvp.Key);
                foreach (string conn in kvp.Value) {
                    sb.AppendFormat("{0}, ", conn);
                }
                sb.AppendFormat("], ");
            }
            sb.AppendFormat("]\n}}\n");
            return sb.ToString();
        }
    }

    class CompositeGate : AbstractCompositeGate {
        public new class Parser : AbstractCompositeGate.Parser
        {
            CompositeGate gate;

            CompositeGate ParsedGate {
                get { return gate; }
            }

            public Parser(CompositeGate gate)
                : base(gate) {
                this.gate = gate;
            }


            // Create a composite gate prototype from string representation
            // NOTE: A common parsing code is in parseAbstractCompositeGate().
            // Return: number of lines read
            public static int parseCompositeGate(
                StreamReader inputStream,
                out CompositeGate parsedGate)
            {
                CompositeGate newGate = new CompositeGate();
                Parser parser = new Parser(newGate);
                parser.parseAbstractCompositeGate(inputStream);
                parsedGate = parser.ParsedGate;
                return parser.Line;
            }

            // A hook for parseAbstractCompositeGate() with
            // class specific details and rules
            protected override bool isCorrecltyParsed() {
                // nothing special for CompositeGate here
                return true;
            }
        }

        protected CompositeGate() { }
        
        protected CompositeGate(CompositeGate other) : base(other) { }

        public override Gate clone() {
            return new CompositeGate(this);
        }

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            sb.Append("CompositeGate {\n");
            sb.Append(base.ToString());
            sb.AppendFormat("}}\n");
            return sb.ToString();
        }
    }

    class Network : AbstractCompositeGate {
        public new class Parser : AbstractCompositeGate.Parser
        {
            Network gate;

            Network ParsedGate {
                get { return gate; }
            }

            public Parser(Network gate)
                : base(gate) {
                this.gate = gate;
            }

            // Create a network prototype from string representation
            // Return: number of lines read
            public static int parseNetwork(
                StreamReader inputStream,
                out Network parsedGate)
            {
                Network newGate = new Network();
                Parser parser = new Parser(newGate);
                // There are two implicit constant gates
                newGate.addGate("0", SimpleGate.FALSE_CONSTANT_GATE);
                newGate.addGate("1", SimpleGate.TRUE_CONSTANT_GATE);
                parser.parseAbstractCompositeGate(inputStream);
                parsedGate = parser.ParsedGate;
                return parser.Line;
            }

            // A hook for parseAbstractCompositeGate() with
            // class specific details and rules
            protected override bool isCorrecltyParsed() {
                // there is at least one input port and one output port
                if ((gate.inputs.Count <= 0) && (gate.outputs.Count <= 0)) {
                    return false;
                }

                // there is at least one inner gate
                // NOTE: there are two implicit gates ("0", "1")
                if (gate.gates.Count <= 2) {
                    return false;
                }

                // every input port is connected to at least
                // one inner gate input port
                string[] inputPortNames = gate.getInputPortNames();
                foreach (string portName in inputPortNames) {
                    if (!gate.reverseConnections.ContainsKey(portName)) {
                        return false; // not connected
                    }
                    List<string> connectedPorts = gate.reverseConnections[portName];
                    if (connectedPorts == null) {
                        throw new GateInconsistenceException();
                    }
                    if (connectedPorts.Count <= 0) {
                        return false; // not connected
                    }
                    // TODO: check if it is connected to al least one
                    // _inner gate_ input port (not just this gate output ports)
                }
                return true;
            }
        }

        protected Network() { }

        protected Network(Network other) : base(other) { }

        public override Gate clone() {
            return new Network(this);
        }

        // Maximum number of ticks before we decide the network can't stabilize.
        // This might be useful when the network has periodic or chaotic behavior.
        const int MAX_TICKS = 1000000;

        // Let the network compute
        // inputValues: <space separated input values>
        // return: <ticks> <space separated output values>
        public string evaluate(string inputValues) {
            // set inputs according to inputValues
            bool?[] inputsArray = TristateBool.arrayFromString(inputValues);
            if (inputsArray.Length != inputs.Count) {
                throw new SyntaxErrorException("Wrong number of values.");
            }
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

        public override string ToString() {
            StringBuilder sb = new StringBuilder();
            // TODO: join these line to one AppendFormat() call
            sb.Append("Network {\n");
            sb.Append(base.ToString());
            sb.AppendFormat("}}\n");
            return sb.ToString();
        }
    }

    class GatePrototypeFactory {
        // Singleton design pattern

        // Singleton instance
        private static GatePrototypeFactory instance = null;

        private GatePrototypeFactory() {
            gates = new Dictionary<string, Gate>();
            network = null;
        }

        // Get singleton instance
        public static GatePrototypeFactory getInstance() {
            if (instance == null) {
                instance = new GatePrototypeFactory();
            }
            return instance;
        }

        // Defined gate prototypes
        Dictionary<string, Gate> gates; // gateName, gate

        // The logic gate network (there's only one)
        Network network;

        // The network as a read-only property
        public Network Network {
            get { return network; }
        }

        // Parse a file (stream) with definitions
        // of all the gates and a network
        public void parseGates(StreamReader inputStream) {
            string line = null;
            int linesRead = 0;
            while ((line = inputStream.ReadLine()) != null) {
                linesRead++;
                // NOTE: Rules which lines to ignore are already
                // in protected SimpleGate.skipIgnoredLine().
                
                // ingore empty lines (containing possibly whitespace)
                // or a comments (starting with ';')
                line = line.Trim();
                if ((line.Length == 0)
                    || (line[0] == ';')) {
                    continue;
                }

                string[] parts = line.Split(' ');
                if (parts[0].Equals("gate")) {
                    if (parts.Length != 2) {
                        throw new SyntaxErrorException();
                    }
                    SimpleGate gate;
                    linesRead += SimpleGate.Parser.parseSimpleGate(inputStream, out gate);
                    defineGate(parts[1], gate);
                } else if (parts[0].Equals("composite")) {
                    if (parts.Length != 2) {
                        throw new SyntaxErrorException();
                    }
                    CompositeGate gate;
                    linesRead += CompositeGate.Parser.parseCompositeGate(inputStream, out gate);
                    defineGate(parts[1], gate);
                } else if (parts[0].Equals("network")) {
                    Network gate;
                    linesRead += Network.Parser.parseNetwork(inputStream, out gate);
                    defineGate("", gate);
                } else {
                    throw new SyntaxErrorException();
                }
            }
            if (network == null) {
                throw new GateInconsistenceException("No network defined.");
            }
        }

        // Register a new gate prototype
        void defineGate(string gateName, Gate gate) {
            if (gate == null) {
                throw new ArgumentException("Gate is null.");
            }
            if (gate is Network) {
                if (network != null) {
                    throw new GateInconsistenceException("Defining more than one network.");
                }
                network = (Network)gate;
            } else {
                if (gates.ContainsKey(gateName)) {
                    throw new GateInconsistenceException("Duplicate gate definition.");
                }
                gates.Add(gateName, gate);
            }
        }

        // Create a clone of a defined gate prototype
        public Gate createGate(string gateName) {
            if (!gates.ContainsKey(gateName)) {
                throw new ArgumentException("No such a gate.");
            }
            return gates[gateName].clone();
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
            } else if (str.Equals("0")) {
                return false;
            } else if (str.Equals("?")) {
                return null;
            } else {
                throw new ArgumentException();
            }
        }

        public static string arrayToString(bool?[] array) {
            StringBuilder sb = new StringBuilder();
            foreach (bool? value in array) {
                sb.Append(TristateBool.toString(value) + ' ');
            }
            return sb.ToString().TrimEnd();
        }

        public static bool?[] arrayFromString(string str) {
            string[] parts = str.Trim().Split(' ');
            List<bool?> values = new List<bool?>();
            foreach (string part in parts) {
                if (part.Length > 0) {
                    values.Add(TristateBool.fromString(part));
                }
            }
            return values.ToArray();
        }

        public static bool isValidArray(string str) {
            return str.Equals(arrayToString(arrayFromString(str)));
        }
    }

    class SyntaxErrorException : ApplicationException {
        public SyntaxErrorException() {}

        public SyntaxErrorException(string message) : base(message) { }
    }

    class GateInconsistenceException : ApplicationException {
        public GateInconsistenceException() {}
        
        public GateInconsistenceException(string message) : base(message) { }
    }

    class Program
    {
        static void Main(string[] args) {
            //Test.run();
            //return;

            if (args.Length != 1) {
                // no file specified
                Console.WriteLine("Usage: LogicNetwork.exe definition_file.txt");
            }

            GatePrototypeFactory gateFactory = GatePrototypeFactory.getInstance();

            FileStream fs = null;
            try {
                fs = new FileStream(args[0], FileMode.Open, FileAccess.Read);
                StreamReader reader = new StreamReader(fs);
                
                // Parse the config file.
                // Fill the GatePrototypeFactory with gate prototypes.

                try {
                    gateFactory.parseGates(reader);
                }
                catch (SyntaxErrorException ex) {
                    Console.WriteLine("Syntax error: {0}", ex.Message);
                    return;
                }
                catch (GateInconsistenceException ex) {
                    Console.WriteLine("Gate inconsistence: {0}", ex.Message);
                    return;
                }
                catch (ArgumentException ex) {
                    Console.WriteLine("Invalid argument: {0}", ex.Message);
                    return;
                }
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
                } else if (line.Equals("print network")) {
                    Console.WriteLine(network); //DEBUG
                } else {
                    try {
                        Console.WriteLine(network.evaluate(line));
                    }
                    catch (SyntaxErrorException ex) {
                        Console.WriteLine("Syntax error: {0}", ex.Message);
                        continue;
                    }
                    catch (ArgumentException ex) {
                        Console.WriteLine("Invalid argument: {0}", ex.Message);
                        continue;
                    }
                }
            }
        }
    }
}
