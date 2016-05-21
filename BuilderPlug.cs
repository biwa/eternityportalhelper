
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

#region ================== Namespaces

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Diagnostics;
using CodeImp.DoomBuilder.Windows;
using CodeImp.DoomBuilder.IO;
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.Rendering;
using CodeImp.DoomBuilder.Geometry;
using System.Drawing;
using CodeImp.DoomBuilder.Editing;
using CodeImp.DoomBuilder.Plugins;
using CodeImp.DoomBuilder.Actions;
using CodeImp.DoomBuilder.Types;
using CodeImp.DoomBuilder.Config;
using CodeImp.DoomBuilder.Data;
using CodeImp.DoomBuilder.BuilderModes;
// using CodeImp.DoomBuilder.GZBuilder.Geometry;
// using CodeImp.DoomBuilder.VisualModes;
using CodeImp.DoomBuilder.Controls;

#endregion

namespace CodeImp.DoomBuilder.EternityPortalHelper
{
	//
	// MANDATORY: The plug!
	// This is an important class to the Doom Builder core. Every plugin must
	// have exactly 1 class that inherits from Plug. When the plugin is loaded,
	// this class is instantiated and used to receive events from the core.
	// Make sure the class is public, because only public classes can be seen
	// by the core.
	//

	[Flags]
	public enum InvalidReason
	{
		None,
		PlaneSidedefCount,
		PlaneSectorGeometry,
		PlaneSectorHeights,
		PlaneFreeLines,
		WallActionTagUsed,
		WallLineLength,
		WallLineAngles,
		WallSectorHeights,
		WallNewGeometry
	}

	public class BuilderPlug : Plug
	{
		#region ================== Variables

		private MenusForm menusform;
		private int wallgeometrydepth = 64;
		private static PortalExplorer portalexplorer;
		private Docker docker;

		#endregion

		#region ================== Properties

		public MenusForm MenusForm { get { return menusform; } }
		public int WallGeometryDepth { get { return wallgeometrydepth; } set { wallgeometrydepth = value; } }

		#endregion

		// Static instance. We can't use a real static class, because BuilderPlug must
		// be instantiated by the core, so we keep a static reference. (this technique
		// should be familiar to object-oriented programmers)
		private static BuilderPlug me;

		// Static property to access the BuilderPlug
		public static BuilderPlug Me { get { return me; } }

		// This plugin relies on some functionality that wasn't there in older versions
		public override int MinimumRevision { get { return 1310; } }

		// This event is called when the plugin is initialized
		public override void OnInitialize()
		{
			base.OnInitialize();

			// This binds the methods in this class that have the BeginAction
			// and EndAction attributes with their actions. Without this, the
			// attributes are useless. Note that in classes derived from EditMode
			// this is not needed, because they are bound automatically when the
			// editing mode is engaged.
			General.Actions.BindMethods(this);

			menusform = new MenusForm();

			// TODO: Add DB2 version check so that old DB2 versions won't crash
			// General.ErrorLogger.Add(ErrorType.Error, "zomg!");

			// Keep a static reference
			me = this;
		}

		// This is called when the plugin is terminated
		public override void Dispose()
		{
			base.Dispose();

			// This must be called to remove bound methods for actions.
			General.Actions.UnbindMethods(this);
		}

		// Add the button to create a portal when engaging sectors mode, otherwise remove it
		public override void OnEditEngage(EditMode oldmode, EditMode newmode)
		{
			if (newmode != null && (newmode.Attributes.DisplayName == "Sectors Mode" || newmode.Attributes.DisplayName == "Linedefs Mode"))
				General.Interface.AddButton(BuilderPlug.Me.MenusForm.EternityEnginePortalButton);
			else
				General.Interface.RemoveButton(BuilderPlug.Me.MenusForm.EternityEnginePortalButton);
		}

		public override void OnMapNewEnd()
		{
			base.OnMapNewEnd();

			AddDocker();
		}

