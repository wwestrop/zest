using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Zest {

    /// <summary>
    /// Rewrites a method as a first class function. This has the effect that the method is
    /// assignable in code at runtime, allowing "monkey patching". This doesn't allow the function to
    /// access a class's private members, but is meant as a syntactically nicer way of stubbing/mocking. 
    /// </summary>
    internal class MethodRewriter {

        internal FieldDeclarationSyntax foo(MethodDeclarationSyntax f) {            

            var retType = GetTypeName(f.ReturnType);
            var funcActionTypeParams = GetFuncTypesList(f);         // if zero ? if void ? if intrinsic type ? if reference type ? if parametric type ?

            var typeRoot = retType == null ? "Action" : "Func";

            string typeParamsList = "";
            if (funcActionTypeParams.Any()) {
                typeParamsList = $"<{ string.Join(", ", funcActionTypeParams) }>";
            }

            var s = $"public {typeRoot}{typeParamsList} {f.Identifier.Text}";

            var l = GetLambdaDeclSyntax(f);

            var replacement = $"{s} = {l};";


            var parsed = SyntaxFactory.ParseSyntaxTree(replacement).GetRoot() as CompilationUnitSyntax;
            return parsed.Members[0] as FieldDeclarationSyntax;
        }

        private string GetLambdaDeclSyntax(MethodDeclarationSyntax f) {

            var s = $"({ string.Join(", ", f.ParameterList.Parameters) }) => { f.Body.ToString() }";

            return s;
        }

        private IEnumerable<string> GetFuncTypesList(MethodDeclarationSyntax f) {

            var l = new List<string>();
            l.AddRange(f.ParameterList.Parameters.Select(p => GetTypeName(p.Type)));

            var retType = GetTypeName(f.ReturnType);
            if (retType != null) {
                l.Add(retType);
            }

            return l;
        }
        
        
        private string GetTypeName(TypeSyntax f) {
            bool isVoid = f is PredefinedTypeSyntax && ((PredefinedTypeSyntax)f).Keyword.Text == "void";
            if(isVoid) {
                return null;
            }

            var primitiveType = (f as PredefinedTypeSyntax)?.Keyword.Text;
            var referenceType = (f as IdentifierNameSyntax)?.Identifier.Text;

            return primitiveType ?? referenceType;
        }

    }
}
