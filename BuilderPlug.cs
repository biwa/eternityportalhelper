
#region ================== Copyright (c) 2016 Boris Iwanski

/*
 * Copyright (c) 2015 Boris Iwanski
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
// using System.Drawing;
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
		SidedefCount,
		SectorGeometry,
		SectorHeights,
		FreeLines
	}

	public class BuilderPlug : Plug
	{
		#region ================== Variables

		private MenusForm menusform;

		#endregion

		#region ================== Properties

		public MenusForm MenusForm { get { return menusform; } }

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
			if (newmode.Attributes.DisplayName == "Sectors Mode")
				General.Interface.AddButton(BuilderPlug.Me.MenusForm.CreateEternityEnginePortal);
			else
				General.Interface.RemoveButton(BuilderPlug.Me.MenusForm.CreateEternityEnginePortal);
		}

		#region ================== Actions

		[BeginAction("createeternityengineportal")]
		public void CreateEternityEnginePortal()
		{
			InvalidReason invalidreason = InvalidReason.None;

			// At least 2 sectors have to be selected
			if (General.Map.Map.SelectedSectorsCount < 2)
			{
				General.Interface.DisplayStatus(StatusType.Warning, "You need to select 2 or more sectors to create an Eternity Engine portal");
				return;
			}

			// Get all selected sectors, ordered by floor height
			List<Sector> sectors = General.Map.Map.GetSelectedSectors(true).OrderBy(s => s.FloorHeight).ToList();

			// Check if selected sectors are valid and print an error if they are not
			if (SectorsAreValid(sectors, out invalidreason) == false)
			{
				if (invalidreason == InvalidReason.SectorGeometry)
					General.Interface.DisplayStatus(StatusType.Warning, "Sector geometry does not match");
				else if (invalidreason == InvalidReason.SidedefCount)
					General.Interface.DisplayStatus(StatusType.Warning, "Selected sectors do not have the same number of lines");
				else if (invalidreason == InvalidReason.SectorHeights)
					General.Interface.DisplayStatus(StatusType.Warning, "Floor and ceiling heights of selected sectors do not line up precisely");
				else if (invalidreason == InvalidReason.FreeLines)
					General.Interface.DisplayStatus(StatusType.Warning, "Selected sectors do not have enough lines without specials/tags");

				return;
			}

			General.Map.UndoRedo.CreateUndo("Create Eternity Engine portals");

			// Create portals for all selected sectors
			for (int i = 0; i < sectors.Count - 1; i++)
			{
				Linedef lda;
				Linedef ldb;

				// Ceiling portal
				if (GetLinedefPair(sectors[i], sectors[i + 1], out lda, out ldb) == false)
				{
					General.Interface.DisplayStatus(StatusType.Warning, "Could not get a matching linedef pair for the portal specials");
					General.Map.UndoRedo.WithdrawUndo();
					return;
				}

				// If the sector already has a tag use it, otherwise get a new tag
				int tag = sectors[i].Tag == 0 ? General.Map.Map.GetNewTag() : sectors[i].Tag;

				lda.Action = 360;
				lda.Tag = tag;
				sectors[i].Tag = tag;

				ldb.Action = 358;
				ldb.Tag = tag;

				// Floor portal
				if (GetLinedefPair(sectors[i], sectors[i + 1], out lda, out ldb) == false)
				{
					General.Interface.DisplayStatus(StatusType.Warning, "Could not get a matching linedef pair for the portal specials");
					General.Map.UndoRedo.WithdrawUndo();
					return;
				}

				// If the sector already has a tag use it, otherwise get a new tag
				tag = sectors[i + 1].Tag == 0 ? General.Map.Map.GetNewTag() : sectors[i + 1].Tag;

				lda.Action = 359;
				lda.Tag = tag;
				sectors[i + 1].Tag = tag;

				ldb.Action = 361;
				ldb.Tag = tag;
			}

			General.Interface.DisplayStatus(StatusType.Info, "Successfully created Eternity Engine portal(s)");
		}

		#endregion

		#region ================== Methods

		private bool SectorsAreValid(List<Sector> sectors, out InvalidReason invalidreason)
		{
			invalidreason = InvalidReason.None;

			// All sectors must have the same number of sides
			for (int i = 1; i < sectors.Count; i++)
				if (sectors[0].Sidedefs.Count != sectors[i].Sidedefs.Count)
				{
					invalidreason = InvalidReason.SidedefCount;
					return false;
				}

			// All sectors must have the same geometry
			for (int i = 1; i < sectors.Count; i++)
				if (SectorGeometryMatches(sectors[0], sectors[i]) == false)
				{
					invalidreason = InvalidReason.SectorGeometry;
					return false;
				}

			// Sector heights must line up precisely
			for (int i = 0; i < sectors.Count - 1; i++)
			{
				if (sectors[i].CeilHeight != sectors[i + 1].FloorHeight)
				{
					invalidreason = InvalidReason.SectorHeights;
					return false;
				}
			}

			// Each sector must have enough lines without specials for the portal specials
			for (int i = 0; i < sectors.Count; i++)
			{
				int freecount = 0;

				foreach (Sidedef sd in sectors[i].Sidedefs)
				{
					if (sd.Line.Action == 0 && sd.Line.Tag == 0)
						freecount++;
				}

				// The first and last sector need 2 free lines, sectors in between 4 free lines
				if (((i == 0 || i == sectors.Count) && freecount < 2) && freecount < 4)
				{
					invalidreason = InvalidReason.FreeLines;
					return false;
				}
			}

			return true;
		}

		private bool SectorGeometryMatches(Sector a, Sector b)
		{
			Vector2D va = new Vector2D(a.BBox.Left, a.BBox.Top);
			Vector2D vb = new Vector2D(b.BBox.Left, b.BBox.Top);
			Vector2D offset = vb - va;

			// If the two sectors don't have lines on the same positions (taking the offset
			// into account), their geometry does not match
			foreach (Sidedef sd in a.Sidedefs)
				if (SectorHasLine(b, sd.Line, offset) == false)
					return false;

			return true;
		}

		private bool SectorHasLine(Sector sector, Linedef line, Vector2D offset)
		{
			foreach (Sidedef sd in sector.Sidedefs)
			{
				// Orientation of the line doesn't matter
				if (
					(sd.Line.Start.Position == line.Start.Position + offset && sd.Line.End.Position == line.End.Position + offset) ||
					(sd.Line.Start.Position == line.End.Position + offset && sd.Line.End.Position == line.Start.Position + offset)
				)
				{
					return true;
				}
			}

			return false;
		}

		private bool GetLinedefPair(Sector sa, Sector sb, out Linedef la, out Linedef lb)
		{
			Vector2D va = new Vector2D(sa.BBox.Left, sa.BBox.Top);
			Vector2D vb = new Vector2D(sb.BBox.Left, sb.BBox.Top);
			Vector2D offset = vb - va;

			la = lb = null;

			// Get all sidedefs of sector a without action and tag, ordered by their length
			foreach (Sidedef sda in sa.Sidedefs.Where(sd => sd.Line.Action == 0 && sd.Line.Tag == 0).OrderByDescending(sd => sd.Line.Length))
			{
				Sidedef sdb = sb.Sidedefs.Where(sd => (sd.Line.Start.Position == sda.Line.Start.Position + offset && sd.Line.End.Position == sda.Line.End.Position + offset) || (sd.Line.Start.Position == sda.Line.End.Position + offset && sd.Line.End.Position == sda.Line.Start.Position + offset)).First();

				if (sdb.Line.Action == 0 && sdb.Line.Tag == 0)
				{
					la = sda.Line;
					lb = sdb.Line;

					return true;
				}
			}

			return false;
		}

		#endregion

	}
}
