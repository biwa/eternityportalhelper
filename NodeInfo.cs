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
using System.Linq;
using System.Text;
using CodeImp.DoomBuilder.Map;

namespace CodeImp.DoomBuilder.EternityPortalHelper
{
	internal sealed class NodeInfo
	{
		private readonly NodeInfoType type;
		private readonly SectorGroup sectorgroup;
		private readonly Sector sector;
		private readonly Linedef linedef;

		public NodeInfoType Type { get { return type; } }
		public SectorGroup SectorGroup { get { return sectorgroup; } }
		public Sector Sector { get { return sector; } }
		public Linedef Linedef { get { return linedef; } }

		public NodeInfo(SectorGroup sg)
		{
			type = NodeInfoType.SECTOR_GROUP;
			sectorgroup = sg;
		}

		public NodeInfo(Sector s)
		{
			type = NodeInfoType.SECTOR;
			sector = s;
		}

		public NodeInfo(Linedef ld)
		{
			type = NodeInfoType.LINEDEF;
			linedef = ld;
		}
	}

	internal enum NodeInfoType
	{
		SECTOR_GROUP,
		SECTOR,
		LINEDEF
	}
}
