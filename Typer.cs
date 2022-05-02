class Typer{
	public static bool IsDecimal(object? obj){
		if(obj == null) return false;
		return obj.GetType() == typeof(decimal);
	}
	
	public static bool IsString(object? obj){
		if(obj == null) return false;
		return obj.GetType() == typeof(string);
	}
	
	public static bool IsBoolean(object? obj){
		if(obj == null) return false;
		return obj.GetType() == typeof(bool);
	}

	public static bool IsNull(object? obj){
		return obj == null;
	}

	public static decimal IntoDecimal(object? obj){
		if(obj == null) return 0;
		try{ return Convert.ToDecimal(obj); }
		catch{ return 0; }
	}

	public static bool IntoBoolean(object? obj){
		if(obj == null) return false;
		if(obj.GetType() == typeof(bool)) return (bool) obj;
		return false;
	}

	public static string IntoString(object? obj){
		if(obj == null) return "null";
		else return obj.ToString() ?? "null";
	}
}
