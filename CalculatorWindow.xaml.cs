using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace calc
{
    public partial class CalculatorWindow : Window
    {
        private bool _useDegrees = true;
        private bool _errorState = false;
        private bool _lastWasEquals = false;

        public CalculatorWindow()
        {
            InitializeComponent();
            // fixed column layout: do not auto-change columns on resize
        }

        // Keeping fixed Columns on the grids for predictable layout similar to Windows Calculator.

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b)
            {
                var input = b.Content?.ToString() ?? string.Empty;
                AppendInput(input);
            }
        }

        private void AppendInput(string input)
        {
            if (_errorState)
            {
                Display.Text = string.Empty;
                _errorState = false;
                _lastWasEquals = false;
            }
            else if (_lastWasEquals)
            {
                _lastWasEquals = false;
                if (!Operators.Contains(input))
                    Display.Text = string.Empty;
            }

            Display.Text += input;
        }

        private void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            var key = e.Key;
            var shiftPressed = (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Shift) != 0;

            if (key == System.Windows.Input.Key.Enter || key == System.Windows.Input.Key.Return)
                Equals_Click(sender, e);
            else if (key == System.Windows.Input.Key.Escape)
                Clear_Click(sender, e);
            else if (key == System.Windows.Input.Key.Delete)
                ClearEntry_Click(sender, e);
            else if (key == System.Windows.Input.Key.Back)
                Backspace_Click(sender, e);
            else if (shiftPressed && key == System.Windows.Input.Key.D9)
                AppendInput("(");
            else if (shiftPressed && key == System.Windows.Input.Key.D0)
                AppendInput(")");
            else if (key >= System.Windows.Input.Key.D0 && key <= System.Windows.Input.Key.D9)
                AppendInput(((int)key - (int)System.Windows.Input.Key.D0).ToString(CultureInfo.InvariantCulture));
            else if (key >= System.Windows.Input.Key.NumPad0 && key <= System.Windows.Input.Key.NumPad9)
                AppendInput(((int)key - (int)System.Windows.Input.Key.NumPad0).ToString(CultureInfo.InvariantCulture));
            else if (key == System.Windows.Input.Key.Add || (key == System.Windows.Input.Key.OemPlus && shiftPressed))
                AppendInput("+");
            else if (key == System.Windows.Input.Key.Subtract || key == System.Windows.Input.Key.OemMinus)
                AppendInput("-");
            else if (key == System.Windows.Input.Key.Multiply)
                AppendInput("*");
            else if (key == System.Windows.Input.Key.Divide || key == System.Windows.Input.Key.Oem2)
                AppendInput("/");
            else if (key == System.Windows.Input.Key.OemPlus)
                Equals_Click(sender, e);
            else if (key == System.Windows.Input.Key.Decimal || key == System.Windows.Input.Key.OemPeriod)
                AppendInput(".");
            else if (key == System.Windows.Input.Key.OemComma)
                AppendInput(",");
            else if (key == System.Windows.Input.Key.OemOpenBrackets)
                AppendInput("(");
            else if (key == System.Windows.Input.Key.Oem6)
                AppendInput(")");
            else
                return;

            e.Handled = true;
        }

        private void Window_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            if (e.Text.Length > 0 && e.Text.All(char.IsLetter))
            {
                AppendInput(e.Text);
                e.Handled = true;
            }
        }

        private void Function_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b)
            {
                var name = b.Content?.ToString() ?? string.Empty;
                if (!_errorState && double.TryParse(Display.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                {
                    try
                    {
                        var result = ApplyFunction(name, value);
                        if (!double.IsFinite(result))
                            throw new ArithmeticException("Result is not finite");

                        Display.Text = result.ToString(CultureInfo.InvariantCulture);
                        _lastWasEquals = true;
                        _errorState = false;
                    }
                    catch (Exception)
                    {
                        Display.Text = "Error";
                        _errorState = true;
                        _lastWasEquals = false;
                    }

                    return;
                }

                if (_errorState || _lastWasEquals)
                {
                    Display.Text = string.Empty;
                    _errorState = false;
                    _lastWasEquals = false;
                }
                // functions append name(
                Display.Text += name + "(";
            }
        }

        private void Constant_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b)
            {
                var name = b.Content.ToString();
                if (_errorState || _lastWasEquals)
                {
                    Display.Text = string.Empty;
                    _errorState = false;
                    _lastWasEquals = false;
                }
                if (string.Equals(name, "pi", StringComparison.OrdinalIgnoreCase))
                    Display.Text += Math.PI.ToString(CultureInfo.InvariantCulture);
                else if (string.Equals(name, "e", StringComparison.OrdinalIgnoreCase))
                    Display.Text += Math.E.ToString(CultureInfo.InvariantCulture);
                else
                    Display.Text += name;
            }
        }

        private void ToggleAngleMode_Click(object sender, RoutedEventArgs e)
        {
            _useDegrees = !_useDegrees;
            if (sender is Button b)
            {
                b.Content = _useDegrees ? "DEG" : "RAD";
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            Display.Text = string.Empty;
            _errorState = false;
            _lastWasEquals = false;
        }

        private void ClearEntry_Click(object sender, RoutedEventArgs e)
        {
            if (_errorState || _lastWasEquals)
            {
                Clear_Click(sender, e);
                return;
            }

            var expression = Display.Text;
            if (expression.Length == 0)
                return;

            var end = expression.Length;
            if (expression[end - 1] == '(')
            {
                var nameEnd = end - 1;
                var nameStart = nameEnd;
                while (nameStart > 0 && char.IsLetter(expression[nameStart - 1]))
                    nameStart--;
                Display.Text = expression.Remove(nameStart);
                return;
            }

            if (expression[end - 1] == ')')
            {
                var depth = 0;
                var openIndex = -1;
                for (var i = end - 1; i >= 0; i--)
                {
                    if (expression[i] == ')') depth++;
                    else if (expression[i] == '(' && --depth == 0)
                    {
                        openIndex = i;
                        break;
                    }
                }

                if (openIndex >= 0)
                {
                    var termStart = openIndex;
                    while (termStart > 0 && char.IsLetter(expression[termStart - 1]))
                        termStart--;
                    Display.Text = expression.Remove(termStart);
                    return;
                }
            }

            var numberStart = end;
            while (numberStart > 0 && (char.IsDigit(expression[numberStart - 1]) || expression[numberStart - 1] == '.'))
                numberStart--;

            if (numberStart < end)
                Display.Text = expression.Remove(numberStart);
        }

        private void Backspace_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(Display.Text))
                Display.Text = Display.Text.Substring(0, Display.Text.Length - 1);
        }

        private void Equals_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var expr = Display.Text;
                var val = EvaluateExpression(expr);
                Display.Text = val.ToString(CultureInfo.InvariantCulture);
                _lastWasEquals = true;
                _errorState = false;
            }
            catch (Exception)
            {
                Display.Text = "Error";
                _errorState = true;
                _lastWasEquals = false;
            }
        }

        // --- Simple expression evaluator using shunting-yard and RPN evaluation ---
        private double EvaluateExpression(string expr)
        {
            var tokens = Tokenize(expr);
            var rpn = ToRpn(tokens);
            var val = EvalRpn(rpn);
            if (!double.IsFinite(val))
                throw new ArithmeticException("Result is not finite");
            return val;
        }

        private enum TokenType { Number, Operator, Function, LeftParen, RightParen, Comma }

        private record Token(TokenType Type, string Value);

        private static readonly HashSet<string> Operators = new() { "+", "-", "*", "/", "^", "%" };

        private static readonly Dictionary<string, int> Precedence = new()
        {
            { "+", 2 }, { "-", 2 }, { "*", 3 }, { "/", 3 }, { "%", 3 },
            { "^", 4 }, { "u+", 4 }, { "u-", 4 }
        };

        private static readonly HashSet<string> RightAssociative = new() { "^", "u+", "u-" };

        private IEnumerable<Token> Tokenize(string s)
        {
            int i = 0;
            var expectsOperand = true;
            while (i < s.Length)
            {
                char c = s[i];
                if (char.IsWhiteSpace(c)) { i++; continue; }
                if (char.IsDigit(c) || c == '.')
                {
                    int j = i;
                    while (j < s.Length && (char.IsDigit(s[j]) || s[j] == '.')) j++;
                    yield return new Token(TokenType.Number, s.Substring(i, j - i));
                    i = j;
                    expectsOperand = false;
                    continue;
                }
                if (char.IsLetter(c))
                {
                    int j = i;
                    while (j < s.Length && char.IsLetter(s[j])) j++;
                    var name = s.Substring(i, j - i);
                    if (string.Equals(name, "pi", StringComparison.OrdinalIgnoreCase))
                        yield return new Token(TokenType.Number, Math.PI.ToString(CultureInfo.InvariantCulture));
                    else if (string.Equals(name, "e", StringComparison.OrdinalIgnoreCase))
                        yield return new Token(TokenType.Number, Math.E.ToString(CultureInfo.InvariantCulture));
                    else
                        yield return new Token(TokenType.Function, name);
                    i = j;
                    expectsOperand = false;
                    continue;
                }
                if (c == ',') { yield return new Token(TokenType.Comma, ","); i++; expectsOperand = true; continue; }
                if (c == '(') { yield return new Token(TokenType.LeftParen, "("); i++; expectsOperand = true; continue; }
                if (c == ')') { yield return new Token(TokenType.RightParen, ")"); i++; expectsOperand = false; continue; }
                // operators
                var op = c.ToString();
                if (Operators.Contains(op))
                {
                    if (expectsOperand && (op == "+" || op == "-"))
                        op = "u" + op;
                    yield return new Token(TokenType.Operator, op);
                    i++;
                    expectsOperand = true;
                    continue;
                }

                throw new FormatException($"Unexpected character: {c}");
            }
        }

        private List<Token> ToRpn(IEnumerable<Token> tokens)
        {
            var output = new List<Token>();
            var stack = new Stack<Token>();

            foreach (var t in tokens)
            {
                switch (t.Type)
                {
                    case TokenType.Number:
                        output.Add(t);
                        break;
                    case TokenType.Function:
                        stack.Push(t);
                        break;
                    case TokenType.Comma:
                        while (stack.Count > 0 && stack.Peek().Type != TokenType.LeftParen)
                            output.Add(stack.Pop());
                        break;
                    case TokenType.Operator:
                        if (t.Value == "u+" || t.Value == "u-")
                        {
                            stack.Push(t);
                            break;
                        }
                        while (stack.Count > 0 && stack.Peek().Type == TokenType.Operator)
                        {
                            var o2 = stack.Peek().Value;
                            var p1 = Precedence[t.Value];
                            var p2 = Precedence[o2];
                            if ((RightAssociative.Contains(t.Value) && p1 < p2) || (!RightAssociative.Contains(t.Value) && p1 <= p2))
                                output.Add(stack.Pop());
                            else break;
                        }
                        stack.Push(t);
                        break;
                    case TokenType.LeftParen:
                        stack.Push(t);
                        break;
                    case TokenType.RightParen:
                        while (stack.Count > 0 && stack.Peek().Type != TokenType.LeftParen)
                            output.Add(stack.Pop());
                        if (stack.Count == 0) throw new Exception("Mismatched parentheses");
                        stack.Pop(); // pop left paren
                        if (stack.Count > 0 && stack.Peek().Type == TokenType.Function)
                            output.Add(stack.Pop());
                        break;
                }
            }
            while (stack.Count > 0)
            {
                var t = stack.Pop();
                if (t.Type == TokenType.LeftParen || t.Type == TokenType.RightParen) throw new Exception("Mismatched parentheses");
                output.Add(t);
            }
            return output;
        }

        private double EvalRpn(List<Token> rpn)
        {
            var st = new Stack<double>();
            foreach (var t in rpn)
            {
                if (t.Type == TokenType.Number)
                {
                    st.Push(double.Parse(t.Value, CultureInfo.InvariantCulture));
                }
                else if (t.Type == TokenType.Operator)
                {
                    if (t.Value == "u+" || t.Value == "u-")
                    {
                        if (st.Count < 1) throw new Exception("Insufficient values");
                        var value = st.Pop();
                        st.Push(t.Value == "u-" ? -value : value);
                    }
                    else
                    {
                        if (st.Count < 2) throw new Exception("Insufficient values");
                        var b = st.Pop();
                        var a = st.Pop();
                        st.Push(ApplyOperator(t.Value, a, b));
                    }
                }
                else if (t.Type == TokenType.Function)
                {
                    // functions take one or two args (pow)
                    if (string.Equals(t.Value, "pow", StringComparison.OrdinalIgnoreCase))
                    {
                        if (st.Count < 2) throw new Exception("Insufficient values for pow");
                        var exp = st.Pop();
                        var bas = st.Pop();
                        st.Push(Math.Pow(bas, exp));
                    }
                    else
                    {
                        if (st.Count < 1) throw new Exception("Insufficient values for function");
                        var v = st.Pop();
                        st.Push(ApplyFunction(t.Value, v));
                    }
                }
            }
            if (st.Count != 1) throw new Exception("Invalid expression");
            return st.Pop();
        }

        private static double ApplyOperator(string op, double a, double b)
        {
            return op switch
            {
                "+" => a + b,
                "-" => a - b,
                "*" => a * b,
                "/" => a / b,
                "%" => a % b,
                "^" => Math.Pow(a, b),
                _ => throw new Exception("Unknown operator")
            };
        }

        private double ApplyFunction(string name, double x)
        {
            double toRadians(double value) => _useDegrees ? value * Math.PI / 180 : value;
            double fromRadians(double value) => _useDegrees ? value * 180 / Math.PI : value;

            return name.ToLowerInvariant() switch
            {
                "sin" => Math.Sin(toRadians(x)),
                "cos" => Math.Cos(toRadians(x)),
                "tan" => Math.Tan(toRadians(x)),
                "asin" => fromRadians(Math.Asin(x)),
                "acos" => fromRadians(Math.Acos(x)),
                "atan" => fromRadians(Math.Atan(x)),
                "sinh" => Math.Sinh(x),
                "cosh" => Math.Cosh(x),
                "tanh" => Math.Tanh(x),
                "exp" => Math.Exp(x),
                "log" => Math.Log10(x),
                "ln" => Math.Log(x),
                "sqrt" => Math.Sqrt(x),
                "abs" => Math.Abs(x),
                "fact" => Factorial(x),
                _ => throw new Exception("Unknown function")
            };
        }

        private static double Factorial(double value)
        {
            if (value < 0 || value != Math.Truncate(value) || value > 170)
                throw new ArithmeticException("Factorial requires an integer from 0 to 170");

            var result = 1d;
            for (var i = 2; i <= (int)value; i++)
                result *= i;
            return result;
        }
    }
}
