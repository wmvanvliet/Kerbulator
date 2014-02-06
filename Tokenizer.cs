// File encoding should remain in UTF-8!

using System;
using System.IO;
using System.Collections.Generic;

namespace Kalculator {
	public enum TokenType { EMPTY, NUMBER, IDENTIFIER, OPERATOR, BRACE, LIST, END, COMMA, TEXT, IN, OUT};
	public class Token {
		public TokenType type;
		public string val;

		public Token() {
			this.type = TokenType.EMPTY;
			this.val = "";
		}

		public Token(TokenType type, string val) {
			this.type = type;
			this.val = val;
		}
		
		override public string ToString() {
			return Enum.GetName(typeof(TokenType), this.type) +": "+ this.val;
		}
	}

	public class Tokenizer {
		public Queue<Token> tokens;
		public Tokenizer() {
			tokens = new Queue<Token>();
		}

		public void Tokenize(string line) {
			Token tok = new Token();
			for(int i=0; i<line.Length; i++) {
				char c = line[i];
				switch(c)
				{
					case ' ':
						HandleToken(tok);
						tok = new Token();
						break;

					case '\n':
						HandleToken(tok);
						HandleToken(new Token(TokenType.END, "\n"));
						tok = new Token();
						break;

					case '#':
						HandleToken(tok);
						// Skip to next newline
						i = line.IndexOf('\n', i+1) - 1;
						if(i < 0)
							i = line.Length-1;
						break;

					case '"':
						HandleToken(tok);

						// Read until next unescaped "
						string text = "";
						for(i=i+1; i<line.Length; i++) {
							if(line[i] == '\\') {
								if(i >= line.Length - 1)
									throw new Exception("Unterminated quoted string.");
								text += line[i+1];
								i += 2;
							} else if(line[i] != '"') {
								text += line[i];
							} else {
								break;
							}
						}

						HandleToken(new Token(TokenType.TEXT, text));
						tok = new Token();
						break;

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
							throw new Exception("invalid char");
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
								i++;
							}
						} else
							throw new Exception("invalid char");
						break;

					case '.':
						if(tok.type == TokenType.EMPTY) {
							tok.type = TokenType.NUMBER;
							tok.val = c.ToString();
						} else if(tok.type == TokenType.NUMBER) {
							if(tok.val.IndexOf(c) >= 0)
								throw new Exception("invalid char");
							tok.val += c;
						} else if(tok.type == TokenType.IDENTIFIER) {
							HandleToken(tok);
							tok.val += c;
						} else 
							throw new Exception("invalid char");
						break;

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
					case '=':
						HandleToken(tok);
						HandleToken(new Token(TokenType.OPERATOR, c.ToString()));
						tok = new Token();
						break;

					case '[':
					case ']':
						HandleToken(tok);
						HandleToken(new Token(TokenType.LIST, c.ToString()));
						tok = new Token();
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
						HandleToken(new Token(TokenType.BRACE, c.ToString()));
						tok = new Token();
						break;

					case ':':
						if(tok.val == "in")
							HandleToken(new Token(TokenType.IN, tok.val));
						else if(tok.val == "out")
							HandleToken(new Token(TokenType.OUT, tok.val));
						else
							throw new Exception("this is not in or out: "+ tok.val);

						tok = new Token();
						break;

					case ',':
						HandleToken(tok);
						HandleToken(new Token(TokenType.COMMA, c.ToString()));
						tok = new Token();
						break;

					default:
						if(tok.type != TokenType.EMPTY && tok.type != TokenType.IDENTIFIER)
							throw new Exception("invalid char: "+ c);
						tok.type = TokenType.IDENTIFIER;
						tok.val += c;
						break;
				}
			}

			HandleToken(tok);
		}
		
		private void HandleToken(Token t) {
			switch(t.type) {
				case TokenType.EMPTY:
					break;

				default:
					Kalculator.Debug(" "+ t.ToString());
					tokens.Enqueue(t);
					break;
			}
		}
	}
}
