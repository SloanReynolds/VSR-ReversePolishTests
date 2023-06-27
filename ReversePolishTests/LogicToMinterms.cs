using System;
using System.Collections.Generic;
using System.Linq;
using DapperLogic;
using Spectre.Console;

namespace ReversePolishTests {
	public class LogicToMinterms {
		public static string FullyFlatten(string infix) {
			PostfixLogic postfixList = InfixHelper.ToPostfixLogic(infix);
			LogicToMinterms ltm = new LogicToMinterms(postfixList);
			PostfixLogic primeRep = ltm.GetPostfix();
			PostfixLogic collected = PostfixSimplificator.Simplify(primeRep);
			string infixPart2 = PostfixHelper.ToInfix(collected);
			return infixPart2;
		}

		private List<Token> _list { get; }
		private UniqueOperands _uniqueOperands = new UniqueOperands();

		private List<Minterm> _minimalSet = null;
		public List<Minterm> MinimalSet => _minimalSet ??= GetMinimalSet();

		private List<Minterm> _essentials = null;
		public List<Minterm> Essentials => _essentials ??= GetEssentialPrimes();

		private List<Minterm> _allTrue = null;
		public List<Minterm> AllTrue => _allTrue ??= _allMinterms.Where(m => m.Value).OrderBy(m => m.OnesCount).ThenBy(m => m.Term).ToList();

		private List<Minterm> _allMinterms = null;
		public List<Minterm> AllMinterms => _allMinterms ??= GetMinterms();

		public int OperandCount => _uniqueOperands.AllTokens.Count;
		public int UsefulOperandCount => _uniqueOperands.UsefulTokens.Count;

		public List<Token> Tokens => new List<Token>(_uniqueOperands.AllTokens);
		public List<Token> UsefulTokens => new List<Token>(_uniqueOperands.UsefulTokens);
		private int? _nMask;
		public int NMask => _nMask ??= GetNegativesMask();

		public List<Minterm> AfterCollapse;



		public LogicToMinterms(PostfixLogic postfixTokens, bool ignoreNegatives = false) {
			PostfixLogic newList = new(postfixTokens);

			_uniqueOperands.IgnoreNegatives = ignoreNegatives;
			//Switch negative logic to "positive" symbols
			if (!ignoreNegatives && _HasNegativeLogic(postfixTokens)) {
				List<Token> uniqueNegatives = PostfixHelper.ConvertNotsToNegativeSymbols(ref newList);
				_uniqueOperands.AddRangeNominal(uniqueNegatives);
			}

			//Token scan
			for (int i = 0; i < newList.Count; i++) {
				Token token = newList[i];
				if (token.IsOperator) continue;

				newList[i] = _uniqueOperands.AddUseful(token);
			}

			_list = newList;
		}

		private bool _HasNegativeLogic(IEnumerable<Token> postfixTokens) {
			foreach (var token in postfixTokens) {
				if (token.IsOperator) {
					if (token.Operator is Not || token.Operator is Nand || token.Operator is Nor) return true;
				}
			}
			return false;
		}

		public List<Minterm> GetMinterms() {
			List<Minterm> list = new List<Minterm>();
			for (int i = 0; i < Math.Pow(2, _uniqueOperands.UsefulTokens.Count); i++) {

				bool value = _EvaluateByInt(i, out int onesCount);

				list.Add(new Minterm(i, value, onesCount, NMask));
			}
			_allMinterms = list;
			_allMinterms = _allMinterms.OrderBy(m => m.Term).ToList();
			return new List<Minterm>(_allMinterms);
		}

		public int GetNegativesMask() {
			int negativeMask = 0;
			for (int i = 0; i < _uniqueOperands.UsefulTokens.Count; i++) {
				negativeMask <<= 1;
				if (_uniqueOperands[i].IsNegative) {
					negativeMask++;
				}
			}
			return negativeMask;
		}

