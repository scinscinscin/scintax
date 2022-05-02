#define PRINT_NUMBERS_AS_ASCII
class Interpreter : ExprVisitor<SIMPValue>, StmtVisitor {
	private Env env = new Env();
	public static bool IsEqual(object? a, object? b){
		if(a == null && b == null) return true;
		else if (a == null || b == null) return false; // one but not both of them is null, so they are inequal
		else return a.Equals(b);
	}
	
	public SIMPValue expression(Expr expr){ return expr.accept(this); }

	public SIMPValue visitLiteralExpr(LiteralExpr expr){
		if (expr.val == null) return new SIMPNull();
		else if(expr.val.GetType() == typeof(string)) return new SIMPString((string) expr.val);
		else if(expr.val.GetType() == typeof(double)) return new SIMPNumber((double) expr.val);
		else if(expr.val.GetType() == typeof(bool)) return new SIMPBool((bool) expr.val);

		throw new Exception($"Failed to convert C# literal value to SIMP value. Attempted to convert '{expr.val}'");
	}

	public SIMPValue visitGroupingExpr(GroupingExpr expr){
		return expression(expr.contents);
	}

	public SIMPValue visitUnaryExpr(UnaryExpr expr){
		SIMPValue rh = expression(expr.expr);
		
		if(expr.op.type == TokenType.MINUS) return new SIMPNumber(-1 * rh.GetDouble());
		else if(expr.op.type == TokenType.BANG) return new SIMPBool(!rh.GetBoolean());

		throw new Exception($"Invalid Unary expression operator of {expr.op.type}");
	}

	public SIMPValue visitShortCircuitExpr(ShortCircuitExpr expr){
		bool lhside = expression(expr.lefthand).GetBoolean();
		
		if(expr.op.type == TokenType.PIPE_PIPE){
			if(lhside) return new SIMPBool(true);
			// we only want to evaluate the right hand side if the left is false in the case of logical OR
			return new SIMPBool(expression(expr.righthand).GetBoolean());
		}else if(expr.op.type == TokenType.AMPERSAND_AMPERSAND){
			if(!lhside) return new SIMPBool(false);
			// only evaluate the right hand side if the left is true
			return new SIMPBool(expression(expr.righthand).GetBoolean());
		}

		else throw new Exception($"Invalid Short Circuit expression operator of {expr.op.type}");
	}

	public SIMPValue visitBinaryExpr(BinaryExpr expr){
		SIMPValue lh = expr.lefthand.accept(this);
		SIMPValue rh = expr.righthand.accept(this);

		// Plus operator is a special case because we can concatenate strings
		if(expr.op.type == TokenType.PLUS) {
			if(lh is SIMPString && lh is SIMPString) return new SIMPString(lh.GetString() + rh.GetString());
			else if(lh is SIMPNumber && lh is SIMPNumber) return new SIMPNumber(lh.GetDouble() + rh.GetDouble());
			throw new Exception("+ operator only applicable to strings or numbers");
		}
		
		// arithmetic and bitwise operators
		else if(expr.op.type == TokenType.MINUS) return new SIMPNumber(lh.GetDouble() - rh.GetDouble());
		else if(expr.op.type == TokenType.STAR) return new SIMPNumber(lh.GetDouble() * rh.GetDouble());
		else if(expr.op.type == TokenType.SLASH) return new SIMPNumber(lh.GetDouble() / rh.GetDouble());
		else if(expr.op.type == TokenType.PIPE) return new SIMPNumber((long) lh.GetDouble() | (long) rh.GetDouble());		
		else if(expr.op.type == TokenType.AMPERSAND) return new SIMPNumber((long) lh.GetDouble() & (long) rh.GetDouble());		
		else if(expr.op.type == TokenType.CARET) return new SIMPNumber((long) lh.GetDouble() ^ (long) rh.GetDouble());		
		
		// equality and comparison operators
		else if(expr.op.type == TokenType.EQUALS_EQUALS) return new SIMPBool(IsEqual(lh.GetRaw(), rh.GetRaw()));
		else if(expr.op.type == TokenType.BANG_EQUALS) return new SIMPBool(!IsEqual(lh.GetRaw(), rh.GetRaw()));
		else if(expr.op.type == TokenType.GREATER_THAN) return new SIMPBool(lh.GetDouble() > rh.GetDouble());
		else if(expr.op.type == TokenType.GREATER_EQUALS) return new SIMPBool(lh.GetDouble() >= rh.GetDouble());
		else if(expr.op.type == TokenType.LESS_THAN) return new SIMPBool(lh.GetDouble() < rh.GetDouble());
		else if(expr.op.type == TokenType.LESS_EQUALS) return new SIMPBool(lh.GetDouble() <= rh.GetDouble());
		
		throw new Exception($"Invalid Binary expression operator of {expr.op.type}");
	}

