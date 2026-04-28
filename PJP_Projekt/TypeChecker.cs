using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime.Misc;
using Antlr4.Runtime.Tree;

namespace PJP_Projekt
{
    // prechádza strom a kontroluje typy
    public class TypeChecker : PJP_ProjektBaseVisitor<Type>
    {
        // pamäť premenných
        public SymbolTables SymbolTables { get; } = new SymbolTables();

        // typ každého uzlu stromu
        public ParseTreeProperty<Type> Types { get; } = new ParseTreeProperty<Type>();

        // výrazy
        public override Type VisitExpression(PJP_ProjektParser.ExpressionContext context)
        {
            // INT literál
            if (context.INT() != null)
            {
                Types.Put(context, Type.Int);
                return Type.Int;
            }

            // FLOAT literál
            if (context.FLOAT() != null)
            {
                Types.Put(context, Type.Float);
                return Type.Float;
            }

            // BOOL literál
            if (context.BOOL() != null)
            {
                Types.Put(context, Type.Bool);
                return Type.Bool;
            }

            // STRING literál
            if (context.STRING() != null)
            {
                Types.Put(context, Type.String);
                return Type.String;
            }

            // premenná
            if (context.ID() != null && context.GetChild(1)?.GetText() != "=")
            {
                var type = SymbolTables[context.ID().Symbol];
                Types.Put(context, type);
                return type;
            }

            // priradenie
            if (context.ID() != null && context.GetChild(1)?.GetText() == "=")
            {
                var rightType = Visit(context.expression(0));
                var varType = SymbolTables[context.ID().Symbol];

                if (rightType == Type.Error || varType == Type.Error)
                {
                    Types.Put(context, Type.Error);
                    return Type.Error;
                }

                // automatická konverzia int → float
                if(varType == Type.Float && rightType == Type.Int)
                {
                    Types.Put(context, Type.Float);
                    return Type.Float;
                }

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

            // aritmetické operátory + - * /
            if (context.GetChild(1)?.GetText() is "+" or "-" or "*" or "/")
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
                        $"Operator {context.GetChild(1).GetText()} cannot be used with {left} and {right}.");
                    Types.Put(context, Type.Error);
                    return Type.Error;
                }

                if (left == Type.Float || right == Type.Float)
                {
                    Types.Put(context, Type.Float);
                    return Type.Float;
                }

                Types.Put(context, Type.Int);
                return Type.Int;
            }

            // modulo %
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

            // logické && ||
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

            // logické !
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

            // relačné < >
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

            // porovnanie == !=
            if (context.GetChild(1)?.GetText() is "==" or "!=")
            {
                var left = Visit(context.expression(0));
                var right = Visit(context.expression(1));

                if (left == Type.Error || right == Type.Error)
                {
                    Types.Put(context, Type.Error);
                    return Type.Error;
                }

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

            // konkatenácia .
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

            // unárny mínus
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

            // zátvorky
            if (context.GetChild(0)?.GetText() == "(")
            {
                var type = Visit(context.expression(0));
                Types.Put(context, type);
                return type;
            }

            return VisitChildren(context);
        }

        // deklarácia
        public override Type VisitDeclaration(PJP_ProjektParser.DeclarationContext context)
        {
            var typeName = context.type().GetText();

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
                SymbolTables.Add(id.Symbol, type);
            }

            return type;
        }

        // read
        public override Type VisitReadStatement(PJP_ProjektParser.ReadStatementContext context)
        {
            foreach (var id in context.ID())
            {
                // priradenie do _ aby sme sa vyhli chybe "expression cannot be used as statement"
                var _ = SymbolTables[id.Symbol];
            }

            return Type.Error;
        }

        // write
        public override Type VisitWriteStatement(PJP_ProjektParser.WriteStatementContext context)
        {
            foreach (var expr in context.expression())
            {
                var type = Visit(expr);

                if (type == Type.Error)
                {
                    return Type.Error;
                }
            }

            return Type.Error;
        }

        // if
        public override Type VisitIfStatement(PJP_ProjektParser.IfStatementContext context)
        {
            var condType = Visit(context.expression());

            if (condType != Type.Bool && condType != Type.Error)
            {
                Errors.ReportError(context.expression().Start,
                    $"Condition in if statement must be bool, got {condType}.");
            }

            Visit(context.statement(0));

            if (context.statement().Length > 1)
            {
                Visit(context.statement(1));
            }

            return Type.Error;
        }

        // while
        public override Type VisitWhileStatement(PJP_ProjektParser.WhileStatementContext context)
        {
            var condType = Visit(context.expression());

            if (condType != Type.Bool && condType != Type.Error)
            {
                Errors.ReportError(context.expression().Start,
                    $"Condition in while statement must be bool, got {condType}.");
            }

            Visit(context.statement());

            return Type.Error;
        }
    }
}