		public List<Minterm> GetMinimalSet() {
			List<Minterm> choices = new List<Minterm>(_essentials);
			List<Minterm> chosen = new List<Minterm>();

			if (choices.Count == 1 && choices[0].Term == 0) {
				chosen = choices;
			} else {
				int uniqueMask = 0;
				int totalMask = 0;
				while (true) {
					uniqueMask = _GetMaskOfUniqueTerms(choices, uniqueMask);
					if (uniqueMask == 0) break;

					//if any of the choices left are entirely covered by the mask already been happened, we can safely removeth it.
					for (int i = 0; i < choices.Count; i++) {
						if (((choices[i].Term & ~totalMask) | uniqueMask) == uniqueMask) {
							chosen.Add(choices[i]);
							choices.RemoveAt(i);
							i--;
						}
					}

					totalMask |= uniqueMask;
				}

				if (choices.Count > 0) {
					//PANIK! MAKE HARD CHOICES!!!
					chosen.AddRange(choices);
					{ }
				}
			}

			_minimalSet = new List<Minterm>(chosen);
			return chosen;
		}

		private int _GetMaskOfUniqueTerms(List<Minterm> choices, int ignoreMask) {
			int uniquesMask = 0;
			for (int i = 0; i < choices.Count; i++) {
				Minterm term = choices[i];
				int tempCover = term.Term & ~ignoreMask;
				for (int j = 0; j < choices.Count; j++) {
					if (i == j) continue;
					tempCover ^= (tempCover & choices[j].Term);
					if (tempCover == 0) break;
				}

				if (tempCover != 0) {
					uniquesMask |= term.Term;
				}
			}
			return uniquesMask;
		}

		public List<Minterm> GetEssentialPrimes() {
			List<Minterm> terms = new List<Minterm>(AllTrue);

			{ // Cover all first- to get a small list
				_EliminateCoveredTerms(terms);
			}

			AfterCollapse = new List<Minterm>(terms);

			{ //Start the Knockouts
				bool changed;
				do {
					changed = _KnockoutExtraBits(terms);
					if (changed) {
						_EliminateCoveredTerms(terms);
					}
				} while (changed);
			}

			_essentials = new List<Minterm>(terms);
			return terms;
		}

		private void _EliminateCoveredTerms(List<Minterm> terms) {
			//Console.WriteLine("\nCoverCheck™\n");
			for (int i = 0; i < terms.Count; i++) {
				//Console.WriteLine(terms[i]);
				Minterm iTerm = terms[i];
				int iPosi = terms[i].PPart;
				int iNega = terms[i].NPart;
				if (iPosi == 0 && iNega == 0) {
					//Return empty list??
				}

				for (int j = 0; j < terms.Count; j++) {
					if (i == j) {
						continue;
					}
					Minterm jTerm = terms[j];

					//Check for all zeros?


					{ // Covers
						var cover = _GetCoveringTerm(iTerm, jTerm);

						if (cover.HasValue) {
							//It covers
							if (cover.Value.Term == iTerm.Term) {
								//Console.WriteLine($"i{iTerm} ~j{jTerm}~");
								terms.RemoveAt(j);
								j--;
								continue;
							} else if (cover.Value.Term == jTerm.Term) {
								//Console.WriteLine($"j{jTerm} ~i{iTerm}~");
								terms.RemoveAt(i);
								i--;
								break;
							}

							//Just kidding, it didn't actually cover.
						}
					}
				}
			}
		}

		private bool _KnockoutExtraBits(List<Minterm> terms) {
			for (int i = 0; i < terms.Count; i++) {
				for (int j = 0; j < terms.Count; j++) {
					if (i == j) continue;

					var newParts = _GetKnockOutResult(terms[i], terms[j]);
					if (newParts.HasValue && (newParts.Value.PPart != terms[j].PPart || newParts.Value.NPart != terms[j].NPart)) {
						terms[j] = newParts.Value;
						return true;
					}
				}
			}
			return false;
		}

		private Minterm? _GetKnockOutResult(Minterm iMin, Minterm jMin) {
			if (iMin.Term == 0 && jMin.Term == 0) {
				return null;
			}

			int a = iMin.Term & jMin.Term;
			int aCross = iMin.Term & jMin.TransposedTerm;
			int b = iMin.Term ^ a;

			if (aCross > 0 && aCross == b && (b & (b - 1)) == 0) {
				int g = ~iMin.TransposedTerm & jMin.Term;
				return _punchMinterm(g);
			}
			return null;

			Minterm? _punchMinterm(int fullPunch) {
				return new Minterm(fullPunch, true, NMask);
			}
		}

