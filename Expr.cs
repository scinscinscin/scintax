using System;

interface ExprVisitor<T>{
	T visitLiteralExpr(LiteralExpr expr);
	T visitBinaryExpr(BinaryExpr expr);
	T visitUnaryExpr(UnaryExpr expr);
	T visitGroupingExpr(GroupingExpr expr);
	T visitVariableExpr(VariableExpr expr);
	T visitAssignmentExpr(AssignmentExpr expr);
	T visitIndexAccessExpr(IndexAccessExpr expr);
	T visitDotAccessExpr(DotAccessExpr expr);
	T visitFunctionCallExpr(FunctionCallExpr expr);
	T visitArrayCreationExpr(ArrayCreationExpr expr);
}

abstract class Expr{
	public abstract T accept<T>(ExprVisitor<T> visitor);
}

class LiteralExpr : Expr{
	public readonly object? val;

	public LiteralExpr(object? val){
		this.val = val;
	}
	public override T accept<T>(ExprVisitor<T> visitor){ return visitor.visitLiteralExpr(this); }
}

class BinaryExpr : Expr{
	public readonly Expr lefthand;
	public readonly Token op;
	public readonly Expr righthand;

	public BinaryExpr(Expr lefthand, Token op, Expr righthand){
		this.lefthand = lefthand;
		this.op = op;
		this.righthand = righthand;
	}
	public override T accept<T>(ExprVisitor<T> visitor){ return visitor.visitBinaryExpr(this); }
}

class UnaryExpr : Expr{
	public readonly Token op;
	public readonly Expr expr;

	public UnaryExpr(Token op, Expr expr){
		this.op = op;
		this.expr = expr;
	}
	public override T accept<T>(ExprVisitor<T> visitor){ return visitor.visitUnaryExpr(this); }
}

class GroupingExpr : Expr{
	public readonly Expr contents;

	public GroupingExpr(Expr contents){
		this.contents = contents;
	}
	public override T accept<T>(ExprVisitor<T> visitor){ return visitor.visitGroupingExpr(this); }
}

class VariableExpr : Expr{
	public readonly Token identifier;

	public VariableExpr(Token identifier){
		this.identifier = identifier;
	}
	public override T accept<T>(ExprVisitor<T> visitor){ return visitor.visitVariableExpr(this); }
}

class AssignmentExpr : Expr{
	public readonly Expr writelocation;
	public readonly Expr val;

	public AssignmentExpr(Expr writelocation, Expr val){
		this.writelocation = writelocation;
		this.val = val;
	}
	public override T accept<T>(ExprVisitor<T> visitor){ return visitor.visitAssignmentExpr(this); }
}

class IndexAccessExpr : Expr{
	public readonly Expr contents;
	public readonly Expr index;

	public IndexAccessExpr(Expr contents, Expr index){
		this.contents = contents;
		this.index = index;
	}
	public override T accept<T>(ExprVisitor<T> visitor){ return visitor.visitIndexAccessExpr(this); }
}

class DotAccessExpr : Expr{
	public readonly Expr contents;
	public readonly Token ident;

	public DotAccessExpr(Expr contents, Token ident){
		this.contents = contents;
		this.ident = ident;
	}
	public override T accept<T>(ExprVisitor<T> visitor){ return visitor.visitDotAccessExpr(this); }
}

class FunctionCallExpr : Expr{
	public readonly Expr function;
	public readonly List<Expr> parameters;

	public FunctionCallExpr(Expr function, List<Expr> parameters){
		this.function = function;
		this.parameters = parameters;
	}
	public override T accept<T>(ExprVisitor<T> visitor){ return visitor.visitFunctionCallExpr(this); }
}

class ArrayCreationExpr : Expr{
	public readonly List<Expr> expressions;

	public ArrayCreationExpr(List<Expr> expressions){
		this.expressions = expressions;
	}
	public override T accept<T>(ExprVisitor<T> visitor){ return visitor.visitArrayCreationExpr(this); }
}

