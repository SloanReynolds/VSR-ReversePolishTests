using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DapperLogic {
	public struct PositiveMinterm {
		public int Term;
		public bool Value;
		public int OnesCount;

		public PositiveMinterm(int term, bool value, int onesCount) {
			this.Term = term;
			this.Value = value;
			this.OnesCount = onesCount;
		}
	}
}
