using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DapperLogic;
using Spectre.Console;

namespace ReversePolishTests {
	class Program {

		static string _Simplify(string postfix) {
			PostfixSimplificator yas = new PostfixSimplificator(PostfixHelper.FromString(postfix));
			yas.Print();
			yas.Prune();
			yas.Print();
			return string.Join(" ", yas.GetPostfix());
		}

		static void Main(string[] args) {
			//var thing = NodeHelper.FromInfix("w | d | c & b | a | f");
			// w d | c b & a | f | |
			// 2 2 1 4 4 3 3 2 2 1 0
			{ }

			//CompareImplicants(
			//	"VYU & BSA | ~NBF & BSA | ~VYU & ~NBF",
			//	"~VYU & ~NBF | VYU & BSA"
			//, true);
			//
			//Console.ReadKey();

			ReductionTests("");

			//IsolatePhrase();

			//TestPositiveMinimizer("WALL_RUN");

			Console.ReadKey();
		}

		private static bool CompareImplicants(string infix1, string infix2, bool getTable) {
			bool result = CompareImplicants(infix1, infix2, getTable, out Table table);
			if (getTable) {
				AnsiConsole.Write(table);
				Console.WriteLine();
			}
			return result;
		}

		private static bool CompareImplicants(string infix1, string infix2, bool getTable, out Table table) {
			if (getTable) {
				Console.WriteLine($"s1: '{infix1}'");
				Console.WriteLine($"s2: '{infix2}'");
			}

			QuineMcCluskey qmc1 = new(InfixHelper.ToPostfixLogic(infix1));
			QuineMcCluskey qmc2 = new(InfixHelper.ToPostfixLogic(infix2));

			table = getTable ? new() : null;
			table?.AddColumns("", "s1", "s2");

			var list1 = qmc1.allTrueImplicants;
			var list2 = qmc2.allTrueImplicants;
			string header = "Minterms";

			if (!_validateSection(table) && !getTable) {
				return false;
			}

			list1 = qmc1.FullyCombine();
			list2 = qmc2.FullyCombine();
			header = "Implicants";

			if (!_validateSection(table) && !getTable) {
				return false;
			}

			list1 = qmc1.FullyExtractEssentials();
			list2 = qmc2.FullyExtractEssentials();
			header = "Primes";

			if (!_validateSection(table) && !getTable) {
				return false;
			}

			return true;

			bool _validateSection(Table table) {
				bool result = true;

				var count1 = list1.Count();
				var count2 = list2.Count();

				if (getTable) {
					table.AddRow(header, string.Join("\n", list1), string.Join("\n", list2));
					table.AddRow("Count", count1.ToString(), count2.ToString());
				}

				if (count1 != count2) {
					if (getTable) {
						result = false;
					} else {
						return false;
					}
				}

				var leftNew = list1.Except(list2);
				var rightNew = list2.Except(list1);

				if (leftNew.Count() > 0 || rightNew.Count() > 0) {
					if (getTable) {
						if (header == "Minterms") {
							table.AddRow("Extras", string.Join("\n", leftNew.Select(imp => imp.Minterms[0])), string.Join("\n", rightNew.Select(imp => imp.Minterms[0])));
						} else {
							table.AddRow("Extras", string.Join("\n", leftNew), string.Join("\n", rightNew));
						}
						result = false;
					} else {
						return false;
					}
				}

				return result;
			}
		}

		private static void TestPositiveMinimizer(string v) {
			Console.WriteLine(PositiveLogicMinimizer.FullyFlatten(v));
			Console.WriteLine();
			Console.WriteLine(PositiveLogicMinimizer.FullyFlatten(v, "opt_randomized_puzzles", "!opt_randomized_puzzles"));
			Console.WriteLine();
			Console.ReadLine();
			{ }
		}

		static void IsolatePhrase() {
			string logicToIsolate = "A & B | C & D | A & C | (TARGET & H & D | H) & E & Z & A & B";
			Console.WriteLine(logicToIsolate);
			// A B & C D & A C & TARGET H & D & E & Z & A & B & | | |
			// 2 2 1 3 3 2 4 4 3 9      9 8 8 7 7 6 6 5 5 4 4 3 2 1 0
			PostfixLogic postfix = InfixHelper.ToPostfixLogic(logicToIsolate);
			Console.WriteLine(postfix);
			var nodeLog = postfix.NodeLogic;
			var thing = nodeLog.NodesWithToken("TARGET");
			foreach (var iso in thing) {
				Console.WriteLine(PostfixHelper.ToInfix(iso.ToPostfixLogic()));
			}
			{ }
		}

