using System;
using System.Collections;
using System.Collections.Generic;

namespace SharpCircuit {
	
	public class VoltageInput : Voltage {

        // Faulty added by CONTRELEC 26/09/2017
        private double faultyVoltage = 0.12;
        private bool _isFaulty;
        public bool isFaulty
        {
            get
            {
                return _isFaulty;
            }
            set
            {
                if (value)
                {
                    if (_isFaulty) return;                    // already set
                    _isFaulty = true;
                }
                else
                {
                    _isFaulty = false;
                }
            }
        }

        public Circuit.Lead leadPos { get { return lead0; } }

		public VoltageInput() : base(WaveType.DC) {

		}

		public VoltageInput(WaveType wf) : base(wf) {

		}

		public override int getLeadCount() {
			return 1;
		}

		public override double getVoltageDelta() {
			return lead_volt[0];
		}

		public override void stamp(Circuit sim) {
			if (waveform == WaveType.DC) {
				sim.stampVoltageSource(0, lead_node[0], voltSource, (_isFaulty ? faultyVoltage : getVoltage(sim)));
			} else {
				sim.stampVoltageSource(0, lead_node[0], voltSource);
			}
		}

		public override void step(Circuit sim) {
			if (waveform != WaveType.DC)
				sim.updateVoltageSource(0, lead_node[0], voltSource, (_isFaulty ? faultyVoltage : getVoltage(sim)));
		}

		public override bool leadIsGround(int n1) {
			return true;
		}

	}
}