// File encoding should remain in UTF-8!

using System;
using System.IO;
using System.Collections.Generic;

namespace Kerbulator {
	public enum TokenType {
		EMPTY,
		NUMBER,
		IDENTIFIER,
		OPERATOR,
		BRACE,
		LIST,
		END,
		ASSIGN,
		COMMA,
		COLON,
		TEXT,
		IN,
		OUT,
		MANEUVER,
		ALARM,
		SKIP_NEWLINE,
		PIECEWISE,
		CONDITIONAL
	};

	public class Token {
		public TokenType type;
		public string val;
		public string func;
		public int line;
		public int col;

		public Token() {
			this.type = TokenType.EMPTY;
			this.val = "";
			this.func = null;
			this.line = -1;
			this.col = -1;
		}

		public Token(string func, int line, int col) {
			this.type = TokenType.EMPTY;
			this.val = "";
			this.func = func;
			this.line = line;
			this.col = col;
		}

		public Token(TokenType type, string val) {
			this.type = type;
			this.val = val;
			this.func = null;
			this.line = -1;
			this.col = -1;
		}

		public Token(TokenType type, string val, string func, int line, int col) {
			this.type = type;
			this.val = val;
			this.func = func;
			this.line = line;
			this.col = col;
		}
		
		override public string ToString() {
			return Enum.GetName(typeof(TokenType), this.type) +": "+ this.val;
		}

		public string pos {
			get {
				string err = "";
				if(func != null)
					err += "In function "+ func;

				if(line > 0) {
					err += " (line "+ line;
					if(col > 0)
						err += ", col "+ col;
					err += ")";
				}

				return err +": ";
			}

			protected set {}
		}

	}

	public class Tokenizer {
		public Queue<Token> tokens;
		private string functionName;

		public Tokenizer(string functionName) {
			tokens = new Queue<Token>();
			this.functionName = functionName;
		}

