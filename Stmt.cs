using System;

interface StmtVisitor{
	void visitVariableStmt(VariableStmt stmt);
	void visitFunctionStmt(FunctionStmt stmt);
	void visitClassStmt(ClassStmt stmt);
	void visitReturnStmt(ReturnStmt stmt);
	void visitExpressionStmt(ExpressionStmt stmt);
	void visitBlockStmt(BlockStmt stmt);
	void visitIfStmt(IfStmt stmt);
	void visitWhileStmt(WhileStmt stmt);
}

abstract class Stmt{
	public abstract void accept(StmtVisitor visitor);
}

class VariableStmt : Stmt{
	public readonly Token identifier;
	public readonly Expr? val;

	public VariableStmt(Token identifier, Expr? val){
		this.identifier = identifier;
		this.val = val;
	}
	public override void accept(StmtVisitor visitor){ visitor.visitVariableStmt(this); }
}

class FunctionStmt : Stmt{
	public readonly Token identifier;
	public readonly List<string> arg_names;
	public readonly Stmt body;

	public FunctionStmt(Token identifier, List<string> arg_names, Stmt body){
		this.identifier = identifier;
		this.arg_names = arg_names;
		this.body = body;
	}
	public override void accept(StmtVisitor visitor){ visitor.visitFunctionStmt(this); }
}

class ClassStmt : Stmt{
	public readonly Token identifier;
	public readonly Dictionary<string, Expr> default_values;
	public readonly Dictionary<string, FunctionStmt> methods;
	public readonly FunctionStmt? ctor;
	public readonly Token? inherited_name;

	public ClassStmt(Token identifier, Dictionary<string, Expr> default_values, Dictionary<string, FunctionStmt> methods, FunctionStmt? ctor, Token? inherited_name){
		this.identifier = identifier;
		this.default_values = default_values;
		this.methods = methods;
		this.ctor = ctor;
		this.inherited_name = inherited_name;
	}
	public override void accept(StmtVisitor visitor){ visitor.visitClassStmt(this); }
}

class ReturnStmt : Stmt{
	public readonly Expr? return_val;

	public ReturnStmt(Expr? return_val){
		this.return_val = return_val;
	}
	public override void accept(StmtVisitor visitor){ visitor.visitReturnStmt(this); }
}

class ExpressionStmt : Stmt{
	public readonly Expr expr;

	public ExpressionStmt(Expr expr){
		this.expr = expr;
	}
	public override void accept(StmtVisitor visitor){ visitor.visitExpressionStmt(this); }
}

class BlockStmt : Stmt{
	public readonly List<Stmt> statements;

	public BlockStmt(List<Stmt> statements){
		this.statements = statements;
	}
	public override void accept(StmtVisitor visitor){ visitor.visitBlockStmt(this); }
}

class IfStmt : Stmt{
	public readonly Expr condition;
	public readonly Stmt true_stmt;
	public readonly Stmt? false_stmt;

	public IfStmt(Expr condition, Stmt true_stmt, Stmt? false_stmt){
		this.condition = condition;
		this.true_stmt = true_stmt;
		this.false_stmt = false_stmt;
	}
	public override void accept(StmtVisitor visitor){ visitor.visitIfStmt(this); }
}

class WhileStmt : Stmt{
	public readonly Expr condition;
	public readonly Stmt stmt;

	public WhileStmt(Expr condition, Stmt stmt){
		this.condition = condition;
		this.stmt = stmt;
	}
	public override void accept(StmtVisitor visitor){ visitor.visitWhileStmt(this); }
}

