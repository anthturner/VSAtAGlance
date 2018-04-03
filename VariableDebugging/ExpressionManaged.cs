using EnvDTE;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace VSAtAGlance.VariableDebugging
{
    class ExpressionManaged
    {
        public bool IsValidValue { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Value { get; set; }
        public ExpressionManaged Parent { get; set; }
        public List<ExpressionManaged> Members { get; set; }

        private static string[] EXPRESSED_SINGULARLY = new[] {
            "System.Guid",
            "System.Guid?",
            "System.TimeSpan",
            "System.TimeSpan?",
            "System.DateTime",
            "System.DateTime?",
            "System.DateTimeOffset",
            "System.DateTimeOffset?",
            "System.Decimal",
            "System.Decimal?",
            "decimal",
            "decimal?",
            "System.Char",
            "System.Char?",
            "char",
            "char?",
            "System.Single",
            "System.Single?",
            "float",
            "float?"
        };

        private static int RECURSION_TRIPWIRE_DEPTH = 100;
        private static string[] SERIALIZABLE_EXPRESSION_NAMES = new[] { "Raw View", "Static Members" };
        private static Regex ClassNameExtractorRegex { get; set; }
        static ExpressionManaged()
        {
            ClassNameExtractorRegex = new Regex("(?<baseClass>.*){(?<subClass>.*)}");
            ClassNameExtractorRegex.Match("a {b}");
        }

        public ExpressionManaged(Expression expression)
        {
            Name = expression.Name;
            Type = expression.Type;
            Value = expression.Value;
            IsValidValue = expression.IsValidValue;
            Members = new List<ExpressionManaged>();
        }

        public ExpressionManaged(Expression expression, int depth) : this(expression)
        {
            if (expression.DataMembers != null && expression.DataMembers.Count > 0 && depth > 0)
            {
                foreach (Expression dm in expression.DataMembers)
                    Members.Add(new ExpressionManaged(dm, this, depth - 1));
            }
        }

        public ExpressionManaged(Expression expression, ExpressionManaged parent, int depth) : this(expression, depth)
        {
            Parent = parent;
        }

        public dynamic ExtractDynamic()
        {
            if (Members.Count > 0)
                return Members.Select(m => m.ExtractDynamic()).ToArray();
            return Value;
        }

        public static int InferDepth(Expression expression, int currentDepth = 0, int maxDepth = 0)
        {
            string expressionType;

            if (currentDepth == 0)
                expressionType = ClassNameExtractorRegex.Match(expression.Type).Groups["subClass"].Value.Trim();
            else
                expressionType = ClassNameExtractorRegex.Match(expression.Type).Groups["baseClass"].Value.Trim();

            if (expression.DataMembers.Count > 0 && !EXPRESSED_SINGULARLY.Contains(expressionType))
            {
                List<Expression> dataMembers = expression.DataMembers.Cast<Expression>().ToList();
                for (int i = 0; i < dataMembers.Count; i++)
                {
                    Expression currentMember = dataMembers[i];

                    if (currentMember.Name == "base" && currentMember.Type.Contains("{"))
                        dataMembers.AddRange(currentMember.DataMembers.Cast<Expression>());
                    else
                        if (maxDepth >= RECURSION_TRIPWIRE_DEPTH) return maxDepth;

                    if (currentDepth > maxDepth)
                        maxDepth = currentDepth;

                    return InferDepth(currentMember, currentDepth + 1, maxDepth);
                }
            }
            return currentDepth;
        }
    }
}
