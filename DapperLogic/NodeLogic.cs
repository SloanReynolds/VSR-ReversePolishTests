using System;
using System.Collections.Generic;
using System.Linq;
using DapperData;

namespace DapperLogic {
	public class NodeLogic {
		private readonly Node _root = null;
		public PostfixLogic PostfixLogic => _root.ToPostfixLogic();

		public NodeLogic(Node root) {
			_root = root;
		}

		public NodeLogic Combine(bool removePseudoNots = false) {
			_root.SetRemovePseudoNots(removePseudoNots);
			_root.Combine(_prioritizeSpecials, _priorityCombines);
			return this;
		}

		private bool _prioritizeSpecials = true;
		private List<string> _priorityCombines = new();
		public NodeLogic SetSpecialSymbols(bool prioritizeSpecials, string[] prioritisedSymbols) {
			_prioritizeSpecials = prioritizeSpecials;
			_priorityCombines = new List<string>(prioritisedSymbols);
			return this;
		}

		public abstract class Node {
			private PostfixLogic _postfixLogic = null;
			private int _startIndex = -1;
			private bool _removePseudoNots = false;

			public Operator Operator { get; }
			public int OpDepth { get; private set; } = -1;
			public List<Node> Nodes { get; private set; } = new();
			public List<Token> Tokens { get; private set; } = new();
			public int PostfixDepth => _postfixLogic.TokenDepths[_startIndex];

			public Node(Operator op) {
				this.Operator = op;
			}

			public Node(Operator op, int opDepth, PostfixLogic postfixLogic, int startIndex) {
				this.Operator = op;
				this.OpDepth = opDepth;
				this._postfixLogic = postfixLogic;
				this._startIndex = startIndex;
			}

			public void SetRemovePseudoNots(bool removePseudoNots) {
				_removePseudoNots = removePseudoNots;
			}

			public PostfixLogic ToPostfixLogic() {
				if (_postfixLogic == null || _startIndex == -1 || OpDepth == -1) {
					_MakePostfix();
				}

				int startDepth = PostfixDepth;
				int enddex = -1;
				for (int i = _startIndex - 1; i >= 0; i--) {
					if (_postfixLogic.TokenDepths[i] <= startDepth) {
						enddex = i;
						break;
					}
				}
				var postfixPart = _postfixLogic.GetRange(enddex + 1, _startIndex - enddex);
				return new PostfixLogic(postfixPart);
			}

			private void _MakePostfix(PostfixLogic postfix = null, int opDepth = 0) {
				if (postfix == null) postfix = new();
				Token opToken = new Token(Operator);
				bool skippedFirst = false;

				foreach (Node node in Nodes) {
					node._MakePostfix(postfix, opDepth + 1);
					if (skippedFirst) {
						postfix.Add(opToken);
					} else {
						skippedFirst = true;
					}
				}

				foreach (Token token in Tokens) {
					postfix.Add(token);
					if (skippedFirst) {
						postfix.Add(opToken);
					} else {
						skippedFirst = true;
					}
				}

				_postfixLogic = postfix;
				_startIndex = postfix.Count - 1;
				OpDepth = opDepth;
			}

			public void AddNode(Node node) {
				Nodes.Add(node);
			}

			public void RemoveNode(Node node) {
				Nodes.Remove(node);
			}

			public void AddToken(Token token) {
				Tokens.Add(token);
			}

			public void RemoveToken(Token token) {
				Tokens.Remove(token);
			}

			public void Combine(bool prioritizeSpecials, List<string> specialSymbols) {
				_ClearPostfix();
				if (Nodes.Count == 0) return;
				bool iterate;
				do {
					iterate = false;
					Bidex<Token, Node> symbols = new();

					//Find all symbols for all children.
					for (int i = 0; i < Nodes.Count; i++) {
						Node child = Nodes[i];
						child._removePseudoNots = this._removePseudoNots;

						child.Combine(prioritizeSpecials, specialSymbols);

						bool destroy = this._removePseudoNots && child._RemovePseudoNots();
						if (destroy || (child.Tokens.Count == 0 && child.Nodes.Count == 0)) {
							//This was an AND node and had both, ergo can never be true.
							//Or it was a completely empty node...
							this.RemoveNode(child);
							i--;
							continue;
						}

						if (child.Nodes.Count == 1 && child.Tokens.Count == 0) {
							//No tokens, just a single node...
							// (a | (??? + (b | c)))
							foreach (Node grandNode in child.Nodes[0].Nodes) {
								this.AddNode(grandNode);
							}
							foreach (Token grandToken in child.Nodes[0].Tokens) {
								this.AddToken(grandToken);
							}
							this.RemoveNode(child);
							i--;
							continue;
						} else if (child.Nodes.Count == 0 && child.Tokens.Count == 1) {
							//No nodes, just a single token...
							// (a | (b))
							this.AddToken(child.Tokens[0]);
							this.RemoveNode(child);
							i--;
							continue;
						}

						foreach (Token childToken in child.Tokens) {
							symbols.Add(childToken, child);
						}
					}
					if (symbols.LeftKeys.Count == 0) return;

					//The token that shows up in the most nodes
					IEnumerable<Token> tokens = symbols.LeftKeys;
					Token best_token;
					if (prioritizeSpecials) {
						best_token = tokens.OrderByDescending(t => specialSymbols.Contains(t.Symbol)).ThenByDescending(t => symbols[t].Count).First();
					} else {
						best_token = tokens.OrderByDescending(t => !specialSymbols.Contains(t.Symbol)).ThenByDescending(t => symbols[t].Count).First();
					}

					if (symbols[best_token].Count > 1) {
						iterate = true;

						Node newChild;
						Node newGrandchild;
						if (this is OrNode) {
							newChild = new AndNode();
							newGrandchild = new OrNode();
						} else {
							newChild = new OrNode();
							newGrandchild = new AndNode();
						}
						this.AddNode(newChild);
						newChild.AddNode(newGrandchild);
						newChild.AddToken(best_token);
						foreach (Node node in symbols[best_token]) {
							node.RemoveToken(best_token);
							newGrandchild.AddNode(node);
							this.RemoveNode(node);
						}
					}
				} while (iterate);
			}

			private bool _RemovePseudoNots() {
				for (int i = 0; i < this.Tokens.Count; i++) {
					Token token = this.Tokens[i];
					Token opposite;
					if (token.Symbol.StartsWith("!")) {
						opposite = new Token(token.Symbol.Substring(1));
					} else {
						opposite = new Token($"!{token.Symbol}");
					}

					if (this.Tokens.Contains(opposite)) {
						//Yep, it has both!
						if (this.Operator is And) {
							return true;
						}
						this.Tokens.Remove(token);
						this.Tokens.Remove(opposite);
						i--;
						continue;
					}
				}
				return false;
			}

			private void _ClearPostfix() {
				_postfixLogic = null;
				_startIndex = -1;
				OpDepth = -1;
			}
		}

		public class OrNode : Node {
			public OrNode(int depth, PostfixLogic postfixLogic, int startIndex) : base(Ops.Or, depth, postfixLogic, startIndex) { }
			public OrNode() : base(Ops.Or) { }
		}

		public class AndNode : Node {
			public AndNode(int depth, PostfixLogic postfixLogic, int startIndex) : base(Ops.And, depth, postfixLogic, startIndex) { }
			public AndNode() : base(Ops.And) { }
		}

		public IEnumerable<Node> NodesWithToken(string target) {
			foreach (Node item in _root.SelectManyRecursive_DFSPre(node => node.Nodes).Where(node => node.Tokens.Any(token => token.Symbol == target))) {
				yield return item;
			}
		}
	}
}
