using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using CodeImp.DoomBuilder.Map;
using CodeImp.DoomBuilder.Geometry;

namespace CodeImp.DoomBuilder.EternityPortalHelper
{
	[Flags]
	public enum SectorGroupType
	{
		None,
		Floor,
		Ceiling
	}

	public class SectorGroup
	{
		public static int _id = 0;
		public int id;

		private List<Sector> sectors;
		private SectorGroupType type;
		private int floorheight;
		private int ceilingheight;
		private int freelinecount;
		private Vector2D anchor;
		private List<Line2D> lines;

		public List<Sector> Sectors { get { return sectors; } set { sectors = value; } }
		public SectorGroupType Type { get { return type; } set { type = value; } }
		public int FloorHeight { get { return floorheight; } }
		public int CeilingHeight { get { return ceilingheight; } }
		public int FreeLineCount { get { return freelinecount; } }
		public Vector2D Anchor { get { return anchor; } }
		public List<Line2D> Lines { get { return lines; } }

		public SectorGroup()
		{
			sectors = new List<Sector>();
			type = SectorGroupType.None;
			floorheight = ceilingheight = freelinecount = 0;
			id = _id;
			_id++;
		}

		public void Update()
		{
			type = SectorGroupType.None;
			floorheight = ceilingheight = freelinecount = 0;

			if (sectors.Count == 0)
				return;

			// All sectors must have the same floor height to be usable for a floor portal
			if (sectors.Count(s => s.FloorHeight == sectors[0].FloorHeight) == sectors.Count)
				type |= SectorGroupType.Floor;

			// All sectors must have the same ceiling height to be usable for a floor portal
			if (sectors.Count(s => s.CeilHeight == sectors[0].CeilHeight) == sectors.Count)
				type |= SectorGroupType.Ceiling;

			// Cache floor and ceiling heights
			if ((type & SectorGroupType.Floor) == SectorGroupType.Floor)
				floorheight = sectors[0].FloorHeight;

			if ((type & SectorGroupType.Ceiling) == SectorGroupType.Ceiling)
				ceilingheight = sectors[0].CeilHeight;

			// Get free lines (line without action) and update the anchor
			anchor = sectors[0].Sidedefs.First().Line.Start.Position;
			foreach (Sector s in sectors)
			{
				foreach (Sidedef sd in s.Sidedefs)
				{
					// Free lines
					if (sd.Line.Action == 0 && sd.Line.Tag == 0)
						freelinecount++;

					// Anchor
					if (sd.Line.Start.Position.x < anchor.x) anchor.x = sd.Line.Start.Position.x;
					if (sd.Line.End.Position.x < anchor.x) anchor.x = sd.Line.End.Position.x;
					if (sd.Line.Start.Position.y > anchor.y) anchor.y = sd.Line.Start.Position.y;
					if (sd.Line.End.Position.y > anchor.y) anchor.y = sd.Line.End.Position.y;
				}
			}

			// Create lines from the geometry outline
			lines = new List<Line2D>();
			List<Line2D> tmplines = new List<Line2D>();

			List<Linedef> linedefs = new List<Linedef>();

			foreach (Sector s in sectors)
			{
				foreach (Sidedef sd in s.Sidedefs)
				{
					if (!(sectors.Contains(sd.Sector) && (sd.Other != null && sectors.Contains(sd.Other.Sector))) || sd.Other == null)
						if(!linedefs.Contains(sd.Line))
							linedefs.Add(sd.Line);
				}
			}

			// Create lines from linedefs, with a normalized angle between 0° and 180° (in radians)
			foreach (Linedef ld in linedefs)
			{
				if (ld.Angle <= Math.PI)
					tmplines.Add(new Line2D(ld.Start.Position, ld.End.Position));
				else
					tmplines.Add(new Line2D(ld.End.Position, ld.Start.Position));
			}

			// Merge lines into bigger lines if possible
			lines.Add(tmplines[0]);
			tmplines.RemoveAt(0);
			while (tmplines.Count > 0)
			{
				// Current line
				Line2D cline = lines[lines.Count-1];

				// Get all lines with the same angle as the current line and the start position is the end position of the current line
				List<Line2D> possiblelines = tmplines.Where(l => l.GetAngle() == cline.GetAngle() && l.v1 == cline.v2).ToList();

				if (possiblelines.Count > 0)
				{
					lines[lines.Count - 1] = new Line2D(cline.v1, possiblelines[0].v2);
					tmplines.Remove(possiblelines[0]);
				}
				else
				{
					// Get all lines with the same angle as the current line and the end position is the start position of the current line
					possiblelines = tmplines.Where(l => l.GetAngle() == cline.GetAngle() && l.v2 == cline.v1).ToList();

					if (possiblelines.Count > 0)
					{
						lines[lines.Count - 1] = new Line2D(possiblelines[0].v1, cline.v2);
						tmplines.Remove(possiblelines[0]);
					}
					else
					{
						lines.Add(tmplines[0]);
						tmplines.Remove(tmplines[0]);
					}
				}
			}
		}

		public int GetTag()
		{
			List<Sector> taggedsectors = sectors.Where(s => s.Tag != 0).ToList();

			// No tagged sectors
			if (taggedsectors.Count == 0)
				return General.Map.Map.GetNewTag();

			if (taggedsectors.Count(s => OnlyPortalTagged(s) == true) == taggedsectors.Count)
				return taggedsectors[0].Tag;

			return General.Map.Map.GetNewTag();
		}

		public bool OnlyPortalTagged(Sector sector)
		{
			int checkedlines = 0;
			bool hasportal = false;

			foreach (Linedef ld in General.Map.Map.Linedefs)
			{
				if (ld.Tag == sector.Tag)
				{
					if (ld.Action < 358 || ld.Action > 361)
						return false;
					else
						hasportal = true;
				}

				checkedlines++;
			}

			// If all lines were checked but none tagged the sector, the sector might be
			// affected by scripts
			if (hasportal == false && checkedlines == General.Map.Map.Linedefs.Count)
				return false;

			return true;
		}

		public bool GeometryMatches(SectorGroup other)
		{
			Vector2D offset = other.Anchor - anchor;

			foreach (Line2D l in lines)
			{
				if (other.lines.Count(ol => ol.v1 == l.v1+offset && ol.v2 == l.v2+offset) == 0)
					return false;
			}

			return true;
		}
	}
}