		static void ReductionTests(string manualLogic = "") {
			Maximize();

			int uniqueTokens = 10;
			int operandCount = 51;
			int parens = operandCount / 7;
			bool positiveOnly = false;
			bool pauseOnEach = false;
			bool repeat = false;
			bool pauseNext = manualLogic != "" ? true : false;
			string logic = "";

			Stopwatch swQ = new Stopwatch();
			Stopwatch swL = new Stopwatch();
			int totalCount = 0;

			Random Rando = new Random();

			int seed = Rando.Next();

			//positiveOnly = true;
			//pauseOnEach = true;

			//Report vars
			List<Implicant> qBeforeExtract = null;
			//Report vars

			while (true) {
				totalCount++;
				PostfixLogic postfixList;
				if (!repeat) {
					seed = Rando.Next();
				}
				repeat = false;

				if (manualLogic == "") {
					logic = _MakeRandomLogic(positiveOnly, operandCount, 23, uniqueTokens, 2, seed);
					postfixList = InfixHelper.ToPostfixLogic(logic);
				} else {
					logic = manualLogic;
					manualLogic = "";
					postfixList = InfixHelper.ToPostfixLogic(logic);
				}

				Console.WriteLine(logic);
				Console.WriteLine();

				Console.WriteLine(string.Join(" ", postfixList));
				Console.WriteLine();

				string qmcInfix = "";
				string ltmInfix = "";

				//Console.WriteLine();
				string qTrueTerms = "";
				string qPrimes = "";

				if (true) {
					swQ.Start();
					Console.WriteLine("--------QMC--------");
					QuineMcCluskey qmc = new QuineMcCluskey(postfixList, true);
					if (true) {
						qTrueTerms += "All Minterms\n";
						qTrueTerms += string.Join("|", qmc.LTM.Tokens.Select(t => t.Symbol)) + "\n";
						foreach (var item in qmc.allTrue) {
							qTrueTerms += item.Term.ToString().PadRight(8) + " " + Convert.ToString(item.Term, 2).PadLeft(qmc.LTM.OperandCount, '0') + "\n";
						}
					}

					qBeforeExtract = qmc.FullyCombine();
					var qPrimeList = qmc.FullyExtractEssentials();

					qPrimes += "QuineMcCluskey primes:\n";
					foreach (var item in qPrimeList) {
						qPrimes += item.MaskString.PadLeft(uniqueTokens, '0') + "\n";
					}
					//Console.WriteLine(string.Join("|", qmc.LTM.Tokens.Select(t => t.Symbol)));
					//Console.WriteLine();

					var qPostfix = qmc.GetPostfix();
					//Console.WriteLine(PostfixHelper.ToInfix(postfixList));
					//Console.WriteLine();
					//Console.WriteLine("Becomes:");
					//Console.WriteLine();
					qmcInfix = PostfixHelper.ToInfix(qPostfix);
					//Console.WriteLine(qmcPreSimplify);
					//Console.WriteLine();

					//Console.WriteLine("Simplifies to:");
					//Console.WriteLine(PostfixSimplificator.SimplifyToInfix(qPostfix));

					//Console.WriteLine(qmcPreSimplify.Replace("! ", "~"));
					Console.WriteLine("-------/QMC--------");
					swQ.Stop();
				}

				string lTrueTerms = "";
				string lPrimes = "";
				string expiraStr = "";
				Table report;
				List<Minterm> collapsed = null;
				List<Minterm> fullEssential = null;

				if (true) {
					swL.Start();
					Console.WriteLine("--------LTM--------");
					LogicToMinterms ltm = new LogicToMinterms(postfixList);
					ltm.GetMinterms();
					var lPreMinimal = ltm.GetEssentialPrimes();
					var lPrimeList = ltm.GetMinimalSet();

					if (lPreMinimal.Count != lPrimeList.Count) {
						pauseNext = true;
					}

					collapsed = ltm.AfterCollapse;
					fullEssential = ltm.Essentials;
					//preFlippedN = ltm.BeforeFlippedN;

					if (true) {
						if (true) {
							lTrueTerms += "All Minterms\n";
							lTrueTerms += string.Join("|", ltm.Tokens.Select(t => t.Symbol)) + "\n";
							foreach (var item in ltm.AllTrue) {
								lTrueTerms += item.Term.ToString().PadRight(8) + " " + Convert.ToString(item.Term, 2).PadLeft(ltm.Tokens.Count, '0') + "\n";
							}
						}

						lPrimes += $"LogicToMinterms primes: (e {lPreMinimal.Count}) (m {lPrimeList.Count})\n";
						foreach (var item in lPrimeList) {
							lPrimes += Convert.ToString(item.Term, 2).PadLeft(ltm.Tokens.Count, '0') + "\n";
						}
						//Console.WriteLine(string.Join("|", ltm.Tokens.Select(t => t.Symbol)));
						//Console.WriteLine();
					}

					var lPostfix = ltm.GetPostfix();
					//Console.WriteLine(PostfixHelper.ToInfix(postfixList));
					//Console.WriteLine();
					//Console.WriteLine("Becomes:");
					//Console.WriteLine();
					ltmInfix = PostfixHelper.ToInfix(lPostfix);
					//Console.WriteLine(ltmPreSimplify);
					//Console.WriteLine();

					//Console.WriteLine("Simplifies to:");
					//Console.WriteLine(PostfixSimplificator.SimplifyToInfix(lPostfix));
					//Console.WriteLine(ltmPreSimplify);
					Console.WriteLine("-------/LTM--------");

					int negaMask = ltm.NMask;
					List<string> listStr = new List<string>();
					listStr.Add(Convert.ToString(negaMask, 2).PadLeft(ltm.OperandCount) + " " + negaMask);
					foreach (var item in ltm.AllTrue) {
						GetNegaTerms(negaMask, item, out int negative, out int positive);
						//listStr.Add(Convert.ToString(item.Term ^ negaMask, 2).PadLeft(ltm.Count, '0'));
						listStr.Add($"{Convert.ToString(negative, 2).PadLeft((int)Math.Ceiling(ltm.OperandCount / 2m), '0').Replace("0", "-").Replace("1", "0")} {Convert.ToString(positive, 2).PadLeft((int)Math.Ceiling(ltm.OperandCount / 2m), '0').Replace("0", "-")} {negative} {positive}");
					}
					//listStr.Sort();
					expiraStr += string.Join("\n", listStr);
					//expiraStr += "\n" + Convert.ToString(ltm.GetNegativeMask(),2);

					report = ltm.ReportUniqueOperandMap();
					swL.Stop();
				}

				string qBeforeExtractStr = qBeforeExtract != null ? string.Join("\n", qBeforeExtract.Select(m => m.MaskString.PadLeft(uniqueTokens, '0'))) : "null";

				Table rable = new Table();

				rable.AddColumn("QMC");
				rable.AddColumn("Q Primes");
				rable.AddColumn("LTM");
				rable.AddColumn("Stuff");
				rable.AddColumn("Stuff#More");
				//rable.AddColumn("More Stuff");
				rable.AddRow(qTrueTerms, "", lTrueTerms, " =) ");
				rable.AddRow(qPrimes,
					qBeforeExtractStr,
					lPrimes,
					string.Join("\n", fullEssential.Select(m => Convert.ToString(m.Term, 2).PadLeft(uniqueTokens * 2, '0'))),
					string.Join("\n", collapsed.Select(m => Convert.ToString(m.Term, 2).PadLeft(uniqueTokens * 2, '0')))
				);
				AnsiConsole.Write(rable);

				bool comparedImplicants = CompareImplicants(qmcInfix.Replace("! ", "~"), ltmInfix.Replace("! ", "~"), true, out Table tableCompare);

				Console.WriteLine();
				Console.WriteLine("[[INF]] " + logic);
				Console.WriteLine("[[QMC]] " + qmcInfix.Replace("! ", "~"));
				Console.WriteLine("[[LTM]] " + ltmInfix.Replace("! ", "~"));
				Console.WriteLine();

				Console.WriteLine();
				AnsiConsole.Write(report);

				Table fable = new Table();
				fable.AddColumns($"{totalCount} Runs", "Total Time", "Average Time");
				fable.AddRow("QMC", $"{swQ.ElapsedMilliseconds}ms", $"{Math.Round(swQ.ElapsedMilliseconds / (float)totalCount, 2)}ms");
				fable.AddRow("LTM", $"{swL.ElapsedMilliseconds}ms", $"{Math.Round(swL.ElapsedMilliseconds / (float)totalCount, 2)}ms");

				AnsiConsole.Write(fable);
				Console.WriteLine();

				if (qmcInfix.Length != ltmInfix.Length) {
					pauseNext = true;
				} else {
					if (!comparedImplicants) {
						pauseNext = true;
					}
				}
				if (pauseNext || pauseOnEach) {
					do {
						ConsoleKeyInfo key = Console.ReadKey();
						Console.WriteLine();
						if (key.Key == ConsoleKey.R) {
							repeat = true;
							pauseNext = true;
							break;
						} else if (key.Key == ConsoleKey.Y) {
							pauseNext = true;
							break;
						} else if (key.Key == ConsoleKey.Spacebar) {
							pauseNext = false;
							break;
						} else if (key.Key == ConsoleKey.I) {
							AnsiConsole.Write(tableCompare);
							Console.WriteLine();
						} else if (key.Key == ConsoleKey.L) {
							manualLogic = Console.ReadLine();
							Console.WriteLine($">{manualLogic}<   ?");
							var ket = Console.ReadKey();
							if (ket.Key != ConsoleKey.Y) {
								manualLogic = "";
							} else {
								pauseNext = true;
							}
							break;
						}
					} while (true);
				}
			}
			Console.WriteLine();

			return;

			//{
			//	var maxterm1 = new ExampleClass(2, true);
			//	var list = new List<ExampleClass>(new[] { maxterm1 });
			//	var maxterm2 = new ExampleClass(2, true);

			//	Console.WriteLine(list.Contains(maxterm1));
			//	Console.WriteLine(list.Contains(maxterm2));
			//	Console.WriteLine(maxterm1.GetHashCode());
			//	Console.WriteLine(maxterm2.GetHashCode());
			//	Console.WriteLine(EqualityComparer<ExampleClass>.Default.GetType());
			//}
			//Console.WriteLine(); Console.WriteLine(); Console.WriteLine();
			//{
			//	var minterm1 = new ExampleStruct(2, true);
			//	var list = new List<ExampleStruct>(new[] { minterm1 });
			//	var minterm2 = new ExampleStruct(2, true);

			//	Console.WriteLine(list.Contains(minterm1));
			//	Console.WriteLine(list.Contains(minterm2));
			//	Console.WriteLine(minterm1.GetHashCode());
			//	Console.WriteLine(minterm2.GetHashCode());
			//	Console.WriteLine(EqualityComparer<ExampleStruct>.Default.GetType());
			//}

			//Console.ReadKey();
			//return;


			// A B |
			// A ! B ! & !
			// D      A      B      |      C      &      |      E      &
			// D      A!     B!     &!
			// D      A!     B!     &!     C!     &!     &!
			// D      A!     B!     &!     C!     &!     &!     E      &
			//
			// E & (C & (B | A) | D)
			//           _ _ _
			//     (    (B & A) | D)
			//           _ _ _
			//     (C & (B & A) | D)
			//           _ _ _
			// E & (C & (B & A) & D)
			//

			// A | (B | (C | D))
			//           _ _ _
			// A | (B | (C & D))
			//      _ _  _ = _
			// A | (B & (C & D))
			// _ _  _ =  _   _
			// A & (B & (C & D))
			// _ _  _    _   _
			// A & (B & (C & D))

			// "A | (B | (C | D))"
			// A 1111111100000000
			// B 1111000011110000
			// C 1100110011001100
			// D 1010101010101010
			// ==================
			// T 1111111111111110

			// _ _  _   _   _
			// A & (B & C & D)
			// _ _ 
			// A & E
			// A 1111111100000000
			// E 0000000100000001
			// B 1111000011110000
			// C 1100110011001100
			// D 1010101010101010
			// ==================
			// T 1111111111111110

			//!& == 11,10,01,00 = 0,1,1,1


			// D C | B | A |

			// D   C   |   B   |   A   |
			// D!  C!  &!  



			//  "A & B | C & B"
			// A f t f t f t f t
			// B f f t t f f t t
			// C f f f f t t t t
			// -----------------
			// T f f f t f f t t
			// =================
			//   "B & (A | C)"
			// A f t f t f t f t
			// B f f t t f f t t
			// C f f f f t t t t
			// -----------------
			// T f f f t f f t t

			//"(A | B) & (C | B)"
			// A f t f t f t f t
			// B f f t t f f t t
			// C f f f f t t t t
			// -----------------
			// T f f t t f t t t
			// =================
			//   "B | (A & C)"
			// A f t f t f t f t
			// B f f t t f f t t
			// C f f f f t t t t
			// -----------------
			// T f f t t f t t t


			// A | (B | (C | D))            D C | B | A |
			//           _ _ _              _ _ _
			// A | (B | (C & D))            D C & B | A |
			//      _ _  _ = _              _ _ = _ _
			// A | (B & (C & D))            D C & B & A |
			// _ _  _ =  _   _              _ _   _ = _ _
			// A & (B & (C & D))            D C & B & A &
			// _ _  _    _   _              _ _   _   _ _
			// A & (B & (C & D))            D C & B & A &





			/*List<Token> postfixTokens = InfixHelper.ToPostfixList("(CHARGE_SHOT & WALL_RUN & SPIN_DODGE & HEAT_RESIST & ENERGY_CLAW & CLAW_BREAKS_CHARGE_SHOT) | " +
				"(CHARGE_SHOT & WALL_RUN & SPIN_DODGE & ENERGY_CLAW & STRIP_SUIT & SPEED_BOOST & CLAW_BREAKS_CHARGE_SHOT) | " +
				"(WALL_RUN & SPIN_DODGE & ENERGY_CLAW & STRIP_SUIT & SPEED_BOOST & CLAW_BREAKS_CHARGE_SHOT & SPEED_BOOST_BREAKS_CLAW) | " +
				"(CHARGE_SHOT & WALL_RUN & SPIN_DODGE & HEAT_RESIST & ENERGY_CLAW & STRIP_SUIT & SPEED_BOOST) | " +
				"(CHARGE_SHOT & WALL_RUN & SPIN_DODGE & HEAT_RESIST & STRIP_SUIT) | " +
				"(CHARGE_SHOT & WALL_RUN & SPIN_DODGE & HEAT_RESIST & STRIP_SUIT) | " +
				"(CHARGE_SHOT & WALL_RUN & SPIN_DODGE) | " +
				"(CHARGE_SHOT & WALL_RUN & SPIN_DODGE) | " +
				"(CHARGE_SHOT & WALL_RUN & SPIN_DODGE & ENERGY_CLAW & STRIP_SUIT & CLAW_BREAKS_CHARGE_SHOT) | " +
				"(CHARGE_SHOT & WALL_RUN & ENERGY_CLAW & STRIP_SUIT & SPEED_BOOST & CLAW_BREAKS_CHARGE_SHOT & SPEED_BOOST_BREAKS_CLAW)");
			//(C | DE) & ((FG) | F | H | GH))
			//(C | (DE))((FG) | (F | H) | (GH))
			//(C | DE)(FG | (F | H) | GH)
			//(C | DE)(FG | F | H | GH)
			//CFG|CF|CH|CGH|DEFG|DEF|DEH|DGH
			//CF|CH|CFG|CGH|DEF|DEH|DGH|DEFG
			//
			//CDEFGH
			//CF DEF DGH

			//postfixTokens = InfixHelper.ToPostfixList("WALL_RUN & SPIN_DODGE & SHOW_IMPORTANT | WALL_RUN & SPIN_DODGE | CHARGE_SHOT & ALTERED_SHOT & WALL_RUN & MENTAL_RECOVERY & ENERGY_CLAW & CLAW_BREAKS_CHARGE_SHOT & DOUBLE_SHOT | STRIP_SUIT");

			Console.WriteLine(string.Join(" ", postfixTokens));

			//NoFreakingWay nfw = new NoFreakingWay(postfixTokens);
			//Console.WriteLine(nfw.Flatten());

			Console.WriteLine();
			if (true) {
				QuineMcCluskey qmc = new QuineMcCluskey(postfixTokens, true);
				//QuineMcCluskey qmc = new QuineMcCluskey(new int[] { 0, 2, 5, 6, 8, 9, 10, 11, 12, 13, 14, 15, 18, 19, 20, 21, 23, 26, 27, 28, 29 });
				Random rnd = new Random();
				int max = 40;
				int[] arr = new int[max];
				for (int i = 0; i < max; i++) {
					arr[i] = rnd.Next(max * 2);
				}

				string str = "2	3	4	5	7	9	12	13	16	19	20	22	24	25	35	36	40	42	44	45	46	48	49	51	62	63	64	65	68	70	75	76	78	86	87	89	90	91	93	97	100	101	104	105	106	107	109	111	114	118	120	122	123";
				arr = str.Split('	').Select(s => { if (int.TryParse(s, out int i)) return i; return -1; }).ToArray();

				{
					var thing = qmc.FullyCombine();
					int maxCount = thing.Max(i => i.Size);
					int size = (int)Math.Round(Math.Log10(Convert.ToDouble(maxCount))) + 2;
					foreach (var item in thing) {
						Console.WriteLine($"m({item})".PadRight(4 + (size * maxCount)) + " : " + $"{item.MaskString}".PadLeft(34));
					}
				}

				Console.ReadKey();
				Console.WriteLine();

				{
					var thing = qmc.FullyExtractEssentials().OrderBy(i => i.LastTerm).OrderBy(i => i.FirstTerm).OrderBy(i => i.OnesCount);
					int maxCount = thing.Max(i => i.Size);
					int size = (int)Math.Round(Math.Log10(Convert.ToDouble(maxCount))) + 2;
					foreach (var item in thing) {
						Console.WriteLine($"m({item})".PadRight(4 + (size * maxCount)) + " : " + $"{item.MaskString}".PadLeft(34));
					}
					Console.WriteLine(thing.Count());
				}

				Console.ReadKey();
				Console.WriteLine();

				List<Token> postfix = qmc.GetPostfix();
				Console.WriteLine(string.Join(" ", postfix));
				List<Token> infix = PostfixHelper.ToInfixList(postfix);
				Console.WriteLine(string.Join(" ", infix));
				List<Token> simplified = PostfixSimplificator.Simplify(postfix);
				Console.WriteLine(PostfixHelper.ToInfix(simplified));

				Console.ReadKey();
				Console.WriteLine();

				_DisplayTree(string.Join(" ", postfix));
				_DisplayTree(string.Join(" ", simplified));

				Console.ReadKey();
				Console.WriteLine();

				Console.ReadKey();
				Console.WriteLine();
				return;
			}

			{
				string shitr = string.Join(" ", postfixTokens);
				_DisplayTree(shitr);
				string simple = _Simplify(shitr);
				_DisplayTree(simple);

				Console.ReadKey();
			}*/
			return;

			//LogicManager.ParseXML();
			//Console.Clear();

			//List<ReqDef> itemDefs = LogicManager.ItemNames.Select(str => LogicManager.GetItemDef(str)).ToList();

			//const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

			//Func<string, string> __FindReplacement;
			//Func<string, string> __Unreplace;

			//{
			//	Dictionary<string, string> replacements = new Dictionary<string, string>();
			//	Dictionary<string, string> unreplacements = new Dictionary<string, string>();

			//	__FindReplacement = ___FindReplacement;
			//	__Unreplace = ___Unreplace;
			//	string ___FindReplacement(string token) {
			//		//if (token.Length < 3 || LogicManager.WaypointDefs.ContainsKey(token)) return token;
			//		if (replacements.TryGetValue(token, out string newKey)) {
			//			return newKey;
			//		} else {
			//			int first = replacements.Count / alphabet.Length;
			//			int second = replacements.Count % alphabet.Length;

			//			string replace = alphabet[first].ToString() + alphabet[second].ToString();
			//			replacements.Add(token, replace);
			//			unreplacements.Add(replace, token);

			//			Console.WriteLine($"Created replacement for {token} => {replace}");
			//			return replace;
			//		}
			//	}

			//	string ___Unreplace(string replace) {
			//		return unreplacements.ContainsKey(replace) ? unreplacements[replace] : replace;
			//	}
			//}

			//string startingArea = "King's_Pass";
			//foreach (string wpName in LogicManager.Waypoints) {
			//	if (startingArea == wpName) continue;
			//	//Waypoint waypoint = LogicManager.WaypointDefs[wpName];
			//	bool hasChanged = false;
			//	do {
			//		hasChanged = false;
			//		List<string> waypointsFound = new List<string>();

			//		waypoint.itemLogic = waypoint.itemLogic.Select(str => __FindReplacement(str)).ToArray();
			//		//for (int i = 0; i < waypoint.itemLogic.Length; i++) {
			//		for (int i = waypoint.itemLogic.Length - 1; i >= 0; i--) {
			//			string token = __Unreplace(waypoint.itemLogic[i]);
			//			if (token.Length > 1 && LogicManager.Waypoints.Contains(token)) {
			//				string tokenName = __Unreplace(waypoint.itemLogic[i]);
			//				//Waypoint tokenWaypoint = LogicManager.WaypointDefs[tokenName];
			//				tokenWaypoint.itemLogic = tokenWaypoint.itemLogic.Select(str => __FindReplacement(str)).ToArray();
			//				Console.WriteLine();
			//				hasChanged = true;
			//				waypointsFound.Add(token);

			//				waypointsFound.ForEach(str => Console.WriteLine(str));
			//				Console.WriteLine();

			//				Console.WriteLine($"-Current Logic for {wpName}:-");
			//				Console.WriteLine($"=> [{PostfixHelper.CountOperatorDifference(waypoint.itemLogic)}] {string.Join(" ", waypoint.itemLogic)}");
			//				Console.WriteLine();

			//				if (waypoint.itemLogic.Contains(startingArea)) {
			//					waypoint.itemLogic = PostfixHelper.RemoveAsTrue(waypoint.itemLogic, startingArea).ToArray();

			//					Console.WriteLine($"-Starting area {startingArea}:");
			//					Console.WriteLine($"=> [{PostfixHelper.CountOperatorDifference(waypoint.itemLogic)}] {string.Join(" ", waypoint.itemLogic)}");
			//					Console.WriteLine();

			//					i = waypoint.itemLogic.Length;
			//					continue;
			//				}

			//				string tokenLogic = string.Join(" ", tokenWaypoint.itemLogic);
			//				if (tokenLogic == "") {
			//					waypoint.itemLogic = PostfixHelper.RemoveAsTrue(waypoint.itemLogic, tokenName).ToArray();

			//					if (waypoint.itemLogic.Length == 0) {
			//						i = waypoint.itemLogic.Length;
			//						continue;
			//					}
			//				} else {
			//					waypoint.itemLogic[i] = tokenLogic;
			//					waypoint.itemLogic = string.Join(" ", waypoint.itemLogic).Split(' ').ToArray();
			//				}

			//				Console.WriteLine($"-Waypoint {token} Replaced:-");
			//				Console.WriteLine($"=> [{PostfixHelper.CountOperatorDifference(waypoint.itemLogic)}] {string.Join(" ", waypoint.itemLogic)}");
			//				Console.WriteLine();

			//				foreach (string reject in waypointsFound) {
			//					if (waypoint.itemLogic.Contains(reject)) {
			//						List<string> list = PostfixHelper.RemoveAsFalse(waypoint.itemLogic, reject);

			//						Console.WriteLine($"-Rejected {reject}:");
			//						Console.WriteLine($"=> [{PostfixHelper.CountOperatorDifference(list)}] {string.Join(" ", list.ToArray())}");
			//						Console.WriteLine();

			//						waypoint.itemLogic = list.ToArray();
			//					}
			//				}

			//				if (!waypointsFound.Contains(token)) {
			//					waypointsFound.Add(token);
			//				}

			//				{
			//					string preSimp = string.Join(" ", waypoint.itemLogic);

			//					Console.WriteLine($"-After Rejections Rejected:-");
			//					Console.WriteLine($"=> [{PostfixHelper.CountOperatorDifference(preSimp)}] {preSimp}");
			//					Console.WriteLine();

			//					PostfixSimplificator yas = new PostfixSimplificator(waypoint.itemLogic);
			//					yas.Prune();
			//					waypoint.itemLogic = yas.GetPostfix().Split(' ');

			//					string postSimp = string.Join(" ", waypoint.itemLogic);

			//					Console.WriteLine($"-Logic Simplified:-");
			//					Console.WriteLine($"=> [{PostfixHelper.CountOperatorDifference(postSimp)}] {postSimp}");
			//					Console.WriteLine();

			//					//_DisplayTree(preSimp);
			//					//_DisplayTree(postSimp);
			//					Console.WriteLine($"From {preSimp.Length} to {postSimp.Length}");
			//					if (preSimp.Length != postSimp.Length)
			//						Console.ReadKey();
			//				}

			//				Console.WriteLine();
			//				Console.WriteLine();
			//				Console.WriteLine();
			//				//Console.Clear();

			//				//i = -1;
			//				i = waypoint.itemLogic.Length;
			//			}
			//		}
			//	} while (hasChanged);

			//	Console.WriteLine(wpName);
			//	if (waypoint.itemLogic.Length > 0) {
			//		string[] thing = waypoint.itemLogic.Select(wp => __Unreplace(wp)).ToArray();
			//		Console.WriteLine(string.Join(" ", thing));
			//		Console.WriteLine(string.Join(" ", ShuntingYardInverse(thing)));
			//	} else {
			//		Console.WriteLine("True");
			//	}

			//	LogicManager.WaypointDefs[wpName] = waypoint;

			//	//Console.ReadKey();
			//}

			//foreach (string iName in LogicManager.ItemNames) {
			//	ReqDef def = LogicManager.GetItemDef(iName);
			//	bool hasChanged = false;
			//	do {
			//		for (int i = 0; i < def.itemLogic.Length; i++) {
			//			string token = def.itemLogic[i];
			//			if (token.Length > 1 && LogicManager.Waypoints.Contains(token)) {
			//				def.itemLogic[i] = string.Join(" ", LogicManager.WaypointDefs[token].itemLogic);
			//				hasChanged = true;
			//				Console.WriteLine($"?=>{def.nameKey} : {token} => {def.itemLogic[i]}");
			//				Console.WriteLine($"!=>{string.Join(" ", def.itemLogic)}");
			//				def.itemLogic = string.Join(" ", def.itemLogic).Split(' ').ToArray();
			//				Console.ReadKey();
			//			}
			//		}
			//	} while (hasChanged);
			//}

			//foreach (string itemName in LogicManager.ItemNames) {
			//	ReqDef rd = LogicManager.GetItemDef(itemName);
			//	string ogLogic = string.Join(" ", rd.itemLogic);

			//	PostfixSimplificator yas = new PostfixSimplificator(ogLogic);
			//	yas.Prune();
			//	string yay = yas.GetPostfix();

			//	//if (ogLogic.Length != yay.Length) {
			//	Console.WriteLine(itemName);

			//	_PrintStuff(ogLogic, yay);
			//	//}
			//	Console.ReadKey();
			//}

			//return;

			//int count = 0;
			//string LogicToUse = "";
			//Console.Clear();
			//Maximize();
			//do {
			//	count++;
			//	if (count == 1) {
			//		LogicToUse = "Upper_Kingdom's_Edge Mothwing_Cloak Shade_Cloak + + Dream_Nail Dream_Gate | Awoken_Dream_Nail | + Vengeful_Spirit Shade_Soul | Howling_Wraiths Abyss_Shriek | | Mothwing_Cloak Shade_Cloak | MILDSKIPS | + SPICYSKIPS | +";
			//		LogicToUse = string.Join(" ", ShuntingYardInverse(LogicToUse.Split(' ').ToArray()));
			//	} else if (count == 2) {
			//		LogicToUse = "A B C + + D E | F | + G H | I J | | B C | MILDSKIPS | + SPICYSKIPS | +";
			//		LogicToUse = string.Join(" ", ShuntingYardInverse(LogicToUse.Split(' ').ToArray()));
			//	} else if (count == 3) {
			//		LogicToUse = "A B C + + D E | F | + G H | I J | | B C | K | + L | +";
			//		LogicToUse = string.Join(" ", ShuntingYardInverse(LogicToUse.Split(' ').ToArray()));
			//	} else {
			//		LogicToUse = _MakeRandomLogic();
			//	}

			//	//Console.WriteLine();
			//	//Console.WriteLine($"OLD {LogicToUse}");

			//	//string thingy = _DumbSimplify(LogicToUse);

			//	//Console.WriteLine();
			//	//Console.WriteLine($"NEW {thingy}");

			//	//thingy = string.Join(" ", TrimParentheses(thingy));

			//	//Console.WriteLine();
			//	//Console.WriteLine($"Trim {thingy}");

			//	string[] postfixArr = ShuntingYard(LogicToUse);
			//	string postfix = string.Join(" ", postfixArr);

			//	//Console.WriteLine();
			//	//Console.WriteLine($"RPN {postfix}");

			//	//Oldtest(LOGIC)

			//	//BTree btr = new BTree(postfix);

			//	//btr.Print();

			//	//btr.Sort();

			//	//btr.Print();

			//	//btr.Prune();

			//	//string simpled = PolishSimplificator.Simplify(postfix);
			//	//BTree btr2 = new BTree(simpled);

			//	//btr.Print();
			//	//btr2.Print();

			//	PostfixSimplificator yas = new PostfixSimplificator(postfix);
			//	yas.Prune();

			//	string yay = yas.GetPostfix();

			//	_PrintStuff(postfix, yay);

			//} while (Console.ReadKey().Key != ConsoleKey.Escape);
		}

