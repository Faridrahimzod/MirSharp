using System;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MirSharp
{
    internal class CodeStyler
    {
        static string fileToAnalys;
        public CodeStyler() { }
        public CodeStyler(string path)
        {
            fileToAnalys = path;
        }
        
        internal static string ErrorAnalyser()
        {
            if (!File.Exists(fileToAnalys))
            {
                return "Введён неправильный путь к файлу";
            }
            else
            {
                string code = File.ReadAllText(fileToAnalys);
                string isError = null;
                SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
                var root = tree.GetRoot();
                var diagnostics = tree.GetDiagnostics();
                if (diagnostics.Any())
                {
                    isError = $"Ошибки в файле {fileToAnalys} : \n";
                    foreach (var diagnostic in diagnostics)
                    {
                        isError += $"- {diagnostic.GetMessage()} в строке {diagnostic.Location.GetLineSpan().StartLinePosition.Line + 1}\n";
                    }
                }
                return isError;

            }
        }

        internal string StyleAnalyser(string fileToAnalys)
        {
            if (!File.Exists(fileToAnalys))
            {
                return "Введён неправильный путь к файлу";
            }
            else
            {
                string code = File.ReadAllText(fileToAnalys);
                SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
                var root = tree.GetRoot();
                return CheckCodeStyle(root);
            }
        }










        static string CheckCodeStyle(SyntaxNode root)
        {
            string ans = "";
            // Проверка отступов
            ans += CheckIndentation(root);

            // Проверка наименования переменных и методов
            ans += CheckNamingConventions(root);

            // Проверка комментариев
            ans += CheckComments(root);
            return ans;
        }

        private static string CheckIndentation(SyntaxNode root)
        {
            string ans = "";
            // Проверяем, что каждая строка кода начинается с правильного отступа
            var lines = root.ToString().Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (!string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith("//"))
                {
                    int expectedIndent = GetExpectedIndentLevel(line);
                    int actualIndent = line.Length - line.TrimStart().Length;

                    if (actualIndent != expectedIndent * 4) // 4 пробела на уровень отступа
                    {
                        ans += ($"Ошибка отступа в строке {i + 1}: ожидается {expectedIndent * 4} пробелов, найдено {actualIndent}\n");
                    }
                }
            }
            return ans;
        }

        private static int GetExpectedIndentLevel(string line)
        {
            // Простая логика для определения уровня отступа
            if (line.Contains("{")) return 1;
            if (line.Contains("}")) return 0;
            return 1; // По умолчанию
        }

        private static string CheckNamingConventions(SyntaxNode root)
        {
            string ans = "";
            // Проверка наименования переменных и методов
            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();
            foreach (var method in methods)
            {
                if (!char.IsUpper(method.Identifier.ValueText[0]))
                {
                    ans += ($"Метод '{method.Identifier.ValueText}' должен начинаться с заглавной буквы (строка: {method.GetLocation().GetLineSpan().StartLinePosition.Line + 1}) \n");
                }
            }

            var variables = root.DescendantNodes().OfType<VariableDeclaratorSyntax>();
            foreach (var variable in variables)
            {
                if (!char.IsLower(variable.Identifier.ValueText[0]))
                {
                    ans += ($"Переменная '{variable.Identifier.ValueText}' должна начинаться с маленькой буквы (строка: {variable.GetLocation().GetLineSpan().StartLinePosition.Line + 1})\n");
                }
            }
            return ans;
        }

        private static string CheckComments(SyntaxNode root)
        {
            string ans = "";
            // Проверка наличия комментариев для публичных методов
            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();
            foreach (var method in methods)
            {
                var leadingTrivia = method.GetLeadingTrivia();
                bool hasComment = leadingTrivia.Any(t => t.IsKind(SyntaxKind.SingleLineCommentTrivia) || t.IsKind(SyntaxKind.MultiLineCommentTrivia));

                if (!hasComment && method.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)))
                {
                    ans += ($"Публичный метод '{method.Identifier.ValueText}' должен иметь комментарий (строка: {method.GetLocation().GetLineSpan().StartLinePosition.Line + 1})\n");
                }
            }
            return ans;
        }
    }
}
