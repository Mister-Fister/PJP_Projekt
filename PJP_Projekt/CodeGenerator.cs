using System;
using System.Collections.Generic;
using System.Text;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;

namespace PJP_Projekt
{

    // CodeGenerator prechádza parse tree a generuje stack-based inštrukcie.
    // Dedí od PJP_ProjektBaseVisitor<string> — každý Visit vracia prázdny string,
    // inštrukcie sa ukladajú do interného zoznamu _code.
    // Používa TypeChecker aby vedel typy výrazov (napr. či treba itof konverziu).
    // Výsledný kód získaš cez GetCode() — jeden riadok = jedna inštrukcia.
    public class CodeGenerator : PJP_ProjektBaseVisitor<string>
    {
        // TypeChecker potrebujeme aby sme vedeli typy výrazov
        private TypeChecker _typeChecker;

        // počítadlo labelov — každý label musí mať unikátne číslo
        // používame pre if a while skoky
        private int _labelCounter = 0;

        // výsledný kód — každý riadok je jedna inštrukcia
        private List<string> _code = new List<string>();

        public CodeGenerator(TypeChecker typeChecker)
        {
            _typeChecker = typeChecker;
        }

        // vráti celý vygenerovaný kód ako string
        public string GetCode()
        {
            return string.Join("\n", _code);
        }

        // vygeneruje nové unikátne číslo pre label
        private int NewLabel()
        {
            return _labelCounter++;
        }

        // pridá inštrukciu do kódu
        private void Emit(string instruction)
        {
            _code.Add(instruction);
        }

        // prevedie náš Type na písmeno pre inštrukcie (I, F, S, B)
        private string TypeToChar(Type type)
        {
            return type switch
            {
                Type.Int => "I",
                Type.Float => "F",
                Type.String => "S",
                Type.Bool => "B",
                _ => "I"
            };
        }

        // ── PROGRAM ───────────────────────────────────────────
        // prejde všetky statementy
        public override string VisitProgram(PJP_ProjektParser.ProgramContext context)
        {
            foreach (var stmt in context.statement())
            {
                Visit(stmt);
            }
            return GetCode();
        }

        // ── DEKLARÁCIA ────────────────────────────────────────
        // "int a, b;" → push defaultnú hodnotu a ulož do premennej
        public override string VisitDeclaration(PJP_ProjektParser.DeclarationContext context)
        {
            var typeName = context.type().GetText();

            // defaultná hodnota podľa typu
            // int → 0, float → 0.0, bool → false, string → ""
            string defaultValue = typeName switch
            {
                "int" => "push I 0",
                "float" => "push F 0.0",
                "bool" => "push B false",
                "string" => "push S \"\"",
                _ => "push I 0"
            };

            foreach (var id in context.ID())
            {
                // vlož defaultnú hodnotu na stack
                Emit(defaultValue);
                // ulož do premennej
                Emit($"save {id.GetText()}");
            }

            return "";
        }

        // ── EXPRESSION STATEMENT ──────────────────────────────
        // "a = 5;" → vyhodnoť výraz, výsledok zahoď (pop)
        public override string VisitExpressionStatement(PJP_ProjektParser.ExpressionStatementContext context)
        {
            Visit(context.expression());
            // výsledok výrazu zahodíme — zadanie hovorí "resulting value is ignored"
            Emit("pop");
            return "";
        }

        // ── READ ──────────────────────────────────────────────
        // "read a, b;" → načítaj zo vstupu a ulož do premennej
        public override string VisitReadStatement(PJP_ProjektParser.ReadStatementContext context)
        {
            foreach (var id in context.ID())
            {
                // zisti typ premennej zo SymbolTable
                var type = _typeChecker.SymbolTables[id.Symbol];
                // načítaj hodnotu správneho typu zo vstupu
                Emit($"read {TypeToChar(type)}");
                // ulož do premennej
                Emit($"save {id.GetText()}");
            }
            return "";
        }