		public override void OnMapOpenEnd()
		{
			base.OnMapOpenEnd();

			AddDocker();
			portalexplorer.Setup();
		}

		public override void OnMapCloseBegin()
		{
			base.OnMapCloseBegin();

			General.Interface.RemoveDocker(docker);
		}

		public override void OnMapNewBegin()
		{
			base.OnMapNewBegin();

			General.Interface.RemoveDocker(docker);
		}

		// Geometry pasted
		public override void OnPasteEnd(PasteOptions options)
		{
			if (portalexplorer != null) portalexplorer.UpdateTreeSoon();
		}

		// Undo performed
		public override void OnUndoEnd()
		{
			if (portalexplorer != null) portalexplorer.UpdateTreeSoon();
		}

		// Redo performed
		public override void OnRedoEnd()
		{
			if (portalexplorer != null) portalexplorer.UpdateTreeSoon();
		}

		public override void OnActionEnd(Actions.Action action)
		{
			if (portalexplorer != null && action.Name == "builder_deleteitem")
				portalexplorer.UpdateTreeSoon();
		}

		public override void OnEditAccept()
		{
			if (portalexplorer != null) portalexplorer.UpdateTreeSoon();
		}

		private void AddDocker()
		{
			portalexplorer = new PortalExplorer();
			// panel.OnShowWarningsOnlyChanged += PanelOnOnShowWarningsOnlyChanged;
			docker = new Docker("portalexplorer", "Portal  Explorer", portalexplorer);
			General.Interface.AddDocker(docker, true);
		}

		#region ================== Actions

		[BeginAction("createeternityengineportal")]
		public void CreateEternityEnginePortal()
		{
			// Create plane portal(s) if 2 or more sectors are selected
			if (General.Map.Map.SelectedSectorsCount >= 2)
				CreatePlanePortal();
			else if (General.Map.Map.SelectedLinedefsCount == 2)
				CreateWallPortal();
			else
				General.Interface.DisplayStatus(StatusType.Warning, "You need to select 2 or more sectors or exactly 2 lines to create an Eternity Engine portal");
		}

		#endregion

		#region ================== Methods

		private void CreateWallPortal()
		{
			InvalidReason invalidreason = InvalidReason.None;

			if (General.Map.Map.SelectedLinedefsCount != 2)
				return;

			if (LinesAreValid(General.Map.Map.GetSelectedLinedefs(true).ToList(), out invalidreason) == false)
			{
				if(invalidreason == InvalidReason.WallActionTagUsed)
					General.Interface.DisplayStatus(StatusType.Warning, "Linedef already has an action and/or tag");
				else if(invalidreason == InvalidReason.WallLineLength)
					General.Interface.DisplayStatus(StatusType.Warning, "Lines don't have the same length ");
				else if(invalidreason == InvalidReason.WallLineAngles)
					General.Interface.DisplayStatus(StatusType.Warning, "Angles do not match");
				else if(invalidreason == InvalidReason.WallSectorHeights)
					General.Interface.DisplayStatus(StatusType.Warning, "Sector heights don't match");
				else if(invalidreason == InvalidReason.WallNewGeometry)
					General.Interface.DisplayStatus(StatusType.Warning, "Not enough space to create wall portal geometry");

				return;
			}

			General.Map.UndoRedo.CreateUndo("Create Eternity Engine wall portals");

			// Create geometry if necessary
			int action = 376;
			int tag = General.Map.Map.GetNewTag();

			foreach (Linedef ld in General.Map.Map.GetSelectedLinedefs(true))
			{
				ld.Action = action;
				ld.Tag = tag;

				if (ld.Back == null)
				{
					if (CreateWallPortalGeometry(ld) == false)
					{
						General.Interface.DisplayStatus(StatusType.Warning, "Could not create geometry for wall portal");
						General.Map.UndoRedo.WithdrawUndo();
						return;
					}

					Sector ns = General.Map.Map.GetMarkedSectors(true).First();
				}

				// Switch to the other action
				action = 377;
			}

			portalexplorer.UpdateTreeSoon();

			General.Interface.DisplayStatus(StatusType.Info, "Successfully created Eternity Engine wall portal");

			General.Map.Map.Update();
			General.Editing.Mode.UpdateSelectionInfo();
			General.Interface.RedrawDisplay();
		}

