using static Crayon.Output;

abstract class SIMPValue {
	public abstract double GetDouble();
	public abstract string GetString();
	public abstract string GetPrettyString();
	public abstract bool GetBoolean();
	public abstract object? GetRaw();
}

class SIMPNull : SIMPValue {
	public override double GetDouble(){ return 0; }
	public override string GetString(){ return "null"; }
	public override string GetPrettyString(){ return Bold(White("null")); }
	public override bool GetBoolean(){ return false; }
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
	public readonly Env defined_env; // current environment when the function was defined
	public readonly List<string>? arg_names = null;
	public readonly int arity; // the number of parameters that it takes
	
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

	public SIMPValue call(Interpreter interpreter, List<SIMPValue> parameters){
		try{ 
			if(body != null) body.accept(interpreter);
			else if(native_fn != null) return native_fn(parameters);
			else throw new Exception("SIMPFunction has no callable property");
		}
		catch(SIMPValueException error){ return error.val; }

		return new SIMPNull();
	}

	public override double GetDouble(){ return 0; }
	public override string GetString(){ return native_fn != null ? "<native fn>" : "[SIMPFunction]"; }
	public override string GetPrettyString(){ return Yellow("[Function]"); }
	public override bool GetBoolean(){ return true; }
	public override object? GetRaw(){ return body; }
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
	public SIMPValue call(Interpreter interpreter, List<SIMPValue> parameters);
}
