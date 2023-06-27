using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace ReversePolishTests {
	static class OLD_PolishSimplificator {
		private static void _WriteLine(string msg) {
			//return;
			Debug.WriteLine(msg);
		}

		public static string Simplify(string logicstring, bool isPostfix = true) {
			List<string> _prefixList = new List<string>(isPostfix ? logicstring.Split(' ').Reverse() : logicstring.Split(' '));
			List<int> _depthList = null;
			SnipList snipList = new SnipList();
			SnipList opSnipList = new SnipList();
			for (int i = 0; i < _prefixList.Count; i++) {
				_WriteLine($"@ind {i}");
				if (_depthList == null || _depthList.Count != _prefixList.Count) {
					_depthList = _CalculateDepths(_prefixList);
				}
				//Remove SnipList items if depth or index > current
				snipList.CheckForRemoval(i, _depthList[i]);

				string curToken = _prefixList[i];

				if (_TokenIsOperator(curToken)) {
				} else {
					//The operAND tokens are the interesting ones. We either add it to the list, or we snip it!
					if (snipList.TryGet(curToken, out SnipItem si)) {
						if (si != null) {
							_WriteLine($"@ind {i}:\"{curToken}\" found! SNIP!");
							i = _Snip(i, si, false);
							_WriteLine($" ind <= {i}, dep <= {_depthList[i]}");
						}
					} else {
						int parentIndex = _FindParentOf(i);

						snipList.Add(new SnipItem(curToken, _OpTypeOf(_prefixList[parentIndex]), i, _depthList[i]));
					}
				}
			}

			return string.Join(" ", _prefixList.Reverse<string>().ToArray());

			int _FindParentOf(int ind) {
				int curDepth = _depthList[ind];
				while (ind > 0 && _depthList[ind] != curDepth - 1) {
					ind--;
				}

				return ind;
			}

			int _Snip(int ind, SnipItem si, bool doByOpDepth2 = false) {
				int begin = ind;
				bool isOp = false;

				//Set begin to correct node
				{
					int parentIndex = _FindParentOf(ind);
					OpType parentOp = _OpTypeOf(_prefixList[parentIndex]);

					if (parentOp != si.ParentOp) {
						//If they are different signs, we have to destroy two nodes instead of one.
						begin = parentIndex;
						isOp = true;
						_WriteLine("    Is Operation; 2 nodes to destroy.");
					} else {
						_WriteLine("    Is Operand; 1 node to destroy.");
					}
				}

				//Set all the refs properly
				//Sibling
				//int survivor = _FindSiblingOf(begin);
				//Parent
				int doomed = _FindParentOf(begin);
				//Grandparent
				int sanctuary;
				sanctuary = _FindParentOf(doomed);
				//sanctuary++;

				if (isOp) {
					//Delete all me children :(
					int startInd = begin + 1;
					int startDepth = _depthList[startInd];
					int endInd = startInd;
					while (endInd + 1 < _depthList.Count && _depthList[endInd + 1] >= startDepth) {
						endInd++;
					}

					for (int i = endInd; i >= startInd; i--) {
						_prefixList.RemoveAt(i);
					}
				}

				//delete begin
				_prefixList.RemoveAt(begin);

				//delete doomed
				if (_prefixList.Count > 1) {
					_prefixList.RemoveAt(doomed);
				}

				return sanctuary;
			}

			List<int> _CalculateDepths(List<string> prefixList) {
				List<int> newList = new List<int>();
				int next = 0;

				for (int i = 0; i < prefixList.Count; i++) {
					newList.Add(next);

					if (_TokenIsOperator(prefixList[i])) {
						next++;
					} else {
						while (newList.Where(it => it == next).Count() % 2 == 0) {
							//This means we've closed out this depth, so we need to start counting back down. Or whatever.
							next--;
							if (next == 0 && i < prefixList.Count - 1) {
								throw new Exception("This isn't quite right... Too many operands in a row?");
							}
						}
					}
				}

				string[] depthfriend = newList.Select(it => it.ToString().Substring(it.ToString().Length - 1)).ToArray();

				_WriteLine($"    0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9");
				_WriteLine($"    {string.Join(" ", _prefixList.ToArray())}");
				_WriteLine($"    {string.Join(" ", depthfriend)}");

				return newList;
			}

			bool _TokenIsOperator(string token) {
				if (_OpTypeOf(token) == OpType.NONE)
					return false;
				return true;
			}

			OpType _OpTypeOf(string token) {
				if (token == "|")
					return OpType.OR;
				if (token == "+" || token == "&")
					return OpType.AND;
				return OpType.NONE;
			}
		}

		class SnipList : List<SnipItem> {
			public new void Add(SnipItem si) {
				_WriteLine($"@ind {si.Index}:Add \"{si.Token}\" to snipList! @{si.Index}, >{si.Depth}, p{si.ParentOp}");
				base.Add(si);

				_LogAll();
			}

			public void CheckForRemoval(int index, int depth) {
				for (int i = 0; i < this.Count; i++) {
					if (this[i].Index >= index || this[i].Depth > depth) {
						_WriteLine($"    Remove \"{this[i].Token}\" from snipList! @{this[i].Index}, >{this[i].Depth}, p{this[i].ParentOp}");
						this.Remove(this[i]);
						i--;
						_LogAll();
					}
				}
			}

			public bool TryGet(string token, out SnipItem si) {
				si = null;

				foreach (SnipItem item in this) {
					if (item.Token == token) {
						si = item;
						return true;
					}
				}

				return false;
			}

			private void _LogAll() {
				_WriteLine("________________snipList________________");

				foreach (SnipItem item in this) {
					_WriteLine($"\"{item.Token}\", @{item.Index}, >{item.Depth}, p{item.ParentOp}");
				}

				_WriteLine("_______________/snipList________________");
			}
		}

		private class SnipItem {
			public string Token;
			public OpType ParentOp;
			public int Index;
			public int Depth;

			public SnipItem(string t, OpType ot, int i, int d) {
				Token = t;
				ParentOp = ot;
				Index = i;
				Depth = d;
			}
		}

		private enum OpType {
			NONE,
			AND,
			OR
		}
	}
}
