using System;
using System.Collections.Generic;
using System.Linq;
using DapperLogic;
using Spectre.Console;

namespace ReversePolishTests {
	public class QuineMcCluskey {
		private readonly bool _visualize;
		private IEnumerable<Implicant> _subset = new List<Implicant>();
		private List<Implicant> _primes = new List<Implicant>();
		private List<Implicant> _essentialPrimes = new List<Implicant>();
		private LogicToMinterms _LTM = null;
		public LogicToMinterms LTM => _LTM;
		public IEnumerable<Minterm> allTrue;



		public QuineMcCluskey(PostfixLogic postfixTokens, bool visualize = false) {
			_LTM = new LogicToMinterms(postfixTokens, true);

			allTrue = _LTM.GetMinterms().Where(m => m.Value);

			_subset = allTrue.Select(m => new Implicant(m)).Distinct().OrderBy(I => I.FirstTerm);
			this._visualize = visualize;
		}

		public QuineMcCluskey(IEnumerable<int> terms, bool visualize = false) : this(terms.Select(t => new Minterm(t, true, 0)), visualize) { }

		public QuineMcCluskey(IEnumerable<Minterm> terms, bool visualize = false) {
			_subset = terms.Select(t => new Implicant(t)).Distinct().OrderBy(I => I.FirstTerm);
			this._visualize = visualize;
		}

		public List<Implicant> FullyCombine() {
			List<Implicant> prime = new List<Implicant>();
			List<Implicant> remaining = new List<Implicant>(_subset);
			if (remaining.Count == 0)
				return null;
			int maxTerm = remaining.Max(i => i.LastTerm);
			int order = 0;
			while (Math.Pow(2, order) <= maxTerm && remaining.Count > 0) {
				List<Implicant> newOrder = new List<Implicant>();
				for (int i = 0; i < remaining.Count; i++) {
					Implicant imp1 = remaining[i];
					for (int j = 0; j < remaining.Count; j++) {
						Implicant imp2 = remaining[j];
						//All Implicants where:
						//  -Its first term is greater than my last (ASC order basically)
						//  -Its bitmask value is the same as mine
						//  -Its 1s count is exactly one more than mine
						//  0, 1, 8, 9  ::  0 0 0 + 0 0 -
						//  0, 8, 1, 9  ::  0 0 0 - 0 0 +
						if (imp1.Mask == imp2.Mask &&
							imp1.OnesCount + 1 == imp2.OnesCount) {
							if (imp1.LastTerm < imp2.FirstTerm) {
								int newMask = imp1.FirstTerm ^ imp2.FirstTerm;
								if ((newMask & (newMask - 1)) == 0) { //It's a power of two! ie There's only one digit's difference
									newOrder.Add(new Implicant(imp1.Minterms, imp2.Minterms, newMask | imp1.Mask));
									{ }
								}
							}
						}
					}
				}
				List<Implicant> used = new List<Implicant>();

				List<Minterm> usedMinterms = newOrder.SelectMany(i => i.Minterms).Distinct().ToList();
				foreach (var implicant in remaining) {
					if (implicant.Minterms.All(m => usedMinterms.Contains(m))) {
						used.Add(implicant);
					}
				}

				prime.AddRange(remaining.Except(used));
				remaining = newOrder;
				order++;
				if (Math.Pow(2, order) > maxTerm && remaining.Count > 0) {
					prime.AddRange(remaining);
				}
			}
			_primes = prime.OrderBy(p => p.LastTerm).OrderBy(p => p.FirstTerm).ToList();
			return _primes;
		}

