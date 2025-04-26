using System.Text;
using WebApplication17.Analyzer._AnalyzerUtils;

namespace WebApplication17.Analyzer;

public interface ILexer
{
    public List<Token> Tokenize(string fileContents);
}

public class LexerSimple : ILexer
{
    private char[] _fileChars;
    private int _currPos;
    private StringBuilder _buf;
    private List<Token> _tokens;
    
    public List<Token> Tokenize(string fileContents)
    {
         _tokens = new List<Token>();
        _fileChars = fileContents.ToCharArray();
        _buf = new StringBuilder();
        _currPos = 0;

        while (PeekChar() != null)
        {
            char consumedChar = ConsumeChar();
            switch (consumedChar)
            {
                case '/':
                    if (CheckForChar('/', 1))
                    {
                        ConsumeComment();
                    }else if (CheckForChar('*', 1))
                    {
                        ConsumeMultiLineComment();
                    }
                    break;
                case '{':
                    _tokens.Add(CreateToken(TokenType.OpenCurly));
                    break;
                case '}':
                    _tokens.Add(CreateToken(TokenType.CloseCurly));
                    break;
                default:
                    if (Char.IsLetter(consumedChar))
                    {
                        _buf.Append(consumedChar);
                        _tokens.Add(ConsumeKeyword(_buf));
                    }
                    break;
            }
        }
        
        return _tokens;
    }
    
    private Token ConsumeKeyword(StringBuilder buf)
    {
        buf.Append(ConsumeChar());
        while (PeekChar() != null && Char.IsLetterOrDigit(PeekChar().Value)) //no it can't be a null but thank you Rider
        {
            buf.Append(ConsumeChar());
        }

        Token token;
        string result = buf.ToString();
        switch (result)
        {
            default:
                token = CreateToken(TokenType.Ident, result);
                break;
        }

        buf.Clear();

        return token;
    }
    private void ConsumeComment()
    {
        ConsumeChar(); //consume '/'
        ConsumeChar(); //consume '/'
        while (PeekChar() != null && !(CheckForChar('\n') || CheckForChar('\r')))
        {
            ConsumeChar();
        }

        ConsumeChar(); // //consume '\n' or '\r'
        if (CheckForChar('\n')) //for windows
        {
            ConsumeChar(); 
        }
    }

    private void ConsumeMultiLineComment()
    {
        ConsumeChar(); //consume '/'
        ConsumeChar(); //consume '/*'
        
        while (PeekChar() != null && !(CheckForChar('*') && CheckForChar('/', 1)))
        {
            ConsumeChar();
        }

        ConsumeChar(); //consume '*'
        ConsumeChar(); //consume '/'
    }

    private bool CheckForChar(char checkedChar, int offset = 0)
    {
        return PeekChar(offset) == checkedChar;
    }
    private char? PeekChar(int offset = 0)
    {
        int accessIndex = offset + _currPos;
        if (accessIndex < _fileChars.Length - 1)
        {
            return _fileChars[accessIndex];
        }

        return null;
    }
    
    private Token CreateToken(TokenType type, string? value = null)
    {
        return new Token(type, _currPos, value);
    }
    private char ConsumeChar()
    {
        return _fileChars[_currPos++];
    }
    private char? TryConsumeChar(int offset = 0)
    {
        char? peekedChar = PeekChar(offset);
        if (peekedChar != null)
        {
            _currPos++;
            return peekedChar;
        }

        return null;
    }
}