/******************************************************************************
 * Spine Runtimes License Agreement
 * Last updated May 1, 2019. Replaces all prior versions.
 *
 * Copyright (c) 2013-2019, Esoteric Software LLC
 *
 * Integration of the Spine Runtimes into software or otherwise creating
 * derivative works of the Spine Runtimes is permitted under the terms and
 * conditions of Section 2 of the Spine Editor License Agreement:
 * http://esotericsoftware.com/spine-editor-license
 *
 * Otherwise, it is permitted to integrate the Spine Runtimes into software
 * or otherwise create derivative works of the Spine Runtimes (collectively,
 * "Products"), provided that each user of the Products must obtain their own
 * Spine Editor license and redistribution of the Products in any form must
 * include this license and copyright notice.
 *
 * THIS SOFTWARE IS PROVIDED BY ESOTERIC SOFTWARE LLC "AS IS" AND ANY EXPRESS
 * OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN
 * NO EVENT SHALL ESOTERIC SOFTWARE LLC BE LIABLE FOR ANY DIRECT, INDIRECT,
 * INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
 * BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES, BUSINESS
 * INTERRUPTION, OR LOSS OF USE, DATA, OR PROFITS) HOWEVER CAUSED AND ON ANY
 * THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE,
 * EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 *****************************************************************************/

using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Globalization;
using System.Collections.Generic;

namespace Spine {
	/// <summary>骨架 JSON 的反序列化入口。</summary>
	public static class Json {
		/// <summary>从文本读取器反序列化 JSON，返回字典、列表或基础值。</summary>
		public static object Deserialize (TextReader text) {
			var parser = new SharpJson.JsonDecoder();
			parser.parseNumbersAsFloat = true;
			return parser.Decode(text.ReadToEnd());
		}
	}
}

