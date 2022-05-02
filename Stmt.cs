using System;

interface StmtVisitor{
	void visitPrintStmt(PrintStmt stmt);
	void visitVariableStmt(VariableStmt stmt);
	void visitExpressionStmt(ExpressionStmt stmt);
	void visitBlockStmt(BlockStmt stmt);
	void visitIfStmt(IfStmt stmt);
	void visitWhileStmt(WhileStmt stmt);
}

abstract class Stmt{
	public abstract void accept(StmtVisitor visitor);
}

class PrintStmt : Stmt{
	public readonly Expr expr;

	public PrintStmt(Expr expr){
		this.expr = expr;
	}
	public override void accept(StmtVisitor visitor){ visitor.visitPrintStmt(this); }
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

