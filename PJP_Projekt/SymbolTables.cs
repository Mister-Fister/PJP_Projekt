using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;

namespace PJP_Projekt
{
    // pamäť ktorá premenná je aký typ
    public class SymbolTables
    {   
         // napr. "a" → Int, "b" → Float
        private Dictionary<string, Type> memory = new Dictionary<string, Type>();

        public void Add(IToken variable, Type type)
        {
            var name = variable.Text.Trim();

            if (memory.ContainsKey(name))
            {
                Errors.ReportError(variable, $"Variable '{name}' was already declared");
            }
            else
            {
                memory.Add(name, type);
            }
        }

        // indexer — umožňuje písať symbolTable[token]
        // get = zisti typ premennej
        // set = zmeň typ premennej
        public Type this[IToken variable]
        {
            get
            {
                var name = variable.Text.Trim();
                if (memory.ContainsKey(name))
                {
                    return memory[name];
                }
                else
                {
                    Errors.ReportError(variable, $"Variable '{name}' was not declared.");
                    return Type.Error;
                }
            }
            set
            {
                var name = variable.Text.Trim();
                memory[name] = value;
            }
        }
    }
}
