enum Kind{
	NONE, COMMENT,
	VARIABLE_NAME,
	ACCESSOR_TOKEN,
	FUNCTION_NAME,
	CTOR_DECLARATION,
	CLASS_NAME,
	ARGUMENT,
	THIS, BASE, SUPER,
	CALLED,

	VAR_KEYWORD,
	FUNCTION_KEYWORD,
	CLASS_KEYWORD,
	IF_KEYWORD,
	ELSE_KEYWORD,
	WHILE_KEYWORD,
	RETURN_KEYWORD,
	STRING_LITERAL,
	NUMBER_LITERAL,
	TRUE, FALSE, NULL,

	B0, B1, B2, // Bracket Pair Colorizer
}

class SyntaxHighlighter{
	private static Kind[] bracketMap = new Kind[3]{ Kind.B0, Kind.B1, Kind.B2 };
	private static Dictionary<TokenType, Kind> keywordMap = new Dictionary<TokenType, Kind>();
	public readonly static Dictionary<Kind, byte[]> colorMap = new Dictionary<Kind, byte[]>();

	static SyntaxHighlighter(){
		// direct syntax highlighting
		keywordMap.Add(TokenType.COMMENT, Kind.COMMENT);/*{{{*/
		keywordMap.Add(TokenType.VAR, Kind.VAR_KEYWORD);
		keywordMap.Add(TokenType.FUNCTION, Kind.FUNCTION_KEYWORD);
		keywordMap.Add(TokenType.CLASS, Kind.CLASS_KEYWORD);
		keywordMap.Add(TokenType.IF, Kind.IF_KEYWORD);
		keywordMap.Add(TokenType.ELSE, Kind.ELSE_KEYWORD);
		keywordMap.Add(TokenType.WHILE, Kind.WHILE_KEYWORD);
		keywordMap.Add(TokenType.RETURN, Kind.RETURN_KEYWORD);
		keywordMap.Add(TokenType.STRING_LITERAL, Kind.STRING_LITERAL);
		keywordMap.Add(TokenType.NUMBER_LITERAL, Kind.NUMBER_LITERAL);
		keywordMap.Add(TokenType.TRUE, Kind.TRUE);
		keywordMap.Add(TokenType.FALSE, Kind.FALSE);
		keywordMap.Add(TokenType.NULL, Kind.NULL);/*}}}*/
		
		// Colors
		colorMap.Add(Kind.NONE,             new byte[3]{ 197, 200, 198 });/*{{{*/
		colorMap.Add(Kind.COMMENT,          new byte[3]{ 106, 153, 85 });
		colorMap.Add(Kind.VARIABLE_NAME,    new byte[3]{ 156, 220, 254 });
		colorMap.Add(Kind.ACCESSOR_TOKEN,   new byte[3]{ 156, 220, 254 });
		colorMap.Add(Kind.FUNCTION_NAME,    new byte[3]{ 220, 220, 170 });
		colorMap.Add(Kind.CTOR_DECLARATION, new byte[3]{ 78, 201, 176 });
		colorMap.Add(Kind.CLASS_NAME,       new byte[3]{ 78, 201, 176 });
		colorMap.Add(Kind.ARGUMENT,         new byte[3]{ 156, 220, 254 });
		colorMap.Add(Kind.THIS,             new byte[3]{ 86, 156, 214 });
		colorMap.Add(Kind.BASE,             new byte[3]{ 86, 156, 214 });
		colorMap.Add(Kind.SUPER,            new byte[3]{ 86, 156, 214 });
		colorMap.Add(Kind.CALLED,           new byte[3]{ 220, 220, 170 });

		colorMap.Add(Kind.VAR_KEYWORD,      new byte[3]{ 86, 156, 214 });
		colorMap.Add(Kind.FUNCTION_KEYWORD, new byte[3]{ 86, 156, 214 });
		colorMap.Add(Kind.CLASS_KEYWORD,    new byte[3]{ 86, 156, 214 });
		colorMap.Add(Kind.IF_KEYWORD,       new byte[3]{ 197, 134, 192 });
		colorMap.Add(Kind.ELSE_KEYWORD,     new byte[3]{ 197, 134, 192 });
		colorMap.Add(Kind.WHILE_KEYWORD,    new byte[3]{ 197, 134, 192 });
		colorMap.Add(Kind.RETURN_KEYWORD,   new byte[3]{ 197, 134, 192 });
		colorMap.Add(Kind.STRING_LITERAL,   new byte[3]{ 206, 145, 120 });
		colorMap.Add(Kind.NUMBER_LITERAL,   new byte[3]{ 181, 206, 128 });
		colorMap.Add(Kind.TRUE,             new byte[3]{ 86, 156, 214 });
		colorMap.Add(Kind.FALSE,            new byte[3]{ 86, 156, 214 });
		colorMap.Add(Kind.NULL,             new byte[3]{ 86, 156, 214 });
		colorMap.Add(Kind.B0,               new byte[3]{ 23, 159, 255 });
		colorMap.Add(Kind.B1,               new byte[3]{ 255, 215, 0 });
		colorMap.Add(Kind.B2,               new byte[3]{ 218, 112, 214 });
		/*}}}*/
	}
	
	public readonly List<Token> tokens;
	public readonly List<Kind> props = new List<Kind>();
	
	public SyntaxHighlighter(List<Token> tokens){
		this.tokens = tokens;
	}

	protected void SetTokenKind(Kind kind, params Token[] tokens){
		foreach(var token in tokens){
			for(int i = token.starting_idx; i <= token.ending_idx; i++){
				while(i >= props.Count) props.Add(Kind.NONE);
				props[i] = kind;
			}
		}
	}

	public virtual void highlight(){
		foreach(var token in tokens){
			if(keywordMap.ContainsKey(token.type)) SetTokenKind(keywordMap[token.type], token);
			BracketPairColorizer(token);
		}
	}

	private int CurrentBracketColor = 0;
	private BracketNode? node = null;
	private void BracketPairColorizer(Token token){
		if(
				token.type == TokenType.L_PAREN ||
				token.type == TokenType.L_BRACE ||
				token.type == TokenType.L_SQ_BRACE
		) node = new BracketNode(token, CurrentBracketColor++, node);
		
		else if(
				(token.type == TokenType.R_PAREN && node?.type == TokenType.L_PAREN) ||
				(token.type == TokenType.R_BRACE && node?.type == TokenType.L_BRACE) ||
				(token.type == TokenType.R_SQ_BRACE && node?.type == TokenType.L_SQ_BRACE)
		) removeNode(token);
	}

	private void removeNode(Token token){
		if(node == null) return;
		
		// Set the color of the brackets
		Kind colorKind = bracketMap[node.colorIdx % 3];
		SetTokenKind(colorKind, node.token, token);

		node = node.previousNode;
	}
}

class BracketNode{
	public readonly Token token;
	public readonly TokenType type;
	public readonly BracketNode? previousNode = null;
	public readonly int colorIdx;

	public BracketNode(Token token, int colorIdx, BracketNode? previousNode){
		this.token = token;
		this.type = token.type;
		this.colorIdx = colorIdx;
		this.previousNode = previousNode;
	}
}
