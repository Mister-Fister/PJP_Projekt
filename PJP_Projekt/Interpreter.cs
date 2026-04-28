using System;
using System.Collections.Generic;

namespace PJP_Projekt
{
    // načíta inštrukcie a vykoná ich na stacku
    public class Interpreter
    {
        // zásobník
        private Stack<object> _stack = new Stack<object>();

        // premenné: meno → hodnota
        private Dictionary<string, object> _memory = new Dictionary<string, object>();

        // labely: číslo → index riadku
        private Dictionary<int, int> _labels = new Dictionary<int, int>();

        // inštrukcie: každá je pole slov
        private List<string[]> _instructions = new List<string[]>();

        public Interpreter(string code)
        {
            foreach (var line in code.Split('\n'))
            {
                var trimmed = line.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;
                _instructions.Add(trimmed.Split(' '));
            }
            MapLabels();
        }

        // zapamätaj pozície labelov pred spustením
        private void MapLabels()
        {
            for (int i = 0; i < _instructions.Count; i++)
            {
                if (_instructions[i][0] == "label")
                {
                    int labelNum = int.Parse(_instructions[i][1]);
                    _labels[labelNum] = i;
                }
            }
        }

        public void Run()
        {
            int ip = 0;

            while (ip < _instructions.Count)
            {
                var instr = _instructions[ip];
                var op = instr[0];

                switch (op)
                {
                    // push — vlož hodnotu na stack
                    case "push":
                        {
                            string type = instr[1];
                            // string môže obsahovať medzery
                            string value = string.Join(" ", instr[2..]);

                            switch (type)
                            {
                                case "I":
                                    _stack.Push(int.Parse(value));
                                    break;
                                case "F":
                                    _stack.Push(double.Parse(value,
                                        System.Globalization.CultureInfo.InvariantCulture));
                                    break;
                                case "B":
                                    _stack.Push(bool.Parse(value));
                                    break;
                                case "S":
                                    // odstráň úvodzovky
                                    _stack.Push(value.Trim('"'));
                                    break;
                            }
                            break;
                        }

                    // pop — zahoď vrchol stacku
                    case "pop":
                        _stack.Pop();
                        break;

                    // load — načítaj premennú na stack
                    case "load":
                        {
                            string name = instr[1];
                            _stack.Push(_memory[name]);
                            break;
                        }

                    // save — ulož vrchol stacku do premennej
                    case "save":
                        {
                            string name = instr[1];
                            _memory[name] = _stack.Pop();
                            break;
                        }

                    // add
                    case "add":
                        {
                            var right = _stack.Pop();
                            var left = _stack.Pop();
                            if (instr[1] == "F")
                                _stack.Push(Convert.ToDouble(left) + Convert.ToDouble(right));
                            else
                                _stack.Push(Convert.ToInt32(left) + Convert.ToInt32(right));
                            break;
                        }

                    // sub
                    case "sub":
                        {
                            var right = _stack.Pop();
                            var left = _stack.Pop();
                            if (instr[1] == "F")
                                _stack.Push(Convert.ToDouble(left) - Convert.ToDouble(right));
                            else
                                _stack.Push(Convert.ToInt32(left) - Convert.ToInt32(right));
                            break;
                        }

                    // mul
                    case "mul":
                        {
                            var right = _stack.Pop();
                            var left = _stack.Pop();
                            if (instr[1] == "F")
                                _stack.Push(Convert.ToDouble(left) * Convert.ToDouble(right));
                            else
                                _stack.Push(Convert.ToInt32(left) * Convert.ToInt32(right));
                            break;
                        }

                    // div
                    case "div":
                        {
                            var right = _stack.Pop();
                            var left = _stack.Pop();
                            if (instr[1] == "F")
                                _stack.Push(Convert.ToDouble(left) / Convert.ToDouble(right));
                            else
                                _stack.Push(Convert.ToInt32(left) / Convert.ToInt32(right));
                            break;
                        }

                    // mod — iba int
                    case "mod":
                        {
                            var right = _stack.Pop();
                            var left = _stack.Pop();
                            _stack.Push(Convert.ToInt32(left) % Convert.ToInt32(right));
                            break;
                        }

                    // uminus — unárny mínus
                    case "uminus":
                        {
                            var val = _stack.Pop();
                            if (instr[1] == "F")
                                _stack.Push(-Convert.ToDouble(val));
                            else
                                _stack.Push(-Convert.ToInt32(val));
                            break;
                        }

                    // concat — spojenie stringov
                    case "concat":
                        {
                            var right = _stack.Pop().ToString();
                            var left = _stack.Pop().ToString();
                            _stack.Push(left + right);
                            break;
                        }

                    // and
                    case "and":
                        {
                            var right = (bool)_stack.Pop();
                            var left = (bool)_stack.Pop();
                            _stack.Push(left && right);
                            break;
                        }

                    // or
                    case "or":
                        {
                            var right = (bool)_stack.Pop();
                            var left = (bool)_stack.Pop();
                            _stack.Push(left || right);
                            break;
                        }

                    // not
                    case "not":
                        {
                            var val = (bool)_stack.Pop();
                            _stack.Push(!val);
                            break;
                        }

                    // gt — väčší ako
                    case "gt":
                        {
                            var right = _stack.Pop();
                            var left = _stack.Pop();
                            if (instr[1] == "F")
                                _stack.Push(Convert.ToDouble(left) > Convert.ToDouble(right));
                            else
                                _stack.Push(Convert.ToInt32(left) > Convert.ToInt32(right));
                            break;
                        }

                    // lt — menší ako
                    case "lt":
                        {
                            var right = _stack.Pop();
                            var left = _stack.Pop();
                            if (instr[1] == "F")
                                _stack.Push(Convert.ToDouble(left) < Convert.ToDouble(right));
                            else
                                _stack.Push(Convert.ToInt32(left) < Convert.ToInt32(right));
                            break;
                        }

                    // eq — porovnanie ==
                    case "eq":
                        {
                            var right = _stack.Pop();
                            var left = _stack.Pop();
                            if (instr[1] == "F")
                                _stack.Push(Convert.ToDouble(left) == Convert.ToDouble(right));
                            else if (instr[1] == "S")
                                _stack.Push(left.ToString() == right.ToString());
                            else
                                _stack.Push(Convert.ToInt32(left) == Convert.ToInt32(right));
                            break;
                        }

                    // itof — int → float
                    case "itof":
                        {
                            var val = Convert.ToInt32(_stack.Pop());
                            _stack.Push((double)val);
                            break;
                        }

                    // label — iba značka miesta
                    case "label":
                        break;

                    // jmp — skoč na label
                    case "jmp":
                        {
                            int labelNum = int.Parse(instr[1]);
                            ip = _labels[labelNum];
                            break;
                        }

                    // fjmp — skoč na label ak false
                    case "fjmp":
                        {
                            var condition = (bool)_stack.Pop();
                            if (!condition)
                            {
                                int labelNum = int.Parse(instr[1]);
                                ip = _labels[labelNum];
                            }
                            break;
                        }

                    // print — vypíš n hodnôt zo stacku
                    case "print":
                        {
                            int count = int.Parse(instr[1]);
                            var values = new object[count];

                            // pop v opačnom poradí
                            for (int i = count - 1; i >= 0; i--)
                                values[i] = _stack.Pop();

                            foreach (var val in values)
                            {
                                if (val is double d)
                                {
                                    // float vždy s desatinnou časťou
                                    string formatted = d.ToString(
                                        System.Globalization.CultureInfo.InvariantCulture);
                                    if (!formatted.Contains('.'))
                                        formatted += ".0";
                                    Console.Write(formatted);
                                }
                                else if (val is bool b)
                                    // lowercase true/false
                                    Console.Write(b ? "true" : "false");
                                else
                                    Console.Write(val);
                            }

                            Console.WriteLine();
                            break;
                        }

                    // read — načítaj zo vstupu na stack
                    case "read":
                        {
                            string line = Console.ReadLine() ?? "";
                            switch (instr[1])
                            {
                                case "I":
                                    _stack.Push(int.Parse(line));
                                    break;
                                case "F":
                                    _stack.Push(double.Parse(line,
                                        System.Globalization.CultureInfo.InvariantCulture));
                                    break;
                                case "B":
                                    _stack.Push(bool.Parse(line));
                                    break;
                                case "S":
                                    _stack.Push(line);
                                    break;
                            }
                            break;
                        }
                }

                ip++;
            }
        }
    }
}