		private bool LinesAreValid(List<Linedef> linedefs, out InvalidReason invalidreason)
		{
			BlockMap<BlockEntry> blockmap = new BlockMap<BlockEntry>(MapSet.CreateArea(General.Map.Map.Vertices), 128);
			invalidreason = InvalidReason.None;

			// Line can't have an action and/or tag
			foreach (Linedef ld in linedefs)
			{
				if (ld.Action != 0 || ld.Tag != 0)
				{
					invalidreason = InvalidReason.WallActionTagUsed;
					return false;
				}

			}

			Linedef ld1 = General.Map.Map.GetSelectedLinedefs(true).First();
			Linedef ld2 = General.Map.Map.GetSelectedLinedefs(true).Last();

			// Make sure the lines are exactly the same length
			if (ld1.Length != ld2.Length)
			{
				invalidreason = InvalidReason.WallLineLength;
				return false;
			}

			// Make sure the angles match. They have to be exactly opposite from each other
			double angle = ld1.Angle + Math.PI;

			if (angle > Math.PI * 2)
				angle -= Math.PI * 2;

			if (Math.Round(ld2.Angle, 5) != Math.Round(angle, 5))
			{
				invalidreason = InvalidReason.WallLineAngles;
				return false;
			}

			// Sector heights between wall portals have to match
			if (ld1.Front.Sector.CeilHeight != ld2.Front.Sector.CeilHeight || ld1.Front.Sector.FloorHeight != ld2.Front.Sector.FloorHeight)
			{
				invalidreason = InvalidReason.WallSectorHeights;
				return false;
			}

			// Check if there's enough space to draw the geometry
			/*
			foreach (Linedef ld in linedefs)
			{
				// Ignore line if it already has a sector at the back
				if (ld.Back != null)
					continue;

				List<Vector2D> points = new List<Vector2D>();

				Vector2D p = ld.Line.GetPerpendicular().GetNormal();
				RectangleF area = MapSet.CreateArea(new List<Linedef>() { ld });
				area = MapSet.IncreaseArea(area, ld.Line.v1 + p * WALLGEOMETRYDEPTH);
				area = MapSet.IncreaseArea(area, ld.Line.v2 + p * WALLGEOMETRYDEPTH);

				blockmap.AddLinedefsSet(General.Map.Map.Linedefs);
				blockmap.AddSectorsSet(General.Map.Map.Sectors);

				points.Add(new Vector2D(ld.Line.v1));
				points.Add(new Vector2D(ld.Line.v2));
				points.Add(new Vector2D(ld.Line.v2 + p * WALLGEOMETRYDEPTH));
				points.Add(new Vector2D(ld.Line.v1 + p * WALLGEOMETRYDEPTH));
				points.Add(new Vector2D(ld.Line.v1));

				foreach (BlockEntry be in blockmap.GetSquareRange(area))
				{
					foreach (Sector s in be.Sectors)
					{
						if (s == ld.Front.Sector)
							continue;

						foreach (Vector2D point in points)
							if (s.Intersect(point))
							{
								invalidreason = InvalidReason.WallNewGeometry;
								return false;
							}
					}

					foreach (Linedef ldo in be.Lines)
					{
						if (ldo == ld)
							continue;

						for (int i = 0; i < points.Count-1; i++)
						{
							Line2D line = new Line2D(points[i], points[i + 1]);

							if (ldo.Line.GetIntersection(line))
							{
								invalidreason = InvalidReason.WallNewGeometry;
								return false;
							}
						}
					}
				}
			}
			*/

			return true;
		}

