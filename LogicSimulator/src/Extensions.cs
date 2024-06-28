using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharpCircuit
{
    public class NetNode
    {
        public int index { get; set; }
        public string node { get; set; }
        public bool connected { get; set; }
        public string location { get; set; }
        public string term { get; set; }
        public NetElement parent { get; set; }

        public NetNode() : this(null, 0, "NONODE", false, "NOTAG", "NOTAG") { }
        public NetNode(NetElement p, int idx, string n, bool c, string l, string t)
        {
            index = idx;
            node = n;         
            connected = false;
            location = l;
            term = t;
            parent = p;
        }
    }
    public class NetElement
    {
        public string location { get; set; }
        public string tag { get; set; }
        public string nettag { get; set; }
        public string name { get; set; }
        public char type { get; set; }
        public List<NetNode> nodes { get; set; }
        public double value { get; set; }
        public CircuitElement simElement { get; set; }
        public bool? lastState { get; set; }
        public double? lastValue { get; set; }
        public string sheet { get; set; }
        public string sheetno { get; set; }

        public NetElement()
        {
            lastState = null;
            lastValue = null;
        }
    }
}