namespace scintax;

class Parser{
	private readonly bool isREPL;
	public bool hadError { get; private set; } = false;

	public readonly List<Token> Tokens;
	private int CurrentIdx = 0;
	private Token CurrentToken { get => Tokens[CurrentIdx]; }
	private Token PreviousToken { get => Tokens[CurrentIdx - 1]; }
	private Token NextToken { get => Tokens[CurrentIdx + 1]; }
	public Parser(List<Token> tokens, bool isREPL) {
		this.Tokens = tokens.Where((tok) => tok.type != TokenType.COMMENT).ToList();
		this.isREPL = isREPL;
	}
	
	// parsing infrastructure{{{
	private bool match(params TokenType[] types){
		foreach(var type in types){	
			if(CurrentToken.type == type){
				CurrentIdx++;
				return true;
			}
		}

		return false;
	}

	private void CheckAndConsume(TokenType type, string errmsg){
		if(CurrentToken.type == type){
			CurrentIdx++;
			return;
		};

		if(isREPL && type == TokenType.SEMICOLON) return;
		throw new Exception($"Line: {CurrentToken.line}. {errmsg}. Received {CurrentToken.type}.");
	}/*}}}*/

	public Expr expression(){
		return ternary_operator();
	}

	// expression parsers{{{
	public Expr ternary_operator(){
		Expr expr = nullish_coalescence();
		
		if(match(TokenType.QUESTION)){
			Expr truthy_val = expression();
			CheckAndConsume(TokenType.COLON, "Expected COLON after truthy value of ternary expression");
			Expr falsey_val = expression();
			return new TernaryExpr(expr, truthy_val, falsey_val);
		}
		
		return expr;
	}

	public Expr nullish_coalescence(){
		Expr expr = logical();
		while(match(TokenType.QUESTION_QUESTION))
			expr = new ShortCircuitExpr(expr, PreviousToken, logical());
		return expr;
	}

	public Expr logical(){
		Expr expr = equality();
		while(match(TokenType.PIPE_PIPE, TokenType.AMPERSAND_AMPERSAND))
			expr = new ShortCircuitExpr(expr, PreviousToken, equality());
		return expr;
	}
	
	public Expr equality(){
		Expr expr = comparison();
		while(match(TokenType.EQUALS_EQUALS, TokenType.BANG_EQUALS))
			expr = new BinaryExpr(expr, PreviousToken, comparison());
		return expr;
	}
	
	public Expr comparison(){
		Expr expr = bitwise();
		while(match(TokenType.GREATER_THAN, TokenType.GREATER_EQUALS, TokenType.LESS_THAN, TokenType.LESS_EQUALS))
			expr = new BinaryExpr(expr, PreviousToken, bitwise());
		return expr;
	}
	
	private Expr bitwise(){
		Expr expr = addition_subtraction();
		while(match(TokenType.PIPE, TokenType.AMPERSAND, TokenType.CARET))
			expr = new BinaryExpr(expr, PreviousToken, addition_subtraction());
		return expr;
	}

	private Expr addition_subtraction(){
		Expr expr = multiplication_division();
		while(match(TokenType.PLUS, TokenType.MINUS))
			expr = new BinaryExpr(expr, PreviousToken, multiplication_division());
		return expr;
	}

	private Expr multiplication_division(){
		Expr expr = unary();
		while(match(TokenType.STAR, TokenType.SLASH))
			expr = new BinaryExpr(expr, PreviousToken, unary());
		return expr;
	}

	private Expr unary(){
		if(match(TokenType.BANG, TokenType.MINUS)){
			 return new UnaryExpr(PreviousToken, prefix());
		}

		return prefix();
	}

	private Expr prefix(){
		if(match(TokenType.PLUS_PLUS, TokenType.MINUS_MINUS)){
			return new PrefixExpr(PreviousToken, postfix()); 
		}
		
		return postfix();
	}
	
	private Expr postfix(){
		Expr expr = literal();
		
		if(match(TokenType.PLUS_PLUS, TokenType.MINUS_MINUS)){
			return new PostfixExpr(expr, PreviousToken); 
		}
		
		return expr;
	}

