grammar PJP_Projekt;


program : statement* EOF ;

statement
    : declaration
    | expressionStatement
    | readStatement
    | writeStatement
    | block
    | ifStatement
    | whileStatement
    | ';'
    ;

declaration     : type ID (',' ID)* ';' ;
expressionStatement : expression ';' ;
readStatement   : 'read' ID (',' ID)* ';' ;
writeStatement  : 'write' expression (',' expression)* ';' ;
block           : '{' statement* '}' ;
ifStatement     : 'if' '(' expression ')' statement ('else' statement)? ;
whileStatement  : 'while' '(' expression ')' statement ;

type : 'int' | 'float' | 'bool' | 'string' ;

expression
    : '!' expression
    | '-' expression
    | expression ('*' | '/' | '%') expression
    | expression ('+' | '-' | '.') expression
    | expression ('<' | '>') expression
    | expression ('==' | '!=') expression
    | expression '&&' expression
    | expression '||' expression
    | <assoc=right> ID '=' expression
    | '(' expression ')'
    | ID
    | INT
    | FLOAT
    | BOOL
    | STRING
    ;

INT    : [0-9]+ ;
FLOAT  : [0-9]+ '.' [0-9]* ;
BOOL   : 'true' | 'false' ;
STRING : '"' (~["\r\n])* '"' ;
ID     : [a-zA-Z][a-zA-Z0-9]* ;

LINE_COMMENT : '//' ~[\r\n]* -> skip ;
WS           : [ \t\r\n]+   -> skip ;