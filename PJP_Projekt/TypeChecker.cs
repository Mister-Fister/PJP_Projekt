using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;

namespace PJP_Projekt
{
    //prechádza strom a kontroluje typy
    public class TypeChecker : PJP_ProjektBaseVisitor<Type>
    {
        // pamäť premenných: "a" → Int
        public SymbolTables SymbolTables { get; } = new SymbolTables();

        // typ každého uzlu stromu
        // napr. uzol "2 + 3" → Int
        public ParseTreeProperty<Type> Types { get; } = new ParseTreeProperty<Type>();

        // keď narazíme na celé číslo napr. 42
        // → typ je Int
        public override Type VisitExpression(PJP_ProjektParser.ExpressionContext context)
        {
            if (context.INT() != null)
            {
                Types.Put(context, Type.Int);
                return Type.Int;
            }

            if (context.FLOAT() != null)
            {
                Types.Put(context, Type.Float);
                return Type.Float;
            }

            if (context.BOOL() != null)
            {
                Types.Put(context, Type.Bool);
                return Type.Bool;
            }

            if (context.STRING() != null)
            {
                Types.Put(context, Type.String);
                return Type.String;
            }

            // premenná napr. a, myVar
            if (context.ID() != null && context.GetChild(1)?.GetText() != "=")
            {
                // zisti typ premennej zo SymbolTable
                var type = SymbolTables[context.ID().Symbol];
                //uzol stromu má typ ..
                Types.Put(context, type);
                return type;
            }

            // priradenie napr. "a = 5"
            if (context.ID() != null && context.GetChild(1)?.GetText() == "=")
            {
                // zisti typ pravej strany napr. typ "5" → Int
                var rightType = Visit(context.expression(0));

                // zisti typ premennej zo SymbolTable napr. "a" → Int
                var varType = SymbolTables[context.ID().Symbol];

                // ak nastala chyba na pravej strane, propaguj chybu
                if (rightType == Type.Error || varType == Type.Error)
                {
                    Types.Put(context, Type.Error);
                    return Type.Error;
                }

                // špeciálny prípad — int sa môže automaticky konvertovať na float
                // napr. float a; a = 5; → OK, 5 sa konvertuje na 5.0
                if (varType == Type.Float && rightType == Type.Int)
                {
                    Types.Put(context, Type.Float);
                    return Type.Float;
                }

                // typy sa musia zhodovať
                // napr. int a; a = 3.14; → CHYBA
                if (varType != rightType)
                {
                    Errors.ReportError(context.ID().Symbol,
                        $"Cannot assign {rightType} to variable '{context.ID().GetText()}' of type {varType}.");
                    Types.Put(context, Type.Error);
                    return Type.Error;
                }

                Types.Put(context, varType);
                return varType;
            }

            // aritmetické operátory: +, -, *, /
            // I × I → I alebo F × F → F (s automatickou konverziou int → float)
            if (context.GetChild(1)?.GetText() is "+" or "-" or "*" or "/")
            {
                var left = Visit(context.expression(0));
                var right = Visit(context.expression(1));

                if (left == Type.Error || right == Type.Error)
                {
                    Types.Put(context, Type.Error);
                    return Type.Error;
                }

                // obidva musia byť int alebo float
                if ((left != Type.Int && left != Type.Float) ||
                    (right != Type.Int && right != Type.Float))
                {
                    Errors.ReportError(context.GetChild(1).Payload as Antlr4.Runtime.IToken,
                        $"Operator {context.GetChild(1).GetText()} cannot be used with {left} and {right}.");
                    Types.Put(context, Type.Error);
                    return Type.Error;
                }

                // ak jeden je float → výsledok je float (automatická konverzia)
                if (left == Type.Float || right == Type.Float)
                {
                    Types.Put(context, Type.Float);
                    return Type.Float;
                }

                Types.Put(context, Type.Int);
                return Type.Int;
            }

            // modulo: % — iba int × int → int
            if (context.GetChild(1)?.GetText() == "%")
            {
                var left = Visit(context.expression(0));
                var right = Visit(context.expression(1));

                if (left == Type.Error || right == Type.Error)
                {
                    Types.Put(context, Type.Error);
                    return Type.Error;
                }

                if (left != Type.Int || right != Type.Int)
                {
                    Errors.ReportError(context.GetChild(1).Payload as Antlr4.Runtime.IToken,
                        $"Operator % can be used only with integers.");
                    Types.Put(context, Type.Error);
                    return Type.Error;
                }

                Types.Put(context, Type.Int);
                return Type.Int;
            }

            // && a || — B × B → B
            if (context.GetChild(1)?.GetText() is "&&" or "||")
            {
                var left = Visit(context.expression(0));
                var right = Visit(context.expression(1));

                if (left == Type.Error || right == Type.Error)
                {
                    Types.Put(context, Type.Error);
                    return Type.Error;
                }

                if (left != Type.Bool || right != Type.Bool)
                {
                    Errors.ReportError(context.GetChild(1).Payload as Antlr4.Runtime.IToken,
                        $"Operator {context.GetChild(1).GetText()} can be used only with bool.");
                    Types.Put(context, Type.Error);
                    return Type.Error;
                }

                Types.Put(context, Type.Bool);
                return Type.Bool;
            }

            // ! — B → B (unárny)
            if (context.GetChild(0)?.GetText() == "!")
            {
                var operand = Visit(context.expression(0));

                if (operand == Type.Error)
                {
                    Types.Put(context, Type.Error);
                    return Type.Error;
                }

                if (operand != Type.Bool)
                {
                    Errors.ReportError(context.GetChild(0).Payload as Antlr4.Runtime.IToken,
                        "Operator ! can be used only with bool.");
                    Types.Put(context, Type.Error);
                    return Type.Error;
                }

                Types.Put(context, Type.Bool);
                return Type.Bool;
            }

            // < a > — int alebo float → bool
            if (context.GetChild(1)?.GetText() is "<" or ">")
            {
                var left = Visit(context.expression(0));
                var right = Visit(context.expression(1));

                if (left == Type.Error || right == Type.Error)
                {
                    Types.Put(context, Type.Error);
                    return Type.Error;
                }

                if ((left != Type.Int && left != Type.Float) ||
                    (right != Type.Int && right != Type.Float))
                {
                    Errors.ReportError(context.GetChild(1).Payload as Antlr4.Runtime.IToken,
                        $"Operator {context.GetChild(1).GetText()} can be used only with int or float.");
                    Types.Put(context, Type.Error);
                    return Type.Error;
                }

                Types.Put(context, Type.Bool);
                return Type.Bool;
            }

            // == a != — int, float alebo string → bool
            if (context.GetChild(1)?.GetText() is "==" or "!=")
            {
                var left = Visit(context.expression(0));
                var right = Visit(context.expression(1));

                if (left == Type.Error || right == Type.Error)
                {
                    Types.Put(context, Type.Error);
                    return Type.Error;
                }

                // typy musia byť rovnaké (alebo int/float kombinácia)
                if (left != right &&
                    !(left == Type.Int && right == Type.Float) &&
                    !(left == Type.Float && right == Type.Int))
                {
                    Errors.ReportError(context.GetChild(1).Payload as Antlr4.Runtime.IToken,
                        $"Cannot compare {left} with {right}.");
                    Types.Put(context, Type.Error);
                    return Type.Error;
                }

                Types.Put(context, Type.Bool);
                return Type.Bool;
            }

            // konkatenácia reťazcov: . — S × S → S
            if (context.GetChild(1)?.GetText() == ".")
            {
                var left = Visit(context.expression(0));
                var right = Visit(context.expression(1));

                if (left == Type.Error || right == Type.Error)
                {
                    Types.Put(context, Type.Error);
                    return Type.Error;
                }

                if (left != Type.String || right != Type.String)
                {
                    Errors.ReportError(context.GetChild(1).Payload as Antlr4.Runtime.IToken,
                        "Operator . can be used only with strings.");
                    Types.Put(context, Type.Error);
                    return Type.Error;
                }

                Types.Put(context, Type.String);
                return Type.String;
            }

            // unárny mínus: - — I → I alebo F → F
            if (context.GetChild(0)?.GetText() == "-" && context.expression().Length == 1)
            {
                var operand = Visit(context.expression(0));

                if (operand == Type.Error)
                {
                    Types.Put(context, Type.Error);
                    return Type.Error;
                }

                if (operand != Type.Int && operand != Type.Float)
                {
                    Errors.ReportError(context.GetChild(0).Payload as Antlr4.Runtime.IToken,
                        "Unary minus can be used only with int or float.");
                    Types.Put(context, Type.Error);
                    return Type.Error;
                }

                Types.Put(context, operand);
                return operand;
            }

            // zátvorky: ( expression ) — iba prejdi dovnútra
            if (context.GetChild(0)?.GetText() == "(")
            {
                var type = Visit(context.expression(0));
                Types.Put(context, type);
                return type;
            }

            //hlbšie do stromu
            return VisitChildren(context);
        }

