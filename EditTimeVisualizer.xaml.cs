using EnvDTE;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text.Editor;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Controls;
using VSAtAGlance.Targeting;
using VSAtAGlance.VariableDebugging;

namespace VSAtAGlance
{
    /// <summary>
    /// Interaction logic for EditTimeVisualizer.xaml
    /// </summary>
    public partial class EditTimeVisualizer : UserControl
    {
        public EditTimeVisualizer()
        {
            InitializeComponent();
        }

        public static EditTimeVisualizer Create(IWpfTextView editor, SyntaxNode node)
        {
            var model = EditTimeVisualizerModel.Create(node);
            if (model.IsValid)
                return Create(editor, model);
            return null;
        }

        public static EditTimeVisualizer Create(IWpfTextView editor, EditTimeVisualizerModel model)
        {
            return editor.VisualElement.Dispatcher.Invoke(() => new EditTimeVisualizer() { DataContext = model });
        }
    }

    public class EditTimeVisualizerModel
    {
        //public string ID { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }

        public bool IsValid => !string.IsNullOrEmpty(Value);

        public static EditTimeVisualizerModel Create(GazeTarget target)
        {
            var model = new EditTimeVisualizerModel();
            //model.ID = target.Start + "_" + target.Length;

            model.Name = ((ISymbol)target.DataModel).Name;
            model.Value = AttemptGetDebuggerValue(model.Name);

            return model;
        }

        public static EditTimeVisualizerModel Create(SyntaxNode node)
        {
            var model = new EditTimeVisualizerModel();
            //model.ID = node.Span.Start + "_" + node.Span.Length;

            Debug.WriteLine($"SyntaxNode is {node.GetType()}");

            if (node is ParameterSyntax)
            {
                model.Name = ((ParameterSyntax)node).Identifier.Text;
                model.Value = AttemptGetDebuggerValue(model.Name);
            }
            else if (node is IdentifierNameSyntax && ((IdentifierNameSyntax)node).IsVar)
            {
                model.Name = ((IdentifierNameSyntax)node).Identifier.Text;
                model.Value = AttemptGetDebuggerValue(model.Name);
            }
            else if (node is QualifiedNameSyntax)
            {
                // todo - dotted syntax
            }
            return model;
        }

        private static string AttemptGetDebuggerValue(string symbol)
        {
            var dte = Package.GetGlobalService(typeof(DTE)) as DTE;
            if (dte.Debugger.CurrentMode == dbgDebugMode.dbgBreakMode)
            {
                var exprParser = dte.Debugger.GetExpression(symbol);
                if (exprParser.IsValidValue)
                {
                    var managed = new ExpressionManaged(exprParser, ExpressionManaged.InferDepth(exprParser));
                    using (var writer = new System.IO.StringWriter())
                    {
                        JsonSerializer.Create().Serialize(writer, managed.ExtractDynamic());
                        //ObjectDumper.Dumper.Dump(managed.ExtractDynamic(), symbol, writer);
                        return writer.ToString();
                    };
                }
            }
            return null;
        }
    }
}