        // ── WRITE ─────────────────────────────────────────────
        // "write expr1, expr2;" → vyhodnoť výrazy a vypíš
        public override string VisitWriteStatement(PJP_ProjektParser.WriteStatementContext context)
        {
            var expressions = context.expression();

            // vyhodnoť každý výraz — hodnoty sa uložia na stack
            foreach (var expr in expressions)
            {
                Visit(expr);
            }

            // print n — vezme n hodnôt zo stacku a vypíše ich
            Emit($"print {expressions.Length}");
            return "";
        }

        // ── BLOCK ─────────────────────────────────────────────
        // "{ stmt1 stmt2 ... }" → prejdi všetky statementy
        public override string VisitBlock(PJP_ProjektParser.BlockContext context)
        {
            foreach (var stmt in context.statement())
            {
                Visit(stmt);
            }
            return "";
        }

        // ── IF ────────────────────────────────────────────────
        // "if (cond) stmt1 else stmt2"
        // generujeme:
        //   [podmienka]
        //   fjmp elseLabel      ← ak false, skoč na else
        //   [then vetva]
        //   jmp endLabel        ← skoč za else
        //   label elseLabel
        //   [else vetva]
        //   label endLabel
        public override string VisitIfStatement(PJP_ProjektParser.IfStatementContext context)
        {
            int elseLabel = NewLabel();
            int endLabel = NewLabel();

            // vyhodnoť podmienku — výsledok (bool) je na stacku
            Visit(context.expression());

            // ak false → skoč na else
            Emit($"fjmp {elseLabel}");

            // then vetva
            Visit(context.statement(0));

            // skoč za else
            Emit($"jmp {endLabel}");

            // else label
            Emit($"label {elseLabel}");

            // else vetva ak existuje
            if (context.statement().Length > 1)
            {
                Visit(context.statement(1));
            }

            // end label
            Emit($"label {endLabel}");

            return "";
        }

        // ── WHILE ─────────────────────────────────────────────
        // "while (cond) stmt"
        // generujeme:
        //   label startLabel    ← začiatok cyklu
        //   [podmienka]
        //   fjmp endLabel       ← ak false, skoč za cyklus
        //   [telo]
        //   jmp startLabel      ← skoč na začiatok
        //   label endLabel
        public override string VisitWhileStatement(PJP_ProjektParser.WhileStatementContext context)
        {
            int startLabel = NewLabel();
            int endLabel = NewLabel();

            // začiatok cyklu
            Emit($"label {startLabel}");

            // vyhodnoť podmienku
            Visit(context.expression());

            // ak false → skoč za cyklus
            Emit($"fjmp {endLabel}");

            // telo cyklu
            Visit(context.statement());

            // skoč na začiatok
            Emit($"jmp {startLabel}");

            // koniec cyklu
            Emit($"label {endLabel}");

            return "";
        }