		private bool CreateWallPortalGeometry(Linedef ld)
		{
			Vector2D p = ld.Line.GetPerpendicular().GetNormal();

			List<DrawnVertex> dv = new List<DrawnVertex>();

			dv.Add(SectorVertex(ld.Line.v2));
			dv.Add(SectorVertex(ld.Line.v1));
			dv.Add(SectorVertex(ld.Line.v1 + p * wallgeometrydepth));
			dv.Add(SectorVertex(ld.Line.v2 + p * wallgeometrydepth));
			dv.Add(SectorVertex(ld.Line.v2));

			return Tools.DrawLines(dv);
		}

		private void CreatePlanePortal()
		{
			InvalidReason invalidreason = InvalidReason.None;
			Dictionary<Sector, int> alreadytaggedsectors = new Dictionary<Sector, int>();
			bool redraw = false;

			// Get all selected sectors, ordered by floor height
			List<Sector> sectors = General.Map.Map.GetSelectedSectors(true).OrderBy(s => s.FloorHeight).ToList();

			List<SectorGroup> sectorgroups = CreateSectorGroups(General.Map.Map.GetSelectedSectors(true).ToList());

			// Check if selected sectors are valid and print an error if they are not
			if (SectorsAreValid(sectorgroups, out invalidreason) == false)
			{
				if (invalidreason == InvalidReason.PlaneSectorGeometry)
					General.Interface.DisplayStatus(StatusType.Warning, "Sector geometry does not match");
				else if (invalidreason == InvalidReason.PlaneSidedefCount)
					General.Interface.DisplayStatus(StatusType.Warning, "Selected sectors do not have the same number of lines");
				else if (invalidreason == InvalidReason.PlaneSectorHeights)
					General.Interface.DisplayStatus(StatusType.Warning, "Floor and ceiling heights of selected sectors do not line up precisely");
				else if (invalidreason == InvalidReason.PlaneFreeLines)
					General.Interface.DisplayStatus(StatusType.Warning, "Selected sectors do not have enough lines without specials/tags");

				return;
			}

			General.Map.UndoRedo.CreateUndo("Create Eternity Engine plane portals");

			// Create portals for all selected sectors
			for (int i = 0; i < sectorgroups.Count - 1; i++)
			{
				Linedef lda;
				Linedef ldb;

				// Ceiling portal
				if (GetLinedefPair(sectorgroups[i], sectorgroups[i + 1], out lda, out ldb) == false)
				{
					General.Interface.DisplayStatus(StatusType.Warning, "Could not get a matching linedef pair for the portal specials");
					General.Map.UndoRedo.WithdrawUndo();
					return;
				}

				// If the sector already has a tag use it, otherwise get a new tag
				int tag = sectorgroups[i].GetTag();

				lda.Action = 360;
				lda.Tag = tag;

				foreach (Sector s in sectorgroups[i].Sectors)
				{
					if (s.Tag == 0 || s.Tag == tag)
						s.Tag = tag;
					else
						alreadytaggedsectors.Add(s, tag);
				}

				ldb.Action = 358;
				ldb.Tag = tag;

				General.Map.Map.Update();

				// Floor portal
				if (GetLinedefPair(sectorgroups[i], sectorgroups[i + 1], out lda, out ldb) == false)
				{
					General.Interface.DisplayStatus(StatusType.Warning, "Could not get a matching linedef pair for the portal specials");
					General.Map.UndoRedo.WithdrawUndo();
					return;
				}

				// If the sector already has a tag use it, otherwise get a new tag
				tag = sectorgroups[i + 1].GetTag();

				lda.Action = 359;
				lda.Tag = tag;

				foreach (Sector s in sectorgroups[i + 1].Sectors)
				{
					if (s.Tag == 0 || s.Tag == tag)
						s.Tag = tag;
					else
						alreadytaggedsectors.Add(s, tag);
				}

				ldb.Action = 361;
				ldb.Tag = tag;

				General.Map.Map.Update();
			}

			// Sectors that already had a non-portal-tag need one of the inside-facing lines to have action 385
			foreach (KeyValuePair<Sector, int> entry in alreadytaggedsectors)
			{
				bool couldtag = false;

				foreach (Sidedef sd in entry.Key.Sidedefs)
				{
					if (sd.Line.Front == sd && sd.Line.Action == 0)
					{
						sd.Line.Action = 385;
						sd.Line.Tag = entry.Value;
						couldtag = true;
						break;
					}
				}

				// Could not tag, check if a linedef can be flipped
				if (couldtag == false)
				{
					foreach (Sidedef sd in entry.Key.Sidedefs)
					{
						if (sd.Other != null && sd.Line.Back == sd && sd.Line.Action == 0)
						{
							sd.Line.FlipVertices();
							sd.Line.FlipSidedefs();
							sd.Line.Action = 385;
							sd.Line.Tag = entry.Value;
							couldtag = true;
							redraw = true;
							break;
						}
					}
				}

				if (couldtag == false)
				{
					General.Interface.DisplayStatus(StatusType.Warning, "Could not assign portal to already tagged sector(s)");
					General.Map.UndoRedo.WithdrawUndo();
					return;
				}
			}

			General.Map.Map.Update();

			if (redraw)
				General.Interface.RedrawDisplay();

			portalexplorer.UpdateTreeSoon();

			General.Interface.DisplayStatus(StatusType.Info, "Successfully created Eternity Engine plane portal(s)");
		}

