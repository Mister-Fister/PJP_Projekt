using System;
using System.Collections.Generic;
using System.Text;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;

namespace PJP_Projekt
{
    // prechádza parse tree a generuje stack-based inštrukcie
    public class CodeGenerator : PJP_ProjektBaseVisitor<string>
    {
        private TypeChecker _typeChecker;
        private int _labelCounter = 0;
        private List<string> _code = new List<string>();

        public CodeGenerator(TypeChecker typeChecker)
        {
            _typeChecker = typeChecker;
        }

        // spoj inštrukcie do stringu
        public string GetCode()
        {
            return string.Join("\n", _code);
        }

        // unikátne číslo pre label
        private int NewLabel()
        {
            return _labelCounter++;
        }

        // pridá inštrukciu
        private void Emit(string instruction)
        {
            _code.Add(instruction);
        }

        // Type → I, F, S, B
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

        // program
        public override string VisitProgram(PJP_ProjektParser.ProgramContext context)
        {
            foreach (var stmt in context.statement())
                Visit(stmt);
            return GetCode();
        }

        // deklarácia — push defaultná hodnota, save
        public override string VisitDeclaration(PJP_ProjektParser.DeclarationContext context)
        {
            var typeName = context.type().GetText();

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
                Emit(defaultValue);
                Emit($"save {id.GetText()}");
            }

            return "";
        }

        // expression statement — vyhodnoť výraz, zahoď výsledok
        public override string VisitExpressionStatement(PJP_ProjektParser.ExpressionStatementContext context)
        {
            Visit(context.expression());
            Emit("pop");
            return "";
        }

        // read — načítaj zo vstupu a ulož do premennej
        public override string VisitReadStatement(PJP_ProjektParser.ReadStatementContext context)
        {
            foreach (var id in context.ID())
            {
                var type = _typeChecker.SymbolTables[id.Symbol];
                Emit($"read {TypeToChar(type)}");
                Emit($"save {id.GetText()}");
            }
            return "";
        }

        // write — vyhodnoť výrazy a vypíš
        public override string VisitWriteStatement(PJP_ProjektParser.WriteStatementContext context)
        {
            var expressions = context.expression();

            foreach (var expr in expressions)
                Visit(expr);

            Emit($"print {expressions.Length}");
            return "";
        }

        // block — prejdi statementy
        public override string VisitBlock(PJP_ProjektParser.BlockContext context)
        {
            foreach (var stmt in context.statement())
                Visit(stmt);
            return "";
        }

        // if — fjmp na else, jmp na end
        public override string VisitIfStatement(PJP_ProjektParser.IfStatementContext context)
        {
            int elseLabel = NewLabel();
            int endLabel = NewLabel();

            Visit(context.expression());
            Emit($"fjmp {elseLabel}");
            Visit(context.statement(0));
            Emit($"jmp {endLabel}");
            Emit($"label {elseLabel}");

            if (context.statement().Length > 1)
                Visit(context.statement(1));

            Emit($"label {endLabel}");
            return "";
        }

        // while — label start, fjmp end, jmp start
        public override string VisitWhileStatement(PJP_ProjektParser.WhileStatementContext context)
        {
            int startLabel = NewLabel();
            int endLabel = NewLabel();

            Emit($"label {startLabel}");
            Visit(context.expression());
            Emit($"fjmp {endLabel}");
            Visit(context.statement());
            Emit($"jmp {startLabel}");
            Emit($"label {endLabel}");
            return "";
        }

        // výrazy
        public override string VisitExpression(PJP_ProjektParser.ExpressionContext context)
        {
            // INT literál
            if (context.INT() != null)
            {
                Emit($"push I {context.INT().GetText()}");
                return "";
            }

            // FLOAT literál
            if (context.FLOAT() != null)
            {
                Emit($"push F {context.FLOAT().GetText()}");
                return "";
            }

            // BOOL literál
            if (context.BOOL() != null)
            {
                Emit($"push B {context.BOOL().GetText()}");
                return "";
            }

            // STRING literál
            if (context.STRING() != null)
            {
                Emit($"push S {context.STRING().GetText()}");
                return "";
            }

            // premenná
            if (context.ID() != null && context.GetChild(1)?.GetText() != "=")
            {
                Emit($"load {context.ID().GetText()}");
                return "";
            }

            // priradenie — save + load (pre reťazové priradenia)
            if (context.ID() != null && context.GetChild(1)?.GetText() == "=")
            {
                var varType = _typeChecker.SymbolTables[context.ID().Symbol];
                var rightType = _typeChecker.Types.Get(context.expression(0));

                Visit(context.expression(0));

                if (varType == Type.Float && rightType == Type.Int)
                    Emit("itof");

                Emit($"save {context.ID().GetText()}");
                Emit($"load {context.ID().GetText()}");
                return "";
            }

            // aritmetické operátory + - * /
            if (context.GetChild(1)?.GetText() is "+" or "-" or "*" or "/")
            {
                var left = _typeChecker.Types.Get(context.expression(0));
                var right = _typeChecker.Types.Get(context.expression(1));

                Visit(context.expression(0));
                if (left == Type.Int && right == Type.Float) Emit("itof");

                Visit(context.expression(1));
                if (right == Type.Int && left == Type.Float) Emit("itof");

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

            // modulo %
            if (context.GetChild(1)?.GetText() == "%")
            {
                Visit(context.expression(0));
                Visit(context.expression(1));
                Emit("mod");
                return "";
            }

            // konkatenácia .
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

            // zátvorky
            if (context.GetChild(0)?.GetText() == "(")
            {
                Visit(context.expression(0));
                return "";
            }

            return VisitChildren(context);
        }
    }
}