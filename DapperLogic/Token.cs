using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DapperLogic {
	public class Token : IEquatable<Token> {
		private Operator _op = null;
		public Operator Operator { get => _op; set => _UpdateOperator(value); }

		public string Symbol { get; private set; } = "";
		public bool Value {
			get => IsNegative ? !_value : _value;
			set => _value = IsNegative ? !value : value;
		}
		private bool _value = false;
		public bool IsNegative { get; } = false;

		public Token(string symbol, Operator op = null, bool value = false) {
			Symbol = symbol;
			if (symbol.StartsWith("~")) {
				IsNegative = true;
			}
			Operator = op;
			Value = value;
		}
		public Token(Operator op) : this(op.Symbol, op) { }
		public Token(string sym) : this(sym, Ops.GetOrNull(sym)) { }

		public bool IsOperator => Operator != null;
		public override string ToString() => Symbol;
		private void _UpdateOperator(Operator newOp) {
			if (newOp == null) return;

			_op = newOp;
			Symbol = _op.Symbol;
		}

		public bool Equals(Token other) {
			if (ReferenceEquals(this, null) && ReferenceEquals(other, null)) return true;
			if (ReferenceEquals(this, null) || ReferenceEquals(other, null)) return false;

			if (ReferenceEquals(this, other)) return true;

			if (other.IsOperator && this.IsOperator) {
				if (other.Operator == this.Operator) {
					return true;
				}
			} else if (other.Symbol == this.Symbol) {
				return true;
			}

			return false;
		}

		public override bool Equals(object obj) {
			return Equals(obj as Token);
		}

		public override int GetHashCode() {
			return -1758840423 + EqualityComparer<string>.Default.GetHashCode(this.Symbol);
		}

		public static bool operator ==(Token left, Token right) => EqualityComparer<Token>.Default.Equals(left, right);
		public static bool operator !=(Token left, Token right) => !(left == right);
	}
}
