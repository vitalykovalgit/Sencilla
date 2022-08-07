using System.Text;

namespace Sencilla.Component.Security
{
    public class ConstraintExpressionBuilder
    {
        Stack<IConstraintExpression> Stack = new Stack<IConstraintExpression>();
        IConstraintExpression? Current = null;

        StringBuilder? Operator;
        StringBuilder? Field;
        StringBuilder? Value;
        bool IsNumber = false;
        bool IsString = false;
        //bool IsNumber = false;

        public void Push()
        {
            if (Current != null)
            {
                Stack.Push(Current);
                Current = null;
            }
        }

        public void Pop()
        {
            if (Stack.Count > 0)
            {
                var parent = Stack.Pop();
                
                if (Current != null)
                    parent.Add(Current);

                Current = parent;
            }
        }

        public void OpenComplex() 
        {
            Push();
            Current = new ConstraintExpressionComplex();
        }

        public void CloseComplex() 
        {
            Pop();
        }

        public void OpenExpression() 
        {
            Current = new ConstraintExpression();
        }

        public void CloseExpression() 
        {
            // do nothing here 
        }

        public void OpenField(char c) 
        {
            Field = new StringBuilder().Append(c);
        }

        public void AppendField(char c)
        {
            if (Field == null)
                Field = new StringBuilder();

            Field?.Append(c);
        }

        public void CloseField()
        {
            if (Current != null && Field != null)
                Current.Field = Field.ToString();

            Field = null;
        }

        public void OpenValue(char c) 
        {
            Value = new StringBuilder().Append(c);
        }

        public void AppendValue(char c)
        {
            if (Value == null)
                Value = new StringBuilder();

            Value?.Append(c);
        }

        public void CloseValue() 
        {
            if (Current != null && Value != null)
                Current.Value = Value.ToString();

            Value = null;
        }

        public void OpenOperator(char c) 
        {
            Operator = new StringBuilder().Append(c);
        }

        public void AppendOperator(char c) 
        {
            if (Operator == null)
                Operator = new StringBuilder();

            Operator?.Append(c);
        }

        public void CloseOperator() 
        {
            if (Current != null && Operator != null)
                Current.Operator = Operator.ToString();

            Operator = null;
        }

        public void OpenValueString()
        {
            IsString = true;
        }

        public void CloseValueString()
        {
            if (!IsString)
                return;

            CloseValue();
            IsString = false;
        }

        public void OpenNumberValue(char c)
        {
            IsNumber = true;
            OpenValue(c);
        }

        public void CloseNumberValue()
        {
            if (!IsNumber)
                return;

            if (Current != null && Value != null)
                Current.Value = long.Parse(Value.ToString());

            Value = null;
            IsNumber = false;
        }

        public void OpenArrayValue()
        {

        }

        public void OpenPlaceValue()
        { 
        }

        public void ClosePlaceValue()
        {
        }

        public void Finish()
        {
            CloseField();
            CloseOperator();
            
            CloseValueString();
            CloseNumberValue();
            CloseValue();
        }

        public IConstraintExpression? ToExpression()
        {
            return Current;
        }
    }
}
