using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DapperLogic;
using Spectre.Console;

namespace ReversePolishTests {
	class UniqueOperands {
		private Semidex<Token, int> _tokenPositions = new Semidex<Token, int>();
		private SortedList<string, Token> _sortedSL = null;
		private SortedList<string, Token> _sortedRealSL = null;
		private Dictionary<Token, Token> _opposites = new Dictionary<Token, Token>();



		public Token this[int index] => AllTokens[index];
		public bool IgnoreNegatives { get; set; } = false;
		public string TruthString => _GetTruthString();
		public IList<Token> AllTokens => (_sortedSL ??= _GetSortedList()).Values;
		public IList<Token> UsefulTokens => (_sortedRealSL ??= _GetSortedRealList()).Values;
		public IList<Token> PositiveTokens => _sortedSL.Where(sl => !sl.Value.IsNegative).Select(sl => sl.Value).ToList();



		private SortedList<string, Token> _GetSortedList() {
			_sortedSL = new SortedList<string, Token>();
			for (int i = 0; i < _tokenPositions.LeftKeys.Count; i++) {
				Token token = _tokenPositions.LeftKeyAt(i);
				_sortedSL.Add(token.Symbol, token);
			}
			return _sortedSL;
		}

		private SortedList<string, Token> _GetSortedRealList() {
			_sortedSL = new SortedList<string, Token>();
			for (int i = 0; i < _tokenPositions.LeftKeys.Count; i++) {
				Token token = _tokenPositions.LeftKeyAt(i);
				_sortedSL.Add(token.Symbol, token);
			}
			return _sortedSL;
		}

		private Token _GetUnsortedToken(int index) {
			return _tokenPositions.LeftKeyAt(index);
		}

		private string _GetTruthString() {
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < _tokenPositions.LeftKeys.Count; i++) {
				Token token = AllTokens[i];
				sb.Append(token.Value ? "1" : "0");
			}
			return sb.ToString();
		}

		public void AddRangeNominal(List<Token> tokens) {
			foreach (var token in tokens) {
				AddNominal(token);
			}
		}

		public void AddNominal(Token token) {
			_Add(token, -1, out _);
		}

		public Token AddUseful(Token token) {
			_Add(token, _tokenPositions.RightKeys.Count, out Token addToken);
			return addToken;
		}

		private void _Add(Token token, int index, out Token addToken) {
			if (IgnoreNegatives && token.IsNegative) throw new Exception("A negative surrogate token (eg. ~ABC) was added, while IgnoreNegatives was on. Not cool, man.");

			if (!_tokenPositions.Contains(token)) {
				_tokenPositions.Add(token, index);

				if (!IgnoreNegatives) _AddOpposite(token);
				addToken = token;
				return;
			}
			if (_tokenPositions[token] == -1 && index > -1) {
				_MoveToken(token, index);
			}

			if (!IgnoreNegatives) _AddOpposite(token);
			addToken = _tokenPositions.LeftKeys.Where(k => k == token).First();
			return;
		}

		private void _MoveToken(Token token, int newIndex) {
			_tokenPositions.RemovePair(token, _tokenPositions[token]);
			_tokenPositions.Add(token, newIndex);
		}

		private void _AddOpposite(Token token) {
			string oppSymbol;
			if (token.IsNegative) {
				oppSymbol = token.Symbol.Substring(1);
			} else {
				oppSymbol = "~" + token.Symbol;
			}
			Token oppToken = _tokenPositions.LeftKeys.Where(t => t.Symbol == oppSymbol).FirstOrDefault();
			if (oppToken == null) {
				oppToken = new Token(oppSymbol);
				AddNominal(oppToken);
			}

			if (!_opposites.ContainsKey(token)) _opposites.Add(token, oppToken);
			if (!_opposites.ContainsKey(oppToken)) _opposites.Add(oppToken, token);
		}

		public Token GetOpposite(Token token) {
			return _opposites[token];
		}

		internal Table ReportUniqueOperandMap() {
			Table table = new Table();
			table.AddColumn("Sorted i");
			table.AddColumn("Actual i");
			//table.AddColumn("Wrong i");
			//table.AddColumn("Wronger i");

			for (int i = 0; i < AllTokens.Count(); i++) {
				Token token = AllTokens[i];
				int unsortedIndex = -1;
				Token unsorted = _tokenPositions.LeftKeys.Where((token, index) => { unsortedIndex = index; return index == i; }).First();
				table.AddRow(
					$"{i} - {AllTokens[i]}",
					$"{unsortedIndex} - {unsorted}"
				);
			}
			return table;
		}
	}
}
