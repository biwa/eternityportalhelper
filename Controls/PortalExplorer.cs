#region ================== Copyright (c) 2016 Boris Iwanski

/*
 * Copyright (c) 2016 Boris Iwanski
 * This program is released under GNU General Public License
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 */

#endregion

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using CodeImp.DoomBuilder.Config;
using CodeImp.DoomBuilder.Controls;
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.Editing;
using CodeImp.DoomBuilder.Geometry;

namespace CodeImp.DoomBuilder.EternityPortalHelper
{
	public partial class PortalExplorer : UserControl
	{
		public BufferedTreeView Portals { get { return portals; } set { portals = value; } }

		public PortalExplorer()
		{
			InitializeComponent();

			portals.ImageList = new ImageList();
			portals.ImageList.Images.Add(Properties.Resources.Sectors);
			portals.ImageList.Images.Add(Properties.Resources.SectorsGroup);
			portals.ImageList.Images.Add(Properties.Resources.Lines);
			portals.ImageList.Images.Add(Properties.Resources.LinesGroup);
			portals.ImageList.Images.Add(Properties.Resources.Warning);
		}

		public void Setup()
		{
			if (this.ParentForm != null) this.ParentForm.Activated += ParentForm_Activated;
			UpdateTree();
		}

		//It is called every time a dialog window closes.
		private void ParentForm_Activated(object sender, EventArgs e)
		{
			UpdateTreeSoon();
		}

		public void UpdateTreeSoon()
		{
			updatetimer.Stop();
			updatetimer.Start();
		}

		private void UpdateTree()
		{
			this.SuspendLayout();

			portals.Nodes.Clear();
			AddPortals(BuilderPlug.Me.GetPlanePortals());
			AddPortals(BuilderPlug.Me.GetWallPortals());

			this.ResumeLayout();
		}

		private void AddPlanePortal(List<SectorGroup> sectorgroups)
		{
			List<int> tags = new List<int>();
			bool geometrymatches;
			bool topspecialmatch;
			bool bottomspecialmatch;

			foreach(SectorGroup sg in sectorgroups)
				if(sg != null && sg.Sectors.Count > 0)
					tags.Add(sg.Sectors[0].Tag);

			TreeNode rootnode = new TreeNode("Plane: tags " + string.Join(", ", tags.OrderBy(o=>o).Select(x => x.ToString()).ToArray()), 1, 1);

			geometrymatches = SectorGroup.GeometryMatches(sectorgroups[0], sectorgroups[1]);
			topspecialmatch = SectorGroup.TopSpecialsMatch(sectorgroups[0], sectorgroups[1]);
			bottomspecialmatch = SectorGroup.BottomSpecialsMatch(sectorgroups[0], sectorgroups[1]);

			if (!geometrymatches || !topspecialmatch || !bottomspecialmatch)
			{
				rootnode.ImageIndex = rootnode.SelectedImageIndex = 4;
				rootnode.ToolTipText = "Right-click for problem information";
				rootnode.ContextMenuStrip = CreateContextMenu(sectorgroups[0], sectorgroups[1], geometrymatches, topspecialmatch, bottomspecialmatch);
			}

			if (sectorgroups[1] != null)
			{
				rootnode.Nodes.Add(BuildPlanePortalSectors(sectorgroups[1], "Top"));
				if (!geometrymatches) rootnode.Nodes[rootnode.Nodes.Count - 1].ImageIndex = rootnode.Nodes[rootnode.Nodes.Count - 1].SelectedImageIndex = 4;

				rootnode.Nodes.Add(BuildPlanePortalLines(sectorgroups[1], "Top", tags));
				if (!topspecialmatch) rootnode.Nodes[rootnode.Nodes.Count - 1].ImageIndex = rootnode.Nodes[rootnode.Nodes.Count - 1].SelectedImageIndex = 4;
			}

			if (sectorgroups[0] != null)
			{
				rootnode.Nodes.Add(BuildPlanePortalSectors(sectorgroups[0], "Bottom"));
				if (!geometrymatches) rootnode.Nodes[rootnode.Nodes.Count - 1].ImageIndex = rootnode.Nodes[rootnode.Nodes.Count - 1].SelectedImageIndex = 4;

				rootnode.Nodes.Add(BuildPlanePortalLines(sectorgroups[0], "Bottom", tags));
				if (!bottomspecialmatch) rootnode.Nodes[rootnode.Nodes.Count - 1].ImageIndex = rootnode.Nodes[rootnode.Nodes.Count - 1].SelectedImageIndex = 4;
			}

			rootnode.Expand();

			portals.Nodes.Add(rootnode);
		}