		private static void GetNegaTerms(int negaMask, Minterm item, out int negative, out int positive) {
			//GetNegaTerms(out int negative, out int positive)
			Stack<int> pStack = new Stack<int>();
			Stack<int> nStack = new Stack<int>();
			int pos = 0;
			int mask = negaMask;
			int term = item.Term;
			while (mask > 0) {
				if ((mask & 1) == 1) {
					//negative
					nStack.Push(1 & (term & 1));
				} else {
					//positive
					pStack.Push(1 & (term & 1));
				}
				mask >>= 1;
				term >>= 1;
				pos++;
			}
			positive = 0;
			while (pStack.Count > 0) {
				positive <<= 1;
				positive += pStack.Pop();
			}
			negative = 0;
			while (nStack.Count > 0) {
				negative <<= 1;
				negative += nStack.Pop();
			}
		}

		private static PostfixLogic _AddRandomNots(PostfixLogic postfix, int notCount, int seed) {
			if (notCount <= 0) return postfix;

			PostfixLogic retList = new(postfix);
			Random rnd = new Random(seed);
			for (int i = 0; i < notCount; i++) {
				retList.Insert(rnd.Next(1, postfix.Count), new Token(Ops.Not));
			}
			return retList;
		}

		private static List<string> ReplaceFromTempTokens(List<string> values, ref Dictionary<string, List<string>> temp) {
			bool flattened = false;
			Console.WriteLine("-----------");
			Console.WriteLine("REPLACE_TEMP:");

			while (flattened == false) {
				flattened = true;
				Console.WriteLine(string.Join(" ", values.ToArray()));
				List<string> newValues = new List<string>();
				foreach (string val in values) {
					if (IsTemp(val)) {
						newValues.AddRange(temp[val]);
						flattened = false;
					} else {
						newValues.Add(val);
					}
				}
				values = newValues;
			}

			return values;
		}

