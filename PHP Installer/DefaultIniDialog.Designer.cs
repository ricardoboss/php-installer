using System.Windows.Forms;

namespace PHP_Installer
{
    partial class DefaultIniDialog
    {
        public const DialogResult DevelopmentResult = DialogResult.OK;
        public const DialogResult ProductionResult = DialogResult.Yes;
        public const DialogResult NoResult = DialogResult.No;

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
            this.buttonDevelopment = new System.Windows.Forms.Button();
            this.buttonProduction = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.buttonNo = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // buttonDevelopment
            // 
            this.buttonDevelopment.DialogResult = DevelopmentResult;
            this.buttonDevelopment.Location = new System.Drawing.Point(12, 55);
            this.buttonDevelopment.Name = "buttonDevelopment";
            this.buttonDevelopment.Size = new System.Drawing.Size(102, 23);
            this.buttonDevelopment.TabIndex = 0;
            this.buttonDevelopment.Text = "Yes, development";
            this.buttonDevelopment.UseVisualStyleBackColor = true;
            // 
            // buttonProduction
            // 
            this.buttonProduction.DialogResult = ProductionResult;
            this.buttonProduction.Location = new System.Drawing.Point(120, 55);
            this.buttonProduction.Name = "buttonProduction";
            this.buttonProduction.Size = new System.Drawing.Size(89, 23);
            this.buttonProduction.TabIndex = 1;
            this.buttonProduction.Text = "Yes, production";
            this.buttonProduction.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(278, 43);
            this.label1.TabIndex = 2;
            this.label1.Text = "Would you like to use a default configuration as your php.ini?";
            // 
            // buttonNo
            // 
            this.buttonNo.DialogResult = NoResult;
            this.buttonNo.Location = new System.Drawing.Point(215, 55);
            this.buttonNo.Name = "buttonNo";
            this.buttonNo.Size = new System.Drawing.Size(75, 23);
            this.buttonNo.TabIndex = 3;
            this.buttonNo.Text = "No";
            this.buttonNo.UseVisualStyleBackColor = true;
            // 
            // CopyDefaultIniDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(303, 88);
            this.Controls.Add(this.buttonNo);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.buttonProduction);
            this.Controls.Add(this.buttonDevelopment);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Name = "CopyDefaultIniDialog";
            this.ShowIcon = false;
            this.Text = "Default configuration";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button buttonDevelopment;
        private System.Windows.Forms.Button buttonProduction;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button buttonNo;
    }
}