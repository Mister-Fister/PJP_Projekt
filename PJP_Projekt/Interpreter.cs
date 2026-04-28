using System;
using System.Collections.Generic;

namespace PJP_Projekt
{
    // Interpreter načíta vygenerované stack-based inštrukcie a vykoná ich.
    // Hlavná pamäť je stack — hodnoty sa kladú (push) a berú (pop) zo stacku.
    // Premenné sú uložené v slovníku memory (meno → hodnota).
    // Pre skoky (jmp, fjmp) sa najprv zmapujú všetky labely na čísla riadkov.
    // Vykonávanie beží v cykle — instruction pointer (ip) ukazuje na aktuálnu inštrukciu.
    public class Interpreter
    {
        // zásobník — hlavná pamäť pri výpočtoch
        private Stack<object> _stack = new Stack<object>();

        // premenné: meno → hodnota
        private Dictionary<string, object> _memory = new Dictionary<string, object>();

        // labely: číslo labelu → index riadku v _instructions
        private Dictionary<int, int> _labels = new Dictionary<int, int>();

        // zoznam inštrukcií — každá inštrukcia je pole slov
        // napr. "push I 42" → ["push", "I", "42"]
        private List<string[]> _instructions = new List<string[]>();

        public Interpreter(string code)
        {
            // rozbi kód na riadky a každý riadok na slová
            foreach (var line in code.Split('\n'))
            {
                var trimmed = line.Trim();

                // preskočí prázdne riadky
                if (string.IsNullOrEmpty(trimmed)) continue;

                _instructions.Add(trimmed.Split(' '));
            }

            // zmapuj všetky labely na indexy riadkov
            // musíme to urobiť pred spustením aby skoky fungovali
            MapLabels();
        }

        // prejde všetky inštrukcie a zapamätá si kde je každý label
        // napr. "label 3" na riadku 10 → _labels[3] = 10
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

