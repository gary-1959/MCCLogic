namespace SharpCircuit
{
    partial class CircuitPanel2
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.circuitPoint1 = new SharpCircuit.CircuitPoint();
            this.circuitPoint2 = new SharpCircuit.CircuitPoint();
            this.circuitPoint3 = new SharpCircuit.CircuitPoint();
            this.circuitPoint4 = new SharpCircuit.CircuitPoint();
            this.voltage = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // circuitPoint1
            // 
            this.circuitPoint1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.circuitPoint1.Location = new System.Drawing.Point(50, 30);
            this.circuitPoint1.Name = "circuitPoint1";
            this.circuitPoint1.Size = new System.Drawing.Size(99, 33);
            this.circuitPoint1.TabIndex = 0;
            this.circuitPoint1.Click += new System.EventHandler(this.circuitPoint_Click);
            // 
            // circuitPoint2
            // 
            this.circuitPoint2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.circuitPoint2.Location = new System.Drawing.Point(50, 69);
            this.circuitPoint2.Name = "circuitPoint2";
            this.circuitPoint2.Size = new System.Drawing.Size(99, 33);
            this.circuitPoint2.TabIndex = 1;
            // 
            // circuitPoint3
            // 
            this.circuitPoint3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.circuitPoint3.Location = new System.Drawing.Point(50, 108);
            this.circuitPoint3.Name = "circuitPoint3";
            this.circuitPoint3.Size = new System.Drawing.Size(99, 33);
            this.circuitPoint3.TabIndex = 2;
            // 
            // circuitPoint4
            // 
            this.circuitPoint4.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.circuitPoint4.Location = new System.Drawing.Point(50, 147);
            this.circuitPoint4.Name = "circuitPoint4";
            this.circuitPoint4.Size = new System.Drawing.Size(99, 33);
            this.circuitPoint4.TabIndex = 3;
            // 
            // voltage
            // 
            this.voltage.Location = new System.Drawing.Point(357, 281);
            this.voltage.Name = "voltage";
            this.voltage.Size = new System.Drawing.Size(100, 22);
            this.voltage.TabIndex = 4;
            // 
            // CircuitPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.voltage);
            this.Controls.Add(this.circuitPoint4);
            this.Controls.Add(this.circuitPoint3);
            this.Controls.Add(this.circuitPoint2);
            this.Controls.Add(this.circuitPoint1);
            this.Name = "CircuitPanel";
            this.Size = new System.Drawing.Size(529, 317);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private CircuitPoint circuitPoint1;
        private CircuitPoint circuitPoint2;
        private CircuitPoint circuitPoint3;
        private CircuitPoint circuitPoint4;
        private System.Windows.Forms.TextBox voltage;
    }
}
