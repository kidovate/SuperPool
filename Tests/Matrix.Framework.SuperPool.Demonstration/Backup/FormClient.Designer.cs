
using Matrix.Common.Diagnostics.FrontEnd.TracerControls;
using Matrix.Common.Diagnostics.TracerCore;

namespace Matrix.Framework.SuperPool.Demonstration
{
    partial class FormClient
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormClient));
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.textBoxReport = new System.Windows.Forms.TextBox();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripButtonRaiseEvent = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            this.toolStripTextBox1 = new System.Windows.Forms.ToolStripTextBox();
            this.toolStripButtonCall = new System.Windows.Forms.ToolStripButton();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(632, 373);
            this.tabControl1.TabIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.textBoxReport);
            this.tabPage1.Controls.Add(this.toolStrip1);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(624, 347);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Control";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // textBoxReport
            // 
            this.textBoxReport.BackColor = System.Drawing.SystemColors.Window;
            this.textBoxReport.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.textBoxReport.Dock = System.Windows.Forms.DockStyle.Fill;
            this.textBoxReport.Location = new System.Drawing.Point(3, 28);
            this.textBoxReport.Multiline = true;
            this.textBoxReport.Name = "textBoxReport";
            this.textBoxReport.ReadOnly = true;
            this.textBoxReport.Size = new System.Drawing.Size(618, 316);
            this.textBoxReport.TabIndex = 1;
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButtonRaiseEvent,
            this.toolStripSeparator1,
            this.toolStripLabel1,
            this.toolStripTextBox1,
            this.toolStripButtonCall});
            this.toolStrip1.Location = new System.Drawing.Point(3, 3);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(618, 25);
            this.toolStrip1.TabIndex = 0;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripButtonRaiseEvent
            // 
            this.toolStripButtonRaiseEvent.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonRaiseEvent.Image")));
            this.toolStripButtonRaiseEvent.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonRaiseEvent.Name = "toolStripButtonRaiseEvent";
            this.toolStripButtonRaiseEvent.Size = new System.Drawing.Size(84, 22);
            this.toolStripButtonRaiseEvent.Text = "Raise Event";
            this.toolStripButtonRaiseEvent.ToolTipText = "Raise [ICommunicationInterface.EventOne] Event...";
            this.toolStripButtonRaiseEvent.Click += new System.EventHandler(this.toolStripButtonRaiseEvent_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripLabel1
            // 
            this.toolStripLabel1.Name = "toolStripLabel1";
            this.toolStripLabel1.Size = new System.Drawing.Size(107, 22);
            this.toolStripLabel1.Text = "Send Work to Server";
            // 
            // toolStripTextBox1
            // 
            this.toolStripTextBox1.Name = "toolStripTextBox1";
            this.toolStripTextBox1.Size = new System.Drawing.Size(100, 25);
            // 
            // toolStripButtonCall
            // 
            this.toolStripButtonCall.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonCall.Image")));
            this.toolStripButtonCall.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonCall.Name = "toolStripButtonCall";
            this.toolStripButtonCall.Size = new System.Drawing.Size(51, 22);
            this.toolStripButtonCall.Text = "Send";
            this.toolStripButtonCall.ToolTipText = "Send a [DoWork] call to server...";
            this.toolStripButtonCall.Click += new System.EventHandler(this.toolStripButtonCall_Click);
            // 
            // FormClient
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(632, 373);
            this.Controls.Add(this.tabControl1);
            this.Name = "FormClient";
            this.Text = "Matrix Platform [Super Pool Framework] Client";
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton toolStripButtonRaiseEvent;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripTextBox toolStripTextBox1;
        private System.Windows.Forms.ToolStripButton toolStripButtonCall;
        private System.Windows.Forms.TextBox textBoxReport;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
    }
}