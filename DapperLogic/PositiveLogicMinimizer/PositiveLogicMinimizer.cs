using System;
using System.Collections.Generic;
using System.Linq;

namespace DapperLogic {
	public class PositiveLogicMinimizer {
		public static string FullyFlatten(string infixString) {
			PostfixLogic postfixList = InfixHelper.ToPostfixLogic(infixString);

			return FullyFlattenPostfix(postfixList);
		}

		public static string FullyFlattenPostfix(string postfix) {
			PostfixLogic postfixList = PostfixHelper.FromString(postfix);
			return FullyFlattenPostfix(postfixList);
		}

		public static string FullyFlattenPostfix(PostfixLogic postfixList) {
			PositiveLogicMinimizer ltm = new PositiveLogicMinimizer(postfixList);
			PostfixLogic primeRep = ltm.GetPostfix();
			PostfixLogic combined = primeRep.NodeLogic.Combine().PostfixLogic;
			return PostfixHelper.ToInfix(combined);
		}

		public static string FullyFlatten(string infixString, params string[] specialSymbols) {
			PostfixLogic postfixList = InfixHelper.ToPostfixLogic(infixString);

			return FullyFlattenPostfix(postfixList, specialSymbols);
		}

		public static string FullyFlattenPostfix(string postfix, params string[] specialSymbols) {
			PostfixLogic postfixList = PostfixHelper.FromString(postfix);
			return FullyFlattenPostfix(postfixList, specialSymbols);
		}

		public static string FullyFlattenPostfix(PostfixLogic postfixList, params string[] specialSymbols) {
			PositiveLogicMinimizer ltm = new PositiveLogicMinimizer(postfixList);
			PostfixLogic primeRep = ltm.GetPostfix();
			PostfixLogic combined = primeRep.NodeLogic.SetSpecialSymbols(false, specialSymbols).Combine(true).PostfixLogic;

			ltm = new PositiveLogicMinimizer(combined);
			PostfixLogic primeRep2 = ltm.GetPostfix();
			PostfixLogic combined2 = primeRep2.NodeLogic.SetSpecialSymbols(true, specialSymbols).Combine().PostfixLogic;
			return PostfixHelper.ToInfix(combined2);
		}




		private PositiveUniqueOperands _uniqueOperands = new();
		private PostfixLogic _postfixLogic = new();

		private List<PositiveMinterm> _essentials = null;
		public List<PositiveMinterm> Essentials => _essentials ??= GetEssentialPrimes();

		private List<PositiveMinterm> _allMinterms = null;
		public List<PositiveMinterm> AllMinterms => _allMinterms ??= GetMinterms();

		private List<PositiveMinterm> _allTrue = null;
		public List<PositiveMinterm> AllTrue => _allTrue ??= AllMinterms.Where(m => m.Value).OrderBy(m => m.OnesCount).ThenBy(m => m.Term).ToList();

		public PositiveLogicMinimizer(PostfixLogic postfix) {
			PostfixLogic newList = new(postfix);

			//Token scan
			for (int i = 0; i < newList.Count; i++) {
				Token token = newList[i];
				if (token.IsOperator) continue;

				newList[i] = _uniqueOperands.AddUseful(token);
			}

			_postfixLogic = newList;
		}

		public PostfixLogic GetPostfix() {
			PostfixLogic postfix = new();

			int orCount = 0;

			foreach (PositiveMinterm minterm in Essentials) {
				string bitRep = Convert.ToString(minterm.Term, 2).PadLeft(this._uniqueOperands.Tokens.Count, '0');
				int andCount = 0;
				for (int i = 0; i < this._uniqueOperands.Tokens.Count; i++) {
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

			return postfix;
		}

		public List<PositiveMinterm> GetEssentialPrimes() {
			List<PositiveMinterm> terms = new List<PositiveMinterm>(AllTrue);

			{ // Cover all first- to get a small list
				_EliminateCoveredTerms(terms);
			}

			_essentials = new List<PositiveMinterm>(terms);
			return terms;
		}

		private void _EliminateCoveredTerms(List<PositiveMinterm> terms) {
			for (int i = 0; i < terms.Count; i++) {
				PositiveMinterm iTerm = terms[i];

				for (int j = i + 1; j < terms.Count; j++) {
					if (i == j) {
						continue;
					}
					PositiveMinterm jTerm = terms[j];

					{ // Covers
						var cover = _GetCoveringTerm(iTerm, jTerm);

						if (cover.HasValue) {
							//It covers
							if (cover.Value.Term == iTerm.Term) {
								terms.RemoveAt(j);
								j--;
								continue;
							} else if (cover.Value.Term == jTerm.Term) {
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

		private PositiveMinterm? _GetCoveringTerm(PositiveMinterm i, PositiveMinterm j) {
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

		public List<PositiveMinterm> GetMinterms() {
			List<PositiveMinterm> list = new List<PositiveMinterm>();
			for (int i = 0; i < Math.Pow(2, _uniqueOperands.Tokens.Count); i++) {

				bool value = _EvaluateByInt(i, out int onesCount);

				list.Add(new PositiveMinterm(i, value, onesCount));
			}
			_allMinterms = list;
			_allMinterms = _allMinterms.OrderBy(m => m.Term).ToList();
			return new List<PositiveMinterm>(_allMinterms);
		}

		private bool _EvaluateByInt(int i, out int onesCount) {
			bool[] bools = _GetBools(i, out onesCount);
			for (int j = 0; j < _uniqueOperands.Tokens.Count; j++) {
				_uniqueOperands.Tokens[j].Value = bools[j];
			}

			return _Evaluate(_postfixLogic);
		}

		private static bool _Evaluate(List<Token> postfix) {
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

		private bool[] _GetBools(int i, out int onesCount) {
			bool[] ret = new bool[_uniqueOperands.Tokens.Count];
			int rem = i;
			int b = 1;  //Bit Index
			onesCount = 0;
			while (rem > 0) {
				if (rem % 2 == 1) {
					ret[_uniqueOperands.Tokens.Count - b] = true;
					onesCount++;
				}
				rem >>= 1;
				b++;
			}
			return ret;
		}
	}
}
