using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DapperLogic {
	public static class InfixHelper {
		public static string ToPostfix(string infix) {
			return string.Join(" ", ToPostfixLogic(infix));
		}

		public static PostfixLogic ToPostfixLogic(string infixString, bool allowNots = true) {
			List<Token> infix = InfixToTokenList(infixString);
			Stack<Token> stack = new Stack<Token>();
			PostfixLogic postfix = new PostfixLogic();

			//Shunting Yard
			for (int i = 0; i < infix.Count; i++) {
				Token token = infix[i];

				if (!token.IsOperator) {
					//Not an operator
					postfix.Add(token);
					continue;
				}

				// Order of Operations: "!" > "@" > "&" > ":" > "|" > "()"
				if (token.Operator is LeftParens) {
					stack.Push(token);
					continue;
				}

				if (token.Operator is RightParens) {
					while (stack.Count == 0 || !(stack.Peek().Operator is LeftParens)) {
						postfix.Add(stack.Pop());
					}

					stack.Pop();
					continue;
				}

				//It's a non-parenthical operator!
				Operator stackOp;
				while (true) { //Single-Argument Operators (aka "Not")
					stackOp = stack.Count > 0 ? stack.Peek().Operator : null;

					if (stackOp != null && stackOp.SingleOperand) {
						postfix.Add(stack.Pop());
						continue;
					}

					break;
				};

				if (stackOp == null) {
					//Not an operator, or there's nothing in the stack.
					stack.Push(token);
				} else if (token.Operator.Precedence > stackOp.Precedence || token.Operator.Precedence == stackOp.Precedence && token.Operator.RightToLeftAssociative) {
					stack.Push(token);
				} else { //token < stackOp || token == stackOp && LTR
					postfix.Add(stack.Pop());
					stack.Push(token);
				}

				if (i + 1 < infix.Count) continue;
				//Last Call!
				while (stack.Count != 0) {
					postfix.Add(stack.Pop());
				}
			}

			while (stack.Count != 0) {
				postfix.Add(stack.Pop());
			}

			PostfixHelper.RestoreNots(postfix);
			return PostfixHelper.DistributeNots(postfix);
		}

		public static List<Token> InfixToTokenList(string infix) {
			List<Token> newList = new List<Token>();
			Dictionary<string, Token> unique = new();
			int i = 0;
			while (i < infix.Length) {
				string sym = _GetNextToken(infix, ref i);

				// Easiest way to deal with whitespace between operators
				if (sym.Trim() == string.Empty) {
					continue;
				}

				if (!unique.ContainsKey(sym)) {
					unique.Add(sym, new Token(sym));
				}
				newList.Add(unique[sym]);
			}
			return newList;
		}

		public static List<Token> TrimParentheses(string infixString) {
			return TrimParentheses(InfixToTokenList(infixString));
		}

		public static List<Token> TrimParentheses(List<Token> infix) {
			Func<List<Token>, int, bool, Token> findNearestOp = (prmIn, prmStart, prmForward) => {
				int i = prmStart;
				while (true) {
					if (prmForward) {
						i++;
						if (i >= prmIn.Count) {
							return null;
						}
					} else {
						i--;
						if (i < 0) {
							return null;
						}
					}

					Token sym = prmIn[i];
					if (sym.IsOperator) {
						if (sym.Operator == Ops.LeftParens || sym.Operator == Ops.RightParens) {
							return null;
						}

						return prmIn[i];
					}
				}
			};

			Func<Token, Token, Token, bool> isRedundant = (pL, pLow, pR) => {
				int lowPri = pLow.Operator.Precedence;
				if ((pL != null && lowPri < pL.Operator.Precedence) || (pR != null && lowPri < pR.Operator.Precedence)) {
					return false;
				}
				return true;
			};

			//Search through the entire infix
			for (int i = 0; i < infix.Count; i++) {
				//We're looking for the first parenthetical so we can trim it maybe.
				if (!infix[i].IsOperator || infix[i].Operator != Ops.LeftParens) {
					continue;
				}

				//Cool, grab the matching one.
				int j = _GetMatchingParens(infix, i);

				//We need to check all of the operators within.
				//  If they all match, then this parenthetical is redundant.

				//Store the Leftmost, the lowest priority, and the Rightmost operators.
				Token L = findNearestOp(infix, i, false); //Backwards
				Token low = null;
				if (i + 1 != j) {
					for (int k = i + 1; k < j; k++) {
						Token sym = infix[k];
						if (sym.IsOperator) {
							if (sym.Operator == Ops.LeftParens) { //skip over this entire sub-parenthetical
								k = _GetMatchingParens(infix, k);
								continue;
							}

							if (sym.Operator.Precedence == 1) {
								low = sym;
								break;  //Lowest priority op possible...
							} else if (low == null || sym.Operator.Precedence < low.Operator.Precedence) {
								low = sym;
								continue;
							}

						}
					}
				}
				Token R = findNearestOp(infix, j, true); //From the end forwards

				//Console.WriteLine(L + "-" + i + "-" + low + "-" + j + "-" + R);

				//Check for redundancy!
				if (isRedundant(L, low, R)) {
					//We can remove these parentheses.
					infix.RemoveAt(j);
					infix.RemoveAt(i);

					i--;
					continue;
				}
			}

			return infix;
		}

		private static int _GetMatchingParens(List<Token> @in, int start) {
			int count = 0;
			for (int i = start; i < @in.Count; i++) {
				Token token = @in[i];

				if (token.IsOperator) {
					if (token.Operator == Ops.LeftParens) {
						count++;
						continue;
					}

					if (token.Operator == Ops.RightParens) {
						count--;
						if (count == 0) {
							return i;
						}
						continue;
					}
				}

				continue;
			}

			throw new Exception("Couldn't find a matching parens!");
		}

		private static string _GetNextToken(string infix, ref int i) {
			int start = i;

			if (infix[i] == '(' || infix[i] == ')' || Ops.IsValid(infix[i])) {
				i++;
				return infix[i - 1].ToString();
			}

			while (i < infix.Length && infix[i] != '(' && infix[i] != ')' && !Ops.IsValid(infix[i])) {
				i++;
			}

			return infix.Substring(start, i - start).Trim(' ');
		}
	}
}
