using System;
using System.Collections.Generic;
using System.Linq;
using DapperLogic;

namespace ReversePolishTests {
	public class PostfixSimplificator_String {
		public static string Simplify(string postfix) {
			PostfixSimplificator_String pfs = new PostfixSimplificator_String(postfix);
			pfs.Prune();
			return pfs.GetPostfix();
		}

		public static string InfixSimplifyInfix(string infix) {
			string postfix = InfixHelper.ToPostfix(infix);
			string simpost = Simplify(postfix);
			string simpinf = PostfixHelper.ToInfix(simpost);
			return simpinf;
		}

		private string _postfix = "";
		private PFSNode _rootNode;

		public PostfixSimplificator_String(IEnumerable<string> postfix) : this(string.Join(" ", postfix.ToArray())) { }

		public PostfixSimplificator_String(string postfix) {
			_postfix = postfix;
			List<string> prefixList = _postfix.Split(' ').Reverse().ToList();
			_rootNode = new PFSNode(prefixList);
		}

		public void Prune() {
			_rootNode.Prune();
		}

		public void Print() {
			_rootNode.Print();
		}

		public string GetPostfix() {
			return string.Join(" ", _rootNode.Postfix().ToArray());
		}

		private class PFSNode : IComparable<PFSNode> {
			public List<PFSNode> Nodes = new List<PFSNode>();
			public OpType OpType = OpType.OPERAND;
			public string OperandValue = "";
			public bool MarkedForDeath = false;
			public string Name => OpType == OpType.OPERAND ? OperandValue : OpType.ToString();

			public PFSNode(string operand) {
				OperandValue = operand;
			}

			public PFSNode(List<string> progenitor) {
				if (progenitor.Count == 1) {
					OperandValue = progenitor[0];
					return;
				}
				List<int> opDepth = _CalculateOpDepths(progenitor);

				OpType = _OpTypeOf(progenitor[0]);

				List<string> newList = null;
				int phraseCount = 0;
				_WriteLine("Progenitor: " + string.Join(" ", progenitor.ToArray()));
				for (int i = 0; i < progenitor.Count; i++) {
					string token = progenitor[i];
					if (opDepth[i] == 0) {
						//opDepth is necessary as it shows us which
						//nodes belong in the current depth... duh?
						AddListToNodes();

						if (_OpTypeOf(token) == OpType) {
							//Do nothing
						} else {
							if (_OpTypeOf(token) != OpType.OPERAND) throw new Exception("rock lobster?");

							//It's an operand!
							AddOperandToNodes(token);
						}
					} else {
						//If we aren't in the same depth as the rest,
						//the operator sign must have switched on us,
						// and we should substitute this "operand" as
						// a whole new Node of its own. Dontcha know.
						if (newList == null) {
							newList = new List<string>();
							phraseCount = 0;    //This variable tracks the difference between
												//  operators and operands, starting at 1 for
												// the current operator. If at any point this
												//   number goes negative, we KNOW that we've
												//  reached the end of the "phrase"/fam tree.
												// + A | A B <=> 2 operator - 3 operands = -1 [QED]
						}
						newList.Add(token);
						if (_OpTypeOf(token) == OpType.OPERAND) {
							phraseCount--;
						} else {
							phraseCount++;
						}

						if (phraseCount == -1) {
							AddListToNodes();
						}
					}
				}
				AddListToNodes();
				Nodes.Sort();

				void AddListToNodes() {
					if (newList != null) {
						//List is done; add this new list as a new node.
						PFSNode newNode = new PFSNode(newList);

						if (newNode.Nodes.Count > 1) {
							Nodes.Add(newNode);
						} else {
							//The rest of this node must have collapsed, whether | or +...
							//Either way, we can just add it as an operand for the parent.
							AddOperandToNodes(newNode.Nodes[0].OperandValue);
						}
						newList = null;
					}
				}

				void AddOperandToNodes(string token) {
					bool contains = Nodes.Select(node => node.OperandValue).Contains(token);
					if (contains) {
						_WriteLine($"Already Contains {token}");
					} else {
						_WriteLine($"Add Operand: {token}");
						Nodes.Add(new PFSNode(token));
					}
				}
			}

			public void Print(string depth = "") {
				if (this.OpType == OpType.OPERAND) {
					_PrintLine($"{OperandValue}");
					return;
				}

				List<string> thing = new List<string>();
				int count = 0;

				foreach (PFSNode node in Nodes) {
					if (node.OpType == OpType.OPERAND) {
						thing.Add($"{node.OperandValue}");
					} else {
						thing.Add($"#{count}");
						count++;
					}
				}
				_PrintLine(string.Join(OpType == OpType.OR ? " | " : " + ", thing.ToArray()));

				depth = depth + "==";
				count = 0;
				foreach (PFSNode node in Nodes) {
					if (node.OpType != OpType.OPERAND) {
						_Print($"{depth}#{count} : ");
						node.Print(depth);
						count++;
					}
				}
			}

			private void _PrintLine(string msg = "") {
				Console.WriteLine(msg);
			}
			private void _Print(string msg = "") {
				Console.Write(msg);
			}

			private void _WriteLine(string msg = "") {
				//Console.WriteLine(">" + msg);
			}
			private void _Write(string msg = "") {
				//Console.Write(">" + msg);
			}

