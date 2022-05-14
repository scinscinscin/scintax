using static Crayon.Output;

abstract class SIMPValue {
	public virtual double GetDouble(){ return 0; }
	public abstract string GetString();
	public abstract string GetPrettyString();
	public virtual bool GetBoolean(){ return false; }
	public virtual object? GetRaw(){ return this; }
}

class SIMPNull : SIMPValue {
	public override string GetString(){ return "null"; }
	public override string GetPrettyString(){ return Bold(White("null")); }
	public override object? GetRaw(){ return null; }
}

class SIMPString : SIMPValue, IndexAccessible, DotAccessible {
	public readonly string val;
	public SIMPString(string val) { this.val = val; }
	
	public override double GetDouble(){ return Convert.ToDouble(val); }
	public override string GetString(){ return val; }
	public override string GetPrettyString(){ return Green($"{val}"); }
	public override bool GetBoolean(){ return val == "" ? false : true; }
	public override object? GetRaw(){ return val; }

	public SIMPValue IndexAccess(SIMPValue idx){
		if(idx is SIMPNumber){
			int numidx = (int) idx.GetDouble();
			if(numidx < val.Length) return new SIMPString(val[numidx].ToString());
			else throw new Exception("Out of bounds string index access");
		}

		throw new Exception("Tried to access string index using non-number type");
	}

	public SIMPNumber GetLength(){ return new SIMPNumber(val.Length); }

	public SIMPValue DotAccess(string identifier){
		if(identifier.Equals("length")){
			return GetLength();
		}

		throw new Exception("Cannot access that identifier in SIMPString");
	}
}

class SIMPNumber : SIMPValue {
	public readonly double val;
	public SIMPNumber(double val) { this.val = val; } 

	public override double GetDouble(){ return val; }
	public override string GetString(){ return Convert.ToString(val); }
	public override string GetPrettyString(){ return $"{Yellow(GetString())}"; }
	public override bool GetBoolean(){ return val != 0; }
	public override object? GetRaw(){ return val; }
}

class SIMPBool : SIMPValue {
	public readonly bool val;
	public SIMPBool(bool val) { this.val = val; } 
	
	public override double GetDouble(){ return val ? 1 : 0; }
	public override string GetString(){ return val ? "true" : "false"; }
	public override string GetPrettyString(){ return Bold(val ? Green("true") : Red("false")); }
	public override bool GetBoolean(){ return val; }
	public override object? GetRaw(){ return val; }
}

class SIMPArray : SIMPValue, IndexAccessible {
	public readonly List<SIMPValue?> val;
	public SIMPArray(List<SIMPValue?> val) { this.val = val; }
	
	public override double GetDouble(){ return val.Count(); }
	public override string GetString(){ return "[SIMPArray]"; }
	public override string GetPrettyString(){ return Yellow("[SIMPArray]"); }
	public override bool GetBoolean(){ return val.Count() != 0; }
	public override object? GetRaw(){ return val;}

	public SIMPNumber GetLength(){ return new SIMPNumber(val.Count()); }
	public SIMPValue IndexAccess(SIMPValue idx){
		if(idx is SIMPNumber){
			int numidx = (int) ((SIMPNumber) idx).GetDouble();
			if(numidx < val.Count()) return val[numidx] ?? new SIMPNull();
			else return new SIMPNull();
		};
		throw new Exception("Tried to access array index using non-number type");
	}

	public void IndexWrite(SIMPValue idx, SIMPValue writeval){
		if(idx is SIMPNumber){
			int numidx = (int) ((SIMPNumber) idx).GetDouble();
			// if numidx < count, the user is trying to rewrite an existing index
			// else, we need to add null values until the Count is equal to numidx, then add the new value
			
			if(numidx < val.Count()) val[numidx] = writeval;
			else{
				while(numidx != val.Count()) val.Add(null);
				val.Add(writeval);
			}
		}else{
			throw new Exception("Tried to access array using non-number type");
		}
	}
}

class SIMPValueException : Exception {
	public readonly SIMPValue val;
	public SIMPValueException(SIMPValue val) { this.val = val; }
}

class SIMPFunction : SIMPValue, Callable {
	private readonly Env defined_env; // current environment when the function was defined
	private readonly List<string>? arg_names = null;
	private readonly int arity; // the number of parameters that it takes
	
	// the things to call
	public readonly Stmt? body = null;
	public readonly Func<List<SIMPValue>, SIMPValue>? native_fn = null;

	public SIMPFunction(
			Env defined_env, List<string>? arg_names = null, int arity = 0,
			Stmt? body = null, Func<List<SIMPValue>, SIMPValue>? native_fn = null
	){
		this.defined_env = defined_env;
		this.arg_names = arg_names;
		this.body = body;
		this.native_fn = native_fn;
		this.arity = arg_names != null ? arg_names.Count : arity;

		if(body == null && native_fn == null)
			throw new Exception("Attempted to instatiate a SIMPFunction with no callable property");
	}

	// Sets the environment of the interpreter to a new environment which encloses the defined_env of the function
	// In this new environment, define all the arguments in arg_names based on the parameters given
	// then execute the body of the function by passing in the interpreter
	public SIMPValue Call(Interpreter interpreter, List<SIMPValue> parameters){
		if(body == null && native_fn == null)
			throw new Exception("SIMPFunction has no callable property");
		if(arity != parameters.Count)
			throw new Exception($"Function expected {arity} arguments, received {parameters.Count}");
		if(native_fn != null) return native_fn(parameters);
		
		// Creates a new environment and defines all the arguments in arg_names based on the parameters given
		Env currentEnv = interpreter.env;
		interpreter.env = new Env(defined_env);
		if(arg_names != null)
			for(int i = 0; i < arity; i++)
				interpreter.env.define(arg_names[i], parameters[i]);

		SIMPValue return_value = new SIMPNull();
		try{
			if(body != null) body.accept(interpreter);
			else throw new Exception("SIMPFunction has no callable property");
		}
		catch(SIMPValueException error){ return_value = error.val; }
		
		interpreter.env = currentEnv;
		return return_value;
	}