		public List<Implicant> FullyExtractEssentials() {
			List<Implicant> remainingPrimes = new List<Implicant>(_primes);
			List<Minterm> remaining = new List<Minterm>(_primes.SelectMany(imp => imp.Minterms).Distinct().OrderBy(m => m.Term));
			List<Implicant> essential = new List<Implicant>();
			List<Minterm> mintermsCovered = new List<Minterm>();

			if (_visualize) _VizualizePrimeTable(remaining, remainingPrimes);

			while (remaining.Count > 0) {
				bool essentialsExtracted = false;

				Bidex<Minterm, Implicant> bidex = _BuildBidex(remaining, remainingPrimes);

				foreach (var minterm in bidex.LeftKeys) {
					if (bidex[minterm].Count == 1) {
						//Can we get a living bidex going plz? This seems silly.
						if (!essential.Contains(bidex[minterm][0])) essential.Add(bidex[minterm][0]);
						foreach (var item in bidex[minterm][0].Minterms) {
							if (!mintermsCovered.Contains(item)) mintermsCovered.Add(item);
						}
						essentialsExtracted = true;
					}
				}

				if (!essentialsExtracted) {
					//Now we makin' the hard choices...
					//Naive score?
					int highScore = -1;
					Implicant highImp = null;
					foreach (var implicant in bidex.RightKeys.OrderByDescending(i => i.Size)) {
						int score = _GetScore(bidex, implicant);
						if (score > highScore) {
							highScore = score;
							highImp = implicant;
						}
					}
					essential.Add(highImp);
					foreach (var minterm in highImp.Minterms) {
						if (!mintermsCovered.Contains(minterm)) mintermsCovered.Add(minterm);
					}
				}

				remaining = remaining.Except(mintermsCovered).ToList();
				remainingPrimes = remainingPrimes.Except(essential).ToList();

				if (_visualize) _VizualizePrimeTable(remaining, remainingPrimes);
			}

			_essentialPrimes = essential;
			return _essentialPrimes;
		}

		private Bidex<Minterm, Implicant> _BuildBidex(List<Minterm> remaining, List<Implicant> remainingPrimes) {
			Bidex<Minterm, Implicant> bidex = new Bidex<Minterm, Implicant>();

			for (int i = 0; i < remaining.Count; i++) {
				for (int j = 0; j < remainingPrimes.Count; j++) {
					if (remainingPrimes[j].Minterms.Contains(remaining[i])) {
						bidex.Add(remaining[i], remainingPrimes[j]);
					}
				}
			}

			return bidex;

		}

		private void _VizualizePrimeTable(List<Minterm> remaining, List<Implicant> remainingPrimes) {
			Bidex<Minterm, Implicant> bidex = _BuildBidex(remaining, remainingPrimes);
			_VizualizePrimeTable(bidex);
		}

		private void _VizualizePrimeTable(Bidex<Minterm, Implicant> bidex) {
			var table = new Table();
			table.AddColumn("");
			table.AddColumns(bidex.LeftKeys.Select(r => new TableColumn($"{r.Term}")).ToArray());
			table.AddColumn("Score");

			foreach (var implicant in bidex.RightKeys.OrderByDescending(i => i.Size)) {
				string[] cols = new string[table.Columns.Count];
				cols[0] = implicant.ToString();
				int i = 0;
				foreach (var minterm in bidex.LeftKeys) {
					i++;
					cols[i] = !implicant.Minterms.Contains(minterm) ? "" : bidex[minterm].Count == 1 ? "[green]x[/]" : $"{bidex[minterm].Count}";
				}
				cols[table.Columns.Count - 1] = $"{_GetScore(bidex, implicant)}";

				table.AddRow(cols);
			}

			AnsiConsole.Write(table);
		}

		private int _GetScore(Bidex<Minterm, Implicant> bidex, Implicant implicant) {
			int score = 0;
			foreach (var minterm in implicant.Minterms) {
				if (bidex.TryGet(minterm, out List<Implicant> list))
					score += list.Count;
			}
			return score;
		}