		// Creates groups of sectors that are connected to each other
		private List<SectorGroup> CreateSectorGroups(List<Sector> sectors)
		{
			List<SectorGroup> groups = new List<SectorGroup>();

			while (sectors.Count > 0)
			{
				SectorGroup sg = new SectorGroup();
				List<Sector> sectorstocheck = new List<Sector>();

				sectorstocheck.Add(sectors[0]);
				sectors.Remove(sectorstocheck[0]);

				while (sectorstocheck.Count > 0)
				{
					foreach (Sidedef sd in sectorstocheck[0].Sidedefs)
					{
						if (sectors.Contains(sd.Sector) && !sg.Sectors.Contains(sd.Sector))
							sectorstocheck.Add(sd.Sector);

						if (sd.Other != null && sectors.Contains(sd.Other.Sector) && !sg.Sectors.Contains(sd.Other.Sector))
							sectorstocheck.Add(sd.Other.Sector);
					}

					if (!sg.Sectors.Contains(sectorstocheck[0]))
						sg.Sectors.Add(sectorstocheck[0]);

					sectors.Remove(sectorstocheck[0]);
					sectorstocheck.RemoveAt(0);
				}

				sg.Update();

				groups.Add(sg);
			}

			return groups.OrderBy(g => g.Sectors[0].FloorHeight).ToList();
		}

		private bool SectorsAreValid(List<SectorGroup> sectorgroups, out InvalidReason invalidreason)
		{
			invalidreason = InvalidReason.None;

			// All sectors must have the same geometry
			for (int i = 0; i < sectorgroups.Count - 1; i++)
			{
				if (sectorgroups[i].GeometryMatches(sectorgroups[i + 1]) == false)
				{
					invalidreason = InvalidReason.PlaneSectorGeometry;
					return false;
				}
			}

			// Sector heights must line up precisely
			for (int i = 0; i < sectorgroups.Count - 1; i++)
			{
				if (sectorgroups[i].CeilingHeight != sectorgroups[i + 1].FloorHeight)
				{
					invalidreason = InvalidReason.PlaneSectorHeights;
					return false;
				}
			}

			// Each sector must have enough lines without specials for the portal specials
			for (int i = 0; i < sectorgroups.Count; i++)
			{
				if (((i == 0 || i == sectorgroups.Count) && sectorgroups[i].FreeLineCount < 2) && sectorgroups[i].FreeLineCount < 4)
				{
					invalidreason = InvalidReason.PlaneFreeLines;
					return false;
				}
			}

			return true;
		}

