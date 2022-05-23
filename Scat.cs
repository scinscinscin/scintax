using static Crayon.Output;
using scintax;
using simp;
using ssh;
namespace scat;

class Scat{
	public readonly Dictionary<Kind, byte[]> theme;

	public Env<SIMPValue> ParseTheme(string? themePath){
		if(themePath == null) themePath = "default.shit";

		List<Token> tokens = Lexer.GenerateTokens(path: themePath);
		Parser parser = new Parser(tokens, isREPL: false);
		List<Stmt> statements = parser.parse();
		
		Func<double, double, double, SIMPArray> CreateRGBArray = (r, g, b) => {
			List<SIMPValue?> val = new List<SIMPValue?>(){
				new SIMPNumber(r), new SIMPNumber(g), new SIMPNumber(b)
			};
			return new SIMPArray(val);
		};
		
		Action<Env<SIMPValue>> stdlib = (global_env) => {
			// Define the default theme
			global_env.define("NONE"             , CreateRGBArray( 197, 200, 198 ));/*{{{*/
			global_env.define("COMMENT"          , CreateRGBArray( 106, 153, 85  ));
			global_env.define("VARIABLE_NAME"    , CreateRGBArray( 156, 220, 254 ));
			global_env.define("ACCESSOR_TOKEN"   , CreateRGBArray( 156, 220, 254 ));
			global_env.define("FUNCTION_NAME"    , CreateRGBArray( 220, 220, 170 ));
			global_env.define("CTOR_DECLARATION" , CreateRGBArray( 78,  201, 176 ));
			global_env.define("CLASS_NAME"       , CreateRGBArray( 78,  201, 176 ));
			global_env.define("ARGUMENT"         , CreateRGBArray( 156, 220, 254 ));
			global_env.define("THIS"             , CreateRGBArray( 86,  156, 214 ));
			global_env.define("BASE"             , CreateRGBArray( 86,  156, 214 ));
			global_env.define("SUPER"            , CreateRGBArray( 86,  156, 214 ));
			global_env.define("CALLED"           , CreateRGBArray( 220, 220, 170 ));
			global_env.define("VAR_KEYWORD"      , CreateRGBArray( 86,  156, 214 ));
			global_env.define("FUNCTION_KEYWORD" , CreateRGBArray( 86,  156, 214 ));
			global_env.define("CLASS_KEYWORD"    , CreateRGBArray( 86,  156, 214 ));
			global_env.define("IF_KEYWORD"       , CreateRGBArray( 197, 134, 192 ));
			global_env.define("ELSE_KEYWORD"     , CreateRGBArray( 197, 134, 192 ));
			global_env.define("WHILE_KEYWORD"    , CreateRGBArray( 197, 134, 192 ));
			global_env.define("RETURN_KEYWORD"   , CreateRGBArray( 197, 134, 192 ));
			global_env.define("STRING_LITERAL"   , CreateRGBArray( 206, 145, 120 ));
			global_env.define("NUMBER_LITERAL"   , CreateRGBArray( 181, 206, 128 ));
			global_env.define("TRUE"             , CreateRGBArray( 86,  156, 214 ));
			global_env.define("FALSE"            , CreateRGBArray( 86,  156, 214 ));
			global_env.define("NULL"             , CreateRGBArray( 86,  156, 214 ));
			global_env.define("B0"               , CreateRGBArray( 23,  159, 255 ));
			global_env.define("B1"               , CreateRGBArray( 255, 215, 0   ));
			global_env.define("B2"               , CreateRGBArray( 218, 112, 214 ));/*}}}*/
			
			global_env.define("rgb", new SIMPFunction(
				defined_env: global_env,
				arity: 3,
				native_fn: (List<SIMPValue> parameters) => {
					return CreateRGBArray( parameters[0].GetDouble(), parameters[1].GetDouble(), parameters[2].GetDouble());
				}
			));
		};

		Interpreter interpreter = new Interpreter(isREPL: false, stdlib: stdlib);
		interpreter.interpret(statements);

		return interpreter.global_env;
	}

	public static Dictionary<Kind, byte[]> GenerateTheme(Env<SIMPValue> env){
		Dictionary<Kind, byte[]> theme = new Dictionary<Kind, byte[]>();
		
		Func<SIMPValue, byte[]> CreateRGBArray = (val) => {
			if(val is SIMPArray arr){
				byte[] byte_arr = new byte[3];
				for(int i = 0; i < 3; i++) byte_arr[i] = (byte) arr.val[i]!.GetDouble();
				return byte_arr;
			}

			throw new Exception("Attempted to convert non SIMPArray to byte array");	
		};
		
		foreach(var kind in (Kind[]) Enum.GetValues(typeof(Kind))){
			theme.Add(kind, CreateRGBArray(env.get(kind.ToString())));
		}

		return theme;
	}

	public Scat(string? customThemePath = null){
		Env<SIMPValue> env = ParseTheme(customThemePath);
		theme = GenerateTheme(env);
	}

	public void Print(string file, List<Kind> props){
		for(int chidx = 0; chidx < file.Length; chidx++){
			Kind currentKind = chidx >= props.Count ? Kind.NONE : props[chidx];
			byte[] colors = theme[currentKind];
			Console.Write(Rgb(colors[0], colors[1], colors[2]).Bold().Text($"{ file[chidx] }"));
		}
	}
}