		public List<Token> GetPostfix() {
			if (_LTM == null) {
				return null;
			}

			if (_IfRedundant(out bool value)) {
				return new List<Token>() { new Token(value ? "true" : "false") };
			}

			List<Token> postfix = new List<Token>();

			int orCount = 0;
			foreach (Implicant implicant in _essentialPrimes) {
				int andCount = 0;
				for (int i = 0; i < _LTM.UsefulOperandCount; i++) {
					char chr = implicant.MaskString.PadLeft(_LTM.UsefulOperandCount, '0')[i];
					if (chr == '-')
						continue;

					postfix.Add(_LTM.UsefulTokens[i]);

					if (chr == '0') {
						postfix.Add(new Token(Ops.Not));
					}

					andCount++;
				}

				while (andCount > 1) {
					postfix.Add(new Token(Ops.And));
					andCount--;
				}
				orCount++;
			}

			while (orCount > 1) {
				postfix.Add(new Token(Ops.Or));
				orCount--;
			}

			PostfixHelper.RestoreNots(postfix);

			return postfix;
		}

		private bool _IfRedundant(out bool value) {
			Console.WriteLine($"_primeCount {_essentialPrimes.Count} | trueCount {_LTM.AllTrue.Count} | firstTerm {(_essentialPrimes.Count > 0 ? _essentialPrimes[0].FirstTerm.ToString() : "NONE")}");
			if (_essentialPrimes.Count <= 1 && _LTM.AllTrue.Count == _LTM.AllMinterms.Count) {
				//         (!thing | thing)
				value = true;
				return true;
			}

			if (_essentialPrimes.Count == 0 && _LTM.AllTrue.Count == 0) {
				//         (!thing & thing)
				value = false;
				return true;
			}

			value = false;
			return false;
		}
	}

	public class Implicant {
		public List<Minterm> Minterms => _minterms;
		private List<Minterm> _minterms = new List<Minterm>();
		private int _mask = 0;

		public Implicant(Minterm minterm) {
			_minterms.Add(minterm);
		}

		public Implicant(IEnumerable<Minterm> minterms, IEnumerable<Minterm> minterms2, int mask) {
			_minterms.AddRange(minterms);
			_minterms.AddRange(minterms2);
			_mask = mask;
		}

		public int OnesCount => _minterms[0].OnesCount;
		public int FirstTerm => _minterms[0].Term;
		public int LastTerm => _minterms[_minterms.Count - 1].Term;
		public int Mask => _mask;
		public int Size => _minterms.Count;
		public bool DidCombine { get; private set; }
		public string MaskString {
			get {
				string newstring = Convert.ToString(LastTerm, 2);//.PadLeft(32, '0');
				string maskstring = Convert.ToString(Mask, 2);
				for (int i = 0; (i < newstring.Length && i < maskstring.Length); i++) {
					if (maskstring[maskstring.Length - 1 - i] == '1') {
						newstring = newstring.Substring(0, newstring.Length - 1 - i) + "-" + newstring.Substring(newstring.Length - i);
					}
				}
				return newstring;
			}
		}

		public override string ToString() {
			return $"{string.Join(",", _minterms.Select(m => m.Term))}";
		}

		public List<Implicant> CombineWith(IEnumerable<Implicant> minterms, List<Implicant> unused) {
			//All Implicants where:
			//  -Its first term is greater than my last (ASC order basically)
			//  -Its bitmask value is the same as mine
			//  -Its 1s count is exactly one more than mine
			DidCombine = false;
			List<Implicant> newImplicants = new List<Implicant>();
			foreach (Implicant imp in minterms) {
				if (imp.FirstTerm > this.LastTerm &&
					imp.OnesCount - 1 == this.OnesCount &&
					imp.Mask == this.Mask &&
					_IsPowerOfTwo(imp.FirstTerm ^ this.FirstTerm, out int mask)) {
					unused.Remove(this);
					unused.Remove(imp);
					newImplicants.Add(new Implicant(this.Minterms, imp.Minterms, mask | _mask));
					DidCombine = true;
				}
			}

			return newImplicants;
		}

		private bool _IsPowerOfTwo(int num, out int mask) {
			mask = num;
			return (num & (num - 1)) == 0;
		}
	}
}
