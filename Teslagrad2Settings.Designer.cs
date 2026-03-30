namespace LiveSplit.Teslagrad2
{
    partial class Teslagrad2Settings
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.pnlTop = new System.Windows.Forms.Panel();
            this.chkAutoReset = new System.Windows.Forms.CheckBox();
            this.lblNote = new System.Windows.Forms.Label();
            this.pnlRows = new System.Windows.Forms.Panel();
            this.pnlTop.SuspendLayout();
            this.SuspendLayout();

            // pnlTop
            this.pnlTop.Location = new System.Drawing.Point(0, 0);
            this.pnlTop.Size = new System.Drawing.Size(415, 30);
            this.pnlTop.Controls.Add(this.chkAutoReset);
            this.pnlTop.Controls.Add(this.lblNote);

            // chkAutoReset
            this.chkAutoReset.AutoSize = true;
            this.chkAutoReset.Checked = true;
            this.chkAutoReset.Location = new System.Drawing.Point(10, 6);
            this.chkAutoReset.Name = "chkAutoReset";
            this.chkAutoReset.Text = "Auto Reset";

            // lblNote
            this.lblNote.AutoSize = true;
            this.lblNote.Location = new System.Drawing.Point(100, 7);
            this.lblNote.Name = "lblNote";
            this.lblNote.ForeColor = System.Drawing.Color.Gray;
            this.lblNote.Text = "Variable: Teslagrad2_Version";

            // pnlRows
            this.pnlRows.Location = new System.Drawing.Point(0, 30);
            this.pnlRows.Size = new System.Drawing.Size(415, 280);
            this.pnlRows.AutoScroll = true;
            this.pnlRows.Name = "pnlRows";

            // Teslagrad2Settings
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.pnlTop);
            this.Controls.Add(this.pnlRows);
            this.Name = "Teslagrad2Settings";
            this.Size = new System.Drawing.Size(415, 310);
            this.pnlTop.ResumeLayout(false);
            this.pnlTop.PerformLayout();
            this.ResumeLayout(false);
        }

        private System.Windows.Forms.Panel pnlTop;
        private System.Windows.Forms.CheckBox chkAutoReset;
        private System.Windows.Forms.Label lblNote;
        private System.Windows.Forms.Panel pnlRows;
    }
}
