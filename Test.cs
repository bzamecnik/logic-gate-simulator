using System;
using System.Collections.Generic;
using System.Text;

namespace LogicNetwork
{
    class Test
    {
        public static void testTristateBool() {
            Console.WriteLine("-- test TristateBool --");
            Console.WriteLine("toString({0}) = {1}", true, TristateBool.toString(true));
            Console.WriteLine("toString({0}) = {1}", false, TristateBool.toString(false));
            Console.WriteLine("toString({0}) = {1}", null, TristateBool.toString(null));

            Console.WriteLine("fromString({0}) = {1}", "1", TristateBool.fromString("1"));
            Console.WriteLine("fromString({0}) = {1}", "0", TristateBool.fromString("0"));
            Console.WriteLine("fromString({0}) = {1}", "?", TristateBool.fromString("?"));
            
            string valuesStr = " 0 1 1 0 ? 0 ? 1 ";
            Console.WriteLine("arrayToString(arrayFromString({0})) = {1}", valuesStr,
                TristateBool.arrayToString(TristateBool.arrayFromString(valuesStr)));

            valuesStr = " abc 1 def   / Ť\t\t0 ' 0 ? 1 ";
            Console.WriteLine("arrayToString(arrayFromString({0})) = {1}", valuesStr,
                TristateBool.arrayToString(TristateBool.arrayFromString(valuesStr)));
        }

        public static void testSimpleGate() {
            Console.WriteLine("-- test SimpleGate --");
            SimpleGate gate = SimpleGate.parseSimpleGate(null);
            Console.WriteLine("example gate: " + gate);

            Console.WriteLine("getInputPortNames() = {0}", String.Join(", ", gate.getInputPortNames()));
            Console.WriteLine("getOutputPortNames() = {0}", String.Join(", ", gate.getOutputPortNames()));

            Gate.Port port = gate.getPort("i1");
            Console.WriteLine("getPort(): {0}", port);
            port.Value = false;
            Console.WriteLine("port = 0; getPort(): {0}", gate.getPort("i1"));
            //port.Value = true;
            //Console.WriteLine("port = 1; getPort(): {0}", gate.getPort("i1"));

            Console.WriteLine("gate: " + gate);

            gate.tick();
            Console.WriteLine("tick()");

            Console.WriteLine("gate: " + gate);

            SimpleGate gate2 = (SimpleGate)gate.clone();

            Console.WriteLine("gate.clone(): " + gate2);

            Console.WriteLine("gate1 port changed");
            port.Value = null;
            Console.WriteLine("gate1: " + gate);
            Console.WriteLine("gate2: " + gate2);
        }

        public static void run() {
            Console.WriteLine("-- Logic Network Simulator - Test Suite --");
            //testTristateBool();
            testSimpleGate();
        }
    }
}