        // keď narazíme na deklaráciu napr. "int a, b, c;"
        public override Type VisitDeclaration(PJP_ProjektParser.DeclarationContext context)
        {
            // zisti typ z pravidla "type" v gramatike
            // context.type() → uzol "type" v strome
            // context.type().GetText() → "int", "float", "bool", "string"
            var typeName = context.type().GetText();

            // string na enum
            Type type = typeName switch
            {
                "int" => Type.Int,
                "float" => Type.Float,
                "bool" => Type.Bool,
                "string" => Type.String,
                _ => Type.Error
            };

            foreach (var id in context.ID())
            {
                // pridaj premennú do SymbolTable
                // ak už existuje, SymbolTable automaticky zavolá Errors.ReportError
                SymbolTables.Add(id.Symbol, type);
            }

            return type;
        }

        // READ: "read a, b, c;"
        // iba kontrolujeme že premenné existujú a sú deklarované
        public override Type VisitReadStatement(PJP_ProjektParser.ReadStatementContext context)
        {
            foreach (var id in context.ID())
            {
                // zisti typ premennej zo SymbolTable
                // ak neexistuje, SymbolTable automaticky zavolá Errors.ReportError
                // priradenie do _ aby sme sa vyhli chybe "expression cannot be used as statement"
                var _ = SymbolTables[id.Symbol];
            }

            return Type.Error; // read nevracia zmysluplný typ
        }

