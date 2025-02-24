using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace MirSharp
{
    /// <summary>
    /// Класс для анализа кода на ошибки и соответствие стилю Microsoft.
    /// </summary>
    internal class CodeStyler
    {
        private readonly string fileToAnalyze;

        public CodeStyler(string path)
        {
            fileToAnalyze = Path.GetFullPath(path);
        }

        /// <summary>
        /// Проверяет, компилируется ли код без ошибок.
        /// </summary>
        public string ErrorAnalyzer()
        {
            if (!File.Exists(fileToAnalyze))
            {
                return "Ошибка: указанный файл не существует.";
            }

            string code = File.ReadAllText(fileToAnalyze);

            // Парсим код с явным указанием версии языка
            var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code, parseOptions);

            // Добавляем все необходимые системные сборки
            var systemAssemblyPath = typeof(object).Assembly.Location;
            var runtimeAssemblyPath = typeof(System.Runtime.GCSettings).Assembly.Location;
            var consoleAssemblyPath = typeof(Console).Assembly.Location;

            var references = new[]
            {
                MetadataReference.CreateFromFile(systemAssemblyPath),
                MetadataReference.CreateFromFile(runtimeAssemblyPath),
                MetadataReference.CreateFromFile(consoleAssemblyPath),
                MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location)
            };

            // Создаем компиляцию с включенными диагностиками
            var compilation = CSharpCompilation.Create("TempAssembly")
                .AddReferences(references)
                .AddSyntaxTrees(syntaxTree)
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            // Получаем все диагностики (синтаксические и семантические)
            var syntaxDiagnostics = syntaxTree.GetDiagnostics();
            var compilationDiagnostics = compilation.GetDiagnostics();
            var allDiagnostics = syntaxDiagnostics.Concat(compilationDiagnostics);

            // Фильтруем ошибки
            var errors = allDiagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .OrderBy(d => d.Location.GetLineSpan().StartLinePosition.Line)
                .ToList();

            if (errors.Count == 0)
            {
                return "Ошибки компиляции не найдены.\r\n";
            }

            // Формируем сообщение
            return "Ошибки компиляции:\r\n" + string.Join("\r\n", errors
                .Select(d => $"- {d.GetMessage()} (Строка: {d.Location.GetLineSpan().StartLinePosition.Line + 1})")) + "\r\n";
        }

        /// <summary>
        /// Проверяет соответствие кода стандартам Microsoft.
        /// </summary>
        public string StyleAnalyzer()
        {
            

            string code = File.ReadAllText(fileToAnalyze);
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(code);
            var diagnostics = syntaxTree.GetDiagnostics();

            // Если есть ошибки компиляции, возвращаем их
            

            SyntaxNode root = syntaxTree.GetRoot();
            List<string> issues = new List<string>();

            issues.AddRange(CheckIndentation(root));
            issues.AddRange(CheckNamingConventions(root));
            issues.AddRange(CheckComments(root));

            return issues.Any() ? "Нарушения кодстайла:\r\n" + string.Join("\r\n", issues) : "Код соответствует стандартам Microsoft.";
        }

        /// <summary>
        /// Проверяет отступы в коде с учетом вложенности блоков.
        /// </summary>
        private List<string> CheckIndentation(SyntaxNode root)
        {
            var issues = new List<string>();
            // Получаем текст и его разбиение на строки
            SourceText text = root.SyntaxTree.GetText();
            var lines = text.Lines;
            bool isBrekingDefault = false;

            // Текущий ожидаемый отступ (в пробелах)
            int currentIndent = 0;
            // Размер одного уровня отступа
            const int spacesPerIndent = 4;
            // Стек для отслеживания отступа конструкции switch:
            // когда мы входим в switch, запоминаем отступ строки с ключевым словом switch,
            // а для меток case/default ожидается switchIndent + spacesPerIndent.
            var switchStack = new Stack<int>();

            // Обрабатываем каждую строку
            for (int i = 0; i < lines.Count; i++)
            {
                string lineText = lines[i].ToString();

                // Пропускаем пустые или состоящие только из пробелов строки
                if (string.IsNullOrWhiteSpace(lineText))
                    continue;

                // Фактический отступ – число пробелов в начале строки
                int actualIndent = lineText.TakeWhile(c => c == ' ').Count();
                string trimmed = lineText.TrimStart();

                // Определяем ожидаемый отступ для текущей строки
                int expectedIndent = currentIndent;

                // Если строка начинается с закрывающей фигурной скобки,
                // то ожидается отступ на один уровень меньше.
                if (trimmed.StartsWith("}"))
                {
                    expectedIndent = currentIndent - spacesPerIndent;
                }
                

                // Если строка начинается с "case" или "default"
                // и мы находимся внутри switch (есть сохранённый switchIndent),
                // то ожидаемый отступ равен switchIndent + spacesPerIndent.
                if ((trimmed.StartsWith("case ") || trimmed.StartsWith("default")) &&
                     switchStack.Count > 0)
                {
                    expectedIndent = switchStack.Peek() + spacesPerIndent;
                    currentIndent = switchStack.Peek() + 2 * spacesPerIndent;
                    
                }
                if (trimmed.StartsWith("default") && switchStack.Count > 0)
                {
                    isBrekingDefault = true;

                }
                if (trimmed.StartsWith("break") && isBrekingDefault)
                {
                    currentIndent = switchStack.Peek() + spacesPerIndent;
                }



                if (actualIndent != expectedIndent)
                {
                    issues.Add($"- Строка {i + 1}: Необходимо {expectedIndent} количество пробелов, обнаружено {actualIndent}.\r\n");
                }

                // Если строка начинается с "switch", запоминаем её текущий отступ.
                // (Можно доработать, чтобы исключить совпадения внутри комментариев.)
                if (trimmed.StartsWith("switch"))
                {
                    switchStack.Push(currentIndent);
                }
                

                // Анализируем символы строки для корректировки уровня отступа.
                // Здесь предполагается, что фигурные скобки не встречаются в строковых литералах/комментариях.
                foreach (char c in lineText)
                {
                    if (c == '{')
                    {
                        currentIndent += spacesPerIndent;
                    }
                    else if (c == '}')
                    {
                        currentIndent -= spacesPerIndent;
                        // Если вышли из блока switch, то сбрасываем уровень switch (если достигли сохранённого отступа)
                        if (switchStack.Count > 0 && currentIndent == switchStack.Peek())
                        {
                            switchStack.Pop();
                        }
                    }
                }
            }

            return issues;
        }

        /// <summary>
        /// Проверяет соглашения именования для методов, переменных, полей и констант.
        /// </summary>
        private List<string> CheckNamingConventions(SyntaxNode root)
        {
            var issues = new List<string>();

            // Проверка методов
            foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                if (!char.IsUpper(method.Identifier.ValueText[0]))
                {
                    issues.Add($"- Метод '{method.Identifier.ValueText}' должен начинаться с заглавной буквы (Строка: {method.GetLocation().GetLineSpan().StartLinePosition.Line + 1})\r\n");
                }
            }

            // Проверка локальных переменных и параметров
            foreach (var variable in root.DescendantNodes().OfType<VariableDeclaratorSyntax>())
            {
                if (variable.Parent is VariableDeclarationSyntax varDecl &&
                    varDecl.Parent is LocalDeclarationStatementSyntax)
                {
                    if (!char.IsLower(variable.Identifier.ValueText[0]) && variable.Identifier.ValueText[0] != '_')
                    {
                        issues.Add($"- Локальная переменная '{variable.Identifier.ValueText}' должна начинаться с маленькой буквы или '_' (Строка: {variable.GetLocation().GetLineSpan().StartLinePosition.Line + 1})\r\n");
                    }
                }
            }

            // Проверка полей и констант
            foreach (var field in root.DescendantNodes().OfType<FieldDeclarationSyntax>())
            {
                foreach (var variable in field.Declaration.Variables)
                {
                    bool isConst = field.Modifiers.Any(m => m.IsKind(SyntaxKind.ConstKeyword));
                    if (isConst)
                    {
                        if (!variable.Identifier.ValueText.All(c => char.IsUpper(c) || c == '_'))
                        {
                            issues.Add($"- Константа '{variable.Identifier.ValueText}' должна состоять из заглавных букв и '_' (Строка: {variable.GetLocation().GetLineSpan().StartLinePosition.Line + 1})");
                        }
                    }
                    else if (field.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword)))
                    {
                        if (variable.Identifier.ValueText.Length == 0 || (variable.Identifier.ValueText[0] != '_' && !char.IsLower(variable.Identifier.ValueText[0])))
                        {
                            issues.Add($"- Приватное поле '{variable.Identifier.ValueText}' должно начинаться с '_' или маленькой буквы (Строка: {variable.GetLocation().GetLineSpan().StartLinePosition.Line + 1})");
                        }
                    }
                }
            }

            return issues;
        }

        /// <summary>
        /// Проверяет наличие XML-документации у публичных методов.
        /// </summary>
        private List<string> CheckComments(SyntaxNode root)
        {
            var issues = new List<string>();

            foreach (var method in root.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                bool hasXmlDoc = method.GetLeadingTrivia()
                    .Any(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                              t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));

                if (!hasXmlDoc && method.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)))
                {
                    issues.Add($"- Публичный метод '{method.Identifier.ValueText}' должен иметь XML-документацию (Строка: {method.GetLocation().GetLineSpan().StartLinePosition.Line + 1})");
                }
            }

            return issues;
        }
    }
}