	private Expr literal(){
		if(match(TokenType.STRING_LITERAL, TokenType.NUMBER_LITERAL)) return new LiteralExpr(PreviousToken.val);
		else if(match(TokenType.FALSE)) return new LiteralExpr(false);
		else if(match(TokenType.TRUE)) return new LiteralExpr(true);
		else if(match(TokenType.NULL)) return new LiteralExpr(null);
		else if(match(TokenType.L_SQ_BRACE)) return array();
		else if(match(TokenType.IDENTIFIER)) return identifier();
	
		else if(match(TokenType.L_PAREN)) {
			Expr expr = expression(); // it's the beginning of an expression
			// the next term must be a right parenthesis to close of the expression;
			CheckAndConsume(TokenType.R_PAREN, "Expected right parenthesis at the end of an expression.");
			return new GroupingExpr(expr);
		}
		
		throw new Exception($"Was not able to match a token of type {CurrentToken.type}.");
	}

	public Expr identifier(){
		Expr expr = new VariableExpr(PreviousToken);

		// handle the start of an identifier/calling/variable access chain
		while(true){
			if(match(TokenType.EQUAL)){
				// assignment operation
				Expr val = expression();
				return new AssignmentExpr(expr, val);
			}else if(match(TokenType.DOT)){
				CheckAndConsume(TokenType.IDENTIFIER, "Expected IDENTIFIER token after Dot access.");
				Token nextIdent = PreviousToken;
				expr = new DotAccessExpr(expr, nextIdent);		
			}else if(match(TokenType.L_SQ_BRACE)){
				Expr index = expression();
				CheckAndConsume(TokenType.R_SQ_BRACE, "Expected R_SQ_BRACE token after index access expression");	
				expr = new IndexAccessExpr(expr, index);
			}else if(match(TokenType.L_PAREN)){
				// get all parameters
				List<Expr> parameters = new List<Expr>();
				while(CurrentToken.type != TokenType.R_PAREN){
					Expr parameter_expression = expression();
					parameters.Add(parameter_expression);
					match(TokenType.COMMA);
				}
				CheckAndConsume(TokenType.R_PAREN, "Expected R_PAREN token after parameter list");
				expr = new FunctionCallExpr(expr, parameters);
			}else{
				break;
			}			
		}
			
		return expr;
	}

	public Expr array(){
		// handle the start of an array
		List<Expr> expressions = new List<Expr>();
		while(!match(TokenType.R_SQ_BRACE)){
			expressions.Add(expression());
			match(TokenType.COMMA);
		}
		
		return new ArrayCreationExpr(expressions);
	}

	/*}}}*/
	
	// statement parsers{{{
	public Stmt? declaration(){
		try{
			if(match(TokenType.VAR)) return variable_declaration();
			else if(match(TokenType.FUNCTION)) return function_declaration();
			else if(match(TokenType.CLASS)) return class_declaration();
			return statement();
		}catch{
			sync();
			return null;
		}
	}

	public Stmt variable_declaration(){
		CheckAndConsume(TokenType.IDENTIFIER, "Expected variable name");
		Token tok = PreviousToken;

		Expr? initializer = match(TokenType.EQUAL) ? expression() : null;
		CheckAndConsume(TokenType.SEMICOLON, "Expected SEMICOLON after variable declaration");
		
		return new VariableStmt(tok, initializer);	
	}

	public Stmt function_declaration(bool isMethod = false){
		CheckAndConsume(TokenType.IDENTIFIER, "Expected function name");
		Token functionName = PreviousToken;
		
		CheckAndConsume(TokenType.L_PAREN, "Expected L_PAREN before function argument list");
		
		// Get all the argument names of the function
		List<Token> arg_names = new List<Token>();
		while(CurrentToken.type != TokenType.R_PAREN){
			CheckAndConsume(TokenType.IDENTIFIER, "Expected argument name.");
			arg_names.Add(PreviousToken);
			match(TokenType.COMMA);
		}	

		CheckAndConsume(TokenType.R_PAREN, "Expected R_PAREN after function argument list");
		CheckAndConsume(TokenType.L_BRACE, "Expected L_BRACE in the beginning of function body");
		Stmt block = block_statement();

		return new FunctionStmt(functionName, arg_names, block, isMethod);
	}
	