		private ContextMenuStrip CreateContextMenu(SectorGroup a, SectorGroup b, bool geometrymatches, bool topspecialmatch, bool bottomspecialmatch)
		{
			ContextMenuStrip cms = new ContextMenuStrip();
			cms.ItemClicked += SelectUnmatchedLinedefs;

			ToolStripMenuItem option;

			if (!geometrymatches)
			{
				option = new ToolStripMenuItem("Select all unmatched linedefs");
				option.Tag = new ContextMenuInfo(a, b, UnmatchingLinedefsType.Bottom | UnmatchingLinedefsType.Top);
				cms.Items.Add(option);

				option = new ToolStripMenuItem("Select unmatched linedefs of top");
				option.Tag = new ContextMenuInfo(a, b, UnmatchingLinedefsType.Top);
				cms.Items.Add(option);

				option = new ToolStripMenuItem("Select unmatched linedefs of bottom");
				option.Tag = new ContextMenuInfo(a, b, UnmatchingLinedefsType.Bottom);
				cms.Items.Add(option);
			}

			if (!topspecialmatch || !bottomspecialmatch)
			{
				if (!geometrymatches)
					cms.Items.Add(new ToolStripSeparator());

				if (!topspecialmatch)
				{
					option = new ToolStripMenuItem("Select unmatched special linedefs of top");
					option.Tag = new ContextMenuInfo(a, b, UnmatchingLinedefsType.SpecialsTop);
					cms.Items.Add(option);
				}

				if (!bottomspecialmatch)
				{
					option = new ToolStripMenuItem("Select unmatched special linedefs of bottom");
					option.Tag = new ContextMenuInfo(a, b, UnmatchingLinedefsType.SpecialsBottom);
					cms.Items.Add(option);
				}
			}

			return cms;
		}

		private void SelectUnmatchedLinedefs(object sender, ToolStripItemClickedEventArgs e)
		{
			ContextMenuInfo cmi = e.ClickedItem.Tag as ContextMenuInfo;

			General.Map.Map.ClearAllSelected();

			if (General.Editing.Mode.GetType().Name != "LinedefsMode") General.Editing.ChangeMode("LinedefsMode");

			foreach (Linedef ld in SectorGroup.GetUnmatchingLinedefs(cmi.Top, cmi.Bottom, cmi.Type))
			{
				if (!ld.IsDisposed)
				{
					((ClassicMode)General.Editing.Mode).SelectMapElement(ld);
					ld.Selected = true;
				}
			}

			// Update info and view
			General.Editing.Mode.UpdateSelectionInfo();
			General.Interface.RedrawDisplay();
		}

		private TreeNode BuildPlanePortalSectors(SectorGroup sectorgroup, string name)
		{
			NodeInfo info = new NodeInfo(sectorgroup);
			TreeNode node = new TreeNode(name + " Sectors", 0, 0) { Tag = info };

			foreach (Sector s in sectorgroup.Sectors.OrderBy(o => o.Index))
			{
				info = new NodeInfo(s);
				node.Nodes.Add(new TreeNode(s.ToString(), 0, 0) { Tag = info });
			}

			return node;
		}

		private TreeNode BuildPlanePortalLines(SectorGroup sectorgroup, string name, List<int> tags)
		{
			TreeNode node = new TreeNode(name + " Linedefs", 2, 2);
			List<Linedef> linedefs = new List<Linedef>();

			foreach (Sector s in sectorgroup.Sectors)
			{
				foreach (Sidedef sd in s.Sidedefs)
				{
					if (!linedefs.Contains(sd.Line) && (sd.Line.Action == 385 || (sd.Line.Action >= 358 && sd.Line.Action <= 361)) && tags.Contains(sd.Line.Tag))
						linedefs.Add(sd.Line);
				}
			}

			foreach (Linedef ld in linedefs.OrderBy(o => o.Action))
			{
				NodeInfo info = new NodeInfo(ld);
				LinedefActionInfo ldai = General.Map.Config.GetLinedefActionInfo(ld.Action);
				string ldname = string.Format("Ld {0}: {1} - {2}", ld.Index, ldai.Index, ldai.Name);
				node.Nodes.Add(new TreeNode(ldname, 2, 2) { Tag = info });
			}

			return node;
		}

