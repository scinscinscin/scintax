class Interpreter : ExprVisitor<SIMPValue>, StmtVisitor {
	private readonly bool isREPL;
	public Interpreter(bool isREPL){ 
		this.isREPL = isREPL;
		global_env = new Env<SIMPValue>();
		env = global_env;
	
		// define native functions{{{
		global_env.define("epoch", new SIMPFunction(
			defined_env: global_env,
			native_fn: (List<SIMPValue> parameters) => {
				long unix_seconds = DateTimeOffset.Now.ToUnixTimeSeconds();
				return new SIMPNumber(unix_seconds);
			}
		));

		global_env.define("int_to_char", new SIMPFunction(
			defined_env: global_env,
			arity: 1,
			native_fn: (List<SIMPValue> parameters) => {
				int to_be_converted = (int) parameters[0].GetDouble();
				char char_convert = (char) to_be_converted;
				return new SIMPString(char_convert.ToString());
			}
		));
		
		global_env.define("readline", new SIMPFunction(
			defined_env: global_env,
			arity: 1,
			native_fn: (List<SIMPValue> parameters) => {
				string to_be_printed = parameters[0].GetString();
				Console.Write(to_be_printed);
				string val = Console.ReadLine() ?? "";
				return new SIMPString(val);
			}
		));

		global_env.define("print", new SIMPFunction(
			defined_env: global_env,
			arity: 1,
			native_fn: (List<SIMPValue> parameters) => {
				string to_be_printed = parameters[0].GetString();
				Console.Write(to_be_printed);
				return new SIMPNull();
			}
		));
	}/*}}}*/

	private readonly Env<SIMPValue> global_env;
	public Env<SIMPValue> env;

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
		SIMPValue lhside = expression(expr.lefthand);
		bool lhbool= lhside.GetBoolean();
		
		if(expr.op.type == TokenType.PIPE_PIPE){
			if(lhbool) return new SIMPBool(true);
			// we only want to evaluate the right hand side if the left is false in the case of logical OR
			return new SIMPBool(expression(expr.righthand).GetBoolean());
		}else if(expr.op.type == TokenType.AMPERSAND_AMPERSAND){
			if(!lhbool) return new SIMPBool(false);
			// only evaluate the right hand side if the left is true
			return new SIMPBool(expression(expr.righthand).GetBoolean());
		}else if(expr.op.type == TokenType.QUESTION_QUESTION){
			if(lhside is SIMPNull) return expression(expr.righthand);
			else return lhside;
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
		// get the SIMPObject to be called then evaluate all the parameters
		SIMPValue function = expression(expr.function);
		List<SIMPValue> parameters = new List<SIMPValue>();
		foreach(var param in expr.parameters) parameters.Add(evaluate(param));

		if(function is Callable CallableFunction)
			return CallableFunction.Call(this, parameters);
		
		throw new Exception("Attempted to call an uncallable value");
	}

	public SIMPValue visitArrayCreationExpr(ArrayCreationExpr expr){
		List<SIMPValue?> values = new List<SIMPValue?>();
		foreach(var bruh in expr.expressions) values.Add(expression(bruh));
		return new SIMPArray(values);
	}

	public SIMPValue visitTernaryExpr(TernaryExpr expr){
		bool cond = expression(expr.condition).GetBoolean();
		return expression(cond ? expr.truthy_val : expr.falsey_val);
	}

	public SIMPValue visitPostfixExpr(PostfixExpr expr){
		// calculate the current value of the expression
		SIMPValue current_value = expression(expr.expr);
		SIMPValue new_value;
		if(expr.op.type == TokenType.PLUS_PLUS) new_value = new SIMPNumber(current_value.GetDouble() + 1);
		else if(expr.op.type == TokenType.MINUS_MINUS) new_value = new SIMPNumber(current_value.GetDouble() - 1);
		else throw new Exception($"Invalid Postfix expression operator of {expr.op.type}");

		expression(new AssignmentExpr(expr.expr, new LiteralExpr(new_value.GetRaw())));
		return current_value;
	}

	public SIMPValue visitPrefixExpr(PrefixExpr expr){
		// calculate the current value of the expression
		SIMPValue current_value = expression(expr.expr);
		SIMPValue new_value;

		if(expr.op.type == TokenType.PLUS_PLUS) new_value = new SIMPNumber(current_value.GetDouble() + 1);
		else if(expr.op.type == TokenType.MINUS_MINUS) new_value = new SIMPNumber(current_value.GetDouble() - 1);
		else throw new Exception($"Invalid Prefix expression operator of {expr.op.type}");

		expression(new AssignmentExpr(expr.expr, new LiteralExpr(new_value.GetRaw())));
		return new_value;
	}

	// statement interpreters below
	public void visitVariableStmt(VariableStmt stmt){
		SIMPValue val = stmt.val != null ? expression(stmt.val) : new SIMPNull();
		env.define(stmt.identifier.lexeme, val);
	}

	public void visitExpressionStmt(ExpressionStmt stmt){
		SIMPValue val = evaluate(stmt.expr);
		if(isREPL) Console.Write(val.GetPrettyString());
	}

	public void visitBlockStmt(BlockStmt stmt){
		Env<SIMPValue> currentEnv = env;
		this.env = new Env<SIMPValue>(env);
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

	public void visitFunctionStmt(FunctionStmt stmt){
		// create a new SIMPFunction then bind it to the current environment
		SIMPFunction new_function = new SIMPFunction(env, stmt.arg_names, body: stmt.body);
		env.define(stmt.identifier.lexeme, new_function);	
	}

	public void visitClassStmt(ClassStmt stmt){
		env.define(stmt.identifier.lexeme, new SIMPClassConstructor(stmt));
	}

	public void visitReturnStmt(ReturnStmt stmt){
		SIMPValue val = stmt.return_val == null ? new SIMPNull() : evaluate(stmt.return_val);
		throw new SIMPValueException(val);
	}

	public SIMPValue evaluate(Expr expr){ return expr.accept(this); }
	public void execute(Stmt stmt){ stmt.accept(this); }
	public void interpret(List<Stmt> statements){
		foreach(var stmt in statements) execute(stmt);
	}
}

