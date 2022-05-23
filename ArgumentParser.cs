
enum ArgTokenType {
	NONE, EQUALS, COMMA, 
	FLAG, IDENTIFIER
}

class ArgToken{
	public readonly ArgTokenType type;
	public readonly string val;
	public ArgToken(string val, ArgTokenType type){ this.val = val; this.type = type; }
}

class ArgumentParser{
	public readonly Dictionary<string, List<string>> flags = new Dictionary<string, List<string>>();
	public readonly List<string> bool_flags = new List<string>(); 
	public readonly List<string> input = new List<string>();
	private readonly string raw;

	private int CurrentIdx = 0;
	private char CurrentChar { get => CurrentIdx >= raw.Length ? '\0': raw[CurrentIdx]; }
	private char NextChar { get => raw[CurrentIdx + 1]; }
	private bool IsFinishedLexing { get => raw.Length <= CurrentIdx; }
	private List<ArgToken> tokens = new List<ArgToken>();
	
	// lexer
	private bool match(char c){/*{{{*/
		if(NextChar == c){ CurrentIdx++; return true; }
		return false;
	} 

	private string GetString(){
		string str = "";
		while(CurrentChar.IsAlphanumeric() || CurrentChar == '.' || CurrentChar == '-'){
			str += CurrentChar;
			CurrentIdx++;
		}
		CurrentIdx--;
		return str;
	}
	
	private void GenerateTokens(){
		while(!IsFinishedLexing){
			switch(CurrentChar){
				case ' ': break;
				case '\n': break;
				case ',':
					tokens.Add(new ArgToken(", ", ArgTokenType.COMMA));
					break;
				case '=':
					tokens.Add(new ArgToken(" ", ArgTokenType.EQUALS));
					break;
				case '-':
					 match('-');
					CurrentIdx++;

					string flag = GetString();
					tokens.Add(new ArgToken(flag, ArgTokenType.FLAG));
					break;
				default:
					string str = GetString();
					tokens.Add(new ArgToken(str, ArgTokenType.IDENTIFIER));
					break;
			}

			CurrentIdx++;
		}		
	}/*}}}*/

	public ArgumentParser(string[] raw){ this.raw = String.Join(" ", raw); }

	private int CurrentTokenIdx = 0;
	private ArgToken CurrentToken { get => tokens[CurrentTokenIdx]; }
	private ArgToken PreviousToken { get => tokens[CurrentTokenIdx - 1]; }
	private ArgToken NextToken { get => tokens[CurrentTokenIdx + 1]; }
	private bool IsFinishedParsing { get => tokens.Count <= CurrentTokenIdx; }
	private bool matchToken(ArgTokenType type){
		if(IsFinishedParsing) return false;
		if(CurrentToken.type == type){ CurrentTokenIdx++; return true; }
		return false;
	}
	
	public ArgumentParser parse(){
		GenerateTokens();
		
		while(!IsFinishedParsing){
			if(matchToken(ArgTokenType.IDENTIFIER)){
				input.Add(PreviousToken.val);
			}else if(matchToken(ArgTokenType.FLAG)){
				ArgToken flag = PreviousToken;
				if(IsFinishedParsing) goto is_boolean; // it's boolean because next token doesn't exist

				if(matchToken(ArgTokenType.EQUALS) || CurrentToken.type == ArgTokenType.IDENTIFIER){
					List<string> idents = new List<string>();
					
					while(true){
						if(matchToken(ArgTokenType.IDENTIFIER)) idents.Add(PreviousToken.val);
						else if(matchToken(ArgTokenType.COMMA)) continue;
						else break; // token after ident is not comma so stop checking for idents
					}
					
					if(!flags.ContainsKey(flag.val)) flags.Add(flag.val, idents);
					else foreach(var ident in idents) flags[flag.val].Add(ident);

					continue; // jumps to the outermost while loop
				}

is_boolean:
				bool_flags.Add(flag.val);
			}
		}

		return this;
	}
}
