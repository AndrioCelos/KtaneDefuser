using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace BombDefuserScripts;

internal static class NATO {
	private static readonly Dictionary<char, string> EncodeMap = new(new CaseInsensitiveCharComparer());
	private static readonly Dictionary<string, char> DecodeMap = new(StringComparer.CurrentCultureIgnoreCase);

	private static void Map(char c, string codeWord) {
		EncodeMap.TryAdd(c, codeWord);
		DecodeMap[codeWord] = c;
	}

	static NATO() {
		Map('A', "Alfa");
		Map('A', "Alpha");
		Map('B', "Bravo");
		Map('C', "Charlie");
		Map('D', "Delta");
		Map('E', "Echo");
		Map('F', "Foxtrot");
		Map('G', "Golf");
		Map('H', "Hotel");
		Map('I', "India");
		Map('J', "Juliet");
		Map('K', "Kilo");
		Map('L', "Lima");
		Map('M', "Mike");
		Map('N', "November");
		Map('O', "Oscar");
		Map('P', "Papa");
		Map('Q', "Quebec");
		Map('R', "Romeo");
		Map('S', "Sierra");
		Map('T', "Tango");
		Map('U', "Uniform");
		Map('Ü', "Uniform umlaut");
		Map('V', "Victor");
		Map('W', "Whiskey");
		Map('X', "X-ray");
		Map('X', "Xray");
		Map('Y', "Yankee");
		Map('Z', "Zulu");
		Map('0', "Zero");
		Map('1', "One");
		Map('2', "Two");
		Map('3', "Three");
		Map('4', "Four");
		Map('5', "Five");
		Map('6', "Six");
		Map('7', "Seven");
		Map('8', "Eight");
		Map('9', "Nine");
	}

	public static string Speak(string s) {
		var builder = new StringBuilder();
		Speak(builder, s);
		return builder.ToString();
	}
	public static void Speak(StringBuilder builder, string s) {
		builder.Append("<oob><speak><s>");
		var any = false;
		foreach (var c in s) {
			if (char.IsWhiteSpace(c)) continue;
			if (any)
				builder.Append(' ');
			else
				any = true;

			if (c is >= '0' and <= '9')
				builder.Append($"<say-as interpret-as='number'>{c}</say-as>");
			else if (EncodeMap.TryGetValue(c, out var codeWord))
				builder.Append(codeWord);
			else
				builder.Append(c);
		}
		builder.Append($"</s></speak><alt>{s}</alt></oob>");
	}

	private class CaseInsensitiveCharComparer : IEqualityComparer<char> {
		public bool Equals(char x, char y) => char.ToLower(x) == char.ToLower(y);
		public int GetHashCode([DisallowNull] char c) => char.ToLower(c).GetHashCode();
	}
}