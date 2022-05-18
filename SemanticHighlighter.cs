enum DefType{
	DEFAULT,
	VARIABLE,
	FUNCTION,
	CLASS,
	ARGUMENT,
	THIS, BASE, SUPER,
}

class SemanticHighlighter : SyntaxHighlighter, ExprVisitor<object?>, StmtVisitor {	
	private static Dictionary<DefType, Kind> defMap = new Dictionary<DefType, Kind>();
	static SemanticHighlighter(){
		defMap.Add(DefType.VARIABLE, Kind.VARIABLE_NAME);
		defMap.Add(DefType.FUNCTION, Kind.FUNCTION_NAME);
		defMap.Add(DefType.CLASS, Kind.CLASS_NAME);
		defMap.Add(DefType.ARGUMENT, Kind.ARGUMENT);
		defMap.Add(DefType.THIS, Kind.THIS);
		defMap.Add(DefType.BASE, Kind.BASE);
		defMap.Add(DefType.SUPER, Kind.SUPER);
	}
	
	private Env<DefType> defs = new Env<DefType>();
	
	public object? visitLiteralExpr(LiteralExpr expr){ return null; }
	public object? visitVariableExpr(VariableExpr expr){
		DefType deftype = defs.get_no_fail(expr.identifier.lexeme);
		if(deftype != DefType.DEFAULT) SetTokenKind(defMap[deftype], expr.identifier);
		return null;
	}
	
	public object? visitBinaryExpr(BinaryExpr expr){ eval(expr.lefthand, expr.righthand); return null; }
	public object? visitShortCircuitExpr(ShortCircuitExpr expr){ eval(expr.lefthand, expr.righthand); return null; }
	public object? visitTernaryExpr(TernaryExpr expr){ eval(expr.condition, expr.truthy_val, expr.falsey_val); return null; }
	public object? visitUnaryExpr(UnaryExpr expr){ eval(expr.expr); return null; }
	public object? visitPrefixExpr(PrefixExpr expr){ eval(expr.expr); return null; }
	public object? visitPostfixExpr(PostfixExpr expr){ eval(expr.expr); return null; }
	public object? visitGroupingExpr(GroupingExpr expr){ eval(expr.contents); return null; }
	public object? visitAssignmentExpr(AssignmentExpr expr){ eval(expr.writelocation, expr.val); return null; }
	public object? visitIndexAccessExpr(IndexAccessExpr expr){ eval(expr.contents, expr.index); return null; }
	
	public object? visitDotAccessExpr(DotAccessExpr expr){
		SetTokenKind(Kind.ACCESSOR_TOKEN, expr.ident);
		eval(expr.contents);
		return null;
	}
	
	public object? visitFunctionCallExpr(FunctionCallExpr expr){
		eval(expr.parameters.ToArray());
		if(expr.function is VariableExpr variableExpr){
			SetTokenKind(Kind.CALLED, variableExpr.identifier);
		}

		eval(expr.function);
		
		if(expr.function is DotAccessExpr dotAccessExpr){
			SetTokenKind(Kind.CALLED, dotAccessExpr.ident);
		}
		
		return null;
	}
	public object? visitArrayCreationExpr(ArrayCreationExpr expr){ eval(expr.expressions.ToArray()); return null; }

	public void visitVariableStmt(VariableStmt stmt){
		SetTokenKind(Kind.VARIABLE_NAME, stmt.identifier);
		defs.define(stmt.identifier.lexeme, DefType.VARIABLE);
		if(stmt.val != null) eval(stmt.val);
	}

	public void visitFunctionStmt(FunctionStmt stmt){
		SetTokenKind(Kind.FUNCTION_NAME, stmt.identifier);
		if(!stmt.isMethod)
			defs.define(stmt.identifier.lexeme, DefType.FUNCTION);

		Env<DefType> currentEnv = defs;
		defs = new Env<DefType>(currentEnv);
		
		foreach(var arg_name in stmt.arg_names){
			SetTokenKind(Kind.ARGUMENT, arg_name);
			defs.define(arg_name.lexeme, DefType.ARGUMENT);
		}

		if(stmt.isMethod){
			defs.define("this", DefType.THIS);
			defs.define("base", DefType.BASE);
			defs.define("super", DefType.SUPER);
		}

		exec(stmt.body);
		defs = currentEnv;
	}

	public void visitClassStmt(ClassStmt stmt){
		SetTokenKind(Kind.CLASS_NAME, stmt.identifier);
		defs.define(stmt.identifier.lexeme, DefType.CLASS);

		foreach(var default_val in stmt.default_values){
			SetTokenKind(Kind.VARIABLE_NAME, default_val.Key);
			eval(default_val.Value); 
		};
		
		foreach(var method in stmt.methods){
			SetTokenKind(Kind.FUNCTION_NAME, method.Key);
			exec(method.Value);
		}

		if(stmt.ctor != null){
			exec(stmt.ctor); SetTokenKind(Kind.CTOR_DECLARATION, stmt.ctor.identifier);
		}

		if(stmt.inherited_name != null){
			SetTokenKind(Kind.CLASS_NAME, stmt.inherited_name);
		}
	}

	public void visitReturnStmt(ReturnStmt stmt){ if(stmt.return_val != null) eval(stmt.return_val); }
	public void visitExpressionStmt(ExpressionStmt stmt){ eval(stmt.expr); }
	
	public void visitBlockStmt(BlockStmt stmt){ 
		Env<DefType> currentEnv = defs;
		defs = new Env<DefType>(currentEnv);
		exec(stmt.statements.ToArray());
		defs = currentEnv;
	}
	
	public void visitIfStmt(IfStmt stmt){
		eval(stmt.condition);
		exec(stmt.true_stmt);
		if(stmt.false_stmt != null) exec(stmt.false_stmt);
	}
	public void visitWhileStmt(WhileStmt stmt){ eval(stmt.condition); exec(stmt.stmt); }

	public void eval(params Expr[] exprs){ foreach(var expr in exprs) expr.accept(this); }
	public void exec(params Stmt[] stmts){ foreach(var stmt in stmts) stmt.accept(this); }
	
	public SemanticHighlighter(List<Token> tokens) : base(tokens){}
	public override void highlight(){
		base.highlight(); // do syntax highlighting
		Parser parser = new Parser(tokens, isREPL: false);
		List<Stmt> statements = parser.parse();
		foreach(var stmt in statements) exec(stmt);
	}
}
