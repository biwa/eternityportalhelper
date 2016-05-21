namespace CodeImp.DoomBuilder.EternityPortalHelper
{
	partial class MenusForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MenusForm));
			this.toolStrip1 = new System.Windows.Forms.ToolStrip();
			this.eternityengineportalbutton = new System.Windows.Forms.ToolStripSplitButton();
			this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
			this.geometry8mp = new System.Windows.Forms.ToolStripMenuItem();
			this.geometry16mp = new System.Windows.Forms.ToolStripMenuItem();
			this.geometry32mp = new System.Windows.Forms.ToolStripMenuItem();
			this.geometry64mp = new System.Windows.Forms.ToolStripMenuItem();
			this.geometry128mp = new System.Windows.Forms.ToolStripMenuItem();
			this.toolStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// toolStrip1
			// 
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.eternityengineportalbutton});
			this.toolStrip1.Location = new System.Drawing.Point(0, 0);
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.Size = new System.Drawing.Size(284, 25);
			this.toolStrip1.TabIndex = 0;
			this.toolStrip1.Text = "toolStrip1";
			// 
			// eternityengineportalbutton
			// 
			this.eternityengineportalbutton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.eternityengineportalbutton.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem1,
            this.geometry8mp,
            this.geometry16mp,
            this.geometry32mp,
            this.geometry64mp,
            this.geometry128mp});
			this.eternityengineportalbutton.Image = ((System.Drawing.Image)(resources.GetObject("eternityengineportalbutton.Image")));
			this.eternityengineportalbutton.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.eternityengineportalbutton.Name = "eternityengineportalbutton";
			this.eternityengineportalbutton.Size = new System.Drawing.Size(32, 22);
			this.eternityengineportalbutton.Tag = "createeternityengineportal";
			this.eternityengineportalbutton.Text = "toolStripSplitButton1";
			this.eternityengineportalbutton.ToolTipText = "Creates portals between selected sectors or lines";
			this.eternityengineportalbutton.ButtonClick += new System.EventHandler(this.InvokeTaggedAction);
			// 
			// toolStripMenuItem1
			// 
			this.toolStripMenuItem1.Enabled = false;
			this.toolStripMenuItem1.Name = "toolStripMenuItem1";
			this.toolStripMenuItem1.Size = new System.Drawing.Size(219, 22);
			this.toolStripMenuItem1.Text = "Wall portal geometry depth";
			// 
			// geometry8mp
			// 
			this.geometry8mp.Name = "geometry8mp";
			this.geometry8mp.Size = new System.Drawing.Size(219, 22);
			this.geometry8mp.Tag = "8";
			this.geometry8mp.Text = "8 mp";
			this.geometry8mp.Click += new System.EventHandler(this.geometryselect_Click);
			// 
			// geometry16mp
			// 
			this.geometry16mp.Name = "geometry16mp";
			this.geometry16mp.Size = new System.Drawing.Size(219, 22);
			this.geometry16mp.Tag = "16";
			this.geometry16mp.Text = "16 mp";
			this.geometry16mp.Click += new System.EventHandler(this.geometryselect_Click);
			// 
			// geometry32mp
			// 
			this.geometry32mp.Name = "geometry32mp";
			this.geometry32mp.Size = new System.Drawing.Size(219, 22);
			this.geometry32mp.Tag = "32";
			this.geometry32mp.Text = "32 mp";
			this.geometry32mp.Click += new System.EventHandler(this.geometryselect_Click);
			// 
			// geometry64mp
			// 
			this.geometry64mp.Checked = true;
			this.geometry64mp.CheckState = System.Windows.Forms.CheckState.Checked;
			this.geometry64mp.Name = "geometry64mp";
			this.geometry64mp.Size = new System.Drawing.Size(219, 22);
			this.geometry64mp.Tag = "64";
			this.geometry64mp.Text = "64 mp";
			this.geometry64mp.Click += new System.EventHandler(this.geometryselect_Click);
			// 
			// geometry128mp
			// 
			this.geometry128mp.Name = "geometry128mp";
			this.geometry128mp.Size = new System.Drawing.Size(219, 22);
			this.geometry128mp.Tag = "128";
			this.geometry128mp.Text = "128 mp";
			this.geometry128mp.Click += new System.EventHandler(this.geometryselect_Click);
			// 
			// MenusForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(284, 262);
			this.Controls.Add(this.toolStrip1);
			this.Name = "MenusForm";
			this.Text = "MenusForm";
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ToolStrip toolStrip1;
		private System.Windows.Forms.ToolStripSplitButton eternityengineportalbutton;
		private System.Windows.Forms.ToolStripMenuItem geometry8mp;
		private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem geometry16mp;
		private System.Windows.Forms.ToolStripMenuItem geometry32mp;
		private System.Windows.Forms.ToolStripMenuItem geometry64mp;
		private System.Windows.Forms.ToolStripMenuItem geometry128mp;
	}
}