namespace scintax;

class Lexer{
	// initialize token dictionary
	private static Dictionary<string, TokenType> dict = new Dictionary<string, TokenType>();/*{{{*/
	static Lexer(){
		dict.Add("if", TokenType.IF);	
		dict.Add("else", TokenType.ELSE);	
		dict.Add("for", TokenType.FOR);	
		dict.Add("while", TokenType.WHILE);	
		dict.Add("function", TokenType.FUNCTION);	
		dict.Add("public", TokenType.PUBLIC);	
		dict.Add("private", TokenType.PRIVATE);	
		dict.Add("protected", TokenType.PROTECTED);	
		dict.Add("abstract", TokenType.ABSTRACT);	
		dict.Add("final", TokenType.FINAL);	
		dict.Add("class", TokenType.CLASS);	
		dict.Add("interface", TokenType.INTERFACE);	
		dict.Add("extends", TokenType.EXTENDS);	
		dict.Add("implements", TokenType.IMPLEMENTS);
		dict.Add("var", TokenType.VAR);
		dict.Add("const", TokenType.CONST);
		dict.Add("true", TokenType.TRUE);
		dict.Add("false", TokenType.FALSE);
		dict.Add("null", TokenType.NULL);
		dict.Add("return", TokenType.RETURN);
	}/*}}}*/
	
	public readonly string FileContents;	// input string
	public readonly List<Token> tokens = new List<Token>();	// output tokens
	public bool hadError { get; private set; } = false;

	private int CurrentLine = 1;
	private int CurrentIdx = 0;
	private int StartingIdxOfCurrentLexeme = 0;
	private string CurrentLexeme { get => FileContents.Substring(StartingIdxOfCurrentLexeme, CurrentIdx - StartingIdxOfCurrentLexeme + 1); }
	private char CurrentChar { get => CurrentIdx >= FileContents.Length ? '\0' : FileContents[CurrentIdx]; }
	private char PreviousChar { get => FileContents[CurrentIdx - 1]; }
	private char NextChar { get => FileContents[CurrentIdx + 1]; }
	public bool Finished { get => FileContents.Length <= CurrentIdx; }
	
	private Lexer(string FileContents){ this.FileContents = FileContents; }

	public Token CreateNewToken(TokenType type, object? val, bool isEOF = false){
		string lexeme = isEOF ? "\0" : CurrentLexeme;
		return new Token(type, lexeme, CurrentLine, val, StartingIdxOfCurrentLexeme, CurrentIdx);
	}

	// returns true if next char matches some char, before consuming it 
	private bool match(char c){
		if(NextChar == c){
			CurrentIdx++;
			return true;
		}
		return false;
	}

	private void comment(){
		while(CurrentChar != '\n') CurrentIdx++;
		Token token = CreateNewToken(TokenType.COMMENT, null);
		tokens.Add(token);
		CurrentLine++;
	}

	private void stringLiteral(){
		string val = "";
		CurrentIdx++;

		while(CurrentChar != '"'){
			val += CurrentChar;
			CurrentIdx++;
			
			if(CurrentChar == '\0'){
				CurrentIdx--;
				error("Expected \", received EOF");
				break;
			};
		}
		
		val = val.Replace("\\n", "\n"); // handle new lines
		Token token = CreateNewToken(TokenType.STRING_LITERAL, val);
		tokens.Add(token);
	}

	private void identifier(){
		string iden = "";

		while(CurrentChar.IsAlphanumeric()){
			iden += CurrentChar;
			CurrentIdx++;
		}

		CurrentIdx--;

		TokenType type = dict.ContainsKey(iden) ? dict[iden] : TokenType.IDENTIFIER;
		Token token = CreateNewToken(type, null);
		tokens.Add(token);
	}

	private void numberLiteral(){
		bool hasHitDecimal = false;
		string numstr = "";

		while(CurrentChar.IsNumeric()|| CurrentChar == '.'){
			if(CurrentChar == '.'){
				if(hasHitDecimal == false){
					hasHitDecimal = true;
					numstr += '.';
				}else{
					// a decimal point has already been found
					error("Multiple decimal points found on a numerical value");
				}
			}else{
				numstr += CurrentChar;
			}

			CurrentIdx++;
		}

		CurrentIdx--;
		double number;	

		if(Double.TryParse(numstr, out number)){
			Token token = CreateNewToken(TokenType.NUMBER_LITERAL, number);
			tokens.Add(token);
		}else{
			error($"Failed to convert a number literal ({numstr}) to double");
		}
	}
	