		public void Tokenize(string line) {
			int lineno = 1;
			int col = 1;
			Token tok = new Token(functionName, lineno, col);
			for(int i=0; i<line.Length; i++, col++) {
				char c = line[i];
				switch(c)
				{
					// Whitespace
					case ' ':
					case '\t':
					case '\r':
						if(tok.type != TokenType.SKIP_NEWLINE) {
							HandleToken(tok);
							tok = new Token(functionName, lineno, col + 1);
						}
						break;

					// End of statement
					case '\n':
						if(tok.type != TokenType.SKIP_NEWLINE) {
							HandleToken(tok);
							HandleToken(new Token(TokenType.END, "\\n", functionName, lineno, col));
						}
						lineno ++;
						col = 0;
						tok = new Token(functionName, lineno, col + 1);
						break;

					// Comments
					case '#':
						HandleToken(tok);
						// Skip to next newline
						int newI = line.IndexOf('\n', i+1) - 1;
						if(newI < 0)
							newI = line.Length-1;
						col += newI - i;
						i = newI;
						tok = new Token(functionName, lineno, col);
						break;

					// Quoted strings
					case '"':
						HandleToken(tok);
						tok = new Token(TokenType.TEXT, "", functionName, lineno, col);

						// Read until next unescaped "
						bool terminated = false;
						for(i=i+1, col++; i<line.Length; i++, col++) {
							if(line[i] == '\\') {
								if(i >= line.Length - 1)
									break;
								tok.val += line[i+1];
								i += 2; col += 2;
							} else if(line[i] == '"') {
								terminated = true;
								break;
							} else {
								tok.val += line[i];
							}
						}

						if(!terminated)
							throw new Exception(tok.pos + "missing end quote (\") for string");

						HandleToken(tok);
						tok = new Token(functionName, lineno, col);
						break;

					// Numbers
					case '0':
					case '1':
					case '2':
					case '3':
					case '4':
					case '5':
					case '6':
					case '7':
					case '8':
					case '9':
						if(tok.type == TokenType.EMPTY) {
							tok.type = TokenType.NUMBER;
							tok.val = c.ToString();
						} else if(tok.type == TokenType.NUMBER || tok.type == TokenType.IDENTIFIER)
							tok.val += c;
						else
							throw new Exception(tok.pos + c +" is invalid here");
						break;

					case 'e':
					case 'E':
						if(tok.type == TokenType.EMPTY) {
							tok.type = TokenType.IDENTIFIER;
							tok.val = c.ToString();
						} else if(tok.type == TokenType.NUMBER || tok.type == TokenType.IDENTIFIER) {
							tok.val += c;
							if(i < line.Length - 1 && line[i+1] == '-') {
								tok.val += line[i+1].ToString();
								i++; col++;
							}
						} else
							throw new Exception(tok.pos + c +" is invalid here");
						break;

					case '.':
						if(tok.type == TokenType.EMPTY) {
							tok.type = TokenType.NUMBER;
							tok.val = c.ToString();
						} else if(tok.type == TokenType.NUMBER) {
							if(tok.val.IndexOf(c) >= 0)
								throw new Exception(tok.pos +"numbers can only have one decimal point (.) in them");
							tok.val += c;
						} else if(tok.type == TokenType.IDENTIFIER) {
							tok.val += c;
						} else 
							throw new Exception(tok.pos +"variable/function names cannot start with a dot (.)");
						break;

					// Operators
					case '-':
					case '+':
					case '/':
					case '÷':
					case '√':
					case '%':
					case '*':
					case '·':
					case '×':
					case '^':
					case '≤':
					case '≥':
					case '≠':
					case '¬':
					case '∧':
					case '∨':
						HandleToken(tok);
						HandleToken(new Token(TokenType.OPERATOR, c.ToString(), functionName, lineno, col));
						tok = new Token(functionName, lineno, col + 1);
						break;

					// Operators that can possibly expand to two characters (<=, =>, ==, !=)
					case '<':
						HandleToken(tok);
						if(i < line.Length - 1 && line[i+1] == '=') {
							HandleToken(new Token(TokenType.OPERATOR, "<=", functionName, lineno, col));
							col++; i++;
						} else {
							HandleToken(new Token(TokenType.OPERATOR, "<", functionName, lineno, col));
						}
						tok = new Token(functionName, lineno, col + 1);
						break;

					case '>':
						HandleToken(tok);
						if(i < line.Length - 1 && line[i+1] == '=') {
							HandleToken(new Token(TokenType.OPERATOR, ">=", functionName, lineno, col));
							col++; i++;
						} else {
							HandleToken(new Token(TokenType.OPERATOR, ">", functionName, lineno, col));
						}
						tok = new Token(functionName, lineno, col + 1);
						break;

					case '!':
						HandleToken(tok);
						if(i < line.Length - 1 && line[i+1] == '=') {
							HandleToken(new Token(TokenType.OPERATOR, "!=", functionName, lineno, col));
							col++; i++;
						} else {
							HandleToken(new Token(TokenType.OPERATOR, "!", functionName, lineno, col));
						}
						tok = new Token(functionName, lineno, col + 1);
						break;

					// The =, ==, ={ case
					case '=':
						HandleToken(tok);
						if(i < line.Length - 1 && line[i+1] == '=') {
							HandleToken(new Token(TokenType.OPERATOR, "==", functionName, lineno, col));
							col++; i++;
						} else if(i < line.Length - 1 && line[i+1] == '{') {
							HandleToken(new Token(TokenType.PIECEWISE, "={", functionName, lineno, col));
							col++; i++;
						} else {
							HandleToken(new Token(TokenType.ASSIGN, "=", functionName, lineno, col));
						}
						tok = new Token(functionName, lineno, col + 1);
						break;

					// Brackets
					case '[':
					case ']':
						HandleToken(tok);
						HandleToken(new Token(TokenType.LIST, c.ToString(), functionName, lineno, col));
						tok = new Token(functionName, lineno, col + 1);
						break;

					case '(':
					case ')':
					case '{':
					case '}':
					case '⌊':
					case '⌋':
					case '⌈':
					case '⌉':
					case '|':
						HandleToken(tok);
						HandleToken(new Token(TokenType.BRACE, c.ToString(), functionName, lineno, col));
						tok = new Token(functionName, lineno, col + 1);
						break;

					// In: and Out: statements
					case ':':
                        if(tok.val == "in")
                            HandleToken(new Token(TokenType.IN, tok.val, functionName, lineno, col));
                        else if(tok.val == "out")
                            HandleToken(new Token(TokenType.OUT, tok.val, functionName, lineno, col));
                        else if(tok.val == "maneuver")
                            HandleToken(new Token(TokenType.MANEUVER, tok.val, functionName, lineno, col));
						else if(tok.val == "alarm")
							HandleToken(new Token(TokenType.ALARM, tok.val, functionName, lineno, col));
                        else {
                            HandleToken(tok);
                            HandleToken(new Token(TokenType.COLON, tok.val, functionName, lineno, col));
                        }

						tok = new Token(functionName, lineno, col + 1);
						break;

					// Others
					case ',':
						HandleToken(tok);
						HandleToken(new Token(TokenType.COMMA, c.ToString(), functionName, lineno, col));
						tok = new Token(functionName, lineno, col + 1);
						break;

					case '\\':
						HandleToken(tok);
						tok = new Token(TokenType.SKIP_NEWLINE, c.ToString(), functionName, lineno, col);
						break;

					default:
						if(tok.type != TokenType.EMPTY && tok.type != TokenType.IDENTIFIER)
							throw new Exception(tok.pos + "unexpected character: "+ c);
						tok.type = TokenType.IDENTIFIER;
						tok.val += c;
						break;
				}
			}

			HandleToken(tok);
			Kerbulator.Debug("\n");
		}
		
		private void HandleToken(Token t) {
			switch(t.type) {
				case TokenType.EMPTY:
					return;

				// Some identifiers are special
				case TokenType.IDENTIFIER:
					// These are actually operators
					if(t.val == "and" || t.val == "or")
						t.type = TokenType.OPERATOR;

					// These are actually conditionals
					else if(t.val == "if" || t.val == "otherwise")
						t.type = TokenType.CONDITIONAL;
					break;
			}

			Kerbulator.Debug(" "+ t.ToString());
			tokens.Enqueue(t);
		}
	}
}
