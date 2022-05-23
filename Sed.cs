using static Crayon.Output;
using ssh;
using scat;
namespace sed;

class Sed{
	private static bool HasBeenInstantiated = false;
	private readonly string filepath;
	private TextBuffer textBuffer;
	private WindowBuffer windowBuffer;
	private Cursor cursor = new Cursor(row: 0, col: 0);
	private readonly List<List<FormatChar>> cache = new List<List<FormatChar>>();
	private readonly Dictionary<Kind, byte[]> theme;
	
	public Sed(ArgumentParser args){
		if(HasBeenInstantiated) throw new Exception("Already editing a File");
		if(args.input.Count == 0){
			Console.WriteLine("There are no files to edit");
			Environment.Exit(-1);
		}
		
		// Load the file, if it's empty, add an empty line
		filepath = args.input[0];
		string[] file = File.ReadAllLines(filepath);
		if(file.Length == 0) file = new string[1] { " " };

		textBuffer = new TextBuffer(file);
		windowBuffer = textBuffer.ToWindowBuffer();
		
		Console.CancelKeyPress += delegate(object? _, ConsoleCancelEventArgs e){ e.Cancel = true; };
		
		string? themePath = args.flags.ContainsKey("theme") ? args.flags["theme"][0] : null;
		theme = new Scat(customThemePath: themePath).theme;
	}

	public void Run(){
		Console.Clear();
		while(true){ Render(); HandleInput(); } 
	}
	
	private void Render(){
		textBuffer.AddSemanticHighlighting();
		windowBuffer = textBuffer.ToWindowBuffer();
		Console.Clear();

		for(int y = 0; y < Console.WindowHeight; y++){
			int lineIndex = windowBuffer.cursor.row - cursor.row + y;
			Console.SetCursorPosition(left: 0, top: y);
			
			if(lineIndex < windowBuffer.lines.Count){
				List<FormatChar> line = windowBuffer.lines[windowBuffer.cursor.row - cursor.row + y];

				for(int x = 0; x < Console.WindowWidth; x++){
					int charIndex = windowBuffer.cursor.col - cursor.col + x;
					
					if(charIndex < line.Count){
						// there is a character to be printed
						FormatChar fch = line[charIndex];
						byte[] colors = theme[fch.kind];
						Console.Write(Rgb(colors[0], colors[1], colors[2]).Bold($"{ fch.ch }"));
					}
				}
			}
		}

		Console.SetCursorPosition(left: cursor.col, top: cursor.row);
	}

	private void HandleInput(){
		ConsoleKeyInfo key = Console.ReadKey();
		
		if(key.Modifiers == ConsoleModifiers.Control){
			switch(key.Key){
				case ConsoleKey.Q:
					Console.Clear();
					Environment.Exit(0);
					break;
				case ConsoleKey.S:
					File.WriteAllText(this.filepath, textBuffer.stringify());
					break;
			}	
		}

		else if(key.IsArrowKey()) HandleArrowKey(key);
		else if(key.IsTextChar()) HandleTextCharacter(key);
	}

	private void ClampCursorHorizontally(){
		if(textBuffer.lines[textBuffer.cursor.row].Count < textBuffer.cursor.col){
			// this clamps the x position of the textBuffer's cursor if it is out of bounds
			textBuffer.cursor.col = textBuffer.lines[textBuffer.cursor.row].Count;
		}

		Cursor windowBufferCursor = textBuffer.CalculateWindowBufferCursor();
		cursor.col = windowBufferCursor.col;
	}