		private static void DictDump(Dictionary<string, List<string>> dict) {
			if (dict.Count > 0) {
				foreach (string key in dict.Keys) {
					Console.WriteLine(key + " = " + string.Join(" ", dict[key].ToArray()));
				}
			}
		}

		private static List<string> GetRemainderAgainstTarget(List<string> values, int index, ref Dictionary<string, List<string>> dict) {
			string arg1 = values[index - 2];
			string arg2 = values[index - 1];
			string op = values[index];
			string result = "";

			Console.WriteLine("Remainder: " + arg1 + ' ' + arg2 + ' ' + op);

			if (IsTarget(arg1) || IsTarget(arg2)) {
				if (IsTarget(arg1) && IsTarget(arg2)) {
					string tempName = "_" + dict.Count;
					dict.Add(tempName, new List<string> { arg1, arg2, op });
					result = tempName;
				} else if (arg1 == "f" || arg2 == "f") {
					if (op == "+") {
						result = "f";
					} else { //Op must be "|"
						result = arg1 == "f" ? arg2 : arg1; //Get the other arg
					}
				} else { //One arg must be "t"
					if (op == "+") {
						result = arg1 == "t" ? arg2 : arg1; //Get the other arg
					} else { //Op must be "|"
						result = "t";
					}
				}
			} else { //No special cases
				if (op == "+") {
					result = (arg1 == "t" && arg2 == "t") ? "t" : "f";
				} else { //Op must be "|"
					result = (arg1 == "f" && arg2 == "f") ? "f" : "t";
				}
			}

			Console.WriteLine("Result: " + result);

			values[index] = result;
			values.RemoveAt(index - 1);
			values.RemoveAt(index - 2);

			return values;
		}

