	public Stmt class_declaration(){
		CheckAndConsume(TokenType.IDENTIFIER, "Expected class name");
		Token class_name = PreviousToken;
		Token? inherited_name = null;
		Dictionary<Token, Expr> default_values = new Dictionary<Token, Expr>();
		Dictionary<Token, FunctionStmt> methods = new Dictionary<Token, FunctionStmt>();
		FunctionStmt? ctor_method = null;

		// handle inheritance here
		if(match(TokenType.COLON)){
			CheckAndConsume(TokenType.IDENTIFIER, "Expected class name");
			inherited_name = PreviousToken;
		}

		CheckAndConsume(TokenType.L_BRACE, "Expected L_BRACE in the beginning of class body"); 
		while(!match(TokenType.R_BRACE)){
			if(NextToken.type == TokenType.EQUAL){
				VariableStmt variable_stmt = (VariableStmt) variable_declaration();
				if(variable_stmt.val == null)
					throw new Exception("Expected default value for class member declaration");
				default_values[variable_stmt.identifier] = variable_stmt.val;
			}else if(NextToken.type == TokenType.L_PAREN){
				FunctionStmt new_function = (FunctionStmt) function_declaration(isMethod: true);
				if(new_function.identifier.lexeme == class_name.lexeme)
					ctor_method = new_function;
				else
					methods[new_function.identifier] = new_function;
			}

			else
				throw new Exception($"Expected either variable or method declaration inside class. Received {CurrentToken.type}.");
		}

		return new ClassStmt(class_name, default_values, methods, ctor_method, inherited_name);
	}

	public Stmt statement(){
		if(match(TokenType.L_BRACE)) return block_statement(); 
		else if(match(TokenType.IF)) return if_statement();
		else if(match(TokenType.WHILE)) return while_statement();
		else if(match(TokenType.RETURN)) return return_statement();
		
		return expression_statement();
	}

	public Stmt block_statement(){
		List<Stmt> statements = new List<Stmt>();
		
		while(CurrentToken.type != TokenType.R_BRACE && CurrentToken.type != TokenType.EOF){
			Stmt? stmt = declaration();
			if(stmt != null) statements.Add(stmt);
		}
		
		CheckAndConsume(TokenType.R_BRACE, "Expected R_BRACE after block statement");
		return new BlockStmt(statements);
	}

	public Stmt if_statement(){
		CheckAndConsume(TokenType.L_PAREN, "Expected L_PAREN after IF token");
		Expr condition = expression();
		CheckAndConsume(TokenType.R_PAREN, "Expected R_PAREN after IF statement condition");
		Stmt true_stmt = statement();
		Stmt? false_stmt = null;

		if(match(TokenType.ELSE))
			false_stmt = statement();
		
		return new IfStmt(condition, true_stmt, false_stmt);
	}

	public Stmt while_statement(){
		CheckAndConsume(TokenType.L_PAREN, "Expected L_PAREN after WHILE token");
		Expr condition = expression();
		CheckAndConsume(TokenType.R_PAREN, "Expected R_PAREN after WHILE statement condition");
		Stmt stmt = statement();

		return new WhileStmt(condition, stmt);
	}

	public Stmt expression_statement(){
		Expr expr = expression();	
		CheckAndConsume(TokenType.SEMICOLON, "Expected ; after expression");
		return new ExpressionStmt(expr);
	}

	public Stmt return_statement(){
		if(match(TokenType.SEMICOLON)) return new ReturnStmt(null);
		Expr return_expr = expression();
		CheckAndConsume(TokenType.SEMICOLON, "Expected ; after return expression");
		return new ReturnStmt(return_expr);
	}
	/*}}}*/

	public void sync(){
		hadError = true;
		if(CurrentToken.type == TokenType.EOF) return;
		
		// Moves until the current character is the start of a sequence
		while(true){
			CurrentIdx++;
			
			switch(CurrentToken.type){
				case TokenType.EOF:
				case TokenType.IF:
				case TokenType.FOR:
				case TokenType.WHILE:
				case TokenType.FUNCTION:
				case TokenType.CLASS:
				case TokenType.VAR:
					return;
			}
		}
	}
	
	/* Parses a scintax program and returns a list of its statements */ 
	public List<Stmt> parse(){
		List<Stmt> statements = new List<Stmt>();
		while(CurrentIdx >= Tokens.Count || CurrentToken.type != TokenType.EOF){
			Stmt? stmt = declaration();
			if(stmt != null) statements.Add(stmt);
		};
		return statements;
	} 
}

