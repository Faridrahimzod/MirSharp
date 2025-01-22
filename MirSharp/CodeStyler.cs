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
    /// <summary>
    /// Вспомогательный класс для проверки кода в файле на корректное срабатывание и кодстайл
    /// </summary>
    internal class CodeStyler
    {   

        string fileToAnalys;
        public CodeStyler() { }
        public CodeStyler(string path)
        {
            fileToAnalys = Path.GetFullPath(path);
        }
        
        /// <summary>
        /// Проверяет на корректное компилирование кода в файле
        /// </summary>
        /// <returns>Отчёт о проверке</returns>
        internal string ErrorAnalyser()
        {
            if (!File.Exists(fileToAnalys))
            {
                return "Введён неправильный путь к файлу";
            }
            else
            {
                string code = File.ReadAllText(fileToAnalys);
                string isError = "";
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
                
                tree = null;
                return isError;

            }
        }

        /// <summary>
        /// Проверяет кодстайл кода ф файле, согласно стандартам Microsoft
        /// </summary>
        /// <returns>Отчёт о проверке</returns>
        internal string StyleAnalyser()
        {
            string ans = "";
            if (!File.Exists(fileToAnalys))
            {
                return "Введён неправильный путь к файлу";
            }
            else
            {
                string code = File.ReadAllText(fileToAnalys);
                SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
                var root = tree.GetRoot();
                if (CheckCodeStyle(root).Length > 0) 
                {
                    ans += $"Ошибки стиля кода в файле {fileToAnalys}\n";
                }
                ans += CheckCodeStyle(root);

                tree = null;
                return ans;
            }
        }









        /// <summary>
        /// Вспомогательный статический метод для проверки соответсию кодстайла
        /// </summary>
        /// <param name="root"></param>
        /// <returns>Отчёт о проверке</returns>
        static string CheckCodeStyle(SyntaxNode root)
        {
            string ans = "";

            ans += CheckIndentation(root);
            ans += "\n";

            ans += CheckNamingConventions(root);
            ans += "\n";

            ans += CheckComments(root);
            ans += "\n";
            
            return ans;
        }
        /// <summary>
        /// Вспомогательный статический метод для проверки отсупов
        /// </summary>
        /// <param name="root"></param>
        /// <returns></returns>
        private static string CheckIndentation(SyntaxNode root)
        {
            
            string ans = "";

            var lines = root.ToString().Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (!string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith("//"))
                {
                    int expectedIndent = GetExpectedIndentLevel(line);
                    int actualIndent = line.Length - line.TrimStart().Length;

                    if (actualIndent != expectedIndent * 4) 
                    {
                        ans += ($"Ошибка отступа в строке {i + 1}: ожидается {expectedIndent * 4} пробелов, найдено {actualIndent}\n");
                    }
                }
                ans += "\n";
            }
            return ans;
        }

        private static int GetExpectedIndentLevel(string line)
        {
            if (line.Contains("{")) return 1;
            if (line.Contains("}")) return 0;
            return 1; 
        }

        /// <summary>
        /// Вспомогательный статический метод для проверки правильного наименования методов и переменных
        /// </summary>
        /// <param name="root"></param>
        /// <returns>Отчёт о проверке</returns>
        private static string CheckNamingConventions(SyntaxNode root)
        {
            string ans = "";

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

        /// <summary>
        /// Вспомгательный статический метод для проверки правильности написания комментариев
        /// </summary>
        /// <param name="root"></param>
        /// <returns>Отчёт о проверке</returns>
        private static string CheckComments(SyntaxNode root)
        {
            string ans = "";

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
