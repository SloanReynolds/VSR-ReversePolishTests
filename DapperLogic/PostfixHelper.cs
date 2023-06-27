using System;
using System.Collections.Generic;
using System.Linq;

namespace DapperLogic {
	public static class PostfixHelper {
		public static string ToInfix(string postfixString) {
			return ToInfix(FromString(postfixString));
		}

		public static string ToInfix(List<Token> postfix) {
			return string.Join(" ", ToInfixList(postfix)).Replace("( ", "(").Replace(" )", ")");
		}

		public static List<Token> ToInfixList(List<Token> postfix) {
			Stack<string> stack = new Stack<string>();

			//Console.WriteLine(string.Join(" ", postfix));
			for (int i = 0; i < postfix.Count; i++) {
				Token token = postfix[i];

				if (!token.IsOperator) {
					//Not an operator
					stack.Push(token.ToString());
				} else {
					if (token.Operator != Ops.Not) {
						string arg1 = stack.Pop();
						string arg2 = stack.Pop();
						stack.Push("(" + arg1 + " " + token.ToString() + " " + arg2 + ")");
					} else {
						string arg1 = stack.Pop();
						stack.Push(token.ToString() + arg1);
					}
				}
			}

			return stack.Count == 0 ? new List<Token>() : InfixHelper.TrimParentheses(stack.Pop());
		}

		public static List<Token> ConvertNotsToNegativeSymbols(ref PostfixLogic refList) {
			List<Token> uniqueNegs = new();
			for (int i = 0; i < refList.Count; i++) {
				Token token = refList[i];
				Token nextToken = i + 1 < refList.Count ? refList[i + 1] : null;

				if (nextToken != null && nextToken.IsOperator && nextToken.Operator is Not) {
					refList[i] = _AddGetUnique(token);
					refList.RemoveAt(i + 1);
					continue;
				}
			}
			return uniqueNegs;



			Token _AddGetUnique(Token token) {
				Token newToken = new Token("~" + token.Symbol);

				Token old = uniqueNegs.Where(t => t.Symbol == newToken.Symbol && t.IsNegative == newToken.IsNegative).FirstOrDefault();
				if (old == null) {
					uniqueNegs.Add(newToken);
					return newToken;
				} else {
					return old;
				}
			}
		}

		public static PostfixLogic FromString(string postfix) {
			string[] postfixArr = postfix.Split(' ');
			PostfixLogic postfixLogic = new(postfixArr.Length);
			for (int i = 0; i < postfixArr.Length; i++) {
				string sym = postfixArr[i];
				postfixLogic.Add(new Token(sym));
			}
			return postfixLogic;
		}










		public static PostfixLogic DistributeNots(PostfixLogic tokens) {
			PostfixLogic newList = new(tokens);

			for (int i = 0; i < newList.Count; i++) {
				Token token = newList[i];
				if (token.IsOperator) {
					if (token.Operator is Nand) {
						newList[i] = new Token(Ops.And);
						newList.Insert(i + 1, new Token(Ops.Not));
						i++;
						continue;
					}
					if (token.Operator is Nor) {
						newList[i] = new Token(Ops.Or);
						newList.Insert(i + 1, new Token(Ops.Not));
						i++;
						continue;
					}
				}
			}

			for (int i = newList.Count - 1; i >= 0; i--) {
				Token token = newList[i];
				if (token.IsOperator && token.Operator is Not) {
					//If the next token ISN'T an operator, then we're golden, we can just continue;
					Token target = newList[i - 1];
					if (!target.IsOperator) continue;

					//Otherwise we need to do the following:
					//null
					// "D", "C", "B", "A", "|", "!", "!", "|", "&", "B", "C", "D", "|", "&", "|"
					//                           |    |
					//                           |  token i
					//                        target i-1
					//                                i
					// "D", "C", "B", "A", "|", "!", "!", "|", "&", "B", "C", "D", "|", "&", "|"
					//                                i
					// "D", "C", "B", "A", "|",           "|", "&", "B", "C", "D", "|", "&", "|"
					//                                     i

					//!doDistribute
					// "D", "C", "B", "A", "|", "@", "!", "|", "&", "B", "C", "D", "|", "&", "|"
					//       |              |    |    |
					//    child2         child1  |  token
					//     find            i-2   |   i
					//                         target
					//                         i-1
					//                                i
					// "D", "C", "B", "A", "|", "@", "!", "|", "&", "B", "C", "D", "|", "&", "|"
					//                                i
					// "D", "C", "B", "A", "|", "&", "!", "|", "&", "B", "C", "D", "|", "&", "|"
					//                                i
					// "D", "C", "B", "A", "|", "&",      "|", "&", "B", "C", "D", "|", "&", "|"
					//                                     i

					//doDistribute
					// "D", "C", "B", "A", "|", "&", "!", "|", "&", "B", "C", "D", "|", "&", "|"
					//       |              |    |    |
					//    child2         child1  |  token
					//     find            i-2   |   i
					//                         target
					//                         i-1
					//                                          i
					// "D", "C",      "B", "A", "|",      "&", "!", "|", "&", "B", "C", "D", "|", "&", "|"
					//                                     i++=>i
					// "D", "C", "!", "B", "A", "|",      "&", "!", "|", "&", "B", "C", "D", "|", "&", "|"
					//                                     i++=>i
					// "D", "C", "!", "B", "A", "|", "!", "&", "!", "|", "&", "B", "C", "D", "|", "&", "|"
					//                                          i
					// "D", "C", "!", "B", "A", "|", "!", "|",      "|", "&", "B", "C", "D", "|", "&", "|"
					//                                               i


					var newOp = Not.GetNottedOpForDistribute(target.Operator, out bool doDistribute);

					//From Not to null
					if (newOp == null) {
						//They're both Nots
						//Kill the first ! and kill the second !
						newList.RemoveAt(i);
						newList.RemoveAt(i - 1);
						//We need to recheck this index, since it's different
						i--;
						continue;
					}

					//From Nand/Nor to And/Or
					if (!doDistribute) {
						target.Operator = newOp;
						newList.RemoveAt(i);
						continue;
					}

					//From And/Or to !(Or)/!(And)
					{
						//Find the target's children, and do the thing.
						int targI = i - 1;
						int child1 = targI - 1; //Target's first child
						int child2 = FindSecondChild(newList, targI);
						newList[i - 1] = new Token(newOp);
						newList.RemoveAt(i);
						newList.Insert(child1 + 1, new Token(Ops.Not));
						newList.Insert(child2 + 1, new Token(Ops.Not));
						i++;
					}
				}
			}
			return newList;
		}

