abstract class SIMPValue {
	public abstract double GetDouble();
	public abstract string GetString();
	public abstract bool GetBoolean();
	public abstract object? GetRaw();
}

class SIMPNull : SIMPValue {
	public override double GetDouble(){ return 0; }
	public override string GetString(){ return "null"; }
	public override bool GetBoolean(){ return false; }
	public override object? GetRaw(){ return null; }
}

class SIMPString : SIMPValue, IndexAccessible, DotAccessible {
	public readonly string val;
	public SIMPString(string val) { this.val = val; }
	
	public override double GetDouble(){ return Convert.ToDouble(val); }
	public override string GetString(){ return val; }
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
	public override bool GetBoolean(){ return val != 0; }
	public override object? GetRaw(){ return val; }
}

class SIMPBool : SIMPValue {
	public readonly bool val;
	public SIMPBool(bool val) { this.val = val; } 
	
	public override double GetDouble(){ return val ? 1 : 0; }
	public override string GetString(){ return val ? "true" : "false"; }
	public override bool GetBoolean(){ return val; }
	public override object? GetRaw(){ return val; }
}

class SIMPArray : SIMPValue, IndexAccessible {
	public readonly List<SIMPValue?> val;
	public SIMPArray(List<SIMPValue?> val) { this.val = val; }
	
	public override double GetDouble(){ return val.Count(); }
	public override string GetString(){ return "[SIMPArray]"; }
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