		private static bool IsTemp(string op) {
			if (op.StartsWith("_"))
				return true;
			return false;
		}

		private static Dictionary<string, bool> TARGET = new Dictionary<string, bool> {
			{ "A", true },
			{ "B", true },
			{ "C", true },
			{ "D", true },
			{ "E", true },
			{ "F", true },
			{ "G", true },
			{ "H", true },
			{ "I", true },
			{ "K", true },
			{ "L", true },
			{ "M", true },
			{ "N", true },
			{ "O", true },
		};

		private static bool IsTarget(string op) {
			if (IsTemp(op))
				return true;


			foreach (string key in TARGET.Keys) {
				if (op == key) {
					return TARGET[key];
				}
			}

			return false;
		}




























































		[DllImport("user32.dll")]
		public static extern bool ShowWindow(System.IntPtr hWnd, int cmdShow);
		private static void Maximize() {
			Process p = Process.GetCurrentProcess();
			ShowWindow(p.MainWindowHandle, 3); //SW_MAXIMIZE = 3
		}

		private static string _MakeRandomLogic(bool positiveOnly, int operandCount, int parensCount, int uniqueTokens, int andToOrRatio, int seed) {
			int tokenCount = operandCount * 2 - 1;

			List<string> keyNames = null;
			Random rnd = new Random(seed);
			if (keyNames == null) {
				keyNames = new List<string>();
				while (keyNames.Count < uniqueTokens) {
					string str = $"{(char)('A' + rnd.Next(0, 26))}{(char)('A' + rnd.Next(0, 26))}{(char)('A' + rnd.Next(0, 26))}";
					if (!keyNames.Contains(str))
						keyNames.Add(str);
				}
			}


			List<string> list = new List<string>();

			bool needOperator = false;
			int lastOperand = 0;

			//Get a list of Letters.
			while (list.Count < tokenCount) {
				if (!needOperator) {
					//Letter
					list.Add(__RandOperand());
					lastOperand = list.Count;
					needOperator = true;
				} else {
					//Operator
					string op = __RandOperator();
					if (op == "!") {
						string op2 = __RandOperator(false);
						list.Add(op2);
					}
					list.Add(op);
					needOperator = false;
				}
			}

			while (list.Count > lastOperand) {
				list = list.GetRange(0, lastOperand);
			}
			tokenCount = list.Count;

			//Get some pairs with which to randomly place some parentheses
			List<int> pair1 = new List<int>();
			List<int> pair2 = new List<int>();
			while (pair1.Count < parensCount) {
				int one = rnd.Next(tokenCount);
				if (__isOp(list[one])) continue; //reject
				int two = rnd.Next(tokenCount);
				if (__isOp(list[two])) continue; //reject
				if (one == two) continue; //reject

				pair1.Add(one < two ? one : two); //Low number first
				pair2.Add(one > two ? one : two); //High number next!
			}

			//Combinarooney...
			for (int i = 0; i < pair1.Count; i++) {
				int lo = pair1[i];
				int hi = pair2[i];

				list[lo] = $"({list[lo]}";  //Low number gets a '('
				list[hi] = $"{list[hi]})";  //High number gets a ')'
			}

			//Console.WriteLine(string.Join(" ", list));

			return string.Join(" ", list.ToArray());

			string __RandOperand() {
				return keyNames[rnd.Next(0, keyNames.Count)];
			}

			string __RandOperator(bool includeNots = true) {
				int rand = rnd.Next(includeNots ? 5 : 4);
				if (positiveOnly || rand == 0 || rand == 1) {
					return rnd.Next(andToOrRatio + 1) == 0 ? "|" : "&";
				}
				if (rand == 4) {
					return "!";
				}
				return rnd.Next(andToOrRatio + 1) == 0 ? ":" : "@";
			}

			bool __isOp(string token) {
				switch (token) {
					case "&":
					case "|":
					case "@":
					case ":":
					case "!":
						return true;
					default:
						return false;
				}
			}
		}


		static void _DisplayTree(string postfix, bool replace = false) {
			BTree btr = new BTree(postfix, replace);
			btr.Print();
		}
	}
}
