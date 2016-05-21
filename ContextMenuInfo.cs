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

namespace CodeImp.DoomBuilder.EternityPortalHelper
{
	internal sealed class ContextMenuInfo
	{
		private readonly SectorGroup top;
		private readonly SectorGroup bottom;
		private readonly UnmatchingLinedefsType type;

		public SectorGroup Top { get { return top; } }
		public SectorGroup Bottom { get { return bottom; } }
		public UnmatchingLinedefsType Type { get { return type; } }

		public ContextMenuInfo(SectorGroup top, SectorGroup bottom, UnmatchingLinedefsType type)
		{
			this.top = top;
			this.bottom = bottom;
			this.type = type;
		}
	}
}