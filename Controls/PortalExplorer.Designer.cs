namespace CodeImp.DoomBuilder.EternityPortalHelper
{
	partial class PortalExplorer
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
			this.components = new System.ComponentModel.Container();
			this.portals = new CodeImp.DoomBuilder.Controls.BufferedTreeView();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.selectonclick = new System.Windows.Forms.CheckBox();
			this.centeronselected = new System.Windows.Forms.CheckBox();
			this.updatetimer = new System.Windows.Forms.Timer(this.components);
			this.groupBox1.SuspendLayout();
			this.SuspendLayout();
			// 
			// portals
			// 
			this.portals.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.portals.Location = new System.Drawing.Point(3, 76);
			this.portals.Name = "portals";
			this.portals.ShowNodeToolTips = true;
			this.portals.Size = new System.Drawing.Size(258, 319);
			this.portals.TabIndex = 1;
			this.portals.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.portals_AfterSelect);
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox1.Controls.Add(this.selectonclick);
			this.groupBox1.Controls.Add(this.centeronselected);
			this.groupBox1.Location = new System.Drawing.Point(3, 3);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(258, 67);
			this.groupBox1.TabIndex = 13;
			this.groupBox1.TabStop = false;
			// 
			// selectonclick
			// 
			this.selectonclick.AutoSize = true;
			this.selectonclick.Checked = true;
			this.selectonclick.CheckState = System.Windows.Forms.CheckState.Checked;
			this.selectonclick.Location = new System.Drawing.Point(6, 42);
			this.selectonclick.Name = "selectonclick";
			this.selectonclick.Size = new System.Drawing.Size(96, 17);
			this.selectonclick.TabIndex = 14;
			this.selectonclick.Text = "Select on click";
			this.selectonclick.UseVisualStyleBackColor = true;
			// 
			// centeronselected
			// 
			this.centeronselected.AutoSize = true;
			this.centeronselected.Checked = true;
			this.centeronselected.CheckState = System.Windows.Forms.CheckState.Checked;
			this.centeronselected.Location = new System.Drawing.Point(6, 19);
			this.centeronselected.Name = "centeronselected";
			this.centeronselected.Size = new System.Drawing.Size(203, 17);
			this.centeronselected.TabIndex = 13;
			this.centeronselected.Text = "Center view on selected map element";
			this.centeronselected.UseVisualStyleBackColor = true;
			// 
			// updatetimer
			// 
			this.updatetimer.Interval = 750;
			this.updatetimer.Tick += new System.EventHandler(this.updatetimer_Tick);
			// 
			// PortalExplorer
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.groupBox1);
			this.Controls.Add(this.portals);
			this.Name = "PortalExplorer";
			this.Size = new System.Drawing.Size(264, 398);
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private CodeImp.DoomBuilder.Controls.BufferedTreeView portals;
		private System.Windows.Forms.GroupBox groupBox1;
		private System.Windows.Forms.CheckBox selectonclick;
		private System.Windows.Forms.CheckBox centeronselected;
		private System.Windows.Forms.Timer updatetimer;
	}
}
