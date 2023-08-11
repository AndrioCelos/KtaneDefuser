using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BombDefuserScripts;
[AimlInterface]
internal static class NatoAiml {
	[AimlCategory("DecodeNato *")]
	public static string DecodeNato(string phrase) => string.Join(null, phrase.Split((char[]?) null, StringSplitOptions.RemoveEmptyEntries).Select(NATO.DecodeChar));
}
