class Env {
	private readonly Env? enclosing = null;
	private readonly Dictionary<string, SIMPValue> dict = new Dictionary<string, SIMPValue>();
	public Env(Env? enclosing = null){ this.enclosing = enclosing; }

	public void define(string key, SIMPValue val){
		dict.Add(key, val);
	}

	public SIMPValue get(Token name){
		if(dict.ContainsKey(name.lexeme)) return dict[name.lexeme];
		if(enclosing != null) return enclosing.get(name);
		throw new Exception($"Tried to fetch undefined variable {name.lexeme}");
	}

	public void assign(Token name, SIMPValue val){
		if(dict.ContainsKey(name.lexeme)) { dict[name.lexeme] = val; return; }
		if(enclosing != null) { enclosing.assign(name, val); }
		else throw new Exception($"Tried to assign to an undefined variable {name.lexeme}");
	}
}
