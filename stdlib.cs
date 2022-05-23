namespace simp;

// The simp standard library
class StandardLibrary{
	public static void attach(Env<SIMPValue> env){
		env.define("epoch", new SIMPFunction(
			defined_env: env,
			native_fn: (List<SIMPValue> parameters) => {
				long unix_seconds = DateTimeOffset.Now.ToUnixTimeSeconds();
				return new SIMPNumber(unix_seconds);
			}
		));

		env.define("int_to_char", new SIMPFunction(
			defined_env: env,
			arity: 1,
			native_fn: (List<SIMPValue> parameters) => {
				int to_be_converted = (int) parameters[0].GetDouble();
				char char_convert = (char) to_be_converted;
				return new SIMPString(char_convert.ToString());
			}
		));
		
		env.define("readline", new SIMPFunction(
			defined_env: env,
			arity: 1,
			native_fn: (List<SIMPValue> parameters) => {
				string to_be_printed = parameters[0].GetString();
				Console.Write(to_be_printed);
				string val = Console.ReadLine() ?? "";
				return new SIMPString(val);
			}
		));

		env.define("print", new SIMPFunction(
			defined_env: env,
			arity: 1,
			native_fn: (List<SIMPValue> parameters) => {
				string to_be_printed = parameters[0].GetString();
				Console.Write(to_be_printed);
				return new SIMPNull();
			}
		));
	}
}
