
using Matrix.Common.Diagnostics.FrontEnd.TracerControls;
using Matrix.Common.Diagnostics.TracerCore;

namespace Matrix.Framework.SuperPool.Demonstration
{
    partial class FormServer
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormServer));
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripLabel1 = new System.Windows.Forms.ToolStripLabel();
            this.toolStripTextBoxClientName = new System.Windows.Forms.ToolStripTextBox();
            this.toolStripButtonCreateClient = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripButtonRaiseEvent = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.toolStripLabel2 = new System.Windows.Forms.ToolStripLabel();
            this.toolStripTextBoxWorkParameter = new System.Windows.Forms.ToolStripTextBox();
            this.toolStripButtonCall = new System.Windows.Forms.ToolStripButton();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.textBoxReport = new System.Windows.Forms.TextBox();
            this.toolStrip1.SuspendLayout();
            this.tabControl1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripLabel1,
            this.toolStripTextBoxClientName,
            this.toolStripButtonCreateClient,
            this.toolStripSeparator2,
            this.toolStripButtonRaiseEvent,
            this.toolStripSeparator1,
            this.toolStripLabel2,
            this.toolStripTextBoxWorkParameter,
            this.toolStripButtonCall});
            this.toolStrip1.Location = new System.Drawing.Point(3, 3);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(658, 25);
            this.toolStrip1.TabIndex = 0;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripLabel1
            // 
            this.toolStripLabel1.Name = "toolStripLabel1";
            this.toolStripLabel1.Size = new System.Drawing.Size(94, 22);
            this.toolStripLabel1.Text = "Create New Client";
            // 
            // toolStripTextBoxClientName
            // 
            this.toolStripTextBoxClientName.Name = "toolStripTextBoxClientName";
            this.toolStripTextBoxClientName.Size = new System.Drawing.Size(100, 25);
            this.toolStripTextBoxClientName.Text = "{ClientName}";
            // 
            // toolStripButtonCreateClient
            // 
            this.toolStripButtonCreateClient.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonCreateClient.Image")));
            this.toolStripButtonCreateClient.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonCreateClient.Name = "toolStripButtonCreateClient";
            this.toolStripButtonCreateClient.Size = new System.Drawing.Size(60, 22);
            this.toolStripButtonCreateClient.Text = "Create";
            this.toolStripButtonCreateClient.Click += new System.EventHandler(this.toolStripButtonCreateClient_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripButtonRaiseEvent
            // 
            this.toolStripButtonRaiseEvent.Enabled = false;
            this.toolStripButtonRaiseEvent.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonRaiseEvent.Image")));
            this.toolStripButtonRaiseEvent.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonRaiseEvent.Name = "toolStripButtonRaiseEvent";
            this.toolStripButtonRaiseEvent.Size = new System.Drawing.Size(84, 22);
            this.toolStripButtonRaiseEvent.Text = "Raise event";
            this.toolStripButtonRaiseEvent.Click += new System.EventHandler(this.toolStripButtonRaiseEvent_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // toolStripLabel2
            // 
            this.toolStripLabel2.Name = "toolStripLabel2";
            this.toolStripLabel2.Size = new System.Drawing.Size(121, 22);
            this.toolStripLabel2.Text = "Send Work to All Clients";
            // 
            // toolStripTextBoxWorkParameter
            // 
            this.toolStripTextBoxWorkParameter.Name = "toolStripTextBoxWorkParameter";
            this.toolStripTextBoxWorkParameter.Size = new System.Drawing.Size(100, 25);
            this.toolStripTextBoxWorkParameter.Text = "{WorkParameter}";
            // 
            // toolStripButtonCall
            // 
            this.toolStripButtonCall.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButtonCall.Image")));
            this.toolStripButtonCall.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonCall.Name = "toolStripButtonCall";
            this.toolStripButtonCall.Size = new System.Drawing.Size(51, 22);
            this.toolStripButtonCall.Text = "Send";
            this.toolStripButtonCall.ToolTipText = "Call [DoWork] Method on all the accessible clients in the pool.";
            this.toolStripButtonCall.Click += new System.EventHandler(this.toolStripButtonCall_Click);
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(672, 373);
            this.tabControl1.TabIndex = 2;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.textBoxReport);
            this.tabPage2.Controls.Add(this.toolStrip1);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(664, 347);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Control";
            this.tabPage2.UseVisualStyleBackColor = true;
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
            this.textBoxReport.Size = new System.Drawing.Size(658, 316);
            this.textBoxReport.TabIndex = 2;
            // 
            // FormServer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(672, 373);
            this.Controls.Add(this.tabControl1);
            this.Name = "FormServer";
            this.Text = "Matrix Platform [Super Pool Framework] Server";
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.tabControl1.ResumeLayout(false);
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton toolStripButtonCreateClient;
        private System.Windows.Forms.ToolStripLabel toolStripLabel1;
        private System.Windows.Forms.ToolStripTextBox toolStripTextBoxClientName;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripButton toolStripButtonCall;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.TextBox textBoxReport;
        private System.Windows.Forms.ToolStripTextBox toolStripTextBoxWorkParameter;
        private System.Windows.Forms.ToolStripLabel toolStripLabel2;
        private System.Windows.Forms.ToolStripButton toolStripButtonRaiseEvent;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
    }
}