		private bool GetLinedefPair(SectorGroup sga, SectorGroup sgb, out Linedef la, out Linedef lb)
		{
			Vector2D offset = sgb.Anchor - sga.Anchor;
			List<Sidedef> sidedefs = new List<Sidedef>();

			la = lb = null;

			foreach (Sector s in sga.Sectors)
			{
				foreach (Sidedef sd in s.Sidedefs.Where(sdx => sdx.Line.Action == 0 && sdx.Line.Tag == 0).OrderByDescending(sdx => sdx.Line.Length))
					if (!sidedefs.Contains(sd))
						sidedefs.Add(sd);
			}

			// Get all sidedefs of sector a without action and tag, ordered by their length
			foreach (Sidedef sda in sidedefs)
			{
				foreach (Sector s in sgb.Sectors)
				{
					try
					{
						
						// Sidedef sdb = s.Sidedefs.Where(sd => (sd.Line.Start.Position == sda.Line.Start.Position + offset && sd.Line.End.Position == sda.Line.End.Position + offset) || (sd.Line.Start.Position == sda.Line.End.Position + offset && sd.Line.End.Position == sda.Line.Start.Position + offset)).First();
						var x = s.Sidedefs.Where(sd => (sd.Line.Start.Position == sda.Line.Start.Position + offset && sd.Line.End.Position == sda.Line.End.Position + offset) || (sd.Line.Start.Position == sda.Line.End.Position + offset && sd.Line.End.Position == sda.Line.Start.Position + offset));

						Sidedef sdb = x.First();

						if (sdb.Line.Action == 0 && sdb.Line.Tag == 0)
						{
							la = sda.Line;
							lb = sdb.Line;

							return true;
						}
					}
					catch (Exception e) { }
				}
			}

			return false;
		}

		public void UpdatePortalExplorer()
		{
			portalexplorer.Portals.Nodes.Clear();
			portalexplorer.AddPortals(GetPlanePortals());
			portalexplorer.AddPortals(GetWallPortals());
		}

		public List<List<SectorGroup>> GetPlanePortals()
		{
			Dictionary<int, List<Sector>> taglookup = new Dictionary<int, List<Sector>>();
			Dictionary<Linedef, SectorGroup> sectorgroups = new Dictionary<Linedef, SectorGroup>();
			List<Linedef> applytofrontlines = new List<Linedef>();
			List<List<SectorGroup>> portalgroups = new List<List<SectorGroup>>();
			List<SectorGroup> checkedsectorgroups = new List<SectorGroup>();

			// Create a lookup dictionary, so we only have to crawl through all sectors once
			foreach (Sector s in General.Map.Map.Sectors)
			{
				if (!taglookup.ContainsKey(s.Tag))
					taglookup.Add(s.Tag, new List<Sector>());

				taglookup[s.Tag].Add(s);
			}

			// Go through all linedefs
			foreach (Linedef ld in General.Map.Map.Linedefs)
			{
				if (ld.Action == 358 || ld.Action == 359) // Actions: Apply linked portal to like-tagged ceilings/floors
				{
					SectorGroup sg = new SectorGroup();

					sg.Type = ld.Action == 358 ? SectorGroupType.Ceiling : SectorGroupType.Floor;

					if (taglookup.ContainsKey(ld.Tag))
						foreach (Sector s in taglookup[ld.Tag])
							sg.Sectors.Add(s);

					sectorgroups.Add(ld, sg);
				}
				else if (ld.Action == 385) // Action: Apply portal to front sector
				{
					// the sector group this line references might not exist yet, so we have
					// to store the line and check it later
					applytofrontlines.Add(ld);
				}
			}

			// Check lines again, that apply a portal to the front sector
			foreach (Linedef ld in applytofrontlines)
			{
				// Find the sector group his front sector of the line belongs to
				foreach (KeyValuePair<Linedef, SectorGroup> entry in sectorgroups)
				{
					// Found the sector group, so add the front sector to it
					if (entry.Key.Tag == ld.Tag)
					{
						entry.Value.Sectors.Add(ld.Front.Sector);
						break;
					}
				}
			}

			// Update the sector groups, but set the type to what the map author intended,
			// not what it really is
			foreach (KeyValuePair<Linedef, SectorGroup> entry in sectorgroups)
			{
				entry.Value.Update();
				entry.Value.Type = entry.Key.Action == 358 ? SectorGroupType.Ceiling : SectorGroupType.Floor;
			}

			// Form portal groups from two sector groups. Each portal group has a top and a bottom sector group
			foreach (KeyValuePair<Linedef, SectorGroup> entry in sectorgroups)
			{
				if (checkedsectorgroups.Contains(entry.Value))
					continue;

				if (entry.Key.Action == 358) // Actions: Apply linked portal to like-tagged ceilings; this is a top sector group
				{
					List<SectorGroup> portalgroup = new List<SectorGroup>() {
						entry.Value,
						GetCorrespondingSectorGroup(359, entry.Value, sectorgroups, checkedsectorgroups)
					};

					checkedsectorgroups.Add(portalgroup[0]);
					checkedsectorgroups.Add(portalgroup[1]);
					portalgroups.Add(portalgroup);
				}
				else if (entry.Key.Action == 359) // Actions: Apply linked portal to like-tagged floors; this is a bottom sector group
				{
					List<SectorGroup> portalgroup = new List<SectorGroup>() {
						GetCorrespondingSectorGroup(358, entry.Value, sectorgroups, checkedsectorgroups),
						entry.Value
					};

					checkedsectorgroups.Add(portalgroup[0]);
					checkedsectorgroups.Add(portalgroup[1]);
					portalgroups.Add(portalgroup);
				}
			}

			return portalgroups;
		}