        // spustí všetky inštrukcie
        public void Run()
        {
            // instruction pointer — index aktuálnej inštrukcie
            int ip = 0;

            while (ip < _instructions.Count)
            {
                var instr = _instructions[ip];
                var op = instr[0]; // názov inštrukcie napr. "push", "add", "save"

                switch (op)
                {
                    // ── PUSH ──────────────────────────────────────────
                    // "push I 42" → vlož int 42 na stack
                    // "push F 3.14" → vlož float 3.14 na stack
                    // "push B true" → vlož bool true na stack
                    // "push S "ahoj"" → vlož string "ahoj" na stack
                    case "push":
                        {
                            string type = instr[1];

                            // string môže obsahovať medzery napr. "push S "a b c""
                            // preto spájame zvyšok poľa
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
                                    // odstráň úvodzovky zo stringu
                                    _stack.Push(value.Trim('"'));
                                    break;
                            }
                            break;
                        }

                    // ── POP ───────────────────────────────────────────
                    // vezme hodnotu zo stacku a zahodí ju
                    case "pop":
                        _stack.Pop();
                        break;

                    // ── LOAD ──────────────────────────────────────────
                    // "load a" → načítaj hodnotu premennej a na stack
                    case "load":
                        {
                            string name = instr[1];
                            _stack.Push(_memory[name]);
                            break;
                        }

                    // ── SAVE ──────────────────────────────────────────
                    // "save a" → vezmi vrchol stacku a ulož do premennej a
                    case "save":
                        {
                            string name = instr[1];
                            _memory[name] = _stack.Pop();
                            break;
                        }

                    // ── ADD ───────────────────────────────────────────
                    // "add I" → vezmi dve int hodnoty, sčítaj, vlož výsledok
                    // "add F" → to isté pre float
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

                    // ── SUB ───────────────────────────────────────────
                    // "sub I/F" → odčítanie
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

                    // ── MUL ───────────────────────────────────────────
                    // "mul I/F" → násobenie
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

                    // ── DIV ───────────────────────────────────────────
                    // "div I/F" → delenie
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

                    // ── MOD ───────────────────────────────────────────
                    // "mod" → modulo — iba pre int
                    case "mod":
                        {
                            var right = _stack.Pop();
                            var left = _stack.Pop();
                            _stack.Push(Convert.ToInt32(left) % Convert.ToInt32(right));
                            break;
                        }

                    // ── UMINUS ────────────────────────────────────────
                    // "uminus I/F" → unárny mínus
                    case "uminus":
                        {
                            var val = _stack.Pop();
                            if (instr[1] == "F")
                                _stack.Push(-Convert.ToDouble(val));
                            else
                                _stack.Push(-Convert.ToInt32(val));
                            break;
                        }

                    // ── CONCAT ────────────────────────────────────────
                    // "concat" → spojenie dvoch stringov
                    case "concat":
                        {
                            var right = _stack.Pop().ToString();
                            var left = _stack.Pop().ToString();
                            _stack.Push(left + right);
                            break;
                        }

                    // ── AND ───────────────────────────────────────────
                    // "and" → logické &&
                    case "and":
                        {
                            var right = (bool)_stack.Pop();
                            var left = (bool)_stack.Pop();
                            _stack.Push(left && right);
                            break;
                        }

                    // ── OR ────────────────────────────────────────────
                    // "or" → logické ||
                    case "or":
                        {
                            var right = (bool)_stack.Pop();
                            var left = (bool)_stack.Pop();
                            _stack.Push(left || right);
                            break;
                        }

                    // ── NOT ───────────────────────────────────────────
                    // "not" → logická negácia
                    case "not":
                        {
                            var val = (bool)_stack.Pop();
                            _stack.Push(!val);
                            break;
                        }

                    // ── GT ────────────────────────────────────────────
                    // "gt I/F" → väčší ako >
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

                    // ── LT ────────────────────────────────────────────
                    // "lt I/F" → menší ako 
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

                    // ── EQ ────────────────────────────────────────────
                    // "eq I/F/S" → porovnanie ==
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

                    // ── ITOF ──────────────────────────────────────────
                    // "itof" → konvertuj int na float
                    case "itof":
                        {
                            var val = Convert.ToInt32(_stack.Pop());
                            _stack.Push((double)val);
                            break;
                        }

                    // ── LABEL ─────────────────────────────────────────
                    // "label 3" → iba značka miesta, nič nevykoná
                    case "label":
                        break;

                    // ── JMP ───────────────────────────────────────────
                    // "jmp 3" → skoč na label 3
                    case "jmp":
                        {
                            int labelNum = int.Parse(instr[1]);
                            // nastav ip na index labelu — na konci cyklu sa ip++
                            ip = _labels[labelNum];
                            break;
                        }

                    // ── FJMP ──────────────────────────────────────────
                    // "fjmp 3" → ak false na stacku, skoč na label 3
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

                    // ── PRINT ─────────────────────────────────────────
                    // "print 3" → vezmi 3 hodnoty zo stacku a vypíš ich
                    // hodnoty sú na stacku v opačnom poradí → musíme obrátiť
                    case "print":
                        {
                            int count = int.Parse(instr[1]);
                            var values = new object[count];

                            // pop v opačnom poradí
                            for (int i = count - 1; i >= 0; i--)
                            {
                                values[i] = _stack.Pop();
                            }

                            // vypíš všetky hodnoty za sebou
                            foreach (var val in values)
                            {
                                if (val is double d)
                                {
                                    // float vypíše vždy s desatinnou časťou
                                    // napr. 10 → "10.0", 1 → "1.0"
                                    string formatted = d.ToString(
                                        System.Globalization.CultureInfo.InvariantCulture);
                                    if (!formatted.Contains('.'))
                                        formatted += ".0";
                                    Console.Write(formatted);
                                }
                                else if (val is bool b)
                                    // C# píše True/False — my chceme true/false
                                    Console.Write(b ? "true" : "false");
                                else
                                    Console.Write(val);
                            }

                            // po poslednej hodnote nový riadok
                            Console.WriteLine();
                            break;
                        }

                    // ── READ ──────────────────────────────────────────
                    // "read I/F/S/B" → načítaj hodnotu zo vstupu na stack
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

                // posuň sa na ďalšiu inštrukciu
                ip++;
            }
        }
    }
}