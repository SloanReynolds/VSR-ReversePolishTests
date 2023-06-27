using System;
using System.Collections.Generic;
using System.Linq;

namespace DapperLogic {
	internal static class Extensions {
		internal static IEnumerable<T> SelectManyRecursive_DFSPre<T>(this T node, Func<T, IEnumerable<T>> children) {
			var queue = new Queue<T>();
			queue.Enqueue(node);
			while (queue.Count != 0) {
				var current = queue.Dequeue();
				foreach (var child in children(current))
					queue.Enqueue(child);
				yield return current;
			}
		}
	}
}