		public List<List<Linedef>> GetWallPortals()
		{
			List<List<Linedef>> portalgroups = new List<List<Linedef>>();
			Dictionary<int, List<Linedef>> linedefsbytag = new Dictionary<int, List<Linedef>>();

			foreach (Linedef ld in General.Map.Map.Linedefs)
			{
				if (ld.Action == 376 || ld.Action == 377)
				{
					if (!linedefsbytag.ContainsKey(ld.Tag)) linedefsbytag.Add(ld.Tag, new List<Linedef>());
					linedefsbytag[ld.Tag].Add(ld);
				}
			}

			foreach (KeyValuePair<int, List<Linedef>> entry in linedefsbytag.OrderBy(o => o.Key))
				portalgroups.Add(entry.Value.OrderBy(o => o.Action).ToList());

			return portalgroups;
		}

		// Finds a sector group by action and tag
		private SectorGroup GetCorrespondingSectorGroup(int action, SectorGroup source, Dictionary<Linedef, SectorGroup> sectorgroups, List<SectorGroup> checkedsectorgroups)
		{
			int tag = -1;

			foreach (Sector s in source.Sectors)
				foreach (Sidedef sd in s.Sidedefs)
					if (sd.Line.Action == action)
						tag = sd.Line.Tag;

			if (tag == -1)
				return null;

			foreach (KeyValuePair<Linedef, SectorGroup> entry in sectorgroups)
				if (!checkedsectorgroups.Contains(entry.Value))
					foreach (Sector s in entry.Value.Sectors)
						if (s.Tag == tag)
							return entry.Value;

			return null;
		}

		// Turns a position into a DrawnVertex and returns it
		private DrawnVertex SectorVertex(float x, float y)
		{
			DrawnVertex v = new DrawnVertex();

			v.stitch = true;
			v.stitchline = true;
			v.pos = new Vector2D((float)Math.Round(x, General.Map.FormatInterface.VertexDecimals), (float)Math.Round(y, General.Map.FormatInterface.VertexDecimals));

			return v;
		}

		private DrawnVertex SectorVertex(Vector2D v)
		{
			return SectorVertex(v.x, v.y);
		}

		#endregion

	}
}
