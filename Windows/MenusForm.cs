using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace CodeImp.DoomBuilder.EternityPortalHelper
{
	public partial class MenusForm : Form
	{
		public ToolStripSplitButton EternityEnginePortalButton { get { return eternityengineportalbutton; } }

		public MenusForm()
		{
			InitializeComponent();
		}

		// This invokes an action from control event
		private void InvokeTaggedAction(object sender, EventArgs e)
		{
			General.Interface.InvokeTaggedAction(sender, e);
		}

		private void geometryselect_Click(object sender, EventArgs e)
		{
			foreach (ToolStripMenuItem item in eternityengineportalbutton.DropDownItems)
			{
				if (item.Tag == null)
					continue;

				if ((string)item.Tag == (string)((ToolStripMenuItem)sender).Tag)
				{
					item.Checked = true;
					BuilderPlug.Me.WallGeometryDepth = int.Parse((string)((ToolStripMenuItem)sender).Tag);
				}
				else
					item.Checked = false;
			}
		}
	}
}
