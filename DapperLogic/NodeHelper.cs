using System;
using System.CodeDom;
using static DapperLogic.NodeLogic;

namespace DapperLogic {
	public static class NodeHelper {

		public static NodeLogic FromInfix(string infix) {
			return FromPostfixLogic(InfixHelper.ToPostfixLogic(infix));
		}

		public static NodeLogic FromPostfixLogic(PostfixLogic postfix) {
			//NodeLogic nl = new NodeLogic(_MakeNodal(postfix), postfix);
			//return nl;
			return new NodeLogic(_MakeNodal(postfix, out _));
		}

		private static Node _MakeNodal(PostfixLogic postfix, out int endIndex, int startIndex = -1, int depth = 0) {
			//      w | d | c & b | a | f
			//      f a | b c & d | w | |
			//      2 2 1 4 4 3 3 2 2 1 0

			int ubound = postfix.Count - 1;
			if (startIndex == -1) startIndex = ubound;
			Operator current = null;
			Node newNode = null;

			if (postfix.Count == 1 && !postfix[0].IsOperator) {
				newNode = new OrNode(0, postfix, 0);
				newNode.AddToken(postfix[0]);
				endIndex = 0;
				return newNode;
			}

			for (int i = startIndex; i >= 0; i--) {
				Token token = postfix[i];
				if (i != startIndex) {
					if (postfix.TokenDepths[i] <= newNode.PostfixDepth) {
						endIndex = i;
						return newNode;
					}
				} else {
					//This had better be an operator...
					current = token.Operator;
					if (current is Or) {
						newNode = new OrNode(depth, postfix, i);
						continue;
					}
					if (current is And) {
						newNode = new AndNode(depth, postfix, i);
						continue;
					}
					throw new Exception("Only OR and AND are supported at this time.");
				}

				//Another Operator Case:
				if (token.IsOperator) {
					if (token.Operator.GetType() == current.GetType()) {
						//Do nothing?
						continue;
					}

					newNode.AddNode(_MakeNodal(postfix, out int newI, i, depth+1));
					i = newI+1;
					continue;
				}

				newNode.AddToken(token);
			}

			endIndex = -1;
			return newNode;
		}
	}
}