		public void AddPortals(List<List<SectorGroup>> sectorgroups)
		{
			foreach (List<SectorGroup> sg in sectorgroups)
				AddPlanePortal(sg);
		}

		public void AddPortals(List<List<Linedef>> linedefgroups)
		{
			foreach (List<Linedef> linedefgroup in linedefgroups)
			{
				TreeNode node = new TreeNode(string.Format("Wall: tag {0}", linedefgroup[0].Tag), 3, 3);

				foreach (Linedef ld in linedefgroup)
				{
					NodeInfo info = new NodeInfo(ld);
					LinedefActionInfo ldai = General.Map.Config.GetLinedefActionInfo(ld.Action);
					string ldname = string.Format("Ld {0}: {1} - {2}", ld.Index, ldai.Index, ldai.Name);
					node.Nodes.Add(new TreeNode(ldname, 2, 2) { Tag = info });
				}

				node.Expand();

				portals.Nodes.Add(node);
			}
		}

		private void portals_AfterSelect(object sender, TreeViewEventArgs e)
		{
			NodeInfo info = e.Node.Tag as NodeInfo;

			if (info == null)
				return;

			if (selectonclick.Checked)
			{
				General.Map.Map.ClearAllSelected();

				if (info.Type == NodeInfoType.SECTOR)
				{
					if (General.Editing.Mode.GetType().Name != "SectorsMode") General.Editing.ChangeMode("SectorsMode");
					if (!info.Sector.IsDisposed)
					{
						((ClassicMode)General.Editing.Mode).SelectMapElement(info.Sector);
						foreach (Sidedef sd in info.Sector.Sidedefs) sd.Line.Selected = true;
					}
				}
				else if (info.Type == NodeInfoType.SECTOR_GROUP)
				{
					if (General.Editing.Mode.GetType().Name != "SectorsMode") General.Editing.ChangeMode("SectorsMode");

					foreach (Sector s in info.SectorGroup.Sectors)
					{
						if (s.IsDisposed) continue;
						((ClassicMode)General.Editing.Mode).SelectMapElement(s);
						foreach (Sidedef sd in s.Sidedefs) sd.Line.Selected = true;
					}
				}
				else if (info.Type == NodeInfoType.LINEDEF)
				{
					if (General.Editing.Mode.GetType().Name != "LinedefsMode") General.Editing.ChangeMode("LinedefsMode");
					if (!info.Linedef.IsDisposed)
					{
						((ClassicMode)General.Editing.Mode).SelectMapElement(info.Linedef);
						info.Linedef.Selected = true;
					}
				}
			}

			// Update info and view
			General.Editing.Mode.UpdateSelectionInfo();
			General.Interface.RedrawDisplay();

			if (centeronselected.Checked)
			{
				List<Vector2D> points = new List<Vector2D>();
				RectangleF area = MapSet.CreateEmptyArea();

				if (info.Type == NodeInfoType.SECTOR && !info.Sector.IsDisposed)
				{
					foreach (Sidedef sd in info.Sector.Sidedefs)
					{
						points.Add(sd.Line.Start.Position);
						points.Add(sd.Line.End.Position);
					}
				}
				else if (info.Type == NodeInfoType.SECTOR_GROUP)
				{
					foreach (Sector s in info.SectorGroup.Sectors)
					{
						if (s.IsDisposed) continue;
						foreach (Sidedef sd in s.Sidedefs)
						{
							points.Add(sd.Line.Start.Position);
							points.Add(sd.Line.End.Position);
						}
					}
				}
				else if (info.Type == NodeInfoType.LINEDEF)
				{
					if (General.Editing.Mode.GetType().Name != "LinedefsMode") General.Editing.ChangeMode("LinedefsMode");
					if (!info.Linedef.IsDisposed)
					{
						points.Add(info.Linedef.Start.Position);
						points.Add(info.Linedef.End.Position);
					}
				}


				area = MapSet.IncreaseArea(area, points);

				// Add padding
				area.Inflate(100f, 100f);

				// Zoom to area
				ClassicMode editmode = (General.Editing.Mode as ClassicMode);
				editmode.CenterOnArea(area, 0.0f);
			}
		}

		private void updatetimer_Tick(object sender, EventArgs e)
		{
			Debug.Print("update...");
			updatetimer.Stop();
			UpdateTree();
		}
	}
}