        // ── VÝRAZY ────────────────────────────────────────────
        public override string VisitExpression(PJP_ProjektParser.ExpressionContext context)
        {
            // INT literál napr. 42
            if (context.INT() != null)
            {
                Emit($"push I {context.INT().GetText()}");
                return "";
            }

            // FLOAT literál napr. 3.14
            if (context.FLOAT() != null)
            {
                Emit($"push F {context.FLOAT().GetText()}");
                return "";
            }

            // BOOL literál napr. true
            if (context.BOOL() != null)
            {
                Emit($"push B {context.BOOL().GetText()}");
                return "";
            }

            // STRING literál napr. "ahoj"
            if (context.STRING() != null)
            {
                Emit($"push S {context.STRING().GetText()}");
                return "";
            }

            // premenná napr. a
            if (context.ID() != null && context.GetChild(1)?.GetText() != "=")
            {
                // načítaj hodnotu premennej na stack
                Emit($"load {context.ID().GetText()}");
                return "";
            }

            // priradenie napr. "a = 5"
            if (context.ID() != null && context.GetChild(1)?.GetText() == "=")
            {
                var varType = _typeChecker.SymbolTables[context.ID().Symbol];
                var rightType = _typeChecker.Types.Get(context.expression(0));

                // vyhodnoť pravú stranu
                Visit(context.expression(0));

                // ak premenná je float ale výraz je int → konvertuj
                if (varType == Type.Float && rightType == Type.Int)
                {
                    Emit("itof");
                }

                // duplicituj hodnotu na stacku — save zoberie vrchol
                // ale výsledok priradenia musí zostať na stacku
                // (pre reťazové priradenia napr. a = b = 5)
                Emit($"save {context.ID().GetText()}");
                Emit($"load {context.ID().GetText()}");
                return "";
            }

            // aritmetické operátory: +, -, *, /
            if (context.GetChild(1)?.GetText() is "+" or "-" or "*" or "/")
            {
                var left = _typeChecker.Types.Get(context.expression(0));
                var right = _typeChecker.Types.Get(context.expression(1));

                Visit(context.expression(0));

                // ak ľavá je int ale pravá float → konvertuj ľavú
                if (left == Type.Int && right == Type.Float)
                    Emit("itof");

                Visit(context.expression(1));

                // ak pravá je int ale ľavá float → konvertuj pravú
                if (right == Type.Int && left == Type.Float)
                    Emit("itof");

                // výsledný typ
                var resultType = (left == Type.Float || right == Type.Float) ? "F" : "I";

                string op = context.GetChild(1).GetText() switch
                {
                    "+" => "add",
                    "-" => "sub",
                    "*" => "mul",
                    "/" => "div",
                    _ => "add"
                };

                Emit($"{op} {resultType}");
                return "";
            }

            // modulo: %
            if (context.GetChild(1)?.GetText() == "%")
            {
                Visit(context.expression(0));
                Visit(context.expression(1));
                Emit("mod");
                return "";
            }

            // konkatenácia reťazcov: .
            if (context.GetChild(1)?.GetText() == ".")
            {
                Visit(context.expression(0));
                Visit(context.expression(1));
                Emit("concat");
                return "";
            }

            // logické &&
            if (context.GetChild(1)?.GetText() == "&&")
            {
                Visit(context.expression(0));
                Visit(context.expression(1));
                Emit("and");
                return "";
            }

            // logické ||
            if (context.GetChild(1)?.GetText() == "||")
            {
                Visit(context.expression(0));
                Visit(context.expression(1));
                Emit("or");
                return "";
            }

            // logické !
            if (context.GetChild(0)?.GetText() == "!")
            {
                Visit(context.expression(0));
                Emit("not");
                return "";
            }

            // relačné < >
            if (context.GetChild(1)?.GetText() is "<" or ">")
            {
                var left = _typeChecker.Types.Get(context.expression(0));
                var right = _typeChecker.Types.Get(context.expression(1));

                Visit(context.expression(0));
                if (left == Type.Int && right == Type.Float) Emit("itof");

                Visit(context.expression(1));
                if (right == Type.Int && left == Type.Float) Emit("itof");

                var resultType = (left == Type.Float || right == Type.Float) ? "F" : "I";

                string op = context.GetChild(1).GetText() == "<" ? "lt" : "gt";
                Emit($"{op} {resultType}");
                return "";
            }

            // porovnanie == !=
            if (context.GetChild(1)?.GetText() is "==" or "!=")
            {
                var left = _typeChecker.Types.Get(context.expression(0));
                var right = _typeChecker.Types.Get(context.expression(1));

                Visit(context.expression(0));
                if (left == Type.Int && right == Type.Float) Emit("itof");

                Visit(context.expression(1));
                if (right == Type.Int && left == Type.Float) Emit("itof");

                var resultType = (left == Type.Float || right == Type.Float) ? "F" :
                                 (left == Type.String) ? "S" : "I";

                Emit($"eq {resultType}");

                // != je eq + not
                if (context.GetChild(1).GetText() == "!=")
                    Emit("not");

                return "";
            }

            // unárny mínus
            if (context.GetChild(0)?.GetText() == "-" && context.expression().Length == 1)
            {
                var operandType = _typeChecker.Types.Get(context.expression(0));
                Visit(context.expression(0));
                Emit($"uminus {TypeToChar(operandType)}");
                return "";
            }

            // zátvorky: ( expression )
            if (context.GetChild(0)?.GetText() == "(")
            {
                Visit(context.expression(0));
                return "";
            }

            return VisitChildren(context);
        }
    }
}