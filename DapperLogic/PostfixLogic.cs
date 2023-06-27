using System;
using System.Collections.Generic;
using System.Linq;

namespace DapperLogic {
	public class PostfixLogic : List<Token> {
		private int[] _tokenDepths = null;
		public int[] TokenDepths => _tokenDepths ??= _GetTokenDepths();

		private NodeLogic _nodeLogic = null;
		public NodeLogic NodeLogic => _nodeLogic ??= NodeHelper.FromPostfixLogic(this);

		private HashSet<Token> _unique = new();





		public PostfixLogic() : base() { }
		public PostfixLogic(int capacity) : base(capacity) { }
		public PostfixLogic(PostfixLogic postfixLogic) : this((List<Token>) postfixLogic) {}
		public PostfixLogic(List<Token> postfixTokens) {
			AddRange(postfixTokens);
		}

		public new void AddRange(IEnumerable<Token> tokens) {
			foreach (var token in tokens) {
				Add(token);
			}
		}

		public new void Add(Token token) {
			base.Add(_AddGetUnique(token));
		}

		public new void Insert(int i, Token token) {
			base.Insert(i, _AddGetUnique(token));
		}

		private Token _AddGetUnique(Token token) {
			if (!_unique.Contains(token)) {
				_unique.Add(token);
			}
			return _unique.First(u => u == token);
		}










		private int[] _GetTokenDepths() {
			//    f a | b c & d | w | |
			//    2 2 1 4 4 3 3 2 2 1 0
			//| 0                     0   1  earlyPeek + 1
			//| 1                   1 0   2  earlyPeek + 1
			//w 2                 2 1 0   2  latePeek
			//| 2               2~2 1 0   3  earlyPeek + 1
			//d 3             3     1 0   3  latePeek
			//& 3           3~3     1 0   4  earlyPeek + 1
			//c 4         4         1 0   4  latePeek
			//b 4       4~4         1 0   1  latePeek
			//| 1     1~~~~~~~~~~~~~1 0   2  earlyPeek + 1
			//a 2   2                 0   2  latePeek
			//f 2 2~2                 0   0  latePeek
			int[] opDepths = new int[this.Count];
			int ubound = this.Count - 1;

			int depth = 0;
			Stack<int> stack = new();

			for (int i = ubound; i >= 0; i--) {
				Token token = this[i];

				//Add depth to list before stack push/annihilate
				opDepths[i] = depth;
				_stackPush(depth);
				if (token.IsOperator) {
					//earlyPeek + 1 - after first push/annihilate
					depth = opDepths[i] + 1;
					continue;
				}

				//latePeek - expressly after stack push/annihilate
				depth = stack.Peek();
				{ }
			}

			void _stackPush(int num) {
				if (stack.Count > 0 && stack.Peek() == num) {
					stack.Pop();
					return;
				}
				stack.Push(num);
			}

			return opDepths;
		}
	}
}