	public int GetArity(){ return arity; }
	public List<string>? GetArgNames(){ return arg_names; }

	public override string GetString(){ return native_fn != null ? "<native fn>" : "[SIMPFunction]"; }
	public override string GetPrettyString(){ return Yellow("[Function]"); }
	public override bool GetBoolean(){ return true; }
	public override object? GetRaw(){ return body; }
}

class SIMPClassConstructor : SIMPValue, Callable {
	public readonly string class_name;
	public readonly string? inherited_class_name = null;
	public readonly Dictionary<string, Expr> default_values;
	public readonly Dictionary<string, FunctionStmt> methods;
	public readonly FunctionStmt? ctor_method = null;

	public SIMPClassConstructor(ClassStmt stmt){
		this.class_name = stmt.identifier.lexeme;
		this.default_values = stmt.default_values;
		this.methods = stmt.methods;
		this.ctor_method = stmt.ctor;

		if(stmt.inherited_name != null)
			inherited_class_name = stmt.inherited_name.lexeme;
	}

	public SIMPValue Call(Interpreter interpreter, List<SIMPValue> parameters){
		Dictionary<string, SIMPValue>	map = new Dictionary<string, SIMPValue>();
		SIMPClassInstance newInstance = new SIMPClassInstance(class_name, map);
		
		Env this_env = new Env(interpreter.env);
		Env base_env = new Env(this_env);

		this_env.define("this", newInstance);
		ApplyInheritance(interpreter, newInstance, this_env, base_env);
	
		// add default values and methods to the class instance
		foreach(var item in default_values) map[item.Key] = interpreter.evaluate(item.Value);
		foreach(var func in methods){
			map[func.Key] = new SIMPFunction(
					defined_env: base_env, // if there is a superclass, it now has access to "base" 
					arg_names: func.Value.arg_names,
					body: func.Value.body
			);
		}

		if(ctor_method != null){
			new SIMPFunction(
				defined_env: base_env,
				arg_names: ctor_method.arg_names,
				body: ctor_method.body
			).Call(interpreter, parameters);
		}
		
		return newInstance;
	}

	public void ApplyInheritance(Interpreter interpreter, SIMPClassInstance instance, Env this_env, Env higher_env){
		if(inherited_class_name == null) return; 
		SIMPValue super = interpreter.env.get(inherited_class_name); // get the superclass constructor
		
		if(super is SIMPClassConstructor superclass){
			// Create environment where superclass will attatch methods to
			Env base_env = new Env(this_env);
			superclass.ApplyInheritance(interpreter, instance, this_env, base_env);
			
			// Add the superclass methods properties in a dictionary
			Dictionary<string, SIMPValue> method_map = new Dictionary<string, SIMPValue>();
			foreach(var method in superclass.methods){
				method_map[method.Key] = new SIMPFunction(
					defined_env: base_env,
					arg_names: method.Value.arg_names,
					body: method.Value.body
				);
			}

			// Add all the superclass properties to the instance dict
			foreach(var member in superclass.default_values)
				instance.map[member.Key] = interpreter.evaluate(member.Value);
				
			// Define base as the superclass in the subclass instance
			higher_env.define("base", new SIMPClassInstance( 
				class_name: superclass.class_name, 
				map: method_map
			));
			
			// Define super as the superclass constructor in the subclass instance
			if(superclass.ctor_method != null)
				higher_env.define("super", new SIMPFunction(
					defined_env: base_env,
					arg_names: superclass.ctor_method.arg_names,
					body: superclass.ctor_method.body
				));

			return;
		}
		
		throw new Exception("Attempted to inherit from non SIMPClassConstructor value");
	}

	public int GetArity(){ return ctor_method != null ? ctor_method.arg_names.Count : 0; }
	public List<string>? GetArgNames(){ return ctor_method?.arg_names; }
	
	public override string GetString(){ return "[SIMPClassConstructor]"; }
	public override string GetPrettyString(){ return Yellow("[SIMPClassConstructor]"); }
}

class SIMPClassInstance : SIMPValue, DotAccessible {
	public readonly string class_name;
	public readonly Dictionary<string, SIMPValue> map;
	
	public SIMPClassInstance(string class_name, Dictionary<string, SIMPValue> map){
		this.class_name = class_name;
		this.map = map;
	}

	public SIMPValue DotAccess(string key){ return map[key]; }
	public void DotWrite(string identifier, SIMPValue val){ map[identifier] = val; }

	public override string GetString(){ return $"[{class_name}]"; }
	public override string GetPrettyString(){ return Green(GetString()); }
	public override object? GetRaw(){ return map; }
}

interface IndexAccessible{
	public SIMPValue IndexAccess(SIMPValue idx);
	public void IndexWrite(SIMPValue idx, SIMPValue val){
		throw new Exception("Attempted to write to a readonly index-accessible SIMP");
	}
	public SIMPNumber GetLength();
}

interface DotAccessible{
	public SIMPValue DotAccess(string identifier);
	public void DotWrite(string identifier, SIMPValue val){
		throw new Exception("Attempted to write to a readonly dot-accessible SIMP");
	}
}

interface Callable{
	public int GetArity();
	public List<string>? GetArgNames();
	public SIMPValue Call(Interpreter interpreter, List<SIMPValue> parameters);
}
