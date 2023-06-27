using System;
using System.Collections.Generic;
using System.Diagnostics;

public class BNode {
	public enum NodeType {
		OPERAND,
		AND,
		OR
	}

	public NodeType Type { get; private set; } = NodeType.OPERAND;

	private string _value;
	public string Value {
		get => _value;
		set {
			_value = value;

			if (_value == "+" || _value == "&") {
				Type = NodeType.AND;
			} else if (_value == "|") {
				Type = NodeType.OR;
			}
		}
	}

	public BNode Right = null;
	public BNode Left = null;
	public BNode Parent = null;

	public string Name => Value + "." + Type;

	public BNode(string val, BNode par = null) {
		this.Value = val;
		this.Parent = par;
	}

	public BNode Add(string val) {
		if (Parent != null && _IsFull()) {
			return Parent.Add(val);
		}

		if (Left == null) return Left = new BNode(val, this);
		if (Right == null) return Right = new BNode(val, this);

		throw new NotImplementedException();
	}

	public void Sort() {
		//ORs go on the left
		//ANDs go to the right
		//Everything else, who cares?

		if (Left.Type != NodeType.OPERAND) {
			Left.Sort();
		}
		if (Right.Type != NodeType.OPERAND) {
			Right.Sort();
		}

		//if (Left.Type != Right.Type && (Left.Type == NodeType.AND || Right.Type == NodeType.OR)) {
		//		0	|	+
		//	0	00	0|	0+
		//	|	|0	||	|+
		//	+	+0	+|	++

		// 123, 456, 789
		// SWAP ON: 4 7 8
		//I want Operands on the left.

		if ((Left.Type != Right.Type) && (Right.Type == NodeType.OPERAND || Left.Type == NodeType.AND)) {
			//Swap me baby!
			BNode temp = Right;
			Right = Left;
			Left = temp;
		}
	}

	public void Prune(List<string> snipList = null, string breadCrumbs = "") {
		if (snipList == null) {
			snipList = new List<string>();
		}
		breadCrumbs += (breadCrumbs == "" ? "" : " ") + Value;

		//Check to see if any legs are operands.
		//	Are they BOTH operands?
		//	If not I think we pass on the operand dude to the recurse the butthole after we stuff

		//Sibling Pruning happens automatically since we are using a Polish Tree... Right, that's a term; totally.

		string leftNewKey = _LegPruner(Left, snipList, breadCrumbs);

		if (!_destroyed) {
			_LegPruner(Right, snipList, breadCrumbs);
		}

		if (leftNewKey != string.Empty) {
			snipList.Remove(leftNewKey);
		}
	}










	private void _WriteLine(string msg) {
		Debug.WriteLine(msg);
	}

	private string _LegPruner(BNode node, List<string> snipList, string breadCrumbs = "") {
		bool isLeftLeg = node == this.Left;
		_WriteLine($"{breadCrumbs.Replace(" ", "")}.{(isLeftLeg ? "Left" : "Right")} = {node}");
		if (node.Type == NodeType.OPERAND) {
			bool snipOr = snipList.Contains("|" + node.Value);
			bool snipAnd = snipList.Contains("+" + node.Value);

			if (snipOr || snipAnd) {
				_WriteLine($"    ==> DESTROY {this.Name}");
				if ((snipOr && this.Type == NodeType.OR) ||
					(snipAnd && this.Type == NodeType.AND)) {
					//Only 1 node destruction
					_WriteLine($"        1 node");
					this._Collapse(node);
				} else {
					//2 node destruction...
					_WriteLine($"        2 nodes");
					this._Destroy();
				}
			} else if (isLeftLeg || true) {
				_WriteLine($"    ==> Watchlist");
				//We can infer the evaluation result based on MY operator type.
				//	eg. if I am of type AND, then Left would have to be true, or we could ignore Right entirely.
				//		if I am of type OR, then Left would have to be false, or we could ignore Right entirely.
				string snipName = (this.Type == NodeType.AND ? "+" : "|") + node.Value;
				snipList.Add(snipName);

				_WriteLine($"        {string.Join(", ", snipList)}");
				return snipName;
			}
		} else {
			_WriteLine($"    ==> Prune Leg");
			node.Prune(snipList, breadCrumbs);
		}

		return string.Empty;
	}

	private void _Destroy() {
		_WriteLine($"        {this.Name}.Collapse()");

		Parent._Collapse(this);
		this._destroyed = true; //Destroy so we don't keep "pruning" the old stuff!
	}

	private bool _destroyed = false;
	private void _Collapse(BNode doomed) {
		_WriteLine($"        {this.Name}.CollapseFrom({doomed.Name})");
		_WriteLine($"        {doomed == this.Left}");

		//Grab the extant leg
		BNode extant = (doomed == this.Left) ? this.Right : this.Left;

		if (Parent == null) {
			this.Value = extant.Value;
		} else {
			Parent._RedirectLeg(this, extant);
		}
	}

