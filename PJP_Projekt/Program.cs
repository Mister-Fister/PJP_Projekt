using System;
using System.IO;
using Antlr4.Runtime;

namespace PJP_Projekt
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var input = File.ReadAllText("input.txt");

            // lexer a parser
            var inputStream = new AntlrInputStream(input);
            var lexer = new PJP_ProjektLexer(inputStream);
            var tokenStream = new CommonTokenStream(lexer);
            var parser = new PJP_ProjektParser(tokenStream);
            var tree = parser.program();

            // syntax chyby
            if (parser.NumberOfSyntaxErrors > 0)
            {
                Console.WriteLine($"{parser.NumberOfSyntaxErrors} syntax error(s) found.");
                return;
            }

            // type checking
            var typeChecker = new TypeChecker();
            typeChecker.Visit(tree);

            if (Errors.NumberOfErrors > 0)
            {
                Errors.PrintAndClearErrors();
                return;
            }

            // generovanie kódu
            var codeGenerator = new CodeGenerator(typeChecker);
            var code = codeGenerator.VisitProgram(tree);
            File.WriteAllText("output.txt", code);

            // interpreter
            var interpreter = new Interpreter(code);
            interpreter.Run();
        }
    }
}