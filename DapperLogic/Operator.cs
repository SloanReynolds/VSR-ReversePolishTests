using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DapperLogic {
	public static class Ops {
		public static LeftParens LeftParens = new LeftParens();
		public static RightParens RightParens = new RightParens();
		public static Not Not = new Not();
		public static Nand Nand = new Nand();
		public static And And = new And();
		public static Nor Nor = new Nor();
		public static Or Or = new Or();

		public static List<Operator> All = new List<Operator>() {
			Or,Nor,And,Nand,Not,LeftParens,RightParens
			//Or,And,LeftParens,RightParens
		};

		public static bool IsValid(char chr) {
			return All.Any(op => op == chr);
		}

		public static bool IsValid(string str) {
			return All.Any(op => op == str);
		}

		public static Operator GetOrNull(char chr) {
			return GetOrNull(chr.ToString());
		}
		public static Operator GetOrNull(string str) {
			return All.Where(op => op == str).FirstOrDefault();
		}

		public static Operator LowestOrder => All.OrderBy(op => op.Precedence).First();
	}

	public abstract class Operator {
		protected readonly List<string> _operatorNames = new List<string>();
		protected readonly int _order;
		public int Precedence => _order;
		public string Symbol => _operatorNames.First();
		public bool LeftToRightAssociative { get; }
		public bool RightToLeftAssociative => !LeftToRightAssociative;
		public bool SingleOperand { get; }

		public Operator(IEnumerable<string> str, int precedence, bool leftAssoc = true, bool singleOperand = false) {
			foreach (var st in str)
				_operatorNames.Add(st);
			_order = precedence;
			LeftToRightAssociative = leftAssoc;
			SingleOperand = singleOperand;
		}

		public Operator(string str, int precedence, bool leftAssoc = true, bool singleOperand = false) : this(new[] { str }, precedence, leftAssoc, singleOperand) { }

		public override string ToString() {
			return _operatorNames.First();
		}

		public bool IsHigherOrder(Operator op) {
			if (ReferenceEquals(op, null)) return false;
			return this.Precedence > op.Precedence;
		}

		public bool IsHigherOrder(string symbol) {
			return IsHigherOrder(Ops.GetOrNull(symbol));
		}

		//<> Comparison
		public static bool operator <(Operator left, Operator right) => left.IsHigherOrder(right);
		public static bool operator >(Operator left, Operator right) => left.Precedence > right.Precedence;
		public static bool operator <=(Operator left, Operator right) => left.Precedence <= right.Precedence;
		public static bool operator >=(Operator left, Operator right) => left.Precedence >= right.Precedence;

		//String Comparison
		public static bool operator ==(Operator left, string right) {
			if (ReferenceEquals(left, null) && string.IsNullOrEmpty(right)) return true;
			if (ReferenceEquals(left, null)) return false;
			if (string.IsNullOrEmpty(right)) return false;
			if (left._operatorNames.Contains(right)) return true;
			return false;
		}
		public static bool operator !=(Operator left, string right) => !(left == right);
		public static bool operator ==(string left, Operator right) => right == left;
		public static bool operator !=(string left, Operator right) => right != left;

		public static bool operator ==(Operator left, char right) => left == right.ToString();
		public static bool operator !=(Operator left, char right) => !(left == right);
		public static bool operator ==(char left, Operator right) => right == left;
		public static bool operator !=(char left, Operator right) => right != left;

		public abstract void Eval(Stack<bool> stack);

		public override bool Equals(object obj) {
			return obj is Operator op &&
				   this.Symbol == op.Symbol;
		}

		public override int GetHashCode() {
			return -1758840423 + EqualityComparer<string>.Default.GetHashCode(this.Symbol);
		}
	}

	public class Not : Operator {
		public Not() : base("!", 5, singleOperand: true) { }

		public override void Eval(Stack<bool> stack) {
			stack.Push(!stack.Pop());
		}



		//internal void Eval(Stack<Token> stack) {
		//	//I don't think this is used... :(
		//	Token sym = stack.Pop();
		//	if (!sym.IsOperator) {
		//		sym.Value = !sym.Value;
		//		stack.Push(sym);
		//		return;
		//	}

		//	Operator notted = GetNottedOp(sym.Operator);
		//	if (notted == null) return; //Are we good here then...? Two nots cancel afterall...?
		//	sym.Operator = notted;
		//	stack.Push(sym);
		//}

		public static Operator GetNottedOp(Operator pIn) {
			if (pIn is Not) { return null; }
			if (pIn is Or) { return Ops.Nor; }
			if (pIn is And) { return Ops.Nand; }
			if (pIn is Nor) { return Ops.Or; }
			if (pIn is Nand) { return Ops.And; }

			throw new Exception("meep not");
		}

		public static Operator GetNottedOpForDistribute(Operator pIn, out bool doDistribute) {
			doDistribute = true;
			if (pIn is And) { return Ops.Or; }
			if (pIn is Or) { return Ops.And; }

			doDistribute = false;
			return GetNottedOp(pIn);

			throw new Exception("meep not");
		}
	}

	public class Nand : Operator {
		public Nand() : base("@", 4) { }

		public override void Eval(Stack<bool> stack) {
			stack.Push(!(stack.Pop() & stack.Pop()));
		}
	}

	public class And : Operator {
		public And() : base(new[] { "&", "+" }, 3) { }

		public override void Eval(Stack<bool> stack) {
			stack.Push(stack.Pop() & stack.Pop());
		}
	}

	public class Nor : Operator {
		public Nor() : base(":", 2) { }

		public override void Eval(Stack<bool> stack) {
			stack.Push(!(stack.Pop() | stack.Pop()));
		}
	}

	public class Or : Operator {
		public Or() : base("|", 1) { }

		public override void Eval(Stack<bool> stack) {
			stack.Push(stack.Pop() | stack.Pop());
		}
	}

	public class LeftParens : Operator {
		public LeftParens() : base("(", 0) { }

		public override void Eval(Stack<bool> stack) {
			throw new Exception("LeftParens attempted bool evaluation. How did it even get postfix'd?");
		}
	}

	public class RightParens : Operator {
		public RightParens() : base(")", 0) { }

		public override void Eval(Stack<bool> stack) {
			throw new Exception("RightParens attempted bool evaluation. How did it even get postfix'd?");
		}
	}
}
