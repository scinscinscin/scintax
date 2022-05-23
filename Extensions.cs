public static class Extensions{
	public static bool IsAlphabetical(this char a) { return (a >= 'A' && a <= 'Z') || (a >= 'a' && a <= 'z') || a == '_'; }
	public static bool IsNumeric(this char a) { return a >= '0' && a <= '9'; }
	public static bool IsAlphanumeric(this char a) { return IsAlphabetical(a) || IsNumeric(a); }

	public static bool IsArrowKey(this ConsoleKeyInfo ch){
		return ch.Key == ConsoleKey.UpArrow 
			|| ch.Key == ConsoleKey.DownArrow 
			|| ch.Key == ConsoleKey.LeftArrow 
			|| ch.Key == ConsoleKey.RightArrow;
	}

	public static bool IsTextChar(this ConsoleKeyInfo ch){
		return !Char.IsControl(ch.KeyChar) || ch.Key == ConsoleKey.Backspace || ch.Key == ConsoleKey.Enter || ch.Key == ConsoleKey.Tab;
	}
}
