using System;
using System.Collections.Generic;
using System.Linq;
using DapperLogic;

namespace ReversePolishTests {
	public class PostfixSimplificator {
		public static PostfixLogic Simplify(PostfixLogic postfix) {
			PostfixSimplificator pfs = new PostfixSimplificator(postfix);
			pfs.Prune();
			return pfs.GetPostfix();
		}

		public static List<Token> SimplifyToInfixList(PostfixLogic postfix) {
			return PostfixHelper.ToInfixList(Simplify(postfix));
		}

		public static string SimplifyToInfix(PostfixLogic postfix) {
			return string.Join(" ", SimplifyToInfixList(postfix));
		}

		public static string InfixSimplifyToInfix(string infix) {
			PostfixLogic postfix = InfixHelper.ToPostfixLogic(infix);
			return SimplifyToInfix(postfix);
		}



		private PostfixLogic _postfix;
		private PFSNode _rootNode;

		public PostfixSimplificator(PostfixLogic postfix) {
			_postfix = new(postfix);
			PostfixHelper.ConvertNotsToNegativeSymbols(ref _postfix);
			List<Token> prefixList = _postfix;
			prefixList.Reverse();
			_rootNode = new PFSNode(prefixList);
		}

		public void Prune() {
			_rootNode.Combine();
			_rootNode.Prune();
		}

		public void Print() {
			_rootNode.Print();
		}

		public PostfixLogic GetPostfix() {
			return _rootNode.Postfix();
		}

		private class PFSNode : IComparable<PFSNode> {
			public List<PFSNode> Nodes = new List<PFSNode>();
			public Token Token;
			public bool MarkedForDeath = false;
			public string Name => Token.ToString();
			public override string ToString() {
				return Name;
			}

			public PFSNode(Token operand) {
				//Doesn't have to be an operand, but it usually is...
				Token = operand;
			}

			public PFSNode(List<Token> phrase) {
				Token = phrase[0];

				if (phrase.Count == 1) {
					return;
				}
				List<int> opDepth = _CalculateOpDepths(phrase);

				List<Token> newList = null;
				int phraseCount = 0;
				_WriteLine("Progenitor: " + string.Join(" ", phrase));
				for (int i = 0; i < phrase.Count; i++) {
					Token token = phrase[i];
					if (opDepth[i] == 0) {
						//opDepth is necessary as it shows us which
						//nodes belong in the current depth... duh?
						AddListToNodes();

						if (token.IsOperator && token.Operator == this.Token.Operator) {
							//Do nothing
						} else {
							//It's an operand!
							AddOperandToNodes(token);
						}
					} else {
						//If we aren't in the same depth as the rest,
						//the operator sign must have switched on us,
						// and we should substitute this "operand" as
						// a whole new Node of its own. Dontcha know.
						if (newList == null) {
							newList = new List<Token>();
							phraseCount = 0;    //This variable tracks the difference between
												//  operators and operands, starting at 1 for
												// the current operator. If at any point this
												//   number goes negative, we KNOW that we've
												//  reached the end of the "phrase"/fam tree.
												// + A | A B <=> 2 operator - 3 operands = -1 [QED]
						}
						newList.Add(token);
						if (!token.IsOperator) {
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
							AddOperandToNodes(newNode.Nodes[0].Token);
						}
						newList = null;
					}
				}

				void AddOperandToNodes(Token token) {
					bool contains = Nodes.Select(node => node.Token.Symbol).Contains(token.Symbol);
					if (contains) {
						_WriteLine($"Already Contains {token}");
					} else {
						_WriteLine($"Add Operand: {token}");
						Nodes.Add(new PFSNode(token));
					}
				}
			}

