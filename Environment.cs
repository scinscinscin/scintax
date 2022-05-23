class Env<T> {
	private readonly Env<T>? enclosing = null;
	private readonly Dictionary<string, T> dict = new Dictionary<string, T>();
	public Env(Env<T>? enclosing = null){ this.enclosing = enclosing; }

	public void define(string key, T val){
		dict.Add(key, val);
	}

	public T get(string name){
		if(dict.ContainsKey(name)) return dict[name];
		if(enclosing != null) return enclosing.get(name);
		throw new Exception($"Tried to fetch undefined variable {name}");
	}

	public void assign(string name, T val){
		if(dict.ContainsKey(name)) { dict[name] = val; return; }
		if(enclosing != null) { enclosing.assign_no_fail(name, val); }
		else throw new Exception($"Tried to assign to undefined variable {name}");
	}
	
	public T? get_no_fail(string name){
		if(dict.ContainsKey(name)) return dict[name];
		if(enclosing != null) return enclosing.get_no_fail(name);
		else return default(T);
	}

	public void assign_no_fail(string name, T val){
		if(dict.ContainsKey(name)) { dict[name] = val; return; }
		if(enclosing != null) { enclosing.assign_no_fail(name, val); }
	}
}
