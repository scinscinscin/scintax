using scintax;
using ssh;
using scat;
namespace sed;

class Buffer{
	public Cursor cursor = new Cursor(row: 0, col: 0);
	public readonly List<List<FormatChar>> lines;
	public FormatChar CurrentChar { get => lines[cursor.row][cursor.col]; }
	public FormatChar PreviousChar { get => lines[cursor.row][cursor.col - 1]; }

	protected Buffer(List<List<FormatChar>> lines){ this.lines = lines; }
	protected Buffer(string[] raw_lines){
		List<List<FormatChar>> lines = new List<List<FormatChar>>();
		
		foreach(var raw_line in raw_lines){
			List<FormatChar> line = new List<FormatChar>();
			foreach(var ch in raw_line) line.Add(new FormatChar(ch));
			lines.Add(line);
		}
		
		this.lines = lines;
	}

	public string stringify(){
		string ret = "";

		foreach(var line in lines){
			foreach(var fch in line) ret += fch.ch;
			ret += '\n';
		}

		return ret;
	}
}

class TextBuffer : Buffer{
	public TextBuffer(string[] lines) : base(lines) {}
	public int TabSize = 2;
	public bool HasBeenModified = true;

	public WindowBuffer ToWindowBuffer(){
		List<List<FormatChar>> WindowBufferLines = new List<List<FormatChar>>();

		foreach(var line in lines){
			List<FormatChar> WindowBufferLine = new List<FormatChar>();
			
			foreach(var fch in line){
				if(fch.ch == '	') // handle tabs
					for(int i = 0; i < TabSize; i++)
						WindowBufferLine.Add(new FormatChar(' '));
				else
					WindowBufferLine.Add(fch);
			}

			WindowBufferLines.Add(WindowBufferLine);
		}

		Cursor WindowBufferCursor = CalculateWindowBufferCursor();
		return new WindowBuffer(WindowBufferLines, WindowBufferCursor);
	}

	public void AddSemanticHighlighting(){
		if(HasBeenModified){
			List<Token> tokens = Lexer.GenerateTokens(file: stringify());
			SemanticHighlighter highlighter = new SemanticHighlighter(tokens);
			highlighter.highlight();

			// Apply highlighter.props to lines
			int kindIdx = 0;
			foreach(var line in lines){
				foreach(var fch in line){
					fch.kind = kindIdx >= highlighter.props.Count ? Kind.NONE : highlighter.props[kindIdx];
					kindIdx++;
				}
				kindIdx++; // consumes newline
			}
		}
	}

	public Cursor CalculateWindowBufferCursor(){
		Cursor WindowBufferCursor = new Cursor(row: cursor.row, col: 0);
		
		for(int i = 0; i < cursor.col; i++){
			var fch = lines[cursor.row][i];
			if(fch.ch == '	') WindowBufferCursor.col += TabSize;
			else WindowBufferCursor.col += 1;
		}
		return WindowBufferCursor;
	}
}

class WindowBuffer : Buffer{
	public WindowBuffer(List<List<FormatChar>> lines, Cursor cursor) : base(lines) {
		this.cursor = cursor;
	}
}

class Cursor{
	public int row, col;
	public Cursor(int row, int col){
		this.row = row; 
		this.col = col;
	}
}
