using System;
using System.Collections;
using System.Collections.Generic;

namespace SharpCircuit {

	public class Resistor : CircuitElement {

        // CONTRELEC 07-05-2017
        // Added isShortCircuit and isOpenCircuit feature

		public Circuit.Lead leadIn { get { return lead0; } }
		public Circuit.Lead leadOut { get { return lead1; } }

        private bool _isOpenCircuit;
        public bool isOpenCircuit
        {
            get
            {
                return _isOpenCircuit;
            }
            set
            {
                if (value)
                {
                    if (_isOpenCircuit) return;                     // already set
                    if (_isShortCircuit) isShortCircuit = false;    // undo this first
                    _isOpenCircuit = true;
                }
                else
                {
                    _isOpenCircuit = false;
                }
            }
        }

        private bool _isShortCircuit;
        public bool isShortCircuit
        {
            get
            {
                return _isShortCircuit;
            }
            set
            {
                if (value)
                {
                    if (_isShortCircuit) return;                    // already set
                    if (_isOpenCircuit) isOpenCircuit = false;      // undo this first
                    _isShortCircuit = true;
                }
                else
                {
                    _isShortCircuit = false;
                }
            }
        }

        /// <summary>
        /// Resistance (ohms)
        /// </summary>
        public double resistance { get; set; }

		public Resistor() : base() {
			resistance = 100;
		}

		public Resistor(double r) : base() {
			resistance = r;
		}

		public override void calculateCurrent() {
            if (_isOpenCircuit)
            {
                current = (lead_volt[0] - lead_volt[1]) / 10E6;
            }
            else if (_isShortCircuit)
            {
                current = (lead_volt[0] - lead_volt[1]) / 0.01;
            }
            else
            {
                current = (lead_volt[0] - lead_volt[1]) / resistance;
            }
			
		}

		public override void stamp(Circuit sim) {
            if (_isOpenCircuit)
            {
                sim.stampResistor(lead_node[0], lead_node[1], 10E6);
            }
            else if (_isShortCircuit)
            {
                sim.stampResistor(lead_node[0], lead_node[1], 0.01);
            }
            else
            {
                sim.stampResistor(lead_node[0], lead_node[1], resistance);
            }
			
		}

		/*public override void getInfo(String[] arr) {
			arr[0] = "resistor";
			getBasicInfo(arr);
			arr[3] = "R = " + getUnitText(resistance, Circuit.ohmString);
			arr[4] = "P = " + getUnitText(getPower(), "W");
		}*/

	}
}