	private void HandleArrowKey(ConsoleKeyInfo key){/*{{{*/
		switch(key.Key){
			case ConsoleKey.UpArrow:
				if(textBuffer.cursor.row == 0) return; // because it's already at the top of the file
				if(cursor.row > 0) cursor.row--; // the cursor is in the middle of the screen
				textBuffer.cursor.row--;
				ClampCursorHorizontally();
				break;

			case ConsoleKey.DownArrow:
				if(textBuffer.cursor.row >= textBuffer.lines.Count - 1){
					textBuffer.cursor.row = textBuffer.lines.Count - 1; // it's already at the end of the file
					return;
				}

				if(cursor.row < Console.WindowHeight - 1) cursor.row++;
				textBuffer.cursor.row++;
				ClampCursorHorizontally();	
				break;

			case ConsoleKey.LeftArrow:
				if(textBuffer.cursor.col == 0) return; // because it's already at the beginning of the line
				if(cursor.col > 0){
					// determine how much to decrement the cursor position by
					if(textBuffer.PreviousChar.ch == '	') cursor.col -= textBuffer.TabSize;
					else cursor.col -= 1;
				}
				textBuffer.cursor.col--;
				break;
			
			case ConsoleKey.RightArrow:
				if(textBuffer.cursor.col >= textBuffer.lines[textBuffer.cursor.row].Count){
					textBuffer.cursor.col = textBuffer.lines[textBuffer.cursor.row].Count;
					// it's already at the beginning of the line
					return;
				}

				if(cursor.col < Console.WindowWidth - 1){
					// determine how much to increment the cursor position by
					if(textBuffer.CurrentChar.ch == '	') cursor.col += textBuffer.TabSize;
					else cursor.col += 1;
				}
				textBuffer.cursor.col++;
				break;

			default:
				throw new Exception("Received arrow instruction but key is not an arrow");
		}
	}/*}}}*/
	
	private void HandleTextCharacter(ConsoleKeyInfo key){
		switch(key.Key){
			case ConsoleKey.Enter:
				List<FormatChar> newLine = new List<FormatChar>();

				int endCondition = textBuffer.lines[textBuffer.cursor.row].Count;
				for(int i = textBuffer.cursor.col; i < endCondition; i++){
					newLine.Add(textBuffer.lines[textBuffer.cursor.row][textBuffer.cursor.col]);
					textBuffer.lines[textBuffer.cursor.row].RemoveAt(textBuffer.cursor.col);
				}

				textBuffer.lines.Insert(textBuffer.cursor.row + 1, newLine);
				textBuffer.cursor.row++;
				textBuffer.cursor.col = 0;
				
				if(cursor.row < Console.WindowHeight - 1) cursor.row++;
				cursor.col = 0;

				break;

			case ConsoleKey.Backspace:
				if(textBuffer.cursor.row == 0 && textBuffer.cursor.col == 0) return; // is at the beginning of the file

				if(textBuffer.cursor.col == 0){
					// append the contents of the current line to that of the previous line
					// then delete the current line then move the raw cursor to where it should be
					int previousRowOriginalLength = textBuffer.lines[textBuffer.cursor.row - 1].Count;
					foreach(var fch in textBuffer.lines[textBuffer.cursor.row])
						textBuffer.lines[textBuffer.cursor.row - 1].Add(fch);
					textBuffer.lines.RemoveAt(textBuffer.cursor.row);

					textBuffer.cursor.row--;
					textBuffer.cursor.col = previousRowOriginalLength;

					int WindowBufferCursorCol = textBuffer.CalculateWindowBufferCursor().col;
					if(cursor.row != 0) cursor.row--;
					cursor.col = WindowBufferCursorCol < Console.WindowWidth ? WindowBufferCursorCol : (int) (Console.WindowWidth / 2);
				}
				
				// deleting a character in the middle of a file
				else{
					FormatChar deleted = textBuffer.lines[textBuffer.cursor.row][textBuffer.cursor.col - 1];
					textBuffer.lines[textBuffer.cursor.row].RemoveAt(textBuffer.cursor.col - 1);
					textBuffer.cursor.col--;
					if(cursor.col != 0){
						if(deleted.ch != '	') cursor.col--;
						else cursor.col -= textBuffer.TabSize;
					};
				}
						
				break;

			default:
				FormatChar newFch = new FormatChar(key.KeyChar);	
				textBuffer.lines[textBuffer.cursor.row].Insert(textBuffer.cursor.col, newFch);
				textBuffer.cursor.col++;
				if(cursor.col < Console.WindowWidth - 1)
					cursor.col += key.KeyChar == '	' ? textBuffer.TabSize : 1;	
				break;
		}
	}
}

class FormatChar{
	public readonly char ch;
	public Kind kind = Kind.NONE;
	public FormatChar(char ch){ this.ch = ch; }
}