	private void _RedirectLeg(BNode old, BNode target) {
		_WriteLine($"        {this.Name}._RedirectLeg({old.Name}, {target.Name})");

		if (old == this.Left) {
			this.Left = target;
		} else {
			this.Right = target;
		}
	}

	private bool _IsFull() {
		if (Type == NodeType.OPERAND) {
			return true;
		}

		if (Left == null) return false;
		if (Right == null) return false;

		return true;
	}






	public override string ToString() {
		return Value;
	}
}























public class BTree {
	private BNode _root;
	private BNode _head;
	private IComparer<int> _comparer = Comparer<int>.Default;

	public BTree() {
		_root = null;
	}

	private static Dictionary<string, string> symbols = new Dictionary<string, string>();

	public BTree(string postfix, bool withReplacement = true) {
		string[] postfixArr = postfix.Split(' ');

		for (int i = postfixArr.Length - 1; i >= 0; i--) {
			string sym = postfixArr[i];
			if (withReplacement) {
				string replace = sym;
				if (sym.Length > 1) {
					if (symbols.TryGetValue(sym, out replace)) {
						//
					} else {
						replace = ((char)(symbols.Count + 65)).ToString();
						symbols.Add(sym, replace);
						Console.WriteLine($"{replace} = {sym}");
					}
				}
				this.Add(replace);
			} else {
				this.Add(sym);
			}
		}
	}

	public BNode Add(int val) {
		return Add(val.ToString());
	}

	public BNode Add(string val) {
		BNode newN = new BNode(val);
		if (_root == null) {
			_root = newN;
			_head = _root;
		} else {
			_head = _head.Add(val);
		}

		return null;
	}

	public void Print(string highlight = "") {
		Print(_root, 4, highlight);
	}

	public void Print(BNode p, int padding, string highlight = "") {

		BTreePrinter.Print(p, padding, highlight: highlight);

		return;
	}

	public void Sort() {
		_root.Sort();
	}

	public void Prune() {
		_root.Prune();
	}
}





























public static class BTreePrinter {
	class NodeInfo {
		public BNode Node;
		public string Text;
		public int StartPos;
		public int Size { get { return Text.Length; } }
		public int EndPos { get { return StartPos + Size; } set { StartPos = value - Size; } }
		public NodeInfo Parent, Left, Right;
	}

	public static void Print(this BNode root, int topMargin = 2, int leftMargin = 2, string highlight = "") {
		if (root == null) return;
		int rootTop = Console.CursorTop + topMargin;
		var last = new List<NodeInfo>();
		var next = root;
		string[] crumbs = highlight.Split(' ');
		for (int level = 0; next != null; level++) {
			var item = new NodeInfo { Node = next, Text = $" {next.Value} " };
			if (crumbs.Length - 1 >= level && crumbs[level] == item.Text) {
				item.Text = $"*{item.Text}*";
			}
			if (level < last.Count) {
				item.StartPos = last[level].EndPos + 1;
				last[level] = item;
			} else {
				item.StartPos = leftMargin;
				last.Add(item);
			}
			if (level > 0) {
				item.Parent = last[level - 1];
				if (next == item.Parent.Node.Left) {
					item.Parent.Left = item;
					item.EndPos = Math.Max(item.EndPos, item.Parent.StartPos);
				} else {
					item.Parent.Right = item;
					item.StartPos = Math.Max(item.StartPos, item.Parent.EndPos);
				}
			}
			next = next.Left ?? next.Right;
			for (; next == null; item = item.Parent) {
				Print(item, rootTop + 2 * level);
				if (--level < 0) break;
				if (item == item.Parent.Left) {
					item.Parent.StartPos = item.EndPos;
					next = item.Parent.Node.Right;
				} else {
					if (item.Parent.Left == null)
						item.Parent.EndPos = item.StartPos;
					else
						item.Parent.StartPos += (item.StartPos - item.Parent.EndPos) / 2;
				}
			}
		}
		Console.SetCursorPosition(0, rootTop + 2 * last.Count - 1);
	}

	private static void Print(NodeInfo item, int top) {
		SwapColors();
		Print(item.Text, top, item.StartPos);
		SwapColors();
		if (item.Left != null)
			PrintLink(top + 1, "┌", "┘", item.Left.StartPos + item.Left.Size / 2, item.StartPos);
		if (item.Right != null)
			PrintLink(top + 1, "└", "┐", item.EndPos - 1, item.Right.StartPos + item.Right.Size / 2);
	}

	private static void PrintLink(int top, string start, string end, int startPos, int endPos) {
		Print(start, top, startPos);
		Print("─", top, startPos + 1, endPos);
		Print(end, top, endPos);
	}

	private static void Print(string s, int top, int left, int right = -1) {
		if (Console.BufferWidth <= left) return;
		Console.SetCursorPosition(left, top);
		if (right < 0) right = left + s.Length;
		while (Console.CursorLeft < right) {
			if (Console.CursorLeft < left - 1) {
				return;
			}
			Console.Write(s);
		}
	}

	private static void SwapColors() {
		var color = Console.ForegroundColor;

		Console.ForegroundColor = Console.BackgroundColor;
		Console.BackgroundColor = color;
	}

}