		public static int FindParent(List<Token> postfix, int index) {
			int max = postfix.Count;

			int total = 0;
			while (index < max) {
				index++;
				if (index == max) return max;

				if (postfix[index].IsOperator) {
					total--;
				} else {
					total++;
				}

				if (total <= 0) {
					return index;
				}
			}

			throw new PostfixHelperException("No parent found!");
		}

		public static int FindSecondChild(List<Token> postfix, int index) {
			int total = 1;

			if (!postfix[index].IsOperator) throw new PostfixHelperException("Operands can't have children.");

			while (index > 0) {
				index--;

				if (total <= 0) {
					return index;
				}

				if (postfix[index].IsOperator) {
					//We should just pretend that NOTs don't exist.
					if (!postfix[index].Operator.SingleOperand) {
						total++;
					}
				} else {
					total--;
				}
			}

			throw new PostfixHelperException("No second child found!");
		}





		public static int FindParent(List<string> postfix, int index) {
			return FindParent(postfix.Select(str => new Token(str)).ToList(), index);
		}

		public static int FindSecondChild(List<string> postfix, int index) {
			return FindSecondChild(postfix.Select(str => new Token(str)).ToList(), index);
		}

		public static int CountOperatorDifference(string postfix) {
			return CountOperatorDifference(postfix.Replace("  ", " ").Split(' ').ToList());
		}

		public static int CountOperatorDifference(IEnumerable<string> postfix) {
			int total = 0;
			foreach (string item in postfix) {
				if (IsOperator(item)) {
					total++;
				} else {
					total--;
				}
			}
			return total;
		}

		public static void RestoreNots(List<Token> postfix) {
			for (int i = 0; i < postfix.Count; i++) {
				Token token = postfix[i];
				if (!token.IsOperator && token.IsNegative) {
					postfix[i] = new Token(token.Symbol.Substring(1));
					postfix.Insert(i + 1, new Token(Ops.Not));
					i++;
				}
			}
		}

		public static bool IsOperator(string token) {
			return token == "|" || token == "+";
		}

		public static List<string> RemoveAsFalse(IEnumerable<string> list, string reject) {
			List<string> copy = list.ToList();

			while (copy.Contains(reject)) {
				copy = RemoveAsAt(copy, false, copy.FindLastIndex(str => str == reject)); //Last index because we might be able to avoid re-traversing
			}

			return copy;
		}

		public static List<string> RemoveAsTrue(IEnumerable<string> list, string reject) {
			List<string> copy = list.ToList();

			while (copy.Contains(reject)) {
				copy = RemoveAsAt(copy, true, copy.FindLastIndex(str => str == reject)); //Last index because we might be able to avoid re-traversing
			}

			return copy;
		}

		public static List<string> RemoveAsAt(IEnumerable<string> list, bool nodeValue, int index, int parent = -1) {
			List<string> copy = list.ToList();

			string recurseOp = nodeValue ? "|" : "+";
			string collapseOp = nodeValue ? "+" : "|";

			//We'll use the parameter if it's set.
			if (parent < 0) parent = FindParent(copy, index);

			if (parent == copy.Count) {
				copy = new List<string>();
			} else if (copy[parent] == collapseOp) {
				copy.RemoveAt(parent);
				copy.RemoveAt(index);
			} else if (copy[parent] == recurseOp) {
				int grandparent = FindParent(copy, parent);

				if (grandparent == copy.Count) {
					copy = new List<string>();
				} else if (copy[grandparent] == recurseOp) {
					return RemoveAsAt(list, nodeValue, parent, grandparent);
				} else {
					//Find Parent (already done)
					//Find end of Phrase
					int phraseEnd = FindSecondChild(copy, parent);
					//Delete grandparent!
					copy.RemoveAt(grandparent);
					//Delete the whole phrase
					copy.RemoveRange(phraseEnd, parent - phraseEnd + 1);
				}
			} else {
				throw new PostfixHelperException("Parent wasn't an operator. That should not be possible.");
			}

			return copy;
		}



		class PostfixHelperException : Exception {
			public PostfixHelperException(string msg) : base(msg) {

			}
		}
	}
}
