enum TokenType{
	NONE,

	IDENTIFIER, STRING_LITERAL, NUMBER_LITERAL, FALSE, TRUE, NULL,

	// Basic grammar
	L_PAREN, R_PAREN,
	L_SQ_BRACE, R_SQ_BRACE,
	L_BRACE, R_BRACE,
	SEMICOLON, COLON, COMMA, DOT,

	BANG, EQUAL, BANG_EQUALS, EQUALS_EQUALS,
	PLUS, MINUS, STAR, SLASH, PLUS_PLUS, MINUS_MINUS, PIPE, AMPERSAND, CARET,
	PLUS_EQUALS, MINUS_EQUALS, STAR_EQUALS, SLASH_EQUALS, PIPE_EQUALS, AMPERSAND_EQUALS, CARET_EQUALS,
	LESS_THAN, GREATER_THAN, LESS_EQUALS, GREATER_EQUALS,
	PIPE_PIPE, AMPERSAND_AMPERSAND,
	QUESTION, QUESTION_QUESTION,

	IF, ELSE, FOR, WHILE, FUNCTION, RETURN,
	PUBLIC, PRIVATE, PROTECTED,
	ABSTRACT, FINAL,
	CLASS, INTERFACE, STRUCT, EXTENDS, IMPLEMENTS,
	VAR, CONST,
	PRINT,
	EOF,
}

class Token{
	public readonly TokenType type;
	public readonly string lexeme;
	public readonly int line;
	public readonly object? val;

	public Token(TokenType _type, string _lexeme, int _line, object? _val){
		type = _type;
		lexeme = _lexeme;
		line = _line;
		val = _val;
	}

	public void PrintToConsole(){
		string msg = $"Line: {line}. Type: {type}. Lexeme: {lexeme}. ";
		if(type == TokenType.STRING_LITERAL || type == TokenType.NUMBER_LITERAL) msg += $"Literal value: {val}";
		Console.WriteLine(msg);
	}
}