			private List<int> _CalculateOpDepths(List<string> prefixList) {
				List<int> newList = new List<int>();
				for (int i = 0; i < prefixList.Count; i++) {
					newList.Add(-1);
				}

				for (int i = prefixList.Count - 1; i >= 0; i--) {
					if (newList[i] == -1)
						newList[i] = _GetOpDepth(i);
				}

				return newList;

				int _GetOpDepth(int i) {
					if (i == 0) return 0;
					int add = 0;

					OpType otMe = _OpTypeOf(prefixList[i]);
					int parentInd = _FindParentOf2(i);
					if (otMe != OpType.OPERAND && otMe != _OpTypeOf(prefixList[parentInd])) {
						add = 1;
					}

					if (newList[parentInd] > -1) return newList[parentInd] + add;

					return _GetOpDepth(parentInd) + add;
				}

				int _FindParentOf2(int ind) {
					List<int> debug = new List<int>();
					for (int i = 0; i < ind; i++) {
						debug.Add(0);
					}

					int total = 0;
					if (ind <= 0) return 0;

					while (ind > 0) {
						ind--;

						if (_TokenIsOperator(prefixList[ind])) {
							total--;
						} else {
							total++;
						}
						debug[ind] = total;

						if (total <= 0) {
							return ind;
						}
					}

					throw new Exception("Nope");
				}

				bool _TokenIsOperator(string token) {
					if (_OpTypeOf(token) == OpType.OPERAND)
						return false;
					return true;
				}
			}

			OpType _OpTypeOf(string token) {
				if (token == "|")
					return OpType.OR;
				if (token == "+" || token == "&")
					return OpType.AND;
				return OpType.OPERAND;
			}

			internal void Prune(Dictionary<string, bool> inferences = null) {
				_WriteLine($"Prune on {Name} Nodes: {Nodes.Count}");
				if (inferences == null) {
					inferences = new Dictionary<string, bool>();
				}
				List<string> addedKeys = new List<string>();
				bool opIsAnd = false;
				if (this.OpType == OpType.AND) {
					opIsAnd = true;
				}

				foreach (PFSNode node in Nodes.Where(node => node.OpType == OpType.OPERAND)) {
					if (inferences.TryGetValue(node.OperandValue, out bool tokenIsTrue)) {
						//Snip?
						if ((tokenIsTrue && opIsAnd) || (!tokenIsTrue && !opIsAnd)) {
							//Operator match; 1 node destroy.
							node.MarkedForDeath = true;
							_WriteLine($"  Remove node: {node.Name}; 1 node");
						} else {
							//Operator mismatch; 2 node destroy.
							this.MarkedForDeath = true;
							_WriteLine($"  Remove node: {node.Name}; 2 nodes");
							_WriteLine($"    Remove node: {this.Name}");
							break;
						}
					} else {
						inferences.Add(node.OperandValue, opIsAnd);
						addedKeys.Add(node.OperandValue);
					}
				}

				if (!this.MarkedForDeath) {
					foreach (PFSNode node in Nodes.Where(node => node.OpType != OpType.OPERAND)) {
						node.Prune(inferences);
					}
				}

				_WriteLine($"  Finished prune on {(OpType == OpType.OPERAND ? OperandValue : OpType.ToString())} Nodes: {Nodes.Count}\n    Cleaning up.");
				Nodes.RemoveAll(node => {
					return node.MarkedForDeath;
				});
				if (Nodes.Count == 0) {
					this.MarkedForDeath = true;

					_WriteLine("!!!!  0 node death mark!");
				} else if (Nodes.Count == 1) {
					//Transformation type kawaiii
					this.OperandValue = Nodes[0].OperandValue;
					this.OpType = Nodes[0].OpType;
					this.Nodes = Nodes[0].Nodes;

					_WriteLine("!!!!  1 node transform!");
				}
				foreach (string key in addedKeys) {
					inferences.Remove(key);
				}
			}

			private List<string> _Prefix() {
				if (this.OpType == OpType.OPERAND) {
					return new List<string> { this.OperandValue };
				}

				List<string> prefix = new List<string>();
				int recentOp = -1;
				foreach (PFSNode node in Nodes) {
					recentOp = prefix.Count();
					prefix.Add(this.OpType == OpType.OR ? "|" : "+");
					if (node.OpType == OpType.OPERAND) {
						prefix.Add(node.OperandValue);
					} else {
						prefix.AddRange(node._Prefix());
					}
				}

				prefix.RemoveAt(recentOp);
				return prefix;
			}

			public List<string> Postfix() {
				return _Prefix().Reverse<string>().ToList();
			}

			public int CompareTo(PFSNode other) {
				if (this.OpType == OpType.OPERAND && other.OpType == OpType.OPERAND) {
					return this.OperandValue.CompareTo(other.OperandValue);
				}

				return this.OpType - other.OpType;
				// AND - OR  = 1
				// AND - OP  = 2

				// OR  - AND = -1
				// OR  - OP  = 1

				// OP  - AND = -2
				// OP  - OR  = -1
			}
		}
	}

	internal enum OpType {
		OPERAND,
		OR,
		AND
	}
}
