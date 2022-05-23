using static Crayon.Output;
using scintax;
using simp;
using ssh;
using scat;
using sed;

class Program{
	public static void repl(){
		Interpreter interpreter = new Interpreter(isREPL: true, stdlib: StandardLibrary.attach);
		// handle Ctrl+C gracefully
		Console.CancelKeyPress += delegate { Console.Write("\n"); Environment.Exit(0); };
		Console.Write(Bold(Green("scintax interpreter & mathematical processor")));
		
		while(true){
			Console.Write("\n>> ");
			string? code = Console.ReadLine();
			if(code != null && code.Length != 0){
				List<Stmt> statements = new Parser(Lexer.GenerateTokens(file: code), isREPL: true).parse();	
				interpreter.interpret(statements);
			}
		}
	}

	public static void Main(string[] rawArgs){
		ArgumentParser args = new ArgumentParser(rawArgs).parse();
		if(args.input.Count == 0) repl();
		
		switch(args.input[0]){
			case "scat":
				string? themePath = args.flags.ContainsKey("theme") ? args.flags["theme"][0] : null;
				Scat scat = new Scat(customThemePath: themePath);
		
				for(int i = 1; i < args.input.Count; i++){
					string file = File.ReadAllText(args.input[i]);
					List<Token> tokens = Lexer.GenerateTokens(file: file);
					SemanticHighlighter highlighter = new SemanticHighlighter(tokens);
					highlighter.highlight();
					scat.Print(file, highlighter.props);
				}
				break;

			case "sed":
				args.input.RemoveAt(0);
				new Sed(args).Run();
				break;
			
			case "run":
				args.input.RemoveAt(0);
				goto default;
			
			default:
				for(int i = 0; i < args.input.Count; i++){
					List<Token> tokens = Lexer.GenerateTokens(path: args.input[i]);
					Parser parser = new Parser(tokens, isREPL: false);
					List<Stmt> statements = parser.parse();

					Interpreter interpreter = new Interpreter(stdlib: StandardLibrary.attach, isREPL: false);
					interpreter.interpret(statements);
				}
				break;
		}
	}
}

