using System.Collections.Generic;

namespace DapperLogic {
	public class PositiveUniqueOperands {
		public Token this[int index] => Tokens[index];
		public IList<Token> Tokens => (_sortedSL ??= _GetSortedList()).Values;
		private SortedList<string, Token> _sortedSL = null;
		private List<Token> _uniques = new();

		public Token AddUseful(Token token) {
			_Add(token, out Token addToken);
			return addToken;
		}

		private void _Add(Token token, out Token addToken) {
			if (!_uniques.Contains(token)) {
				_uniques.Add(token);

				addToken = token;
				return;
			}

			addToken = _uniques.Find(t => t == token);
			return;
		}

		private SortedList<string, Token> _GetSortedList() {
			_sortedSL = new SortedList<string, Token>();
			for (int i = 0; i < _uniques.Count; i++) {
				Token token = _uniques[i];
				_sortedSL.Add(token.Symbol, token);
			}
			return _sortedSL;
		}
	}
}