		private Minterm? _GetCoveringTerm(Minterm i, Minterm j) {
			int iTerm = i.Term;
			int jTerm = j.Term;

			int and = iTerm & jTerm;
			if (and == iTerm) {
				return i;
			} else if (and == jTerm) {
				return j;
			}
			return null;
		}

		private int? _GetCoveringTerm(int iTermPart, int jTermPart) {
			int and = iTermPart & jTermPart;
			if (and == iTermPart) {
				return iTermPart;
			} else if (and == jTermPart) {
				return jTermPart;
			}
			return null;
		}

		internal Table ReportUniqueOperandMap() {
			return _uniqueOperands.ReportUniqueOperandMap();
		}

		public PostfixLogic GetPostfix() {
			if (_minimalSet == null) {
				GetEssentialPrimes();
			}

			if (_IfRedundant(out bool value)) {
				return new PostfixLogic() { new Token(value ? "true" : "false") };
			}

			PostfixLogic postfix = new();

			int orCount = 0;
			//Console.WriteLine(string.Join(" ", this._uniqueOperands));
			foreach (Minterm minterm in _minimalSet) {
				string bitRep = Convert.ToString(minterm.Term, 2).PadLeft(this._uniqueOperands.AllTokens.Count, '0');
				int andCount = 0;
				for (int i = 0; i < this._uniqueOperands.AllTokens.Count; i++) {
					char chr = bitRep[i];
					if (chr == '0')
						continue;

					postfix.Add(this._uniqueOperands[i]);

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
			if (_essentials.Count == 1 && AllTrue.Count > 0 && _essentials[0].Term == 0) {
				//         (!thing | thing)
				value = true;
				return true;
			}

			if (_essentials.Count == 0 && AllTrue.Count == 0) {
				//         (!thing & thing)
				value = false;
				return true;
			}

			value = false;
			return false;
		}

		private bool _EvaluateByInt(int i, out int onesCount) {
			bool[] bools = _GetBools(i, out onesCount);
			for (int j = 0; j < _uniqueOperands.UsefulTokens.Count; j++) {
				_uniqueOperands.UsefulTokens[j].Value = bools[j];
			}
			if (_LogicallyImpossible()) return false;

			bool result = _Evaluate();

			if (result) {
				//Console.WriteLine(string.Join(" ", _uniqueOperands.TruthString) + " | " + (result ? "1" : "0"));
			}
			return result;
		}

		private bool _LogicallyImpossible() {
			for (int i = 0; i < _uniqueOperands.UsefulTokens.Count; i++) {
				Token negativeToken = _uniqueOperands.UsefulTokens[i];
				if (!negativeToken.IsNegative) continue;
				Token positiveToken = _uniqueOperands.GetOpposite(negativeToken);
				if (negativeToken.Value && positiveToken.Value) return true;
			}

			return false;
		}

		private bool[] _GetBools(int i, out int onesCount) {
			bool[] ret = new bool[_uniqueOperands.UsefulTokens.Count];
			int rem = i;
			int b = 1;  //Bit Index
			onesCount = 0;
			while (rem > 0) {
				if (rem % 2 == 1) {
					ret[_uniqueOperands.UsefulTokens.Count - b] = rem % 2 == 1;
					onesCount++;
				}
				rem >>= 1;
				b++;
			}
			return ret;
		}

		private bool _Evaluate() {
			return Evaluate(_list);
		}

		public static bool Evaluate(List<Token> postfix) {
			Stack<bool> stack = new Stack<bool>();

			for (int i = 0; i < postfix.Count; i++) {
				Token symbol = postfix[i];

				//If it's an operand, slap it on the stack.
				if (!symbol.IsOperator) {
					stack.Push(symbol.Value);
					continue;
				}

				//It's an operator, I guess we'll do some stuff.
				symbol.Operator.Eval(stack);
				continue;
			}

			if (stack.Count > 1) {
				throw new Exception("Eveluart cudlnt fugrie out what was going on  dosdoeo now uit s utpp to you");
			}

			return stack.Pop();
		}

	}
}