	public void parse(){
		StartingIdxOfCurrentLexeme = CurrentIdx;
		TokenType tokenType = TokenType.NONE;

		if(CurrentChar == '\n'){ CurrentLine++; }
		else if(CurrentChar == ' ' || CurrentChar == '	' || CurrentChar == '\r') {} // do nothing if space / tab / <CR>

		// basic grammar
		else if(CurrentChar == '(') tokenType = TokenType.L_PAREN;
		else if(CurrentChar == ')') tokenType = TokenType.R_PAREN;
		else if(CurrentChar == '[') tokenType = TokenType.L_SQ_BRACE;
		else if(CurrentChar == ']') tokenType = TokenType.R_SQ_BRACE;
		else if(CurrentChar == '{') tokenType = TokenType.L_BRACE;
		else if(CurrentChar == '}') tokenType = TokenType.R_BRACE;
		else if(CurrentChar == ';') tokenType = TokenType.SEMICOLON;
		else if(CurrentChar == ':') tokenType = TokenType.COLON;
		else if(CurrentChar == ',') tokenType = TokenType.COMMA;
		else if(CurrentChar == '.') tokenType = TokenType.DOT;
		else if(CurrentChar == '!') tokenType = match('=') ? TokenType.BANG_EQUALS : TokenType.BANG;
		else if(CurrentChar == '=') tokenType = match('=') ? TokenType.EQUALS_EQUALS : TokenType.EQUAL;

		// basic operators
		else if(CurrentChar == '>') tokenType = match('=') ? TokenType.GREATER_EQUALS : TokenType.GREATER_THAN;
		else if(CurrentChar == '<') tokenType = match('=') ? TokenType.LESS_EQUALS : TokenType.LESS_THAN;
		else if(CurrentChar == '+') tokenType = match('+') ? TokenType.PLUS_PLUS : match('=') ? TokenType.PLUS_EQUALS : TokenType.PLUS;
		else if(CurrentChar == '-') tokenType = match('-') ? TokenType.MINUS_MINUS : match('=') ? TokenType.MINUS_EQUALS : TokenType.MINUS;
		else if(CurrentChar == '|') tokenType = match('|') ? TokenType.PIPE_PIPE : match('=') ? TokenType.PIPE_EQUALS : TokenType.PIPE;
		else if(CurrentChar == '&') tokenType = match('&') ? TokenType.AMPERSAND_AMPERSAND : match('=') ? TokenType.AMPERSAND_EQUALS : TokenType.AMPERSAND;
		else if(CurrentChar == '^') tokenType = match('=') ? TokenType.CARET_EQUALS : TokenType.CARET;
		else if(CurrentChar == '*') tokenType = match('=') ? TokenType.STAR_EQUALS : TokenType.STAR;
		else if(CurrentChar == '/'){
			if(NextChar == '/') comment();
			else tokenType = match('=') ? TokenType.SLASH_EQUALS : TokenType.SLASH;
		}
		else if(CurrentChar == '?') tokenType = match('?') ? TokenType.QUESTION_QUESTION : TokenType.QUESTION;

		else if(CurrentChar == '"') stringLiteral();
		else if(CurrentChar.IsNumeric()) numberLiteral();
		else if(CurrentChar.IsAlphanumeric()) identifier();
		else error($"Unexpected character '{CurrentChar}'");

		if(tokenType != TokenType.NONE){
			// create new token and append it the list;
			Token token = CreateNewToken(tokenType, null);
			tokens.Add(token);
		}
		
		CurrentIdx++; 
		
		if(CurrentIdx == FileContents.Length) {
			// reached the end of the file
			Token EOFToken = CreateNewToken(TokenType.EOF, null, isEOF: true);
			tokens.Add(EOFToken);
		}
	}

	private void error(string msg){
		hadError = true;
		string err = $"Line {CurrentLine}. {msg}";
	}

	public static List<Token> GenerateTokens(string? file = null, string? path = null){
		if(file == null && path == null)
			throw new Exception("Cannot generate tokens because there is no input");
		if(file == null) file = File.ReadAllText(path!);
		
		Lexer lexer = new Lexer(file);
		while(!lexer.Finished) lexer.parse();
		return lexer.tokens;
	}
}

