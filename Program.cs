using static Crayon.Output;

class Program{
	public static List<Token> generateTokens(string code){
		Lexer lexer = new Lexer(code);
		while(!lexer.Finished) lexer.parse();
		return lexer.tokens;
	}

	public static void repl(){
		Interpreter interpreter = new Interpreter(isREPL: true);
		// handle Ctrl+C gracefully
		Console.CancelKeyPress += delegate { Console.Write("\n"); Environment.Exit(0); };
		Console.Write(Bold(Green("scintax interpreter & mathematical processor")));
		
		while(true){
			Console.Write("\n>> ");
			string? code = Console.ReadLine();
			if(code != null && code.Length != 0){
				List<Stmt> statements = new Parser(generateTokens(code), isREPL: true).parse();	
				interpreter.interpret(statements);
			}
		}
	}

	public static void Main(string[] args){
		if(args.Length == 0) repl();

		for(int i = 0; i < args.Length; i++){
			Lexer lexer = new Lexer(File.ReadAllText(args[i]));
			while(lexer.Finished == false) lexer.parse();
			Console.WriteLine("Lexer has finished tokenizing. Generated {0} tokens.", lexer.tokens.Count);
			// Uncomment to list out all of the generated tokens
			// foreach(var token in lexer.tokens) token.PrintToConsole();
			
			Parser parser = new Parser(lexer.tokens, isREPL: false);
			List<Stmt> statements = parser.parse();
			Console.WriteLine("Parser has finished parsing. Generated {0} statements", statements.Count);

			Interpreter interpreter = new Interpreter(isREPL: false);
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
