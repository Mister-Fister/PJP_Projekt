using System;
using System.IO;
using Antlr4.Runtime;

namespace PJP_Projekt
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // načítaj zdrojový kód zo súboru
            var input = File.ReadAllText("input.txt");

            // ── KROK 2: LEXER A PARSER ────────────────────────
            // ANTLR potrebuje svoj vlastný typ vstupu
            var inputStream = new AntlrInputStream(input);

            // lexer rozozná tokeny (INT, FLOAT, ID, ...)
            var lexer = new PJP_ProjektLexer(inputStream);

            // zabalí tokeny do streamu pre parser
            var tokenStream = new CommonTokenStream(lexer);

            // parser zostaví strom podľa gramatiky
            var parser = new PJP_ProjektParser(tokenStream);

            // spusti parsovanie od pravidla program
            var tree = parser.program();

            // ── KROK 3: KONTROLA SYNTAX CHÝB ─────────────────
            // ak nastali syntax chyby, ANTLR ich vypíše sám
            // my iba zastavíme výpočet
            if (parser.NumberOfSyntaxErrors > 0)
            {
                Console.WriteLine($"{parser.NumberOfSyntaxErrors} syntax error(s) found.");
                return;
            }

            // ── KROK 4: TYPE CHECKING ─────────────────────────
            // prejde strom a skontroluje typy
            var typeChecker = new TypeChecker();
            typeChecker.Visit(tree);

            // ak nastali type chyby, vypíš ich a zastav
            if (Errors.NumberOfErrors > 0)
            {
                Errors.PrintAndClearErrors();
                return;
            }

            // ── KROK 5: GENEROVANIE KÓDU ──────────────────────
            // prejde strom a vygeneruje stack-based inštrukcie
            var codeGenerator = new CodeGenerator(typeChecker);
            var code = codeGenerator.VisitProgram(tree);

            // voliteľné — ulož vygenerovaný kód do súboru pre debugging
            File.WriteAllText("output.txt", code);

            // ── KROK 6: INTERPRETER ───────────────────────────
            // načítaj vygenerovaný kód a spusti ho
            var interpreter = new Interpreter(code);
            interpreter.Run();
        }
    }
}