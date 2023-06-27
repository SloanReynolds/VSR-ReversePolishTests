using System;

namespace ReversePolishTests {
	public static class Extensions {
		public static int CountOnes(this int value) {
			int count = 0;
			while (value != 0) {
				count++;
				value &= value - 1;
			}
			return count;
		}

		public static string ToBits(this int value, int pad, int breakIndex = -1) {
			string thing = Convert.ToString(value, 2).PadLeft(pad, '0');
			if (breakIndex > 0) {
				thing = thing.Insert(breakIndex, " ");
			}
			return thing;
		}
	}
}
