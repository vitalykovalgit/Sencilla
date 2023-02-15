namespace Sencilla.Component.Security
{
    public static class ConstraintExpressionParser
    {
        public delegate ParseAction ParseAction(char c, ConstraintExpressionBuilder ctx);

        public static IConstraintExpression? Parse(string? expr)
        {
            if (expr == null) 
                return null;

            ParseAction action = @expression;
            var context = new ConstraintExpressionBuilder();

            foreach (var c in expr)
                action = action(c, context);

            context.Finish();

            return context.ToExpression();
        }

        static ParseAction @expression(char c, ConstraintExpressionBuilder ctx)
        {
            switch (c)
            {
                case '(': ctx.OpenComplex();    return @expression;
                case ')': ctx.CloseComplex();   return @expression;
                case ' ': /*do nothing*/;       return @expression;
                default : ctx.OpenExpression(); return @field(c, ctx);
            }
        }

        static ParseAction @field(char c, ConstraintExpressionBuilder ctx)
        {
            switch (c)
            {
                case '=': ctx.CloseField();   return @operator(c, ctx);
                case '>': ctx.CloseField();   return @operator(c, ctx);
                case '<': ctx.CloseField();   return @operator(c, ctx);
                case ' ': ctx.CloseField();   return @operator(c, ctx);
                default : ctx.AppendField(c); return @field;
            }
        }

        static ParseAction @operator(char c, ConstraintExpressionBuilder ctx)
        {
            switch (c)
            {
                case '=': ctx.AppendOperator(c); return @operator2;
                case '>': ctx.AppendOperator(c); return @operator2;
                case '<': ctx.AppendOperator(c); return @operator2;
                case '!': ctx.AppendOperator(c); return @operator2;
                case 'i': ctx.AppendOperator(c); return @operator2;
                case 'l': ctx.AppendOperator(c); return @operator2;

                case ' ': /* do nothing */ ;     return @operator;
                default : ctx.CloseOperator();   return @value(c, ctx);
            }
        }

        static ParseAction @operator2(char c, ConstraintExpressionBuilder ctx)
        {
            switch (c)
            {
                // >= or <= or !=
                case '=': ctx.AppendOperator(c); return @operator;
                
                // in
                case 'n': ctx.AppendOperator(c); return @operator;
                
                // like 
                case 'i': ctx.AppendOperator(c); return @operator;
                case 'k': ctx.AppendOperator(c); return @operator;
                case 'e': ctx.AppendOperator(c); return @operator;
                
                // if whitespace or other character close operator 
                case ' ': ctx.CloseOperator();   return @value(c, ctx);
                default : ctx.CloseOperator();   return @value(c, ctx);
            }
        }

        static ParseAction @value(char c, ConstraintExpressionBuilder ctx)
        {
            switch (c)
            {
                case ' ': /* do nothing */        return @value;
                case '"': ctx.OpenValueString(); return @valuestring;
                case '-': ctx.OpenNumberValue(c); return @valuenumber;
                case '1': ctx.OpenNumberValue(c); return @valuenumber;
                case '2': ctx.OpenNumberValue(c); return @valuenumber;
                case '3': ctx.OpenNumberValue(c); return @valuenumber;
                case '4': ctx.OpenNumberValue(c); return @valuenumber;
                case '5': ctx.OpenNumberValue(c); return @valuenumber;
                case '6': ctx.OpenNumberValue(c); return @valuenumber;
                case '7': ctx.OpenNumberValue(c); return @valuenumber;
                case '8': ctx.OpenNumberValue(c); return @valuenumber;
                case '9': ctx.OpenNumberValue(c); return @valuenumber;
                case '[': ctx.OpenArrayValue();   return @valuearray;
                case '{': ctx.OpenPlaceValue();   return @valueplaceholder;
                default : ctx.OpenValueString();  return @valueconstant(c, ctx);
            }
        }

        static ParseAction @valueconstant(char c, ConstraintExpressionBuilder ctx)
        {
            switch (c)
            {
                case '"': ctx.CloseValueString(); return @expression;
                default : ctx.AppendValue(c);     return @valuestring;
            }
        }

        static ParseAction @valueplaceholder(char c, ConstraintExpressionBuilder ctx)
        {
            switch (c)
            {
                case ' ': /* do nothing */;      return @valueplaceholder;
                case '}': ctx.ClosePlaceValue(); return @valueplaceholder;
                default : ctx.AppendValue(c);    return @valueplaceholder;
            }
        }

        static ParseAction @valuearray(char c, ConstraintExpressionBuilder ctx)
        {
            switch (c)
            {
                case '"': ctx.CloseValueString(); return @expression;
                default: ctx.AppendValue(c); return @valuestring;
            }
        }

        static ParseAction @valuestring(char c, ConstraintExpressionBuilder ctx)
        {
            switch (c)
            {
                case '"': ctx.CloseValueString(); return @expression;
                default : ctx.AppendValue(c);     return @valuestring;
            }
        }

        static ParseAction @valuenumber(char c, ConstraintExpressionBuilder ctx)
        {
            switch (c)
            {
                case '0': ctx.AppendValue(c); return @valuenumber;
                case '1': ctx.AppendValue(c); return @valuenumber;
                case '2': ctx.AppendValue(c); return @valuenumber;
                case '3': ctx.AppendValue(c); return @valuenumber;
                case '4': ctx.AppendValue(c); return @valuenumber;
                case '5': ctx.AppendValue(c); return @valuenumber;
                case '6': ctx.AppendValue(c); return @valuenumber;
                case '7': ctx.AppendValue(c); return @valuenumber;
                case '8': ctx.AppendValue(c); return @valuenumber;
                case '9': ctx.AppendValue(c); return @valuenumber;
                case ' ': ctx.CloseNumberValue(); return @expression;
                default : ctx.CloseNumberValue(); return @expression;
            }
        }
    }
}
