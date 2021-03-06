﻿using Code.Generation.Roslyn;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using static Microsoft.CodeAnalysis.CSharp.SyntaxKind;

namespace Alberto.FastMap.CodeGen
{
    public class ImplementCloneableInterfaceGenerator : DocumentCodeGenerator
    {
        /// <summary>
        /// Public Constructor.
        /// </summary>
        /// <param name="attributeData"></param>
        /// <inheritdoc />
        public ImplementCloneableInterfaceGenerator(AttributeData attributeData) : base(attributeData) { }

        private static SyntaxToken EndOfStatementToken => Token(SemicolonToken);

        private static SyntaxToken ObjectKeywordToken => Token(ObjectKeyword);

        private static SyntaxToken PrivateKeywordToken => Token(PrivateKeyword);

        private static SyntaxToken PublicKeywordToken => Token(PublicKeyword);

        private static SyntaxToken VoidKeywordToken => Token(VoidKeyword);

        /// <summary>
        /// Returns the <see cref="UsingDirectiveSyntax"/> for the <typeparamref name="T"/>
        /// <see cref="Type.Namespace"/>.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <see cref="Type.Namespace"/>
        private static UsingDirectiveSyntax GetUsingNamespaceDirective<T>()
            => UsingDirective(IdentifierName(typeof(T).Namespace));

        private static MemberDeclarationSyntax GetICloneableCloneMethodDecl(string className)
            => MethodDeclaration(PredefinedType(ObjectKeywordToken), Identifier(nameof(ICloneable.Clone)))
                .WithModifiers(TokenList(PublicKeywordToken))
                .WithExpressionBody(ArrowExpressionClause(
                    ObjectCreationExpression(IdentifierName(className))
                        .WithArgumentList(ArgumentList(SingletonSeparatedList(
                            Argument(ThisExpression())
                        )))
                )).WithSemicolonToken(EndOfStatementToken);

        // TODO: TBD: we will default to this for now... but this should be conditional whether the method exists...
        // TODO: TBD: possibly capture attribute parameters: i.e. generate initialize member, invoke base ctor, copy ctor accessibility, etc
        /// <summary>
        /// 
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="other">The Type of the Other parameter.</param>
        /// <returns></returns>
        private static MemberDeclarationSyntax GetInitializeMethodDecl(string methodName, string other)
            => MethodDeclaration(PredefinedType(VoidKeywordToken), Identifier(methodName))
                .WithModifiers(TokenList(PrivateKeywordToken))
                .WithParameterList(ParameterList(
                    SingletonSeparatedList(
                        Parameter(Identifier(nameof(other))).WithType(IdentifierName(other)))
                ))
                .WithBody(Block());

        /// <summary>
        /// 
        /// </summary>
        /// <param name="other">The Type of the Other parameter.
        /// As a Copy Constructor, also informs the name itself.</param>
        /// <param name="invocationName"></param>
        /// <returns></returns>
        private static MemberDeclarationSyntax GetCopyCtorDecl(string other, string invocationName)
            => ConstructorDeclaration(Identifier(other))
                .WithModifiers(TokenList(PrivateKeywordToken))
                .WithParameterList(ParameterList(SingletonSeparatedList(
                    Parameter(Identifier(nameof(other)))
                        .WithType(IdentifierName(other))
                )))
                .WithBody(Block(SingletonList(ExpressionStatement(
                    InvocationExpression(IdentifierName(invocationName))
                        .WithArgumentList(ArgumentList(SingletonSeparatedList(
                            Argument(IdentifierName(nameof(other)))
                        )))
                ))));

        /// <summary>
        /// Now, this is a fairly trivial, if a bit naive Code Generation implementation.
        /// We are solely interested in making sure that fundamental building blocks of
        /// Code Generation, such as inserting a Leading Preamble Text, are taking place
        /// successfully.
        /// </summary>
        /// <inheritdoc />
        public override Task GenerateAsync(DocumentTransformationContext context, IProgress<Diagnostic> progress
            , CancellationToken cancellationToken)
        {
            IEnumerable<CodeGeneratorDescriptor> Generate()
            {
                var namespaceDecl = context.SourceCompilationUnit.DescendantNodesAndSelf().OfType<NamespaceDeclarationSyntax>().Single();
                var classDecl = context.SourceCompilationUnit.DescendantNodesAndSelf().OfType<ClassDeclarationSyntax>().Single();

                // ReSharper disable once InconsistentNaming
                const string Initialize = nameof(Initialize);
                var other = $"{classDecl.Identifier}";

                yield return new CodeGeneratorDescriptor
                {
                    CompilationUnits =
                    {
                        CompilationUnit()
                            .WithUsings(SingletonList(GetUsingNamespaceDirective<ICloneable>()))
                            .WithMembers(SingletonList<MemberDeclarationSyntax>(
                                NamespaceDeclaration(namespaceDecl.Name)
                                    .WithMembers(SingletonList<MemberDeclarationSyntax>(
                                        ClassDeclaration(classDecl.Identifier)
                                            .WithModifiers(classDecl.Modifiers)
                                            .WithBaseList(BaseList(
                                                SingletonSeparatedList<BaseTypeSyntax>(
                                                    SimpleBaseType(IdentifierName(nameof(ICloneable)))
                                                )
                                            ))
                                            .WithMembers(List(new[]
                                            {
                                                GetCopyCtorDecl(other, Initialize),
                                                GetInitializeMethodDecl(Initialize, other),
                                                GetICloneableCloneMethodDecl(other)
                                            }))
                                    ))
                            ))
                    }
                };
            }

            void RunGenerate()
            {
                foreach (var d in Generate())
                {
                    Descriptors.Add(d);
                }
            }

            return Task.Run(RunGenerate, cancellationToken);
        }
    }
}
