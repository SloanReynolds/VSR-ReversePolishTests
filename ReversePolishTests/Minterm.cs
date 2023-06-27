using System;

namespace ReversePolishTests {
	public struct Minterm {
		public int Term;
		public bool Value;
		public int OnesCount;
		public int NMask;
		private int? _pPart;
		private int? _nPart;
		private int? _transposed;

		public int TransposedTerm => _transposed ??= _GetTransposed();
		public int PPart => _pParts();
		public int NPart => _nParts();

		public Minterm(int nPart, int pPart, int nMask) {
			this.Term = _CombineParts(nPart, pPart, nMask);
			this._nPart = nPart;
			this._pPart = pPart;
			this.Value = true;
			this.NMask = nMask;
			this.OnesCount = this.Term.CountOnes();

			this._transposed = null;
		}

		public Minterm(int term, bool value, int nMask) : this(term, value, -1, nMask) { }

		public Minterm(int term, bool value, int onesCount, int nMask) {
			this.Term = term;
			this.Value = value;
			this.OnesCount = onesCount > -1 ? onesCount : this.Term.CountOnes();
			this.NMask = nMask;

			this._pPart = null;
			this._nPart = null;
			this._transposed = null;
		}

		private int _GetTransposed() {
			//Notice that positive is on the wrong side here. that is intentional, and I am a fucking genius god. maybe
			return _CombineParts(PPart, NPart, NMask);
		}

		private int _pParts() {
			_InitTermParts();
			return _pPart.Value;
		}

		private int _nParts() {
			_InitTermParts();
			return _nPart.Value;
		}

		private static int _CombineParts(int nP, int pP, int nM) {
			int x = -1;
			int pX = -1;
			int nX = -1;
			int nMask = nM;
			int nPart = nP;
			int pPart = pP;
			int term = 0;

			while (nMask > 0) {
				x++;
				if ((nMask & 1) > 0) {
					pX += 1;
				} else {
					nX += 1;
				}
				nMask >>= 1;
			}

			while (x >= 0) {
				int xBit = 1 << x;
				int nXBit = 1 << nX;
				int pXBit = 1 << pX;
				if ((nM & xBit) > 0) {
					term += (nPart & nXBit) > 0 ? xBit : 0;
					nX--;
				} else {
					term += (pPart & pXBit) > 0 ? xBit : 0;
					pX--;
				}
				x--;
			}

			return term;
		}

		private void _InitTermParts() {
			if (_pPart.HasValue && _nPart.HasValue) {
				return;
			}

			int x = -1;
			int pX = -1;
			int nX = -1;
			int nMask = this.NMask;
			int nPart = 0;
			int pPart = 0;
			int term = this.Term;

			while (nMask > 0) {
				x++;
				if ((nMask & 1) > 0) {
					pX += 1;
				} else {
					nX += 1;
				}
				nMask >>= 1;
			}
			{ }
			while (x >= 0) {
				int xBit = 1 << x;
				int nXBit = 1 << nX;
				int pXBit = 1 << pX;
				if ((this.NMask & xBit) > 0) {
					nPart += (term & xBit) > 0 ? nXBit : 0;
					nX--;
				} else {
					pPart += (term & xBit) > 0 ? pXBit : 0;
					pX--;
				}
				x--;
			}

			this._nPart = nPart;
			this._pPart = pPart;
		}

		public override string ToString() {
			int opCountKinda = NMask.CountOnes();
			return $"{Convert.ToString(Term, 2).PadLeft(opCountKinda * 2, '0')}".Insert(opCountKinda, " ");
		}
	}
}
