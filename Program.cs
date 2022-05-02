class Program{
	public static void Main(string[] args){
		if(args.Length == 0){	Console.WriteLine("USAGE: ./simp <filetoparse>.scintax"); return; }
		for(int i = 0; i < args.Length; i++){
			Lexer lexer = new Lexer(File.ReadAllText(args[i]));
			while(lexer.Finished == false) lexer.parse();
			Console.WriteLine("Lexer has finished tokenizing. Generated {0} tokens.", lexer.tokens.Count);
			// Uncomment to list out all of the generated tokens
			// foreach(var token in lexer.tokens) token.PrintToConsole();
			
			Parser parser = new Parser(lexer.tokens);
			List<Stmt> statements = parser.parse();
			Console.WriteLine("Parser has finished parsing. Generated {0} statements", statements.Count);

			Interpreter interpreter = new Interpreter();
			interpreter.interpret(statements);
			//Console.WriteLine(interpreter.expression(headnode).GetRaw());
			//Console.WriteLine("The interpreter has finished running");
		}
	}
}

class ArgumentParser{
	private Dictionary<string, object?> map = new Dictionary<string, object?>();
	public ArgumentParser(){}
	
}
