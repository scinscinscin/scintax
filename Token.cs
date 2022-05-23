namespace scintax;

enum TokenType{
	NONE, COMMENT, 
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
	EOF,
}

class Token{
	public readonly TokenType type;
	public readonly string lexeme;
	public readonly int line;
	public readonly object? val;
	public readonly int starting_idx;
	public readonly int ending_idx;

	public Token(TokenType type, string lexeme, int line, object? val, int starting_idx, int ending_idx){
		this.type = type;
		this.lexeme = lexeme;
		this.line = line;
		this.val = val;
		this.starting_idx = starting_idx;
		this.ending_idx = ending_idx;
	}

	public void PrintToConsole(){
		string msg = $"Line: {line}. Type: {type}. Lexeme: {lexeme}. ";
		if(type == TokenType.STRING_LITERAL || type == TokenType.NUMBER_LITERAL) msg += $"Literal value: {val}";
		Console.WriteLine(msg);
	}
}