			public void Print(string depth = "") {
				if (!this.Token.IsOperator) {
					_PrintLine($" {Token} ");
					return;
				}

				List<string> thing = new List<string>();
				int count = 0;

				foreach (PFSNode node in Nodes) {
					if (!node.Token.IsOperator) {
						thing.Add($" {node.Token} ");
					} else {
						thing.Add($"#{count}");
						count++;
					}
				}
				_PrintLine(string.Join(this.Token.Operator.ToString(), thing.ToArray()));

				depth = depth + "==";
				count = 0;
				foreach (PFSNode node in Nodes) {
					if (node.Token.IsOperator) {
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

			private List<int> _CalculateOpDepths(List<Token> prefixList) {
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
					Token token = prefixList[i];
					if (i == 0) return 0;
					int add = 0;

					int parentInd = _FindParentOf2(i);
					if (token.IsOperator && token.Operator != prefixList[parentInd].Operator) {
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

						if (prefixList[ind].IsOperator) {
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
			}

			internal void Prune(Dictionary<string, bool> inferences = null) {
				if (this.Token.IsOperator && this.Token.Operator != Ops.Or && this.Token.Operator != Ops.And) {
					throw new NotImplementedException("Other types of Operators besides AND and OR aren't implemented yet!");
				}

				_WriteLine($"Prune on {Name} Nodes: {Nodes.Count}");
				if (inferences == null) {
					inferences = new Dictionary<string, bool>();
				}
				List<string> addedKeys = new List<string>();
				bool opIsAnd = false;
				if (this.Token.Operator == Ops.And) {
					opIsAnd = true;
				}

				foreach (PFSNode node in Nodes.Where(node => !node.Token.IsOperator)) {
					if (inferences.TryGetValue(node.Token.Symbol, out bool tokenIsTrue)) {
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
						inferences.Add(node.Token.Symbol, opIsAnd);
						addedKeys.Add(node.Token.Symbol);
					}
				}

				if (!this.MarkedForDeath) {
					foreach (PFSNode node in Nodes.Where(node => node.Token.IsOperator)) {
						node.Prune(inferences);
					}
				}

				_WriteLine($"  Finished prune on {Token} Nodes: {Nodes.Count}\n    Cleaning up.");
				Nodes.RemoveAll(node => {
					return node.MarkedForDeath;
				});
				if (Nodes.Count == 0) {
					this.MarkedForDeath = true;

					_WriteLine("!!!!  0 node death mark!");
				} else if (Nodes.Count == 1) {
					//Transformation type kawaiii
					this.Token = Nodes[0].Token;
					this.Nodes = Nodes[0].Nodes;

					_WriteLine("!!!!  1 node transform!");
				}
				foreach (string key in addedKeys) {
					inferences.Remove(key);
				}
			}

			private List<Token> _Prefix() {
				if (!this.Token.IsOperator) {
					return new List<Token> { this.Token };
				}

				List<Token> prefix = new List<Token>();
				int recentOp = -1;
				foreach (PFSNode node in Nodes) {
					recentOp = prefix.Count();
					prefix.Add(this.Token);
					if (!node.Token.IsOperator) {
						prefix.Add(node.Token);
					} else {
						prefix.AddRange(node._Prefix());
					}
				}

				if (recentOp >= 0)
					prefix.RemoveAt(recentOp);
				return prefix;
			}

			public PostfixLogic Postfix() {
				return new PostfixLogic(_Prefix().Reverse<Token>().ToList());
			}

			public int CompareTo(PFSNode other) {
				if (!this.Token.IsOperator && !other.Token.IsOperator) {
					return this.Token.Symbol.CompareTo(other.Token.Symbol);
				}

				int thisScore = this.Token.IsOperator ? this.Token.Operator.Precedence : 0;
				int otherScore = other.Token.IsOperator ? other.Token.Operator.Precedence : 0;

				return thisScore - otherScore;
				// AND - OR  = 1
				// AND - OP  = 2

				// OR  - AND = -1
				// OR  - OP  = 1

				// OP  - AND = -2
				// OP  - OR  = -1
			}

			internal void Combine() {
				if (this.Token.IsOperator && this.Token.Operator != Ops.Or && this.Token.Operator != Ops.And && this.Token.Operator != Ops.Not) {
					throw new NotImplementedException("Other types of Operators besides OR/AND/NOT aren't implemented!");
				}

				if (this.Token.IsOperator && this.Token.Operator == Ops.Or) {
					bool iterate = false;
					do {
						iterate = false;
						//This dictionary will be filled with all grandchild symbols with references to each of the CHILDREN they live under.
						Dictionary<string, List<PFSNode>> symbols = new Dictionary<string, List<PFSNode>>();

						//Find all grandchildren operands
						foreach (PFSNode child in Nodes) {
							if (child.Token.IsOperator && child.Nodes.Count < 1) {
								child.MarkedForDeath = true;
								continue;
							}
							foreach (PFSNode grandNode in child.Nodes.OrderByDescending(n => n.Token.IsOperator)) {
								if (grandNode.Token.IsOperator) {
									grandNode.Combine();
									continue;
								}

								if (!symbols.ContainsKey(grandNode.Token.Symbol)) {
									symbols.Add(grandNode.Token.Symbol, new List<PFSNode>());
								}
								symbols[grandNode.Token.Symbol].Add(child);
							}
						}
						this.Nodes.RemoveAll(n => n.MarkedForDeath);

						if (symbols.Any(kvp => kvp.Value.Count > 1)) {
							//Any symbol that shows up in multiple Nodes
							// The best symbol to pick would be the symbol that shows up in:
							//   A) The most nodes, but if there's a tie:
							//   B) The nodes with the longest lists??? Am I even doing this?
							iterate = true;

							List<PFSNode> each = symbols.OrderByDescending(k => k.Value.Count).SelectMany(kvp => kvp.Value).Distinct().ToList();
							(int, int) bestPair = (0, 1);
							int bestCount = 0;
							for (int i = 0; i < each.Count; i++) {
								var iNodes = each[i].Nodes.Where(n => !n.Token.IsOperator);
								if (iNodes.Count() <= bestCount) {
									continue; //No way this could beat the champ, might as well skip it.
								}
								for (int j = i + 1; j < each.Count; j++) {
									var jNodes = each[j].Nodes.Where(n => !n.Token.IsOperator);
									if (jNodes.Count() <= bestCount) {
										continue; //No way this could beat the champ, might as well skip it.
									}
									var union = iNodes.Select(n => n.Name).Intersect(jNodes.Select(n => n.Name));
									if (union.Count() > bestCount) {
										bestPair = (i, j);
										bestCount = union.Count();
									}
								}
							}

							KeyValuePair<string, List<PFSNode>> item = symbols.Where(k => k.Value.Contains(each[bestPair.Item1]) && k.Value.Contains(each[bestPair.Item2])).OrderByDescending(k => k.Value.Count).First();

							string symbol = item.Key;
							List<PFSNode> children = item.Value;

							PFSNode newAnd = new PFSNode(new Token(Ops.And));
							this.Nodes.Add(newAnd);
							PFSNode newOr = new PFSNode(new Token(Ops.Or));
							newAnd.Nodes.Add(new PFSNode(new Token(symbol)));
							newAnd.Nodes.Add(newOr);
							foreach (PFSNode child in children) {
								this.Nodes.Remove(child);
								newOr.Nodes.Add(child);
								child.Nodes.RemoveAll(gc => gc.Token.Symbol == symbol);
							}
							//Death here?
							if (this.Nodes.Count == 1) {
								this.Token = Nodes[0].Token;
								this.Nodes = Nodes[0].Nodes;

								foreach (var newChild in this.Nodes) {
									newChild.Combine();
								}

								return;
							}
						}
					} while (iterate);
				}
			}
		}
	}
}
