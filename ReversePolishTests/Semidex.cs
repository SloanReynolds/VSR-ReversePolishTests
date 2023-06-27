﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ReversePolishTests {
	class Semidex<T,U> {
		private Dictionary<T, U> _leftDict = new Dictionary<T, U>();
		private Dictionary<U, List<T>> _rightDict = new Dictionary<U, List<T>>();

		public Dictionary<T, U>.KeyCollection LeftKeys => _leftDict.Keys;
		public Dictionary<U, List<T>>.KeyCollection RightKeys => _rightDict.Keys;

		public void RemovePair(T left, U right) {
			_leftDict[left] = default;
			_rightDict[right].Remove(left);
		}

		//public void RemoveAndPrunePair(T left, U right) {
		//	RemovePair(left, right);
		//	if (_leftDict.Count == 0) _leftDict.Remove(left);
		//	if (_rightDict.Count == 0) _rightDict.Remove(right);
		//}

		public U this[T item] => _leftDict[item];
		public List<T> this[U item] => _rightDict[item];

		public T LeftKeyAt(int index) => LeftKeys.Where((x, y) => y == index).First();
		public U RightKeyAt(int index) => RightKeys.Where((x, y) => y == index).First();
		public U LeftAt(int index) => _leftDict[LeftKeyAt(index)];
		public List<T> RightAt(int index) => _rightDict[RightKeyAt(index)];

		public void Add(T left, U right) {
			if (!_leftDict.ContainsKey(left)) _leftDict.Add(left, right);
			if (!_rightDict.ContainsKey(right)) _rightDict.Add(right, new List<T>());
			_leftDict[left] = right;
			_rightDict[right].Add(left);
		}

		public void Add(U right, T left) {
			Add(left, right);
		}

		public bool TryGet(U rightKey, out List<T> leftList) {
			return _rightDict.TryGetValue(rightKey, out leftList);
		}

		public bool TryGet(T leftKey, out U right) {
			return _leftDict.TryGetValue(leftKey, out right);
		}

		public bool Contains(T key) {
			return _leftDict.ContainsKey(key);
		}

		public bool Contains(U key) {
			return _rightDict.ContainsKey(key);
		}
	}
}