        // WRITE: "write expr1, expr2, ...;"
        // kontrolujeme typy všetkých výrazov
        public override Type VisitWriteStatement(PJP_ProjektParser.WriteStatementContext context)
        {
            foreach (var expr in context.expression())
            {
                // navštív každý výraz a skontroluj jeho typ
                var type = Visit(expr);

                if (type == Type.Error)
                {
                    return Type.Error;
                }
            }

            return Type.Error; // write nevracia zmysluplný typ
        }

        // IF: "if (condition) statement [else statement]"
        public override Type VisitIfStatement(PJP_ProjektParser.IfStatementContext context)
        {
            // skontroluj podmienku — musí byť bool
            var condType = Visit(context.expression());

            if (condType != Type.Bool && condType != Type.Error)
            {
                Errors.ReportError(context.expression().Start,
                    $"Condition in if statement must be bool, got {condType}.");
            }

            // navštív then vetvu
            Visit(context.statement(0));

            // navštív else vetvu ak existuje
            if (context.statement().Length > 1)
            {
                Visit(context.statement(1));
            }

            return Type.Error; // if nevracia zmysluplný typ
        }

        // WHILE: "while (condition) statement"
        public override Type VisitWhileStatement(PJP_ProjektParser.WhileStatementContext context)
        {
            // skontroluj podmienku — musí byť bool
            var condType = Visit(context.expression());

            if (condType != Type.Bool && condType != Type.Error)
            {
                Errors.ReportError(context.expression().Start,
                    $"Condition in while statement must be bool, got {condType}.");
            }

            // navštív telo cyklu
            Visit(context.statement());

            return Type.Error; // while nevracia zmysluplný typ
        }
    }
}