/**
 * Copyright (c) 2016 Adriano Tinoco d'Oliveira Rezende
 *
 * Based on the JSON parser by Patrick van Bergen
 * http://techblog.procurios.nl/k/news/view/14605/14863/how-do-i-write-my-own-parser-(for-json).html
 *
 * Changes made:
 *
 * - Optimized parser speed (deserialize roughly near 3x faster than original)
 * - Added support to handle lexer/parser error messages with line numbers
 * - Added more fine grained control over type conversions during the parsing
 * - Refactory API (Separate Lexer code from Parser code and the Encoder from Decoder)
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this software
 * and associated documentation files (the "Software"), to deal in the Software without restriction,
 * including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
 * and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
 * subject to the following conditions:
 * The above copyright notice and this permission notice shall be included in all copies or substantial
 * portions of the Software.
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT
 * LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
 * WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE
 * OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
namespace SharpJson
{
	/// <summary>识别 JSON 字符串、数字和分隔符的词法分析器。</summary>
	class Lexer
	{
		/// <summary>JSON 词法单元类型。</summary>
		public enum Token {
			/// <summary>没有有效词法单元。</summary>
			None,
			/// <summary>JSON 空值。</summary>
			Null,
			/// <summary>JSON 布尔真值。</summary>
			True,
			/// <summary>JSON 布尔假值。</summary>
			False,
			/// <summary>键值之间的冒号。</summary>
			Colon,
			/// <summary>元素之间的逗号。</summary>
			Comma,
			/// <summary>JSON 字符串。</summary>
			String,
			/// <summary>JSON 数字。</summary>
			Number,
			/// <summary>对象开始的大括号。</summary>
			CurlyOpen,
			/// <summary>对象结束的大括号。</summary>
			CurlyClose,
			/// <summary>数组开始的方括号。</summary>
			SquaredOpen,
			/// <summary>数组结束的方括号。</summary>
			SquaredClose,
		};

		/// <summary>词法分析是否发生错误。</summary>
		public bool hasError {
			get {
				return !success;
			}
		}

		/// <summary>当前解析行号。</summary>
		public int lineNumber {
			get;
			private set;
		}

		/// <summary>是否将 JSON 数字解析为单精度浮点数。</summary>
		public bool parseNumbersAsFloat {
			get;
			set;
		}

		/// <summary>待解析的 JSON 字符数组。</summary>
		char[] json;
		/// <summary>在所属集合中的索引。</summary>
		int index = 0;
		/// <summary>当前词法解析是否成功。</summary>
		bool success = true;
		/// <summary>复用的 JSON 字符串解码缓冲。</summary>
		char[] stringBuffer = new char[4096];

		/// <summary>创建识别 JSON 字符串、数字和分隔符的词法分析器，并初始化关联数据及缓冲区。</summary>
		public Lexer(string text)
		{
			Reset();

			json = text.ToCharArray();
			parseNumbersAsFloat = false;
		}

		/// <summary>重置词法分析位置和错误状态，从文本开头重新解析。</summary>
		public void Reset()
		{
			index = 0;
			lineNumber = 1;
			success = true;
		}

		/// <summary>读取并解码 JSON 字符串及转义字符。</summary>
		public string ParseString()
		{
			int idx = 0;
			StringBuilder builder = null;

			SkipWhiteSpaces();

			// "
			char c = json[index++];

			bool failed = false;
			bool complete = false;

			while (!complete && !failed) {
				if (index == json.Length)
					break;

				c = json[index++];
				if (c == '"') {
					complete = true;
					break;
				} else if (c == '\\') {
					if (index == json.Length)
						break;

					c = json[index++];

					switch (c) {
					case '"':
						stringBuffer[idx++] = '"';
						break;
					case '\\':
						stringBuffer[idx++] = '\\';
						break;
					case '/':
						stringBuffer[idx++] = '/';
						break;
					case 'b':
						stringBuffer[idx++] = '\b';
						break;
					case'f':
							stringBuffer[idx++] = '\f';
						break;
					case 'n':
						stringBuffer[idx++] = '\n';
						break;
					case 'r':
						stringBuffer[idx++] = '\r';
						break;
					case 't':
						stringBuffer[idx++] = '\t';
						break;
					case 'u':
						int remainingLength = json.Length - index;
						if (remainingLength >= 4) {
							var hex = new string(json, index, 4);

							// 待完善项：处理 UTF 字符转义。 原注： XXX: handle UTF
							stringBuffer[idx++] = (char) Convert.ToInt32(hex, 16);

							// 跳过四个十六进制字符。 原注： skip 4 chars
							index += 4;
						} else {
							failed = true;
						}
						break;
					}
				} else {
					stringBuffer[idx++] = c;
				}

				if (idx >= stringBuffer.Length) {
					if (builder == null)
						builder = new StringBuilder();

					builder.Append(stringBuffer, 0, idx);
					idx = 0;
				}
			}

			if (!complete) {
				success = false;
				return null;
			}

			if (builder != null)
				return builder.ToString ();
			else
				return new string (stringBuffer, 0, idx);
		}

		/// <summary>取得当前位置的 JSON 数字文本并推进读取位置。</summary>
		string GetNumberString()
		{
			SkipWhiteSpaces();

			int lastIndex = GetLastIndexOfNumber(index);
			int charLength = (lastIndex - index) + 1;

			var result = new string (json, index, charLength);

			index = lastIndex + 1;

			return result;
		}

		/// <summary>读取 JSON 数字并返回单精度浮点值。</summary>
		public float ParseFloatNumber()
		{
			float number;
			var str = GetNumberString ();

			if (!float.TryParse (str, NumberStyles.Float, CultureInfo.InvariantCulture, out number))
				return 0;

			return number;
		}

		/// <summary>读取 JSON 数字并返回双精度浮点值。</summary>
		public double ParseDoubleNumber()
		{
			double number;
			var str = GetNumberString ();

			if (!double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out number))
				return 0;

			return number;
		}

		/// <summary>查找连续数字文本的末尾索引。</summary>
		int GetLastIndexOfNumber(int index)
		{
			int lastIndex;

			for (lastIndex = index; lastIndex < json.Length; lastIndex++) {
				char ch = json[lastIndex];

				if ((ch < '0' || ch > '9') && ch != '+' && ch != '-'
				    && ch != '.' && ch != 'e' && ch != 'E')
					break;
			}

			return lastIndex - 1;
		}

		/// <summary>跳过空格、制表符和换行，准备读取下一个词法单元。</summary>
		void SkipWhiteSpaces()
		{
			for (; index < json.Length; index++) {
				char ch = json[index];

				if (ch == '\n')
					lineNumber++;

				if (!char.IsWhiteSpace(json[index]))
					break;
			}
		}

		/// <summary>查看下一个词法单元，不消耗该单元。</summary>
		public Token LookAhead()
		{
			SkipWhiteSpaces();

			int savedIndex = index;
			return NextToken(json, ref savedIndex);
		}

		/// <summary>读取下一个词法单元并推进解析位置。</summary>
		public Token NextToken()
		{
			SkipWhiteSpaces();
			return NextToken(json, ref index);
		}

		/// <summary>读取下一个词法单元并推进解析位置。</summary>
		static Token NextToken(char[] json, ref int index)
		{
			if (index == json.Length)
				return Token.None;

			char c = json[index++];

			switch (c) {
			case '{':
				return Token.CurlyOpen;
			case '}':
				return Token.CurlyClose;
			case '[':
				return Token.SquaredOpen;
			case ']':
				return Token.SquaredClose;
			case ',':
				return Token.Comma;
			case '"':
				return Token.String;
			case '0': case '1': case '2': case '3': case '4':
			case '5': case '6': case '7': case '8': case '9':
			case '-':
				return Token.Number;
			case ':':
				return Token.Colon;
			}

			index--;

			int remainingLength = json.Length - index;

			// 识别 JSON 的 false 字面量。 原注： false
			if (remainingLength >= 5) {
				if (json[index] == 'f' &&
				    json[index + 1] == 'a' &&
				    json[index + 2] == 'l' &&
				    json[index + 3] == 's' &&
				    json[index + 4] == 'e') {
					index += 5;
					return Token.False;
				}
			}

			// 识别 JSON 的 true 字面量。 原注： true
			if (remainingLength >= 4) {
				if (json[index] == 't' &&
				    json[index + 1] == 'r' &&
				    json[index + 2] == 'u' &&
				    json[index + 3] == 'e') {
					index += 4;
					return Token.True;
				}
			}

			// 识别 JSON 的 null 字面量。 原注： null
			if (remainingLength >= 4) {
				if (json[index] == 'n' &&
				    json[index + 1] == 'u' &&
				    json[index + 2] == 'l' &&
				    json[index + 3] == 'l') {
					index += 4;
					return Token.Null;
				}
			}

			return Token.None;
		}
	}

	/// <summary>将 JSON 文本解析为字典、列表和基础值的解码器。</summary>
	public class JsonDecoder
	{
		/// <summary>最后一次解析失败的说明。</summary>
		public string errorMessage {
			get;
			private set;
		}

		/// <summary>是否将 JSON 数字解析为单精度浮点数。</summary>
		public bool parseNumbersAsFloat {
			get;
			set;
		}

		/// <summary>当前 JSON 词法分析器。</summary>
		Lexer lexer;

		/// <summary>创建将 JSON 文本解析为字典、列表和基础值的解码器，并初始化关联数据及缓冲区。</summary>
		public JsonDecoder()
		{
			errorMessage = null;
			parseNumbersAsFloat = false;
		}

		/// <summary>解析 JSON 文本为对象树，记录错误信息。</summary>
		public object Decode(string text)
		{
			errorMessage = null;

			lexer = new Lexer(text);
			lexer.parseNumbersAsFloat = parseNumbersAsFloat;

			return ParseValue();
		}

		/// <summary>创建解码器并解析给定 JSON 文本。</summary>
		public static object DecodeText(string text)
		{
			var builder = new JsonDecoder();
			return builder.Decode(text);
		}

		/// <summary>读取 JSON 对象成员并生成字符串键字典。</summary>
		IDictionary<string, object> ParseObject()
		{
			var table = new Dictionary<string, object>();

			// {
			lexer.NextToken();

			while (true) {
				var token = lexer.LookAhead();

				switch (token) {
				case Lexer.Token.None:
					TriggerError("Invalid token");
					return null;
				case Lexer.Token.Comma:
					lexer.NextToken();
					break;
				case Lexer.Token.CurlyClose:
					lexer.NextToken();
					return table;
				default:
					// 读取对象成员名称。 原注： name
					string name = EvalLexer(lexer.ParseString());

					if (errorMessage != null)
						return null;

					// :
					token = lexer.NextToken();

					if (token != Lexer.Token.Colon) {
						TriggerError("Invalid token; expected ':'");
						return null;
					}

					// 读取对象成员的值。 原注： value
					object value = ParseValue();

					if (errorMessage != null)
						return null;

					table[name] = value;
					break;
				}
			}

			// 保留的参考返回语句，此分支不可到达。 原注：return null; // Unreachable code
		}

		/// <summary>读取 JSON 数组元素并生成列表。</summary>
		IList<object> ParseArray()
		{
			var array = new List<object>();

			// [
			lexer.NextToken();

			while (true) {
				var token = lexer.LookAhead();

				switch (token) {
				case Lexer.Token.None:
					TriggerError("Invalid token");
					return null;
				case Lexer.Token.Comma:
					lexer.NextToken();
					break;
				case Lexer.Token.SquaredClose:
					lexer.NextToken();
					return array;
				default:
					object value = ParseValue();

					if (errorMessage != null)
						return null;

					array.Add(value);
					break;
				}
			}

			// 保留的参考返回语句，此分支不可到达。 原注：return null; // Unreachable code
		}

		/// <summary>根据下一词法单元选择对象、数组、字符串、数字或字面量解析。</summary>
		object ParseValue()
		{
			switch (lexer.LookAhead()) {
			case Lexer.Token.String:
				return EvalLexer(lexer.ParseString());
			case Lexer.Token.Number:
				if (parseNumbersAsFloat)
					return EvalLexer(lexer.ParseFloatNumber());
				else
					return EvalLexer(lexer.ParseDoubleNumber());
			case Lexer.Token.CurlyOpen:
				return ParseObject();
			case Lexer.Token.SquaredOpen:
				return ParseArray();
			case Lexer.Token.True:
				lexer.NextToken();
				return true;
			case Lexer.Token.False:
				lexer.NextToken();
				return false;
			case Lexer.Token.Null:
				lexer.NextToken();
				return null;
			case Lexer.Token.None:
				break;
			}

			TriggerError("Unable to parse value");
			return null;
		}

		/// <summary>记录带行号的解析错误信息。</summary>
		void TriggerError(string message)
		{
			errorMessage = string.Format("Error: '{0}' at line {1}",
			                             message, lexer.lineNumber);
		}

		/// <summary>检查词法分析结果，失败时把错误传递给解码器。</summary>
		T EvalLexer<T>(T value)
		{
			if (lexer.hasError)
				TriggerError("Lexical error ocurred");

			return value;
		}
	}
}