	public SIMPValue visitVariableExpr(VariableExpr expr){
		return env.get(expr.identifier);
	}
	
	public SIMPValue visitAssignmentExpr(AssignmentExpr expr){
		// attempt to assign to the variable
		SIMPValue a = evaluate(expr.val);

		if(expr.writelocation is VariableExpr){
			// If the write location is immediately a variable, that means we can assign to it
			env.assign(((VariableExpr) expr.writelocation).identifier, a);
		}else if(expr.writelocation is DotAccessExpr writelocation){
			// A DotAccessExpr has two parts, the identifier of what to access, and the thing to look inside of
			// The thing to look inside of has a couple of methods to write or read data off of it
			SIMPValue obj = expression(writelocation.contents);
			
			if(obj is DotAccessible dotAccessibleObject) dotAccessibleObject.DotWrite(writelocation.ident.lexeme, a);
			else throw new Exception("Attempted to member-write to a value that is not DotAccessible");
		}else if(expr.writelocation is IndexAccessExpr arrayWriteLocation){
			SIMPValue arr = expression(arrayWriteLocation.contents);
			SIMPValue index = expression(arrayWriteLocation.index);
			if(arr is IndexAccessible indexAccessibleArray) indexAccessibleArray.IndexWrite(index, a);
			else throw new Exception("Attempted to index-write to a value that is not IndexAccessible");
		}

		return a;
	}

	public SIMPValue visitIndexAccessExpr(IndexAccessExpr expr){
		SIMPValue a = evaluate(expr.contents);
		SIMPValue idx = evaluate(expr.index);

		if(a is IndexAccessible) return ((IndexAccessible) a).IndexAccess(idx);
		throw new Exception("Tried to access index of non-index accessible expression");
	}

	public SIMPValue visitDotAccessExpr(DotAccessExpr expr){
		// Evaluate the contents of expr.contents and return the value of the identifier
		SIMPValue contents = expression(expr.contents); // this should implement DotAccessible
		if(contents is DotAccessible) return ((DotAccessible) contents).DotAccess(expr.ident.lexeme);
		else {
			throw new Exception("Attempted to access identifier on a non DotAccessible value");
		}
	}
	
	public SIMPValue visitFunctionCallExpr(FunctionCallExpr expr){
		Console.WriteLine("Attempted to use unfinished logic == Function call expression");
		return new SIMPNull();
	}

	public SIMPValue visitArrayCreationExpr(ArrayCreationExpr expr){
		List<SIMPValue?> values = new List<SIMPValue?>();
		foreach(var bruh in expr.expressions) values.Add(expression(bruh));
		return new SIMPArray(values);
	}

	// statement interpreters below
	public void visitVariableStmt(VariableStmt stmt){
		SIMPValue val = stmt.val != null ? expression(stmt.val) : new SIMPNull();
		env.define(stmt.identifier.lexeme, val);
	}

	public void visitPrintStmt(PrintStmt stmt){
		SIMPValue val = expression(stmt.expr);

#if PRINT_NUMBERS_AS_ASCII
		if(val is SIMPNumber numval) Console.Write((char) numval.GetDouble());
		else Console.Write(val.GetString());
#else
		Console.Write(val.GetString());
#endif
	}

	public void visitExpressionStmt(ExpressionStmt stmt){
		evaluate(stmt.expr);
	}

	public void visitBlockStmt(BlockStmt stmt){
		Env currentEnv = env;
		this.env = new Env(env);
		foreach(var statement in stmt.statements) execute(statement);
		this.env = currentEnv;
	}

	public void visitIfStmt(IfStmt stmt){
		bool condition = evaluate(stmt.condition).GetBoolean();
		if(condition == true) execute(stmt.true_stmt);
		else if(stmt.false_stmt != null) execute(stmt.false_stmt);
	}

	public void visitWhileStmt(WhileStmt stmt){
		while(evaluate(stmt.condition).GetBoolean() == true) execute(stmt.stmt);	
	}

	public SIMPValue evaluate(Expr expr){ return expr.accept(this); }
	public void execute(Stmt stmt){ stmt.accept(this); }
	public void interpret(List<Stmt> statements){
		foreach(var stmt in statements) execute(stmt);
	}
}

